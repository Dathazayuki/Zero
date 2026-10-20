using System;
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
    public static class LivingRoomRugAnomalyTool
    {
        private const string Room = "InfinityRoom2_LivingRoom_Demo";

        [MenuItem("Tools/Zero/Living Room/Add Family Rug Anomaly 20")]
        private static void Setup()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.name != Room) return;
            Transform room = GameObject.Find(Room).transform;
            Transform root = room.Find("IR2_Anomalies");
            Transform existing = root.Find("IR2_20_YourSeat");
            if (existing != null)
            {
                ConfigureDeath(existing.gameObject);
                EditorSceneManager.MarkSceneDirty(scene);
                AssetDatabase.SaveAssets();
                return;
            }
            GameObject node = LivingRoomTelevisionAssets.Node(root, "IR2_20_YourSeat");
            GameObject family = LivingRoomTelevisionAssets.Node(node.transform, "Family");
            Animator[] actors = { LivingRoomRugAssets.Actor("Father", family.transform, "SeatedFather"),
                LivingRoomRugAssets.Actor("Mother", family.transform, "SeatedMother") };
            Transform sofa = room.Find("IR2_Furniture/IR2_DetailSofa");
            LivingRoomRugAssets.Seat(actors[0], sofa, -0.65f);
            LivingRoomRugAssets.Seat(actors[1], sofa, 0.65f);
            NavMeshAgent[] agents = Agents(actors);
            LivingRoomRugAssets.ChildShadow(family.transform);
            Transform hands = LivingRoomRugAssets.Hands(family.transform);
            Renderer darkening = Overlay(node.transform, room);
            var view = Undo.AddComponent<AnomalyDeathPresentation>(node);
            view.SetDeathStyle(AnomalyDeathStyle.Style2_FreezeBlackout);
            view.SetFreezeDuration(3.1f);
            view.SetFocusTarget(actors[1].GetBoneTransform(HumanBodyBones.Head));
            FamilyRugPresentation display = Display(node, family, actors, agents);
            Set(display, "hands", hands);
            Set(display, "darkening", darkening);
            Set(display, "chair", room.Find("IR2_Furniture/IR2_DetailArmchairB"));
            Bind(node, display, view, darkening.bounds);
            family.SetActive(false);
            BindPreview(root);
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("IR2_RUG_AUTHORED: 20 bound; save scene manually", node);
        }

        private static void ConfigureDeath(GameObject node)
        {
            var view = node.GetComponent<AnomalyDeathPresentation>();
            Undo.RecordObject(view, "Set family death camera duration");
            view.SetFreezeDuration(3.1f);
            EditorUtility.SetDirty(view);
        }

        private static NavMeshAgent[] Agents(Animator[] actors)
        {
            var agents = new NavMeshAgent[actors.Length];
            for (int i = 0; i < actors.Length; i++)
            {
                agents[i] = Undo.AddComponent<NavMeshAgent>(actors[i].gameObject);
                agents[i].radius = 0.25f;
                agents[i].height = 1.7f;
                agents[i].speed = 4.8f;
                agents[i].acceleration = 22f;
                agents[i].angularSpeed = 540f;
                agents[i].stoppingDistance = 0.4f;
                agents[i].enabled = false;
            }
            return agents;
        }

        [MenuItem("Tools/Zero/Living Room/Refresh Rug Hands")]
        private static void RefreshHands()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().name != Room) return;
            Transform node = GameObject.Find(Room + "/IR2_Anomalies/IR2_20_YourSeat").transform;
            Transform family = node.Find("Family");
            bool active = family.gameObject.activeSelf;
            try
            {
                family.gameObject.SetActive(true);
                Transform previous = family.Find("GrabbingHands");
                if (previous != null) Undo.DestroyObjectImmediate(previous.gameObject);
                Set(node.GetComponent<FamilyRugPresentation>(), "hands", LivingRoomRugAssets.Hands(family));
            }
            finally { family.gameObject.SetActive(active); }
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(node.gameObject.scene);
        }

        private static Renderer Overlay(Transform node, Transform room)
        {
            Bounds bounds = room.Find("IR2_Decor/IR2_DetailRug/PersianRug").GetComponent<Renderer>().bounds;
            var surface = GameObject.CreatePrimitive(PrimitiveType.Quad);
            surface.name = "DarkeningRug";
            surface.transform.SetParent(node, false);
            surface.transform.SetPositionAndRotation(bounds.center + Vector3.up * 0.008f, Quaternion.Euler(90f, 0f, 0f));
            surface.transform.localScale = new Vector3(bounds.size.x, bounds.size.z, 1f);
            UnityEngine.Object.DestroyImmediate(surface.GetComponent<Collider>());
            string path = LivingRoomTelevisionAssets.Folder + "M_IR2_RugDarkening.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Zero/IR2/RugDarkening"));
                AssetDatabase.CreateAsset(material, path);
            }
            Renderer renderer = surface.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.enabled = false;
            return renderer;
        }

        private static FamilyRugPresentation Display(GameObject node, GameObject family, Animator[] actors, NavMeshAgent[] agents)
        {
            var display = Undo.AddComponent<FamilyRugPresentation>(node);
            Set(display, "family", family);
            var serialized = new SerializedObject(display);
            SerializedProperty cast = serialized.FindProperty("actors"), navigation = serialized.FindProperty("agents");
            cast.arraySize = navigation.arraySize = 2;
            SerializedProperty origins = serialized.FindProperty("attackOrigins");
            origins.arraySize = 2;
            for (int i = 0; i < 2; i++)
            {
                cast.GetArrayElementAtIndex(i).objectReferenceValue = actors[i];
                navigation.GetArrayElementAtIndex(i).objectReferenceValue = agents[i];
                origins.GetArrayElementAtIndex(i).vector3Value = new Vector3(-1.85f, 0f, i == 0 ? 1.35f : -0.15f);
            }
            serialized.ApplyModifiedProperties();
            return display;
        }

        private static void Bind(GameObject node, FamilyRugPresentation display, AnomalyDeathPresentation view, Bounds carpet)
        {
            var zone = Undo.AddComponent<BoxCollider>(node);
            zone.center = new Vector3(carpet.center.x, 0.5f, carpet.center.z);
            zone.size = new Vector3(carpet.size.x, 1.5f, carpet.size.z);
            zone.enabled = false;
            zone.isTrigger = true;
            var anomaly = Undo.AddComponent<FamilyRugAnomaly>(node);
            GameObject player = GameObject.Find("IR2_DemoPlayer");
            Set(anomaly, "family", display);
            Set(anomaly, "observer", player.transform.Find("IR2_PreviewCamera"));
            Set(anomaly, "player", player.GetComponent<FirstPersonController>());
            Set(anomaly, "death", player.GetComponent<PlayerDeathController>());
            Set(anomaly, "presentation", view);
            Set(anomaly, "rug", zone);
            LivingRoomAnomalyAuthoringTool.Data(anomaly, 20, "Your Seat", AnomalyPacingTier.MajorAnomaly, AnomalyType.Creature);
            var data = new SerializedObject(anomaly.Data);
            data.FindProperty("canKillPlayer").boolValue = true;
            data.FindProperty("deathStyle").enumValueIndex = (int)AnomalyDeathStyle.Style2_FreezeBlackout;
            data.ApplyModifiedProperties();
        }

        private static void BindPreview(Transform root)
        {
            AnomalyBehaviour[] entries = root.GetComponentsInChildren<AnomalyBehaviour>(true);
            Array.Sort(entries, (a, b) => string.CompareOrdinal(a.Data.Id, b.Data.Id));
            var preview = root.GetComponent<LivingRoomAnomalyPreview>();
            Undo.RecordObject(preview, "Add IR2-20 to F3");
            var serialized = new SerializedObject(preview);
            SerializedProperty array = serialized.FindProperty("anomalies");
            array.arraySize = entries.Length;
            for (int i = 0; i < entries.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = entries[i];
            serialized.ApplyModifiedProperties();
        }

        [MenuItem("Tools/Zero/Living Room/Review Family Rug Anomaly")]
        private static void Review()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().name != Room) return;
            Transform node = GameObject.Find(Room + "/IR2_Anomalies/IR2_20_YourSeat").transform;
            var anomaly = node.GetComponent<FamilyRugAnomaly>();
            var display = node.GetComponent<FamilyRugPresentation>();
            try
            {
                anomaly.Activate();
                display.Warn(0.65f, new Vector3(0.9f, 0f, 1.5f));
                LivingRoomExtraAnomalyTool.Capture("IR2_20_FamilyWarning", new Vector3(3f, 2.3f, -3.8f), new Vector3(-1f, 0.6f, 0.3f));
                display.Grab(new Vector3(0.9f, 0f, 1.5f), 0.6f);
                LivingRoomExtraAnomalyTool.Capture("IR2_20_GrabbingHands", new Vector3(1.2f, 0.7f, 0.9f), new Vector3(0.9f, 0.1f, 1.5f));
            }
            finally { anomaly.ResetAnomaly(); }
            foreach (Vector3 point in new[] { new Vector3(-1.85f, 0f, 1.35f), new Vector3(-1.85f, 0f, -0.15f) })
            {
                bool valid = NavMesh.SamplePosition(point, out NavMeshHit hit, 1.2f, NavMesh.AllAreas);
                Debug.Log($"IR2_RUG_ATTACK_ORIGIN: requested={point}, valid={valid}, position={hit.position}", node);
            }
        }

        private static void Set(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedProperties();
        }
    }
}
