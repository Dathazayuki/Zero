# Title and checkpoint Unity setup

> Campaign checkpoint v2 supersedes the old current-floor checkpoint policy below.
> Continue now resumes at the current Infinity Room's floor 0 or minigame entrance,
> with encounter history retained. See [Campaign setup](CAMPAIGN_FRAMEWORK_UNITY_SETUP.md)
> for definitions, migration, scene drafts and readiness requirements.

The Title menu, Settings panels, exit confirmation, and their serialized references are
configured in `TitleScene`. Do not run Scene Builder or apply scene-wide prefab overrides.

## TitleScene

1. Open `Assets/Game/Scenes/TitleScene.unity` to review the configured menu.
2. Keep the static office backdrop, `TitleCamera`, lighting, and Canvas. Do not add a
   Player or a second persistent `GameFlowController`; the existing bootstrap creates it.
3. Use a Screen Space Overlay Canvas with `CanvasScaler` set to Scale With Screen Size,
   reference resolution 1920 x 1080, and matching 0.5. Add an `EventSystem` with
   `InputSystemUIInputModule`.
4. `TitleMenuController` is wired to New Game, Continue, Settings, Exit,
   `TitleMainPanel`, `TitleSettingsPanel`, and `TitleExitPanel`. Settings and Exit panels
   are inactive by default. Back, Confirm Exit, and Cancel Exit are also assigned.
5. `TitleGraphicsSettingsPanel` is wired to resolution and quality dropdowns plus
   fullscreen and VSync toggles.
6. `TitleAudioSettingsPanel` stays active on the Canvas root and is wired to
   `MX_ZeroGame` plus the Master, Music, SFX, and Ambience sliders. Saved volumes are
   therefore applied even if the user does not open Settings.
7. Use UGUI Button Navigation/explicit selectable links so New Game, Continue,
   Settings, Exit, sliders, toggles, and Back work with mouse and gamepad/keyboard.
   Continue is disabled by `TitleMenuController` when no valid checkpoint exists.

## Mixer

`Assets/Game/Audio/MX_ZeroGame.mixer` contains Master, Music, SFX, and Ambience groups.
Their Volume controls are exposed as `MasterVolume`, `MusicVolume`, `SfxVolume`, and
`AmbienceVolume`. Route gameplay AudioSources to the appropriate group; until a source
is routed, its group slider cannot affect it. Do not rename the exposed parameters
without changing `GameSettingsService`.

## Build Settings and checkpoint marker

1. Add `TitleScene` as the first enabled scene in Build Profiles/Build Settings. Keep
   `OfficeLoopScene`, `InfiniteStairwayScene`, and `BackRoomScene` enabled afterward.
   Campaign additionally registers the two reviewed `Scenes/Campaign/Minigame*.unity`
   scenes; original BackRoomScene remains available during migration.
2. In `InfiniteStairwayScene`, add a `LoopResumePoint` child to the physical floor used
   as the initial current segment. Place it at a safe location facing into the room,
   outside all commit trigger volumes. Without it, Continue uses the existing
   `LoopEntrySpawnPoint` and logs `CHECKPOINT_RESUME_MARKER_MISSING`.
3. In `BackRoomScene`, verify `BackroomGameplayManager.Respawn Point` is the safe marker
   at the destination elevator, and that its NavMeshSurface and chase trigger are set.
4. Save scenes manually. Do not run Scene Builder.

## Smoke test

Run `Tools > Zero > Code Policy > Validate Changed Code`, then EditMode and PlayMode
tests. In a build, check: New Game goes to Office opening; initial room-entry checkpoint
appears; commit updates encounter history but never saves current floor/progress or pose.
Continue starts at the current room's floor 0 as pure Normal, with history retained.
Successful minigame handoff replaces the checkpoint with that minigame's entrance;
Continue starts there with its challenge waiting. Verify graphics and mixer settings
persist after restarting the application.
