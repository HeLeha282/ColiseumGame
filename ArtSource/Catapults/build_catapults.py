import bpy, math, random
from mathutils import Vector
from math import sin, cos, pi

OUT = 'C:/Users/Lesha/Desktop/ColiseumGame/ArtSource/Catapults'
random.seed(81)
scene = bpy.data.scenes.new('Catapults | Three upgrade levels')
bpy.context.window.scene = scene
scene.unit_settings.system = 'METRIC'
scene.render.fps = 24
scene.frame_start, scene.frame_end = 1, 96

def material(name, color, metal=0, rough=.5):
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*color, 1)
    m.use_nodes = True
    p = next(n for n in m.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
    p.inputs['Base Color'].default_value = (*color,1)
    p.inputs['Metallic'].default_value = metal
    p.inputs['Roughness'].default_value = rough
    return m

oak = bpy.data.materials['Honey oak • painted grain']
walnut = bpy.data.materials['Elite walnut • painted grain']
iron = bpy.data.materials['Forged iron']
bronze = bpy.data.materials['Warm bronze']
red = bpy.data.materials['Crimson cloth']
gold = bpy.data.materials['Banner gold thread']
rune = bpy.data.materials['Amber rune']
rope = material('Catapult | flax rope',(.48,.30,.13),0,.85)
dark = material('Catapult | deep recess',(.027,.015,.009),0,.75)
iron_edge = material('Catapult | iron rivets',(.22,.19,.16),.72,.36)
gold_edge = material('Catapult | polished bronze edges',(.72,.40,.10),.72,.29)
woodcut = material('Catapult | cut oak endgrain',(.41,.21,.079),0,.65)
floor_mat = material('Catapult | studio charcoal',(.065,.077,.086),0,.8)

coll = None
root = None
prefix = ''

def attach(o, name, mat=None, parent=None):
    o.name = prefix + name
    for c in list(o.users_collection): c.objects.unlink(o)
    coll.objects.link(o)
    if mat: o.data.materials.append(mat)
    p = parent if parent is not None else root
    if p:
        bpy.context.view_layer.update()
        w = o.matrix_world.copy()
        o.parent = p
        o.matrix_world = w
    return o

def bevel(o, amount=.025, seg=2):
    m=o.modifiers.new('Soft carved edges','BEVEL');m.width=amount;m.segments=seg
    m=o.modifiers.new('Weighted face normals','WEIGHTED_NORMAL');m.keep_sharp=True;m.weight=40
    return o

def box(name, loc, size, mat, edge=.025, parent=None):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc)
    o=bpy.context.object;o.dimensions=size
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    attach(o,name,mat,parent)
    if edge: bevel(o,edge)
    return o

def beam(name,a,b,width,depth,mat,parent=None,edge=.025):
    a,b=Vector(a),Vector(b)
    o=box(name,(a+b)/2,(width,depth,(b-a).length),mat,edge,parent)
    o.rotation_mode='QUATERNION';o.rotation_quaternion=(b-a).to_track_quat('Z','Y')
    return o

def cyl(name,loc,radius,length,mat,axis=(1,0,0),parent=None,verts=20):
    bpy.ops.mesh.primitive_cylinder_add(vertices=verts,radius=radius,depth=length,location=loc)
    o=bpy.context.object
    o.rotation_mode='QUATERNION';o.rotation_quaternion=Vector(axis).to_track_quat('Z','Y')
    attach(o,name,mat,parent);bevel(o,.009,2)
    for f in o.data.polygons: f.use_smooth=len(f.vertices)==4
    return o

def ball(name,loc,r,mat,parent=None,normal=None):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=10,ring_count=6,radius=r,location=loc)
    o=bpy.context.object
    if normal:
        o.scale=(1,1,.43);o.rotation_mode='QUATERNION';o.rotation_quaternion=Vector(normal).to_track_quat('Z','Y')
    attach(o,name,mat,parent)
    for p in o.data.polygons:p.use_smooth=True
    return o

def curve(name,points,radius,mat,parent=None):
    d=bpy.data.curves.new(prefix+name,'CURVE');d.dimensions='3D';d.resolution_u=1
    d.bevel_depth=radius;d.bevel_resolution=2
    s=d.splines.new('POLY');s.points.add(len(points)-1)
    for p,co in zip(s.points,points):p.co=(*co,1)
    o=bpy.data.objects.new(prefix+name,d);coll.objects.link(o)
    if mat:d.materials.append(mat)
    if parent is None:parent=root
    if parent:
        o.parent=parent;o.matrix_parent_inverse=parent.matrix_world.inverted()
    return o

