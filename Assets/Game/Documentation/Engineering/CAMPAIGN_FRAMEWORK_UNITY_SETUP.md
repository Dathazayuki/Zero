# Campaign framework: 5 Infinity Rooms + 5 Minigames

Hướng dẫn từng bước bằng tiếng Việt cho nội dung mới:
[README — Unity Campaign Authoring](README_CAMPAIGN_AUTHORING.md).

## Scope and readiness

This is a small-game framework, not a finished five-stage campaign. It preserves the
original scenes, geometry, anomaly behaviour and three ending presentations. Nothing
creates new room content, auto-wins a placeholder or saves/rebuilds a scene automatically.

| Stage | Infinity Room direction | Minigame direction | Initial readiness |
|---|---|---|---|
| 1 | The Gallery: existing Room and its authored roster | Existing Backroom Chase | Room ready; minigame requires draft review/save |
| 2 | The Guest Room | Existing Don't Look Back + Lights OFF | Room placeholder; minigame requires draft review/save |
| 3 | The Records Room | The Calling Corridor — visual dossier-code memory and door matching | Both placeholders |
| 4 | The Hotel Kitchen | The Watchful Hall — hide, charge four pads, unlock the exit | Both placeholders |
| 5 | The Crimson Cabaret (red performance stage in an old bar) | The Flooded Cistern — mannequin-gaze chase | Both placeholders |

Themes are changeable authoring suggestions, not gameplay requirements. A total budget
of 112 anomalies is a planning target only (for example 24 + 22 + 22 + 22 + 22);
there is no runtime minimum or generated content. Do not promise infinite non-repetition.

### Minigame 3 — The Calling Corridor (visual-only code memory)

At the start of each round, a lit index board displays one archive code or a short ordered
list of codes. The board closes before the corridor; the player walks through each doorway
whose complete metal plaque matches the current code. Route clues and success/failure
feedback are visual; audio never identifies a route or reveals a code. Door thresholds
trigger automatically; no interaction prompt is required.

| Round | Content | Difficulty step |
|---|---|---|
| 1 | One short code; six shared doors | Safe introduction; one exact match |
| 2 | A longer code; the same six doors | Near-match decoys differ by one field |
| 3 | A two-code sequence; the same six doors and corridor | Remember both codes and their order |
| 4 | A three-code sequence; the same six doors and corridor | Final test; the exact-match rule stays unchanged |

There is no countdown. A wrong door loops back to the beginning of the current round and
replays its code board; earlier rounds stay complete. Use legible high-contrast lettering,
avoid ambiguous characters such as `O/0` and `I/1`, and let every clue be read at normal
walking distance. There are only six physical choice doors (three per side), reused for
all seven decisions. Plaques stay fixed within a round and change between rounds. Enter
the matching pocket and return physically to the shared corridor to confirm each step;
there is no correct-route teleport or wall dividing the corridor into separate rounds.
The separate exit stays locked until all four rounds are solved; crossing it completes
the minigame exactly once.

Round 1 is safe. From round 2, two wrong choices in that round trigger one mannequin
encounter; the first correct choice in rounds 3 and 4 also triggers an authored encounter.
Visual warning precedes pursuit. Reach a marked refuge and wait until the mannequin
passes. Being caught resets the entire minigame to round 1; a wrong code only resets its
current round. Neither resets the completed Records Room. Target duration is 6–8 minutes,
not yet measured. The standalone demo remains unpublished (Content Ready = false).
See [Calling Corridor demo setup](CALLING_CORRIDOR_DEMO.md).

### Minigame 4 — The Watchful Hall (four-pad exit unlock)

The Player crosses a large service/industrial area watched by a giant creature whose
elevated position lets it survey broad sections of the map. The creature periodically
sweeps its gaze across the route, then turns away for a readable movement window. The
Player must hide in authored cabinets or solid safe alcoves during a sweep, then run to the
next cover while the creature is looking elsewhere. The objective is to activate four charge
pads distributed across this one map, unlock the exit, then reach it.

#### Exit lock and charge pads

