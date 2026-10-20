using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Zero.AnomalyPrototype.Anomalies;
using Zero.AnomalyPrototype.Demo;
using Zero.AnomalyPrototype.Player;

namespace Zero.Editor
{
    public static class RecordsRoomHorrorTool
    {
        private const string SceneName = "InfinityRoom3_RecordsRoom_Demo";
        private const string Decor = "IR3_Decor/";

        [MenuItem("Tools/Zero/Records Room/Refine New Horror Prop Placement")]
        private static void Refine()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.name != SceneName || EditorApplication.isPlayingOrWillChangePlaymode) return;
            Transform room = SceneRoot(scene, SceneName);
            Transform node = Require(room, "IR3_Anomalies/IR3_15_SixLeggedHead");
            Transform box = Require(node, "HollowArchiveBox");
            Transform head = Require(node, "HeadCreature");
            Undo.RecordObjects(new UnityEngine.Object[] { box, head }, "Refine head encounter placement");
            Transform oldBox = Require(room, Decor + "IR3_DetailStorage_IR3_Cabinet1");
            Transform newBox = Require(room, Decor + "IR3_DetailStorage_IR3_Cabinet4");
            Bounds bounds = BoundsOf(Require(newBox, "BoxBody"));
            Undo.RecordObjects(new UnityEngine.Object[] { oldBox.gameObject, newBox.gameObject }, "Keep box clear of plant");
            oldBox.gameObject.SetActive(true);
            newBox.gameObject.SetActive(false);
            box.position = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            box.localScale = new Vector3(0.96f, 1.3f, 0.95f);
            head.position = new Vector3(box.position.x + 2.33f, 0.28f, box.position.z);
            RecordsRoomHorrorAssets.RefineLegs(head);
            var anomaly = new SerializedObject(node.GetComponent<ArchiveHeadAnomaly>());
            anomaly.FindProperty("landingOffset").vector3Value = new Vector3(1.7f, 0.34f - box.position.y, 0f);
            anomaly.ApplyModifiedProperties();
            foreach (Transform part in room.GetComponentsInChildren<Transform>(true)) RefineStamp(part);
            EditorSceneManager.MarkSceneDirty(scene);
        }

        private static void RefineStamp(Transform part)
        {
            if (part.name != "IR3_07_BlackPalm") return;
            Bounds bounds = part.parent.GetComponent<MeshRenderer>().bounds;
            Vector3 normal = part.parent.position.x < 0f ? Vector3.right : Vector3.left;
            if (bounds.size.x > bounds.size.z) normal = Vector3.back;
            float offset = Mathf.Sin(part.parent.position.z * 7f + part.parent.position.y * 13f) > 0f ? 0.24f : -0.24f;
            Undo.RecordObject(part, "Move handprint clear of handle");
            part.position = bounds.center + Vector3.Scale(bounds.extents, normal) + normal * 0.0025f
                + Vector3.Cross(Vector3.up, normal) * offset;
        }

        [MenuItem("Tools/Zero/Records Room/Add Horror Anomalies 06 07 12 15")]
        private static void Build()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.name != SceneName || EditorApplication.isPlayingOrWillChangePlaymode) return;
            Transform room = SceneRoot(scene, SceneName);
            Transform root = Require(room, "IR3_Anomalies");
            Transform source = SceneRoot(scene, "vintage_woman_3d_model");
            var head = Require(source, "Head").GetComponent<SkinnedMeshRenderer>();
            if (head == null) throw new InvalidOperationException("IR3_HEAD_SOURCE_MISSING");
            Transform player = SceneRoot(scene, "IR3_DemoPlayer");
            var camera = Require(player, "IR3_PreviewCamera").GetComponent<Camera>();
            Require(room, Decor + "Tailieu (1)");
            Require(room, Decor + "IR3_DetailStorage_IR3_Cabinet4");
            Require(room, Decor + "IR3_DetailStorage_IR3_Cabinet3");
            var entries = new List<AnomalyBehaviour>(root.GetComponent<LivingRoomAnomalyPreview>().Anomalies);
            Add(entries, Blood(root, room));
            Add(entries, Handprints(root, room));
            Add(entries, Struggling(root, room));
            Add(entries, HeadEncounter(root, room, head, camera));
            entries.Sort((a, b) => string.CompareOrdinal(a.Data.Id, b.Data.Id));
            SetRoster(root.GetComponent<LivingRoomAnomalyPreview>(), entries);
            Refine();
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = root.gameObject;
            Debug.Log("IR3_HORROR_AUTHORED: 06/07/12/15 added; shared hollow box; F2 restores; save scene manually", root);
        }

        private static Transform SceneRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects()) if (root.name == name) return root.transform;
            throw new InvalidOperationException("IR3_ROOT_MISSING: " + name);
        }

        private static Transform Require(Transform parent, string path)
        {
            Transform target = parent.Find(path);
            if (target == null) throw new InvalidOperationException("IR3_TARGET_MISSING: " + path);
            return target;
        }

        private static void Add(List<AnomalyBehaviour> entries, AnomalyBehaviour entry)
        {
            if (!entries.Contains(entry)) entries.Add(entry);
        }

        internal static void SetRoster(LivingRoomAnomalyPreview preview, List<AnomalyBehaviour> entries)
        {
            var serialized = new SerializedObject(preview);
            SerializedProperty array = serialized.FindProperty("anomalies");
            array.arraySize = entries.Count;
            for (int i = 0; i < entries.Count; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = entries[i];
            serialized.ApplyModifiedProperties();
        }

        private static AnomalyBehaviour Blood(Transform root, Transform room)
        {
            Transform existing = root.Find("IR3_06_BloodRun");
            if (existing != null) return existing.GetComponent<AnomalyBehaviour>();
            GameObject node = RecordsRoomHorrorAssets.Node(root, "IR3_06_BloodRun");
            GameObject marks = RecordsRoomHorrorAssets.Node(node.transform, "BloodLetters");
            for (int i = 0; i < 3; i++)
            {
                Transform stack = Require(room, Decor + $"Tailieu ({i + 1})");
                Bounds bounds = BoundsOf(stack);
                GameObject quad = RecordsRoomMarkAssets.Quad(marks.transform, "Blood_" + "RUN"[i],
                    RecordsRoomMarkAssets.Ink(i), new Vector2(bounds.size.x * 0.8f, bounds.size.z * 0.82f));
                quad.transform.SetPositionAndRotation(new Vector3(bounds.center.x, bounds.max.y + 0.002f, bounds.center.z), Quaternion.Euler(90f, 0f, 0f));
            }
            marks.SetActive(false);
            var anomaly = node.AddComponent<LivingRoomArrangementAnomaly>();
            anomaly.Configure(Array.Empty<Transform>(), Array.Empty<Vector3>(), Array.Empty<Vector3>(), new[] { marks }, Array.Empty<GameObject>());
            Data(anomaly, 6, "Three Blood Letters", "R, U, N appear on the three paper stacks at the rear table.");
            return anomaly;
        }

        private static AnomalyBehaviour Handprints(Transform root, Transform room)
        {
            Transform existing = root.Find("IR3_07_BlackHandprints");
            if (existing != null) return existing.GetComponent<AnomalyBehaviour>();
            GameObject node = RecordsRoomHorrorAssets.Node(root, "IR3_07_BlackHandprints");
            var marks = new List<GameObject>();
            Transform[] parts = Require(room, "IR3_Furniture").GetComponentsInChildren<Transform>(true);
            foreach (Transform part in parts)
            {
                if (!part.name.Contains("_Drawer") || part.GetComponent<MeshRenderer>() == null) continue;
                var renderer = part.GetComponent<MeshRenderer>();
                if (!renderer.enabled || !part.gameObject.activeInHierarchy) continue;
                Stamp(part, renderer.bounds, marks);
            }
            var anomaly = node.AddComponent<LivingRoomArrangementAnomaly>();
            anomaly.Configure(Array.Empty<Transform>(), Array.Empty<Vector3>(), Array.Empty<Vector3>(), marks.ToArray(), Array.Empty<GameObject>());
            Data(anomaly, 7, "Black Hands On The Cabinets", "Smeared black palm and finger prints cover cabinet drawer fronts.");
            Debug.Log("IR3_HANDPRINTS_AUTHORED: count=" + marks.Count, node);
            return anomaly;
        }

        private static void Stamp(Transform drawer, Bounds bounds, List<GameObject> marks)
        {
            Vector3 normal = drawer.position.x < 0f ? Vector3.right : Vector3.left;
            if (bounds.size.x > bounds.size.z) normal = Vector3.back;
            Vector3 point = bounds.center + Vector3.Scale(bounds.extents, normal) + normal * 0.0025f;
            GameObject mark = RecordsRoomMarkAssets.Quad(drawer, "IR3_07_BlackPalm", RecordsRoomMarkAssets.Ink(-1), new Vector2(0.19f, 0.25f));
            mark.transform.SetPositionAndRotation(point, Quaternion.LookRotation(-normal, Vector3.up) * Quaternion.Euler(0f, 0f, (marks.Count % 5 - 2) * 13f));
            // Counteract the primitive drawer's nonuniform scale: keep the print palm-sized in world space.
            Vector3 parentScale = drawer.lossyScale;
            mark.transform.localScale = new Vector3(0.19f / Mathf.Abs(parentScale.z), 0.25f / Mathf.Abs(parentScale.y), 1f);
            mark.SetActive(false);
            marks.Add(mark);
        }

        private static AnomalyBehaviour Struggling(Transform root, Transform room)
        {
            Transform existing = root.Find("IR3_12_StrugglingBox");
            if (existing != null) return existing.GetComponent<AnomalyBehaviour>();
            GameObject node = RecordsRoomHorrorAssets.Node(root, "IR3_12_StrugglingBox");
            Transform box = ReplaceBox(node.transform, Require(room, Decor + "IR3_DetailStorage_IR3_Cabinet3"));
            var anomaly = node.AddComponent<ArchiveBoxShakeAnomaly>();
            anomaly.Configure(box, Require(box, "LidHinge"));
            Data(anomaly, 12, "Someone Inside The File Box", "A sealed file box shakes as if a trapped person is struggling inside.");
            return anomaly;
        }

        private static AnomalyBehaviour HeadEncounter(Transform root, Transform room, SkinnedMeshRenderer source, Camera camera)
        {
            Transform existing = root.Find("IR3_15_SixLeggedHead");
            if (existing != null) return existing.GetComponent<AnomalyBehaviour>();
            GameObject node = RecordsRoomHorrorAssets.Node(root, "IR3_15_SixLeggedHead");
            Transform box = ReplaceBox(node.transform, Require(room, Decor + "IR3_DetailStorage_IR3_Cabinet4"));
            ArchiveHeadLegs head = RecordsRoomHorrorAssets.Creature(node.transform, source);
            head.transform.position = new Vector3(box.position.x + 1.2f, 0.28f, box.position.z);
            head.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            var view = node.AddComponent<AnomalyDeathPresentation>();
            Transform anchor = RecordsRoomHorrorAssets.Node(head.transform, "JumpscareCameraAnchor").transform;
            anchor.localPosition = new Vector3(0f, 0f, 0.48f);
            Set(view, "focusTarget", head.transform);
            Set(view, "jumpscareCameraAnchor", anchor);
            var anomaly = node.AddComponent<ArchiveHeadAnomaly>();
            anomaly.Configure(box, Require(box, "LidHinge"), head);
            anomaly.BindPlayer(camera, Death(camera), view);
            var settings = new SerializedObject(anomaly);
            settings.FindProperty("landingOffset").vector3Value = new Vector3(0.95f, 0.34f - box.position.y, 0f);
            settings.ApplyModifiedProperties();
            Data(anomaly, 15, "The Six Legged Head", "Look away during leg growth. Watching until growth finishes commits an unavoidable fatal lunge.");
            return anomaly;
        }

        private static Transform ReplaceBox(Transform parent, Transform original)
        {
            Bounds bounds = BoundsOf(Require(original, "BoxBody"));
            GameObject prefab = RecordsRoomHorrorAssets.BoxPrefab();
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            Undo.RegisterCreatedObjectUndo(instance, "Place hollow archive box");
            instance.name = "HollowArchiveBox";
            instance.transform.position = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            instance.transform.localScale = new Vector3(bounds.size.x / 0.69f, bounds.size.y / 0.42f, bounds.size.z / 0.75f);
            Undo.RecordObject(original.gameObject, "Replace solid storage prop");
            original.gameObject.SetActive(false);
            Debug.Log($"IR3_HOLLOW_BOX_PLACED: source={original.name}, bounds={bounds}", instance);
            return instance.transform;
        }

        private static Bounds BoundsOf(Transform target)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) throw new InvalidOperationException("IR3_RENDERER_MISSING: " + target.name);
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        private static PlayerDeathController Death(Camera camera)
        {
            GameObject player = camera.transform.parent.gameObject;
            var death = player.GetComponent<PlayerDeathController>();
            if (death == null) death = Undo.AddComponent<PlayerDeathController>(player);
            Set(death, "playerController", player.GetComponent<FirstPersonController>());
            Set(death, "playerCamera", camera);
            Transform overlay = player.transform.Find("IR3_DeathOverlay");
            if (overlay == null) overlay = Overlay(player.transform);
            Set(death, "blackoutCanvasGroup", overlay.GetComponent<CanvasGroup>());
            return death;
        }

        private static Transform Overlay(Transform player)
        {
            var canvas = new GameObject("IR3_DeathOverlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
            Undo.RegisterCreatedObjectUndo(canvas, "Add Records Room death overlay");
            canvas.transform.SetParent(player, false);
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.GetComponent<Canvas>().sortingOrder = 1000;
            CanvasGroup group = canvas.GetComponent<CanvasGroup>();
            group.alpha = 0f; group.blocksRaycasts = false;
            var image = new GameObject("Black", typeof(RectTransform), typeof(Image));
            image.transform.SetParent(canvas.transform, false);
            RectTransform rect = image.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            image.GetComponent<Image>().color = Color.black;
            image.GetComponent<Image>().raycastTarget = false;
            return canvas.transform;
        }

        private static void Set(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var settings = new SerializedObject(target);
            settings.FindProperty(field).objectReferenceValue = value;
            settings.ApplyModifiedProperties();
        }

        internal static void Data(AnomalyBehaviour anomaly, int id, string title, string description)
        {
            string path = $"Assets/Game/ScriptableObjects/Anomalies/InfinityRoom3/IR3_{id:00}.asset";
            AnomalyData data = AssetDatabase.LoadAssetAtPath<AnomalyData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<AnomalyData>();
                var serialized = new SerializedObject(data);
                serialized.FindProperty("id").stringValue = $"IR3-{id:00}";
                serialized.FindProperty("displayName").stringValue = title;
                serialized.FindProperty("description").stringValue = description;
                serialized.FindProperty("pacingTier").enumValueIndex = (int)(id == 15 ? AnomalyPacingTier.MajorAnomaly : AnomalyPacingTier.MinorAnomaly);
                serialized.FindProperty("canKillPlayer").boolValue = id == 15;
                serialized.FindProperty("difficulty").intValue = id == 15 ? 3 : 1;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.CreateAsset(data, path);
            }
            anomaly.Initialize(data);
            EditorUtility.SetDirty(anomaly);
        }
    }
}
