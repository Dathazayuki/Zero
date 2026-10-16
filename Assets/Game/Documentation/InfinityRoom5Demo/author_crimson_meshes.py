"""Offline detailed mesh authoring for the isolated Crimson Cabaret demo."""
import math
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "InfinityRoom4Demo"))
import author_kitchen_meshes
from author_kitchen_meshes import Mesh
import random


def ellipsoid(mesh, center, radii, material="Porcelain", rings=8, sides=12):
    x, y, z = center
    rx, ry, rz = radii
    layers = []
    for row in range(rings + 1):
        phi = -math.pi / 2 + math.pi * row / rings
        layers.append([(x + rx * math.cos(phi) * math.cos(i * math.tau / sides),
                        y + ry * math.sin(phi),
                        z + rz * math.cos(phi) * math.sin(i * math.tau / sides))
                       for i in range(sides)])
    for lower, upper in zip(layers, layers[1:]):
        for i in range(sides):
            nxt = (i + 1) % sides
            mesh.face(material, [lower[i], lower[nxt], upper[nxt], upper[i]])


def bottle(mesh, x, y, z, height, material="Glass", radius=.045):
    mesh.lathe((x, y, z), [(0, 0), (radius * .72, 0), (radius, .025),
                 (radius, height * .62), (radius * .78, height * .72),
                 (radius * .35, height * .75), (radius * .33, height * .94),
                 (radius * .45, height * .95), (radius * .45, height),
                 (radius * .25, height), (0, height)], material, 16)
    mesh.box((x, y + height * .38, z - radius * .98),
             (radius * 1.2, height * .22, .005), "Label")


def bar_counter():
    m = Mesh()
    # Long service counter runs parallel to the room, with a softened brass edge.
    m.box((0, .48, 0), (1.02, .78, 6.0), "Dark")
    for z in [i * .5 - 2.75 for i in range(12)]:
        m.box((0, .47, z), (.04, .60, .008), "Wood")
        m.box((-.52, .47, z), (.035, .60, .018), "Dark")
        m.box((.52, .47, z), (.035, .60, .018), "Dark")
        for y in [.19, .74]:
            m.box((-.545, y, z), (.01, .018, .025), "Brass")
    m.box((0, .9, 0), (1.17, .14, 6.18), "Wood")
    m.box((0, .975, 0), (1.20, .025, 6.20), "Brass")
    # Brass foot rail, brackets and four squared supports.
    m.tube([(.32, .19, -2.8), (.32, .19, 2.8)], .022, "Brass")
    for z in [-2.4, -1.2, 0, 1.2, 2.4]:
        m.tube([(.32, .22, z), (.32, .39, z)], .014, "Brass")
    for x in [-.38, .38]:
        for z in [-2.7, 2.7]:
            m.box((x, .03, z), (.10, .06, .12), "Dark")
    m.save("CabaretBarCounter")


def backbar():
    m = Mesh()
    m.box((0, .56, 0), (.20, 1.08, 5.5), "Dark")
    for z in [-2.4, 2.4]:
        m.box((.04, 1.45, z), (.30, 2.9, .16), "Wood")
    for y in [1.0, 1.82, 2.63]:
        m.box((.20, y, 0), (.42, .065, 5.4), "Wood")
        m.box((.416, y + .055, 0), (.025, .025, 5.4), "Brass")
        for z in [-2.5, 2.5]:
            m.tube([(.02, y, z), (.36, y, z)], .017, "Brass")
    # Tumbler racks and stems suspended below each shelf.
    for row_y in [1.73, 2.54]:
        for i in range(12):
            z = -2.25 + i * .41
            x = .19
            m.tube([(x, row_y, z), (x + .12, row_y, z),
                    (x + .12, row_y - .23, z)], .007, "Brass")
            m.lathe((x + .12, row_y - .29, z),
                    [(0, .07), (.055, .07), (.044, .01), (.012, 0), (0, 0)],
                    "Glass", 12)
    # Distinct bottles and a few small casks staggered along the three shelves.
    for row, shelf_y in enumerate([1.04, 1.86, 2.67]):
        for i in range(13):
            z = -2.35 + i * .39
            height = .20 + ((i * 7 + row * 5) % 5) * .045
            bottle(m, .13, shelf_y, z, height,
                   "BottleGreen" if (i + row) % 3 else "BottleAmber",
                   .045 + (i % 2) * .008)
    m.save("BackBarDisplay")


def stage():
    m = Mesh()
    m.box((0, .12, 0), (6.8, .24, 1.82), "Dark")
    m.box((0, .25, -.01), (6.82, .035, 1.84), "Wood")
    m.box((0, .02, -.94), (6.78, .14, .045), "Brass")
    for x in [-3.25, 3.25]:
        m.box((x, .31, 0), (.08, .12, 1.7), "Brass")
    # Low centered stair access, broad enough to read as a stage step.
    m.box((0, .07, -1.18), (1.55, .14, .40), "Wood")
    m.box((0, .145, -1.18), (1.58, .018, .42), "Brass")
    m.box((0, .18, -.88), (1.55, .10, .24), "Wood")
    m.save("EmptyPerformanceStage")


