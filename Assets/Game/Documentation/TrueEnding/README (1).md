# Ending B package — Unity / Codex

This package describes a linear walking-simulation branch:

`Secret corridor B -> Room 1 (Don't Look Back) -> Room 2 (Move only in darkness) -> Blue Sky staircase ending`

## Files

- `endingB_topdown_map.drawio` — editable top-down blockout map.
- `endingB_topdown_map_preview.png` — quick map preview.
- `endingB_map_objects.csv` — one row per Unity object/trigger/light/stair block.
- `endingB_map.json` — canonical map + mechanic configuration. Prefer this as the main machine-readable source.
- `MAP_SPEC.md` — dimensions, coordinate system, and object rules.
- `MECHANICS_SETUP.md` — Room 1, Room 2, reset, and Blue Sky ending setup.
- `CODEX_TASK.md` — ready-to-use instructions for Codex.
- `Scripts/*.cs` — implementation templates.

## Source of truth

Use `endingB_map.json` as the canonical behavior/config source. Use the CSV for procedural object placement. The Draw.io file is for human review and layout editing.

## Unity coordinate convention

- X: progression through Ending B.
- Y: vertical height.
- Z: lateral direction.
- Units: meters.

Do not rotate the whole map unless the generator applies the same root transform to every object.
