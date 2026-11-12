"""Run with Blender in background using Catapults.blend. Does not alter the source file."""
from pathlib import Path
import bpy
from mathutils import Matrix, Vector

out = Path(bpy.data.filepath).parents[2] / 'Assets/_Project/Art/Catapults'
out.mkdir(parents=True, exist_ok=True)
scene = bpy.context.scene
scene.frame_set(1)
scene.render.engine = 'CYCLES'
scene.cycles.samples = 1
scene.render.bake.margin = 8
converted = {}

def mat_copy(source):
    if source.name in converted: return converted[source.name]
    m = source.copy()
    for node in list(m.node_tree.nodes):
        if node.type != 'TEX_COORD': continue
        for socket, attribute in [('Generated','SourceGenerated'), ('Object','SourceLocal')]:
            links = list(node.outputs[socket].links)
            if links:
                n = m.node_tree.nodes.new('ShaderNodeAttribute');n.attribute_name = attribute
                for link in links:m.node_tree.links.new(n.outputs['Vector'],link.to_socket)
    converted[source.name] = m
    return m

def bake(obj, name, kind):
    existing=out/(name+'.png')
    if existing.exists():
        print('Reusing '+name,flush=True)
        return bpy.data.images.load(str(existing),check_existing=True)
    im=bpy.data.images.new(name,width=2048,height=2048,alpha=False)
    if kind=='NORMAL':im.colorspace_settings.name='Non-Color'
    for m in obj.data.materials:
        n=next((n for n in m.node_tree.nodes if n.name=='Unity Bake Target'),None)
        if n is None:n=m.node_tree.nodes.new('ShaderNodeTexImage');n.name='Unity Bake Target'
        n.image=im;m.node_tree.nodes.active=n
    if kind=='DIFFUSE':bpy.ops.object.bake(type=kind,pass_filter={'COLOR'})
    else:bpy.ops.object.bake(type=kind)
    im.filepath_raw=str(out/(name+'.png'));im.file_format='PNG';im.save()
    print('Baked '+name,flush=True)
    return im

def group_of(o):
    while o:
        for token,idx in [('ARM_PIVOT',1),('WINCH_PIVOT',2),('RELEASE_PIVOT',3)]:
            if o.name.endswith(token):return idx
        o=o.parent
    return 0

def category(mat):
    if mat.name.startswith('Amber rune'):return 2
    if any(token in mat.name.lower() for token in ('iron','bronze')):return 1
    return 0

