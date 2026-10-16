"""Explicit, offline detail meshes and REST placement for the Room4 demo only."""
import math
import sys
from author_kitchen_meshes import Mesh, pot, tray
from author_kitchen_scene import call, batch, MAT, MODEL, ROOT, prop


def plate_stack(m,x,y,z,count=8):
    for i in range(count):
        m.lathe((x,y+i*.019,z),[(0,0),(.11,0),(.145,.015),
                  (.155,.027),(.15,.030),(.105,.018),(0,.018)],'Ceramic',32)


def fixtures():
    m=Mesh()
    m.tube([(0,2.96,0),(0,3.5,0)],.014,'Dark')
    m.lathe((0,2.9,0),[(.10,.10),(.16,.06),(.32,-.12),
              (.32,-.145),(.31,-.15),(.15,.045),(.09,.09)],'Dark',32)
    m.lathe((0,2.86,0),[(0,0),(.10,0),(.14,-.04),(.08,-.08),(0,-.09)],'Glow',24)
    for z in [-.28,.28]:
        m.tube([(-.27,2.75,z),(.27,2.75,z)],.006)
    for i in range(6):
        angle=i*math.tau/6
        x,z=math.cos(angle),math.sin(angle)
        m.tube([(x*.24,2.76,z*.24),(x*.18,2.64,z*.18),
                (x*.07,2.60,z*.07)],.007)
    m.save('PendantFixture')
    m=Mesh()
    m.box((0,3.49,0),(.8,.035,.55))
    for i in range(14):
        m.box((-.34+i*.052,3.465,0),(.026,.022,.46),'Dark')
    m.save('CeilingVent')
    m=Mesh()
    for y in [3.24,3.38]:
        m.tube([(-3.80,y,-4.9),(-3.80,y,4.60),(3.65,y,4.60)],.029)
        for z in [-4,-2,0,2,4]:
            m.box((-3.80,y,z),(.085,.08,.022),'Dark')
    m.save('ServicePipes')


def cart():
    m=Mesh()
    for y in [.20,.58,.96]:
        tray(m,0,y,0,.75,.50,.035)
    for x in [-.35,.35]:
        for z in [-.225,.225]:
            m.tube([(x,.10,z),(x,1.0,z)],.020)
            m.box((x,.08,z),(.060,.13,.095),'Dark')
            m.tube([(x-.035,.073,z),(x+.035,.073,z)],.055,'Dark',16)
    m.tube([(-.35,.93,.25),(-.35,1.1,.25),(.35,1.1,.25),(.35,.93,.25)],.020)
    plate_stack(m,-.17,.25,0,10)
    plate_stack(m,.17,.25,0,6)
    for y in [.62,.71]:
        tray(m,0,y,0,.63,.40,.065)
    pot(m,-.14,1.02,.02,.14,.15)
    m.save('ServiceCart')


def clutter():
    m=Mesh()
    plate_stack(m,-.32,.94,.04,9)
    plate_stack(m,0,.94,.04,5)
    m.lathe((.32,.94,0),[(0,0),(.09,0),(.1,.18),(.095,.19),
               (.087,.18),(.08,.02),(0,.02)],'Ceramic')
    for i in range(7):
        x=.32+math.cos(i)*.05
        z=math.sin(i)*.05
        m.tube([(x,1.05,z),(x+.018,1.36,z)],.009,'Wood')
        m.lathe((x+.018,1.36,z),[(0,0),(.022,.005),(.031,.02),(0,.05)],'Wood',12)
    m.save('PlateStation')
    m=Mesh()
    for x in [-.25,0,.25]:
        m.lathe((x,.94,0),[(0,0),(.085,0),(.09,.025),(.09,.22),
                   (.08,.25),(0,.25)],'Glass')
        m.lathe((x,1.19,0),[(0,0),(.09,0),(.09,.025),(0,.025)],'Dark')
        m.box((x,1.055,-.09),(.11,.08,.005),'Ceramic')
    # Folded towels have several thin layers and visible creases, not one cube.
    for i in range(4):
        m.box((.54,.95+i*.018,0),(.28,.017,.24),'Ceramic')
    m.save('PantryJars')
    m=Mesh()
    m.box((0,.953,0),(.7,.025,.40),'Wood')
    for x in [-.13,.04,.20]:
        m.lathe((x,.985,0),[(0,0),(.065,0),(.075,.04),(.05,.095),(0,.11)],'Ceramic',16)
    m.box((-.23,.98,.13),(.18,.01,.07))
    m.box((-.37,.98,.13),(.12,.014,.032),'Dark')
    tray(m,.47,.94,0,.23,.35,.045)
    # Cloth folds drape over the table edge; one connected wavy surface.
    for i in range(12):
        xa=.65+i*.025; xb=xa+.025
        ya=.955+math.sin(i*1.8)*.006
        yb=.955+math.sin((i+1)*1.8)*.006
        m.face('Ceramic',[(xa,ya,.15),(xb,yb,.15),
               (xb,yb,-.34),(xa,ya,-.34)])
        m.face('Ceramic',[(xa,ya,-.34),(xb,yb,-.34),
               (xb,.62,-.38),(xa,.62,-.38)])
    m.save('ChoppingStation')


