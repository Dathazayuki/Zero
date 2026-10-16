"""Offline mesh authoring for the Room4 demo. Never opens or saves Unity scenes."""
from pathlib import Path
import math
import random

OUT = Path(__file__).resolve().parents[2] / 'Models' / 'InfinityRoom4Demo'
ASSET_PREFIX = 'IR4_'


class Mesh:
    def __init__(self):
        self.parts = {}

    def face(self, material, points):
        self.parts.setdefault(material, []).append(points)

    def box(self, pos, size, mat='Metal'):
        x, y, z = pos
        a, b, c = (v / 2 for v in size)
        p = [(x+i*a, y+j*b, z+k*c) for i,j,k in
             [(-1,-1,-1),(1,-1,-1),(1,1,-1),(-1,1,-1),
              (-1,-1,1),(1,-1,1),(1,1,1),(-1,1,1)]]
        for ids in [(0,3,2,1),(4,5,6,7),(0,4,7,3),
                    (1,2,6,5),(3,7,6,2),(0,1,5,4)]:
            self.face(mat, [p[i] for i in ids])

    def tube(self, points, radius, mat='Metal', sides=12):
        rings = []
        for i,p in enumerate(points):
            before, after = points[max(i-1,0)], points[min(i+1,len(points)-1)]
            d = [after[j]-before[j] for j in range(3)]
            length = math.sqrt(sum(v*v for v in d))
            d = [v/length for v in d]
            seed = (1,0,0) if abs(d[0]) < .8 else (0,1,0)
            u = cross(d, seed)
            norm = math.sqrt(sum(v*v for v in u))
            u = [v/norm for v in u]
            v = cross(d,u)
            rings.append([tuple(p[j]+radius*(u[j]*math.cos(k*math.tau/sides)+
                          v[j]*math.sin(k*math.tau/sides)) for j in range(3))
                          for k in range(sides)])
        self.face(mat, rings[0][::-1])
        self.face(mat, rings[-1])
        for a,b in zip(rings,rings[1:]):
            for i in range(sides):
                n = (i+1)%sides
                self.face(mat,[a[i],a[n],b[n],b[i]])

    def lathe(self, pos, profile, mat='Metal', sides=24):
        x,y,z = pos
        rings = [[(x+r*math.cos(i*math.tau/sides), y+h,
                   z+r*math.sin(i*math.tau/sides)) for i in range(sides)]
                 for r,h in profile]
        for a,b in zip(rings,rings[1:]):
            for i in range(sides):
                n=(i+1)%sides
                self.face(mat,[a[i],b[i],b[n],a[n]])

    def save(self, name):
        lines = ['# Authored Room4 kitchen mesh. Metres; grouped by material.']
        index = 1
        for mat,faces in self.parts.items():
            lines += [f'o {mat}', f'g {mat}']
            for face in faces:
                for x,y,z in face:
                    lines.append(f'v {-x:.5f} {y:.5f} {z:.5f}')
                for i,(x,y,z) in enumerate(face):
                    if mat.startswith('Wall'):
                        u,v=x*.35,y*.35
                    elif mat.startswith('Tile'):
                        u,v=x*.45,z*.45
                    else:
                        u,v=float(i in (1,2)),float(i>=2)
                    lines.append(f'vt {u:.5f} {v:.5f}')
                ids = [f'{index+i}/{index+i}' for i in range(len(face)-1,-1,-1)]
                lines.append('f '+' '.join(ids))
                index += len(face)
        (OUT/f'{ASSET_PREFIX}{name}.obj').write_text('\n'.join(lines)+'\n', encoding='utf-8')
        print(name, 'faces=',sum(len(f) for f in self.parts.values()))


def cross(a,b):
    return (a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0])


def handle(m,x,y,z,width=.23):
    m.tube([(x-width/2,y,z+.035),(x-width/2,y,z),
            (x+width/2,y,z),(x+width/2,y,z+.035)],.012,'Dark')