def mesh(name,verts,faces,mat,parent=None):
    d=bpy.data.meshes.new(prefix+name);d.from_pydata(verts,[],faces);d.update()
    o=bpy.data.objects.new(prefix+name,d);coll.objects.link(o)
    if mat:d.materials.append(mat)
    if parent is None:parent=root
    if parent:
        o.parent=parent;o.matrix_parent_inverse=parent.matrix_world.inverted()
    return o

def empty(name,loc,parent=None):
    o=bpy.data.objects.new(prefix+name,None);coll.objects.link(o);o.location=loc
    o.empty_display_size=.13
    bpy.context.view_layer.update()
    if parent:
        w=o.matrix_world.copy();o.parent=parent;o.matrix_world=w
    bpy.context.view_layer.update()
    return o

def torus(name,loc,major,minor,mat,parent=None,axis=(0,0,1)):
    pts=[]
    q=Vector(axis).to_track_quat('Z','Y')
    for i in range(65):
        pts.append(Vector(loc)+q@Vector((major*cos(i*2*pi/64),major*sin(i*2*pi/64),0)))
    return curve(name,pts,minor,mat,parent)

def plate(name,x,yz,thick,mat):
    vs=[(x+dx,y,z) for dx in (-thick/2,thick/2) for y,z in yz]
    n=len(yz);fs=[tuple(range(n-1,-1,-1)),tuple(range(n,n*2))]
    fs += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    return bevel(mesh(name,vs,fs,mat),.018,2)

def leaf(name,center,up,side,length,width,mat,parent=None):
    c=Vector(center);u=Vector(up).normalized();s=Vector(side).normalized();n=s.cross(u).normalized()
    vs=[c-u*length/2,c-s*width/2,c+u*length/2,c+s*width/2,c+n*.009]
    return mesh(name,vs,[(0,1,4),(1,2,4),(2,3,4),(3,0,4)],mat,parent)

def wreath(name,center,right,up,r,mat,parent=None):
    c=Vector(center);rt=Vector(right);u=Vector(up)
    for sign in (-1,1):
        points=[]
        for i in range(19):
            t=i/18*2.05
            points.append(c+rt*(sign*r*sin(t))+u*(-r*cos(t)))
        curve(name+' stem',points,.007,mat,parent)
        for j in range(7):
            t=.18+j*.255
            pos=c+rt*(sign*r*sin(t))+u*(-r*cos(t))
            tangent=rt*(sign*cos(t))+u*sin(t)
            out=rt*(sign*sin(t))-u*cos(t)
            leaf(name+' leaf',pos+out*r*.19,tangent*.65+out*.75,rt*cos(t)+u*(-sign*sin(t)),r*.50,r*.21,mat,parent)

def banner(level):
    width=.41 if level==1 else .49
    height=.43 if level==1 else .58
    top=1.99;y=.095
    nx,nz=14,16
    vs=[]
    for j in range(nz+1):
        t=j/nz
        for i in range(nx+1):
            x=(i/nx-.5)*width
            bottom=height+.09*(1-abs(x/(width/2)))
            if level==1:bottom+=.025*sin(i*2.3)
            z=top-t*bottom
            yy=y-.021*sin(i/nx*pi*3)*sin(t*pi)-.025*sin(t*pi)
            vs.append((x,yy,z))
    fs=[(j*(nx+1)+i,j*(nx+1)+i+1,(j+1)*(nx+1)+i+1,(j+1)*(nx+1)+i) for j in range(nz) for i in range(nx)]
    o=mesh('Crimson pennant',vs,fs,red)
    m=o.modifiers.new('Cloth thickness','SOLIDIFY');m.thickness=.008
    for p in o.data.polygons:p.use_smooth=True
    cyl('Banner rod',(0,y,top+.018),.021,width+.12,bronze if level>1 else iron)
    for x in (-width/2-.035,width/2+.035):ball('Banner fastener',(x,y-.02,top+.018),.033,bronze if level>1 else iron_edge,normal=(0,-1,0))
    if level>1:
        inds=[j*(nx+1) for j in range(nz+1)]+[nz*(nx+1)+i for i in range(1,nx+1)]+[j*(nx+1)+nx for j in range(nz-1,-1,-1)]
        pts=[(vs[i][0]*.92,vs[i][1]-.013,vs[i][2]+.012) for i in inds]
        curve('Pennant gold edging',pts,.009,gold)
        wreath('Pennant laurel',(0,y-.07,top-.29),(1,0,0),(0,0,1),.115,gold)

