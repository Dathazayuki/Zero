# Zero Architecture Contract

## Project tier

Long-lived small game. Prefer explicit composition and a few directional modules over a
large reusable framework.

## Current safe boundary

Phase 1 uses one runtime assembly because Player, EnvironmentLoop, Anomalies, and Flow
currently contain circular source dependencies:

```text
Zero.Game.Runtime
      ^       ^
Editor tools  Tests
```

Editor code may depend on Runtime. Runtime never depends on Editor or Tests.

## Target boundary

After cycles are removed, dependencies become:

```text
Zero.Core <- Zero.Gameplay <- Zero.Flow
    ^             ^             ^
    +------------- Tests -------+
```

- `Zero.Core`: contracts, value types, clocks/random abstractions, and pure shared rules.
- `Zero.Gameplay`: Player, Interaction, Environment Loop, and Anomalies.
- `Zero.Flow`: Office, Backroom, Ending, and scene transitions.
- `Zero.Editor`: explicit setup and validation tools.
- EditMode/PlayMode test assemblies reference only the modules under test.

Do not split Anomalies and EnvironmentLoop into separate assemblies while they depend on
each other. Do not create a `Common`, `Helpers`, or `Utils` dumping-ground; every reusable
type must have a clear owner.

## Composition and communication

- Scene/bootstrap code wires dependencies once.
- `MonoBehaviour` adapters translate Unity callbacks into commands on owned rules.
- Domain state does not find scene objects and does not manipulate camera/audio/UI.
- Presentation observes events or read-only state.
- One-to-one operations use direct references. One-to-many notifications use C# events
  with subscription symmetry. Interfaces are narrow and justified by a boundary.

## Runtime state ownership

| State | Owner |
|---|---|
| Cross-scene phase and loading | `GameFlowController` |
| Floor topology and recycling | `StairLoopManager` |
| Encounter/pacing decision | `DirectionalAnomalyGameManager` and extracted rule objects |
| Player movement/input | `FirstPersonController` |
| Death sequence | `PlayerDeathController` until orchestration is extracted |
| Individual anomaly lifecycle | Its `AnomalyBehaviour` instance |
| Campaign stage and entrance phase | `CampaignProgress`, commanded by `CampaignRuntime` |
| Additive campaign destination preparation | `CampaignSceneTransition` and destination `CampaignLevelBootstrap` |
| Per-room encountered anomaly coverage | `EncounterDiversityHistory`, recorded only on current encounters |
| Local minigame attempt | Its gameplay controller through `IMinigameSession` |

Only the owner writes state. Reset commands flow toward owners; observers do not repair
another system's fields directly.

## Bootstrap and execution order

Prefer one explicit bootstrap per scene/flow. Initialization uses `Configure/Initialize`
and guards all later entry points. `DefaultExecutionOrder` is reserved for the persistent
flow/bootstrap layer and must not become a substitute for dependency injection.

## Debt migration order

1. Isolate `AnomalyDemoBootstrap` from production runtime.
2. Extract pacing, direction, and encounter rules from `DirectionalAnomalyGameManager`.
3. Separate death orchestration from death presentation.
4. Separate pool topology and ending route from `StairLoopManager`.
5. Reduce `GameFlowController` scene-specific responsibilities.
6. Split anomaly state/rules from movement and presentation when each large anomaly is
   next changed.

Each migration preserves serialized fields and gameplay behaviour. Behaviour changes are
reviewed as separate tasks.