def cabinet():
    m=Mesh()
    m.box((0,.45,0),(1.15,.75,.68),'Dark')
    m.box((0,.9,0),(1.2,.065,.78))
    m.box((0,.95,.35),(1.2,.10,.03))
    for x in [-.28,.28]:
        m.box((x,.40,-.353),(.54,.53,.025))
        m.box((x,.76,-.353),(.54,.13,.025))
        handle(m,x,.72,-.4)
        handle(m,x,.58,-.4)
        for y in [.25,.53]:
            m.box((x-.22,y,-.374),(.018,.045,.015),'Dark')
    for x in [-.49,.49]:
        for z in [-.25,.25]:
            m.tube([(x,.02,z),(x,.12,z)],.026,'Dark')
    m.save('Cabinet')


def island():
    m=Mesh()
    m.box((0,.9,0),(2.55,.07,1.30))
    m.box((0,.20,0),(2.35,.04,1.1))
    for x in [-1.08,1.08]:
        for z in [-.47,.47]:
            m.tube([(x,.02,z),(x,.88,z)],.031)
            m.lathe((x,0,z),[(0,0),(.043,0),(.043,.035),(0,.035)],'Dark')
    for x in [-.55,.55]:
        tray(m,x,.235,.05,.65,.40,.11)
        tray(m,x,.36,.05,.62,.38,.07)
    pot(m,0,.22,.20,.24,.34)
    m.box((.62,.96,.1),(.65,.032,.4),'Wood')
    m.box((.82,.99,.1),(.28,.014,.032),'Dark')
    m.box((.54,.99,.1),(.26,.009,.055))
    pot(m,-.65,.94,.12,.19,.15)
    tray(m,-.7,.945,-.33,.65,.28,.07)
    m.save('PrepIsland')


def tray(m,x,y,z,w=.5,d=.32,h=.06):
    m.box((x,y,z),(w,.012,d))
    for sx in [-1,1]:
        m.box((x+sx*w/2,y+h/2,z),(.012,h,d))
    for sz in [-1,1]:
        m.box((x,y+h/2,z+sz*d/2),(w,h,.012))
    for side in [-1,1]:
        m.box((x+side*w/2,y+h,z),(.025,.012,d+.02))
        m.box((x,y+h,z+side*d/2),(w+.02,.012,.025))


def pot(m,x,y,z,r,h):
    m.lathe((x,y,z),[(0,0),(r*.85,0),(r,.03),(r,h),
              (r-.012,h),(r-.012,.025),(0,.025)])
    for side in [-1,1]:
        m.tube([(x+side*r,y+h*.7,z-.055),(x+side*(r+.07),y+h*.7,z-.055),
                (x+side*(r+.07),y+h*.7,z+.055),(x+side*r,y+h*.7,z+.055)],.012)
    m.lathe((x,y+h+.01,z),[(0,.02),(r*.75,.015),(r+.008,0),(r, -.01)])
    m.lathe((x,y+h+.03,z),[(0,0),(.038,0),(.038,.028),(0,.028)],'Dark')


def range_and_hood():
    m=Mesh()
    m.box((0,.47,0),(2,.78,.90))
    m.box((0,.89,0),(2.06,.07,.94),'Dark')
    for x in [-.50,.50]:
        m.box((x,.42,-.469),(.91,.55,.035),'Dark')
        m.box((x,.42,-.493),(.74,.39,.016),'Glass')
        handle(m,x,.66,-.53,.62)
        for a in [-.25,.25]:
            m.lathe((x+a,.815,-.49),[(0,0),(.04,0),(.04,.022),(0,.022)],'Dark')
    for x in [-.65,0,.65]:
        for z in [-.22,.22]:
            m.lathe((x,.936,z),[(.09,0),(.13,0),(.13,.025),(.09,.025)],'Dark')
            for offset in [-.11,.11]:
                m.box((x+offset,.96,z),(.018,.018,.28),'Dark')
                m.box((x,.96,z+offset),(.28,.018,.018),'Dark')
    for x,z,r,h in [(-.65,.22,.20,.31),(0,-.22,.15,.20),(.65,.22,.25,.36)]:
        pot(m,x,.965,z,r,h)
    m.save('CommercialRange')
    m=Mesh()
    m.box((0,2.55,.16),(2.45,.42,1.18))
    m.box((0,2.32,.1),(2.38,.035,1.10),'Dark')
    for i in range(28):
        m.box((-.99+i*.074,2.30,.12),(.028,.035,.8))
    m.box((0,3.02,.44),(.65,.65,.5))
    for x in [-1.1,1.1]:
        for y in [2.42,2.65]:
            m.box((x,y,-.439),(.013,.013,.008),'Dark')
    m.box((0,2.30,-.22),(1.7,.018,.055),'Glow')
    m.save('ExtractorHood')


