# Infinity Room 4 — The Hotel Kitchen

Room 4's campaign theme is **The Hotel Kitchen**: a back-of-house service kitchen in an old hotel, not a family kitchen. It is a single continuous, enclosed room—not a set of smaller rooms—and keeps the campaign's neglected, warm-but-uneasy interior direction.

The active concept shows a commercial hotel service kitchen with stainless prep counters, a heavy range and extraction hood, deep sink, commercial refrigerator, pot racks, service trays and a central prep island. It should not read as a guest dining room or domestic kitchen. Keep a traversable route through the single room when using this as a blockout reference. The Room 4 minigame remains **The Long Hall**; no gameplay or readiness flags are changed here.

## Concept

`InfinityRoom4_HotelKitchen_Concept.png`

The earlier `InfinityRoom4_FamilyRoom_Concept.png` and `InfinityRoom4_Kitchen_Concept.png` are retained as superseded references; neither is the active Room 4 direction.

## Provenance and status

- Generated with the built-in image generation tool as a visual concept reference.
- The concept is a reference, not production geometry. A standalone scene demo now exists (see below).
- Room 4 remains unready until its room prefab and anomaly roster are authored and reviewed.

## Unity scene demo

Scene: `Assets/Game/Scenes/Demos/InfinityRoom4_HotelKitchen_Demo.unity`.

**Authoring is currently in the open Unity Editor scene. Press Ctrl+S before closing
Unity or switching scenes.** The REST authoring never saves or overwrites scenes
automatically. The initial empty scene asset alone does not contain unsaved edits.

The demo is one enclosed commercial kitchen, approximately 8 x 10 m and 3.5 m high.
It contains a FirstPersonController for inspecting the room in Play Mode:
WASD/mouse and the existing controller's sprint input. It has no campaign bootstrap,
anomaly selection, navigation, transition triggers or gameplay dependencies.

### Authored details

- Stainless counters: separate doors, drawer seams, handles, hinges and feet.
- Six-burner commercial range: grates, burner rings, controls and two oven windows.
- Extraction hood: underside baffle strips, duct and task-light strip.
- Double sink: recessed basins, drains, gooseneck faucet and tap handles.
- Two-door commercial refrigerator: long handles, ventilation slats and small display.
- Central prep island: tubular legs, adjustable feet, lower shelf, open trays and pots.
- Pot racks, hanging ladles, plate stacks, utensil crock, labeled jars and chopping board.
- Service trolley: three open tray shelves, frame, wheels and handles.
- Folded towels, cloth draped over the island, ceiling vent and service pipes.
- Caged pendant, warm task lighting, dark window, tile geometry and weathered surfaces.

Models are material-grouped OBJ assets under `Models/InfinityRoom4Demo`; small
details do not each require a separate scene object. Dedicated materials are under
`Materials/InfinityRoom4Demo`. Stainless and wood textures reuse existing project
assets; wall/floor weathering reuses the existing EnvironmentLoop concrete texture.
No source textures or existing materials were modified.

### Hierarchy / editing

```text
InfinityRoom4_HotelKitchen_Demo
  IR4_Architecture   -- shell colliders and window
  IR4_Equipment      -- imported equipment and invisible collision proxies
  IR4_Props          -- movable visual detail groups
  IR4_Lighting      -- five local lights, only one shadow-casting light
  IR4_Markers       -- safe inspection spawn marker
IR4_DemoPlayer
  IR4_PreviewCamera
IR4_ReflectionProbe -- 128px, realtime OnAwake, individual-face time slicing
```

The geometry is a detailed demo, not final production art: equipment bevels,
unique PBR wear maps, window frost, baked lighting and anomaly-specific changes
remain authoring work. Reflection capture is not updated every frame.

Preview: [Room4_Preview.png](Room4_Preview.png),
[equipment angle](Room4_EquipmentView.png).

### Verification and safety

- UnitySkills Play Mode observation: 5 seconds, completed, healthy, zero runtime errors.
- Camera renders checked from the doorway and a side angle.
- Continuous floor/wall colliders and equipment proxy colliders are present.
- Full player traversal/collision QA remains a manual check after saving.
- No Scene Builder, runtime script changes, project settings changes or campaign
  readiness changes. Existing scenes and room prefabs were not authored by this demo.

The three `author_kitchen_*.py` scripts are explicit offline/REST authoring tools;
they do not run inside Unity or on compile/reload. **Do not rerun scene placement
stages over an already-authored scene**, since that would add duplicate objects.
No tool rebuilds, clears or saves a scene. Keep the authored scene as the source
for further manual editing.
