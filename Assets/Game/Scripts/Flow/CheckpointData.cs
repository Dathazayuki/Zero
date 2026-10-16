using System;
using UnityEngine;

namespace Zero.AnomalyPrototype.Flow
{
    [Serializable]
    public sealed class CheckpointData
    {
        public const int CurrentVersion = 2;
        private const string Signature = "ZERO_CHECKPOINT";

        [SerializeField] private string signature = Signature;
        [SerializeField] private int version = CurrentVersion;
        [SerializeField] private CheckpointKind kind;
        [SerializeField] private int logicalFloorIndex;
        [SerializeField] private int displayedFloorNumber;
        [SerializeField] private string campaignId;
        [SerializeField] private string stageId;
        [SerializeField] private string[] encounteredAnomalyIds;

        public CheckpointKind Kind => kind;
        public int LogicalFloorIndex => logicalFloorIndex;
        public int DisplayedFloorNumber => displayedFloorNumber;
        public int Version => version;
        public string CampaignId => campaignId;
        public string StageId => stageId;
        public CampaignCheckpoint ToCampaignCheckpoint() => new CampaignCheckpoint(campaignId, stageId,
            kind == CheckpointKind.Stairway ? CampaignCheckpoint.LocationKind.InfinityRoomStart :
                CampaignCheckpoint.LocationKind.MinigameEntrance, encounteredAnomalyIds);

        private CheckpointData() { }

        public static bool TryDeserialize(string json, out CheckpointData checkpoint)
        {
            checkpoint = new CheckpointData
            {
                signature = null,
                version = -1,
                kind = 0,
                logicalFloorIndex = int.MinValue,
                displayedFloorNumber = -1
            };
            try { JsonUtility.FromJsonOverwrite(json, checkpoint); }
            catch (ArgumentException) { checkpoint = null; return false; }
            if (checkpoint.version == 1) return TryMigrateLegacy(checkpoint, out checkpoint);
            if (checkpoint.IsValid()) return true;
            checkpoint = null;
            return false;
        }

        public static CheckpointData ForStairway(int logicalFloor, int displayedNumber)
        {
            CheckpointData data = ForCampaign(new CampaignCheckpoint(CampaignCheckpoint.DefaultCampaignId,
                CampaignCheckpoint.FirstStageId, CampaignCheckpoint.LocationKind.InfinityRoomStart));
            if (logicalFloor < -100000 || logicalFloor > 100000 || displayedNumber < 0 || displayedNumber > 8)
                data.version = -1;
            return data;
        }

        public static CheckpointData ForBackroom()
        {
            return ForCampaign(new CampaignCheckpoint(CampaignCheckpoint.DefaultCampaignId,
                CampaignCheckpoint.FirstStageId, CampaignCheckpoint.LocationKind.MinigameEntrance));
        }

        public static CheckpointData ForCampaign(CampaignCheckpoint checkpoint)
        {
            if (checkpoint == null || !checkpoint.IsValid()) throw new ArgumentException("Invalid checkpoint.");
            var ids = new string[checkpoint.EncounteredIds.Count];
            for (int i = 0; i < ids.Length; i++) ids[i] = checkpoint.EncounteredIds[i];
            return new CheckpointData { campaignId = checkpoint.CampaignId, stageId = checkpoint.StageId,
                kind = checkpoint.Location == CampaignCheckpoint.LocationKind.InfinityRoomStart
                    ? CheckpointKind.Stairway : CheckpointKind.Backroom, encounteredAnomalyIds = ids };
        }

        private static bool TryMigrateLegacy(CheckpointData legacy, out CheckpointData migrated)
        {
            migrated = null;
            if (legacy.signature != Signature || legacy.logicalFloorIndex < -100000 ||
                legacy.logicalFloorIndex > 100000 || legacy.displayedFloorNumber < 0 ||
                legacy.displayedFloorNumber > 8) return false;
            if (legacy.kind == CheckpointKind.Stairway)
                migrated = ForStairway(0, 0);
            else if (legacy.kind == CheckpointKind.Backroom && legacy.logicalFloorIndex == 0 &&
                legacy.displayedFloorNumber == 0) migrated = ForBackroom();
            return migrated != null;
        }

        public bool IsValid()
        {
            return signature == Signature && version == CurrentVersion &&
                (kind == CheckpointKind.Stairway || kind == CheckpointKind.Backroom) &&
                logicalFloorIndex == 0 && displayedFloorNumber == 0 &&
                !string.IsNullOrWhiteSpace(campaignId) && !string.IsNullOrWhiteSpace(stageId) &&
                encounteredAnomalyIds != null && encounteredAnomalyIds.Length <= 4096;
        }
    }
}
