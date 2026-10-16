using System.Collections.Generic;
using UnityEngine;
using Zero.EnvironmentLoop;
using Zero.AnomalyPrototype.Anomalies;

namespace Zero.AnomalyPrototype.Flow
{
    [CreateAssetMenu(menuName = "Zero/Campaign/Infinity Room")]
    public sealed class InfinityRoomDefinition : ScriptableObject
    {
        [SerializeField] private string roomId;
        [SerializeField] private string designTheme;
        [SerializeField] private bool contentReady;
        [SerializeField] private bool useAuthoredScenePool;
        [SerializeField] private FloorSegment segmentPrefab;
        [SerializeField] private AnomalyData[] anomalyRoster;
        [SerializeField, Range(1, 3)] private int maximumDifficulty = 3;
        public string RoomId => roomId;
        public string DesignTheme => designTheme;
        public bool IsReady => contentReady && (useAuthoredScenePool || segmentPrefab != null) &&
            anomalyRoster != null && anomalyRoster.Length > 0;
        public bool UseAuthoredScenePool => useAuthoredScenePool;
        public FloorSegment SegmentPrefab => segmentPrefab;
        public IReadOnlyList<AnomalyData> AnomalyRoster => anomalyRoster;
        public int MaximumDifficulty => maximumDifficulty;

        public string ValidateRoster()
        {
            if (anomalyRoster == null || anomalyRoster.Length == 0)
                return contentReady ? "ready room requires an anomaly roster" : null;
            var ids = new HashSet<string>();
            foreach (AnomalyData data in anomalyRoster)
            {
                if (data == null || string.IsNullOrWhiteSpace(data.Id)) return "missing anomaly reference or ID";
                if (!ids.Add(data.Id)) return "duplicate anomaly ID in room roster";
            }
            return null;
        }
    }
}
