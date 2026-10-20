using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zero.AnomalyPrototype.Anomalies;
using Zero.AnomalyPrototype.Demo;

namespace Zero.Editor
{
    public static class RecordsRoomAnomalyAuthoringTool
    {
        private const string SceneName = "InfinityRoom3_RecordsRoom_Demo";
        private const string Folder = "Assets/Game/Prefabs/InfinityRoom3Demo/Anomalies";
        private const string DataFolder = "Assets/Game/ScriptableObjects/Anomalies/InfinityRoom3";
        private const string Cabinet = "IR3_Furniture/IR3_Cabinet1/IR3_Cabinet1_";

        [MenuItem("Tools/Zero/Records Room/Add First Five Anomalies To Demo")]
        private static void AddAnomalies()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.name != SceneName) return;
            Transform room = RoomRoot(scene);
            if (room.Find("IR3_Anomalies") != null)
            {
                Debug.LogWarning("RECORDS_ROOM_AUTHORING_SKIPPED: IR3_Anomalies already exists", room);
                return;
            }
            ValidateTargets(room);
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Game/Prefabs/InfinityRoom3Demo", "Anomalies");
            if (!AssetDatabase.IsValidFolder(DataFolder)) AssetDatabase.CreateFolder("Assets/Game/ScriptableObjects/Anomalies", "InfinityRoom3");
            Transform root = Node(room, "IR3_Anomalies").transform;
            AnomalyBehaviour[] entries = { Labels(root, room), Pen(root, room), Binder(root, room), Drawer(root, room), Notice(root, room) };
            var preview = Undo.AddComponent<LivingRoomAnomalyPreview>(root.gameObject);
            var serialized = new SerializedObject(preview);
            SerializedProperty array = serialized.FindProperty("anomalies");
            array.arraySize = entries.Length;
            for (int i = 0; i < entries.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = entries[i];
            serialized.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = root.gameObject;
            Debug.Log("RECORDS_ROOM_FIVE_ANOMALIES_AUTHORED: F3 cycles IR3-01..05, F2 restores normal; save scene manually", root);
        }

