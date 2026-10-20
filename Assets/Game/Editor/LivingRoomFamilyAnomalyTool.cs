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
    public static class LivingRoomFamilyAnomalyTool
    {
        private const string Room = "InfinityRoom2_LivingRoom_Demo";

        [MenuItem("Tools/Zero/Living Room/Restore IR2-16 Original Child Texture")]
        private static void RestoreChildTexture()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().name != Room) return;
            Transform face = Require(Require(GameObject.Find(Room).transform, "IR2_Anomalies/IR2_16_ChildUnderTable"), "ChildFace");
            LivingRoomFamilyAssets.RestoreOriginalMaterials(Source("Children"), face);
            EditorSceneManager.MarkSceneDirty(face.gameObject.scene);
            Debug.Log("IR2_CHILD_TEXTURE_RESTORED: original Children materials assigned", face);
        }

        [MenuItem("Tools/Zero/Living Room/Review Family Anomalies")]
        private static void Review()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().name != Room) return;
            Transform root = GameObject.Find(Room + "/IR2_Anomalies").transform;
            string[] nodes = { "IR2_10_OccupiedSofa", "IR2_15_FeetUnderCurtain", "IR2_16_ChildUnderTable", "IR2_17_SeatedFather" };
            Vector3[] views = { new Vector3(-0.5f, 1.6f, 0.7f), new Vector3(-1.05f, 0.45f, 3.2f),
                new Vector3(-0.65f, 0.3f, -1.6f), new Vector3(0.8f, 1.6f, -4.3f) };
            Vector3[] points = { new Vector3(-2.8f, 0.55f, 0.7f), new Vector3(-1.05f, 0.15f, 4.48f),
                new Vector3(-0.65f, 0.26f, -0.38f), new Vector3(-0.35f, 1f, -2.6f) };
            for (int i = 0; i < nodes.Length; i++)
            {
                AnomalyBehaviour anomaly = Require(root, nodes[i]).GetComponent<AnomalyBehaviour>();
                try { anomaly.Activate(); LivingRoomExtraAnomalyTool.Capture(nodes[i], views[i], points[i]); }
                finally { anomaly.ResetAnomaly(); }
            }
        }

        [MenuItem("Tools/Zero/Living Room/Add Anomalies 10 15 16 17")]
        private static void Add()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.name != Room) return;
            Transform room = GameObject.Find(Room).transform;
            Transform root = Require(room, "IR2_Anomalies");
            GameObject father = Source("Father"), child = Source("Children");
            Transform camera = GameObject.Find("IR2_DemoPlayer/IR2_PreviewCamera").transform;
            if (father.GetComponent<Animator>().runtimeAnimatorController == null || !child.GetComponent<Animator>().isHuman)
                throw new InvalidOperationException("IR2_FAMILY_RIG_CONFIGURATION_INVALID");
            Sofa(room, root);
            if (root.Find("IR2_15_FeetUnderCurtain") == null) Feet(room, root, child, camera);
            if (root.Find("IR2_16_ChildUnderTable") == null) Child(root, child, camera);
            ChildAudio(root);
            if (root.Find("IR2_17_SeatedFather") == null) Father(room, root, father, camera);
            FitFatherSeat(room, root);
            BindPreview(root);
            foreach (string name in new[] { "Father", "Mother", "Children" })
            {
                GameObject source = Source(name);
                Undo.RecordObject(source, "Hide family authoring reference");
                source.SetActive(false);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("IR2_FAMILY_AUTHORED: 10/15/16/17 bound; save scene manually", root);
        }

        private static GameObject Source(string name)
        {
            foreach (GameObject item in SceneManager.GetActiveScene().GetRootGameObjects())
                if (item.name == name) return item;
            throw new InvalidOperationException("IR2_FAMILY_SOURCE_MISSING: " + name);
        }

        private static Transform Require(Transform root, string path)
        {
            Transform item = root.Find(path);
            if (item == null) throw new InvalidOperationException("IR2_FAMILY_REFERENCE_MISSING: " + path);
            return item;
        }

        private static GameObject Node(Transform root, string name)
        {
            var item = new GameObject(name);
            item.transform.SetParent(root, false);
            Undo.RegisterCreatedObjectUndo(item, "Add family anomaly");
            return item;
        }

        private static void Sofa(Transform room, Transform root)
        {
            Transform existing = root.Find("IR2_10_OccupiedSofa");
            GameObject node = existing != null ? existing.gameObject : Node(root, "IR2_10_OccupiedSofa");
            Transform sofa = Require(room, "IR2_Furniture/IR2_DetailSofa");
            Renderer normal = Require(sofa, "SeatCushions").GetComponent<Renderer>();
            SkinnedMeshRenderer occupied = LivingRoomFamilyAssets.Deformed(normal, node.transform, true);
            var anomaly = node.GetComponent<BreathingSofaAnomaly>();
            if (anomaly == null) anomaly = Undo.AddComponent<BreathingSofaAnomaly>(node);
            anomaly.Configure(normal, occupied, Require(sofa, "ThrowPillows"));
            LivingRoomAnomalyAuthoringTool.Data(anomaly, 10, "Someone On The Long Sofa", AnomalyPacingTier.MinorAnomaly);
        }

        private static void Feet(Transform room, Transform root, GameObject child, Transform camera)
        {
            GameObject node = Node(root, "IR2_15_FeetUnderCurtain");
            GameObject feet = LivingRoomFamilyAssets.ChildPart(child, node.transform, true);
            feet.transform.position = new Vector3(-1.05f, 0.09f, 4.48f);
            feet.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            feet.SetActive(false);
            Renderer normal = Require(room, "IR2_Furniture/IR2_DetailCurtains/CurtainFabric").GetComponent<Renderer>();
            SkinnedMeshRenderer bulge = LivingRoomFamilyAssets.Deformed(normal, node.transform, false);
            var anomaly = Undo.AddComponent<FamilyPeekAnomaly>(node);
            anomaly.Configure(feet.transform, camera, new Vector3(0f, 0f, 0.55f));
            Set(anomaly, "normalCurtain", normal);
            Set(anomaly, "bulgingCurtain", bulge);
            LivingRoomAnomalyAuthoringTool.Data(anomaly, 15, "Small Feet Under The Curtain", AnomalyPacingTier.MinorAnomaly, AnomalyType.Creature);
        }

        private static void Child(Transform root, GameObject source, Transform camera)
        {
            GameObject node = Node(root, "IR2_16_ChildUnderTable");
            GameObject face = LivingRoomFamilyAssets.ChildPart(source, node.transform, false);
            face.transform.SetPositionAndRotation(new Vector3(-0.65f, 0.26f, -0.38f), Quaternion.Euler(0f, 180f, 0f));
            face.transform.localScale = Vector3.one * 0.9f;
            face.SetActive(false);
            var anomaly = Undo.AddComponent<FamilyPeekAnomaly>(node);
            anomaly.Configure(face.transform, camera, new Vector3(0f, 0f, 0.55f));
            var serialized = new SerializedObject(anomaly);
            serialized.FindProperty("reactToLookingDown").boolValue = true;
            serialized.FindProperty("retreatSeconds").floatValue = 0.7f;
            serialized.ApplyModifiedProperties();
            LivingRoomAnomalyAuthoringTool.Data(anomaly, 16, "The Child Under The Table", AnomalyPacingTier.MinorAnomaly, AnomalyType.Creature);
        }

        private static void Father(Transform room, Transform root, GameObject source, Transform camera)
        {
            GameObject node = Node(root, "IR2_17_SeatedFather");
            Animator actor = LivingRoomFamilyAssets.SeatedFather(source, node.transform, Require(room, "IR2_Furniture/IR2_DetailArmchairA"));
            var presentation = Undo.AddComponent<AnomalyDeathPresentation>(node);
            presentation.SetDeathStyle(AnomalyDeathStyle.Style2_FreezeBlackout);
            Transform head = actor.GetBoneTransform(HumanBodyBones.Head);
            Set(presentation, "focusTarget", head);
            var anomaly = Undo.AddComponent<SeatedFatherAnomaly>(node);
            Transform seat = CreateSeat(room, node.transform);
            anomaly.Configure(actor, camera, Death(camera), presentation, seat);
            LivingRoomAnomalyAuthoringTool.Data(anomaly, 17, "The Father In The Armchair", AnomalyPacingTier.MajorAnomaly, AnomalyType.Creature);
            var data = new SerializedObject(anomaly.Data);
            data.FindProperty("canKillPlayer").boolValue = true;
            data.FindProperty("deathStyle").enumValueIndex = (int)AnomalyDeathStyle.Style2_FreezeBlackout;
            data.ApplyModifiedProperties();
        }

        private static void ChildAudio(Transform root)
        {
            Transform node = Require(root, "IR2_16_ChildUnderTable");
            GameObject actor = node.Find("ChildFace").gameObject;
            AudioSource audio = actor.GetComponent<AudioSource>();
            if (audio == null) audio = Undo.AddComponent<AudioSource>(actor);
            audio.clip = LivingRoomFamilyAssets.ChairScrape();
            audio.playOnAwake = false;
            audio.spatialBlend = 1f;
            audio.minDistance = 0.8f;
            audio.maxDistance = 6f;
            audio.volume = 0.65f;
            Set(node.GetComponent<FamilyPeekAnomaly>(), "retreatAudio", audio);
        }

        private static Transform CreateSeat(Transform room, Transform node)
        {
            Transform seat = node.Find("SeatAnchor");
            if (seat == null) seat = Node(node, "SeatAnchor").transform;
            Transform chair = Require(room, "IR2_Furniture/IR2_DetailArmchairA");
            seat.position = LivingRoomFamilyAssets.SeatPoint(chair);
            return seat;
        }

        private static void FitFatherSeat(Transform room, Transform root)
        {
            Transform node = Require(root, "IR2_17_SeatedFather");
            Transform seat = CreateSeat(room, node);
            var anomaly = node.GetComponent<SeatedFatherAnomaly>();
            Set(anomaly, "seatAnchor", seat);
            Animator actor = node.Find("SeatedFather").GetComponent<Animator>();
            Undo.RecordObject(actor.transform, "Fit hips to actual seat cushion");
            actor.transform.position += seat.position - actor.GetBoneTransform(HumanBodyBones.Hips).position;
        }

        private static PlayerDeathController Death(Transform camera)
        {
            GameObject player = camera.parent.gameObject;
            var death = player.GetComponent<PlayerDeathController>();
            if (death == null) death = Undo.AddComponent<PlayerDeathController>(player);
            Set(death, "playerController", player.GetComponent<FirstPersonController>());
            Set(death, "playerCamera", camera.GetComponent<Camera>());
            Transform existing = player.transform.Find("IR2_DeathOverlay");
            if (existing != null) { Set(death, "blackoutCanvasGroup", existing.GetComponent<CanvasGroup>()); return death; }
            var canvas = new GameObject("IR2_DeathOverlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
            Undo.RegisterCreatedObjectUndo(canvas, "Add demo blackout");
            canvas.transform.SetParent(player.transform, false);
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.GetComponent<Canvas>().sortingOrder = 1000;
            CanvasGroup group = canvas.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            var image = new GameObject("Black", typeof(RectTransform), typeof(Image));
            image.transform.SetParent(canvas.transform, false);
            RectTransform rect = image.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            image.GetComponent<Image>().color = Color.black;
            image.GetComponent<Image>().raycastTarget = false;
            Set(death, "blackoutCanvasGroup", group);
            return death;
        }

        private static void Set(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedProperties();
        }

        private static void BindPreview(Transform root)
        {
            var entries = new List<AnomalyBehaviour>();
            foreach (AnomalyBehaviour entry in root.GetComponentsInChildren<AnomalyBehaviour>(true)) entries.Add(entry);
            entries.Sort((a, b) => string.CompareOrdinal(a.Data.Id, b.Data.Id));
            var serialized = new SerializedObject(root.GetComponent<LivingRoomAnomalyPreview>());
            SerializedProperty array = serialized.FindProperty("anomalies");
            array.arraySize = entries.Count;
            for (int i = 0; i < entries.Count; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = entries[i];
            serialized.ApplyModifiedProperties();
        }
    }
}
