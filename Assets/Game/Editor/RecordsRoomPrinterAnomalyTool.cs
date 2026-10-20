using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zero.AnomalyPrototype.Anomalies;
using Zero.AnomalyPrototype.Demo;

namespace Zero.Editor
{
    public static class RecordsRoomPrinterAnomalyTool
    {
        private const string SceneName = "InfinityRoom3_RecordsRoom_Demo";
        private const string Menu = "Tools/Zero/Records Room/Add Printer Anomaly 08";

        [MenuItem("Tools/Zero/Records Room/Align Printer 08 To Actual Roller")]
        private static void AlignRoller()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.name != SceneName) return;
            Transform room = RoomRoot(scene);
            Transform root = Require(room, "IR3_Anomalies/IR3_08_EndlessPrinter");
            Transform normal = Require(room, "IR3_Decor/IR3_MeshyProps/PF_IR3_Meshy_OfficePrinter");
            Transform existing = root.Find("PrinterModel");
            GameObject model = existing != null ? existing.gameObject : UnityEngine.Object.Instantiate(normal.gameObject, root);
            if (existing == null) Undo.RegisterCreatedObjectUndo(model, "Add IR3-08 actual printer model");
            model.name = "PrinterModel";
            Undo.RecordObject(root, "Align paper with actual roller");
            // Calibrated from top/side orthographic views of the current Mayin model, not its bounds.
            root.SetPositionAndRotation(new Vector3(-2.937f, 1.219f, -0.771f), Quaternion.Euler(0f, 315f, 0f));
            ResetPaperFrame(Require(root, "PaperRoute"));
            ResetPaperFrame(Require(root, "PaperRibbon"));
            model.transform.SetPositionAndRotation(normal.position, normal.rotation);
            model.transform.localScale = normal.lossyScale;
            model.SetActive(false);
            var anomaly = root.GetComponent<EndlessPrinterAnomaly>();
            Undo.RecordObject(anomaly, "Configure actual printer variant");
            anomaly.Configure(root.Find("PaperRibbon").GetComponent<MeshFilter>(), CalibratedRoute(root));
            anomaly.ConfigurePrinter(normal.gameObject, model);
            var settings = new SerializedObject(anomaly);
            settings.FindProperty("paperWidth").floatValue = 0.366f;
            settings.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = root.gameObject;
            Debug.Log("RECORDS_PRINTER_ROLLER_ALIGNED: source at (-2.937,1.219,-0.771), yaw315; save scene manually", root);
        }

        private static Transform[] CalibratedRoute(Transform root)
        {
            float floor = 0.025f - root.position.y;
            Vector3[] points = { Vector3.zero, new Vector3(0f, 0.07f, 0.055f),
                new Vector3(0f, 0.145f, 0.108f), new Vector3(0f, 0.19f, 0.15f),
                new Vector3(0f, 0.20f, 0.21f), new Vector3(0f, 0.16f, 0.28f),
                new Vector3(0f, 0.04f, 0.37f), new Vector3(0f, -0.19f, 0.48f),
                new Vector3(0f, floor + 0.12f, 0.62f), new Vector3(0f, floor, 0.76f),
                new Vector3(0.02f, floor + 0.025f, 0.98f), new Vector3(0.01f, floor + 0.07f, 0.76f),
                new Vector3(-0.01f, floor + 0.12f, 0.98f), new Vector3(0f, floor + 0.16f, 0.76f) };
            var route = new Transform[points.Length];
            Transform folder = Require(root, "PaperRoute");
            for (int i = 0; i < route.Length; i++)
            {
                route[i] = Require(folder, $"Feed_{i:00}");
                Undo.RecordObject(route[i], "Align paper feed path");
                route[i].localPosition = points[i];
            }
            return route;
        }

        private static void ResetPaperFrame(Transform part)
        {
            Undo.RecordObject(part, "Clear previous paper frame offset");
            part.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            part.localScale = Vector3.one;
        }

        [MenuItem(Menu)]
        private static void AddPrinter()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.name != SceneName) return;
            Transform room = RoomRoot(scene);
            Transform root = Require(room, "IR3_Anomalies");
            if (root.Find("IR3_08_EndlessPrinter") != null) return;
            Transform printer = Require(room, "IR3_Decor/IR3_MeshyProps/PF_IR3_Meshy_OfficePrinter");
            LivingRoomAnomalyPreview preview = root.GetComponent<LivingRoomAnomalyPreview>();
            if (preview == null) throw new InvalidOperationException("RECORDS_PRINTER_PREVIEW_MISSING");
            Renderer[] renderers = printer.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) throw new InvalidOperationException("RECORDS_PRINTER_RENDERER_MISSING");
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
            GameObject node = Node(root, "IR3_08_EndlessPrinter");
            node.transform.SetPositionAndRotation(new Vector3(bounds.max.x - 0.035f,
                bounds.min.y + bounds.size.y * 0.45f, bounds.center.z), Quaternion.LookRotation(Vector3.right));
            var anomaly = Undo.AddComponent<EndlessPrinterAnomaly>(node);
            MeshFilter paper = Paper(node.transform);
            anomaly.Configure(paper, Route(node.transform));
            anomaly.Initialize(Data());
            Append(preview, anomaly);
            AlignRoller();
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = node;
            Debug.Log("RECORDS_PRINTER_AUTHORED: IR3-08 appended to F3; F2 clears paper; save scene manually", node);
        }

        private static Transform RoomRoot(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == SceneName) return root.transform;
            throw new InvalidOperationException("RECORDS_ROOM_ROOT_MISSING");
        }

        private static Transform Require(Transform root, string path)
        {
            Transform target = root.Find(path);
            if (target == null) throw new InvalidOperationException("RECORDS_PRINTER_TARGET_MISSING: " + path);
            return target;
        }

        private static GameObject Node(Transform parent, string name)
        {
            var node = new GameObject(name);
            node.transform.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(node, "Author IR3-08 printer paper");
            return node;
        }

        private static MeshFilter Paper(Transform root)
        {
            GameObject surface = Node(root, "PaperRibbon");
            var filter = Undo.AddComponent<MeshFilter>(surface);
            var renderer = Undo.AddComponent<MeshRenderer>(surface);
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Game/Materials/InfinityRoom3Demo/M_IR3Demo_Paper.mat");
            if (renderer.sharedMaterial == null) throw new InvalidOperationException("RECORDS_PRINTER_PAPER_MATERIAL_MISSING");
            surface.SetActive(false);
            return filter;
        }

        private static Transform[] Route(Transform root)
        {
            Transform folder = Node(root, "PaperRoute").transform;
            float floor = 0.025f - root.position.y;
            Vector3[] positions = { Vector3.zero, new Vector3(0f, 0.01f, 0.12f),
                new Vector3(0f, -0.12f, 0.28f), new Vector3(0f, -0.15f, 0.62f),
                new Vector3(0f, floor + 0.12f, 0.82f), new Vector3(0f, floor, 1.02f),
                new Vector3(0.03f, floor + 0.025f, 1.26f), new Vector3(-0.02f, floor + 0.035f, 1.5f),
                new Vector3(0.02f, floor + 0.08f, 1.29f), new Vector3(0.04f, floor + 0.09f, 1.05f),
                new Vector3(0.01f, floor + 0.14f, 1.3f), new Vector3(-0.02f, floor + 0.16f, 1.54f),
                new Vector3(0f, floor + 0.2f, 1.3f), new Vector3(0.02f, floor + 0.21f, 1.07f) };
            var route = new Transform[positions.Length];
            for (int i = 0; i < route.Length; i++)
            {
                route[i] = Node(folder, $"Feed_{i:00}").transform;
                route[i].localPosition = positions[i];
            }
            return route;
        }

        private static AnomalyData Data()
        {
            const string path = "Assets/Game/ScriptableObjects/Anomalies/InfinityRoom3/IR3_08.asset";
            AnomalyData data = AssetDatabase.LoadAssetAtPath<AnomalyData>(path);
            if (data != null) return data;
            data = ScriptableObject.CreateInstance<AnomalyData>();
            var serialized = new SerializedObject(data);
            serialized.FindProperty("id").stringValue = "IR3-08";
            serialized.FindProperty("displayName").stringValue = "The Printer Will Not Stop";
            serialized.FindProperty("description").stringValue = "A continuous white paper ribbon feeds out of the printer and folds onto the floor. The paper remains after feeding stops.";
            serialized.FindProperty("pacingTier").enumValueIndex = (int)AnomalyPacingTier.MinorAnomaly;
            serialized.FindProperty("difficulty").intValue = 1;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(data, path);
            return data;
        }

        private static void Append(LivingRoomAnomalyPreview preview, AnomalyBehaviour anomaly)
        {
            var serialized = new SerializedObject(preview);
            SerializedProperty entries = serialized.FindProperty("anomalies");
            int index = entries.arraySize;
            entries.arraySize++;
            entries.GetArrayElementAtIndex(index).objectReferenceValue = anomaly;
            serialized.ApplyModifiedProperties();
        }
    }
}
