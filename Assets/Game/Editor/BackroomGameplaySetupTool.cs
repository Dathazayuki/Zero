using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using Zero.AnomalyPrototype.Flow;

namespace Zero.EditorTools
{
    public static class BackroomGameplaySetupTool
    {
        [MenuItem("Tools/Zero/Setup Backroom Monster Chase Gameplay")]
        public static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogError("Exit Play Mode before setup."); return; }
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!scene.name.Equals("BackRoomScene", StringComparison.OrdinalIgnoreCase))
            { Debug.LogError("Open BackRoomScene before setup. No changes made."); return; }
            var all = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
            Transform rootTransform = Unique(all, "BackRoomRoot");
            Transform arrival = Unique(all, "BackRoomArrivalAnchor");
            Transform exit = Unique(all, "GT_Exit_Door_Zone");
            if (rootTransform == null || arrival == null || exit == null)
            { Debug.LogError("BACKROOM_SETUP_ABORTED: requires unique BackRoomRoot, BackRoomArrivalAnchor and GT_Exit_Door_Zone; scene unchanged."); return; }
            var candidates = all.Where(t => t.GetComponent<Animator>() != null &&
                (t.name == "HorrorGirrl" || t.name.Contains("The_Tattered_Wraith") ||
                (t.GetComponent<Animator>().runtimeAnimatorController != null &&
                 t.GetComponent<Animator>().runtimeAnimatorController.name.Contains("The_Tattered_Wraith")))).ToArray();
            if (candidates.Length != 1)
            { Debug.LogError($"BACKROOM_SETUP_ABORTED: expected one Wraith/HorrorGirrl Animator, found {candidates.Length}. Scene unchanged."); return; }
            Undo.IncrementCurrentGroup();
            int undo = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Setup Backroom Monster Chase Gameplay");
            try
            {
                Transform creature = candidates[0];
                bool freshMonster = creature.GetComponent<BackroomMonsterController>() == null;
                if (freshMonster && !creature.IsChildOf(rootTransform)) Undo.SetTransformParent(creature, rootTransform, "Parent Wraith preserving world pose");
                var surface = rootTransform.GetComponent<NavMeshSurface>();
                if (surface == null)
                {
                    surface = Undo.AddComponent<NavMeshSurface>(rootTransform.gameObject);
                    surface.collectObjects = CollectObjects.Children;
                    surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
                }
                var agent = creature.GetComponent<NavMeshAgent>();
                if (agent == null)
                {
                    agent = Undo.AddComponent<NavMeshAgent>(creature.gameObject);
                    agent.agentTypeID = surface.agentTypeID;
                    agent.radius = 0.35f;
                    agent.height = 2f;
                    agent.speed = 4.6f;
                    agent.acceleration = 16f;
                    agent.angularSpeed = 360f;
                    agent.stoppingDistance = 0.2f;
                    agent.enabled = false;
                }
                var monster = GetOrAdd<BackroomMonsterController>(creature.gameObject);
                Transform systems = Child(rootTransform, "BackroomChaseGameplay", rootTransform.position, rootTransform.rotation);
                var manager = GetOrAdd<BackroomGameplayManager>(systems.gameObject);
                // Use the artist's monster placement, never an unchecked coordinate outside this map.
                Transform spawn = Child(systems, "MonsterSpawnPoint", creature.position, creature.rotation);
                Transform focus = Child(creature, "ChaseFocusTarget", creature.position + Vector3.up * 1.7f, creature.rotation);
                Vector3 forward = Vector3.ProjectOnPlane(-arrival.forward, Vector3.up).normalized;
                Quaternion facing = Quaternion.LookRotation(forward, Vector3.up);
                Transform respawn = Child(systems, "ChaseRespawnPoint", arrival.position + forward * 1.5f, facing);
                Transform start = Child(systems, "BackroomChaseStart", arrival.position + forward * 3.5f + Vector3.up, facing);
                SetupVolume(start.gameObject, new Vector3(2.5f, 2.5f, 0.5f));
                var startTrigger = GetOrAdd<BackroomChaseStartTrigger>(start.gameObject);
                Transform end = Child(systems, "BackroomChaseEscape", exit.position + Vector3.up, exit.rotation);
                SetupVolume(end.gameObject, new Vector3(2.5f, 2.5f, 1f));
                var escapeTrigger = GetOrAdd<BackroomEscapeTrigger>(end.gameObject);
                AssignIfEmpty(monster, "spawnPoint", spawn);
                AssignIfEmpty(monster, "animator", creature.GetComponent<Animator>());
                AssignIfEmpty(monster, "focusTarget", focus);
                AssignIfEmpty(manager, "surface", surface);
                AssignIfEmpty(manager, "monster", monster);
                AssignIfEmpty(manager, "respawnPoint", respawn);
                AssignIfEmpty(manager, "startTrigger", startTrigger);
                AssignIfEmpty(startTrigger, "manager", manager);
                AssignIfEmpty(escapeTrigger, "manager", manager);
                EditorSceneManager.MarkSceneDirty(scene);
                Undo.CollapseUndoOperations(undo);
                Debug.Log("BACKROOM_SETUP_COMPLETE: no scene saved, no NavMesh baked, existing fields/transforms preserved. Review spawn/respawn/start/escape volumes, then Ctrl+S. Undo is available.", manager);
                Selection.activeGameObject = systems.gameObject;
            }
            catch (Exception exception)
            {
                Undo.RevertAllDownToGroup(undo);
                Debug.LogException(exception);
            }
        }

        private static Transform Unique(Transform[] all, string name)
        {
            var matches = all.Where(t => t.name.Equals(name, StringComparison.OrdinalIgnoreCase)).ToArray();
            return matches.Length == 1 ? matches[0] : null;
        }
        private static T GetOrAdd<T>(GameObject target) where T : Component => target.GetComponent<T>() ?? Undo.AddComponent<T>(target);
        private static Transform Child(Transform parent, string name, Vector3 position, Quaternion rotation)
        {
            Transform existing = parent.Find(name);
            if (existing != null) return existing;
            var created = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(created, "Create " + name);
            created.transform.SetParent(parent, false);
            created.transform.SetPositionAndRotation(position, rotation);
            return created.transform;
        }
        private static void SetupVolume(GameObject target, Vector3 size)
        {
            if (target.GetComponent<BoxCollider>() == null)
            {
                var collider = Undo.AddComponent<BoxCollider>(target);
                collider.isTrigger = true;
                collider.size = size;
            }
            if (target.GetComponent<Rigidbody>() == null)
            {
                var body = Undo.AddComponent<Rigidbody>(target);
                body.isKinematic = true;
                body.useGravity = false;
            }
        }
        private static void AssignIfEmpty(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(field);
            if (property == null || property.objectReferenceValue != null) return;
            property.objectReferenceValue = value;
            serialized.ApplyModifiedProperties();
        }
    }
}
