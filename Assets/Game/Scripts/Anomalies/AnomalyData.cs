using UnityEngine;

namespace Zero.AnomalyPrototype.Anomalies
{
    public enum AnomalyType
    {
        ObjectChange,
        Environment,
        Audio,
        Creature,
        Psychological,
        Observer
    }

    public enum AnomalyPacingTier
    {
        UneasyNormal,
        MinorAnomaly,
        MajorAnomaly
    }

    public enum AnomalyDeathStyle
    {
        Style1_JumpscareBlood = 0,
        Style2_FreezeBlackout = 1
    }

    [CreateAssetMenu(fileName = "AnomalyData", menuName = "Zero/Anomaly Prototype/Anomaly Data")]
    public sealed class AnomalyData : ScriptableObject
    {
        [SerializeField] private string id = "anomaly.id";
        [SerializeField] private string displayName = "Unnamed Anomaly";
        [SerializeField] private AnomalyType type = AnomalyType.ObjectChange;
        [SerializeField] private AnomalyPacingTier pacingTier = AnomalyPacingTier.MinorAnomaly;
        [SerializeField, Range(1, 3)] private int difficulty = 1;
        [SerializeField, Min(0.01f)] private float spawnWeight = 1f;
        [SerializeField] private bool canKillPlayer;
        [SerializeField] private AnomalyDeathStyle deathStyle = AnomalyDeathStyle.Style1_JumpscareBlood;
        [SerializeField, TextArea] private string description;

        public string Id => id;
        public string DisplayName => displayName;
        public AnomalyType Type => type;
        public AnomalyPacingTier PacingTier => pacingTier;
        public int Difficulty => difficulty;
        public float SpawnWeight => spawnWeight;
        public bool CanKillPlayer => canKillPlayer;
        public AnomalyDeathStyle DeathStyle => deathStyle;
        public string Description => description;

        public void SetDeathStyle(AnomalyDeathStyle style) => deathStyle = style;

        public void Configure(
            string anomalyId,
            string anomalyDisplayName,
            AnomalyType anomalyType,
            int anomalyDifficulty,
            float weight,
            bool lethal,
            string anomalyDescription)
        {
            Configure(
                anomalyId,
                anomalyDisplayName,
                anomalyType,
                lethal ? AnomalyPacingTier.MajorAnomaly : AnomalyPacingTier.MinorAnomaly,
                anomalyDifficulty,
                weight,
                lethal,
                anomalyDescription);
        }

        public void Configure(
            string anomalyId,
            string anomalyDisplayName,
            AnomalyType anomalyType,
            AnomalyPacingTier anomalyPacingTier,
            int anomalyDifficulty,
            float weight,
            bool lethal,
            string anomalyDescription)
        {
            id = anomalyId;
            displayName = anomalyDisplayName;
            type = anomalyType;
            pacingTier = anomalyPacingTier;
            difficulty = Mathf.Clamp(anomalyDifficulty, 1, 3);
            spawnWeight = Mathf.Max(0.01f, weight);
            canKillPlayer = lethal;
            description = anomalyDescription;
        }
    }
}
