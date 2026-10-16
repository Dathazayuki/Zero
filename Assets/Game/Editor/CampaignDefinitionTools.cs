using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Zero.AnomalyPrototype.Flow;
using Zero.EnvironmentLoop;
using Zero.AnomalyPrototype.Anomalies;

namespace Zero.Editor
{
    public static class CampaignDefinitionTools
    {
        public const string Folder = "Assets/Game/ScriptableObjects/Campaign";
        public const string CampaignPath = Folder + "/Campaign_Main.asset";
        private static readonly string[] Themes = { "The Gallery", "The Guest Room", "The Records Room", "The Hotel Kitchen", "The Crimson Cabaret" };
        private static readonly string[] Games = { "Backroom Chase", "Don't Look Back + Lights OFF", "The Calling Corridor", "The Long Hall", "The Final Rehearsal" };

        [MenuItem("Tools/Zero/Campaign/Create Definitions")]
        public static void CreateDefinitions()
        {
            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();
            var rooms = new InfinityRoomDefinition[5];
            var games = new MinigameDefinition[5];
            for (int i = 0; i < 5; i++)
            {
                rooms[i] = CreateRoom(i);
                games[i] = CreateGame(i);
            }
            if (AssetDatabase.LoadAssetAtPath<CampaignDefinition>(CampaignPath) != null) return;
            var campaign = ScriptableObject.CreateInstance<CampaignDefinition>();
            var serialized = new SerializedObject(campaign);
            SerializedProperty stages = serialized.FindProperty("stages");
            stages.arraySize = 5;
            for (int i = 0; i < 5; i++)
            {
                SerializedProperty stage = stages.GetArrayElementAtIndex(i);
                stage.FindPropertyRelative("stageId").stringValue = "stage-" + (i + 1);
                stage.FindPropertyRelative("infinityRoom").objectReferenceValue = rooms[i];
                stage.FindPropertyRelative("minigame").objectReferenceValue = games[i];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(campaign, CampaignPath);
            Debug.Log("CAMPAIGN_DEFINITIONS_CREATED: placeholders are not playable; no scene was saved.", campaign);
        }

        private static InfinityRoomDefinition CreateRoom(int index)
        {
            string path = $"{Folder}/InfinityRoom{index + 1}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<InfinityRoomDefinition>(path);
            if (existing != null) return existing;
            var room = ScriptableObject.CreateInstance<InfinityRoomDefinition>();
            var data = new SerializedObject(room);
            data.FindProperty("roomId").stringValue = "infinity-room-" + (index + 1);
            data.FindProperty("designTheme").stringValue = Themes[index];
            if (index == 0) ConfigureFirstRoom(data);
            data.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(room, path);
            return room;
        }

        private static void ConfigureFirstRoom(SerializedObject data)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Environment/PF_FloorSegment.prefab");
            if (prefab == null) return;
            data.FindProperty("segmentPrefab").objectReferenceValue = prefab.GetComponent<FloorSegment>();
            data.FindProperty("useAuthoredScenePool").boolValue = true;
            RoomAnomalyController room = prefab.GetComponentInChildren<RoomAnomalyController>(true);
            if (room?.AnomalySlots == null) return;
            var valid = new System.Collections.Generic.List<AnomalyData>();
            for (int i = 0; i < room.AnomalySlots.Count; i++)
            {
                AnomalyData anomaly = room.AnomalySlots[i]?.Data;
                if (anomaly != null && !valid.Contains(anomaly)) valid.Add(anomaly);
                else Debug.LogWarning($"CAMPAIGN_ROSTER_SLOT_INVALID: slot={i}, prefab={prefab.name}", prefab);
            }
            SerializedProperty roster = data.FindProperty("anomalyRoster");
            roster.arraySize = valid.Count;
            for (int i = 0; i < roster.arraySize; i++)
                roster.GetArrayElementAtIndex(i).objectReferenceValue = valid[i];
            data.FindProperty("contentReady").boolValue = valid.Count > 0;
        }

        [MenuItem("Tools/Zero/Campaign/Sync Room1 Roster From Prefab")]
        public static void SyncFirstRoomRoster()
        {
            var room = AssetDatabase.LoadAssetAtPath<InfinityRoomDefinition>(Folder + "/InfinityRoom1.asset");
            if (room == null) return;
            Undo.RecordObject(room, "Sync Room1 roster");
            var data = new SerializedObject(room);
            ConfigureFirstRoom(data);
            data.ApplyModifiedProperties();
            AssetDatabase.SaveAssetIfDirty(room);
            Debug.Log($"CAMPAIGN_ROSTER_SYNCED: count={room.AnomalyRoster.Count}. Prefab unchanged.", room);
        }

        private static MinigameDefinition CreateGame(int index)
        {
            string path = $"{Folder}/Minigame{index + 1}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<MinigameDefinition>(path);
            if (existing != null) return existing;
            var game = ScriptableObject.CreateInstance<MinigameDefinition>();
            var data = new SerializedObject(game);
            data.FindProperty("minigameId").stringValue = "minigame-" + (index + 1);
            data.FindProperty("designTheme").stringValue = Games[index];
            data.FindProperty("sceneName").stringValue = index == 0 ? "Minigame1_Backroom" :
                index == 1 ? "Minigame2_Rooms" : "Minigame" + (index + 1);
            data.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(game, path);
            return game;
        }

        [MenuItem("Tools/Zero/Campaign/Bind Active Scene Flow")]
        public static void BindActiveFlow()
        {
            var definition = AssetDatabase.LoadAssetAtPath<CampaignDefinition>(CampaignPath);
            if (definition == null) { Debug.LogError("CAMPAIGN_CONFIGURATION_INVALID: create definitions first."); return; }
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            GameFlowController flow = null;
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.TryGetComponent(out GameFlowController candidate)) flow = candidate;
            if (flow == null)
            {
                var root = new GameObject("GameFlow");
                Undo.RegisterCreatedObjectUndo(root, "Bind campaign flow");
                flow = Undo.AddComponent<GameFlowController>(root);
            }
            var runtime = flow.GetComponent<CampaignRuntime>();
            if (runtime == null) runtime = Undo.AddComponent<CampaignRuntime>(flow.gameObject);
            Undo.RecordObject(runtime, "Bind campaign definition");
            var data = new SerializedObject(runtime);
            data.FindProperty("definition").objectReferenceValue = definition;
            data.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("CAMPAIGN_FLOW_BOUND: scene is dirty; save manually.", runtime);
        }

        [MenuItem("Tools/Zero/Campaign/Validate Definitions")]
        public static void ValidateDefinitions()
        {
            var definition = AssetDatabase.LoadAssetAtPath<CampaignDefinition>(CampaignPath);
            if (definition == null) { Debug.LogError("CAMPAIGN_CONFIGURATION_INVALID: definition missing"); return; }
            if (!definition.TryValidate(out string reason))
            { Debug.LogError($"CAMPAIGN_CONFIGURATION_INVALID: {reason}", definition); return; }
            foreach (CampaignDefinition.Stage stage in definition.Stages)
                Debug.Log($"CAMPAIGN_STAGE: id={stage.StageId}, roomReady={stage.InfinityRoom.IsReady}, minigameReady={stage.Minigame.IsReady}", definition);
        }
    }
}
