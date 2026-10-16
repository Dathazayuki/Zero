"""Explicitly authored architecture and stage dressing for the Room 5 detail pass."""
from cabaret_mesh import Mesh
from cabaret_furniture import lamp_mesh
import math
import random


def moldings():
    m=Mesh()
    for y,thickness,depth in [(3.46,.08,.21),(3.38,.035,.16),(3.31,.055,.12),
                               (1.02,.045,.055),(.10,.15,.07)]:
        for x in [-3.94,3.94]: m.box((x,y,0),(depth,thickness,10),'Wood')
        for z in [-4.94,4.94]: m.box((0,y,z),(7.94,thickness,depth),'Wood')
    for x in [-3.89,3.89]:
        m.box((x,3.34,0),(.018,.015,9.86),'Brass')
        m.box((x,1.04,0),(.014,.012,9.9),'Brass')
    for x in [-1.8,1.8]: m.box((x,3.47,0),(.12,.075,9.78),'Wood')
    for z in [-3.2,0,3.2]: m.box((0,3.47,z),(7.85,.075,.12),'Wood')
    for side in [-1,1]:
        x=side*3.965
        m.box((x,.53,0),(.045,.90,9.87),'Wood')
        for z in [-4.3,-3,-1.7,-.4,.9,2.2,3.5,4.5]:
            for dz in [-.53,.53]: m.box((x-side*.029,.53,z+dz),(.018,.70,.025),'Brass')
            for y in [.18,.88]: m.box((x-side*.029,y,z),(.018,.025,1.08),'Brass')
    for radius in [.32,.37,.42,.46]:
        m.ring((0,3.485,-.30),(radius,radius),.013,'Brass')
    for i in range(24):
        t=i*math.tau/24
        m.ellipsoid((.39*math.cos(t),3.47,-.30+.39*math.sin(t)),(.024,.015,.044),'Brass',12,8)
    m.save('CorniceAndWainscot')


def floor():
    m=Mesh(); rng=random.Random(55)
    for row in range(40):
        z=-4.875+row*.25
        for col in range(8):
            x=-3.5+col+(0.5 if row%2 else 0)
            if x>.0+3.5: continue
            mat='FloorA' if rng.random()<.6 else 'FloorB'
            m.box((x,-.01,z),(.993,.012,.243),mat)
    m.save('NarrowParquet')


def curtains():
    m=Mesh(); width=6.55; cols=160; rows=25
    grid=[]
    for j in range(rows+1):
        y=.26+j*3.06/rows
        grid.append([(-width/2+i*width/cols,y,
                      .11*math.cos(i*math.tau*34/cols)+.012*math.sin(y*3+i*.3)) for i in range(cols+1)])
    for j in range(rows):
        for i in range(cols):
            m.face('Curtain',[grid[j][i],grid[j+1][i],grid[j+1][i+1],grid[j][i+1]],True)
    for k in range(4):
        x0=-3.28+k*1.64; top=3.32; g=[]
        for j in range(13):
            v=j/12; ring=[]
            for i in range(41):
                u=i/40; bottom=3.13-.46*math.sin(u*math.pi)
                y=top+(bottom-top)*v
                z=-.13-.11*math.sin(u*math.pi)+.025*math.cos(v*math.tau*5)
                ring.append((x0+u*1.64,y,z))
            g.append(ring)
        for j in range(12):
            for i in range(40): m.face('Curtain',[g[j][i],g[j][i+1],g[j+1][i+1],g[j+1][i]],True)
        m.tube(g[-1],.012,'Brass',8)
    for side in [-1,1]:
        x=side*3.26
        m.box((x,1.78,-.035),(.21,3.1,.22),'Curtain')
        m.tube([(x,1.59,-.15),(x+side*.17,1.52,-.16),(x+side*.20,1.27,-.16)],.018,'Brass')
        m.lathe((x+side*.20,1.15,-.16),[(0,0),(.04,0),(.045,.08),(.01,.17)],'Brass')
        m.box((side*3.52,1.74,-.04),(.12,3.30,.17),'Wood')
        m.box((side*3.50,1.74,-.15),(.018,3.30,.02),'Brass')
    m.box((0,3.40,-.06),(7.18,.13,.20),'Wood')
    m.box((0,3.40,-.17),(7.15,.025,.025),'Brass')
    m.save('PleatedStageCurtains')


def mirror_frame():
    m=Mesh(); m.box((0,1.50,0),(.85,1.7,.045),'Mirror')
    for x in [-.455,.455]:
        m.box((x,1.5,-.015),(.055,1.80,.075),'Wood')
        m.box((x,1.5,-.057),(.024,1.78,.016),'Brass')
    for y in [.59,2.41]:
        m.box((0,y,-.015),(.965,.055,.075),'Wood')
        m.box((0,y,-.057),(.95,.024,.016),'Brass')
    for s in [-1,1]:
        m.tube([(s*.35,2.43,-.03),(s*.27,2.51,-.03),(s*.13,2.53,-.03),(0,2.59,-.03)],.022,'Brass')
    m.save('GildedWallMirror')


def sconce():
    m=Mesh(); m.ellipsoid((0,.32,.045),(.09,.24,.02),'Brass')
    for s in [-1,1]:
        m.tube([(0,.25,0),(s*.08,.22,-.10),(s*.15,.27,-.15),(s*.15,.36,-.15)],.012,'Brass')
        lamp_mesh(m,(s*.15,.17,-.15))
    m.save('DoubleWallSconce')


def clock():
    m=Mesh()
    m.ellipsoid((0,0,0),(.32,.32,.035),'Dial')
    m.ring((0,0,-.040),(.33,.33),.027,'Wood',plane='xy')
    m.ring((0,0,-.055),(.29,.29),.006,'Brass',plane='xy')
    for i in range(60):
        t=i*math.tau/60; inner=.235 if i%5==0 else .262
        m.tube([(inner*math.sin(t),inner*math.cos(t),-.065),
                (.275*math.sin(t),.275*math.cos(t),-.065)],.0035 if i%5==0 else .0015,'Black',6)
    m.tube([(0,0,-.073),(-.12,.08,-.073)],.009,'Black')
    m.tube([(0,0,-.078),(.08,.205,-.078)],.006,'Black')
    m.ellipsoid((0,0,-.08),(.015,.015,.008),'Brass',12,8)
    m.save('CabaretWallClock')


def pedestal():
    m=Mesh(); m.box((0,.31,0),(.52,.62,.52),'Plaster')
    m.box((0,.62,0),(.57,.045,.57),'Plinth')
    m.box((0,.045,0),(.58,.07,.58),'Wood')
    for x in [-.235,.235]: m.box((x,.31,-.27),(.012,.51,.012),'Brass')
    m.save('DisplayPedestal')


if __name__=='__main__':
    moldings(); floor(); curtains(); mirror_frame(); sconce(); clock(); pedestal()
