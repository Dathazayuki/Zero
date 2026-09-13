# Old Film Dust setup (URP 17)

1. Create a Material named `M_OldFilmDust` and choose shader `Zero/Old Film Dust URP`.
2. Find the URP asset assigned by the current Quality level (or Graphics settings if no override).
3. Open its renderer used by Main Camera. Add `Full Screen Pass Renderer Feature` once.
4. Set Pass Material to `M_OldFilmDust`, Injection Point to `After Rendering Post Processing`,
   Fetch Color Buffer on, Requirements None, Pass Index 0, Bind Depth Stencil off.
   Fetch Color Buffer / Pass Index may be under the feature's Advanced Properties menu.
5. Keep the existing color-grading Volume. No textures, mesh, camera component, or animation script are required.

The installed URP 17 implementation skips Reflection cameras. PlanarMirrorRenderer sets its
camera to CameraType.Reflection, so the film effect is applied to the final view, not inside
the mirror texture. Other Game cameras sharing this renderer will also receive the effect.
Use a separate renderer for those cameras if needed. Screen Space Overlay UI is drawn later.

Default values are subtle. For a quick visibility check use Effect Intensity 1, Dust Density 0.3,
Dust Opacity 0.7, Dust Radius 3, Scratch Chance 1 and Scratch Opacity 0.6; restore the defaults
after checking. Set Effect Intensity to 0 to compare with the original image. Set Brightness
Flicker to 0 to disable brightness modulation. Disable the renderer feature to avoid its render cost.

Marks update using Unity's scaled time. They can stop during pause, or in Scene view when
Animated Materials is disabled. Test in Play Mode. Radius and width are relative to 1080p.
This shader adds dust and scratches, not grain, sepia, blur, or camera shake.

Checks in Unity: confirm no shader errors; see changing small marks in Play Mode; Intensity 0
restores the source; test 16:9 and 4:3; check a mirror for double application; verify the effect
after switching Quality levels. No prefab, scene, material, or renderer asset is modified by
the code addition; all asset setup is manual.
