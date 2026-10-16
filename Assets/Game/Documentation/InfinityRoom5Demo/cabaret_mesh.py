"""Manual offline OBJ authoring with explicit normals; no Unity lifecycle hooks."""
from collections import defaultdict
from pathlib import Path
import math

OUT = Path(__file__).resolve().parents[2] / 'Models' / 'InfinityRoom5Demo'


def cross(a, b):
    return (a[1]*b[2]-a[2]*b[1], a[2]*b[0]-a[0]*b[2], a[0]*b[1]-a[1]*b[0])


def unit(v):
    length = math.sqrt(sum(x*x for x in v))
    return tuple(x / length for x in v) if length > 1e-10 else (0, 1, 0)


class Mesh:
    def __init__(self):
        self.parts = defaultdict(list)

    def face(self, mat, points, smooth=False):
        clean = []
        for point in points:
            p = tuple(round(v, 6) for v in point)
            if p not in clean:
                clean.append(p)
        if len(clean) >= 3:
            self.parts[mat].append((clean, smooth))

    def box(self, p, size, mat='Wood'):
        x, y, z = p
        a, b, c = (s/2 for s in size)
        points = [(x+i*a,y+j*b,z+k*c) for i,j,k in
                  [(-1,-1,-1),(1,-1,-1),(1,1,-1),(-1,1,-1),
                   (-1,-1,1),(1,-1,1),(1,1,1),(-1,1,1)]]
        for ids in [(0,3,2,1),(4,5,6,7),(0,4,7,3),(1,2,6,5),(3,7,6,2),(0,1,5,4)]:
            self.face(mat, [points[i] for i in ids])

    def loft(self, rings, mat, smooth=True):
        for a, b in zip(rings, rings[1:]):
            for i in range(len(a)):
                n = (i+1) % len(a)
                self.face(mat, [a[i],b[i],b[n],a[n]], smooth)
        self.face(mat, rings[0][::-1])
        self.face(mat, rings[-1])

    def body(self, profile, mat='Ivory', sides=40, sculpt=None):
        rings = []
        for x,y,z,rx,rz in profile:
            ring = []
            for i in range(sides):
                t = i*math.tau/sides
                p = (x+rx*math.cos(t), y, z+rz*math.sin(t))
                ring.append(sculpt(p) if sculpt else p)
            rings.append(ring)
        self.loft(rings, mat)

    def lathe(self, p, profile, mat='Brass', sides=36):
        x,y,z = p
        self.body([(x,y+h,z,r,r) for r,h in profile], mat, sides)

    def ellipsoid(self, p, radii, mat='Ivory', sides=32, rows=18):
        x,y,z = p
        rx,ry,rz = radii
        profile = []
        for i in range(rows+1):
            phi = -math.pi/2+math.pi*i/rows
            profile.append((x,y+ry*math.sin(phi),z,
                            rx*math.cos(phi),rz*math.cos(phi)))
        self.body(profile,mat,sides)

    def tube(self, points, radius, mat='Brass', sides=12):
        rings = []
        for i,p in enumerate(points):
            a,b = points[max(0,i-1)],points[min(len(points)-1,i+1)]
            d = unit(tuple(b[j]-a[j] for j in range(3)))
            seed = (1,0,0) if abs(d[0]) < .8 else (0,1,0)
            u = unit(cross(d,seed)); v = cross(d,u)
            rings.append([tuple(p[j]+radius*(u[j]*math.cos(k*math.tau/sides)+
                          v[j]*math.sin(k*math.tau/sides)) for j in range(3))
                          for k in range(sides)])
        self.face(mat,rings[0][::-1]); self.face(mat,rings[-1])
        for a,b in zip(rings,rings[1:]):
            for i in range(sides):
                n = (i+1)%sides
                self.face(mat,[a[i],a[n],b[n],b[i]],True)

    def ring(self, p, radii, radius=.008, mat='Brass', plane='xz', count=64):
        points=[]
        for i in range(count+1):
            t=i*math.tau/count
            a,b=radii[0]*math.cos(t),radii[1]*math.sin(t)
            points.append((p[0]+a,p[1]+(b if plane=='xy' else 0),
                           p[2]+(b if plane=='xz' else 0)))
        self.tube(points,radius,mat)

    def save(self, name):
        lines=['# Locally authored Crimson Cabaret detail mesh. Metres; explicit normals.']
        index=1
        for mat,faces in self.parts.items():
            normals=[]; accum=defaultdict(lambda:[0,0,0])
            for points,smooth in faces:
                a,b,c=points[:3]
                n=unit(cross(tuple(b[j]-a[j] for j in range(3)),
                             tuple(c[j]-a[j] for j in range(3))))
                normals.append(n)
                if smooth:
                    for p in points:
                        for j in range(3): accum[p][j]+=n[j]
            lines.extend([f'o {mat}',f'g {mat}'])
            for (points,smooth),normal in zip(faces,normals):
                axis=max(range(3),key=lambda k:abs(normal[k]))
                axes=[k for k in range(3) if k!=axis]
                for p in points:
                    n=unit(accum[p]) if smooth else normal
                    lines.extend([f'v {-p[0]:.6f} {p[1]:.6f} {p[2]:.6f}',
                                  f'vt {p[axes[0]]*2:.6f} {p[axes[1]]*2:.6f}',
                                  f'vn {-n[0]:.6f} {n[1]:.6f} {n[2]:.6f}'])
                ids=[f'{index+i}/{index+i}/{index+i}' for i in range(len(points)-1,-1,-1)]
                lines.append('f '+' '.join(ids)); index+=len(points)
        OUT.mkdir(parents=True,exist_ok=True)
        (OUT/f'IR5V2_{name}.obj').write_text('\n'.join(lines)+'\n',encoding='utf-8')
        print(name,'faces',sum(len(f) for f in self.parts.values()),flush=True)
