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
    public static class LivingRoomAnomalyAuthoringTool
    {
        [MenuItem("Tools/Zero/Living Room/Polish Six Anomaly Presentation")]
        private static void PolishPresentation()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.name != "InfinityRoom2_LivingRoom_Demo") return;
            Transform room = GameObject.Find("InfinityRoom2_LivingRoom_Demo").transform;
            Transform root = Require(room, "IR2_Anomalies");
            Vases(room, Require(root, "IR2_04_InvertedPorcelain").gameObject);
            Transform books = Require(root, "IR2_05_ReversedBooks/ReversedBooksAndOpenBook");
            Transform bookcase = Require(room, "IR2_Furniture/IR2_Bookcase");
            int index = 0;
            foreach (Transform item in books)
            {
                Undo.RecordObject(item, "Align anomaly books within shelf");
                if (item.name.StartsWith("Reversed_"))
                {
                    LayoutBooks(item, bookcase, index++);
                    continue;
                }
                item.position = bookcase.TransformPoint(new Vector3(0f, 1.44f, 0.04f));
            }
            LivingRoomAnomalyProps.DetailPages();
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("LIVING_ROOM_PRESENTATION_POLISHED: books aligned, alternating vases tipped; save scene manually", root);
        }

        [MenuItem("Tools/Zero/Living Room/Add First Six Anomalies To Demo")]
        private static void AddAnomalies()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.name != "InfinityRoom2_LivingRoom_Demo") return;
            Transform room = GameObject.Find("InfinityRoom2_LivingRoom_Demo").transform;
            if (room.Find("IR2_Anomalies") != null)
            {
                Debug.LogWarning("LIVING_ROOM_AUTHORING_SKIPPED: IR2_Anomalies already exists; authored objects preserved");
                return;
            }
            Require(room, "IR2_Decor/IR2_DetailWallClock/ClockHands");
            Require(room, "IR2_Furniture/IR2_Bookcase");
            Require(room, "IR2_Furniture/IR2_DetailArmchairB");
            LivingRoomAnomalyProps.EnsureFolder();
            var root = new GameObject("IR2_Anomalies");
            root.transform.SetParent(room, false);
            Undo.RegisterCreatedObjectUndo(root, "Add six Living Room anomalies");
            var entries = new AnomalyBehaviour[6];
            entries[0] = Clock(room, Node(root.transform, 1, "ReverseClock"));
            entries[1] = Cup(room, Node(root.transform, 2, "FourthTeaCup"));
            entries[2] = Chair(room, Node(root.transform, 3, "ChairFacingWall"));
            entries[3] = Vases(room, Node(root.transform, 4, "InvertedPorcelain"));
            entries[4] = Books(room, Node(root.transform, 5, "ReversedBooks"));
            entries[5] = Shadow(Node(root.transform, 6, "ExtraReaderShadow"));
            var preview = Undo.AddComponent<LivingRoomAnomalyPreview>(root);
            var serialized = new SerializedObject(preview);
            SerializedProperty array = serialized.FindProperty("anomalies");
            array.arraySize = entries.Length;
            for (int i = 0; i < entries.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = entries[i];
            serialized.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = root;
            Debug.Log("LIVING_ROOM_SIX_ANOMALIES_AUTHORED: F3 cycles IR2-01..06, F2 restores baseline; save scene manually", root);
        }

        private static Transform Require(Transform room, string path)
        {
            Transform item = room.Find(path);
            if (item == null) throw new InvalidOperationException("LIVING_ROOM_AUTHORING_MISSING: " + path);
            return item;
        }

        private static GameObject Node(Transform parent, int index, string name)
        {
            var node = new GameObject($"IR2_{index:00}_{name}");
            node.transform.SetParent(parent, false);
            return node;
        }

        internal static void Data(AnomalyBehaviour anomaly, int number, string title,
            AnomalyPacingTier tier = AnomalyPacingTier.UneasyNormal, AnomalyType type = AnomalyType.ObjectChange)
        {
            const string folder = "Assets/Game/ScriptableObjects/Anomalies/InfinityRoom2";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Game/ScriptableObjects/Anomalies", "InfinityRoom2");
            string path = folder + $"/IR2_{number:00}.asset";
            AnomalyData data = AssetDatabase.LoadAssetAtPath<AnomalyData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<AnomalyData>();
                data.Configure($"IR2-{number:00}", title, type, tier, 1, 1f, false,
                    "Living Room preview: F3 selects this anomaly; F2 restores the normal room.");
                AssetDatabase.CreateAsset(data, path);
            }
            anomaly.Initialize(data);
            EditorUtility.SetDirty(anomaly);
        }

        private static AnomalyBehaviour Clock(Transform room, GameObject node)
        {
            Transform clock = Require(room, "IR2_Decor/IR2_DetailWallClock");
            GameObject normal = Require(clock, "ClockHands").gameObject;
            Transform hour = NewHand(clock, "IR2_ReverseHour", 0.10f, -54f);
            Transform minute = NewHand(clock, "IR2_ReverseMinute", 0.145f, 142f);
            var anomaly = Undo.AddComponent<ReverseClockAnomaly>(node);
            anomaly.Configure(normal, hour, minute);
            Data(anomaly, 1, "Clock Runs Backwards");
            return anomaly;
        }

        private static Transform NewHand(Transform clock, string name, float length, float angle)
        {
            var pivot = new GameObject(name);
            pivot.transform.SetParent(clock, false);
            Undo.RegisterCreatedObjectUndo(pivot, "Add reversible clock hand");
            Material ink = AssetDatabase.LoadAssetAtPath<Material>("Assets/Game/Materials/InfinityRoom2Demo/M_IR2Detail_Dark.mat");
            LivingRoomAnomalyProps.Part("Hand", pivot.transform, new Vector3(0f, length * 0.5f, -0.037f), new Vector3(0.005f, length, 0.003f), ink);
            pivot.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            pivot.SetActive(false);
            return pivot.transform;
        }

        private static LivingRoomArrangementAnomaly Variant(GameObject node, GameObject[] show, GameObject[] hide)
        {
            for (int i = 0; i < show.Length; i++) show[i].SetActive(false);
            var anomaly = Undo.AddComponent<LivingRoomArrangementAnomaly>(node);
            anomaly.Configure(Array.Empty<Transform>(), Array.Empty<Vector3>(), Array.Empty<Vector3>(), show, hide);
            return anomaly;
        }

        private static AnomalyBehaviour Cup(Transform room, GameObject node)
        {
            Bounds table = BoundsOf(Require(room, "IR2_Furniture/IR2_CoffeeTable/IR2_CoffeeTable_Top").gameObject);
            var details = new GameObject("FourthCupAndTeaTrail");
            details.transform.SetParent(node.transform, false);
            GameObject cup = LivingRoomAnomalyProps.Cup(details.transform);
            cup.transform.position = new Vector3(table.max.x - 0.10f, table.max.y + 0.004f, table.center.z - 0.55f);
            LivingRoomAnomalyProps.Steam(cup.transform);
            Material stain = LivingRoomAnomalyProps.Material("M_IR2_TeaStain", new Color(0.15f, 0.065f, 0.018f));
            LivingRoomAnomalyProps.Part("SpilledTea", details.transform, cup.transform.position + new Vector3(0.05f, -0.002f, 0f), new Vector3(0.10f, 0.001f, 0.018f), stain);
            LivingRoomAnomalyProps.Part("TeaDownTableEdge", details.transform, new Vector3(table.max.x + 0.001f, table.max.y - 0.05f, cup.transform.position.z), new Vector3(0.001f, 0.10f, 0.014f), stain);
            var anomaly = Variant(node, new[] { details }, Array.Empty<GameObject>());
            Data(anomaly, 2, "The Fourth Tea Cup");
            return anomaly;
        }

        private static AnomalyBehaviour Chair(Transform room, GameObject node)
        {
            Transform visual = Require(room, "IR2_Furniture/IR2_DetailArmchairB");
            Transform collision = Require(room, "IR2_Furniture/IR2_ArmchairB");
            var anomaly = Undo.AddComponent<LivingRoomArrangementAnomaly>(node);
            Vector3 offset = new Vector3(1.15f, 0f, 0f), turn = new Vector3(0f, 180f, 0f);
            anomaly.Configure(new[] { visual, collision }, new[] { offset, offset }, new[] { turn, turn }, Array.Empty<GameObject>(), Array.Empty<GameObject>());
            Data(anomaly, 3, "The Chair Faces the Wall");
            return anomaly;
        }

        private static AnomalyBehaviour Vases(Transform room, GameObject node)
        {
            Transform props = Require(room, "IR2_Decor/IR2_MeshyProps");
            Transform cabinet = Require(room, "IR2_Furniture/IR2_DisplayCabinet");
            var targets = new List<Transform>();
            var offsets = new List<Vector3>();
            var turns = new List<Vector3>();
            foreach (Transform item in props)
            {
                if (!item.name.Contains("Vase")) continue;
                float floor = ShelfHeight(cabinet, BoundsOf(item.gameObject).min.y) + 0.001f;
                Quaternion rotation = item.localRotation;
                Vector3 turn = targets.Count % 2 == 0 ? new Vector3(0f, 0f, 90f) : new Vector3(180f, 0f, 0f);
                item.localRotation = rotation * Quaternion.Euler(turn);
                offsets.Add(item.parent.InverseTransformVector(Vector3.up * (floor - BoundsOf(item.gameObject).min.y)));
                item.localRotation = rotation;
                targets.Add(item);
                turns.Add(turn);
            }
            if (targets.Count != 6) throw new InvalidOperationException("LIVING_ROOM_VASE_COUNT: expected six porcelain instances");
            var anomaly = node.GetComponent<LivingRoomArrangementAnomaly>();
            if (anomaly == null) anomaly = Undo.AddComponent<LivingRoomArrangementAnomaly>(node);
            Undo.RecordObject(anomaly, "Set readable porcelain poses");
            anomaly.Configure(targets.ToArray(), offsets.ToArray(), turns.ToArray(), Array.Empty<GameObject>(), Array.Empty<GameObject>());
            Data(anomaly, 4, "Porcelain Turned Upside Down");
            return anomaly;
        }

        private static float ShelfHeight(Transform cabinet, float previousBottom)
        {
            float height = float.NegativeInfinity;
            foreach (Transform shelf in cabinet)
            {
                if (!shelf.name.StartsWith("IR2_Cabinet_Shelf_")) continue;
                float top = BoundsOf(shelf.gameObject).max.y;
                if (top <= previousBottom + 0.005f) height = Mathf.Max(height, top);
            }
            if (float.IsNegativeInfinity(height)) throw new InvalidOperationException("LIVING_ROOM_VASE_SHELF_MISSING");
            return height;
        }

        private static AnomalyBehaviour Books(Transform room, GameObject node)
        {
            Transform bookcase = Require(room, "IR2_Furniture/IR2_Bookcase");
            var hidden = new List<GameObject>();
            var replacement = new GameObject("ReversedBooksAndOpenBook");
            replacement.transform.SetParent(node.transform, false);
            foreach (Transform row in bookcase)
            {
                if (row.name.StartsWith("IR2_")) continue;
                hidden.Add(row.gameObject);
                var group = new GameObject("Reversed_" + row.name);
                group.transform.SetParent(replacement.transform, false);
                for (int i = 0; i < 6; i++)
                {
                    LivingRoomAnomalyProps.Book(group.transform, false);
                }
                LayoutBooks(group.transform, bookcase, hidden.Count - 1);
            }
            GameObject open = LivingRoomAnomalyProps.Book(replacement.transform, true);
            open.transform.position = bookcase.TransformPoint(new Vector3(0f, 1.44f, 0.04f));
            open.transform.rotation = bookcase.rotation;
            var anomaly = Variant(node, new[] { replacement }, hidden.ToArray());
            Data(anomaly, 5, "Books Turned Backwards");
            return anomaly;
        }

        private static void LayoutBooks(Transform group, Transform bookcase, int index)
        {
            float[] shelves = { 0.14f, 0.55f, 0.98f, 1.85f };
            float height = index == 3 ? 0.12f : 0.26f;
            group.SetPositionAndRotation(bookcase.TransformPoint(new Vector3(0f, shelves[index] + 0.04f + height * 0.5f, 0f)), bookcase.rotation);
            for (int i = 0; i < group.childCount; i++)
            {
                Transform book = group.GetChild(i);
                Undo.RecordObject(book, "Align repeated anomaly book");
                book.localPosition = new Vector3((i - 2.5f) * 0.76f / 6f, -height * 0.5f, 0f);
                book.localScale = new Vector3(0.76f / 6f / 0.11f, height / 0.25f, 1f);
            }
        }

        private static AnomalyBehaviour Shadow(GameObject node)
        {
            GameObject shadow = LivingRoomAnomalyProps.Shadow(node.transform);
            shadow.transform.SetPositionAndRotation(new Vector3(-4.395f, 0.08f, 1.2f), Quaternion.Euler(0f, -90f, 0f));
            var anomaly = Variant(node, new[] { shadow }, Array.Empty<GameObject>());
            Data(anomaly, 6, "An Extra Reader's Shadow");
            return anomaly;
        }

        private static Bounds BoundsOf(GameObject item)
        {
            Renderer[] renderers = item.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new InvalidOperationException("LIVING_ROOM_RENDERER_MISSING: " + item.name);
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }
    }
}