def curtains():
    m = Mesh()
    # Pleated full-width blackout curtain: dense deep-red vertical folds.
    for i in range(43):
        x = -3.18 + i * .151
        material = "CurtainShadow" if i % 3 == 0 else "CurtainRed"
        pleat = .035 * math.sin(i * math.pi)
        # Exporter mirrors X and reverses OBJ winding; author these back-to-front
        # so the audience-side surface normal faces the room camera after import.
        m.face(material, [(x, 3.27, .055 + pleat), (x + .15, 3.27, pleat),
                          (x + .15, .28, .0), (x, .28, .0)])
    # Draped valance with a scalloped lower edge.
    for i in range(15):
        x = -3.25 + i * .43
        bottom = 2.73 - .16 * abs(math.sin(i * math.pi / 2))
        m.face("CurtainRed", [(x, bottom, .04), (x + .44, bottom, .04),
                               (x + .44, 3.34, -.04), (x, 3.34, -.04)])
    # Two weighted side legs, brass tie-backs and tassels.
    for side in [-1, 1]:
        x = side * 3.35
        m.box((x, 1.77, .065), (.26, 3.16, .16), "CurtainRed")
        m.tube([(x, 1.72, -.04), (x + side * .14, 1.72, -.05),
                (x + side * .17, 1.46, -.05)], .018, "Brass")
        m.lathe((x + side * .17, 1.38, -.05),
                [(0, 0), (.035, 0), (.055, .08), (0, .15)], "Brass", 12)
    m.save("VelvetCurtainWall")


def mannequin(head=True, costume=False):
    m = Mesh()
    body = "DressFabric" if costume else "Porcelain"
    # Two-column display plinth, collar and a static neutral upright body.
    m.lathe((0, .02, 0), [(0, 0), (.19, 0), (.20, .035),
                         (.15, .07), (.11, .10), (0, .10)], "Brass", 24)
    m.tube([(0, .10, 0), (0, .88, 0)], .025, "Dark")
    ellipsoid(m, (0, .57, 0), (.16, .11, .105), body)
    ellipsoid(m, (0, .79, 0), (.19, .25, .11), body)
    ellipsoid(m, (0, 1.12, 0), (.245, .30, .12), body)
    ellipsoid(m, (0, 1.34, 0), (.28, .13, .115), body)
    # Legs are separate sculpted forms with visible knees and narrow ankles.
    for side in [-1, 1]:
        x = side * .09
        ellipsoid(m, (x, .44, 0), (.068, .23, .07), body)
        ellipsoid(m, (x, .18, -.012), (.052, .16, .052), body)
        ellipsoid(m, (x, .035, -.045), (.075, .045, .14), body)
        # arms rest down at the sides; poses never face or reach toward the stage.
        shoulder = side * .245
        elbow = side * .335
        wrist = side * .365
        m.tube([(shoulder, 1.31, 0), (elbow, 1.06, -.015),
                (wrist, .82, -.025)], .044, body, 14)
        ellipsoid(m, (wrist, .77, -.03), (.046, .075, .034), body, 6, 10)
        if costume:
            m.box((x, .48, 0), (.16, .10, .17), "Brass")
    if head:
        m.tube([(0, 1.42, 0), (0, 1.51, 0)], .048, body)
        ellipsoid(m, (0, 1.66, 0), (.115, .15, .11), body)
        # Featureless face plane; no eyes, mouth, makeup or expression.
        ellipsoid(m, (0, 1.65, -.068), (.066, .09, .018), "FaceBlank", 6, 10)
    else:
        m.lathe((0, 1.41, 0), [(0, 0), (.10, 0), (.09, .045),
                               (.06, .07), (0, .07)], "Brass", 16)
    if costume:
        m.lathe((0, .55, 0), [(.12, 0), (.21, .10), (.29, .32),
                              (.28, .40), (.235, .40), (.23, .32),
                              (.17, .10), (.12, 0)], "DressFabric", 32)
        m.box((0, 1.27, -.115), (.055, .32, .018), "Brass")
    m.save("CostumedMannequin" if costume else
           "HeadlessDisplayTorso" if not head else "BlankDisplayMannequin")


def stool():
    m = Mesh()
    m.tube([(0, .02, 0), (0, .72, 0)], .037, "Brass")
    m.tube([(-.24, .32, 0), (.24, .32, 0)], .016, "Brass")
    for a in range(4):
        angle = a * math.pi / 2
        m.tube([(0, .12, 0), (.25 * math.cos(angle), .025,
                               .25 * math.sin(angle))], .019, "Dark")
    m.lathe((0, .75, 0), [(0, 0), (.25, 0), (.25, .07),
                         (.21, .12), (0, .12)], "Upholstery", 24)
    m.save("CabaretBarStool")


