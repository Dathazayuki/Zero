using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zero.AnomalyPrototype.Player;
using Zero.EnvironmentLoop;

namespace Zero.AnomalyPrototype.Flow
{
    [DisallowMultipleComponent]
    public sealed class CheckpointRuntime : MonoBehaviour
    {
        private GameFlowController flow;
        private CheckpointStore store;
        private CheckpointData pending;
        private StairLoopManager loop;
        private Coroutine initialSaveRoutine;
        private bool autosaveEnabled;
        private const float ReadinessTimeout = 15f;
        public bool HasPendingRestore => pending != null;
        public bool RestoreSucceeded { get; private set; }
        public bool RestoredBackroom { get; private set; }

        private void Awake()
        {
            flow = GetComponent<GameFlowController>();
            store = new CheckpointStore(Application.persistentDataPath);
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            if (flow != null) flow.PhaseChanged += OnPhaseChanged;
            if (flow != null) OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            if (flow != null) flow.PhaseChanged -= OnPhaseChanged;
            UnbindLoop();
            if (initialSaveRoutine != null) StopCoroutine(initialSaveRoutine);
            initialSaveRoutine = null;
        }

        public bool TryGetCheckpoint(out CheckpointData checkpoint)
        {
            return store.TryLoad(out checkpoint) && (flow.Campaign == null || flow.Campaign.CanRestore(checkpoint.ToCampaignCheckpoint()));
        }

        public bool BeginNewGame()
        {
            if (!store.Clear()) return false;
            pending = null;
            autosaveEnabled = true;
            return true;
        }

        public bool PrepareContinue()
        {
            if (pending != null || !store.TryLoad(out CheckpointData checkpoint)) return false;
            if (flow.Campaign != null && !flow.Campaign.TryRestore(checkpoint.ToCampaignCheckpoint())) return false;
            pending = checkpoint;
            autosaveEnabled = true;
            return true;
        }

        public void CancelPendingContinue() => pending = null;

        public void EnableAutosave() => autosaveEnabled = true;

        public void FinishRun()
        {
            autosaveEnabled = false;
            pending = null;
            store.Clear();
        }

        public IEnumerator RestoreLoadedLoop(string backroomSceneName)
        {
            RestoreSucceeded = false;
            RestoredBackroom = false;
            if (pending == null) yield break;

            Scene scene = SceneManager.GetSceneByName(flow.LoopSceneName);
            yield return WaitForLoop(scene);
            if (pending == null) yield break;
            CheckpointData checkpoint = pending;
            FirstPersonController player = FindInScene<FirstPersonController>(scene);
            if (player == null)
            {
                Debug.LogError("CHECKPOINT_RESTORE_FAILED: player missing", this);
                pending = null;
                yield break;
            }

            player.SetCinematicLock(true);
            player.GetComponent<PlayerInteraction>()?.SetInputEnabled(false);
            if (checkpoint.Kind == CheckpointKind.Stairway)
                RestoreSucceeded = RestoreStairway(player, checkpoint);
            else
            {
                var loader = new BackroomCheckpointLoader(backroomSceneName);
                if (flow.Campaign?.IsActive == true)
                {
                    yield return flow.Campaign.RestoreMinigame(player);
                    RestoreSucceeded = flow.Campaign.IsDestinationReady;
                }
                else
                {
                    yield return loader.Restore(player);
                    RestoreSucceeded = loader.Succeeded;
                }
                RestoredBackroom = RestoreSucceeded;
            }

            if (!RestoreSucceeded)
                Debug.LogError($"CHECKPOINT_RESTORE_FAILED: kind={checkpoint.Kind}", this);
            if (RestoreSucceeded) Save(checkpoint);
            pending = null;
        }

        public void ReleaseRestoredPlayer()
        {
            FirstPersonController player = FindFirstObjectByType<FirstPersonController>();
            if (player == null) return;
            player.SetCinematicLock(false);
            player.GetComponent<PlayerInteraction>()?.SetInputEnabled(true);
        }

        private bool RestoreStairway(FirstPersonController player, CheckpointData data)
        {
            InfiniteBuildingManager building = FindInScene<InfiniteBuildingManager>(player.gameObject.scene);
            if (building == null || !building.IsInitialized) return false;
            StairLoopManager stair = building.StairLoopManager;
            FloorProgressManager progress = building.FloorProgressManager;
            DirectionalAnomalyGameManager anomaly = building.AnomalyGameManager;
            if (stair == null || progress == null || anomaly == null) return false;
            Transform spawn = ResolveStairwaySpawn(stair, player.gameObject.scene);
            if (spawn == null) return false;
            stair.ResetToInitialState();
            anomaly.ResetRun();
            player.Teleport(spawn.position, spawn.rotation);
            Physics.SyncTransforms();
            Debug.Log($"CHECKPOINT_STAIRWAY_RESTORED: logical={data.LogicalFloorIndex}, " +
                      $"number={data.DisplayedFloorNumber}", stair);
            return true;
        }

