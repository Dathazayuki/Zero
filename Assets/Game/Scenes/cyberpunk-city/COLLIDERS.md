# Cyberpunk City — static collision

The current scene instance `Cyberpunkcity/untitled.glb/ny_times_squareFBX/RootNode`
has 57 non-convex, non-trigger MeshColliders on its structural MeshFilter objects:
ground/roads, building shells, street-level architecture, glazing, interiors and
metal structural groups. Each collider references its object's existing mesh and
inherits its transform, so moving/scaling the city keeps collision aligned.

Five advertising/neon groups were excluded. The imported Blender helper `Cube`
outside RootNode was not changed. Existing colliders were preserved; setup skipped
objects already carrying a Collider. No Rigidbody, runtime script, physics layer,
project setting, model importer, render geometry or material was changed.

The model groups contain geometry spread over several buildings. A single bounds
BoxCollider would close streets/alleys, so exact static mesh collision is used.
This is **1,189,628 collision triangles**, not a simplified collision proxy; no
performance benchmark is claimed. A separate low-poly collision mesh can replace
this later if profiling identifies a physics bottleneck.

Editor verification: 57/57 collider configuration checks and direct triangle-normal
raycasts passed. Ground ray at Player XZ hit `(0, 0, -4.2)` beneath Player feet
`(0, 0.04, -4.2)`; Player was not moved. No PlayMode walkthrough was run.

Temporary authoring tool removed after use. Scene is not automatically saved;
press Ctrl+S to retain these instance collider components. The source FBX importer
is unchanged, so a fresh FBX instance will not automatically receive these colliders.
