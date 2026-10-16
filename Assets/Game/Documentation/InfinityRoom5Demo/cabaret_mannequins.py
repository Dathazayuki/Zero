"""Unclothed static display figures with continuous torso profiles and smooth normals."""
from cabaret_mesh import Mesh
import math


def torso(mesh, headless=False):
    profile = [(.84,.13,.085),(.88,.18,.12),(.94,.205,.135),(1.00,.185,.12),
               (1.07,.15,.097),(1.14,.135,.086),(1.21,.155,.103),
               (1.28,.187,.115),(1.35,.205,.11),(1.41,.21,.09),
               (1.46,.19,.07),(1.50,.115,.055),(1.535,.055,.047),
               (1.57,.048,.045)]
    def sculpt(p):
        x,y,z=p
        front=max(0,-z/.11)
        bust=.038*math.exp(-((y-1.31)/.095)**2)*front
        bust*=math.exp(-((abs(x)-.09)/.07)**2)
        return (x,y,z-bust)
    mesh.body([(0,y,0,rx,rz) for y,rx,rz in profile],sculpt=sculpt)
    if headless:
        mesh.lathe((0,1.57,0),[(0,0),(.05,0),(.054,.006),(.044,.014),(0,.014)])
    else:
        head=[(1.575,.038,.042),(1.605,.042,.060),(1.64,.062,.072),
              (1.69,.083,.081),(1.745,.088,.079),(1.79,.078,.067),
              (1.82,.055,.045),(1.835,0,0)]
        mesh.body([(0,y,-.006,rx,rz) for y,rx,rz in head])


def arms(mesh, full):
    for s in [-1,1]:
        if not full:
            mesh.ellipsoid((s*.194,1.427,0),(.055,.059,.064))
            continue
        profile=[(s*.18,1.44,0,.048,.053),(s*.218,1.405,0,.052,.055),
                 (s*.25,1.31,-.004,.048,.047),(s*.27,1.20,-.009,.039,.040),
                 (s*.272,1.14,-.013,.035,.034),(s*.285,1.05,-.014,.033,.035),
                 (s*.30,.93,-.022,.026,.026),(s*.305,.895,-.025,.024,.021)]
        mesh.body(profile[::-1])
        mesh.body([(s*.308,.795,-.025,.016,.020),(s*.31,.815,-.022,.031,.025),
                   (s*.31,.865,-.024,.032,.022),(s*.305,.895,-.025,.023,.020)])
        for finger in range(4):
            x=s*(.289+finger*.012)
            mesh.body([(x,.77+(finger%2)*.005,-.022,.004,.005),
                       (x,.804,-.023,.006,.007)],sides=10)
        mesh.tube([(s*.278,.861,-.027),(s*.267,.823,-.036)],.01,'Ivory',12)
        mesh.ring((s*.305,.894,-.025),(.024,.021),.0018,'Joint')


def mannequin(full=True):
    mesh=Mesh()
    torso(mesh,headless=not full); arms(mesh,full)
    if full:
        for s in [-1,1]:
            mesh.body([(s*.087,.095,0,.029,.031),(s*.086,.19,.005,.034,.037),
                       (s*.09,.33,.005,.045,.045),(s*.092,.47,-.004,.046,.048),
                       (s*.098,.53,-.014,.052,.05),(s*.099,.64,-.006,.065,.064),
                       (s*.098,.76,.003,.073,.077),(s*.097,.86,0,.075,.085)])
            mesh.ellipsoid((s*.087,.061,-.05),(.053,.039,.113))
        mesh.lathe((0,0,0),[(0,0),(.26,0),(.27,.02),(.23,.045),(0,.045)],'Plinth')
    else:
        mesh.lathe((0,.13,0),[(0,0),(.18,0),(.17,.035),(.07,.055)],'Brass')
        mesh.lathe((0,.18,0),[(.024,0),(.024,.65)],'Brass')
        mesh.lathe((0,0,0),[(0,0),(.26,0),(.27,.055),(.25,.10),(0,.10)],'Plinth')
    mesh.save('FullMannequin' if full else 'TorsoMannequin')


if __name__=='__main__':
    mannequin(); mannequin(False)