def sink():
    m=Mesh()
    for x in [-1.04,1.04]:
        for z in [-.35,.35]:
            m.tube([(x,.04,z),(x,.84,z)],.03)
    m.box((0,.3,0),(2.15,.035,.7))
    m.box((0,.9,.30),(2.4,.06,.20))
    m.box((0,.9,-.35),(2.4,.06,.10))
    for x in [-1.1,0,1.1]:
        m.box((x,.9,0),(.17,.06,.62))
    for x in [-.54,.54]:
        m.box((x,.67,0),(.94,.03,.52))
        for z in [-.265,.265]:
            m.box((x,.785,z),(.94,.22,.02))
        for dx in [-.47,.47]:
            m.box((x+dx,.785,0),(.02,.22,.52))
        m.lathe((x,.692,0),[(0,0),(.034,0),(.034,.003),(0,.003)],'Dark')
    points=[(0,.94,.30),(0,1.3,.30)]
    points += [(0,1.3+.2*math.sin(i*math.pi/12),.1+.2*math.cos(i*math.pi/12)) for i in range(13)]
    m.tube(points,.018)
    for x in [-.18,.18]:
        m.tube([(x,.94,.29),(x,1.01,.29)],.021)
        m.box((x,1.02,.29),(.09,.01,.025),'Dark')
    m.box((.98,.98,.10),(.13,.15,.09),'Ceramic')
    m.save('DoubleSink')


def fridge():
    m=Mesh()
    m.box((0,1.25,0),(1.55,2.40,.90),'Dark')
    for x in [-.38,.38]:
        m.box((x,1.38,-.47),(.735,2.05,.045))
        hx=x+(.19 if x<0 else -.19)
        m.tube([(hx,1.15,-.505),(hx,1.15,-.56),
                (hx,1.63,-.56),(hx,1.63,-.505)],.018)
    for x in [i*.065-.62 for i in range(20)]:
        m.box((x,.23,-.475),(.015,.21,.035))
    m.box((.4,2.19,-.502),(.20,.055,.01),'Dark')
    m.box((.4,2.19,-.51),(.16,.028,.008),'Glow')
    m.save('CommercialFridge')


def shelving():
    m=Mesh()
    for y in [1.35,1.95,2.55]:
        m.box((0,y,0),(2.50,.045,.40))
        for x in [-1.1,1.1]:
            m.tube([(x,y,.18),(x,y-.22,.18),(x,y,-.15)],.015,'Dark')
        for i in range(6):
            x=-1.04+i*.42
            pot(m,x,y+.035,0,.13+(.02 if i%2 else 0),.14+.055*(i%3))
    m.save('PotShelves')


def utensils():
    m=Mesh()
    m.tube([(-1.15,1.76,0),(1.15,1.76,0)],.016)
    for i in range(10):
        x=-1.05+i*.23
        m.tube([(x,1.78,0),(x,1.79,-.04),(x,1.65,-.07)],.008)
        m.tube([(x,1.63,-.07),(x,1.26,-.07)],.012,'Dark')
        m.lathe((x,1.21,-.07),[(0,0),(.055,.012),(.065,.04),(0,.045)])
    m.save('UtensilRail')


def tiles():
    rng=random.Random(4)
    m=Mesh()
    for x in range(20):
        for z in range(25):
            mat=rng.choice(['TileA','TileA','TileB','TileC'])
            m.box((-3.8+x*.4,-.013,-4.8+z*.4),(.392,.025,.392),mat)
    m.save('QuarryFloorTiles')
    m=Mesh()
    for x in range(32):
        for y in range(14):
            m.box((-3.875+x*.25,.125+y*.25,0),(.244,.244,.014),rng.choice(['WallA','WallA','WallB']))
    m.save('WallTiles')


if __name__=='__main__':
    OUT.mkdir(parents=True,exist_ok=True)
    cabinet(); island(); range_and_hood(); sink(); fridge(); shelving(); utensils(); tiles()
