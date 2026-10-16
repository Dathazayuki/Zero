"""Explicit UnitySkills authoring of the new Crimson Cabaret demo scene."""
import json
from pathlib import Path
import sys
import urllib.request

BASE = "http://localhost:8090"
SCENE = "Assets/Game/Scenes/Demos/InfinityRoom5_CrimsonCabaret_Demo.unity"
SCENE_NAME = "InfinityRoom5_CrimsonCabaret_Demo"
ROOT = "IR5_CrimsonCabaret"
MAT = "Assets/Game/Materials/InfinityRoom5Demo/"
MODEL = "Assets/Game/Models/InfinityRoom5Demo/IR5_"
validated = set()


def request(endpoint, body):
    data = json.dumps(body).encode()
    req = urllib.request.Request(BASE + endpoint, data,
                                 {"Content-Type": "application/json"})
    with urllib.request.urlopen(req, timeout=45) as response:
        return json.load(response)


def call(skill, **args):
    if skill not in validated:
        preview = request("/skill/" + skill + "?mode=dryRun", args)
        if not preview.get("valid") or not preview.get("authorization", {}).get("allowed"):
            raise RuntimeError(json.dumps(preview))
        validated.add(skill)
    result = request("/skill/" + skill, args)
    if result.get("status") == "error":
        raise RuntimeError(json.dumps(result))
    value = result.get("result", result)
    if value.get("failCount", 0):
        raise RuntimeError(json.dumps(value))
    print(skill, "OK", value.get("successCount", ""), flush=True)
    return value


def batch(skill, items):
    for start in range(0, len(items), 24):
        call(skill, items=json.dumps(items[start:start + 24]))


def prop(name, component, key, value):
    return dict(name=name, componentType=component, propertyName=key, value=str(value))


def parent(name, parent_name):
    call("gameobject_set_parent", childName=name, parentName=parent_name)


def cube(name, pos, size, material, parent_name="IR5_Architecture"):
    create = dict(name=name, primitiveType="Cube", parentName=parent_name,
                  x=pos[0], y=pos[1], z=pos[2],
                  scaleX=size[0], scaleY=size[1], scaleZ=size[2])
    assign = dict(name=name, materialPath=MAT + "M_IR5_" + material + ".mat")
    return create, assign


def create_scene():
    call("scene_create", scenePath=SCENE)
    info = call("scene_get_info")
    if info.get("sceneName") != SCENE_NAME or info.get("rootObjectCount", 0) != 0:
        raise RuntimeError("New empty Crimson Cabaret scene was not created.")


def materials():
    folder = Path(__file__).resolve().parents[2] / "Materials" / "InfinityRoom5Demo"
    if not folder.exists():
        call("asset_create_folder", folderPath="Assets/Game/Materials/InfinityRoom5Demo")
    colors = {
        "Wood": (.25, .12, .055), "Dark": (.055, .037, .027),
        "Brass": (.47, .25, .072), "Glass": (.12, .16, .17),
        "BottleGreen": (.025, .14, .085), "BottleAmber": (.38, .13, .025),
        "Label": (.52, .31, .13), "CurtainRed": (.17, .008, .023),
        "CurtainShadow": (.045, .002, .012), "DressFabric": (.29, .035, .043),
        "Porcelain": (.68, .61, .49), "FaceBlank": (.79, .72, .60),
        "Upholstery": (.18, .018, .027), "Plaster": (.47, .40, .32),
        "ParquetA": (.56, .31, .16), "ParquetB": (.48, .25, .13),
        "PanelA": (.43, .34, .26), "PanelB": (.37, .28, .21),
        "Ceiling": (.28, .22, .16), "Glow": (1, .34, .095),
    }
    batch("material_create_batch", [dict(name="M_IR5_" + n, savePath=MAT,
          shaderName="Universal Render Pipeline/Lit") for n in colors])
    batch("material_set_colors_batch", [dict(path=MAT + "M_IR5_" + n + ".mat",
          r=c[0], g=c[1], b=c[2]) for n, c in colors.items()])
    wood_tex = "Assets/Game/Model/table/textures/wood_table_001_diff_1k.jpg"
    grunge = ("Assets/Game/Materials/EnvironmentLoop/" +
        "dark-yellow-concrete-scary-for-background-yellow-wall-halloween-background-" +
        "concept-horror-cement-texture-free-photo.jpg")
    for name in ["Wood", "ParquetA", "ParquetB"]:
        call("material_set_texture", path=MAT + "M_IR5_" + name + ".mat",
             propertyName="_BaseMap", texturePath=wood_tex if name == "Wood" else grunge)
    for name in ["Plaster", "PanelA", "PanelB"]:
        call("material_set_texture", path=MAT + "M_IR5_" + name + ".mat",
             propertyName="_BaseMap", texturePath=grunge)
    for name, value in [("Brass", .72), ("Glass", .22)]:
        call("material_set_float", path=MAT + "M_IR5_" + name + ".mat",
             propertyName="_Metallic", value=value)
    call("material_set_emission", path=MAT + "M_IR5_Glow.mat",
         r=1, g=.20, b=.045, intensity=1.15)