def garment_rack():
    m = Mesh()
    for z in [-.76, .76]:
        m.tube([(-.68, .08, z), (-.68, 1.82, z)], .027, "Dark")
        m.tube([(-.68, .08, z), (.68, .08, z)], .027, "Dark")
        m.tube([(-.68, 1.78, z), (.68, 1.78, z)], .027, "Brass")
    m.tube([(-.68, 1.78, -.76), (-.68, 1.78, .76)], .03, "Brass")
    for i in range(11):
        z = -.65 + i * .13
        m.tube([(0, 1.76, z), (0, 1.91, z), (.08, 1.96, z)], .008, "Brass")
        m.tube([(0, 1.91, z), (-.08, 1.96, z)], .008, "Brass")
        width = .13 + (i % 3) * .025
        color = "CurtainRed" if i % 2 else "DressFabric"
        m.face(color, [(-width, 1.76, z), (-.22, .58, z + .055),
                       (.22, .58, z + .055), (width, 1.76, z)])
        m.face(color, [(0, 1.72, z), (-width, 1.76, z),
                       (width, 1.76, z)])
    for x in [-.5, .5]:
        for z in [-.58, .58]:
            m.tube([(x, .10, z), (x, 1.75, z)], .014, "Dark")
    m.save("CostumeDisplayRack")


def chandelier():
    m = Mesh()
    m.tube([(0, 3.05, 0), (0, 3.43, 0)], .025, "Brass")
    m.lathe((0, 2.99, 0), [(0, 0), (.08, .03), (.13, .09),
                          (.15, .13), (.07, .17), (0, .18)], "Brass", 24)
    m.tube([(-.65, 2.96, 0), (.65, 2.96, 0)], .025, "Brass")
    m.tube([(0, 2.96, -.42), (0, 2.96, .42)], .025, "Brass")
    for x, z in [(-.65, 0), (.65, 0), (0, -.42), (0, .42)]:
        m.tube([(x, 2.96, z), (x * 1.12, 2.78, z * 1.12)], .018, "Brass")
        m.lathe((x * 1.12, 2.72, z * 1.12),
                [(0, 0), (.085, 0), (.08, .08), (.05, .15), (0, .16)],
                "Glow", 16)
    m.save("CabaretChandelier")


def parquet_and_wall_panels():
    rng = random.Random(51)
    m = Mesh()
    for row in range(20):
        z = -4.75 + row * .5
        shift = .32 if row % 2 else 0
        for col in range(12):
            x = -3.58 + col * .65 - shift
            m.box((x, -.009, z), (.635, .014, .485),
                  "ParquetA" if rng.random() < .57 else "ParquetB")
    m.save("ParquetFloor")
    m = Mesh()
    for col in range(32):
        for row in range(14):
            x = -3.875 + col * .25
            y = .125 + row * .25
            mat = "PanelA" if rng.random() < .67 else "PanelB"
            m.box((x, y, 0), (.244, .244, .016), mat)
    m.save("DistressedWallPanels")


def table_and_chair():
    m = Mesh()
    m.lathe((0, .77, 0), [(0, 0), (.43, 0), (.48, .03),
                         (.48, .08), (.43, .10), (0, .10)], "Wood", 32)
    m.tube([(0, .05, 0), (0, .77, 0)], .047, "Dark")
    m.lathe((0, .035, 0), [(0, 0), (.29, 0), (.32, .04),
                          (0, .07)], "Dark", 24)
    m.save("CabaretTable")
    m = Mesh()
    m.lathe((0, .46, 0), [(0, 0), (.23, 0), (.25, .07),
                         (.22, .12), (0, .12)], "Upholstery", 24)
    m.box((0, .91, .19), (.47, .68, .10), "Wood")
    m.box((0, .91, .247), (.39, .54, .026), "Upholstery")
    m.box((0, .54, .20), (.42, .055, .07), "Brass")
    for x in [-.18, .18]:
        m.tube([(x, .03, -.16), (x, .48, -.16),
                (x, .48, .18), (x, .78, .18)], .024, "Wood")
        m.tube([(x, .03, -.16), (x, .03, .18)], .022, "Wood")
    m.save("CabaretChair")


if __name__ == "__main__":
    import sys
    from pathlib import Path
    out = Path(__file__).resolve().parents[2] / "Models" / "InfinityRoom5Demo"
    out.mkdir(parents=True, exist_ok=True)
    # Redirect the shared lightweight OBJ writer to Room 5's own asset directory.
    author_kitchen_meshes.OUT = out
    author_kitchen_meshes.ASSET_PREFIX = "IR5_"
    bar_counter(); backbar(); stage(); curtains(); parquet_and_wall_panels()
    mannequin(); mannequin(head=False); mannequin(costume=True)
    stool(); table_and_chair(); garment_rack(); chandelier()
