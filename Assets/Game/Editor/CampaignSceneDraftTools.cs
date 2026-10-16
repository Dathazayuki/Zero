using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.AI.Navigation;
using Zero.AnomalyPrototype.Flow;
using Zero.AnomalyPrototype.Player;

namespace Zero.Editor
{
    /// <summary>Explicit copy-and-configure only. Original scenes are never changed or saved.</summary>
    public static class CampaignSceneDraftTools
    {
        private const string Source = "Assets/Game/Scenes/BackRoomScene.unity";
        public const string Folder = "Assets/Game/Scenes/Campaign";

        [MenuItem("Tools/Zero/Campaign/Prepare Minigame Scene Drafts")]
        public static void PrepareDrafts()
        {
            if (EditorApplication.isPlaying) return;
            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();
            PrepareCopy("Minigame1_Backroom", false);
            PrepareCopy("Minigame2_Rooms", true);
            Debug.Log("CAMPAIGN_SCENE_DRAFTS_READY: save each draft manually, then Publish Saved Minigames. Source unchanged.");
        }

        private static void PrepareCopy(string name, bool rooms)
        {
            string path = Folder + "/" + name + ".unity";
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null)
            { Debug.LogWarning($"CAMPAIGN_DRAFT_EXISTS: preserving {path}"); return; }
            if (!AssetDatabase.CopyAsset(Source, path))
            { Debug.LogError("CAMPAIGN_SCENE_COPY_FAILED"); return; }
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            Transform root = Find(scene, "BackRoomRoot");
            if (root == null) { Debug.LogError("CAMPAIGN_SCENE_CONFIGURATION_INVALID: BackRoomRoot missing"); return; }
            Scene previous = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(scene);
            MonoBehaviour session = rooms ? ConfigureRooms(root) : ConfigureChase(root);
            var host = new GameObject("CampaignLevelBootstrap");
            SceneManager.MoveGameObjectToScene(host, scene);
            Undo.RegisterCreatedObjectUndo(host, "Configure campaign minigame");
            var level = Undo.AddComponent<CampaignLevelBootstrap>(host);
            var data = new SerializedObject(level);
            data.FindProperty("environmentRoot").objectReferenceValue = root;
            data.FindProperty("arrivalAnchor").objectReferenceValue = root.Find("BackRoomArrivalAnchor");
            data.FindProperty("entranceCheckpoint").objectReferenceValue = CreateEntrance(root, rooms);
            data.FindProperty("destinationFacade").objectReferenceValue = root.Find("DestinationElevatorFacade")?.gameObject;
            data.FindProperty("minigameSession").objectReferenceValue = session;
            data.ApplyModifiedProperties();
            DisableStandaloneSystems(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            SceneManager.SetActiveScene(previous);
        }

        private static MonoBehaviour ConfigureChase(Transform root)
        {
            Transform ending = root.Find("Ending3_Root");
            if (ending != null) { Undo.RecordObject(ending.gameObject, "Separate challenges"); ending.gameObject.SetActive(false); }
            var chase = root.GetComponentInChildren<BackroomGameplayManager>(true);
            var session = Undo.AddComponent<BackroomMinigameSession>(root.gameObject);
            var data = new SerializedObject(session);
            data.FindProperty("chase").objectReferenceValue = chase;
            data.ApplyModifiedProperties();
            return session;
        }

        private static MonoBehaviour ConfigureRooms(Transform root)
        {
            string[] excluded = { "Backrooms_GroundTruth_Map", "BackRoom_Exit_Facility", "HorrorGirl", "BackroomChaseGameplay" };
            foreach (string name in excluded)
            {
                Transform child = root.Find(name);
                if (child != null) Undo.DestroyObjectImmediate(child.gameObject);
            }
            foreach (NavMeshSurface surface in root.GetComponents<NavMeshSurface>()) Undo.DestroyObjectImmediate(surface);
            var challenges = root.GetComponentInChildren<TrueEndingGameplayController>(true);
            Transform blueSky = challenges != null ? challenges.transform.Find("BlueSky") : null;
            if (blueSky != null) { Undo.RecordObject(blueSky.gameObject, "Separate Blue Sky"); blueSky.gameObject.SetActive(false); }
            var session = Undo.AddComponent<RoomsMinigameSession>(root.gameObject);
            var data = new SerializedObject(session);
            data.FindProperty("challenges").objectReferenceValue = challenges;
            data.ApplyModifiedProperties();
            ConfigureRoomsArrival(root, challenges);
            return session;
        }

