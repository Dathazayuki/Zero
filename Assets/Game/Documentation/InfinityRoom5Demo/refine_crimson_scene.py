"""User-invoked UnitySkills detail pass. Never auto-runs or saves a scene on reload."""
import json
import sys
from pathlib import Path

sys.path.insert(0,str(Path(__file__).resolve().parent))
from author_crimson_scene import call, batch, prop, SCENE_NAME

ROOT='IR5V2_DetailPass'
MAT='Assets/Game/Materials/InfinityRoom5Demo/M_IR5V2_'
MODEL='Assets/Game/Models/InfinityRoom5Demo/IR5V2_'


def materials():
    colors={'Ivory':(.77,.70,.56),'Joint':(.14,.095,.054),'Plinth':(.54,.45,.31),
            'Velvet':(.23,.018,.025),'Piping':(.31,.135,.048),'Wood':(.85,.65,.40),
            'Brass':(.55,.34,.12),'Tablecloth':(.28,.020,.035),'Shade':(.85,.65,.40),
            'Black':(.025,.018,.012),'Plaster':(.74,.63,.45),'FloorA':(1,.60,.28),
            'FloorB':(.83,.52,.25),'Curtain':(.32,.010,.025),
            'Mirror':(.095,.08,.055),'Dial':(.68,.57,.37)}
    folder='Assets/Game/Materials/InfinityRoom5Demo/'
    batch('material_create_batch',[dict(name='M_IR5V2_'+n,savePath=folder,
          shaderName='Universal Render Pipeline/Lit') for n in colors])
    batch('material_set_colors_batch',[dict(path=MAT+n+'.mat',r=c[0],g=c[1],b=c[2])
                                     for n,c in colors.items()])
    for n in colors:
        smooth=.42 if n in ['Ivory','Black','FloorA','FloorB','Brass','Mirror'] else .16
        call('material_set_float',path=MAT+n+'.mat',propertyName='_Smoothness',value=smooth)
    call('material_set_float',path=MAT+'Brass.mat',propertyName='_Metallic',value=.65)
    wood='Assets/Game/Model/table/textures/wood_table_001_diff_1k.jpg'
    for n in ['Wood','FloorA','FloorB']:
        call('material_set_texture',path=MAT+n+'.mat',texturePath=wood)
    call('material_set_texture',path=MAT+'Plaster.mat',texturePath=
         'Assets/Game/Materials/EnvironmentLoop/white-wall-textures_74190-2830.jpg')
    call('material_set_emission',path=MAT+'Shade.mat',r=1,g=.61,b=.23,intensity=.32)
    call('asset_refresh')


def specs():
    result=[('Architecture','CorniceAndWainscot',0,0,0,0),
            ('Parquet','NarrowParquet',0,0,0,0),('Curtains','PleatedStageCurtains',0,0,4.63,0),
            ('Bar','OrnateBar',-3.08,0,.20,0),('Piano','StagePiano',-.85,.275,3.82,0),
            ('Chandelier','SixArmChandelier',0,2.65,-.30,0),
            ('Clock','CabaretWallClock',-3.945,2.52,2.05,-90)]
    for i,z in enumerate([-2.30,-.30,1.70]):
        result += [(f'Booth{i}','VelvetBooth',3.40,0,z,90),
                   (f'Table{i}','DrapedTable',2.33,0,z,0),
                   (f'Chair{i}','BentwoodChair',1.60,0,z-.13,70),
                   (f'TableLamp{i}','FringedTableLamp',2.33,.84,z,0),
                   (f'Mirror{i}','GildedWallMirror',3.94,.35,z+.05,90)]
    for i,z in enumerate([-2.25,-.70,.85,2.30]):
        result.append((f'Stool{i}','BrassBarStool',-2.16,0,z,0))
    for i,z in enumerate([-2.40,-.25,1.90]):
        result.append((f'BarLamp{i}','FringedTableLamp',-2.87,1.075,z,0))
    for i,(x,z,ry) in enumerate([(-2.20,2.78,25),(3.38,2.95,-35),(-3.45,-3.40,-10)]):
        result.append((f'Figure{i}','FullMannequin',x,0,z,ry))
    for i,(x,z,ry) in enumerate([(3.40,-3.55,20),(3.43,-1.3,-20),(3.43,.70,15),
                               (-2.32,1.92,-30),(-3.35,3.28,45)]):
        result += [(f'Pedestal{i}','DisplayPedestal',x,0,z,0),
                   (f'Bust{i}','TorsoMannequin',x,.20,z,ry)]
    for i,z in enumerate([-2.65,-.45,1.70]):
        result.append((f'Sconce{i}','DoubleWallSconce',3.94,1.55,z,90))
    for i,(x,z,ry) in enumerate([(-2.15,2.09,0),(-1.85,2.5,8)]):
        result.append((f'Trunk{i}','PropTrunk',x,0,z,ry))
    return result


