using System;
using System.Collections.Generic;

namespace Zero.AnomalyPrototype.Flow
{
    /// <summary>Scene-independent checkpoint; never captures a live floor or player pose.</summary>
    public sealed class CampaignCheckpoint
    {
        public enum LocationKind { InfinityRoomStart = 1, MinigameEntrance = 2 }
        public const string DefaultCampaignId = "zero-main";
        public const string FirstStageId = "stage-1";
        private readonly string[] encounteredIds;
        private readonly IReadOnlyList<string> readOnlyEncounteredIds;
        public string CampaignId { get; }
        public string StageId { get; }
        public LocationKind Location { get; }
        public IReadOnlyList<string> EncounteredIds => readOnlyEncounteredIds;

        public CampaignCheckpoint(string campaignId, string stageId, LocationKind location,
            IEnumerable<string> history = null)
        {
            CampaignId = campaignId;
            StageId = stageId;
            Location = location;
            var ids = new List<string>();
            if (history != null)
                foreach (string id in history)
                    if (!string.IsNullOrWhiteSpace(id)) ids.Add(id);
            encounteredIds = ids.ToArray();
            readOnlyEncounteredIds = Array.AsReadOnly(encounteredIds);
        }

        public bool IsValid() => !string.IsNullOrWhiteSpace(CampaignId) &&
            !string.IsNullOrWhiteSpace(StageId) &&
            (Location == LocationKind.InfinityRoomStart || Location == LocationKind.MinigameEntrance);
    }
}
