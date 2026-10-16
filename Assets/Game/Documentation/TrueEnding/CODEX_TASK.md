# Codex task — build Ending B in Unity

Implement the Ending B branch using the files in this folder.

## Required workflow

1. Read `endingB_map.json` first.
2. Read `endingB_map_objects.csv` and verify it matches the JSON objects.
3. Generate a root object named `EndingB_Root`.
4. Create child roots: `Room1`, `Connector1`, `Room2`, `Connector2`, `BlueSky`.
5. Instantiate/map every CSV object to its corresponding child root.
6. Use primitive cubes if a named prefab does not exist. Do not stop generation because an art prefab is missing.
7. Add trigger colliders and mechanics based on `behavior_id`.
8. Add the scripts in `Scripts/` or implement equivalent behavior if the project already has compatible architecture.
9. Keep gameplay first-person walking only. Do not add interaction buttons, chase enemies, pickups, inventory, or puzzle UI.
10. Add debug gizmos and an optional developer-only overlay for challenge state; keep them disabled in release gameplay.

## Acceptance criteria

### Room 1
- Entering the activation trigger enables the challenge.
- Looking backward beyond the configured threshold resets only Room 1.
- Side glances do not reset.
- Passing the finish trigger completes the challenge and prevents further Room 1 resets.

### Room 2
- All ceiling lights share the configured cycle.
- Player can move while OFF.
- Player receives a brief warning flicker before ON.
- Moving during ON after the grace time resets only Room 2.
- Camera rotation while stationary does not count as movement.
- Passing the finish trigger ends the challenge.

### Blue Sky
- Presentation blends on entry.
- Stair route is walkable.
- At the top trigger, player controls lock, camera blends to ending camera, player faces camera, Bow animation triggers, and the scene fades to white.

## Important

Treat the JSON coordinates as authoritative. If project-specific controller or camera APIs differ, adapt the scripts rather than changing the map layout.