def hierarchy():
    batch("gameobject_create_batch", [dict(name=ROOT)])
    batch("gameobject_create_batch", [dict(name=n, parentName=ROOT) for n in
          ["IR5_Architecture", "IR5_Furniture", "IR5_Decor", "IR5_Lighting", "IR5_Markers"]])
    specs = [
        ("IR5_Floor", (0, -.14, 0), (8.35, .25, 10.35), "Dark"),
        ("IR5_Ceiling", (0, 3.62, 0), (8.35, .24, 10.35), "Ceiling"),
        ("IR5_WestWall", (-4.08, 1.75, 0), (.18, 3.5, 10.0), "Plaster"),
        ("IR5_EastWall", (4.08, 1.75, 0), (.18, 3.5, 10.0), "Plaster"),
        ("IR5_NorthWall", (0, 1.75, 5.08), (8.35, 3.5, .18), "Plaster"),
        ("IR5_SouthWallL", (-2.45, 1.75, -5.08), (3.45, 3.5, .18), "Plaster"),
        ("IR5_SouthWallR", (2.45, 1.75, -5.08), (3.45, 3.5, .18), "Plaster"),
        ("IR5_EntryHeader", (0, 3.05, -5.08), (1.45, .9, .18), "Plaster"),
        ("IR5_WestSkirting", (-3.96, .085, 0), (.05, .17, 9.9), "Wood"),
        ("IR5_EastSkirting", (3.96, .085, 0), (.05, .17, 9.9), "Wood"),
        ("IR5_NorthSkirting", (0, .085, 4.96), (8.0, .17, .05), "Wood"),
        ("IR5_SouthSkirtingL", (-2.45, .085, -4.96), (3.35, .17, .05), "Wood"),
        ("IR5_SouthSkirtingR", (2.45, .085, -4.96), (3.35, .17, .05), "Wood"),
    ]
    objects = [cube(*spec) for spec in specs]
    batch("gameobject_create_batch", [o[0] for o in objects])
    batch("material_assign_batch", [o[1] for o in objects])


def import_models():
    call("asset_refresh")
    models = [
        ("IR5_Parquet", "ParquetFloor", 0, -.007, 0, 0, "IR5_Architecture"),
        ("IR5_WallPanelNorth", "DistressedWallPanels", 0, 0, 4.985, 0, "IR5_Architecture"),
        ("IR5_WallPanelWest", "DistressedWallPanels", -3.985, 0, 0, -90, "IR5_Architecture"),
        ("IR5_WallPanelEast", "DistressedWallPanels", 3.985, 0, 0, 90, "IR5_Architecture"),
        ("IR5_Stage", "EmptyPerformanceStage", 0, 0, 4.0, 0, "IR5_Furniture"),
        ("IR5_Curtain", "VelvetCurtainWall", 0, 0, 4.63, 0, "IR5_Decor"),
        ("IR5_Bar", "CabaretBarCounter", -3.08, 0, .20, 0, "IR5_Furniture"),
        ("IR5_BackBar", "BackBarDisplay", -3.77, 0, .2, 0, "IR5_Furniture"),
        ("IR5_Chandelier", "CabaretChandelier", 0, 0, -.30, 0, "IR5_Decor"),
        ("IR5_CostumeRack", "CostumeDisplayRack", 3.38, 0, -.2, 90, "IR5_Decor"),
    ]
    for i, z in enumerate([-2.15, -.75, .70, 2.12]):
        models.append((f"IR5_BarStool{i}", "CabaretBarStool", -2.12, 0, z, 0, "IR5_Furniture"))
    for i, (x, z) in enumerate([(1.95, -2.55), (1.95, -.10), (1.95, 2.30)]):
        models.append((f"IR5_CafeTable{i}", "CabaretTable", x, 0, z, i * 21, "IR5_Furniture"))
        for j, (dx, dz) in enumerate([(-.70, 0), (.70, 0)]):
            models.append((f"IR5_CafeChair{i}_{j}", "CabaretChair", x + dx, 0,
                           z + dz, 90 if j == 0 else -90, "IR5_Furniture"))
    placements = [(-3.62, -3.65, 90), (-3.60, -2.75, 110), (-3.55, 3.45, 90),
                  (3.50, -3.8, 270), (3.50, -2.8, 286), (3.50, -1.8, 254),
                  (3.50, -.8, 278), (3.50, .2, 258), (3.50, 1.2, 282),
                  (3.50, 2.2, 262), (3.50, 3.2, 276)]
    for i, (x, z, rot) in enumerate(placements):
        model = "CostumedMannequin" if i in [1, 5, 9] else "BlankDisplayMannequin"
        models.append((f"IR5_Mannequin{i:02d}", model, x, 0, z, rot, "IR5_Decor"))
    models += [("IR5_HeadlessTorso0", "HeadlessDisplayTorso", -3.48, 0, 1.25, 100, "IR5_Decor"),
               ("IR5_HeadlessTorso1", "HeadlessDisplayTorso", 3.48, 0, .95, -120, "IR5_Decor")]
    payload = [dict(prefabPath=MODEL + src + ".obj", name=name,
                    x=x, y=y, z=z, rotY=rot, parentName=owner)
               for name, src, x, y, z, rot, owner in models]
    call("prefab_instantiate_batch", items=payload)
    assignments = []
    colliders = []
    for name, src, *_ in models:
        info = call("gameobject_get_info", name=name)
        for child in info.get("children", []):
            assignments.append(dict(path=child["path"],
                materialPath=MAT + "M_IR5_" + child["name"] + ".mat"))
        if src in ["BlankDisplayMannequin", "CostumedMannequin", "HeadlessDisplayTorso"]:
            colliders.append(name)
    for assignment in assignments:
        call("material_assign", path=assignment["path"],
             materialPath=assignment["materialPath"])
    for i in range(0, len(colliders), 20):
        chunk = colliders[i:i + 20]
        for name in chunk:
            call("component_add", name=name, componentType="CapsuleCollider")
        batch("component_set_property_batch", [prop(n, "CapsuleCollider", "height", 1.78) for n in chunk])
        batch("component_set_property_batch", [prop(n, "CapsuleCollider", "radius", .17) for n in chunk])
        batch("component_set_property_batch", [prop(n, "CapsuleCollider", "center", "0,.89,0") for n in chunk])


