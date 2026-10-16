"""Cabaret furniture details, explicitly generated offline."""
from cabaret_mesh import Mesh
import math


def cushion(mesh,p,radii,mat='Velvet'):
    def shape(point):
        return tuple(p[i]+radii[i]*math.copysign(
            abs((point[i]-p[i])/radii[i])**.28,point[i]-p[i]) for i in range(3))
    rings=[]
    for j in range(25):
        phi=-math.pi/2+j*math.pi/24
        rings.append([shape((p[0]+radii[0]*math.cos(phi)*math.cos(t*math.tau/48),
                             p[1]+radii[1]*math.sin(phi),
                             p[2]+radii[2]*math.cos(phi)*math.sin(t*math.tau/48)))
                      for t in range(48)])
    mesh.loft(rings,mat)


def chair():
    m=Mesh()
    m.ellipsoid((0,.48,0),(.275,.07,.265),'Velvet')
    m.ring((0,.48,0),(.276,.266),.006,'Piping')
    m.ellipsoid((0,.89,.23),(.235,.30,.052),'Velvet')
    m.ring((0,.89,.23),(.246,.314),.023,'Wood',plane='xy')
    m.ring((0,.89,.167),(.221,.286),.005,'Piping',plane='xy')
    for x in [-.19,.19]:
        for z in [-.18,.18]:
            m.tube([(x*1.18,.025,z*1.2),(x*1.08,.20,z*1.04),
                    (x,.45,z),(x,.52,z)],.019,'Wood',16)
        m.tube([(x,.47,.18),(x,.68,.23),(x,.87,.25),(x*.9,1.05,.24)],.021,'Wood',16)
        m.tube([(x,.27,-.19),(x,.27,.19)],.012,'Wood')
    for x,y in [(-.09,.90),(.09,.90),(0,1.015),(0,.785)]:
        m.ellipsoid((x,y,.174),(.011,.011,.007),'Brass',16,8)
    m.save('BentwoodChair')


def booth():
    m=Mesh()
    m.box((0,.21,0),(1.72,.30,.65),'Wood')
    m.box((0,.67,.29),(1.74,.90,.14),'Velvet')
    for x in [-.43,.43]:
        cushion(m,(x,.445,-.04),(.415,.080,.32))
        m.ring((x,.448,-.04),(.42,.332),.007,'Piping')
    for i in range(5):
        x=-.68+i*.34
        cushion(m,(x,.82,.20),(.169,.31,.080))
        m.ellipsoid((x,.82,.112),(.008,.008,.004),'Velvet',12,8)
    for x in [-.87,.87]:
        cushion(m,(x,.64,-.01),(.095,.16,.35))
        m.tube([(x,.81,-.3),(x,.81,.28)],.009,'Brass')
    m.box((0,.065,0),(1.62,.04,.58),'Brass')
    m.save('VelvetBooth')


def stool():
    m=Mesh()
    m.lathe((0,.77,0),[(0,0),(.26,0),(.27,.025),(.26,.09),(.22,.12),(0,.12)],'Velvet')
    m.ring((0,.81,0),(.267,.267),.007,'Piping')
    m.ring((0,.77,0),(.267,.267),.014,'Wood')
    for i in range(4):
        t=i*math.pi/2+math.pi/4; x,z=math.cos(t),math.sin(t)
        m.tube([(x*.27,.03,z*.27),(x*.23,.35,z*.23),
                (x*.16,.69,z*.16),(x*.19,.77,z*.19)],.023,'Wood',16)
        for y in [.10,.67]: m.ellipsoid((x*.22,y,z*.22),(.028,.025,.028),'Brass',16,8)
    m.ring((0,.28,0),(.25,.25),.017,'Brass')
    m.save('BrassBarStool')


def table():
    m=Mesh()
    m.lathe((0,.03,0),[(0,0),(.29,0),(.30,.04),(.20,.08),(.055,.11),(.04,.73)],'Wood')
    m.lathe((0,.75,0),[(0,0),(.51,0),(.53,.04),(.50,.08),(0,.08)],'Wood')
    rings=[]
    for r,y in [(0,.848),(.30,.848),(.55,.843),(.565,.77),(.565,.55),(.565,.16)]:
        ring=[]
        for i in range(96):
            t=i*math.tau/96
            rr=r+(.018*math.cos(t*24)*(1 if y<.8 else 0))
            ring.append((rr*math.cos(t),y-.025*math.sin(t*5)**2 if y<.4 else y,rr*math.sin(t)))
        rings.append(ring)
    # Cloth runs down from its center: reverse loft order for exterior normals.
    m.loft(rings[::-1],'Tablecloth')
    m.save('DrapedTable')


