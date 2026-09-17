using System;
using System.Collections.Generic;
using Zero.AnomalyPrototype.Anomalies;
using Zero.EnvironmentLoop;
using UnityEditor;
using UnityEngine;

namespace Zero.AnomalyPrototype.Editor
{
    public static class AnomalyPacingValidator
    {
        private const int SimulationSeed = 20260915;
        private const int RunCount = 1000;
        private const int EncountersPerRun = 64;

        private static readonly Dictionary<string, AnomalyPacingTier> ExpectedTiers =
            new Dictionary<string, AnomalyPacingTier>
            {
                { "A01_MissingPainting", AnomalyPacingTier.MinorAnomaly },
                { "A02_MovingChair", AnomalyPacingTier.UneasyNormal },
                { "A04_MissingDoor", AnomalyPacingTier.MinorAnomaly },
                { "A05_WatchingStatue", AnomalyPacingTier.MajorAnomaly },
                { "A06_ObserverStatue", AnomalyPacingTier.MajorAnomaly },
                { "A07_ArrowLightFlicker", AnomalyPacingTier.UneasyNormal },
                { "A08_ArrowFlipOnSight", AnomalyPacingTier.MinorAnomaly },
                { "A09_CeilingCreature", AnomalyPacingTier.MajorAnomaly },
                { "A10_WrongReflection", AnomalyPacingTier.MajorAnomaly },
                { "A11_Baloons", AnomalyPacingTier.MinorAnomaly },
                { "A12_HiddenRoom", AnomalyPacingTier.MinorAnomaly }
            };

        [MenuItem("Tools/Zero/Validation/Validate Anomaly Pacing")]
        private static void ValidatePacing()
        {
            try
            {
                ValidateTierWeights();
                ValidateAssets();
                SimulateRuns();
                Debug.Log($"ANOMALY_PACING_VALIDATION_PASS: runs={RunCount}, seed={SimulationSeed}");
            }
            catch (Exception exception)
            {
                Debug.LogError($"ANOMALY_PACING_VALIDATION_FAIL: {exception.Message}");
            }
        }

        private static void ValidateTierWeights()
        {
            ValidateWeightBand(0, 0.55f, 0.45f, 0f);
            ValidateWeightBand(3, 0.20f, 0.42f, 0.38f);
            ValidateWeightBand(6, 0.10f, 0.25f, 0.65f);

            Require(Mathf.Approximately(AnomalyPacingDirector.GetAnomalyChance(0), 0.40f),
                "Progress 0-2 anomaly chance must be 40%.");
            Require(Mathf.Approximately(AnomalyPacingDirector.GetAnomalyChance(3), 0.50f),
                "Progress 3-5 anomaly chance must be 50%.");
            Require(Mathf.Approximately(AnomalyPacingDirector.GetAnomalyChance(6), 0.65f),
                "Progress 6-8 anomaly chance must be 65%.");
        }

        private static void ValidateWeightBand(int progress, float uneasy, float minor, float major)
        {
            Require(Mathf.Approximately(
                    AnomalyPacingDirector.GetTierWeight(AnomalyPacingTier.UneasyNormal, progress), uneasy),
                $"Unexpected UneasyNormal weight at progress {progress}.");
            Require(Mathf.Approximately(
                    AnomalyPacingDirector.GetTierWeight(AnomalyPacingTier.MinorAnomaly, progress), minor),
                $"Unexpected MinorAnomaly weight at progress {progress}.");
            Require(Mathf.Approximately(
                    AnomalyPacingDirector.GetTierWeight(AnomalyPacingTier.MajorAnomaly, progress), major),
                $"Unexpected MajorAnomaly weight at progress {progress}.");
        }

        private static void ValidateAssets()
        {
            foreach (KeyValuePair<string, AnomalyPacingTier> expected in ExpectedTiers)
            {
                string path = $"Assets/Game/ScriptableObjects/Anomalies/{expected.Key}.asset";
                AnomalyData data = AssetDatabase.LoadAssetAtPath<AnomalyData>(path);
                Require(data != null, $"Missing AnomalyData asset: {path}");
                Require(data.PacingTier == expected.Value,
                    $"{expected.Key} is {data.PacingTier}; expected {expected.Value}.");
            }
        }

