"""Refine the existing catapult assets in place, keeping their animation pivots."""
import bpy, bmesh, math
from mathutils import Vector, Matrix

scene = bpy.context.scene
scene.frame_set(1)

def replace_mesh(obj, vertices, faces):
    # Input coordinates are in the level's rest space, independent of presentation offset.
    root = obj
    while root.parent: root = root.parent
    to_local = obj.matrix_world.inverted() @ root.matrix_world
    data = bpy.data.meshes.new(obj.name + ' refined mesh')
    data.from_pydata([to_local @ Vector(v) for v in vertices], [], faces)
    bm = bmesh.new()
    bm.from_mesh(data)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    bm.to_mesh(data)
    bm.free()
    data.update()
    for mat in obj.data.materials: data.materials.append(mat)
    obj.data = data
    for modifier in list(obj.modifiers): obj.modifiers.remove(modifier)
    for polygon in data.polygons: polygon.use_smooth = True

def rounded_arm(obj):
    # The square timber blends into a small curved oval neck under the bowl.
    stations = [
        ((0,.29,1.39),.1025,.115,8),
        ((0,.03,1.187),.1025,.115,8),
        ((0,-.25,.965),.1025,.115,7),
        ((0,-.47,.79),.101,.108,5),
        ((0,-.62,.67),.094,.087,3),
        ((0,-.73,.575),.083,.061,2),
        ((0,-.83,.512),.072,.045,2),
        ((0,-.94,.487),.064,.037,2),
        ((0,-1.025,.49),.055,.032,2),
        ((0,-1.065,.49),.025,.020,2),
        ((0,-1.078,.49),.004,.004,2),
    ]
    vertices=[];faces=[];n=32
    for j,(center,rx,ry,power) in enumerate(stations):
        a=Vector(stations[max(0,j-1)][0]);b=Vector(stations[min(len(stations)-1,j+1)][0])
        direction=(b-a).normalized()
        side=Vector((1,0,0));up=side.cross(direction).normalized()
        for k in range(n):
            angle=2*math.pi*k/n;u=math.cos(angle);v=math.sin(angle)
            u=math.copysign(abs(u)**(2/power),u);v=math.copysign(abs(v)**(2/power),v)
            vertices.append(Vector(center)+side*(u*rx)+up*(v*ry))
    for j in range(len(stations)-1):
        for k in range(n):faces.append((j*n+k,j*n+(k+1)%n,(j+1)*n+(k+1)%n,(j+1)*n+k))
    faces.extend([tuple(reversed(range(n))),tuple((len(stations)-1)*n+k for k in range(n))])
    replace_mesh(obj,vertices,faces)
    # Use a restrained subdivision to smooth the bend without changing the silhouette.
    sub=obj.modifiers.new('Smooth shaped neck','SUBSURF');sub.levels=1;sub.render_levels=1

def round_socket(obj):
    profile=[(.055,.463),(.085,.472),(.108,.49),(.122,.515),(.121,.535),(.102,.553)]
    n=48;vs=[(r*math.cos(k*2*math.pi/n),-1.025+r*math.sin(k*2*math.pi/n),z) for r,z in profile for k in range(n)]
    fs=[(j*n+k,j*n+(k+1)%n,(j+1)*n+(k+1)%n,(j+1)*n+k) for j in range(len(profile)-1) for k in range(n)]
    fs += [tuple(reversed(range(n))),tuple((len(profile)-1)*n+k for k in range(n))]
    replace_mesh(obj,vs,fs)
    obj.name=obj.name.replace('Cup mounting saddle','Rounded cup socket')
    bevel=obj.modifiers.new('Rounded socket lips','BEVEL');bevel.width=.008;bevel.segments=3

def map_geometry(obj, transform):
    root=obj
    while root.parent:root=root.parent
    local_to_level=root.matrix_world.inverted()@obj.matrix_world
    level_to_local=local_to_level.inverted()
    if obj.type=='MESH':
        for vertex in obj.data.vertices:vertex.co=level_to_local@transform(local_to_level@vertex.co)
    elif obj.type=='CURVE':
        for spline in obj.data.splines:
            for point in spline.points:
                v=level_to_local@transform(local_to_level@Vector(point.co[:3]));point.co=(*v,1)

for level in (1,2,3):
    prefix=f'L{level} | '
    root=bpy.data.objects[prefix+f'Catapult_L{level}']
    if root.get('cup_clearance_revision',0)>=1:continue
    rounded_arm(bpy.data.objects[prefix+'Throwing arm'])
    round_socket(bpy.data.objects[prefix+'Cup mounting saddle'])
    # A shorter banner sits above the full moving-arm envelope, instead of touching the axle.
    for obj in list(scene.objects):
        if not obj.name.startswith(prefix):continue
        name=obj.name[len(prefix):]
        if name.startswith(('Crimson pennant','Banner rod','Banner fastener','Pennant gold edging')):
            map_geometry(obj,lambda v:Vector((v.x*.80,v.y-.04,1.99+(v.z-1.99)*.43)))
        elif name.startswith('Pennant laurel'):
            map_geometry(obj,lambda v:Vector((v.x*.70,v.y-.04,1.85+(v.z-1.70)*.62)))
    root['cup_clearance_revision']=1

# Remove relationship-line clutter from normal viewing, without removing any rig controls.
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':area.spaces.active.overlay.show_relationship_lines=False
bpy.context.view_layer.update()
print('Refined all three arms, round cup sockets, and raised clear banners. Animation hierarchy retained.')