def place():
    current=call('scene_get_info')
    if current.get('sceneName')!='InfinityRoom4_HotelKitchen_Demo':
        raise RuntimeError('Only the new Room4 demo may be edited.')
    call('asset_refresh')
    instances=[('PendantFixture',0,0,.3,0),('CeilingVent',-1.8,0,2.1,0),
               ('CeilingVent',1.8,0,-2.8,0),('ServicePipes',0,0,0,0),
               ('ServiceCart',-2.05,0,2.9,8),('PlateStation',-3.50,0,-1.05,-90),
               ('PlateStation',2.8,0,4.45,0),('PantryJars',-3.50,0,2.6,-90),
               ('ChoppingStation',.30,0,-.60,0),('PantryJars',-1.6,0,4.45,0)]
    payload=[dict(prefabPath=MODEL+model+'.obj',name=f'IR4_Detail_{i}_{model}',
                x=x,y=y,z=z,rotY=rot,parentName='IR4_Props')
             for i,(model,x,y,z,rot) in enumerate(instances)]
    call('prefab_instantiate_batch',items=payload)
    assignments=[]
    for item in payload:
        info=call('gameobject_get_info',name=item['name'])
        assignments += [dict(path=c['path'],materialPath=MAT+'M_IR4_'+c['name']+'.mat')
                        for c in info.get('children',[])]
    batch('material_assign_batch',assignments)
    batch('component_set_property_batch',[
          prop('IR4_DemoPlayer','CharacterController','height',1.8),
          prop('IR4_DemoPlayer','CharacterController','radius',.28),
          prop('IR4_DemoPlayer','CharacterController','center','0,.9,0')])
    call('material_set_float',path=MAT+'M_IR4_Metal.mat',propertyName='_Metallic',value=.38)
    call('material_set_float',path=MAT+'M_IR4_TileA.mat',propertyName='_Smoothness',value=.32)
    call('material_set_float',path=MAT+'M_IR4_TileB.mat',propertyName='_Smoothness',value=.32)
    call('material_set_float',path=MAT+'M_IR4_TileC.mat',propertyName='_Smoothness',value=.28)
    call('camera_screenshot',name='IR4_PreviewCamera',width=1600,height=900,
         savePath='Assets/Game/Documentation/InfinityRoom4Demo/Room4_Preview.png')


def tune():
    if call('scene_get_info').get('sceneName')!='InfinityRoom4_HotelKitchen_Demo':
        raise RuntimeError('Open the Room4 demo first.')
    grunge='Assets/Game/Materials/EnvironmentLoop/' + (
        'dark-yellow-concrete-scary-for-background-yellow-wall-halloween-background-concept-horror-cement-texture-free-photo.jpg')
    for name in ['WallA','WallB','Ceiling','TileA','TileB','TileC']:
        call('material_set_texture',path=MAT+'M_IR4_'+name+'.mat',
             propertyName='_BaseMap',texturePath=grunge)
    batch('material_set_colors_batch',[
          dict(path=MAT+'M_IR4_'+n+'.mat',r=c[0],g=c[1],b=c[2])
          for n,c in [('WallA',(.92,.86,.74)),('WallB',(.86,.80,.69)),
                      ('Ceiling',(.6,.52,.43)),('TileA',(.75,.44,.26)),
                      ('TileB',(.82,.51,.31)),('TileC',(.68,.38,.22))]])
    batch('light_set_properties_batch',[
          dict(name='IR4_Pendant',intensity=5,r=1,g=.65,b=.30),
          dict(name='IR4_WestFill',intensity=2.5,r=1,g=.58,b=.24),
          dict(name='IR4_HoodFill',intensity=3,r=1,g=.65,b=.32),
          dict(name='IR4_WindowFill',intensity=.7),
          dict(name='IR4_EntranceFill',intensity=.25)])
    batch('component_set_property_batch',[
          prop('IR4_ReflectionProbe','ReflectionProbe','mode','Realtime'),
          prop('IR4_ReflectionProbe','ReflectionProbe','refreshMode','OnAwake'),
          prop('IR4_ReflectionProbe','ReflectionProbe','timeSlicingMode','IndividualFaces'),
          prop('IR4_ReflectionProbe','ReflectionProbe','boxProjection','True')])
    call('camera_screenshot',name='IR4_PreviewCamera',width=1600,height=900,
         savePath='Assets/Game/Documentation/InfinityRoom4Demo/Room4_Preview.png')


if __name__=='__main__':
    if len(sys.argv)>1 and sys.argv[1]=='tune':
        tune()
    else:
        fixtures(); cart(); clutter(); place(); tune()