- The exit starts locked and displays four red lock symbols. Each symbol corresponds to
  exactly one of the four pads; author a stable pad ID and a matching visible symbol/number.
  Reaching the exit before all four are active does not open it or complete the minigame.
- All four pads start red and uncharged. The Player activates a pad by standing on it for
  15 seconds. A visible energy bar fills from empty to full while the Player is on that pad;
  the bar measures charge progress, not a deadline for completing the level.
- At full charge, that pad becomes green and its corresponding exit symbol changes from
  red to green. Completed pads stay active for the rest of the attempt, even when the Player
  leaves them, and cannot add completion twice.
- The four pads may be activated in any order. Only when all four pads and all four exit
  symbols are green does the exit unlock. Passing the unlocked exit completes the minigame
  exactly once; finishing the fourth charge alone does not complete it.
- Stand still for 1.5 seconds before charging starts; this settling time is additional to
  the 15-second charge. Moving or leaving pauses charging but retains partial progress.
  Returning or stopping movement requires another uninterrupted 1.5-second settle.
  Looking around without moving does not interrupt charging.
- Being caught resets the attempt, clears all four pad charges/activations, returns all
  symbols to red and locks the exit again. Completed Infinity Rooms remain complete.

Place the pads in distinct parts of the hall so reaching the exit requires exploring both
sides of the map. The first pad teaches the energy bar in a readable, low-pressure position;
later pads require moving between cover and choosing when to stand exposed. Give each pad
nearby accessible cover and a readable route onward. Allow useful charging windows after
the 1.5-second settle, with time to retreat; partial progress allows multiple visits.
Use authored sightline protection or readable look-away windows, not unavoidable detection.

- Use an authored array of elevated Watchers (two initially in the demo) and two ground
  Patrols. Each Watcher walks between two clear points on its gallery. Its `mixamorig:Neck`
  scans independently of body turning at route endpoints; the eye spotlight follows the head.
  Solid walls and cover block line of sight. After 1.5 seconds of continuous sight it
  reports the last-seen position to the closest reachable Patrol by NavMesh path distance.
  Each Watcher's further reports update its own responder while sight is maintained; losing sight ends
  updates. Patrols without an alert continue their own work unless they independently see a Player.
- Patrols follow authored routes, chase visible Players, investigate the last-seen position
  after losing sight, search nearby for eight seconds, then resume their routes. A radio
  report does not grant wall-penetrating vision or override a Patrol's direct visual chase.
- A fully occupied cabinet hides the Player from distant sight, but is not invulnerability:
  a Patrol within 3 m of its accessible entrance can inspect it, approach the entrance and
  catch the occupant within 1.4 m. A solid wall blocking sight to the entrance prevents
  remote capture. Alcoves rely on physical cover, not a global safe-zone flag.
- Telegraph scan direction and phase with body/head orientation, a moving light/shadow, or
  another clear visual cue. A caught Player gets a short, fair warning/chase response before
  reset; do not teleport the monster onto the Player.
- Patrol capture plays an attack, briefly locks input and fades to black, then returns the
  Player to the entrance and resets all four charges, gate and enemies. The Watcher never
  teleports onto the Player. The Player wins by charging all four pads and passing the exit.
- Teach one complete hide/sweep/run cycle from a safe starting position. Then vary cover
  spacing and route choices; keep the next safe spot visible or clearly signposted from
  each decision point. Do not require crossing an exposed distance longer than the creature's
  telegraphed look-away window can reasonably support.
- Catch resets this minigame attempt to its entrance checkpoint, resets creature state and
  all pad/exit-lock state. No overall countdown or audio-only clue is required.

Recommended first-pass AI states: `Surveying → LookAwayWindow → Chase → Search → Surveying`.
Tune patrol speed, scan duration and cover spacing in a greybox before adding art. The goal
is route planning and holding charge positions under threat. Four successful charges alone
require 60 seconds of charging plus at least 6 seconds of settling; travel, hiding and
repeated settling after interruptions add to that time.
The earlier 3–5 minute target needs a new playtest against this objective and the widened
60 × 84 m hall; it is not a measured completion time.

