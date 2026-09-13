# Zero - CorrectArrow Dynamic Environment Loop

## Core rule

Every active `FloorSegment` owns a locked Room round with a random `CorrectArrow` direction and a 55% anomaly roll.

```text
Normal Room  -> follow CorrectArrow
Anomaly Room -> take the opposite direction from CorrectArrow
```

Truth table:

```text
Normal  + Arrow UP   -> choose UP
Normal  + Arrow DOWN -> choose DOWN
Anomaly + Arrow UP   -> choose DOWN
Anomaly + Arrow DOWN -> choose UP
```

The Room state is prepared when its segment becomes active and never rerolls when the segment becomes Current.

## Floor number progress

`Floor_Number` contains display objects `0` through `8` (names `Number0` through `Number8` are also accepted).

```text
Correct decision -> increase by 1, clamped at 8
Wrong decision   -> the next floor displays 0
```

This value is consecutive-correct progress and is independent from `LogicalFloorIndex`.

## Dynamic segment pool

- The configured pool contains at least nine `PF_FloorSegment` instances.
- Exactly five are active: `Below2 -> Below1 -> Current -> Above1 -> Above2`.
- All remaining segments are disabled, reset reserves.
- A commit keeps only the adjacent target, acquires four inactive reserves, releases the other four members of the old active window, then builds two new Below and two new Above positions.
- The segment immediately behind the Player is always a different physical instance from the floor just left.
- There is no runtime `Instantiate`, `Destroy`, Player teleport, fade, or scene reload.
- `SegmentCommitTrigger` owns the complete transition. The old recycle-trigger system has been removed.

Each returned segment resets transforms, anomaly state, CorrectArrow, audio, particles, animators, rigidbodies, and commit triggers before entering the inactive pool.

## Runtime debug

- `F1`: toggle debug panel.
- `F2`: force the current Room to Normal without changing its Arrow direction.
- `F3`: force an Anomaly without changing its Arrow direction.

The panel displays current Arrow, expected direction, anomaly, Number progress, all five active roles, active/inactive/total pool counts, and active segment order.

## Rebuild and validation

- `Tools > Environment Loop > Install or Repair Room Anomalies`
- `Tools > Environment Loop > Create or Rebuild Complete Scene`
- `Tools > Environment Loop > Run Environment Stress Validation`

The builder reuses `Assets/Game/Prefabs/Environment/PF_FloorSegment.prefab`, preserving the handmade Room and environment geometry.

The validator covers the eight Arrow/Anomaly truth-table cases, Number `0 -> 8` and wrong reset, the five-role active window, pools 9/12, inactive baseline state, target-only retention, physical replacement behind the Player, 100 UP, 100 DOWN, 200 direction changes, stable pooled identities, anchor alignment, and unchanged Player transform.