for level in (1,2,3):
    print('Exporting level '+str(level),flush=True)
    scene.frame_set(1)
    source_root=bpy.data.objects[f'L{level} | Catapult_L{level}']
    sources=list(source_root.children_recursive)
    source_controls=[None]+[bpy.data.objects[f'L{level} | '+n] for n in ('ARM_PIVOT','WINCH_PIVOT','RELEASE_PIVOT')]
    dg=bpy.context.evaluated_depsgraph_get()
    bpy.ops.object.select_all(action='DESELECT')
    copies=[]
    for source in sources:
        if source.type not in {'MESH','CURVE'} or source.name.endswith('Reload draw rope'):continue
        ev=source.evaluated_get(dg)
        me=bpy.data.meshes.new_from_object(ev,preserve_all_data_layers=True,depsgraph=dg)
        points=[v.co.copy() for v in me.vertices]
        if not points:continue
        lo=[min(p[i] for p in points) for i in range(3)]
        size=[max(max(p[i] for p in points)-lo[i],1e-6) for i in range(3)]
        for name,coords in [('SourceGenerated', [[(p[i]-lo[i])/size[i] for i in range(3)] for p in points]),('SourceLocal',points)]:
            attr=me.attributes.new(name,'FLOAT_VECTOR','POINT')
            attr.data.foreach_set('vector',[c for v in coords for c in v])
        attr=me.attributes.new('ExportGroup','INT','POINT')
        attr.data.foreach_set('value',[group_of(source)]*len(points))
        me.transform(source_root.matrix_world.inverted()@source.matrix_world)
        for i,m in enumerate(list(me.materials)):me.materials[i]=mat_copy(m)
        o=bpy.data.objects.new('Export part',me);scene.collection.objects.link(o);o.select_set(True);copies.append(o)
    bpy.context.view_layer.objects.active=copies[0];bpy.ops.object.join();atlas_obj=bpy.context.object
    for uv in list(atlas_obj.data.uv_layers):atlas_obj.data.uv_layers.remove(uv)
    bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(island_margin=.006,correct_aspect=False,scale_to_bounds=True)
    bpy.ops.object.mode_set(mode='OBJECT')
    name=f'Catapult_L{level}'
    color=bake(atlas_obj,name+'_BaseColor','DIFFUSE')
    bake(atlas_obj,name+'_Normal','NORMAL')
    mats=[]
    for label,metal in [('Base',0),('Metal',.7),('Rune',0)]:
        m=bpy.data.materials.new(name+'_'+label);m.use_nodes=True
        p=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
        t=m.node_tree.nodes.new('ShaderNodeTexImage');t.image=color;m.node_tree.links.new(t.outputs['Color'],p.inputs['Base Color'])
        p.inputs['Metallic'].default_value=metal;mats.append(m)
    export_root=bpy.data.objects.new(name,None);scene.collection.objects.link(export_root)
    controls=[];data=atlas_obj.data;group=data.attributes['ExportGroup'].data
    cats=[category(m) for m in data.materials]
    for index,label in enumerate(('Body','Arm','Winch','Release')):
        polygons=[p for p in data.polygons if group[p.vertices[0]].value==index]
        indices=sorted({i for p in polygons for i in p.vertices});remap={v:i for i,v in enumerate(indices)}
        me=bpy.data.meshes.new(name+'_'+label)
        pivot=Vector((0,0,0)) if index==0 else (source_root.matrix_world.inverted()@source_controls[index].matrix_world).translation
        me.from_pydata([data.vertices[i].co-pivot for i in indices],[],[[remap[i] for i in p.vertices] for p in polygons]);me.update()
        uv=me.uv_layers.new(name='UVMap')
        for dest,src in zip(me.polygons,polygons):
            dest.material_index=cats[src.material_index];dest.use_smooth=src.use_smooth
            for dl,sl in zip(dest.loop_indices,src.loop_indices):uv.data[dl].uv=data.uv_layers.active.data[sl].uv
        for m in mats:me.materials.append(m)
        o=bpy.data.objects.new(label,me);scene.collection.objects.link(o);o.parent=export_root;o.location=pivot;o.rotation_mode='QUATERNION';controls.append(o)
    socket=bpy.data.objects.new('ProjectileSocket',None);scene.collection.objects.link(socket);socket.parent=controls[1]
    socket.location=(source_controls[1].matrix_world.inverted()@bpy.data.objects[f'L{level} | PROJECTILE_SOCKET'].matrix_world).translation
    markers=[]
    for label,position,parent in [('RopeDrum',(0,.58,.40),export_root),('RopeGuide',(0,-.66,.35),export_root),('DrawHook',(0,-1.0,.50),controls[1])]:
        o=bpy.data.objects.new(label,None);scene.collection.objects.link(o);o.parent=parent
        o.location=Vector(position)-(controls[1].location if parent==controls[1] else Vector())
        markers.append(o)
    for f in range(1,97):
        scene.frame_set(f)
        for index in (1,2,3):
            o=controls[index];o.matrix_local=source_root.matrix_world.inverted()@source_controls[index].matrix_world
            o.keyframe_insert('location',frame=f);o.keyframe_insert('rotation_quaternion',frame=f)
    scene.frame_set(1)
    bpy.ops.object.select_all(action='DESELECT')
    for o in [export_root,socket]+controls+markers:o.select_set(True)
    bpy.context.view_layer.objects.active=export_root
    bpy.ops.export_scene.fbx(filepath=str(out/(name+'.fbx')),use_selection=True,axis_forward='-Z',axis_up='Y',
        apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',bake_anim=True,bake_anim_use_all_actions=False,
        bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,path_mode='STRIP',mesh_smooth_type='OFF')
    print('EXPORTED '+name+' polygons='+str(len(data.polygons)),flush=True)
    for o in [atlas_obj,socket]+markers+controls+[export_root]:bpy.data.objects.remove(o,do_unlink=True)
print('ALL CATAPULTS EXPORTED',flush=True)