def bowl(level,center,parent):
    # A real hollow stave bowl with an inner wall and closed bottom.
    cx,cy,cz=center
    profile=[(.085,-.255),(.16,-.23),(.25,-.16),(.31,-.06),(.34,.07),(.34,.14),(.292,.14),(.286,.055),(.253,-.065),(.18,-.15),(.07,-.183),(0,-.183),(0,-.255)]
    n=48;vs=[]
    for r,z in profile:
        for i in range(n):vs.append((cx+r*cos(i*2*pi/n),cy+r*sin(i*2*pi/n),cz+z))
    fs=[]
    for j in range(len(profile)-1):
        for i in range(n):fs.append((j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i))
    o=mesh('Hollow projectile cup',vs,fs,bronze if level==3 else oak,parent)
    o.data.materials.append(walnut if level<3 else bronze)
    for p in o.data.polygons:
        p.use_smooth=True
        if p.index//n>=6:p.material_index=1
    rim=bronze if level==3 else iron
    # Broad rim band, with bright rolled edges.
    prof=[(.347,.035),(.347,.151),(.29,.151),(.29,.132)]
    vs=[(cx+r*cos(i*2*pi/n),cy+r*sin(i*2*pi/n),cz+z) for r,z in prof for i in range(n)]
    fs=[(j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i) for j in range(3) for i in range(n)]
    band=mesh('Cup rim band',vs,fs,rim,parent)
    for p in band.data.polygons:p.use_smooth=True
    torus('Rolled cup lip',(cx,cy,cz+.15),.32,.024,gold_edge if level==3 else iron_edge,parent)
    torus('Cup bottom ferrule',(cx,cy,cz-.21),.174,.018,rim,parent)
    for i in range(12):
        a=i*2*pi/12
        if level<3:
            pts=[(cx+(r+.002)*cos(a),cy+(r+.002)*sin(a),cz+z) for r,z in profile[:5]]
            curve('Cup stave joint',pts,.004,dark,parent)
        ball('Cup rim rivet',(cx+.352*cos(a),cy+.352*sin(a),cz+.09),.022,gold_edge if level==3 else iron_edge,parent,(cos(a),sin(a),0))
    if level==3:
        # Small raised leaves conform to the curved outer bowl.
        for a0 in (-pi/2,0,pi/2,pi):
            for side in (-1,1):
                for k in range(5):
                    a=a0+side*(.08+k*.13);z=cz-.13+k*.034
                    rr=.27+(z-(cz-.13))*.35
                    c=(cx+(rr+.01)*cos(a),cy+(rr+.01)*sin(a),z)
                    tangent=Vector((-sin(a),cos(a),0))*side
                    leaf('Cup embossed laurel',c,tangent*.65+Vector((0,0,.9)),Vector((-sin(a),cos(a),0)),.09,.038,gold_edge,parent)
    return o

def anim_rot(o,axis,keys):
    o.rotation_mode='XYZ'
    for frame,degrees in keys:
        o.rotation_euler[axis]=math.radians(degrees)
        o.keyframe_insert(data_path='rotation_euler',index=axis,frame=frame,group=o.name)
    if o.animation_data and o.animation_data.action:o.animation_data.action.name=o.name+' | Fire and reload'