        private static Transform RoomRoot(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects()) if (root.name == SceneName) return root.transform;
            throw new InvalidOperationException("RECORDS_ROOM_ROOT_MISSING");
        }

        private static Transform Require(Transform room, string path)
        {
            Transform target = room.Find(path);
            if (target == null && (path.Contains("_Label") || path.Contains("_Handle")))
            {
                int split = path.LastIndexOf('/');
                string name = path.Substring(split + 1);
                int suffix = name.LastIndexOf('_');
                string drawer = name.Substring(0, suffix) + "_Drawer" + name[name.Length - 1];
                target = room.Find(path.Substring(0, split + 1) + drawer + "/" + name);
            }
            if (target == null) throw new InvalidOperationException("RECORDS_ROOM_AUTHORING_MISSING: " + path);
            return target;
        }

        private static void ValidateTargets(Transform room)
        {
            for (int i = 0; i < 4; i++) Require(room, Cabinet + "Label" + i);
            Require(room, Cabinet + "Drawer3");
            Require(room, Cabinet + "Handle3");
            Require(room, "IR3_Decor/IR3_DetailFolder_Desk/Pen/Pen");
            Require(room, "IR3_Furniture/IR3_DetailedFiles/IR3_File_A_L3_S0_B2");
            Require(room, "IR3_Decor/IR3_Noticeboard/IR3_Notice_0");
        }

        private static GameObject Node(Transform parent, string name)
        {
            var node = new GameObject(name);
            node.transform.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(node, "Author Records Room anomaly");
            return node;
        }

        private static LivingRoomArrangementAnomaly Movement(GameObject node, Transform[] targets, Vector3[] offsets, Vector3[] turns)
        {
            var anomaly = Undo.AddComponent<LivingRoomArrangementAnomaly>(node);
            anomaly.Configure(targets, offsets, turns, Array.Empty<GameObject>(), Array.Empty<GameObject>());
            return anomaly;
        }

        private static void Data(AnomalyBehaviour anomaly, int index, string title, string description)
        {
            string path = DataFolder + $"/IR3_{index:00}.asset";
            AnomalyData data = AssetDatabase.LoadAssetAtPath<AnomalyData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<AnomalyData>();
                var serialized = new SerializedObject(data);
                serialized.FindProperty("id").stringValue = $"IR3-{index:00}";
                serialized.FindProperty("displayName").stringValue = title;
                serialized.FindProperty("description").stringValue = description;
                serialized.FindProperty("pacingTier").enumValueIndex = (int)AnomalyPacingTier.UneasyNormal;
                serialized.FindProperty("difficulty").intValue = 3;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.CreateAsset(data, path);
            }
            anomaly.Initialize(data);
            EditorUtility.SetDirty(anomaly);
        }

        private static AnomalyBehaviour Labels(Transform root, Transform room)
        {
            Transform[] labels = new Transform[4];
            for (int i = 0; i < labels.Length; i++)
            {
                Transform plate = Require(room, Cabinet + "Label" + i);
                TextMeshPro text = Text(plate, "ArchiveCode", "A-" + (10 + i), new Vector2(0.15f, 0.065f));
                text.transform.localPosition = new Vector3(0.011f, 0f, 0f);
                text.transform.localRotation = Quaternion.Euler(0f, 270f, 0f);
                text.fontSize = 0.4f;
                labels[i] = text.transform;
            }
            Vector3 delta = labels[3].position - labels[2].position;
            var anomaly = Movement(Node(root, "IR3_01_SwappedLabels"), new[] { labels[2], labels[3] },
                new[] { labels[2].parent.InverseTransformVector(delta), labels[3].parent.InverseTransformVector(-delta) }, new Vector3[2]);
            Data(anomaly, 1, "Archive Labels Out Of Order", "Only A-12 and A-13 swap places on cabinet 1; all other labels stay unchanged.");
            return anomaly;
        }

        private static AnomalyBehaviour Pen(Transform root, Transform room)
        {
            Transform pen = Require(room, "IR3_Decor/IR3_DetailFolder_Desk/Pen/Pen");
            Vector3 turn = Quaternion.AngleAxis(180f, pen.InverseTransformDirection(Vector3.up)).eulerAngles;
            var anomaly = Movement(Node(root, "IR3_02_ReversedPen"), new[] { pen }, new Vector3[1], new[] { turn });
            Data(anomaly, 2, "The Pen Points The Other Way", "One pen on the desk rotates 180 degrees in the tabletop plane, retaining its position.");
            return anomaly;
        }

        private static AnomalyBehaviour Binder(Transform root, Transform room)
        {
            Transform node = Node(root, "IR3_03_ProtrudingBinder").transform;
            return RecordsRoomRevisedAnomaliesTool.ConfigureFallingFiles(node, Require(room, "IR3_Furniture/IR3_DetailedFiles"));
        }

        private static AnomalyBehaviour Drawer(Transform root, Transform room)
        {
            Transform[] targets = { Require(room, Cabinet + "Drawer3") };
            var offsets = new Vector3[targets.Length];
            for (int i = 0; i < targets.Length; i++) offsets[i] = targets[i].parent.InverseTransformVector(Vector3.right * 0.025f);
            var anomaly = Movement(Node(root, "IR3_04_AjarDrawer"), targets, offsets, new Vector3[targets.Length]);
            Data(anomaly, 4, "The Drawer Is Not Quite Closed", "The top drawer of cabinet 1 is 2.5 cm ajar, with its handle and label moving together.");
            return anomaly;
        }

        private static TextMeshPro Text(Transform parent, string name, string content, Vector2 size)
        {
            GameObject node = Node(parent, name);
            var text = Undo.AddComponent<TextMeshPro>(node);
            text.text = content;
            text.color = new Color(0.09f, 0.08f, 0.065f);
            text.alignment = TextAlignmentOptions.Center;
            text.rectTransform.sizeDelta = size;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Truncate;
            return text;
        }

        private static AnomalyBehaviour Notice(Transform root, Transform room)
        {
            Transform notice = Require(room, "IR3_Decor/IR3_Noticeboard/IR3_Notice_0");
            MeshRenderer original = notice.GetComponent<MeshRenderer>();
            Bounds bounds = notice.GetComponent<MeshFilter>().sharedMesh.bounds;
            GameObject normal = FoldedPaper(notice, bounds, original.sharedMaterial, false);
            GameObject changed = FoldedPaper(notice, bounds, original.sharedMaterial, true);
            Undo.RecordObject(original, "Replace flat notice with folded paper");
            original.enabled = false;
            changed.SetActive(false);
            TextMeshPro text = Text(notice, "NoticePrinting", "ARCHIVE NOTICE\n\nRETURN FILES\nTO THEIR PLACE\n\nRECORDS DEPARTMENT", new Vector2(bounds.size.x * 0.85f, bounds.size.y * 0.7f));
            text.fontSize = 0.14f;
            text.transform.localPosition = new Vector3(bounds.center.x, bounds.center.y, bounds.min.z - 0.002f);
            var anomaly = Undo.AddComponent<LivingRoomArrangementAnomaly>(Node(root, "IR3_05_WrongFoldedCorner"));
            anomaly.Configure(Array.Empty<Transform>(), Array.Empty<Vector3>(), Array.Empty<Vector3>(), new[] { changed }, new[] { normal });
            Data(anomaly, 5, "The Notice Has The Wrong Fold", "The top-right 5 cm folded corner changes to the top-left; the notice text and position remain identical.");
            return anomaly;
        }

        private static GameObject FoldedPaper(Transform parent, Bounds bounds, Material paper, bool left)
        {
            string name = left ? "FoldTopLeft" : "FoldTopRight";
            GameObject node = Node(parent, name);
            var filter = Undo.AddComponent<MeshFilter>(node);
            filter.sharedMesh = PaperMesh(name, bounds, left);
            var renderer = Undo.AddComponent<MeshRenderer>(node);
            renderer.sharedMaterials = new[] { paper, UndersideMaterial() };
            return node;
        }

        private static Material UndersideMaterial()
        {
            string path = Folder + "/M_IR3_PaperFold.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            Material source = AssetDatabase.LoadAssetAtPath<Material>("Assets/Game/Materials/InfinityRoom3Demo/M_IR3Demo_Paper.mat");
            material = new Material(source) { name = "M_IR3_PaperFold" };
            material.SetColor("_BaseColor", new Color(0.49f, 0.45f, 0.34f));
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Mesh PaperMesh(string name, Bounds bounds, bool left)
        {
            string path = Folder + "/IR3_" + name + ".asset";
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh != null) return mesh;
            float x = bounds.extents.x, y = bounds.extents.y, z = bounds.min.z - 0.001f, f = 0.05f;
            Vector3[] vertices = { new Vector3(-x, -y, z), new Vector3(x, -y, z), new Vector3(x, y-f, z),
                new Vector3(x-f, y, z), new Vector3(-x, y, z), new Vector3(x-f, y, z-0.001f),
                new Vector3(x, y-f, z-0.001f), new Vector3(x-f, y-f, z-0.008f) };
            for (int i = 0; i < vertices.Length; i++)
            {
                if (left) vertices[i].x = -vertices[i].x;
                vertices[i].x += bounds.center.x;
                vertices[i].y += bounds.center.y;
            }
            int[] body = left ? new[] { 0, 1, 2, 0, 2, 3, 0, 3, 4 } : new[] { 0, 2, 1, 0, 3, 2, 0, 4, 3 };
            int[] fold = left ? new[] { 5, 7, 6 } : new[] { 5, 6, 7 };
            mesh = new Mesh { name = name, vertices = vertices, subMeshCount = 2 };
            mesh.SetTriangles(body, 0);
            mesh.SetTriangles(fold, 1);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }
    }
}