def colliders_and_lights():
    proxies = [
        ("IR5_StageCollider", (0, .12, 4.0), (6.8, .24, 1.82)),
        ("IR5_StageStepCollider", (0, .07, 2.81), (1.55, .14, .4)),
        ("IR5_BarCollider", (-3.08, .47, .20), (1.02, .94, 6.0)),
        ("IR5_BackBarCollider", (-3.69, 1.40, .2), (.32, 2.8, 5.5)),
        ("IR5_CostumeRackCollider", (3.38, .9, -.2), (1.6, 1.8, .16)),
    ]
    batch("gameobject_create_batch", [cube(n, p, s, "Dark", "IR5_Furniture")[0]
                                      for n, p, s in proxies])
    for name, *_ in proxies:
        call("component_set_enabled", name=name, componentType="MeshRenderer", enabled=False)
    lights = [
        ("IR5_BarGlow", -2.65, 2.45, -1.3, (1, .53, .25), 2.1, 5, "None"),
        ("IR5_BackBarGlow", -3.1, 2.45, 1.5, (1, .55, .24), 1.7, 4, "None"),
        ("IR5_EntranceGlow", 1.3, 2.7, -3.7, (1, .65, .38), .75, 4, "None"),
        ("IR5_StageRedWash", 0, 2.6, 3.25, (.48, .012, .03), 1.8, 5.5, "None"),
        ("IR5_CurtainEdge", 0, 2.4, 4.3, (.60, .018, .035), .85, 4.0, "None"),
    ]
    for name, x, y, z, color, intensity, radius, shadows in lights:
        call("light_create", name=name, lightType="Point", x=x, y=y, z=z,
             r=color[0], g=color[1], b=color[2], intensity=intensity,
             range=radius, shadows=shadows)
        parent(name, "IR5_Lighting")
    call("light_add_reflection_probe", probeName="IR5_ReflectionProbe", x=0, y=1.5, z=0,
         sizeX=8, sizeY=3.2, sizeZ=10, resolution=128)


def player_and_preview():
    batch("gameobject_create_batch", [
        dict(name="IR5_DemoPlayer", x=0, y=.04, z=-4.20),
        dict(name="IR5_ResumePoint", parentName="IR5_Markers", x=0, y=.04, z=-4.20)])
    call("camera_create", name="IR5_PreviewCamera", x=0, y=1.65, z=-4.20,
         addAudioListener=True)
    call("gameobject_set_parent", childName="IR5_PreviewCamera", parentName="IR5_DemoPlayer")
    call("camera_set_properties", name="IR5_PreviewCamera", fieldOfView=66,
         nearClipPlane=.05, farClipPlane=60, clearFlags="Color", bgR=.018, bgG=.009, bgB=.012)
    call("component_add", name="IR5_DemoPlayer",
         componentType="Zero.AnomalyPrototype.Player.FirstPersonController")
    batch("component_set_property_batch", [
          prop("IR5_DemoPlayer", "CharacterController", "height", 1.8),
          prop("IR5_DemoPlayer", "CharacterController", "radius", .28),
          prop("IR5_DemoPlayer", "CharacterController", "center", "0,.9,0")])
    call("camera_screenshot", name="IR5_PreviewCamera", width=1600, height=900,
         savePath="Assets/Game/Documentation/InfinityRoom5Demo/Room5_Preview.png")


if __name__ == "__main__":
    stage = sys.argv[1] if len(sys.argv) > 1 else "all"
    if stage == "create":
        create_scene()
    else:
        info = call("scene_get_info")
        if info.get("sceneName") != SCENE_NAME:
            raise RuntimeError("Open the new Crimson Cabaret demo scene first.")
        for name, function in [("materials", materials), ("hierarchy", hierarchy),
                               ("models", import_models), ("lighting", colliders_and_lights),
                               ("player", player_and_preview)]:
            if stage in [name, "all"]:
                function()