`Minigame4_WatchfulHall_Demo` now has four charge pads, progress bars and a locked exit
controlled by `WatchfulHallSession`, with Watcher/Patrol AI and capture/reset coordinated by
`WatchfulHallThreatDirector`. The current implementation is a standalone single-player demo;
network authority, multiple Player targets and campaign readiness are not implemented.
`ResetAttempt()` clears all four charges, closes the gate and resets enemy state.
See [Watchful Hall demo authoring](WATCHFUL_HALL_DEMO.md).

### Minigame 5 — The Flooded Cistern (mannequin-gaze chase)

The minigame takes place in a modern underground cistern with standing water, low broad concrete
arches and square support piers clad in small white ceramic tiles; it is not a medieval hall.
Lighting is dim neutral grey with a small warm exit glow, never saturated blue/violet. The critical
path leads to a staircase at the center, marked by a persistent overhead light. Water reflects
the sparse practical lights without obscuring floor edges or safe routes. Mannequins stand
between the columns, along the banks and in alcoves; each has a local activation zone.
Entering that zone arms the mannequin and gives a brief, readable visual tell before pursuit.

- An active mannequin freezes only while the Player can clearly see it: it is inside the
  camera view and has unobstructed line of sight. Turning away or losing sight behind a
  corner lets it move again. Looking at one threat does not freeze other active mannequins.
- Decorative mannequins outside activation zones remain inert. Stage activations along the
  critical route so the player learns the rule before overlapping threats are introduced.
- Use the column rows and arch bays to create readable sightline breaks and alternate exposed
  crossings with brief cover. Keep the lit center staircase visible from major junctions;
  water dressing must not hide floor edges or silently slow the Player.
- Reaching the staircase exit completes the minigame and stops all chases. Being caught
  resets the attempt to its entrance checkpoint, returns mannequins to authored positions
  and rearms their zones; it never resets completed Infinity Rooms.
- Audio may support atmosphere and pursuit feedback, but the gaze rule, activation and safe
  direction must be communicated visually. No timer is required for the first version.

Difficulty should rise by encounter composition, not by changing the rule: first one
mannequin in an open sightline, then staggered activation zones around bends, then a final
approach where the Player must manage two threats while navigating to the lit stairs. Build
and test the blockout before adding detailed water, props or mannequin dressing. Target a
short, replayable 3–5 minute run; tune the catch/reset loop in playtests.

Environment demo: `Assets/Game/Scenes/Demos/Minigame5_FloodedCistern_Demo.unity`.
See [Flooded Cistern demo authoring](FLOODED_CISTERN_DEMO.md) for water, reflection baking
and preview controls. The demo does not implement chase or promote Minigame5 readiness.

## Architecture and ownership

- `CampaignDefinition`, `InfinityRoomDefinition`, `MinigameDefinition`: immutable authored
  references, stable IDs and explicit readiness flags.
- `CampaignProgress`: pure C# stage/phase rule. Completing a minigame permits a next
  stage, but only a successfully prepared destination commits that stage.
- `CampaignRuntime`: composition and completion subscriptions on the existing persistent
  GameFlow object. No new singleton or event bus.
- `CampaignRoomBootstrap`: definition/roster injection before pool initialization.
- `CampaignLevelBootstrap`, `CampaignSceneTransition`: additive loading, anchor alignment,
  readiness, actual-Player adoption and source unload behind closed elevator doors.
- `IMinigameSession`: preparation, begin/reset and completion notification. Existing
  chase/challenge adapters implement this contract.
- `EncounterDiversityHistory`: only encounters of the current room; retained across wrong
  decisions, death and Continue. Cleared on New Game and successful new-room handoff.

The loop still has three active segments: current, one above, one below. A pool of five
physical segments provides recycling capacity. Room completion still requires the correct
final decision at number 8; reaching 8 alone does not finish the room or campaign.

## Explicit Unity workflow

Do not run Scene Builder. Save your working scenes yourself before entering Play Mode.

1. Run `Tools > Zero > Campaign > Create Definitions` once.
   Existing assets are preserved. Assets appear in `Assets/Game/ScriptableObjects/Campaign`.
   The current prefab has 24 slots but slot 16 has no usable reference/Data, so 23 valid
   entries were imported. No prefab was repaired or changed. Assign the missing anomaly
   manually if appropriate, then run `Sync Room1 Roster From Prefab` to refresh this asset.