        private static void SimulateRuns()
        {
            var random = new System.Random(SimulationSeed);
            var director = new AnomalyPacingDirector();
            var recentIds = new Queue<string>();

            for (int run = 0; run < RunCount; run++)
            {
                director.Reset(2, true);
                recentIds.Clear();
                Require(director.NormalStreak == 1 && director.AnomalyStreak == 0,
                    "A reset run must start after one fixed Normal floor.");

                int previewNormalStreak = director.NormalStreak;
                int previewAnomalyStreak = director.AnomalyStreak;
                director.PlanNextEncounter(0, (float)random.NextDouble());
                director.PlanNextEncounter(0, (float)random.NextDouble());
                Require(director.NormalStreak == previewNormalStreak &&
                        director.AnomalyStreak == previewAnomalyStreak,
                    "Planning reserve floors mutated encounter history.");

                bool previousWasMajor = false;
                int lastMajorEncounter = -100;
                for (int encounter = 0; encounter < EncountersPerRun; encounter++)
                {
                    int progress = encounter % 9;
                    PacingDecision decision = director.PlanNextEncounter(
                        progress,
                        (float)random.NextDouble());

                    if (previousWasMajor)
                    {
                        Require(!decision.ShouldHaveAnomaly,
                            $"Run {run}, encounter {encounter}: Major was not followed by Normal.");
                        Require(decision.Reason == PacingDecisionReason.MajorRecovery,
                            $"Run {run}, encounter {encounter}: recovery reason was not preserved.");
                    }

                    if (!decision.ShouldHaveAnomaly)
                    {
                        director.RecordNormal();
                        previousWasMajor = false;
                    }
                    else
                    {
                        AnomalyPacingTier tier = PickTier(director, progress, random);
                        Require(tier != AnomalyPacingTier.MajorAnomaly || progress >= 1,
                            $"Run {run}, encounter {encounter}: Major appeared before progress 1.");
                        if (tier == AnomalyPacingTier.MajorAnomaly)
                        {
                            Require(encounter - lastMajorEncounter > AnomalyPacingDirector.MajorCooldownEncounters,
                                $"Run {run}, encounter {encounter}: Major cooldown was shorter than expected encounters.");
                            lastMajorEncounter = encounter;
                        }

                        string id = PickNonRecentId(tier, recentIds, random);
                        Require(!recentIds.Contains(id),
                            $"Run {run}, encounter {encounter}: repeated recent anomaly {id}.");
                        AnomalyType type = (AnomalyType)((uint)id.GetHashCode() % 6u);
                        director.RecordAnomaly(id, type, tier);
                        recentIds.Enqueue(id);
                        while (recentIds.Count > 2)
                        {
                            recentIds.Dequeue();
                        }

                        previousWasMajor = tier == AnomalyPacingTier.MajorAnomaly;
                    }

                    Require(director.NormalStreak <= AnomalyPacingDirector.MaximumSameBeatStreak,
                        $"Run {run}, encounter {encounter}: more than two Normal floors in sequence.");
                    Require(director.AnomalyStreak <= AnomalyPacingDirector.MaximumSameBeatStreak,
                        $"Run {run}, encounter {encounter}: more than two Anomaly floors in sequence.");
                }

                director.Reset(2, true);
                Require(director.MajorCooldownRemaining == 0 && director.RecentAnomalyCount == 0,
                    "Run reset did not clear pacing cooldown/history.");
            }
        }

        private static AnomalyPacingTier PickTier(
            AnomalyPacingDirector director,
            int progress,
            System.Random random)
        {
            float uneasy = AnomalyPacingDirector.GetTierWeight(AnomalyPacingTier.UneasyNormal, progress);
            float minor = AnomalyPacingDirector.GetTierWeight(AnomalyPacingTier.MinorAnomaly, progress);
            float major = director.IsTierAllowed(AnomalyPacingTier.MajorAnomaly, progress)
                ? AnomalyPacingDirector.GetTierWeight(AnomalyPacingTier.MajorAnomaly, progress)
                : 0f;
            float roll = (float)random.NextDouble() * (uneasy + minor + major);
            if (roll < uneasy)
            {
                return AnomalyPacingTier.UneasyNormal;
            }

            return roll < uneasy + minor
                ? AnomalyPacingTier.MinorAnomaly
                : AnomalyPacingTier.MajorAnomaly;
        }

        private static string PickNonRecentId(
            AnomalyPacingTier tier,
            Queue<string> recentIds,
            System.Random random)
        {
            int start = random.Next(0, 4);
            for (int offset = 0; offset < 4; offset++)
            {
                string candidate = $"{tier}.{(start + offset) % 4}";
                if (!recentIds.Contains(candidate))
                {
                    return candidate;
                }
            }

            throw new InvalidOperationException($"No synthetic candidate remained for tier {tier}.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
