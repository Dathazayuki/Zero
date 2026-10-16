"""Explicit REST authoring of the NEW Room4 demo. Never saves or rebuilds scenes."""
import json
from pathlib import Path
import sys
import urllib.request

BASE='http://localhost:8090'
ROOT='InfinityRoom4_HotelKitchen_Demo'
MAT='Assets/Game/Materials/InfinityRoom4Demo/'
MODEL='Assets/Game/Models/InfinityRoom4Demo/IR4_'
validated=set()


def request(endpoint, args):
    body=json.dumps(args).encode()
    req=urllib.request.Request(BASE+endpoint,body,{'Content-Type':'application/json'})
    with urllib.request.urlopen(req, timeout=45) as response:
        return json.load(response)


def call(skill, **args):
    if skill not in validated:
        preview=request('/skill/'+skill+'?mode=dryRun',args)
        if not preview.get('valid') or not preview.get('authorization',{}).get('allowed'):
            raise RuntimeError(json.dumps(preview))
        validated.add(skill)
    result=request('/skill/'+skill,args)
    if result.get('status')=='error':
        raise RuntimeError(json.dumps(result))
    value=result.get('result', result)
    if value.get('failCount',0):
        raise RuntimeError(json.dumps(value))
    print(skill, 'OK',value.get('successCount',''), flush=True)
    return value


def batch(skill,items):
    for start in range(0,len(items),30):
        call(skill,items=json.dumps(items[start:start+30]))


def prop(name,component,key,value):
    return dict(name=name,componentType=component,propertyName=key,value=str(value))


def cube(name,pos,size,material,parent='IR4_Architecture'):
    create=dict(name=name,primitiveType='Cube',parentName=parent,
                x=pos[0],y=pos[1],z=pos[2],
                scaleX=size[0],scaleY=size[1],scaleZ=size[2])
    assign=dict(name=name,materialPath=MAT+'M_IR4_'+material+'.mat')
    return create,assign


def materials():
    folder=Path(__file__).resolve().parents[2]/'Materials'/'InfinityRoom4Demo'
    if not folder.exists():
        call('asset_create_folder',folderPath='Assets/Game/Materials/InfinityRoom4Demo')
    colors={'Metal':(.55,.54,.50),'Dark':(.11,.105,.09),'Wood':(.30,.19,.09),
            'Ceramic':(.70,.66,.53),'Glass':(.12,.16,.19),'Glow':(1,.72,.32),
            'TileA':(.35,.24,.16),'TileB':(.40,.285,.18),'TileC':(.29,.21,.16),
            'WallA':(.59,.55,.44),'WallB':(.52,.48,.39),'Ceiling':(.34,.31,.25)}
    batch('material_create_batch',[dict(name='M_IR4_'+n,savePath=MAT,
          shaderName='Universal Render Pipeline/Lit') for n in colors])
    batch('material_set_colors_batch',[dict(path=MAT+'M_IR4_'+n+'.mat',
          r=c[0],g=c[1],b=c[2]) for n,c in colors.items()])
    call('material_set_float',path=MAT+'M_IR4_Metal.mat',propertyName='_Metallic',value=.65)
    call('material_set_float',path=MAT+'M_IR4_Metal.mat',propertyName='_Smoothness',value=.38)
    call('material_set_texture',path=MAT+'M_IR4_Metal.mat',propertyName='_BaseMap',
         texturePath='Assets/Game/Model/retro-tv/textures/Metal011_2K_Color.jpg')
    call('material_set_texture',path=MAT+'M_IR4_Wood.mat',propertyName='_BaseMap',
         texturePath='Assets/Game/Model/table/textures/wood_table_001_diff_1k.jpg')
    call('material_set_emission',path=MAT+'M_IR4_Glow.mat',r=1,g=.62,b=.22,intensity=2)


def shell():
    batch('gameobject_create_batch',[dict(name=ROOT)])
    batch('gameobject_create_batch',[
          dict(name=n,parentName=ROOT) for n in
          ['IR4_Architecture','IR4_Equipment','IR4_Props','IR4_Lighting','IR4_Markers']])
    specs=[('IR4_Floor',(0,-.14,0),(8.4,.25,10.4),'Dark'),
           ('IR4_Ceiling',(0,3.6,0),(8.4,.2,10.4),'Ceiling'),
           ('IR4_WestWall',(-4.1,1.75,0),(.2,3.5,10),'WallA'),
           ('IR4_EastWall',(4.1,1.75,0),(.2,3.5,10),'WallA'),
           ('IR4_NorthWall',(0,1.75,5.1),(8.4,3.5,.2),'WallA'),
           ('IR4_SouthLeft',(-2.45,1.75,-5.1),(3.5,3.5,.2),'WallA'),
           ('IR4_SouthRight',(2.45,1.75,-5.1),(3.5,3.5,.2),'WallA'),
           ('IR4_DoorHeader',(0,3.0,-5.1),(1.4,1,.2),'WallA'),
           ('IR4_DoorLeaf',(0,1.25,-5.15),(1.36,2.5,.055),'Dark'),
           ('IR4_WindowPane',(0,2.10,4.965),(3.9,1.65,.025),'Glass')]
    for i in range(5):
        specs.append((f'IR4_WindowMullion_{i}',(-1.95+i*.975,2.1,4.92),(.045,1.72,.055),'Dark'))
    for y in [1.25,2.1,2.95]:
        specs.append((f'IR4_WindowHorizontal_{y}',(0,y,4.91),(4,.045,.065),'Dark'))
    specs.append(('IR4_WindowSill',(0,1.23,4.8),(4.15,.065,.34),'Metal'))
    objects=[cube(*s) for s in specs]
    batch('gameobject_create_batch',[o[0] for o in objects])
    batch('material_assign_batch',[o[1] for o in objects])


