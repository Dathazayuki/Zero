using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zero.AnomalyPrototype.Anomalies;
using Zero.AnomalyPrototype.Demo;

namespace Zero.Editor
{
    public static class LivingRoomExtraAnomalyTool
    {
        [MenuItem("Tools/Zero/Living Room/Add Anomalies 07 08 09 11")]
        private static void AddExtras()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.name != "InfinityRoom2_LivingRoom_Demo") return;
            Transform room = GameObject.Find("InfinityRoom2_LivingRoom_Demo").transform;
            Transform root = Require(room, "IR2_Anomalies");
            var preview = root.GetComponent<LivingRoomAnomalyPreview>();
            GameObject camera = GameObject.Find("IR2_DemoPlayer/IR2_PreviewCamera");
            if (camera == null) throw new InvalidOperationException("LIVING_ROOM_OBSERVER_MISSING");
            if (root.Find("IR2_07_TeapotTurned") != null)
            {
                CompleteExisting(room, root, preview, camera.transform);
                return;
            }
            Require(room, "IR2_Decor/IR2_DetailLandscapeNorth");
            Require(room, "IR2_Decor/IR2_MeshyProps/PF_IR2_Meshy_DeskTelephone");
            Texture2D eye = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Game/Art/Eye.png");
            if (eye == null) throw new InvalidOperationException("LIVING_ROOM_EYE_TEXTURE_MISSING");
            Transform[] tea = LivingRoomTeaMeshAuthoring.Articulate(Require(room, "IR2_Decor/IR2_DetailTeaSet"));
            var entries = new AnomalyBehaviour[4];
            entries[0] = Teapot(root, tea[0]);
            entries[1] = Picture(root, room);
            entries[2] = Drinking(root, tea[1]);
            entries[3] = Phone(root, room, eye, camera.transform);
            Append(preview, entries);
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("LIVING_ROOM_EXTRAS_AUTHORED: F3 cycles 01..09 then 11; save scene manually", root);
        }

        private static void CompleteExisting(Transform room, Transform root, LivingRoomAnomalyPreview preview, Transform observer)
        {
            var phone = Require(root, "IR2_11_PhoneEye").GetComponent<PhoneEyeAnomaly>();
            Renderer display = Require(room, "IR2_Decor/IR2_MeshyProps/PF_IR2_Meshy_DeskTelephone/IR2_LCD/EyeScreen").GetComponent<Renderer>();
            phone.Configure(display, observer);
            LivingRoomAnomalyAuthoringTool.Data(phone, 11, "The Eye On The Telephone LCD", AnomalyPacingTier.MinorAnomaly, AnomalyType.Psychological);
            Append(preview, new AnomalyBehaviour[] { Require(root, "IR2_07_TeapotTurned").GetComponent<AnomalyBehaviour>(),
                Require(root, "IR2_08_TiltedPicture").GetComponent<AnomalyBehaviour>(),
                Require(root, "IR2_09_InvisibleTeaDrinker").GetComponent<AnomalyBehaviour>(), phone });
            EditorSceneManager.MarkSceneDirty(room.gameObject.scene);
            Debug.Log("LIVING_ROOM_EXTRAS_BOUND: existing four reused; save scene manually", root);
        }

        private static Transform Require(Transform root, string path)
        {
            Transform item = root.Find(path);
            if (item == null) throw new InvalidOperationException("LIVING_ROOM_EXTRA_REFERENCE_MISSING: " + path);
            return item;
        }

        private static GameObject Node(Transform root, string name)
        {
            var node = new GameObject(name);
            node.transform.SetParent(root, false);
            Undo.RegisterCreatedObjectUndo(node, "Add Living Room anomaly");
            return node;
        }

        private static AnomalyBehaviour Teapot(Transform root, Transform pot)
        {
            var anomaly = Undo.AddComponent<LivingRoomArrangementAnomaly>(Node(root, "IR2_07_TeapotTurned"));
            anomaly.Configure(new[] { pot }, new[] { Vector3.zero }, new[] { new Vector3(0f, 180f, 0f) },
                Array.Empty<GameObject>(), Array.Empty<GameObject>());
            LivingRoomAnomalyAuthoringTool.Data(anomaly, 7, "Teapot Handle Turned");
            return anomaly;
        }

        private static AnomalyBehaviour Picture(Transform root, Transform room)
        {
            var anomaly = Undo.AddComponent<LivingRoomArrangementAnomaly>(Node(root, "IR2_08_TiltedPicture"));
            Transform frame = Require(room, "IR2_Decor/IR2_DetailLandscapeNorth");
            Transform backing = Require(room, "IR2_Decor/IR2_Painting_North_Backing");
            anomaly.Configure(new[] { frame, backing }, new[] { Vector3.zero, Vector3.zero },
                new[] { new Vector3(0f, 0f, 5f), new Vector3(0f, 0f, 5f) }, Array.Empty<GameObject>(), Array.Empty<GameObject>());
            LivingRoomAnomalyAuthoringTool.Data(anomaly, 8, "Slightly Tilted Picture");
            return anomaly;
        }

        private static AnomalyBehaviour Drinking(Transform root, Transform cup)
        {
            var anomaly = Undo.AddComponent<FloatingTeaCupAnomaly>(Node(root, "IR2_09_InvisibleTeaDrinker"));
            Material tea = LivingRoomAnomalyProps.Material("M_IR2_DrinkingTea", new Color(0.13f, 0.045f, 0.012f));
            GameObject surface = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            surface.name = "TeaSurface";
            surface.transform.SetParent(cup, false);
            surface.transform.localPosition = new Vector3(0f, 0.051f, 0f);
            surface.transform.localScale = new Vector3(0.075f, 0.0005f, 0.075f);
            UnityEngine.Object.DestroyImmediate(surface.GetComponent<Collider>());
            surface.GetComponent<Renderer>().sharedMaterial = tea;
            Transform ripple = Ripple(cup);
            anomaly.Configure(cup, ripple);
            LivingRoomAnomalyAuthoringTool.Data(anomaly, 9, "An Invisible Person Drinks Tea", AnomalyPacingTier.MinorAnomaly);
            return anomaly;
        }

        private static Transform Ripple(Transform cup)
        {
            var root = new GameObject("TeaRipple", typeof(LineRenderer));
            root.transform.SetParent(cup, false);
            root.transform.localPosition = new Vector3(0f, 0.052f, 0f);
            LineRenderer ring = root.GetComponent<LineRenderer>();
            ring.useWorldSpace = false;
            ring.loop = true;
            ring.positionCount = 32;
            ring.widthMultiplier = 0.0008f;
            ring.sharedMaterial = LivingRoomAnomalyProps.Material("M_IR2_TeaRipple", new Color(0.32f, 0.15f, 0.04f));
            for (int i = 0; i < 32; i++)
            {
                float angle = i * Mathf.PI / 16f;
                ring.SetPosition(i, new Vector3(Mathf.Cos(angle) * 0.025f, 0f, Mathf.Sin(angle) * 0.025f));
            }
            return root.transform;
        }

        private static AnomalyBehaviour Phone(Transform root, Transform room, Texture2D eye, Transform observer)
        {
            Transform phone = Require(room, "IR2_Decor/IR2_MeshyProps/PF_IR2_Meshy_DeskTelephone");
            GameObject lcd = Node(phone, "IR2_LCD");
            FitLcd(phone, lcd.transform);
            Material frame = LivingRoomAnomalyProps.Material("M_IR2_LCDFrame", new Color(0.065f, 0.06f, 0.045f));
            LivingRoomAnomalyProps.Part("Bezel", lcd.transform, Vector3.zero, new Vector3(0.14f, 0.075f, 0.012f), frame);
            Material glass = LivingRoomAnomalyProps.Material("M_IR2_LCDGlass", new Color(0.035f, 0.055f, 0.035f));
            LivingRoomAnomalyProps.Part("NormalScreen", lcd.transform, new Vector3(0f, 0f, -0.007f), new Vector3(0.124f, 0.057f, 0.002f), glass);
            GameObject image = GameObject.CreatePrimitive(PrimitiveType.Quad);
            image.name = "EyeScreen";
            image.transform.SetParent(lcd.transform, false);
            image.transform.localPosition = new Vector3(0f, 0f, -0.009f);
            image.transform.localScale = new Vector3(0.122f, 0.055f, 1f);
            UnityEngine.Object.DestroyImmediate(image.GetComponent<Collider>());
            Renderer display = image.GetComponent<Renderer>();
            display.sharedMaterial = EyeMaterial(eye);
            display.enabled = false;
            var anomaly = Undo.AddComponent<PhoneEyeAnomaly>(Node(root, "IR2_11_PhoneEye"));
            anomaly.Configure(display, observer);
            LivingRoomAnomalyAuthoringTool.Data(anomaly, 11, "The Eye On The Telephone LCD", AnomalyPacingTier.MinorAnomaly, AnomalyType.Psychological);
            return anomaly;
        }

        [MenuItem("Tools/Zero/Living Room/Fit Existing Telephone LCD")]
        private static void FitExistingLcd()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().name != "InfinityRoom2_LivingRoom_Demo") return;
            Transform phone = GameObject.Find("InfinityRoom2_LivingRoom_Demo/IR2_Decor/IR2_MeshyProps/PF_IR2_Meshy_DeskTelephone").transform;
            FitLcd(phone, Require(phone, "IR2_LCD"));
            EditorSceneManager.MarkSceneDirty(phone.gameObject.scene);
        }

        private static void FitLcd(Transform phone, Transform lcd)
        {
            Transform model = Require(phone, "Phone");
            var probe = new GameObject("IR2_TemporarySurfaceProbe", typeof(MeshCollider));
            try
            {
                probe.transform.SetPositionAndRotation(model.position, model.rotation);
                probe.transform.localScale = model.lossyScale;
                MeshCollider collider = probe.GetComponent<MeshCollider>();
                collider.sharedMesh = model.GetComponent<MeshFilter>().sharedMesh;
                Physics.SyncTransforms();
                Vector3 origin = phone.TransformPoint(new Vector3(0f, 0.484f, 0.5f));
                Vector3 target = phone.TransformPoint(new Vector3(-0.115f, 0.144f, -0.03f));
                if (!collider.Raycast(new Ray(origin, (target - origin).normalized), out RaycastHit hit, 2f))
                    throw new InvalidOperationException("LIVING_ROOM_LCD_SURFACE_NOT_FOUND");
                Undo.RecordObject(lcd, "Fit LCD onto original telephone screen");
                lcd.SetPositionAndRotation(hit.point, Quaternion.LookRotation(-hit.normal, model.up));
                Debug.Log($"LIVING_ROOM_LCD_FITTED: localPosition={lcd.localPosition}, normal={hit.normal}", lcd);
            }
            finally { UnityEngine.Object.DestroyImmediate(probe); }
        }

        private static Material EyeMaterial(Texture2D eye)
        {
            string path = LivingRoomAnomalyProps.Folder + "M_IR2_PhoneEye.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "M_IR2_PhoneEye" };
            material.SetTexture("_BaseMap", eye);
            material.SetColor("_BaseColor", new Color(0.72f, 0.88f, 0.72f));
            material.SetTextureScale("_BaseMap", new Vector2(0.6f, 0.5f));
            material.SetTextureOffset("_BaseMap", new Vector2(0.2f, 0.25f));
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void Append(LivingRoomAnomalyPreview preview, AnomalyBehaviour[] additions)
        {
            Undo.RecordObject(preview, "Append four Living Room preview anomalies");
            var serialized = new SerializedObject(preview);
            SerializedProperty array = serialized.FindProperty("anomalies");
            foreach (AnomalyBehaviour addition in additions)
            {
                bool present = false;
                for (int i = 0; i < array.arraySize; i++)
                    if (array.GetArrayElementAtIndex(i).objectReferenceValue == addition) present = true;
                if (present) continue;
                int index = array.arraySize++;
                array.GetArrayElementAtIndex(index).objectReferenceValue = addition;
            }
            serialized.ApplyModifiedProperties();
        }

        [MenuItem("Tools/Zero/Living Room/Add Anomaly 14 Mini Potted Plant")]
        public static void AddPottedPlant()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.name != "InfinityRoom2_LivingRoom_Demo") return;
            Transform room = GameObject.Find("InfinityRoom2_LivingRoom_Demo").transform;
            Transform root = Require(room, "IR2_Anomalies");
            Transform plant = Require(room, "IR2_Decor/IR2_MeshyProps/PF_IR2_Meshy_PottedPlant");
            Transform node = root.Find("IR2_14_MiniPottedPlant");
            if (node == null) node = Node(root, "IR2_14_MiniPottedPlant").transform;
            var anomaly = node.GetComponent<MiniPottedPlantAnomaly>();
            if (anomaly == null) anomaly = Undo.AddComponent<MiniPottedPlantAnomaly>(node.gameObject);
            anomaly.Configure(plant, 0.6f);
            LivingRoomAnomalyAuthoringTool.Data(anomaly, 14, "Mini Potted Plant", AnomalyPacingTier.UneasyNormal, AnomalyType.ObjectChange);
            var preview = root.GetComponent<LivingRoomAnomalyPreview>();
            if (preview != null) Append(preview, new AnomalyBehaviour[] { anomaly });
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("LIVING_ROOM_ANOMALY_14_BOUND: Mini Potted Plant (scale 0.6) registered; save scene manually", node);
        }

        [MenuItem("Tools/Zero/Living Room/Review Added Anomalies")]
        private static void Review()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().name != "InfinityRoom2_LivingRoom_Demo") return;
            Transform root = GameObject.Find("InfinityRoom2_LivingRoom_Demo/IR2_Anomalies").transform;
            string[] names = { "IR2_07_TeapotTurned", "IR2_08_TiltedPicture", "IR2_09_InvisibleTeaDrinker", "IR2_11_PhoneEye" };
            Vector3[] points = { new Vector3(-0.7f, 0.6f, 0.05f), new Vector3(2.5f, 2.25f, 4.87f), new Vector3(-0.48f, 0.8f, 0.25f), new Vector3(4.1f, 0.96f, -1.52f) };
            Vector3[] cameras = { new Vector3(-0.7f, 1.15f, -0.65f), new Vector3(2.5f, 2.25f, 3.5f), new Vector3(-0.1f, 1.1f, -0.5f), new Vector3(3.6f, 1.3f, -1.52f) };
            for (int i = 0; i < names.Length; i++)
            {
                AnomalyBehaviour anomaly = Require(root, names[i]).GetComponent<AnomalyBehaviour>();
                try
                {
                    anomaly.Activate();
                    if (anomaly is FloatingTeaCupAnomaly tea) tea.Cup.localPosition += Vector3.up * 0.3f;
                    Capture(names[i], cameras[i], points[i]);
                }
                finally { anomaly.ResetAnomaly(); }
            }
        }

        internal static void Capture(string name, Vector3 position, Vector3 target)
        {
            var root = new GameObject("IR2_ReviewCamera", typeof(Camera));
            var render = new RenderTexture(900, 700, 24);
            var pixels = new Texture2D(900, 700, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active;
            try
            {
                Camera camera = root.GetComponent<Camera>();
                camera.enabled = false;
                camera.fieldOfView = 50f;
                camera.nearClipPlane = 0.02f;
                camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(target - position));
                camera.targetTexture = render;
                camera.Render();
                RenderTexture.active = render;
                pixels.ReadPixels(new Rect(0, 0, 900, 700), 0, 0);
                pixels.Apply();
                string directory = System.IO.Path.Combine(Application.dataPath, "Game/.scratch/LivingRoomAnomalies");
                System.IO.Directory.CreateDirectory(directory);
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(directory, name + ".png"), pixels.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                UnityEngine.Object.DestroyImmediate(root);
                render.Release();
                UnityEngine.Object.DestroyImmediate(render);
                UnityEngine.Object.DestroyImmediate(pixels);
            }
        }
    }
}
