using System;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Zero.AnomalyPrototype.Anomalies;
using Zero.AnomalyPrototype.Demo;
using Zero.AnomalyPrototype.Player;

namespace Zero.Editor
{
    public static class LivingRoomTelevisionAnomalyTool
    {
        private const string Room = "InfinityRoom2_LivingRoom_Demo";

        [MenuItem("Tools/Zero/Living Room/Add Television Anomalies 12 18")]
        private static void Setup()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.name != Room) return;
            Transform room = GameObject.Find(Room).transform;
            Transform root = room.Find("IR2_Anomalies");
            GameObject mother = LivingRoomTelevisionAssets.Source("Mother");
            ValidateAnimations();
            if (root.Find("IR2_12_FamilyTelevision") == null) Family(root, mother);
            if (root.Find("IR2_18_MotherFromTelevision") == null) Mother(root, mother);
            foreach (string name in new[] { "IR2_12_FamilyTelevision", "IR2_18_MotherFromTelevision" })
            {
                LivingRoomTelevisionAssets.RepairStudio(root.Find(name), name == "IR2_12_FamilyTelevision");
                LivingRoomTelevisionAssets.UpgradeGlass(root.Find(name));
            }
            Bake(room);
            BindPreview(root);
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("IR2_TELEVISION_AUTHORED: 12/18 ready; save scene manually", root);
        }

        private static void ValidateAnimations()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Game/MixamoAnimation/FamilyAnimation.controller");
            foreach (string name in new[] { "Sit", "RunNormal", "RunLunge" })
            {
                bool found = false;
                foreach (ChildAnimatorState state in controller.layers[0].stateMachine.states)
                    if (state.state.name == name && state.state.motion != null) found = true;
                if (!found) throw new InvalidOperationException("IR2_FAMILY_ANIMATION_REQUIRED: " + name);
            }
        }

        private static void Family(Transform root, GameObject mother)
        {
            GameObject node = LivingRoomTelevisionAssets.Node(root, "IR2_12_FamilyTelevision");
            GameObject[] family = { LivingRoomTelevisionAssets.Source("Father"), LivingRoomTelevisionAssets.Source("Children"), mother };
            TelevisionFeed feed = LivingRoomTelevisionAssets.Feed(node.transform, family, true);
            var anomaly = Undo.AddComponent<FamilyTelevisionAnomaly>(node);
            anomaly.Configure(feed);
            LivingRoomAnomalyAuthoringTool.Data(anomaly, 12, "The Family Programme", AnomalyPacingTier.MinorAnomaly, AnomalyType.Psychological);
        }

        private static void Mother(Transform root, GameObject source)
        {
            GameObject node = LivingRoomTelevisionAssets.Node(root, "IR2_18_MotherFromTelevision");
            TelevisionFeed feed = LivingRoomTelevisionAssets.Feed(node.transform, new[] { source }, false);
            GameObject clone = UnityEngine.Object.Instantiate(source, node.transform);
            clone.name = "AttackingMother";
            clone.transform.SetPositionAndRotation(LivingRoomTelevisionAssets.ScreenPosition - Vector3.up * 0.45f, Quaternion.Euler(0f, 180f, 0f));
            Animator actor = clone.GetComponent<Animator>();
            actor.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Game/MixamoAnimation/FamilyAnimation.controller");
            actor.enabled = true;
            actor.applyRootMotion = false;
            actor.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var agent = Undo.AddComponent<NavMeshAgent>(clone);
            agent.radius = 0.25f;
            agent.height = 1.7f;
            agent.stoppingDistance = 0.35f;
            agent.acceleration = 18f;
            agent.angularSpeed = 540f;
            agent.enabled = false;
            clone.SetActive(false);
            var view = Undo.AddComponent<AnomalyDeathPresentation>(node);
            view.SetDeathStyle(AnomalyDeathStyle.Style2_FreezeBlackout);
            Set(view, "focusTarget", actor.GetBoneTransform(HumanBodyBones.Head));
            BindMother(node, actor, agent, feed, view);
        }

        private static void BindMother(GameObject node, Animator actor, NavMeshAgent agent, TelevisionFeed feed, AnomalyDeathPresentation view)
        {
            var bounds = Undo.AddComponent<BoxCollider>(node);
            bounds.center = new Vector3(0f, 1f, 0f);
            bounds.size = new Vector3(9.8f, 5f, 9f);
            bounds.isTrigger = true;
            bounds.enabled = false;
            var anomaly = Undo.AddComponent<TelevisionMotherAnomaly>(node);
            Set(anomaly, "actor", actor);
            Set(anomaly, "agent", agent);
            Set(anomaly, "observer", GameObject.Find("IR2_DemoPlayer/IR2_PreviewCamera").transform);
            Set(anomaly, "death", GameObject.Find("IR2_DemoPlayer").GetComponent<PlayerDeathController>());
            Set(anomaly, "presentation", view);
            Set(anomaly, "feed", feed);
            Set(anomaly, "roomBounds", bounds);
            var fields = new SerializedObject(anomaly);
            fields.FindProperty("landingPosition").vector3Value = new Vector3(2.3f, 0f, 3.25f);
            fields.ApplyModifiedProperties();
            LivingRoomAnomalyAuthoringTool.Data(anomaly, 18, "Mother Steps Out Of The Television", AnomalyPacingTier.MajorAnomaly, AnomalyType.Creature);
            var data = new SerializedObject(anomaly.Data);
            data.FindProperty("canKillPlayer").boolValue = true;
            data.FindProperty("deathStyle").enumValueIndex = (int)AnomalyDeathStyle.Style2_FreezeBlackout;
            data.ApplyModifiedProperties();
        }

        private static void Bake(Transform room)
        {
            Transform node = room.Find("IR2_TelevisionNavigation");
            if (node == null) node = LivingRoomTelevisionAssets.Node(room, "IR2_TelevisionNavigation").transform;
            var surface = node.GetComponent<NavMeshSurface>();
            if (surface == null) surface = Undo.AddComponent<NavMeshSurface>(node.gameObject);
            surface.collectObjects = CollectObjects.Volume;
            surface.center = new Vector3(0f, 1f, 0f);
            surface.size = new Vector3(12f, 5f, 12f);
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.overrideVoxelSize = true;
            surface.voxelSize = 0.08f;
            surface.BuildNavMesh();
            if (surface.navMeshData == null) throw new InvalidOperationException("IR2_NAVIGATION_BAKE_FAILED");
            SaveNavigation(surface);
        }

        private static void SaveNavigation(NavMeshSurface surface)
        {
            string path = LivingRoomTelevisionAssets.Folder + "IR2_TelevisionNavigation.asset";
            NavMeshData generated = surface.navMeshData;
            NavMeshData existing = AssetDatabase.LoadAssetAtPath<NavMeshData>(path);
            if (existing == null) { AssetDatabase.CreateAsset(generated, path); return; }
            surface.RemoveData();
            EditorUtility.CopySerialized(generated, existing);
            surface.navMeshData = existing;
            surface.AddData();
            EditorUtility.SetDirty(existing);
            UnityEngine.Object.DestroyImmediate(generated);
        }

        [MenuItem("Tools/Zero/Living Room/Review Television Screens")]
        private static void Review()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().name != Room) return;
            Transform root = GameObject.Find(Room + "/IR2_Anomalies").transform;
            foreach (string name in new[] { "IR2_12_FamilyTelevision", "IR2_18_MotherFromTelevision" })
            {
                Transform node = root.Find(name);
                var anomaly = node.GetComponent<AnomalyBehaviour>();
                try
                {
                    anomaly.Activate();
                    node.GetComponent<TelevisionFeed>().RenderFrame();
                    LivingRoomExtraAnomalyTool.Capture(name, new Vector3(2.3f, 1.25f, 3.2f), LivingRoomTelevisionAssets.ScreenPosition);
                }
                finally { anomaly.ResetAnomaly(); }
            }
            bool spawn = NavMesh.SamplePosition(new Vector3(2.3f, 0f, 3.25f), out NavMeshHit start, 1.5f, NavMesh.AllAreas);
            bool exit = NavMesh.SamplePosition(new Vector3(3.4f, 0f, -4.35f), out NavMeshHit end, 1f, NavMesh.AllAreas);
            var path = new NavMeshPath();
            bool route = spawn && exit && NavMesh.CalculatePath(start.position, end.position, NavMesh.AllAreas, path);
            Debug.Log($"IR2_TV_NAVIGATION: spawn={spawn}, exit={exit}, route={route}, status={path.status}", root);
        }

        private static void BindPreview(Transform root)
        {
            AnomalyBehaviour[] entries = root.GetComponentsInChildren<AnomalyBehaviour>(true);
            Array.Sort(entries, (a, b) => string.CompareOrdinal(a.Data.Id, b.Data.Id));
            var preview = root.GetComponent<LivingRoomAnomalyPreview>();
            Undo.RecordObject(preview, "Add television anomalies to F3");
            var serialized = new SerializedObject(preview);
            SerializedProperty list = serialized.FindProperty("anomalies");
            list.arraySize = entries.Length;
            for (int i = 0; i < entries.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = entries[i];
            serialized.ApplyModifiedProperties();
        }

        private static void Set(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedProperties();
        }
    }
}
