using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zero.AnomalyPrototype.Anomalies;
using Zero.AnomalyPrototype.Demo;

namespace Zero.Editor
{
    public static class LivingRoomPaintingAnomalyTool
    {
        private const string Folder = "Assets/Game/Art/InfinityRoom2/";

        [MenuItem("Tools/Zero/Living Room/Add Painting Anomaly 13")]
        private static void Setup()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.name != "InfinityRoom2_LivingRoom_Demo") return;
            Transform room = GameObject.Find("InfinityRoom2_LivingRoom_Demo").transform;
            Transform root = room.Find("IR2_Anomalies");
            Renderer painting = room.Find("IR2_Decor/IR2_DetailLandscapeNorth/Landscape").GetComponent<Renderer>();
            Camera camera = GameObject.Find("IR2_DemoPlayer/IR2_PreviewCamera").GetComponent<Camera>();
            Texture2D normal = Texture("00_Normal");
            Texture2D[] stages = { Texture("01_Distant"), Texture("02_Closer"), Texture("03_Close") };
            FitPainting(painting, normal);
            Transform node = root.Find("IR2_13_ApproachingPainting");
            if (node == null)
            {
                var created = new GameObject("IR2_13_ApproachingPainting");
                Undo.RegisterCreatedObjectUndo(created, "Add painting anomaly");
                created.transform.SetParent(root, false);
                node = created.transform;
            }
            var anomaly = node.GetComponent<ApproachingPaintingAnomaly>();
            if (anomaly == null) anomaly = Undo.AddComponent<ApproachingPaintingAnomaly>(node.gameObject);
            Undo.RecordObject(anomaly, "Bind painting anomaly");
            anomaly.Configure(painting, camera, stages);
            LivingRoomAnomalyAuthoringTool.Data(anomaly, 13, "They Approach Inside The Painting", AnomalyPacingTier.MinorAnomaly, AnomalyType.Psychological);
            BindPreview(root);
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("LIVING_ROOM_PAINTING_AUTHORED: normal and three stages bound; save scene manually", anomaly);
        }

        private static Texture2D Texture(string suffix)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "IR2_13_Landscape_" + suffix + ".png");
            if (texture == null) throw new InvalidOperationException("LIVING_ROOM_PAINTING_TEXTURE_MISSING: " + suffix);
            return texture;
        }

        private static void FitPainting(Renderer painting, Texture2D normal)
        {
            var filter = painting.GetComponent<MeshFilter>();
            string meshPath = Folder + "IR2_13_PaintingFullUV.asset";
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (mesh == null)
            {
                mesh = UnityEngine.Object.Instantiate(filter.sharedMesh);
                mesh.name = "IR2_13_PaintingFullUV";
                Vector3[] vertices = mesh.vertices;
                Vector2[] uv = new Vector2[vertices.Length];
                Bounds bounds = mesh.bounds;
                for (int i = 0; i < vertices.Length; i++)
                    uv[i] = new Vector2(Mathf.InverseLerp(bounds.min.x, bounds.max.x, vertices[i].x), Mathf.InverseLerp(bounds.min.y, bounds.max.y, vertices[i].y));
                mesh.uv = uv;
                AssetDatabase.CreateAsset(mesh, meshPath);
            }
            string materialPath = Folder + "M_IR2_13_Landscape.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(painting.sharedMaterial) { name = "M_IR2_13_Landscape" };
                AssetDatabase.CreateAsset(material, materialPath);
            }
            material.SetTexture("_BaseMap", normal);
            material.SetColor("_BaseColor", Color.white);
            material.SetTextureScale("_BaseMap", Vector2.one);
            material.SetTextureOffset("_BaseMap", Vector2.zero);
            Undo.RecordObjects(new UnityEngine.Object[] { painting, filter }, "Fit complete landscape texture");
            filter.sharedMesh = mesh;
            painting.sharedMaterial = material;
            EditorUtility.SetDirty(material);
        }

        private static void BindPreview(Transform root)
        {
            var preview = root.GetComponent<LivingRoomAnomalyPreview>();
            AnomalyBehaviour[] entries = root.GetComponentsInChildren<AnomalyBehaviour>(true);
            Array.Sort(entries, (a, b) => string.CompareOrdinal(a.Data.Id, b.Data.Id));
            Undo.RecordObject(preview, "Add painting to F3 preview");
            var serialized = new SerializedObject(preview);
            SerializedProperty array = serialized.FindProperty("anomalies");
            array.arraySize = entries.Length;
            for (int i = 0; i < entries.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = entries[i];
            serialized.ApplyModifiedProperties();
        }
    }
}
