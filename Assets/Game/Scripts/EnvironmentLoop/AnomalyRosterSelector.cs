using System.Collections.Generic;
using UnityEngine;
using Zero.AnomalyPrototype.Anomalies;
using Zero.AnomalyPrototype.Flow;

namespace Zero.EnvironmentLoop
{
    /// <summary>Tier-first weighted selection, with encounter-only diversity and hard safety filters.</summary>
    public sealed class AnomalyRosterSelector
    {
        private readonly AnomalyPacingDirector pacing;
        private readonly HashSet<string> roster = new HashSet<string>();
        private EncounterDiversityHistory history;
        private bool restrictedRoster;
        private int difficulty = 3;
        private int authoredDifficulty = 3;

        public AnomalyRosterSelector(AnomalyPacingDirector pacingDirector) { pacing = pacingDirector; }

        public void Configure(InfinityRoomDefinition definition, EncounterDiversityHistory diversity)
        {
            history = diversity;
            roster.Clear();
            restrictedRoster = definition != null;
            authoredDifficulty = definition != null ? definition.MaximumDifficulty : 3;
            if (definition?.AnomalyRoster == null) return;
            foreach (AnomalyData data in definition.AnomalyRoster)
                if (data != null) roster.Add(data.Id);
        }

        public AnomalyBehaviour Select(IReadOnlyList<AnomalyBehaviour> pool, int progress, int maxDifficulty)
        {
            difficulty = Mathf.Min(authoredDifficulty, maxDifficulty);
            bool unseen = HasCandidate(pool, progress, null, true);
            float uneasy = TierWeight(pool, progress, AnomalyPacingTier.UneasyNormal, unseen);
            float minor = TierWeight(pool, progress, AnomalyPacingTier.MinorAnomaly, unseen);
            float major = TierWeight(pool, progress, AnomalyPacingTier.MajorAnomaly, unseen);
            float total = uneasy + minor + major;
            if (total <= 0f) return null;
            float roll = UnityEngine.Random.value * total;
            AnomalyPacingTier tier = roll < uneasy ? AnomalyPacingTier.UneasyNormal :
                roll < uneasy + minor ? AnomalyPacingTier.MinorAnomaly : AnomalyPacingTier.MajorAnomaly;
            return Pick(pool, progress, tier, unseen);
        }

        private float TierWeight(IReadOnlyList<AnomalyBehaviour> pool, int progress,
            AnomalyPacingTier tier, bool unseen)
        {
            return HasCandidate(pool, progress, tier, unseen) ? AnomalyPacingDirector.GetTierWeight(tier, progress) : 0f;
        }

        private bool HasCandidate(IReadOnlyList<AnomalyBehaviour> pool, int progress,
            AnomalyPacingTier? tier, bool unseen)
        {
            for (int i = 0; i < pool.Count; i++)
                if (Eligible(pool[i]?.Data, progress, tier, unseen)) return true;
            return false;
        }

        private bool Eligible(AnomalyData data, int progress, AnomalyPacingTier? tier, bool unseen)
        {
            if (data == null || data.Difficulty > difficulty || data.SpawnWeight <= 0f) return false;
            if (tier.HasValue && data.PacingTier != tier.Value) return false;
            if (!TierAllowed(data, progress)) return false;
            if (restrictedRoster && !roster.Contains(data.Id)) return false;
            return HistoryAllows(data.Id, unseen);
        }

        private bool TierAllowed(AnomalyData data, int progress) => pacing.IsTierAllowed(data.PacingTier, progress) &&
            AnomalyPacingDirector.GetTierWeight(data.PacingTier, progress) > 0f;

        private bool HistoryAllows(string id, bool unseen)
        {
            if (pacing.IsRecentAnomaly(id)) return false;
            if (history == null) return true;
            return !history.IsRecent(id) && (!unseen || !history.HasEncountered(id));
        }

        private AnomalyBehaviour Pick(IReadOnlyList<AnomalyBehaviour> pool, int progress,
            AnomalyPacingTier tier, bool unseen)
        {
            float total = 0f;
            for (int i = 0; i < pool.Count; i++)
                if (Eligible(pool[i]?.Data, progress, tier, unseen)) total += pool[i].Data.SpawnWeight;
            float roll = UnityEngine.Random.value * total;
            for (int i = 0; i < pool.Count; i++)
            {
                if (!Eligible(pool[i]?.Data, progress, tier, unseen)) continue;
                roll -= pool[i].Data.SpawnWeight;
                if (roll <= 0f) return pool[i];
            }
            return null;
        }
    }
}
