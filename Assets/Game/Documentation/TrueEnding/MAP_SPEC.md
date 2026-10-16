# Map specification

## Overall route

The route is intentionally linear because the game is a walking simulation. Difficulty comes from player behavior rather than pathfinding.

| Area | Approx. footprint | Purpose |
|---|---:|---|
| Room 1 | 28 m x 6 m | Do not look backward |
| Connector 1 | 4 m x 4 m | Reset pacing between mechanics |
| Room 2 | 34 m x 8 m | Move only while lights are OFF |
| Connector 2 | 6 m x 4 m | Visual transition from Backrooms to Blue Sky |
| Blue Sky | 36 m x 26 m | Staircase and Ending B cutscene |

## Object generation

Read each CSV row and instantiate based on `object_type` and `prefab`.

- `WALL`: place a rectangular wall centered at the supplied position and scale to `size_x_m, size_y_m, size_z_m`.
- `FLOOR`: generate a floor plane/cube with the given footprint.
- `TRIGGER` / `ZONE`: create an invisible BoxCollider with `isTrigger = true`; attach behavior according to `behavior_id`.
- `SPAWN` / `MARKER`: create an empty Transform.
- `LIGHT`: create or instantiate a ceiling-light prefab and register it with the Room 2 controller.
- `BOUNDARY`: invisible collider used only in Blue Sky.
- `STAIR`: blockout stair step. It may later be replaced by a stair mesh while preserving the same route.
- `PLATFORM`: final top platform.
- `CAMERA`: Cinemachine camera anchor.

## Wall conventions

Wall height: 3.0 m  
Wall thickness: 0.20 m

## Player route

1. Enter Room 1 at approximately `(2, 0, 3)`.
2. Cross `R1_ACTIVATE`; from here, turning backward can reset the room.
3. Cross `R1_COMPLETE` to permanently finish Room 1 for this run.
4. Enter Room 2 and cross `R2_ACTIVATE`.
5. Move during OFF phases and stop during ON phases.
6. Cross `R2_COMPLETE`.
7. `BLUESKY_ENTER` changes ambience and presentation.
8. Walk up the stairs.
9. `BLUESKY_ENDING` locks controls and begins the final bow/camera sequence.