2. Open TitleScene and run `Bind Active Scene Flow`. It adds/binds CampaignRuntime to
   GameFlow using Undo, leaves the scene dirty and does NOT save it. Save manually.
   Do not assign a different CampaignDefinition to other duplicate scene GameFlow objects.
3. Run `Prepare Minigame Scene Drafts`. This creates NEW copies under
   `Assets/Game/Scenes/Campaign`, opens them additively and configures only those copies.
   The original BackRoomScene remains unchanged. Re-running preserves existing drafts.
4. Inspect and manually save each draft:
   - `Minigame1_Backroom`: chase only; Ending3_Root is disabled. Check the NavMesh surface,
     Horror Girl spawn, start/escape trigger and respawn marker. Bake navigation manually
     if necessary. Keep the original chase audio and catch/reset presentation.
   - `Minigame2_Rooms`: Room1/Room2 challenge cluster only. Original chase geometry/systems
     are removed from THIS COPY. Blue Sky is disabled. Verify both rooms, lights, cues,
     trigger references and a safe entrance immediately before Room1_StartTrigger.
   - Both: CampaignLevelBootstrap must reference the environment root, arrival anchor,
     safe entrance marker and matching session. No active placeholder Player/camera.
5. Only after review/save, run `Publish Saved Minigames` with both drafts open. It rejects
   dirty scenes, enables the definition readiness flags and registers these new scenes
   in Build Settings. This menu saves definition assets, never a scene/prefab.
6. Run `Validate Definitions`. Unready placeholders are reported and are NOT skipped.

Changing TitleScene only in memory does not survive an Editor restart. Drafts on disk
initially contain the raw source copy until you save their in-memory configuration.

## Authoring subsequent Infinity Rooms

1. Create your own segment prefab with the existing FloorSegment anchors, one floor
   commit trigger, Number lifecycle and RoomAnomalyController contract.
2. Populate its anomaly slots and assign the SAME data assets to the room definition's
   roster. A roster does not manufacture missing anomaly components on a prefab.
3. Assign prefab, difficulty and `contentReady`. Leave `useAuthoredScenePool` OFF for
   newly authored rooms. It is ON only for the existing Room1 scene pool.
4. The shared InfiniteStairwayScene needs ONE CampaignLevelBootstrap for return trips:
   - `environmentRoot`: a parent containing all loop geometry/managers/anchors, not Player.
   - `building`: the existing InfiniteBuildingManager.
   - `arrivalAnchor`: a marker under that root at the destination elevator threshold,
     forward facing into the incoming cabin (opposite the cabin anchor forward).
   - `entranceCheckpoint`: safe marker at current floor 0, outside commit/kill volumes.
   - `destinationFacade`: optional authored facade that is hidden when the incoming cabin
     occupies the threshold. Do not overlap incoming cabin/Player with the original stairs.
   - No minigame session is needed for an Infinity Room destination.
5. Do not include Player in environmentRoot. The destination scene's Player is disabled
   and the actual travelling Player is rebound to the newly initialized loop/death context.
6. Review alignment and collisions in Play Mode before declaring this scene contract ready.

No additional room geometry or arrival elevator facade is generated by this delivery.
Without these authored references, a subsequent-room load fails safely and reopens the
source cabin instead of opening into unprepared geometry.

## Minigame exits and new minigames

Author an exit elevator using the existing EndFloorElevatorController door/trigger contract.
Assign it to CampaignLevelBootstrap.exitElevator. It stays Dormant until the session emits
Completed. The next InfinityRoom must be marked ready before a ride can start.

Implement `IMinigameSession` on a small adapter for new gameplay:

```csharp
bool IsReady { get; }
IEnumerator Prepare();
void Begin(MinigameContext context);
void ResetAttempt();
event Action Completed;
```

Use `context.Player`, never a scene placeholder. Keep fail/reset local. Clean up camera,
audio, agents, event subscriptions and coroutines on disable/reset. Raise completion once;
the campaign also gates duplicates. Validate readiness before publishing a definition.

