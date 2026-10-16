using System;
using System.Collections.Generic;

namespace Zero.AnomalyPrototype.Flow
{
    /// <summary>Only writer of cross-stage progression. Load failure cannot advance it.</summary>
    public sealed class CampaignProgress
    {
        public enum PhaseKind { InfinityRoom, Minigame, NextStage, FinalEnding }
        private readonly string campaignId;
        private readonly string[] stageIds;
        public int StageIndex { get; private set; }
        public string StageId => stageIds[StageIndex];
        public PhaseKind Phase { get; private set; }
        public EndingIdentity? SelectedEnding { get; private set; }

        public CampaignProgress(string id, IReadOnlyList<string> stages)
        {
            if (string.IsNullOrWhiteSpace(id) || stages == null || stages.Count != 5)
                throw new ArgumentException("A campaign requires an ID and exactly five stages.");
            campaignId = id;
            stageIds = new string[stages.Count];
            var unique = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < stages.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(stages[i]) || !unique.Add(stages[i]))
                    throw new ArgumentException("Stage IDs must be nonempty and unique.");
                stageIds[i] = stages[i];
            }
        }

        public bool TryCompleteInfinityRoom() => TryTransition(PhaseKind.InfinityRoom, PhaseKind.Minigame);

        public bool TryCompleteMinigame()
        {
            return TryTransition(PhaseKind.Minigame,
                StageIndex == stageIds.Length - 1 ? PhaseKind.FinalEnding : PhaseKind.NextStage);
        }

        public bool TryEnterNextStage()
        {
            if (Phase != PhaseKind.NextStage || StageIndex + 1 >= stageIds.Length) return false;
            StageIndex++;
            return TryTransition(PhaseKind.NextStage, PhaseKind.InfinityRoom);
        }

        public bool TryChooseEnding(EndingIdentity ending)
        {
            if (Phase != PhaseKind.FinalEnding || SelectedEnding.HasValue) return false;
            if (ending != EndingIdentity.TheCast && ending != EndingIdentity.AnotherShift &&
                ending != EndingIdentity.CurtainCall) return false;
            SelectedEnding = ending;
            return true;
        }

        public void ReleaseEnding(EndingIdentity ending)
        {
            if (Phase == PhaseKind.FinalEnding && SelectedEnding == ending) SelectedEnding = null;
        }

        public void Restore(CampaignCheckpoint checkpoint)
        {
            if (checkpoint == null || !checkpoint.IsValid() || checkpoint.CampaignId != campaignId)
                throw new ArgumentException("Checkpoint does not belong to this campaign.");
            int index = Array.IndexOf(stageIds, checkpoint.StageId);
            if (index < 0) throw new ArgumentException("Unknown checkpoint stage.");
            StageIndex = index;
            SelectedEnding = null;
            Phase = checkpoint.Location == CampaignCheckpoint.LocationKind.InfinityRoomStart
                ? PhaseKind.InfinityRoom : PhaseKind.Minigame;
        }

        private bool TryTransition(PhaseKind expected, PhaseKind next)
        {
            if (Phase != expected) return false;
            Phase = next;
            return true;
        }
    }
}