        private static void ConfigureRoomsArrival(Transform root, TrueEndingGameplayController challenges)
        {
            if (challenges == null) return;
            var data = new SerializedObject(challenges);
            Transform checkpoint = data.FindProperty("room1Checkpoint").objectReferenceValue as Transform;
            Transform anchor = root.Find("BackRoomArrivalAnchor");
            if (checkpoint == null || anchor == null) return;
            Undo.RecordObject(anchor, "Set rooms arrival anchor");
            anchor.SetPositionAndRotation(checkpoint.position, checkpoint.rotation * Quaternion.Euler(0, 180f, 0));
        }

        private static Transform CreateEntrance(Transform root, bool rooms)
        {
            Transform source = null;
            if (rooms)
            {
                var challenges = root.GetComponentInChildren<TrueEndingGameplayController>(true);
                if (challenges != null) source = new SerializedObject(challenges).FindProperty("room1Checkpoint").objectReferenceValue as Transform;
            }
            else source = root.GetComponentInChildren<BackroomGameplayManager>(true)?.RespawnPoint;
            var marker = new GameObject("CampaignEntranceCheckpoint");
            Undo.RegisterCreatedObjectUndo(marker, "Create campaign entrance marker");
            marker.transform.SetParent(root, false);
            if (source != null) marker.transform.SetPositionAndRotation(source.position, source.rotation);
            return marker.transform;
        }

        private static void DisableStandaloneSystems(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.GetComponent<FirstPersonController>() == null && root.name != "OldFilmGlobalVolume" &&
                    root.name != "PB_Test_Probe" && root.GetComponent<BackroomSceneController>() == null) continue;
                Undo.RecordObject(root, "Disable standalone systems");
                root.SetActive(false);
            }
        }

        private static Transform Find(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == name) return root.transform;
            return null;
        }

        [MenuItem("Tools/Zero/Campaign/Publish Saved Minigames")]
        public static void PublishSavedMinigames()
        {
            for (int i = 1; i <= 2; i++) Publish(i);
        }

        private static void Publish(int index)
        {
            string name = index == 1 ? "Minigame1_Backroom" : "Minigame2_Rooms";
            string path = Folder + "/" + name + ".unity";
            Scene scene = SceneManager.GetSceneByPath(path);
            if (!scene.IsValid() || scene.isDirty)
            { Debug.LogError($"CAMPAIGN_PUBLISH_REJECTED: open and manually save {path} first."); return; }
            CampaignLevelBootstrap level = null;
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.TryGetComponent(out CampaignLevelBootstrap candidate)) level = candidate;
            if (level == null || level.Entrance == null || level.Session == null)
            { Debug.LogError($"CAMPAIGN_PUBLISH_REJECTED: incomplete scene contract {name}"); return; }
            var definition = AssetDatabase.LoadAssetAtPath<MinigameDefinition>($"{CampaignDefinitionTools.Folder}/Minigame{index}.asset");
            Undo.RecordObject(definition, "Publish minigame definition");
            var data = new SerializedObject(definition);
            data.FindProperty("contentReady").boolValue = true;
            data.ApplyModifiedProperties();
            AssetDatabase.SaveAssetIfDirty(definition);
            AddBuildScene(path);
            Debug.Log($"CAMPAIGN_MINIGAME_PUBLISHED: {name}", definition);
        }

        private static void AddBuildScene(string path)
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            for (int i = 0; i < scenes.Count; i++)
                if (scenes[i].path == path) { scenes[i] = new EditorBuildSettingsScene(path, true); EditorBuildSettings.scenes = scenes.ToArray(); return; }
            scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