Minigame2 fail in either room resets BOTH challenges and re-arms their triggers at the
shared entrance. Room2 tutorial runs once per session; retry skips it, Continue creates
a new session. Room2 completion no longer starts Blue Sky in minigame mode.

## Three ending presentations

Only CampaignProgress.FinalEnding (after Minigame5) permits ending presentation.
Existing early white-door and ending callbacks are rejected while the campaign is active.
No new ending-choice geometry is authored here.

On the future final hub, add a trigger collider plus CampaignEndingChoiceTrigger per route:

- TheCast: provide the existing EndingOneCutsceneController presentation in the hub.
- AnotherShift: loads Office ending through GameFlow.
- CurtainCall: assign TrueEndingGameplayController with its existing Blue Sky/ending
  references; entry prepares the safe Blue Sky phase, then its normal entry/platform
  triggers run the bow presentation.

End titles remain `ENDING I — THE CAST`, `ENDING II — ANOTHER SHIFT`,
`TRUE ENDING — CURTAIN CALL`, followed by the existing return-to-Title flow.

## Checkpoint schema 2

JSON stores campaign ID, stage ID, entrance kind and encountered anomaly IDs. Floor index
and displayed number are compatibility fields fixed to zero. No player pose or active
anomaly state is saved. Settings remain separate.

- Infinity Continue rebuilds floor 0 as pure Normal; history is restored before reserve rolls.
- Minigame Continue bootstraps the real Player through the loop scene, loads the selected
  minigame directly and teleports to its entrance. It does not fake an elevator ride.
- Save v1 Stairway migrates in memory to stage-1 Infinity entrance; v1 Backroom to stage-1
  minigame entrance. Schema 2 is written only after a successful restore.
- Unready/unknown stages disable Continue. Atomic temp/backup writing is retained.
- A failed load/readiness check does not promote stage or overwrite the last checkpoint.
- Encounter autosaves update only the history; Continue still starts at that room's entrance.

## Verification checklist

Run EditMode campaign/checkpoint/diversity/selection tests and PlayMode
CampaignMinigameContractTests + CheckpointRuntimeContractTests. Run Code Policy after
source changes; do not raise baseline debt to make it pass.

Manual verification after saving the authored contracts:

- Correct/wrong at number 8; only a correct final entry can select a minigame elevator.
- Closed doors throughout load/alignment; minimum ride >=5s; slow load holds doors closed.
- Source cabin/actual Player survive source unload; no duplicate camera, agent or audio.
- Backroom catch/replay; escape completion once. Minigame2 fail resets both rooms.
- Continue at stage entrance, death preserving anomaly diversity, corrupted save fallback.
- Placeholders block progression with CAMPAIGN_CONTENT_NOT_READY, never auto-win.
- Three ending presentations rejected before Minigame5.

Runtime logs include CAMPAIGN_ROOM_CONFIGURATION_INVALID, CAMPAIGN_CONTENT_NOT_READY,
CAMPAIGN_LEVEL_READY, CAMPAIGN_DESTINATION_READY, CAMPAIGN_MINIGAME_COMPLETED,
CAMPAIGN_LOAD_FAILED, and existing elevator/checkpoint logs.

## Implementation verification (2026-10-02)

- Unity compile checks: no C# compilation errors reported.
- Game EditMode suite: 48/48 passed, including fake five-stage progression, ending-choice
  gating, schema migration, backup safety, diversity and bootstrap dependency binding.
- Game PlayMode suite: 15/15 passed (checkpoint, pause/audio and minigame lifecycle contracts).
- Definitions created and Title flow bound through explicit MCP menu calls. Unity's
  editor journal recorded manual saves of Title and the minigame drafts before publication.
- Minigame1/2 published and included in Build Settings. Room2–5 and Minigame3–5 remain unready.
- Code Policy is NOT a project-wide pass: 13 pre-existing errors remain (runtime UnityEditor
  references and complexity in BackroomMonsterController/FlickeringLightController).
  No campaign module errors were reported and the versioned baseline was not raised.
- Real-play validation of destination geometry, arrival thresholds, exit elevators and the
  future final hub remains an authoring task. These are not fabricated by the framework.