roots=[]
for level,offset in [(1,-2.9),(2,0),(3,2.9)]:
    prefix=f'L{level} | '
    coll=bpy.data.collections.new(f'CATAPULT LEVEL {level}');scene.collection.children.link(coll)
    root=None;root=empty(f'Catapult_L{level}',(0,0,0));roots.append(root)
    root['Level']=level;root['Purpose']='Stationary area-damage catapult; +Y forward; crew added later'
    wood=walnut if level==3 else oak
    metal=bronze if level==3 else iron
    accent=gold_edge if level==3 else iron_edge
    # Sled, recessed planks, and heavy corner shoes.
    for x in (-.67,.67):beam('Sled runner',(x,-1.10,.19),(x,1.02,.19),.23,.26,wood)
    for y in (-1.05,.96):beam('Sled end beam',(-.67,y,.20),(.67,y,.20),.25,.27,wood)
    for j in range(9):box('Deck plank',(0,-.88+j*.205,.24),(1.12,.195,.125),wood,.012)
    for x in (-.67,.67):
        for y in (-1.05,.96):
            box('Forged corner shoe',(x,y,.21),(.29,.29,.30),metal,.035)
            for normal in [(0,-1 if y<0 else 1,0),(-1 if x<0 else 1,0,0),(0,0,1)]:
                loc=Vector((x,y,.21))+Vector(normal)*.153
                ball('Corner boss',loc,.049,accent,normal=normal)
            if level==3:
                box('Corner top gold inlay',(x,y,.367),(.19,.19,.014),gold_edge,.012)
    # A-frame supports. The front legs leave clear space for the moving arm.
    for x in (-.59,.59):
        for y in (-.77,.77):
            beam('A-frame timber',(x,y,.34),(x,.27,1.99),.225,.23,wood)
            box('Foot collar',(x,y,.40),(.26,.25,.12),metal,.018)
            ball('Foot collar bolt',(x+(.14 if x>0 else -.14),y,.40),.035,accent,normal=(1 if x>0 else -1,0,0))
        box('Top cap',(x,.27,2.015),(.31,.34,.25),metal if level>1 else woodcut,.035)
        ball('Top cap bolt',(x,.085,2.015),.047,accent,normal=(0,-1,0))
        beam('Side lower stretcher',(x,-.74,.47),(x,.72,.47),.12,.14,wood)
        if level>1:
            sx=x+(.125 if x>0 else -.125)
            plate('Pivot reinforcement',sx,[(-.02,1.17),(.55,1.17),(.32,1.80),(.18,1.80)],.055,metal)
            for y,z in [(0,1.23),(.51,1.23),(.26,1.72)]:ball('Plate bolt',(sx+(.035 if x>0 else -.035),y,z),.032,accent,normal=(1 if x>0 else -1,0,0))
        if level==3:
            for yy in (-.77,.77):
                a=Vector((x,yy,.43));b=Vector((x,.27,1.90))
                for dx in (-.081,.081):curve('Timber bronze piping',[(a.x+dx,a.y-.045,a.z),(b.x+dx,b.y-.045,b.z)],.011,bronze)
    beam('Crown crossbeam',(-.62,.27,1.99),(.62,.27,1.99),.22,.23,wood)
    beam('Rear lower crossbar',(-.61,.73,.69),(.61,.73,.69),.15,.15,wood)
    # Axis and central rotating arm assembly, preserved as independent parts.
    pivot=empty('ARM_PIVOT',(0,.27,1.39),root)
    pivot['Animation']='Rotate local X; frame 1 loaded, frame 18 released, frame 90 reloaded'
    cyl('Transverse axle',(0,.27,1.39),.145,1.47,wood,parent=pivot)
    for x in (-.70,-.46,.46,.70):cyl('Axle ferrule',(x,.27,1.39),.178,.075,metal)
    for x in (-.77,.77):cyl('Axle end pin',(x,.27,1.39),.104,.055,accent)
    arm_end=(0,-.99,.40)
    beam('Throwing arm',(0,.29,1.39),arm_end,.205,.23,wood,pivot)
    cyl('Arm hub',(0,.27,1.39),.21,.30,wood,parent=pivot)
    for x in (-.17,.17):cyl('Arm hub binding',(x,.27,1.39),.22,.047,metal,parent=pivot)
    if level>1:
        aa=Vector((0,.27,1.39));bb=Vector(arm_end)
        for t in (.22,.67):
            c=aa.lerp(bb,t)
            b=beam('Arm reinforcement band',c-(bb-aa).normalized()*.052,c+(bb-aa).normalized()*.052,.239,.263,metal,pivot,.013)
    # Helical rope binding around the spoon neck.
    aa=Vector((0,.27,1.39));bb=Vector(arm_end);direction=(bb-aa).normalized()
    q=direction.to_track_quat('Z','Y');center=aa.lerp(bb,.62)
    pts=[]
    for i in range(241):
        t=i/240;a=t*6*2*pi
        pts.append(center+q@Vector((.151*cos(a),.151*sin(a),(t-.5)*.18)))
    curve('Arm rope binding',pts,.016,rope,pivot)
    box('Cup mounting saddle',(0,-.99,.52),(.25,.25,.105),metal,.018,pivot)
    bowl(level,(0,-1.025,.80),pivot)
    shot=empty('PROJECTILE_SOCKET',(0,-1.025,.84),pivot)
    # Rear winch with continuous rope coil, independent crank control.
    winch=empty('WINCH_PIVOT',(0,.58,.55),root)
    cyl('Winch drum',(0,.58,.55),.116,1.12,wood,parent=winch)
    for x in (-.52,.52):
        cyl('Winch end flange',(x,.58,.55),.177,.075,metal,parent=winch)
        box('Winch bearing foot',(x,.58,.37),(.13,.29,.24),metal,.02)
        cyl('Winch bearing pin',(x+(-.055 if x<0 else .055),.58,.55),.09,.06,accent)
    pts=[]
    for i in range(481):
        t=i/480;a=t*13*2*pi
        pts.append((-.37+t*.74,.58+.14*cos(a),.55+.14*sin(a)))
    curve('Wound winch rope',pts,.025,rope,winch)
    cyl('Crank shaft',(-.78,.58,.55),.048,.40,accent,parent=winch)
    beam('Winch crank',(-.94,.58,.55),(-.94,.33,.39),.075,.075,metal,winch,.014)
    cyl('Crank wooden grip',(-1.045,.33,.39),.052,.24,woodcut,parent=winch)
    cyl('Crank end knob',(-1.17,.33,.39),.057,.024,accent,parent=winch)
    # Release catch and red handle, deliberately visible from the operator side.
    box('Catch mounting shoe',(-.18,-.67,.33),(.18,.24,.085),metal,.013)
    catch=empty('RELEASE_PIVOT',(-.18,-.67,.38),root)
    beam('Retaining pawl',(-.18,-.67,.38),(-.18,-.72,.75),.075,.07,metal,catch,.012)
    beam('Catch hook',(-.18,-.72,.75),(0,-.72,.75),.055,.065,accent,catch,.012)
    cyl('Release cross shaft',(-.54,-.67,.38),.045,.78,metal,parent=catch)
    beam('Release handle',(-.92,-.67,.38),(-.92,-.54,.67),.06,.06,metal,catch,.01)
    beam('Crimson handle grip',(-.92,-.58,.58),(-.92,-.52,.72),.088,.09,red,catch,.015)
    # Draw rope has an animated attachment: released during firing, reattached for winding.
    cable=curve('Reload draw rope',[(0,.58,.40),(0,-.66,.35),(0,-.73,.64)],.018,rope)
    cable['Note']='Stylized detachable winch cable; crew hookup is reserved for the crew animation pass'
    cp=cable.data.splines[0].points
    for frame,angle,attached in [(1,0,1),(8,0,1),(10,0,0),(18,-62,0),(30,-60,0),(39,-60,0),(40,-60,1),(88,0,1),(96,0,1)]:
        end=Vector((0,-.73,.64))
        if attached:
            rel=end-Vector((0,.27,1.39));a=math.radians(angle)
            end=Vector((rel.x,rel.y*cos(a)-rel.z*sin(a),rel.y*sin(a)+rel.z*cos(a)))+Vector((0,.27,1.39))
        else:end=Vector((0,-.66,.32))
        cp[2].co=(*end,1);cp[2].keyframe_insert('co',frame=frame)
    # Decorative identity shared with the archer towers.
    banner(level)
    if level>1:
        for sx in (-.745,.745):
            sign=1 if sx>0 else -1
            yz=[(.05,1.20),(-.18,1.06),(-.14,.79),(.05,.67),(.24,.79),(.28,1.06)]
            plate('Heraldic side shield',sx,yz,.045,bronze if level==3 else iron)
            cy=.05;cz=.95
            if level==2:
                yz2=[(cy+(y-cy)*.82,cz+(z-cz)*.82) for y,z in yz]
                plate('Crimson shield enamel',sx+sign*.03,yz2,.015,red)
                wreath('Shield laurel',(sx+sign*.047,cy,cz),(0,1,0),(0,0,1),.105,gold)
            else:
                yz2=[(cy+(y-cy)*.76,cz+(z-cz)*.76) for y,z in yz]
                plate('Rune shield recess',sx+sign*.03,yz2,.014,dark)
                xx=sx+sign*.045
                curve('Amber rune spine',[(xx,cy-.045,cz-.15),(xx,cy-.045,cz+.15)],.014,rune)
                curve('Amber rune glyph',[(xx,cy-.045,cz+.15),(xx,cy+.085,cz+.06),(xx,cy-.045,cz-.015),(xx,cy+.085,cz-.08),(xx,cy-.045,cz-.15)],.014,rune)
            for y,z in yz:ball('Shield trim rivet',(sx+sign*.03,y,z),.018,accent,normal=(sign,0,0))
    # Crew markers are empties, not geometry, and can be used later in Unity.
    empty('CREW_LOADER',(.98,-.90,0),root)
    empty('CREW_WINCH',(-1.30,.55,0),root)
    anim_rot(pivot,0,[(1,0),(8,0),(10,-4),(16,-64),(20,-59),(25,-61),(32,-60),(40,-60),(88,0),(96,0)])
    anim_rot(catch,0,[(1,0),(8,0),(11,38),(30,38),(40,38),(84,38),(89,0),(96,0)])
    anim_rot(winch,0,[(1,0),(32,0),(40,0),(88,1080),(96,1080)])
    root.location.x=offset
    root['Footprint']='1.65 x 2.25 m including fittings; same on all levels'

