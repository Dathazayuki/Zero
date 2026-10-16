using System;
using System.Collections.Generic;
using UnityEngine;

namespace Zero.AnomalyPrototype.Flow
{
    [CreateAssetMenu(menuName = "Zero/Campaign/Campaign")]
    public sealed class CampaignDefinition : ScriptableObject
    {
        [Serializable]
        public sealed class Stage
        {
            [SerializeField] private string stageId;
            [SerializeField] private InfinityRoomDefinition infinityRoom;
            [SerializeField] private MinigameDefinition minigame;
            public string StageId => stageId;
            public InfinityRoomDefinition InfinityRoom => infinityRoom;
            public MinigameDefinition Minigame => minigame;
        }

        [SerializeField] private string campaignId = CampaignCheckpoint.DefaultCampaignId;
        [SerializeField] private Stage[] stages;
        public string CampaignId => campaignId;
        public IReadOnlyList<Stage> Stages => stages;

        public bool TryValidate(out string reason)
        {
            reason = null;
            if (string.IsNullOrWhiteSpace(campaignId) || stages == null || stages.Length != 5)
                reason = "campaign ID and exactly five stages are required";
            if (reason != null) return false;
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var roomIds = new HashSet<string>(StringComparer.Ordinal);
            var gameIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < stages.Length; i++)
            {
                reason = ValidateStage(stages[i], ids, roomIds, gameIds);
                if (reason != null) return false;
            }
            return true;
        }

        private static string ValidateStage(Stage stage, HashSet<string> ids,
            HashSet<string> roomIds, HashSet<string> gameIds)
        {
            if (stage == null || string.IsNullOrWhiteSpace(stage.StageId) || !ids.Add(stage.StageId))
                return "missing or duplicate stage ID";
            if (stage.InfinityRoom == null || stage.Minigame == null) return "definitions missing";
            if (!UniqueId(stage.InfinityRoom.RoomId, roomIds)) return "missing or duplicate room ID";
            if (!UniqueId(stage.Minigame.MinigameId, gameIds)) return "missing or duplicate minigame ID";
            return stage.InfinityRoom.ValidateRoster();
        }

        private static bool UniqueId(string id, HashSet<string> ids) => !string.IsNullOrWhiteSpace(id) && ids.Add(id);

        public CampaignProgress CreateProgress()
        {
            if (!TryValidate(out string reason)) throw new InvalidOperationException(reason);
            var ids = new string[stages.Length];
            for (int i = 0; i < stages.Length; i++) ids[i] = stages[i].StageId;
            return new CampaignProgress(campaignId, ids);
        }
    }
}
