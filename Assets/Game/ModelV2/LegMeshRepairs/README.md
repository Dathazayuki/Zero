# Repaired trouser-leg meshes

Sources remain unchanged in `Assets/TripoModels`:

- `human_figure_3d_model_1`: removed 118 bridge faces, capped 4 small loops.
- `human_character_3d_model`: removed 105 bridge faces, capped 3 small loops.
- `human_3d_model`: removed 59 bridge faces, capped 2 small loops.

The edits remove triangles spanning the midline below the knee, smooth the new
rim locally and transfer opposite-leg weights to the corresponding same-side
leg/foot bones below that height. Crotch/upper body geometry and existing UVs
remain unchanged. Closed holes wholly on one leg are capped; no cap spans both legs.

`*_LegsFixed.fbx` are editable repaired copies. `*_LegsFixed.asset` are Unity meshes
converted to the original renderer coordinate system, bone index order and original
bindposes. The three scene instances use these mesh assets only: their original
skeleton, Avatar, Animator, controllers, transforms and materials remain in place.
Blender bone-axis differences are not copied into the scene rig.

`*_spread_before.png` / `*_spread_final.png` compare an opposing-leg pose.
`*_audit.json` records source geometry and repair counts. Test coverage is in
`Tests/EditMode/LegMeshRepairTests.cs` (bindposes, UVs, mesh size, remaining bridge
faces and weight sums). Temporary Blender/Unity repair tools were removed.

Scene changes require manual Ctrl+S. To undo the repair, assign the original FBX
mesh to the instance's SkinnedMeshRenderer; the source FBXs were not overwritten.
