using System;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Zero.AnomalyPrototype.Anomalies;
using Zero.AnomalyPrototype.Demo;
using Zero.AnomalyPrototype.Player;

namespace Zero.Editor
{
    public static class RecordsRoomFinalEncounterTool
    {
        private const string RoomName = "InfinityRoom3_RecordsRoom_Demo";

        [MenuItem("Tools/Zero/Records Room/Preview Headless Crawl")]
        private static void PreviewCrawl()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.name != RoomName || EditorApplication.isPlayingOrWillChangePlaymode) return;
            Transform node = Require(SceneRoot(scene, RoomName), "IR3_Anomalies/IR3_18_BodyAssembly");
            node.GetComponent<ArchiveAssemblyPresentation>().Capture();
            Animator actor = Require(node, "HeadlessEmployee").GetComponent<Animator>();
            Undo.RecordObjects(actor.GetComponentsInChildren<Transform>(true), "Preview crawl pose");
            actor.gameObject.SetActive(true);
            actor.enabled = true;
            actor.Rebind();
            actor.Play(Animator.StringToHash("Running Crawl"), 0, 0.45f);
            actor.Update(0f);
            actor.enabled = false;
            Require(node, "FloorSatanCircle").gameObject.SetActive(true);
            EditorSceneManager.MarkSceneDirty(scene);
        }

        [MenuItem("Tools/Zero/Records Room/Restore Encounter Preview")]
        private static void RestorePreview()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.name != RoomName || EditorApplication.isPlayingOrWillChangePlaymode) return;
            Transform node = Require(SceneRoot(scene, RoomName), "IR3_Anomalies/IR3_18_BodyAssembly");
            node.GetComponent<ArchiveAssemblyPresentation>().Restore();
            EditorSceneManager.MarkSceneDirty(scene);
        }

        [MenuItem("Tools/Zero/Records Room/Add Encounters 17 18")]
        private static void Build()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.name != RoomName || EditorApplication.isPlayingOrWillChangePlaymode) return;
            Transform room = SceneRoot(scene, RoomName);
            Transform root = Require(room, "IR3_Anomalies");
            Transform player = SceneRoot(scene, "IR3_DemoPlayer");
            Camera camera = Require(player, "IR3_PreviewCamera").GetComponent<Camera>();
            PlayerDeathController death = player.GetComponent<PlayerDeathController>();
            if (death == null) throw new InvalidOperationException("IR3_DEATH_CONTROLLER_MISSING");
            Transform source = SceneRoot(scene, "vintage_woman_3d_model");
            Require(room, "IR3_Furniture/IR3_Cabinet1 (5)");
            Require(room, "IR3_Furniture/IR3_Cabinet1 (8)");
            var drawers = Require(root, "IR3_13_BodyDrawers").GetComponent<ArchiveBodyDrawersAnomaly>();
            var preview = root.GetComponent<LivingRoomAnomalyPreview>();
            var entries = new List<AnomalyBehaviour>(preview.Anomalies);
            Add(entries, Crush(root, room, camera, death));
            Add(entries, Assembly(root, source, drawers, camera, death));
            Bake(room);
            entries.Sort((a, b) => string.CompareOrdinal(a.Data.Id, b.Data.Id));
            RecordsRoomHorrorTool.SetRoster(preview, entries);
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = root.gameObject;
            Debug.Log("IR3_FINAL_ENCOUNTERS_AUTHORED: 17/18; NavMesh baked; save scene manually", root);
        }

        private static Transform SceneRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects()) if (root.name == name) return root.transform;
            throw new InvalidOperationException("IR3_ROOT_MISSING: " + name);
        }

        private static Transform Require(Transform parent, string path)
        {
            Transform result = parent.Find(path);
            if (result == null) throw new InvalidOperationException("IR3_ENCOUNTER_TARGET_MISSING: " + path);
            return result;
        }

        private static void Add(List<AnomalyBehaviour> entries, AnomalyBehaviour anomaly)
        {
            if (!entries.Contains(anomaly)) entries.Add(anomaly);
        }

        private static AnomalyBehaviour Crush(Transform root, Transform room, Camera camera, PlayerDeathController death)
        {
            Transform moving = Require(room, "IR3_Furniture/IR3_Cabinet1 (5)");
            Transform warning = Require(room, "IR3_Furniture/IR3_Cabinet1 (8)");
            ClearStatic(moving);
            ClearStatic(warning);
            Transform existing = root.Find("IR3_17_CabinetCrush");
            if (existing != null)
            {
                var current = existing.GetComponent<ArchiveCabinetCrushAnomaly>();
                Data(current, 17, current.Data.DisplayName, current.Data.Description);
                return current;
            }
            Bounds bounds = BoundsOf(moving);
            var strike = Undo.AddComponent<BoxCollider>(moving.gameObject);
            strike.center = moving.InverseTransformPoint(bounds.center);
            strike.size = new Vector3(bounds.size.x / moving.lossyScale.x, bounds.size.y / moving.lossyScale.y, bounds.size.z / moving.lossyScale.z);
            strike.isTrigger = true;
            var body = moving.GetComponent<Rigidbody>();
            if (body == null) body = Undo.AddComponent<Rigidbody>(moving.gameObject);
            body.isKinematic = true;
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            Transform node = RecordsRoomHorrorAssets.Node(root, "IR3_17_CabinetCrush").transform;
            AnomalyDeathPresentation view = View(node, 0.8f);
            var anomaly = Undo.AddComponent<ArchiveCabinetCrushAnomaly>(node.gameObject);
            anomaly.Configure(strike, warning, camera, death, view);
            Data(anomaly, 17, "The Filing Cabinet Crushes", "Cabinet 8 shakes; cabinet 5 shoots horizontally towards it and crushes anyone in its path.");
            return anomaly;
        }

        private static void ClearStatic(Transform root)
        {
            foreach (Transform part in root.GetComponentsInChildren<Transform>(true))
            {
                if (GameObjectUtility.GetStaticEditorFlags(part.gameObject) == 0) continue;
                Undo.RecordObject(part.gameObject, "Make moving cabinet dynamic geometry");
                GameObjectUtility.SetStaticEditorFlags(part.gameObject, 0);
            }
        }

        private static Bounds BoundsOf(Transform root)
        {
            Bounds result = default;
            bool first = true;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
            {
                if (!renderer.enabled) continue;
                if (first) { result = renderer.bounds; first = false; }
                else result.Encapsulate(renderer.bounds);
            }
            return result;
        }

        private static AnomalyBehaviour Assembly(Transform root, Transform source, ArchiveBodyDrawersAnomaly drawers,
            Camera camera, PlayerDeathController death)
        {
            Transform existing = root.Find("IR3_18_BodyAssembly");
            Transform node = existing != null ? existing : RecordsRoomHorrorAssets.Node(root, "IR3_18_BodyAssembly").transform;
            ArchiveAssemblyPresentation body = RecordsRoomFinalEncounterAssets.Body(node, source, drawers);
            AnomalyDeathPresentation view = node.GetComponent<AnomalyDeathPresentation>();
            if (view == null) view = View(node, 2.8f);
            else view.SetFreezeDuration(2.8f);
            var anomaly = node.GetComponent<ArchiveAssemblyAnomaly>();
            if (anomaly == null) anomaly = Undo.AddComponent<ArchiveAssemblyAnomaly>(node.gameObject);
            anomaly.Configure(body, camera, death, view);
            Data(anomaly, 18, "The Archive Reassembles Its Employee", "Labelled body parts crawl into the floor blood circle, join into a headless employee and pursue until the Player leaves the floor.");
            return anomaly;
        }

        private static AnomalyDeathPresentation View(Transform parent, float duration)
        {
            var view = Undo.AddComponent<AnomalyDeathPresentation>(parent.gameObject);
            view.SetDeathStyle(AnomalyDeathStyle.Style2_FreezeBlackout);
            view.SetFreezeDuration(duration);
            return view;
        }

        private static void Data(AnomalyBehaviour anomaly, int id, string title, string description)
        {
            RecordsRoomHorrorTool.Data(anomaly, id, title, description);
            var data = new SerializedObject(anomaly.Data);
            data.FindProperty("displayName").stringValue = title;
            data.FindProperty("description").stringValue = description;
            data.FindProperty("canKillPlayer").boolValue = true;
            data.FindProperty("pacingTier").enumValueIndex = (int)AnomalyPacingTier.MajorAnomaly;
            data.FindProperty("deathStyle").enumValueIndex = (int)AnomalyDeathStyle.Style2_FreezeBlackout;
            data.FindProperty("difficulty").intValue = 3;
            data.FindProperty("type").enumValueIndex = (int)(id == 17 ? AnomalyType.Environment : AnomalyType.Creature);
            data.ApplyModifiedProperties();
            AssetDatabase.SaveAssetIfDirty(anomaly.Data);
        }

        private static void Bake(Transform room)
        {
            var surface = room.GetComponent<NavMeshSurface>();
            if (surface == null) surface = Undo.AddComponent<NavMeshSurface>(room.gameObject);
            Undo.RecordObject(surface, "Bake Records Room navigation");
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.overrideVoxelSize = true;
            surface.voxelSize = 0.06f;
            surface.BuildNavMesh();
            if (surface.navMeshData == null) throw new InvalidOperationException("IR3_NAVIGATION_BAKE_FAILED");
            string path = RecordsRoomHorrorAssets.Folder + "/IR3_18_Navigation.asset";
            NavMeshData generated = surface.navMeshData;
            NavMeshData existing = AssetDatabase.LoadAssetAtPath<NavMeshData>(path);
            if (existing == null) AssetDatabase.CreateAsset(generated, path);
            else
            {
                surface.RemoveData();
                EditorUtility.CopySerialized(generated, existing);
                surface.navMeshData = existing;
                surface.AddData();
                EditorUtility.SetDirty(existing);
                AssetDatabase.SaveAssetIfDirty(existing);
                UnityEngine.Object.DestroyImmediate(generated);
            }
        }
    }
}
