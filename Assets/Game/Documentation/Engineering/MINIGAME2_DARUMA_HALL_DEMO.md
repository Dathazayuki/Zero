# Minigame 2 — Daruma Hall environment demo

Scene: `Assets/Game/Scenes/Demos/Minigame2_DarumaHall_Demo.unity`.

Save the authored scene manually with Ctrl+S before closing Unity. Play to explore
with WASD, mouse look and Shift. This standalone demo now has Daruma gameplay.
Cross the start stripe to begin the configured countdown (currently 60 seconds in
the authored scene). Move while the monster faces away; stop when it finishes
turning toward you. The turn and red wall lamps give advance warning; variable
turns never take less than 0.45 seconds. Camera rotation alone is safe. Horizontal displacement
over 0.06 m from the start of a watching phase counts as movement (small steps
accumulate, so creeping does not bypass detection).

Being detected or running out of time teleports the monster in front of the Player
using the authored Catch Distance, locks input for 0.8 seconds, fades to black, and resets the Player,
monster and timer to the entrance. Reach the finish stripe alive to emit
`Completed` once. No campaign readiness or transition is changed.

Select `DarumaHallDemo` > `DarumaSession` in the Inspector to adjust Time Limit,
Move Seconds, Watch Seconds, Turn Seconds, Movement Tolerance,
Catch Distance and Catch Hold Seconds. Restart Play Mode to apply rule timing
changes. `DarumaTimerDisplay` drives the existing physical seven-segment bars.

### Variable rhythm

`Randomize Timing` is enabled by default. Existing authored Move/Watch/Turn Seconds
remain the base durations; each phase samples `Timing Multiplier Range` once on
entry (default 0.65–1.45), never while already turning or watching. Both turning
directions sample independently, so rotation speed varies between turns. Running
and watching phases have a 0.7-second minimum; turns have a 0.45-second minimum.

`Finish Pressure` defaults to 0.25. Based on normalized progress from entrance to
finish, the next movement/turn phase becomes up to 25% shorter and watching up to
25% longer. Set this to 0 for random timing without progress-based difficulty.
Retries continue the random sequence instead of replaying the same rhythm. Rule
tests inject a fixed seed; runtime seeds once when the session initializes.
Disable `Randomize Timing` to restore the original fixed-duration cycle.

Wall lamps turn red as soon as the monster starts turning toward the Player.
Movement is still safe during that warning; detection begins only after the turn
finishes. The timer, movement tolerance, catch/reset and exit rules are unchanged.

## Layout

- Room: approximately 12 m wide, 82 m long, 4.6 m high.
- Player start: `(0, 0.03, -1.1)`, facing the far platform.
- Start stripe: `z = 4`; finish stripe/marker: `z = 74`: exactly 70 m of gameplay distance.
- Mannequin pivot: `(0.75, 0.6, 76.7)`; exit backdrop near `z = 78.7`.
- Sixteen wall lamps (eight per side) use `DarumaSignalLamp`: red while turning to watch, watching
  or during a catch, authored warm color otherwise. Diffuser emission changes
  with the light; reset/disable restores the original colors and property blocks.
  No shared material is modified. The Player camera far clip is 120 m.
- Eight additional countdown displays use the same `PF_DH_TimerDisplay` prefab,
  mounted in facing pairs at `z = 10, 30, 50, 70`, height 2.1 m. They are grouped
  under `DH_WallTimers`. Each display's `Source` references `DarumaHallDemo`'s
  `DarumaSession`; all read the same remaining time and follow attempt resets.
- Three low steps lead to the raised platform. The central route stays clear.

Hierarchy under `DarumaHallDemo`: `DH_Geometry`, `DH_Stage`, `DH_Lighting`,
`DH_Props`, `DH_Actors`, `DH_Markers`. Objects belonging to a repeated prop are
children of its prefab root.

## Shared assets

`Assets/Game/Prefabs/DarumaHallDemo/` contains shared floor bay, solid wall bay,
vent grille, wall lamp, maintenance panel and timer display prefabs. Repeated
walls and fixtures use prefab instances. The floor bay derives from the existing
Calling Corridor floor module; the fluorescent fixture and demo Player are reused.
The monster is `Assets/Game/Prefabs/MINIGAME/5/Mannequin.prefab` with root motion
disabled on this scene instance.

Dedicated grey floor/ceiling materials are in `Materials/DarumaHallDemo`.
Existing source materials remain shared for the other surfaces and props.

## Validation

Scene validation found no missing scripts or prefabs. An editor physics box cast
representing a standing Player found no obstruction along the central route to
the finish area. A Play Mode smoke check confirmed the Player initialized with
input enabled, the mannequin stayed on its platform, and no exception was logged.
`DarumaRulesTests` covers the stop/go cycle, movement tolerance, timeout,
idempotent reset and duplicate completion. `DarumaGameplayTests` covers the
configured scene, camera-only movement, teleport/reset, completion and disabling
during a catch. Save the authored scene before running the scene tests.
No packages, Build Settings or campaign readiness changes are required.
