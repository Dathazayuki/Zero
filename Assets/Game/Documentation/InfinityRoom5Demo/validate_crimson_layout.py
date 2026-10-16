"""Read-only solid-prop overlap regression check against live demo transforms."""
import itertools
import math
import sys
from pathlib import Path

sys.path.insert(0,str(Path(__file__).resolve().parent))
from author_crimson_scene import call, SCENE_NAME
from refine_crimson_scene import specs


def bounds(model,info):
    path=Path(__file__).resolve().parents[2]/'Models'/'InfinityRoom5Demo'/f'IR5V2_{model}.obj'
    vertices=[tuple(float(v) for v in line.split()[1:])
              for line in path.read_text().splitlines() if line.startswith('v ')]
    # OBJ source mirrors X to compensate for Unity's OBJ coordinate conversion.
    vertices=[(-x,y,z) for x,y,z in vertices]
    lo=[min(p[i] for p in vertices) for i in range(3)]
    hi=[max(p[i] for p in vertices) for i in range(3)]
    angle=math.radians(info['rotation']['y']); c,s=math.cos(angle),math.sin(angle)
    scale=info['scale']; pos=info['position']; transformed=[]
    for corner in itertools.product(*zip(lo,hi)):
        x,y,z=[corner[i]*scale[k] for i,k in enumerate('xyz')]
        transformed.append((pos['x']+x*c+z*s,pos['y']+y,pos['z']-x*s+z*c))
    return ([min(p[i] for p in transformed) for i in range(3)],
            [max(p[i] for p in transformed) for i in range(3)])


def validate():
    if call('scene_get_info')['sceneName']!=SCENE_NAME:
        raise RuntimeError('Open the Crimson Cabaret demo for read-only validation.')
    solids={}
    prefixes=('Bar','Piano','Booth','Table','Chair','Stool','Figure','Pedestal','Trunk')
    for name,model,*_ in specs():
        if name.startswith(prefixes) and 'Lamp' not in name:
            solids[name]=bounds(model,call('gameobject_get_info',name='IR5V2_'+name))
    problems=[]
    for (a,(al,ah)),(b,(bl,bh)) in itertools.combinations(solids.items(),2):
        overlap=[min(ah[i],bh[i])-max(al[i],bl[i]) for i in range(3)]
        if min(overlap)>.012:
            problems.append((a,b,[round(v,3) for v in overlap]))
    for problem in problems:
        print('CABARET_SOLID_OVERLAP:',problem)
    for name in ('Figure0','Figure1','Pedestal4','Piano'):
        lo,hi=solids[name]
        if lo[0]<-3.4 or hi[0]>3.4 or lo[2]<3.09 or hi[2]>4.51:
            problems.append((name,'outside stage support / intersects curtain'))
            print('CABARET_STAGE_SUPPORT_INVALID:',name)
    if problems:
        raise RuntimeError('Resolve solid-prop overlaps before handoff.')
    print('CABARET_LAYOUT_VALIDATION_PASS: solids='+str(len(solids)))


if __name__=='__main__':
    validate()
