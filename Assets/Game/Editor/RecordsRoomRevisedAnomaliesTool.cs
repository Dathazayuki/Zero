using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zero.AnomalyPrototype.Anomalies;
using Zero.AnomalyPrototype.Demo;

namespace Zero.Editor
{
    public static class RecordsRoomRevisedAnomaliesTool
    {
        private const string SceneName = "InfinityRoom3_RecordsRoom_Demo";

        [MenuItem("Tools/Zero/Records Room/Use Physics For Falling Files 03")]
        private static void MigratePhysics()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.name != SceneName || EditorApplication.isPlayingOrWillChangePlaymode) return;
            Transform room = Root(scene);
            ConfigureFallingFiles(Require(room, "IR3_Anomalies/IR3_03_ProtrudingBinder"),
                Require(room, "IR3_Furniture/IR3_DetailedFiles"));
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("IR3_FILES_PHYSICS_AUTHORED: five colliders and rigidbodies; save scene manually", room);
        }

        [MenuItem("Tools/Zero/Records Room/Update Anomalies 03 09 14 16")]
        private static void Build()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.name != SceneName || EditorApplication.isPlayingOrWillChangePlaymode) return;
            Transform room = Root(scene);
            Transform root = Require(room, "IR3_Anomalies");
            Transform files = Require(room, "IR3_Furniture/IR3_DetailedFiles");
            Light lamp = Require(room, "IR3_Lighting/IR3_DeskLampLight").GetComponent<Light>();
            Require(room, "IR3_Architecture/IR3_Ceiling_Slab");
            Material smile = RecordsRoomBloodMarkAssets.Material("Smile");
            Material circle = RecordsRoomBloodMarkAssets.Material("SatanCircle");
            var preview = root.GetComponent<LivingRoomAnomalyPreview>();
            var entries = new List<AnomalyBehaviour>(preview.Anomalies);
            FallingFiles(root, files, entries);
            Smiles(root, files, smile);
            Add(entries, Lamp(root, lamp));
            Add(entries, Ceiling(root, room, circle));
            entries.Sort((a, b) => string.CompareOrdinal(a.Data.Id, b.Data.Id));
            RecordsRoomHorrorTool.SetRoster(preview, entries);
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = root.gameObject;
            Debug.Log("IR3_REVISED_AUTHORED: 03/09/14/16 configured; F3 roster=" + entries.Count + "; save scene manually", root);
        }

        private static Transform Root(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects()) if (root.name == SceneName) return root.transform;
            throw new InvalidOperationException("IR3_ROOM_MISSING");
        }

        private static Transform Require(Transform parent, string path)
        {
            Transform target = parent.Find(path);
            if (target == null) throw new InvalidOperationException("IR3_REVISED_TARGET_MISSING: " + path);
            return target;
        }

        private static void Add(List<AnomalyBehaviour> entries, AnomalyBehaviour anomaly)
        {
            if (!entries.Contains(anomaly)) entries.Add(anomaly);
        }

        private static Transform Node(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            return existing != null ? existing : RecordsRoomHorrorAssets.Node(parent, name).transform;
        }

        private static void FallingFiles(Transform root, Transform files, List<AnomalyBehaviour> entries)
        {
            Transform node = Require(root, "IR3_03_ProtrudingBinder");
            var old = node.GetComponent<LivingRoomArrangementAnomaly>();
            if (old != null)
            {
                old.ResetAnomaly();
                entries.Remove(old);
                Undo.DestroyObjectImmediate(old);
            }
            Add(entries, ConfigureFallingFiles(node, files));
        }

        internal static ArchiveFallingFilesAnomaly ConfigureFallingFiles(Transform node, Transform files)
        {
            var anomaly = node.GetComponent<ArchiveFallingFilesAnomaly>();
            if (anomaly == null) anomaly = Undo.AddComponent<ArchiveFallingFilesAnomaly>(node.gameObject);
            var targets = new Transform[5];
            var bodies = new Rigidbody[5];
            for (int i = 0; i < targets.Length; i++)
            {
                targets[i] = Require(files, "IR3_File_A_L3_S0_B" + i);
                bodies[i] = PhysicsFile(targets[i]);
                Transform obsolete = node.Find("Landing_" + i);
                if (obsolete != null) Undo.DestroyObjectImmediate(obsolete.gameObject);
            }
            Undo.RecordObject(anomaly, "Configure falling files");
            anomaly.Configure(targets, bodies);
            Data(anomaly, 3, "Files Fall From The Shelf", "Files fall from shelf A using rigidbody gravity and collisions.");
            return anomaly;
        }

        private static Rigidbody PhysicsFile(Transform file)
        {
            var collider = file.GetComponent<BoxCollider>();
            if (collider == null) collider = Undo.AddComponent<BoxCollider>(file.gameObject);
            Undo.RecordObject(collider, "Enable file collision");
            collider.enabled = true;
            collider.isTrigger = false;
            var body = file.GetComponent<Rigidbody>();
            if (body == null) body = Undo.AddComponent<Rigidbody>(file.gameObject);
            Undo.RecordObjects(new UnityEngine.Object[] { body, file.gameObject }, "Configure file physics");
            GameObjectUtility.SetStaticEditorFlags(file.gameObject, 0);
            body.mass = 0.4f;
            body.isKinematic = true;
            body.useGravity = false;
            body.detectCollisions = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            return body;
        }

        private static void Smiles(Transform root, Transform files, Material material)
        {
            Transform node = Require(root, "IR3_09_ClosedDossiers");
            ConfigureSmiles(node, files, material);
        }

        internal static LivingRoomArrangementAnomaly ConfigureSmiles(Transform node, Transform files, Material material)
        {
            Transform old = node.Find("ClosedFileStamps");
            if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
            Transform existing = node.Find("BloodSmiles");
            if (existing != null) Undo.DestroyObjectImmediate(existing.gameObject);
            Transform marks = Node(node, "BloodSmiles");
            foreach (Transform file in files) Smile(file, marks, material);
            marks.gameObject.SetActive(false);
            var anomaly = node.GetComponent<LivingRoomArrangementAnomaly>();
            if (anomaly == null) anomaly = Undo.AddComponent<LivingRoomArrangementAnomaly>(node.gameObject);
            Undo.RecordObject(anomaly, "Replace dossier text with blood smiles");
            anomaly.Configure(Array.Empty<Transform>(), Array.Empty<Vector3>(), Array.Empty<Vector3>(), new[] { marks.gameObject }, Array.Empty<GameObject>());
            Data(anomaly, 9, "Blood Smiles On The Files", "Scribbled blood smiles appear on archive file spines.");
            return anomaly;
        }

        private static void Smile(Transform file, Transform parent, Material material)
        {
            if (!file.name.StartsWith("IR3_File_") && !file.name.StartsWith("tailieuketu")) return;
            Renderer renderer = file.GetComponent<Renderer>();
            if (renderer == null || !renderer.enabled || !file.gameObject.activeInHierarchy) return;
            Bounds bounds = renderer.bounds;
            for (int side = -1; side <= 1; side += 2)
            {
                float size = Mathf.Min(bounds.size.y * 0.7f, bounds.size.z * 0.85f);
                GameObject mark = RecordsRoomMarkAssets.Quad(parent, "Smile_" + file.name + "_" + side, material, Vector2.one * size);
                Vector3 position = new Vector3(bounds.center.x + side * (bounds.extents.x + 0.004f), bounds.min.y + size * 0.8f + 0.012f, bounds.center.z);
                float tilt = (file.GetSiblingIndex() % 5 - 2) * 7f;
                mark.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, side < 0 ? 90f : 270f, tilt));
            }
        }

        private static AnomalyBehaviour Lamp(Transform root, Light light)
        {
            Transform node = Node(root, "IR3_14_DeskLampFlicker");
            var anomaly = node.GetComponent<DeskLampFlickerAnomaly>();
            if (anomaly == null) anomaly = Undo.AddComponent<DeskLampFlickerAnomaly>(node.gameObject);
            Undo.RecordObject(anomaly, "Configure flickering desk lamp");
            anomaly.Configure(light);
            Data(anomaly, 14, "The Desk Lamp Flickers", "The desk lamp repeatedly flickers with an irregular rhythm.");
            return anomaly;
        }

        private static AnomalyBehaviour Ceiling(Transform root, Transform room, Material material)
        {
            Transform node = Node(root, "IR3_16_CeilingPentagram");
            Transform mark = node.Find("SatanCircle");
            if (mark == null) mark = RecordsRoomMarkAssets.Quad(node, "SatanCircle", material, Vector2.one * 2.8f).transform;
            Bounds ceiling = Require(room, "IR3_Architecture/IR3_Ceiling_Slab").GetComponent<Renderer>().bounds;
            Undo.RecordObject(mark, "Place blood circle on ceiling");
            mark.SetPositionAndRotation(new Vector3(0f, ceiling.min.y - 0.008f, 2.8f), Quaternion.Euler(-90f, 0f, 0f));
            mark.gameObject.SetActive(false);
            var anomaly = node.GetComponent<LivingRoomArrangementAnomaly>();
            if (anomaly == null) anomaly = Undo.AddComponent<LivingRoomArrangementAnomaly>(node.gameObject);
            Undo.RecordObject(anomaly, "Configure ceiling circle visibility");
            anomaly.Configure(Array.Empty<Transform>(), Array.Empty<Vector3>(), Array.Empty<Vector3>(), new[] { mark.gameObject }, Array.Empty<GameObject>());
            Data(anomaly, 16, "Blood Pentagram On The Ceiling", "A blood-drawn circle and five-pointed star appear on the ceiling.");
            return anomaly;
        }

        private static void Data(AnomalyBehaviour anomaly, int index, string title, string description)
        {
            RecordsRoomHorrorTool.Data(anomaly, index, title, description);
            Undo.RecordObject(anomaly.Data, "Update archive anomaly definition");
            var settings = new SerializedObject(anomaly.Data);
            settings.FindProperty("displayName").stringValue = title;
            settings.FindProperty("description").stringValue = description;
            settings.FindProperty("pacingTier").enumValueIndex = (int)AnomalyPacingTier.MinorAnomaly;
            settings.FindProperty("canKillPlayer").boolValue = false;
            settings.ApplyModifiedProperties();
            AssetDatabase.SaveAssetIfDirty(anomaly.Data);
        }
    }
}
