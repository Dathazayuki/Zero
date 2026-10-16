# Crimson Cabaret — bar correction

- Front counter centre: X -1.85 m; depth scale 0.85. Back display remains at X -3.77 m.
- Clear service aisle between rear display shelf lip and counter rear edge: approximately 0.97 m (shelf lip), 1.2 m below the shelf. Access is around the counter ends; no bartender NPC is added.
- Stools, lamps and counter collision proxy move with the front counter. Two prop trunks are moved to the entrance-side wall so they do not intersect the shifted counter.
- Shared prop: `Assets/Game/Prefabs/InfinityRoom5Demo/PF_IR5_DrinkingGlass.prefab`, six glasses on the bar and two on each of three cafe tables.
- Original mesh: `Models/InfinityRoom5Demo/IR5_DrinkingGlass.obj`, analytically authored for this project, no downloaded assets or external licence requirements. Metre units, Y up, base pivot, hollow 80 mm-wide / 118 mm-tall glass, water insert and coaster. Decorative only: no collider, scripts or animated water.
- Materials: dedicated URP Lit transparent glass/water; existing black material for coasters. No changes to existing shared glass materials.
- Asset validation: inspect the hollow rim, normals, liquid level, table contact and shared-prefab references in Unity. Scene changes use UnitySkills/Undo and are saved manually by the user.
- Verified in Editor: 12 drinkware roots; shared mesh is 486 vertices / 960 triangles. A 0.70 m-wide, 1.70 m-tall box cast along the service aisle from (-2.75, 0.95, -3.2) towards +Z over 5.7 m is clear; the central guest route is also clear in the sampled volume. The far end approaches the existing raised stage, so entry is from the entrance-side counter end. These are Editor geometry checks, not a runtime bartender/navigation test.
