using Zero.AnomalyPrototype.Player;
using UnityEngine;

namespace Zero.EnvironmentLoop
{
    [DefaultExecutionOrder(-1000)]
    public sealed class InfiniteBuildingManager : MonoBehaviour
    {
        public const int CurrentBakeVersion = 1;

        [SerializeField] private int bakedVersion;
        [SerializeField] private StairLoopManager stairLoopManager;
        [SerializeField] private FloorProgressManager floorProgressManager;
        [SerializeField] private EnvironmentLoopDebug environmentDebugManager;
        [SerializeField] private DirectionalAnomalyGameManager anomalyGameManager;

        public StairLoopManager StairLoopManager => stairLoopManager;
        public FloorProgressManager FloorProgressManager => floorProgressManager;
        public bool IsInitialized => stairLoopManager != null && stairLoopManager.IsInitialized;
        public int BakedVersion => bakedVersion;
        public DirectionalAnomalyGameManager AnomalyGameManager => anomalyGameManager;

        private void Awake()
        {
            var campaign = Zero.AnomalyPrototype.Flow.GameFlowController.Instance?.Campaign;
            if (campaign != null && campaign.IsActive) campaign.PrepareInitialRoom(this);
            else InitializeEnvironment();
        }

        public void Configure(
            StairLoopManager loopManager,
            FloorProgressManager progressManager,
            EnvironmentLoopDebug debugManager)
        {
            stairLoopManager = loopManager;
            floorProgressManager = progressManager;
            environmentDebugManager = debugManager;
        }

        public void MarkBaked()
        {
            bakedVersion = CurrentBakeVersion;
        }

        public void InitializeEnvironment()
        {
            InitializeEnvironment(FindFirstObjectByType<FirstPersonController>());
        }

        public void InitializeEnvironment(FirstPersonController player)
        {
            if (stairLoopManager == null || floorProgressManager == null)
            {
                Debug.LogError("InfiniteBuildingManager has incomplete environment manager references.", this);
                return;
            }

            stairLoopManager.Initialize();
            ResolveAnomalyManager();

            if (player == null)
            {
                player = transform.root.GetComponentInChildren<FirstPersonController>(true);
            }
            stairLoopManager.SetAnomalyGameManager(anomalyGameManager);
            anomalyGameManager.Initialize(stairLoopManager, player);
            environmentDebugManager?.Configure(stairLoopManager, floorProgressManager);
        }

        public DirectionalAnomalyGameManager ResolveAnomalyManager()
        {
            if (anomalyGameManager == null) anomalyGameManager = GetComponent<DirectionalAnomalyGameManager>();
            if (anomalyGameManager == null) anomalyGameManager = gameObject.AddComponent<DirectionalAnomalyGameManager>();
            return anomalyGameManager;
        }
    }
}