def place():
    h=call('scene_get_hierarchy',maxDepth=2)
    roots=[n['name'] for n in h['hierarchy']]
    if ROOT in roots:
        raise RuntimeError('Detail pass exists. Edit its objects rather than duplicate it.')
    batch('gameobject_create_batch',[dict(name=ROOT)])
    items=[dict(prefabPath=MODEL+src+'.obj',name='IR5V2_'+name,
                x=x,y=y,z=z,rotY=ry,parentName=ROOT) for name,src,x,y,z,ry in specs()]
    call('prefab_instantiate_batch',items=items)
    refs=[]
    for name,*_ in specs():
        info=call('gameobject_get_info',name='IR5V2_'+name)
        for child in info.get('children',[]):
            refs.append(dict(instanceId=child['instanceId'],componentType='MeshRenderer',
                             propertyPath='m_Materials.Array.data[0]',
                             assetPath=MAT+child['name']+'.mat'))
    batch('component_set_serialized_property_batch',refs)
    hide_legacy()
    shell_materials()


def hide_legacy():
    names=['IR5_Parquet','IR5_WallPanelNorth','IR5_WallPanelEast','IR5_WallPanelWest',
           'IR5_Curtain','IR5_Chandelier','IR5_CostumeRack','IR5_Bar',
           'IR5_HeadlessTorso0','IR5_HeadlessTorso1','IR5_CostumeRackCollider']
    names += [f'IR5_Mannequin{i:02d}' for i in range(11)]
    names += [f'IR5_BarStool{i}' for i in range(4)]
    names += [f'IR5_CafeTable{i}' for i in range(3)]
    names += [f'IR5_CafeChair{i}_{j}' for i in range(3) for j in range(2)]
    batch('gameobject_set_active_batch',[dict(name=n,active=False) for n in names])


def shell_materials():
    names=['IR5_WestWall','IR5_EastWall','IR5_NorthWall','IR5_SouthWallL',
           'IR5_SouthWallR','IR5_EntryHeader','IR5_Ceiling']
    batch('material_assign_batch',[dict(name=n,materialPath=MAT+'Plaster.mat') for n in names])
    info=call('gameobject_get_info',name='IR5_BackBar')
    for child in info['children']:
        if child['name'] in ['Wood','Brass','Dark']:
            mat='Black' if child['name']=='Dark' else child['name']
            call('material_assign',instanceId=child['instanceId'],materialPath=MAT+mat+'.mat')


def lighting():
    batch('light_set_properties_batch',[
          dict(name='IR5_BarGlow',intensity=1.25,r=1,g=.66,b=.34,range=4),
          dict(name='IR5_BackBarGlow',intensity=1.25,r=1,g=.67,b=.34,range=4),
          dict(name='IR5_EntranceGlow',intensity=.35,r=1,g=.67,b=.36,range=4),
          dict(name='IR5_StageRedWash',intensity=.85,r=1,g=.055,b=.035,range=5),
          dict(name='IR5_CurtainEdge',intensity=.40,r=1,g=.025,b=.02,range=3)])
    lights=[('ChandelierGlow',0,3.10,-.30,2.3,5.0),
            ('WarmBounce',0,1.50,-1.30,2.0,7.0)]
    lights += [(f'LampGlow{i}',2.33,1.20,z,.9,2.5) for i,z in enumerate([-2.30,-.30,1.70])]
    lights += [(f'WallGlow{i}',3.7,2.02,z,.9,2.6) for i,z in enumerate([-2.65,-.45,1.70])]
    for name,x,y,z,intensity,radius in lights:
        call('light_create',name='IR5V2_'+name,lightType='Point',x=x,y=y,z=z,
             r=1,g=.68,b=.35,intensity=intensity,range=radius,
             shadows='None')
        call('gameobject_set_parent',childName='IR5V2_'+name,parentName=ROOT)
    call('component_set_property',name='IR5_ReflectionProbe',componentType='ReflectionProbe',
         propertyName='intensity',value='.25')
    call('camera_set_properties',name='IR5_PreviewCamera',fieldOfView=62)


def preview():
    call('camera_screenshot',name='IR5_PreviewCamera',width=1600,height=900,
         savePath='Assets/Game/Documentation/InfinityRoom5Demo/Room5_RefinedPreview.png')


if __name__=='__main__':
    stage=sys.argv[1]
    info=call('scene_get_info')
    if info['sceneName']!=SCENE_NAME:
        raise RuntimeError('Open the Crimson Cabaret demo scene before this manual pass.')
    {'materials':materials,'place':place,'lighting':lighting,'preview':preview}[stage]()
