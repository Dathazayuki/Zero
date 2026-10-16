"""Explicit, repeatable layout correction; no Unity startup hook or automatic scene save."""
import sys
from pathlib import Path

sys.path.insert(0,str(Path(__file__).resolve().parent))
from author_crimson_scene import call, batch, prop, SCENE_NAME

BOOTH_CENTERS=(-3.05,-.48,2.09)
PEDESTALS=((3.4,0,-4.32),(3.43,0,-1.765),(3.43,0,.805),
           (-3.45,0,-3.70),(-3.10,.24,3.75))


def move(name,x,y,z,**extra):
    return dict(name=name,posX=x,posY=y,posZ=z,**extra)


def layout():
    items=[move('IR5V2_Chandelier',0,2.45,-.30),
           move('IR5V2_Clock',-3.945,2.75,3.93),
           move('IR5V2_Figure0',-2.17,.24,3.80),
           move('IR5V2_Figure1',2.90,.24,3.65),
           move('IR5V2_Figure2',-2.6,0,-4.25),
           move('IR5V2_Trunk0',-1.4,0,1.55),
           move('IR5V2_Trunk1',-1.20,0,2.74),
           move('IR5V2_Piano',-.85,.24,3.82,scaleZ=.75)]
    for i,z in enumerate(BOOTH_CENTERS):
        items += [move(f'IR5V2_Booth{i}',3.40,0,z),move(f'IR5V2_Table{i}',2.33,0,z),
                  move(f'IR5V2_Chair{i}',1.35,0,z-.13),
                  move(f'IR5V2_TableLamp{i}',2.33,.850,z),
                  move(f'IR5V2_Mirror{i}',3.94,.35,z+.05),
                  move(f'IR5V2_LampGlow{i}',2.33,1.20,z)]
    for i,(x,y,z) in enumerate(PEDESTALS):
        items += [move(f'IR5V2_Pedestal{i}',x,y,z),
                  move(f'IR5V2_Bust{i}',x,y+.643,z)]
    for i,z in enumerate((-4.32,-1.765,.805)):
        items += [move(f'IR5V2_Sconce{i}',3.94,2.42,z),
                  move(f'IR5V2_WallGlow{i}',3.70,2.75,z)]
    items += [move(f'IR5V2_Stool{i}',-1.91,0,z)
              for i,z in enumerate((-2.25,-.70,.85,2.30))]
    batch('gameobject_set_transform_batch',items)


def collisions():
    # Root-local proxies stay aligned when a display is moved or rotated.
    shapes=[('IR5V2_Booth'+str(i),'0,.60,0','1.93,1.20,.80') for i in range(3)]
    shapes += [(f'IR5V2_Table{i}','0,.42,0','1.12,.84,1.12') for i in range(3)]
    shapes += [(f'IR5V2_Chair{i}','0,.59,.04','.56,1.18,.57') for i in range(3)]
    shapes += [(f'IR5V2_Stool{i}','0,.44,0','.55,.88,.55') for i in range(4)]
    shapes += [(f'IR5V2_Pedestal{i}','0,.322,0','.58,.645,.58') for i in range(5)]
    shapes += [(f'IR5V2_Trunk{i}','0,.27,0','.78,.54,.54') for i in range(2)]
    shapes += [('IR5V2_Piano','0,.52,-.05','1.72,1.04,1.75')]
    batch('component_add_batch',[dict(name=n,componentType='BoxCollider') for n,_,_ in shapes])
    values=[]
    for n,center,size in shapes:
        values += [prop(n,'BoxCollider','center',center),prop(n,'BoxCollider','size',size)]
    batch('component_set_property_batch',values)
    batch('component_add_batch',[dict(name=f'IR5V2_Figure{i}',componentType='CapsuleCollider')
                                 for i in range(3)])
    values=[]
    for i in range(3):
        n=f'IR5V2_Figure{i}'
        values += [prop(n,'CapsuleCollider','center','0,.92,0'),
                   prop(n,'CapsuleCollider','height',1.84),
                   prop(n,'CapsuleCollider','radius',.22)]
    batch('component_set_property_batch',values)


def capture():
    call('camera_screenshot',name='IR5_PreviewCamera',width=1600,height=900,
         savePath='Assets/Game/Documentation/InfinityRoom5Demo/Room5_RefinedPreview.png')


if __name__=='__main__':
    if call('scene_get_info')['sceneName']!=SCENE_NAME:
        raise RuntimeError('Only the existing Crimson Cabaret demo may be corrected.')
    {'layout':layout,'collisions':collisions,'capture':capture}[sys.argv[1]]()