prefix='';root=None
coll=bpy.data.collections.new('PRESENTATION | cameras and lights');scene.collection.children.link(coll)
box('Studio floor',(0,0,-.07),(200,200,.08),floor_mat,.0)
def area(name,loc,energy,color,size):
    d=bpy.data.lights.new(name,'AREA');d.energy=energy;d.color=color;d.shape='DISK';d.size=size
    o=bpy.data.objects.new(name,d);coll.objects.link(o);o.location=loc;o.rotation_euler=(Vector((0,0,.8))-o.location).to_track_quat('-Z','Y').to_euler()
area('Key softbox',(-3,-5,8),1500,(1,.80,.59),6)
area('Cool fill',(5,-1,6),1050,(.66,.78,1),5)
area('Golden rim',(0,5,7),1700,(1,.69,.37),5)
world=bpy.data.worlds.new('Catapult studio world');world.use_nodes=True
next(n for n in world.node_tree.nodes if n.type=='BACKGROUND').inputs['Color'].default_value=(.22,.26,.32,1)
next(n for n in world.node_tree.nodes if n.type=='BACKGROUND').inputs['Strength'].default_value=.45
scene.world=world
d=bpy.data.cameras.new('Catapults overview camera');cam=bpy.data.objects.new('Catapults overview camera',d);coll.objects.link(cam)
cam.location=(-7.5,-12,7.2);target=Vector((0,0,1.05));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
d.type='ORTHO';d.ortho_scale=10.2;scene.camera=cam
scene.render.resolution_x=1800;scene.render.resolution_y=1000;scene.render.resolution_percentage=100
try:scene.render.engine='CYCLES'
except TypeError:pass
scene.cycles.samples=48
scene.cycles.use_denoising=True
scene.render.image_settings.file_format='PNG'
for frame,name in [(1,'LOADED / idle'),(9,'RELEASE'),(16,'SHOT / arm raised'),(40,'RELOAD / winch'),(88,'LOCK / ready')]:scene.timeline_markers.new(name,frame=frame)
scene.frame_set(1)
scene['Asset notes']='Three fantasy catapult upgrade levels. Mechanical preview animation 1–96 at 24fps. Crew hookup and projectile flight belong to later gameplay animation. All moving parts have named pivots.'
for screen in bpy.data.screens:
    for a in screen.areas:
        if a.type=='VIEW_3D':
            a.spaces.active.region_3d.view_perspective='CAMERA'
            a.spaces.active.shading.type='MATERIAL'
bpy.ops.object.select_all(action='DESELECT')
roots[1].select_set(True);bpy.context.view_layer.objects.active=roots[1]
# Apply the clearance and rounded-neck refinements to regenerated assets as well.
exec(compile(open(OUT+'/refine_catapults.py', encoding='utf-8').read(), 'refine_catapults.py', 'exec'))
bpy.data.libraries.write(OUT+'/Catapults.blend',{scene},fake_user=True,compress=True)
print('Created',len(scene.objects),'objects in',scene.name)
print('Saved',OUT+'/Catapults.blend')