def lamp_mesh(m,p=(0,0,0)):
    x,y,z=p
    m.lathe((x,y,z),[(0,0),(.11,0),(.115,.015),(.095,.033),(.04,.055),
                    (.03,.13),(.056,.19),(.052,.25),(.021,.29),(.013,.36)],'Brass')
    rings=[]
    for radius,h in [(.155,.31),(.153,.33),(.078,.56),(.074,.58)]:
        rings.append([(x+(radius+.003*math.cos(i*math.pi))*math.cos(i*math.tau/64),
                      y+h,z+(radius+.003*math.cos(i*math.pi))*math.sin(i*math.tau/64)) for i in range(64)])
    m.loft(rings,'Shade')
    m.ring((x,y+.315,z),(.155,.155),.006,'Brass')
    m.ring((x,y+.58,z),(.075,.075),.004,'Brass')
    for i in range(24):
        t=i*math.tau/24
        m.tube([(x+.153*math.cos(t),y+.31,z+.153*math.sin(t)),
                (x+.153*math.cos(t),y+.275,z+.153*math.sin(t))],.0025,'Piping',6)


def lamp():
    m=Mesh(); lamp_mesh(m); m.save('FringedTableLamp')


def chandelier():
    m=Mesh()
    m.lathe((0,.20,0),[(0,0),(.08,0),(.12,.07),(.09,.16),(.04,.25),
                      (.08,.32),(.065,.40),(.025,.48),(.017,.78)],'Brass')
    m.ring((0,.47,0),(.17,.17),.013,'Brass')
    for i in range(6):
        t=i*math.tau/6; a,b=math.cos(t),math.sin(t)
        curve=[(a*r,y,b*r) for r,y in [(.06,.35),(.20,.32),(.36,.21),(.52,.18),(.63,.25),(.64,.39)]]
        m.tube(curve,.013,'Brass',12)
        lamp_mesh(m,(a*.64,.23,b*.64))
        for h in [.1,.18]: m.ellipsoid((a*.40,h,b*.40),(.018,.033,.018),'Brass',16,8)
    m.save('SixArmChandelier')


def trunk():
    m=Mesh(); m.box((0,.27,0),(.75,.52,.48),'Wood')
    for x in [-.31,.31]: m.box((x,.27,0),(.025,.54,.5),'Brass')
    for y in [.02,.50]: m.box((0,y,-.246),(.75,.025,.018),'Brass')
    for x in [-.30,.30]:
        for y in [.065,.455]: m.ellipsoid((x,y,-.261),(.013,.013,.006),'Brass',12,8)
    m.box((0,.29,-.257),(.05,.07,.025),'Brass')
    m.tube([(-.08,.38,-.27),(-.08,.43,-.30),(.08,.43,-.30),(.08,.38,-.27)],.009,'Brass')
    m.save('PropTrunk')


def piano():
    m=Mesh(); m.box((0,.84,0),(1.7,.28,.75),'Black')
    m.lathe((0,.97,.31),[(0,0),(.52,0),(.55,.04),(.48,.06),(0,.06)],'Black',64)
    m.box((0,.72,-.42),(1.72,.075,.26),'Black')
    for i in range(36):
        x=-.79+i*.044
        m.box((x,.768,-.46),(.040,.013,.16),'Ivory')
        if i%7 not in [2,6]: m.box((x+.019,.781,-.421),(.017,.016,.082),'Black')
    for x,z in [(-.70,-.21),(.70,-.21),(0,.67)]:
        m.lathe((x,0,z),[(.045,0),(.03,.05),(.03,.67),(.05,.74)],'Black',24)
    for x in [-.13,0,.13]: m.box((x,.08,-.30),(.035,.015,.13),'Brass')
    m.box((0,.58,-.78),(.58,.07,.25),'Black')
    for x in [-.22,.22]: m.box((x,.29,-.78),(.03,.58,.17),'Black')
    m.save('StagePiano')


def bar():
    m=Mesh(); m.box((0,.49,0),(1.02,.92,6.0),'Wood')
    m.box((0,1.015,0),(1.2,.12,6.20),'Black')
    for y in [.97,1.075]: m.box((.612,y,0),(.014,.018,6.18),'Brass')
    for z in [-2.6,-1.55,-.5,.55,1.6,2.65]:
        for dz in [-.44,.44]: m.box((.534,.52,z+dz),(.018,.75,.028),'Brass')
        for y in [.16,.88]: m.box((.534,y,z),(.018,.025,.90),'Brass')
        m.box((.536,.53,z),(.014,.60,.74),'Wood')
        m.ellipsoid((.55,.82,z),(.015,.025,.035),'Brass',12,8)
    m.tube([(.78,.22,-2.95),(.78,.22,2.95)],.025,'Brass')
    for z in [-2.6,-1.3,0,1.3,2.6]:
        m.tube([(.51,.14,z),(.70,.14,z),(.78,.22,z)],.019,'Brass')
    m.box((.52,.07,0),(.075,.10,6.08),'Wood')
    m.save('OrnateBar')


if __name__=='__main__':
    chair(); booth(); stool(); table(); lamp(); chandelier(); trunk(); piano(); bar()