        private static Transform ResolveStairwaySpawn(StairLoopManager stair, Scene scene)
        {
            LoopResumePoint marker = stair.CurrentSegment.GetComponentInChildren<LoopResumePoint>(true);
            Transform spawn = marker != null ? marker.SpawnTransform : null;
            if (spawn == null)
            {
                LoopEntrySpawnPoint fallback = FindInScene<LoopEntrySpawnPoint>(scene);
                spawn = fallback != null ? fallback.SpawnTransform : null;
                Debug.LogWarning("CHECKPOINT_RESUME_MARKER_MISSING: using LoopEntrySpawnPoint", stair);
            }
            return spawn;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (flow == null ||
                scene.name != flow.LoopSceneName) return;
            autosaveEnabled = true;
            if (initialSaveRoutine != null) StopCoroutine(initialSaveRoutine);
            initialSaveRoutine = StartCoroutine(SaveInitialCheckpointWhenReady(scene));
        }

        private void OnSceneUnloaded(Scene scene)
        {
            if (flow == null || scene.name != flow.LoopSceneName) return;
            if (loop != null && loop.gameObject.scene != scene) return;
            UnbindLoop();
            if (initialSaveRoutine != null) StopCoroutine(initialSaveRoutine);
            initialSaveRoutine = null;
        }

        private void OnPhaseChanged(GameFlowPhase phase)
        {
            if (phase != GameFlowPhase.BackroomGameplay || !autosaveEnabled || pending != null) return;
            Save(flow.Campaign?.IsActive == true
                ? CheckpointData.ForCampaign(flow.Campaign.CaptureCheckpoint()) : CheckpointData.ForBackroom());
        }

        private IEnumerator SaveInitialCheckpointWhenReady(Scene scene)
        {
            bool restoring = pending != null;
            yield return null;
            yield return WaitForLoop(scene);
            initialSaveRoutine = null;
            if (restoring || !autosaveEnabled || pending != null || flow == null ||
                flow.Phase != GameFlowPhase.LoopGameplay) yield break;
            SaveCurrentStairway();
        }

        private IEnumerator WaitForLoop(Scene scene)
        {
            float deadline = Time.realtimeSinceStartup + ReadinessTimeout;
            while (scene.IsValid() && scene.isLoaded && Time.realtimeSinceStartup < deadline)
            {
                InfiniteBuildingManager building = FindInScene<InfiniteBuildingManager>(scene);
                if (building != null && building.IsInitialized && building.AnomalyGameManager != null)
                {
                    BindLoop(building.StairLoopManager);
                    yield break;
                }
                yield return null;
            }
            Debug.LogError("CHECKPOINT_LOOP_NOT_READY: initialization failed or timed out", this);
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            if (!scene.IsValid() || !scene.isLoaded) return null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T component = root.GetComponentInChildren<T>();
                if (component != null) return component;
            }
            return null;
        }

        private void BindLoop(StairLoopManager source)
        {
            if (loop == source) return;
            UnbindLoop();
            loop = source;
            if (loop != null) loop.CurrentFloorCommitted += OnFloorCommitted;
            if (loop != null && loop.AnomalyGameManager != null)
                loop.AnomalyGameManager.RoundPrepared += OnRoundPrepared;
        }

        private void UnbindLoop()
        {
            if (loop != null) loop.CurrentFloorCommitted -= OnFloorCommitted;
            if (loop != null && loop.AnomalyGameManager != null)
                loop.AnomalyGameManager.RoundPrepared -= OnRoundPrepared;
            loop = null;
        }

        private void OnRoundPrepared()
        {
            if (loop == null || loop.RecycleCount != 0 || loop.ProgressManager == null ||
                loop.ProgressManager.LogicalFloorIndex != 0 ||
                loop.AnomalyGameManager.DisplayedFloorNumber != 0) return;
            OnFloorCommitted(loop.CurrentSegment);
        }

        private void OnFloorCommitted(FloorSegment segment)
        {
            if (segment != null && autosaveEnabled && pending == null &&
                flow != null && flow.Phase == GameFlowPhase.LoopGameplay) SaveCurrentStairway();
        }

        private void SaveCurrentStairway()
        {
            if (loop == null || !loop.IsInitialized || loop.IsEndingFloorEntered ||
                loop.ProgressManager == null || loop.AnomalyGameManager == null) return;
            Save(flow.Campaign?.IsActive == true
                ? CheckpointData.ForCampaign(flow.Campaign.CaptureCheckpoint()) : CheckpointData.ForStairway(0, 0));
        }

        public void SaveCampaignCheckpoint(CampaignCheckpoint checkpoint)
        {
            if (autosaveEnabled && pending == null) Save(CheckpointData.ForCampaign(checkpoint));
        }

        private void Save(CheckpointData checkpoint)
        {
            if (!store.Save(checkpoint))
                Debug.LogError("CHECKPOINT_AUTOSAVE_FAILED", this);
        }
    }
}
