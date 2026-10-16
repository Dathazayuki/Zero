using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zero.EnvironmentLoop;
using Zero.AnomalyPrototype.Player;

namespace Zero.AnomalyPrototype.Flow
{
    [DisallowMultipleComponent]
    public sealed class CampaignRuntime : MonoBehaviour
    {
        [SerializeField] private CampaignDefinition definition;
        private CampaignProgress progress;
        private CampaignSceneTransition travel;
        private CampaignLevelBootstrap currentLevel;
        private IMinigameSession session;
        private bool entranceCommitted;
        public GameFlowController Flow { get; private set; }
        public EncounterDiversityHistory Diversity { get; private set; } = new EncounterDiversityHistory();
        public bool IsActive => definition != null && progress != null;
        public bool CanPresentEndings => !IsActive || progress.Phase == CampaignProgress.PhaseKind.FinalEnding;
        public bool IsTravelling => travel != null && travel.IsBusy;
        public bool IsDestinationReady => travel != null && travel.IsReady;
        public CampaignProgress Progress => progress;
        public CampaignDefinition Definition => definition;
        public InfinityRoomDefinition CurrentRoom => IsActive ? definition.Stages[progress.StageIndex].InfinityRoom : null;
        public InfinityRoomDefinition DestinationRoom => !IsActive ? null : definition.Stages[
            progress.Phase == CampaignProgress.PhaseKind.NextStage ? progress.StageIndex + 1 : progress.StageIndex].InfinityRoom;
        public bool DestinationIsInfinity => IsActive && progress.Phase != CampaignProgress.PhaseKind.InfinityRoom &&
            progress.Phase != CampaignProgress.PhaseKind.Minigame;

        private void Awake()
        {
            Flow = GetComponent<GameFlowController>();
            travel = gameObject.AddComponent<CampaignSceneTransition>();
            if (definition != null) Configure(definition);
        }

        public bool Configure(CampaignDefinition authored)
        {
            if (IsTravelling || authored == null || !authored.TryValidate(out _)) return false;
            definition = authored;
            BeginNewGame();
            return true;
        }

        public void BeginNewGame()
        {
            UnbindSession();
            travel?.Cancel();
            currentLevel = null;
            entranceCommitted = false;
            Diversity.Restore(null);
            if (definition != null) progress = definition.CreateProgress();
        }

        public bool TryRestore(CampaignCheckpoint checkpoint)
        {
            if (!CanRestore(checkpoint)) return false;
            if (!IsActive) return true;
            try
            {
                var restored = definition.CreateProgress();
                restored.Restore(checkpoint);
                var stage = definition.Stages[restored.StageIndex];
                bool ready = restored.Phase == CampaignProgress.PhaseKind.InfinityRoom
                    ? stage.InfinityRoom.IsReady : stage.Minigame.IsReady;
                if (!ready) return false;
                progress = restored;
                Diversity.Restore(checkpoint.EncounteredIds);
                return true;
            }
            catch (ArgumentException exception)
            {
                Debug.LogWarning($"CAMPAIGN_RESTORE_REJECTED: {exception.Message}", this);
                return false;
            }
        }

        public bool CanRestore(CampaignCheckpoint checkpoint)
        {
            if (checkpoint == null || !checkpoint.IsValid()) return false;
            if (!IsActive) return checkpoint.CampaignId == CampaignCheckpoint.DefaultCampaignId &&
                checkpoint.StageId == CampaignCheckpoint.FirstStageId;
            if (checkpoint.CampaignId != definition.CampaignId) return false;
            foreach (CampaignDefinition.Stage stage in definition.Stages)
            {
                if (stage.StageId != checkpoint.StageId) continue;
                return checkpoint.Location == CampaignCheckpoint.LocationKind.InfinityRoomStart
                    ? stage.InfinityRoom.IsReady : stage.Minigame.IsReady &&
                        Application.CanStreamedLevelBeLoaded(stage.Minigame.SceneName);
            }
            return false;
        }