def furniture():
    call('asset_refresh')
    instances=[('FloorTiles','QuarryFloorTiles',0,0,0,0),
               ('NorthTiles','WallTiles',0,0,4.987,0),
               ('WestTiles','WallTiles',-3.987,0,0,-90),
               ('EastTiles','WallTiles',3.987,0,0,90),
               ('Island','PrepIsland',0,0,-.3,0),
               ('Sink','DoubleSink',0,0,4.35,0),
               ('Range','CommercialRange',3.43,0,1.3,90),
               ('Hood','ExtractorHood',3.40,0,1.3,90),
               ('Fridge','CommercialFridge',3.42,0,-2.7,90),
               ('ShelfWest1','PotShelves',-3.65,0,-1.8,-90),
               ('ShelfWest2','PotShelves',-3.65,0,1.2,-90),
               ('ShelfEast','PotShelves',3.65,0,3.35,90),
               ('Utensils','UtensilRail',-3.72,0,-1.8,-90)]
    instances += [(f'CounterWest{i}','Cabinet',-3.50,0,z,-90)
                  for i,z in enumerate([-3.4,-2.2,-1,.2,1.4,2.6,3.8])]
    instances += [(f'CounterEast{i}','Cabinet',3.5,0,z,90)
                  for i,z in enumerate([-1.4,-.2,3.6])]
    instances += [(f'CounterNorth{i}','Cabinet',x,0,4.45,0)
                  for i,x in enumerate([-2.8,-1.6,1.6,2.8])]
    payload=[dict(prefabPath=MODEL+model+'.obj',name='IR4_'+name,
                 x=x,y=y,z=z,rotY=rot,parentName='IR4_Equipment')
             for name,model,x,y,z,rot in instances]
    call('prefab_instantiate_batch',items=payload)
    assign=[]
    for name,_,_,_,_,_ in instances:
        info=call('gameobject_get_info',name='IR4_'+name)
        for child in info.get('children',[]):
            group=child['name']
            assign.append(dict(path=child['path'],materialPath=MAT+'M_IR4_'+group+'.mat'))
    batch('material_assign_batch',assign)
    proxies=[('IR4_IslandCollision',(0,.48,-.3),(2.55,.96,1.3),'Dark'),
             ('IR4_WestCollision',(-3.5,.45,.2),(1,.9,8.4),'Dark'),
             ('IR4_EastCollision',(3.5,.45,1.3),(1,.9,5.8),'Dark'),
             ('IR4_FridgeCollision',(3.42,1.2,-2.7),(.9,2.4,1.55),'Dark'),
             ('IR4_NorthCollision',(0,.45,4.45),(7,.9,.9),'Dark')]
    batch('gameobject_create_batch',[cube(*s,parent='IR4_Equipment')[0] for s in proxies])
    for name,*_ in proxies:
        call('component_set_enabled',name=name,componentType='MeshRenderer',enabled=False)


def systems():
    batch('gameobject_create_batch',[dict(name='IR4_DemoPlayer',x=0,y=.05,z=-4.25),
          dict(name='IR4_LoopResumePoint',parentName='IR4_Markers',x=0,y=.05,z=-4.25)])
    call('camera_create',name='IR4_PreviewCamera',x=0,y=1.75,z=-4.25,addAudioListener=True)
    call('camera_set_properties',name='IR4_PreviewCamera',fieldOfView=65,nearClipPlane=.05,
         farClipPlane=50,clearFlags='Color',bgR=.025,bgG=.028,bgB=.035)
    call('gameobject_set_parent',childName='IR4_PreviewCamera',parentName='IR4_DemoPlayer')
    call('component_add',name='IR4_DemoPlayer',componentType='Zero.AnomalyPrototype.Player.FirstPersonController')
    items=[prop('IR4_DemoPlayer','CharacterController','height',1.8),
           prop('IR4_DemoPlayer','CharacterController','radius',.28),
           prop('IR4_DemoPlayer','CharacterController','center','0,.9,0')]
    batch('component_set_property_batch',items)
    lighting()


def lighting():
    for name,x,y,z,intensity,color,shadows in [
        ('Pendant',0,2.8,.5,3.0,(1,.72,.42),'Soft'),
        ('WestFill',-2.7,2.05,-.8,1.5,(1,.65,.35),'None'),
        ('HoodFill',2.7,2.05,1.3,1.8,(1,.70,.4),'None'),
        ('WindowFill',0,2.5,3.9,1.1,(.52,.66,.8),'None'),
        ('EntranceFill',0,2.8,-3.8,.7,(.7,.74,.8),'None')]:
        call('light_create',name='IR4_'+name,lightType='Point',x=x,y=y,z=z,
             r=color[0],g=color[1],b=color[2],intensity=intensity,range=8,shadows=shadows)
        call('gameobject_set_parent',childName='IR4_'+name,parentName='IR4_Lighting')
    call('light_add_reflection_probe',probeName='IR4_ReflectionProbe',x=0,y=1.6,z=0,
         sizeX=8,sizeY=3.5,sizeZ=10,resolution=128)
    call('camera_screenshot',name='IR4_PreviewCamera',width=1600,height=900,
         savePath='Assets/Game/Documentation/InfinityRoom4Demo/Room4_Preview.png')


if __name__=='__main__':
    current=call('scene_get_info')
    if current.get('sceneName')!='InfinityRoom4_HotelKitchen_Demo':
        raise RuntimeError('Open the Room4 demo scene first. No other scene may be modified.')
    stage=sys.argv[1] if len(sys.argv)>1 else 'all'
    for name,func in [('materials',materials),('shell',shell),('furniture',furniture),('systems',systems),('lighting',lighting)]:
        if stage==name or stage=='all' and name!='lighting':
            func()
