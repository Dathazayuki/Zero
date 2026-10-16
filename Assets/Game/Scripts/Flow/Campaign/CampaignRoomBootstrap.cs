using UnityEngine;
using Zero.EnvironmentLoop;
using Zero.AnomalyPrototype.Player;

namespace Zero.AnomalyPrototype.Flow
{
    /// <summary>Composition-only pool construction; never modifies authored assets.</summary>
    public sealed class CampaignRoomBootstrap : MonoBehaviour
    {
        public bool Initialize(InfinityRoomDefinition room, InfiniteBuildingManager building,
            FirstPersonController player, EncounterDiversityHistory history)
        {
            if (room == null || !room.IsReady || building == null || player == null) return false;
            DirectionalAnomalyGameManager anomalies = building.ResolveAnomalyManager();
            if (!room.UseAuthoredScenePool && !CreatePool(room, building)) return false;
            anomalies.ConfigureCampaign(room, history);
            building.InitializeEnvironment(player);
            return building.IsInitialized;
        }

        private bool CreatePool(InfinityRoomDefinition room, InfiniteBuildingManager building)
        {
            StairLoopManager loop = building.StairLoopManager;
            if (loop == null || loop.IsInitialized || room.SegmentPrefab == null) return false;
            foreach (FloorSegment segment in loop.ConfiguredSegments)
                if (segment != null) segment.gameObject.SetActive(false);
            var segments = new FloorSegment[StairLoopManager.RequiredPoolSize];
            Transform parent = new GameObject("CampaignFloorPool").transform;
            parent.SetParent(building.StairLoopManager.transform.parent, false);
            for (int i = 0; i < segments.Length; i++)
            {
                segments[i] = Instantiate(room.SegmentPrefab, parent);
                segments[i].name = $"{room.RoomId}_Segment_{i}";
            }
            loop.Configure(building.FloorProgressManager, segments);
            return true;
        }
    }
}