        public bool TryResolveDestination(out string sceneName)
        {
            sceneName = null;
            if (!IsActive || CanPresentEndings) return false;
            if (progress.Phase == CampaignProgress.PhaseKind.InfinityRoom)
            {
                MinigameDefinition game = definition.Stages[progress.StageIndex].Minigame;
                if (game.IsReady) sceneName = game.SceneName;
            }
            else if (progress.Phase == CampaignProgress.PhaseKind.NextStage && DestinationRoom.IsReady)
                sceneName = Flow.LoopSceneName;
            bool ready = sceneName != null && Application.CanStreamedLevelBeLoaded(sceneName);
            if (!ready) Debug.LogWarning($"CAMPAIGN_CONTENT_NOT_READY: stage={progress.StageId}, phase={progress.Phase}", this);
            return ready;
        }

        public void PrepareInitialRoom(InfiniteBuildingManager building)
        {
            if (!IsActive || IsTravelling) return;
            FirstPersonController player = FindPlayer(building.gameObject.scene);
            InfinityRoomDefinition room = progress.Phase == CampaignProgress.PhaseKind.Minigame
                ? definition.Stages[0].InfinityRoom : CurrentRoom;
            var bootstrap = building.gameObject.AddComponent<CampaignRoomBootstrap>();
            if (!bootstrap.Initialize(room, building, player, Diversity))
                Debug.LogError("CAMPAIGN_ROOM_CONFIGURATION_INVALID", building);
        }

        public bool BeginRide(EndFloorElevatorController source)
        {
            return !IsTravelling && TryResolveDestination(out string destination) &&
                travel.Begin(this, source, destination, DestinationIsInfinity);
        }

        public bool CommitRide(FirstPersonController player)
        {
            if (entranceCommitted || !IsDestinationReady || travel.Player != player) return false;
            entranceCommitted = true;
            SaveEntrance();
            Flow.SetCampaignPhase(progress.Phase == CampaignProgress.PhaseKind.Minigame
                ? GameFlowPhase.BackroomGameplay : GameFlowPhase.LoopGameplay);
            return true;
        }

        public bool AcceptArrival(CampaignLevelBootstrap level, bool infinity)
        {
            bool accepted = infinity ? progress.TryEnterNextStage() : progress.TryCompleteInfinityRoom();
            if (!accepted) return false;
            entranceCommitted = false;
            if (infinity) Diversity = level.RoomHistory;
            currentLevel = level;
            BindSession(infinity ? null : level.Session);
            Debug.Log($"CAMPAIGN_LEVEL_READY: stage={progress.StageId}, phase={progress.Phase}", this);
            return true;
        }

        public void AcceptRestoredLevel(CampaignLevelBootstrap level)
        {
            currentLevel = level;
            BindSession(level.Session);
        }

        public IEnumerator RestoreMinigame(FirstPersonController player)
        {
            string name = definition.Stages[progress.StageIndex].Minigame.SceneName;
            yield return travel.Restore(this, player, name);
        }

        public CampaignCheckpoint CaptureCheckpoint()
        {
            var location = progress.Phase == CampaignProgress.PhaseKind.Minigame ||
                progress.Phase == CampaignProgress.PhaseKind.NextStage
                ? CampaignCheckpoint.LocationKind.MinigameEntrance : CampaignCheckpoint.LocationKind.InfinityRoomStart;
            return new CampaignCheckpoint(definition.CampaignId, progress.StageId, location, Diversity.Capture());
        }

        public void CancelTravel() => travel?.Cancel();

        private void SaveEntrance() { Flow.Checkpoints?.SaveCampaignCheckpoint(CaptureCheckpoint()); }

        private void BindSession(IMinigameSession next)
        {
            UnbindSession();
            session = next;
            if (session != null) session.Completed += OnMinigameCompleted;
        }

        private void UnbindSession()
        {
            if (session != null) session.Completed -= OnMinigameCompleted;
            session = null;
        }

        private void OnMinigameCompleted()
        {
            if (!IsActive || !progress.TryCompleteMinigame()) return;
            Debug.Log($"CAMPAIGN_MINIGAME_COMPLETED: stage={progress.StageId}", this);
            if (CanPresentEndings) return;
            if (currentLevel?.ExitElevator != null) currentLevel.ExitElevator.MakeAvailable(null);
            else Debug.LogWarning("CAMPAIGN_EXIT_ELEVATOR_MISSING: author and assign the next-stage elevator", this);
        }

        private static FirstPersonController FindPlayer(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                FirstPersonController player = root.GetComponentInChildren<FirstPersonController>();
                if (player != null) return player;
            }
            return null;
        }

        private void OnDisable() { UnbindSession(); CancelTravel(); }
    }
}
