# Crimson Cabaret — Meshy asset pass

Updated: 2026-10-14. Scene: `Assets/Game/Scenes/Demos/InfinityRoom5_CrimsonCabaret_Demo.unity`.

## Assets and authoring

Three reusable prefabs live in `Assets/Game/Prefabs/InfinityRoom5Demo/`: `PF_IR5_JazzDrumKit`, `PF_IR5_DoubleBass`, `PF_IR5_DisplayMannequin`. Each has a bottom-center wrapper, a static `Visual` child and a simple BoxCollider. Mannequins are display decorations and have no AI, Animator or gameplay callbacks.

Runtime model files are `Assets/Game/Models/Meshy/CrimsonCabaret/*_Game.fbx`. The original dense FBX files remain as source assets; do not use those in scene instances. Meshy metadata, prompts, task snapshots and previews are retained under `meshy_output/` so work can continue without regenerating.

Use the prefabs to place additional copies. Root scale 1 is approximately full size; display figures on plinths use scale 0.75. The visual child preserves the remeshed FBX axis correction (X=270, Y=180) and FBX scale=100. Rotate the prefab root to change facing.

URP materials and 2K texture maps are shared. FBX embedded texture mapping was incorrect after remesh, so prefabs use explicit materials with downloaded base-color, normal and metallic maps. Normal and metallic textures are imported in linear space. Smoothness is a shared constant of 0.3, not a converted roughness-alpha map.

## Layout

The stage platform and collider are about 7.2 m wide and 2.8 m deep, at height 0.24 m. Piano remains on the left, drums in the middle and double bass on the right. Side curtains are gathered left/right with a top valance and dark backdrop. The bar was shortened at the stage end, adjacent seating/drinkware relocated, and stools moved in front of the counter to clear overlap.

Seven mannequin instances reuse one prefab around the walls and plinths. Previous simple figure/bust groups are disabled in the scene for reversible replacement. `IR5_MeshyInstruments` and `IR5_MeshyMannequins` group the new content under `IR5V2_DetailPass`.

## Cost and verification

Per asset: preview 20 + texture refine 10 + remesh 5 = 35 Meshy credits. Total for three assets: **105 credits**. Balance after completion: **1195 credits**. Task IDs and prompts are recorded in `IR5_MESHY_MANIFEST.json`.

Unity reports 35,170 triangles for drums, 14,450 for bass and 12,322 for the mannequin. Scene missing-script/prefab validation returned zero issues. Wide and close camera previews were inspected; no FPS measurement or gameplay test is claimed. Final topology/manifold review and distribution-license review remain production authoring work.

Scene changes are made through UnitySkills with Undo and must be saved manually in Unity. No scene rebuild tool, runtime script or automatic scene save was added.
