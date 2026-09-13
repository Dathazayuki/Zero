# Office Loop setup and replacement guide

`OfficeLoopScene` is a functional greybox used for both the opening and ending.
It is intentionally built from primitives so production assets can be replaced
without changing the gameplay flow.

## Safe editing rules

- Open `Assets/Game/Scenes/OfficeLoopScene.unity` directly.
- Do not run any environment Scene Builder.
- Keep the root names `OfficeSequence`, `Player`, `InteractiveProps`, `Lighting`
  and `UI`; the validator treats them as the scene contract.
- Keep `OfficeLoopTransitionTrigger` behind the stairwell door.
- Replace meshes below existing wrapper objects instead of deleting scripts or
  trigger objects.

## Asset replacement order

1. Replace walls, floor and ceiling while preserving the current walkable route.
2. Replace `DeskTop`, chair, monitor, keyboard, phone and filing cabinet meshes.
3. Replace the placeholder elevator doors or place the existing elevator FBX
   behind them after checking its scale and collider.
4. Replace fluorescent fixture meshes but retain their Light components.
5. Assign production clips to `OfficeAudioController`. If a field is empty, the
   controller generates a small procedural fallback at runtime.
6. Edit `ComputerDisplay` for final localized copy. It is a World Space Canvas;
   no RenderTexture is required.

## Cinematic editing

- Opening Timeline: `Assets/Game/Timelines/Office/OfficeOpening.playable`.
- Ending Timeline: `Assets/Game/Timelines/Office/OfficeEnding.playable`.
- Move the Cinemachine cameras under `OfficeSequence/TimelineCameras`; do not
  animate the gameplay camera directly.
- Opening control is returned at 26 seconds at `OpeningControlPoint`.
- Ending remains input-locked and exposes only `R` to restart and `Escape` to exit.

## Validation

Run `Zero > Office Loop > Validate Opening And Ending` and expect:

```text
OFFICE_LOOP_VALIDATION_PASS
```

Play from `OfficeLoopScene` to test the complete opening. In Editor or a
Development Build, press `F9` in `InfiniteStairwayScene` to test the ending
transition without completing eight rounds manually.
