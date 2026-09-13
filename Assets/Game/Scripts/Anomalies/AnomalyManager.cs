using System.Collections.Generic;
using UnityEngine;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class AnomalyManager : MonoBehaviour
    {
        private enum NextLoopOverride
        {
            None,
            ForceNormal,
            ForceAnomaly
        }

        [SerializeField, Range(0f, 1f)] private float anomalyChance = 0.62f;
        [SerializeField, Min(0)] private int recentHistorySize = 2;

        private readonly List<AnomalyBehaviour> anomalies = new List<AnomalyBehaviour>();
        private readonly List<AnomalyBehaviour> eligible = new List<AnomalyBehaviour>();
        private readonly Queue<string> recentIds = new Queue<string>();
        private NextLoopOverride nextLoopOverride;

        public AnomalyBehaviour Current { get; private set; }
        public bool HasActiveAnomaly => Current != null;

        public void Initialize(IReadOnlyList<AnomalyBehaviour> registeredAnomalies, float chance = 0.62f, int historySize = 2)
        {
            ResetCurrent();
            anomalies.Clear();
            if (registeredAnomalies != null)
            {
                for (int i = 0; i < registeredAnomalies.Count; i++)
                {
                    AnomalyBehaviour anomaly = registeredAnomalies[i];
                    if (anomaly != null && anomaly.Data != null && !anomalies.Contains(anomaly))
                    {
                        anomalies.Add(anomaly);
                    }
                }
            }

            anomalyChance = Mathf.Clamp01(chance);
            recentHistorySize = Mathf.Max(0, historySize);
            recentIds.Clear();
            nextLoopOverride = NextLoopOverride.None;
        }

        public AnomalyBehaviour SelectAndActivate(int maximumDifficulty)
        {
            ResetCurrent();

            bool shouldSpawn = nextLoopOverride == NextLoopOverride.ForceAnomaly ||
                               (nextLoopOverride == NextLoopOverride.None && Random.value <= anomalyChance);
            if (nextLoopOverride == NextLoopOverride.ForceNormal)
            {
                shouldSpawn = false;
            }

            nextLoopOverride = NextLoopOverride.None;
            if (!shouldSpawn)
            {
                return null;
            }

            BuildEligiblePool(Mathf.Clamp(maximumDifficulty, 1, 3), true);
            if (eligible.Count == 0)
            {
                BuildEligiblePool(Mathf.Clamp(maximumDifficulty, 1, 3), false);
            }

            Current = WeightedPick(eligible);
            if (Current == null)
            {
                return null;
            }

            Current.Activate();
            Remember(Current.Data.Id);
            return Current;
        }

        public void ResetCurrent()
        {
            if (Current != null)
            {
                Current.ResetAnomaly();
                Current = null;
            }
        }

        public void ForceNormalNextLoop()
        {
            nextLoopOverride = NextLoopOverride.ForceNormal;
        }

        public void ForceAnomalyNextLoop()
        {
            nextLoopOverride = NextLoopOverride.ForceAnomaly;
        }

        private void BuildEligiblePool(int maximumDifficulty, bool excludeRecent)
        {
            eligible.Clear();
            for (int i = 0; i < anomalies.Count; i++)
            {
                AnomalyBehaviour anomaly = anomalies[i];
                if (anomaly == null || anomaly.Data == null || anomaly.Data.Difficulty > maximumDifficulty)
                {
                    continue;
                }

                if (excludeRecent && recentIds.Contains(anomaly.Data.Id))
                {
                    continue;
                }

                eligible.Add(anomaly);
            }
        }

        private static AnomalyBehaviour WeightedPick(List<AnomalyBehaviour> pool)
        {
            float totalWeight = 0f;
            for (int i = 0; i < pool.Count; i++)
            {
                totalWeight += Mathf.Max(0.01f, pool[i].Data.SpawnWeight);
            }

            if (totalWeight <= 0f)
            {
                return null;
            }

            float roll = Random.value * totalWeight;
            for (int i = 0; i < pool.Count; i++)
            {
                roll -= Mathf.Max(0.01f, pool[i].Data.SpawnWeight);
                if (roll <= 0f)
                {
                    return pool[i];
                }
            }

            return pool[pool.Count - 1];
        }

        private void Remember(string anomalyId)
        {
            if (recentHistorySize <= 0 || string.IsNullOrEmpty(anomalyId))
            {
                return;
            }

            recentIds.Enqueue(anomalyId);
            while (recentIds.Count > recentHistorySize)
            {
                recentIds.Dequeue();
            }
        }
    }
}
