# Cyberpunk City — material repair

The converted FBX referenced `textures/packed/Image_*` beneath the old Downloads
folder. The project textures are named `gltf_embedded_*`, so all extracted Unity
material texture slots were empty. Mapping and material factors were recovered
from the original `scene.glb` and checked against the FBX connections.

| Material | Base-color image | Emission strength |
|---|---:|---:|
| Material.007 | 0 | 1.9 |
| Material.001 | 1 | 1.5 |
| advertising_screens_texture_01 | 3 | 0 |
| Material.003 | 6 | 0 |
| Material.009 | 7 | 0 |
| Material.004 | 8 | 0 |
| Material.008 | 9 | 1.9 |
| Material.005 | 10 | 0 |
| Material.002 | 11 | 2 |

Image 4 is the road normal and is now imported as NormalMap. Two generated
`*_URP_MetallicSmoothness.png` textures pack metallic into R and **1 - roughness**
into A, from the existing B/G channel images 2 and 5. These masks are linear,
not sRGB. The corresponding URP keywords are enabled. Base-color tint is white
for textured materials; double-sided culling and metallic factors follow the GLB.

Material GUIDs, FBX geometry, material remaps, transforms and scenes are unchanged.
No render pipeline/project settings or packages were modified. No scene was saved.
The static repair menu/tool was removed after use.

`MaterialBackup/*.mat.before-texture-fix` keeps the original material text before
the repair. Restore by copying a backup's contents to its matching `.mat`, keeping
the existing `.mat.meta` file/GUID, then reimport. These backups are not scene materials.

Regression asset checks: `Tests/EditMode/CyberpunkMaterialTextureTests.cs`.
