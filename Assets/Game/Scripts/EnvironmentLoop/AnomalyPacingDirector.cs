using System.Collections.Generic;
using Zero.AnomalyPrototype.Anomalies;
using UnityEngine;

namespace Zero.EnvironmentLoop
{
    public enum PacingDecisionReason
    {
        Weighted,
        MaximumNormalStreak,
        MaximumAnomalyStreak,
        MajorRecovery
    }

    public readonly struct PacingDecision
    {
        public PacingDecision(bool shouldHaveAnomaly, bool wasForced, PacingDecisionReason reason)
        {
            ShouldHaveAnomaly = shouldHaveAnomaly;
            WasForced = wasForced;
            Reason = reason;
        }

        public bool ShouldHaveAnomaly { get; }
        public bool WasForced { get; }
        public PacingDecisionReason Reason { get; }
    }

    /// <summary>
    /// Mutable per-run pacing state. Previewing reserve floors never mutates this object;
    /// only recording the floor the player actually entered advances its state.
    /// </summary>
    public sealed class AnomalyPacingDirector
    {
        public const int MaximumSameBeatStreak = 2;
        public const int MajorCooldownEncounters = 1;

        private readonly Queue<string> recentAnomalyIds = new Queue<string>();
        private int recentHistorySize = 2;
        private bool hasLastAnomalyType;
        private AnomalyType lastAnomalyType;

        public int NormalStreak { get; private set; }
        public int AnomalyStreak { get; private set; }
        public int MajorCooldownRemaining { get; private set; }
        public bool ForceRecoveryAfterMajor { get; private set; }
        public int RecentAnomalyCount => recentAnomalyIds.Count;

        public void Reset(int historySize, bool initialFloorIsNormal)
        {
            recentHistorySize = Mathf.Max(1, historySize);
            recentAnomalyIds.Clear();
            hasLastAnomalyType = false;
            lastAnomalyType = default;
            NormalStreak = initialFloorIsNormal ? 1 : 0;
            AnomalyStreak = 0;
            MajorCooldownRemaining = 0;
            ForceRecoveryAfterMajor = false;
        }

        public PacingDecision PlanNextEncounter(int projectedProgress, float randomValue)
        {
            if (ForceRecoveryAfterMajor)
            {
                return new PacingDecision(false, true, PacingDecisionReason.MajorRecovery);
            }

            if (AnomalyStreak >= MaximumSameBeatStreak)
            {
                return new PacingDecision(false, true, PacingDecisionReason.MaximumAnomalyStreak);
            }

            if (NormalStreak >= MaximumSameBeatStreak)
            {
                return new PacingDecision(true, true, PacingDecisionReason.MaximumNormalStreak);
            }

            bool anomaly = Mathf.Clamp01(randomValue) < GetAnomalyChance(projectedProgress);
            return new PacingDecision(anomaly, false, PacingDecisionReason.Weighted);
        }

        public void RecordNormal()
        {
            NormalStreak++;
            AnomalyStreak = 0;
            ForceRecoveryAfterMajor = false;
            DecrementMajorCooldown();
        }

        public void RecordAnomaly(string anomalyId, AnomalyType type, AnomalyPacingTier tier)
        {
            NormalStreak = 0;
            AnomalyStreak++;

            if (tier == AnomalyPacingTier.MajorAnomaly)
            {
                MajorCooldownRemaining = MajorCooldownEncounters;
                ForceRecoveryAfterMajor = true;
            }
            else
            {
                DecrementMajorCooldown();
            }

            hasLastAnomalyType = true;
            lastAnomalyType = type;
            if (string.IsNullOrEmpty(anomalyId))
            {
                return;
            }

            recentAnomalyIds.Enqueue(anomalyId);
            while (recentAnomalyIds.Count > recentHistorySize)
            {
                recentAnomalyIds.Dequeue();
            }
        }

        public bool IsRecentAnomaly(string anomalyId)
        {
            return !string.IsNullOrEmpty(anomalyId) && recentAnomalyIds.Contains(anomalyId);
        }

        public bool IsImmediateRepeat(string anomalyId)
        {
            if (string.IsNullOrEmpty(anomalyId) || recentAnomalyIds.Count == 0)
            {
                return false;
            }

            string latest = null;
            foreach (string recentId in recentAnomalyIds)
            {
                latest = recentId;
            }

            return latest == anomalyId;
        }

        public bool MatchesLastAnomalyType(AnomalyType type)
        {
            return hasLastAnomalyType && lastAnomalyType == type;
        }

        public bool IsTierAllowed(AnomalyPacingTier tier, int projectedProgress)
        {
            return tier != AnomalyPacingTier.MajorAnomaly ||
                   (projectedProgress >= 1 && MajorCooldownRemaining <= 0);
        }

        public static float GetAnomalyChance(int projectedProgress)
        {
            if (projectedProgress <= 2)
            {
                return 0.40f;
            }

            return projectedProgress <= 5 ? 0.50f : 0.55f;
        }

        public static float GetTierWeight(AnomalyPacingTier tier, int projectedProgress)
        {
            if (projectedProgress <= 0)
            {
                switch (tier)
                {
                    case AnomalyPacingTier.UneasyNormal: return 0.55f;
                    case AnomalyPacingTier.MinorAnomaly: return 0.45f;
                    default: return 0f;
                }
            }

            if (projectedProgress <= 5)
            {
                switch (tier)
                {
                    case AnomalyPacingTier.UneasyNormal: return 0.20f;
                    case AnomalyPacingTier.MinorAnomaly: return 0.42f;
                    case AnomalyPacingTier.MajorAnomaly: return 0.38f;
                    default: return 0f;
                }
            }

            switch (tier)
            {
                case AnomalyPacingTier.UneasyNormal: return 0.10f;
                case AnomalyPacingTier.MinorAnomaly: return 0.35f;
                case AnomalyPacingTier.MajorAnomaly: return 0.55f;
                default: return 0f;
            }
        }

        private void DecrementMajorCooldown()
        {
            if (MajorCooldownRemaining > 0)
            {
                MajorCooldownRemaining--;
            }
        }
    }
}
