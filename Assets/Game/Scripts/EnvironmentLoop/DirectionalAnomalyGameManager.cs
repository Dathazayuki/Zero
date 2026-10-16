using System;
using System.Collections.Generic;
using Zero.AnomalyPrototype.Anomalies;
using Zero.AnomalyPrototype.Flow;
using Zero.AnomalyPrototype.Player;
using UnityEngine;

namespace Zero.EnvironmentLoop
{
    public enum FinalRunState
    {
        Normal,
        FinalFloorArmed,
        EndingFloorEntered,
        Completed
    }

    public sealed class DirectionalAnomalyGameManager : MonoBehaviour
    {
        [SerializeField, Range(1, 3)] private int maximumDifficulty = 3;
        [SerializeField, Min(1)] private int recentHistorySize = 2;

        private readonly AnomalyPacingDirector pacingDirector = new AnomalyPacingDirector();
        private AnomalyRosterSelector rosterSelector;
        private EncounterDiversityHistory diversityHistory;
        private StairLoopManager loopManager;
        private FirstPersonController player;
        private RoomAnomalyController currentRoom;
        private RoomAnomalyController pendingRoom;
        private bool pendingDecision;
        private bool pendingWasCorrect;
        private bool pendingHadAnomaly;
        private TravelDirection pendingArrowDirection;
        private TravelDirection pendingExpectedDirection;
        private TravelDirection pendingPlayerDirection;
        private FloorSegment lastRecordedEncounterSegment;
        private FloorSegment numberedSegment;
        private readonly RunCompletionGate completionGate = new RunCompletionGate();

        private const int MinimumFloorNumber = 0;
        private const int MaximumFloorNumber = 8;

        public bool CurrentRoomHasAnomaly => currentRoom != null && currentRoom.HasActiveAnomaly;
        public TravelDirection CurrentArrowDirection => currentRoom != null ? currentRoom.ArrowDirection : TravelDirection.Up;
        public TravelDirection CurrentExpectedDirection => currentRoom != null ? currentRoom.ExpectedDirection : TravelDirection.Up;
        public string ActiveAnomalyName => currentRoom?.ActiveAnomaly?.Data != null ? currentRoom.ActiveAnomaly.Data.DisplayName : "None";
        public int RoundNumber { get; private set; }
        public int CorrectDecisions { get; private set; }
        public int IncorrectDecisions { get; private set; }
        public int DisplayedFloorNumber { get; private set; }
        public bool HasPendingDecision => pendingDecision;
        public bool LastDecisionWasCorrect { get; private set; }
        public TravelDirection LastPlayerDirection { get; private set; }
        public bool HasCompletedRun => completionGate.IsCompleted;
        public FinalRunState FinalState { get; private set; }
        public int NormalStreak => pacingDirector.NormalStreak;
        public int AnomalyStreak => pacingDirector.AnomalyStreak;
        public int MajorCooldownRemaining => pacingDirector.MajorCooldownRemaining;

        [Header("Arrow Direction Constraints")]
        [SerializeField, Range(1, 4)] private int maximumConsecutiveArrowDirection = 2;

        private FloorSegment lastRecordedFloorSegment;
        private TravelDirection lastRecordedArrowDirection = TravelDirection.Up;
        private int consecutiveArrowDirectionCount = 1;
        private TravelDirection nextPlannedArrowDirection = TravelDirection.Up;
        private TravelDirection lastExpectedDirection;
        private int consecutiveExpectedCount;
        public int ConsecutiveExpectedCount => consecutiveExpectedCount;

        public int ConsecutiveArrowCount => consecutiveArrowDirectionCount;
        public int MaximumConsecutiveArrowDirection => maximumConsecutiveArrowDirection;
        public TravelDirection NextPlannedArrowDirection => nextPlannedArrowDirection;

        public event Action RoundPrepared;
        public event Action<bool, TravelDirection> DecisionResolved;
        public event Action RunCompleted;
        public EndingRoute? SelectedEndingRoute { get; private set; }

        public bool TrySelectEndingRoute(EndingRoute route)
        {
            if (FinalState != FinalRunState.EndingFloorEntered || HasCompletedRun ||
                DisplayedFloorNumber != MaximumFloorNumber) return false;
            if (SelectedEndingRoute.HasValue) return SelectedEndingRoute.Value == route;
            SelectedEndingRoute = route;
            Debug.Log($"ENDING_ROUTE_SELECTED: {route}", this);
            return true;
        }

        public void ReleaseEndingRoute(EndingRoute route)
        {
            if (!HasCompletedRun && SelectedEndingRoute == route) SelectedEndingRoute = null;
        }
        public event Action<TravelDirection> FinalFloorReady;

        public void Initialize(StairLoopManager manager, FirstPersonController playerController)
        {
            loopManager = manager;
            if (playerController == null) playerController = FindFirstObjectByType<FirstPersonController>();
            player = playerController;
            RoundNumber = 1;
            CorrectDecisions = 0;
            IncorrectDecisions = 0;
            DisplayedFloorNumber = MinimumFloorNumber;
            pendingDecision = false;
            completionGate.Reset();
            FinalState = FinalRunState.Normal;

            RoomAnomalyController[] rooms = loopManager != null ? loopManager.transform.root.GetComponentsInChildren<RoomAnomalyController>(true) : Array.Empty<RoomAnomalyController>();
            Camera playerCamera = player != null ? player.PlayerCamera : null;
            for (int i = 0; i < rooms.Length; i++)
            {
                rooms[i].InitializeRoom(player != null ? player.transform : null, playerCamera);
            }

            PrepareInitialRound();
        }

        public void PrepareSegment(FloorSegment segment)
        {
            UpdateArrowStreakForCurrentSegment();
            PreparePacedSegment(segment, nextPlannedArrowDirection);
        }

        public void ResetRun()
        {
            SelectedEndingRoute = null;
            GameFlowController.Instance?.ResetEndingForRun();
            if (loopManager != null && (loopManager.IsEndingFloorEntered || loopManager.IsEndingRouteArmed)) loopManager.ResetToInitialState();
            RoundNumber = 1;
            CorrectDecisions = 0;
            IncorrectDecisions = 0;
            DisplayedFloorNumber = MinimumFloorNumber;
            LastDecisionWasCorrect = false;
            LastPlayerDirection = default;
            pendingDecision = false;
            pendingRoom = null;
            pendingWasCorrect = false;
            pendingHadAnomaly = false;
            pendingArrowDirection = default;
            pendingExpectedDirection = default;
            pendingPlayerDirection = default;
            completionGate.Reset();
            FinalState = FinalRunState.Normal;

            FloorSegment[] segments = loopManager?.GetAllPooledSegments() ?? Array.Empty<FloorSegment>();
            for (int i = 0; i < segments.Length; i++)
            {
                segments[i]?.RoomAnomalies?.ResetRoom();
                segments[i]?.HideFloorNumber();
            }

            PrepareInitialRound();
            Debug.Log($"ANOMALY_RUN_RESET: round={RoundNumber}, floor={DisplayedFloorNumber}, current={loopManager?.CurrentSegment?.SegmentId ?? "None"}, arrow={lastRecordedArrowDirection}", this);
        }

        public bool RestoreCheckpointProgress(int displayedNumber)
        {
            if (displayedNumber < MinimumFloorNumber || displayedNumber > MaximumFloorNumber ||
                loopManager == null || !loopManager.IsInitialized || loopManager.IsEndingFloorEntered)
                return false;

            ResetRun();
            DisplayedFloorNumber = displayedNumber;
            FloorSegment current = loopManager.CurrentSegment;
            pacingDirector.Reset(recentHistorySize, false);
            lastRecordedEncounterSegment = null;
            TravelDirection arrow = UnityEngine.Random.value < 0.5f
                ? TravelDirection.Up : TravelDirection.Down;
            PreparePacedSegment(current, arrow);
            lastRecordedFloorSegment = null;
            UpdateArrowStreakForCurrentSegment();
            RecordCurrentEncounter();
            RefreshCurrentRoom();
            ApplyFloorNumberToCurrentSegment();
            FloorSegment[] active = loopManager.GetOrderedSegments();
            for (int i = 0; i < active.Length; i++)
                if (active[i] != current) PrepareSegment(active[i]);
            if (displayedNumber == MaximumFloorNumber)
            {
                FinalState = FinalRunState.FinalFloorArmed;
                FinalFloorReady?.Invoke(CurrentExpectedDirection);
            }

            RoundPrepared?.Invoke();
            Debug.Log($"CHECKPOINT_FLOOR_RESTORED: logical={loopManager.ProgressManager.LogicalFloorIndex}, " +
                      $"number={displayedNumber}", this);
            return true;
        }

        public void PrepareSegment(
            FloorSegment segment,
            bool shouldHaveAnomaly,
            TravelDirection arrowDirection)
        {
            PrepareSegmentCore(
                segment,
                shouldHaveAnomaly,
                arrowDirection,
                GetProjectedPacingProgress(segment),
                shouldHaveAnomaly);
        }

        private void PreparePacedSegment(FloorSegment segment, TravelDirection arrowDirection)
        {
            int progress = GetProjectedPacingProgress(segment);
            PacingDecision decision = pacingDirector.PlanNextEncounter(progress, UnityEngine.Random.value);
            PrepareSegmentCore(
                segment,
                decision.ShouldHaveAnomaly,
                arrowDirection,
                progress,
                decision.WasForced,
                true);

            RoomAnomalyController room = GetRoom(segment);
            string tier = room?.ActiveAnomaly?.Data != null
                ? room.ActiveAnomaly.Data.PacingTier.ToString()
                : "Normal";
            Debug.Log(
                $"PACING_DECISION: segment={segment?.SegmentId ?? "None"}, progress={progress}, " +
                $"beat={(room != null && room.HasActiveAnomaly ? "Anomaly" : "Normal")}, " +
                $"forced={decision.WasForced}, reason={decision.Reason}, tier={tier}",
                segment != null ? segment : this);
            if (decision.Reason == PacingDecisionReason.MajorRecovery)
            {
                Debug.Log("PACING_MAJOR_RECOVERY_FORCED", segment != null ? segment : this);
            }
        }

        private void PrepareSegmentCore(
            FloorSegment segment,
            bool shouldHaveAnomaly,
            TravelDirection arrowDirection,
            int projectedProgress,
            bool allowHistoryRelaxation,
            bool constrainDirections = false)
        {
            RoomAnomalyController room = GetRoom(segment);
            if (room == null)
            {
                Debug.LogError($"ANOMALY_ROOM_MISSING: {segment?.SegmentId ?? "No segment"}", this);
                return;
            }

            AnomalyBehaviour selected = shouldHaveAnomaly
                ? SelectWeightedAnomaly(room, projectedProgress, allowHistoryRelaxation)
                : null;
            bool hasAnomaly = selected != null;
            bool keepsPreparedArrow = selected != null && selected.KeepsPreparedArrowAsExpected;
            if (constrainDirections)
            {
                arrowDirection = ChoosePreparedArrow(hasAnomaly, keepsPreparedArrow, arrowDirection);
            }
            room.PrepareRound(arrowDirection);
            if (selected != null) room.Activate(selected.Data.Id);
            if (shouldHaveAnomaly && !hasAnomaly)
            {
                Debug.LogWarning(
                    $"PACING_POOL_EXHAUSTED: segment={segment.SegmentId}, progress={projectedProgress}; " +
                    "using Normal.",
                    segment);
            }

            Debug.Log(
                $"SEGMENT_ROUND_PREPARED: segment={segment.SegmentId}, arrow={room.ArrowDirection}, " +
                $"anomaly={room.HasActiveAnomaly}, expected={room.ExpectedDirection}, " +
                $"active={room.ActiveAnomaly?.Data?.DisplayName ?? "None"}",
                segment);
        }

        public bool IsCorrectDecision(
            TravelDirection playerDirection,
            TravelDirection arrowDirection,
            bool hasAnomaly)
        {
            TravelDirection expected = hasAnomaly ? Reverse(arrowDirection) : arrowDirection;
            return playerDirection == expected;
        }

        public bool RegisterPendingDecision(TravelDirection playerDirection)
        {
            if (pendingDecision)
            {
                Debug.LogWarning("ANOMALY_DECISION_REJECTED: another decision is already pending.", this);
                return false;
            }

            RefreshCurrentRoom();
            if (currentRoom == null || !currentRoom.IsRoundPrepared)
            {
                Debug.LogError("ANOMALY_DECISION_REJECTED: current Room has no prepared round.", this);
                return false;
            }

            pendingDecision = true;
            pendingRoom = currentRoom;
            pendingHadAnomaly = currentRoom.HasActiveAnomaly;
            pendingArrowDirection = currentRoom.ArrowDirection;
            pendingExpectedDirection = currentRoom.ExpectedDirection;
            pendingPlayerDirection = playerDirection;
            pendingWasCorrect = playerDirection == pendingExpectedDirection;

            Debug.Log(
                $"ANOMALY_DECISION_PENDING: round={RoundNumber}, arrow={pendingArrowDirection}, " +
                $"anomaly={pendingHadAnomaly}, expected={pendingExpectedDirection}, " +
                $"player={playerDirection}, correct={pendingWasCorrect}",
                this);
            return true;
        }

        public void CancelPendingDecision()
        {
            pendingDecision = false;
            pendingRoom = null;
        }

        public void CompletePendingDecision()
        {
            UpdateArrowStreakForCurrentSegment();
            RefreshCurrentRoom();
            if (!pendingDecision)
            {
                ApplyFloorNumberToCurrentSegment();
                RoundPrepared?.Invoke();
                return;
            }

            LastDecisionWasCorrect = pendingWasCorrect;
            LastPlayerDirection = pendingPlayerDirection;
            if (pendingWasCorrect)
            {
                CorrectDecisions++;
                DisplayedFloorNumber = Mathf.Min(DisplayedFloorNumber + 1, MaximumFloorNumber);
            }
            else
            {
                IncorrectDecisions++;
                DisplayedFloorNumber = MinimumFloorNumber;
            }

            RoundNumber++;
            ApplyFloorNumberToCurrentSegment();
            Debug.Log(
                $"FLOOR_NUMBER_UPDATED: correct={pendingWasCorrect}, number={DisplayedFloorNumber}",
                this);

            pendingDecision = false;
            pendingRoom = null;
            DecisionResolved?.Invoke(LastDecisionWasCorrect, LastPlayerDirection);
            RoundPrepared?.Invoke();

            if (!LastDecisionWasCorrect && FinalState == FinalRunState.FinalFloorArmed)
            {
                FinalState = FinalRunState.Normal;
            }
            else if (LastDecisionWasCorrect && DisplayedFloorNumber == MaximumFloorNumber &&
                     FinalState == FinalRunState.Normal)
            {
                FinalState = FinalRunState.FinalFloorArmed;
                TravelDirection expected = CurrentExpectedDirection;
                Debug.Log($"FINAL_FLOOR_READY: expected={expected}", this);
                FinalFloorReady?.Invoke(expected);
            }
        }

        public bool MarkEndingFloorEntered()
        {
            if (FinalState != FinalRunState.FinalFloorArmed)
            {
                return false;
            }

            FinalState = FinalRunState.EndingFloorEntered;
            return true;
        }

        public bool CompleteRunFromEndingDoor()
        {
            if (GameFlowController.Instance?.Campaign?.IsActive == true) return false;
            return CompleteRun(EndingRoute.OfficeDoor);
        }

        public bool CompleteRunFromBackroomExit() => CompleteRun(EndingRoute.BackroomElevator);

        public bool CompleteRun(EndingRoute route)
        {
            if (FinalState != FinalRunState.EndingFloorEntered || HasCompletedRun ||
                DisplayedFloorNumber != MaximumFloorNumber || GameFlowController.Instance == null ||
                !TrySelectEndingRoute(route))
            {
                return false;
            }

            bool accepted = route == EndingRoute.OfficeDoor
                ? GameFlowController.Instance.BeginEndingFromEndDoor()
                : GameFlowController.Instance.IsBackroomReady;
            if (!accepted)
            {
                ReleaseEndingRoute(route);
                return false;
            }

            completionGate.TryComplete(DisplayedFloorNumber, MaximumFloorNumber);
            FinalState = FinalRunState.Completed;
            Debug.Log($"ENDING_RUN_COMPLETED: floor={DisplayedFloorNumber}, round={RoundNumber}", this);
            RunCompleted?.Invoke();
            return true;
        }

        public void ForceCurrentRound(bool hasAnomaly)
        {
            ForceCurrentRound(hasAnomaly, CurrentArrowDirection);
        }

        public void ForceCurrentRound(bool hasAnomaly, TravelDirection arrowDirection)
        {
            FloorSegment current = loopManager?.CurrentSegment;
            if (current == null)
            {
                return;
            }

            PrepareSegment(current, hasAnomaly, arrowDirection);
            lastRecordedFloorSegment = current;
            lastRecordedArrowDirection = arrowDirection;
            consecutiveArrowDirectionCount = 1;
            lastExpectedDirection = GetEffectiveExpectedDirection(GetRoom(current));
            consecutiveExpectedCount = 1;
            if (consecutiveArrowDirectionCount >= maximumConsecutiveArrowDirection)
            {
                nextPlannedArrowDirection = Reverse(lastRecordedArrowDirection);
            }
            else
            {
                nextPlannedArrowDirection = UnityEngine.Random.value < 0.5f ? TravelDirection.Up : TravelDirection.Down;
            }

            RefreshCurrentRoom();
            RoundPrepared?.Invoke();
        }

        public bool ForceCurrentAnomaly(string anomalyId)
        {
            FloorSegment current = loopManager?.CurrentSegment;
            RoomAnomalyController room = GetRoom(current);
            if (room == null || string.IsNullOrEmpty(anomalyId))
            {
                return false;
            }

            room.PrepareRound(CurrentArrowDirection);
            bool activated = room.Activate(anomalyId);
            RefreshCurrentRoom();
            RoundPrepared?.Invoke();
            Debug.Log(
                $"ANOMALY_FORCED: segment={current.SegmentId}, id={anomalyId}, active={activated}",
                this);
            return activated;
        }

        public void SetFloorNumberForValidation(int number)
        {
            DisplayedFloorNumber = Mathf.Clamp(number, MinimumFloorNumber, MaximumFloorNumber);
            ApplyFloorNumberToCurrentSegment();
        }

        public bool ForceCompleteRunForValidation()
        {
            DisplayedFloorNumber = MaximumFloorNumber;
            ApplyFloorNumberToCurrentSegment();
            FinalState = FinalRunState.EndingFloorEntered;
            if (!completionGate.TryComplete(DisplayedFloorNumber, MaximumFloorNumber))
            {
                return false;
            }

            FinalState = FinalRunState.Completed;
            Debug.Log("ANOMALY_RUN_COMPLETED_FOR_VALIDATION", this);
            RunCompleted?.Invoke();
            return true;
        }

        public static TravelDirection Reverse(TravelDirection direction)
        {
            return direction == TravelDirection.Up ? TravelDirection.Down : TravelDirection.Up;
        }

        private void ResetArrowStreakTracking()
        {
            FloorSegment current = loopManager?.CurrentSegment;
            lastRecordedFloorSegment = current;
            lastRecordedArrowDirection = UnityEngine.Random.value < 0.5f ? TravelDirection.Up : TravelDirection.Down;
            consecutiveArrowDirectionCount = 1;
            lastExpectedDirection = lastRecordedArrowDirection;
            consecutiveExpectedCount = 1;

            if (consecutiveArrowDirectionCount >= maximumConsecutiveArrowDirection)
            {
                nextPlannedArrowDirection = Reverse(lastRecordedArrowDirection);
            }
            else
            {
                nextPlannedArrowDirection = UnityEngine.Random.value < 0.5f ? TravelDirection.Up : TravelDirection.Down;
            }
        }

        private void UpdateArrowStreakForCurrentSegment()
        {
            if (loopManager != null && loopManager.IsEndingFloorEntered) return;
            FloorSegment current = loopManager?.CurrentSegment;
            if (current == null || current == lastRecordedFloorSegment)
            {
                return;
            }

            RoomAnomalyController room = GetRoom(current);
            if (room == null || !room.IsRoundPrepared)
            {
                return;
            }

            lastRecordedFloorSegment = current;
            TravelDirection currentArrow = room.ArrowDirection;
            TravelDirection expected = GetEffectiveExpectedDirection(room);
            consecutiveExpectedCount = expected == lastExpectedDirection
                ? consecutiveExpectedCount + 1 : 1;
            lastExpectedDirection = expected;

            if (currentArrow == lastRecordedArrowDirection)
            {
                consecutiveArrowDirectionCount++;
            }
            else
            {
                lastRecordedArrowDirection = currentArrow;
                consecutiveArrowDirectionCount = 1;
            }

            if (consecutiveArrowDirectionCount >= maximumConsecutiveArrowDirection)
            {
                nextPlannedArrowDirection = Reverse(lastRecordedArrowDirection);
            }
            else
            {
                nextPlannedArrowDirection = UnityEngine.Random.value < 0.5f ? TravelDirection.Up : TravelDirection.Down;
            }

            Debug.Log(
                $"ARROW_STREAK_UPDATED: floor={current.SegmentId}, currentArrow={currentArrow}, " +
                $"consecutive={consecutiveArrowDirectionCount}, nextPlanned={nextPlannedArrowDirection}, " +
                $"expected={lastExpectedDirection}, expectedStreak={consecutiveExpectedCount}",
                this);
        }

        public void ConfigureCampaign(InfinityRoomDefinition definition, EncounterDiversityHistory history)
        {
            diversityHistory = history;
            rosterSelector = new AnomalyRosterSelector(pacingDirector);
            rosterSelector.Configure(definition, history);
        }

        private AnomalyBehaviour SelectWeightedAnomaly(RoomAnomalyController room,
            int projectedProgress, bool allowHistoryRelaxation)
        {
            if (rosterSelector == null) rosterSelector = new AnomalyRosterSelector(pacingDirector);
            return rosterSelector.Select(room.AnomalySlots ?? Array.Empty<AnomalyBehaviour>(),
                projectedProgress, maximumDifficulty);
        }

        private TravelDirection ChoosePreparedArrow(
            bool hasAnomaly,
            bool keepsPreparedArrow,
            TravelDirection preferredArrow)
        {
            // Pure preview: preparing either reserve never advances history.
            if (consecutiveExpectedCount >= 2)
            {
                TravelDirection expected = Reverse(lastExpectedDirection);
                return hasAnomaly && !keepsPreparedArrow ? Reverse(expected) : expected;
            }
            return consecutiveArrowDirectionCount >= maximumConsecutiveArrowDirection
                ? Reverse(lastRecordedArrowDirection) : preferredArrow;
        }

        private static TravelDirection GetEffectiveExpectedDirection(RoomAnomalyController room)
        {
            if (room?.ActiveAnomaly != null && room.ActiveAnomaly.KeepsPreparedArrowAsExpected)
            {
                return room.PreparedArrowDirection;
            }

            return room != null ? room.ExpectedDirection : TravelDirection.Up;
        }

        // Called once per successful regular-floor entry, before preparing reserves.
        public void RecordCurrentEncounter()
        {
            FloorSegment current = loopManager?.CurrentSegment;
            if (current == null || current == lastRecordedEncounterSegment)
            {
                return;
            }

            RoomAnomalyController room = GetRoom(current);
            if (room == null || !room.IsRoundPrepared)
            {
                return;
            }

            lastRecordedEncounterSegment = current;
            if (!room.HasActiveAnomaly || room.ActiveAnomaly?.Data == null)
            {
                pacingDirector.RecordNormal();
                Debug.Log(
                    $"PACING_ENCOUNTER_RECORDED: beat=Normal, normalStreak={pacingDirector.NormalStreak}, " +
                    $"anomalyStreak={pacingDirector.AnomalyStreak}, majorCooldown={pacingDirector.MajorCooldownRemaining}",
                    room);
                return;
            }

            AnomalyData data = room.ActiveAnomaly.Data;
            // Reserves were selected against this encounter history. Record the
            // content already visible to the player; never reroll on entry (also
            // preserves the explicit history-relaxation fallback for small pools).

            pacingDirector.RecordAnomaly(data.Id, data.Type, data.PacingTier);
            diversityHistory?.Record(data.Id);
            Debug.Log(
                $"PACING_ENCOUNTER_RECORDED: beat=Anomaly, tier={data.PacingTier}, id={data.Id}, " +
                $"normalStreak={pacingDirector.NormalStreak}, anomalyStreak={pacingDirector.AnomalyStreak}, " +
                $"majorCooldown={pacingDirector.MajorCooldownRemaining}",
                room);
        }

        // Legacy adapter retained for validators/debug code compiled against the previous API.
        public void RecordCurrentAnomalyEncounter()
        {
            RecordCurrentEncounter();
        }

        private int GetProjectedPacingProgress(FloorSegment segment)
        {
            int currentProgress;
            if (!pendingDecision)
            {
                currentProgress = DisplayedFloorNumber;
            }
            else
            {
                currentProgress = pendingWasCorrect
                    ? Mathf.Min(DisplayedFloorNumber + 1, MaximumFloorNumber)
                    : MinimumFloorNumber;
            }

            FloorSegment current = loopManager?.CurrentSegment;
            if (segment == null || current == null || segment == current)
            {
                return currentProgress;
            }

            TravelDirection direction;
            if (segment == loopManager.GetAdjacentCurrent(TravelDirection.Up))
            {
                direction = TravelDirection.Up;
            }
            else if (segment == loopManager.GetAdjacentCurrent(TravelDirection.Down))
            {
                direction = TravelDirection.Down;
            }
            else
            {
                return currentProgress;
            }

            RoomAnomalyController room = GetRoom(current);
            if (room == null || !room.IsRoundPrepared)
            {
                return currentProgress;
            }

            return direction == room.ExpectedDirection
                ? Mathf.Min(currentProgress + 1, MaximumFloorNumber)
                : MinimumFloorNumber;
        }

        private void ResetPacing(FloorSegment initialCurrent)
        {
            pacingDirector.Reset(recentHistorySize, true);
            lastRecordedEncounterSegment = initialCurrent;
            Debug.Log("PACING_RESET: initialNormal=true", this);
        }

        private void PrepareInitialRound()
        {
            ResetArrowStreakTracking();
            FloorSegment current = loopManager?.CurrentSegment;
            ResetPacing(current);
            if (current != null)
            {
                PrepareSegment(current, false, lastRecordedArrowDirection);
            }

            FloorSegment[] activeSegments = loopManager?.GetOrderedSegments() ?? Array.Empty<FloorSegment>();
            for (int i = 0; i < activeSegments.Length; i++)
            {
                if (activeSegments[i] != current)
                {
                    PreparePacedSegment(activeSegments[i], nextPlannedArrowDirection);
                }
            }

            RefreshCurrentRoom();
            ApplyFloorNumberToCurrentSegment(true);
            RoundPrepared?.Invoke();
        }

        private void RefreshCurrentRoom()
        {
            currentRoom = GetRoom(loopManager?.CurrentSegment);
        }

        private static RoomAnomalyController GetRoom(FloorSegment segment)
        {
            return segment != null ? segment.RoomAnomalies : null;
        }

        private void ApplyFloorNumberToCurrentSegment(bool reportConfigurationErrors = false)
        {
            if (reportConfigurationErrors)
            {
                FloorSegment[] segments = loopManager?.GetAllPooledSegments() ?? Array.Empty<FloorSegment>();
                for (int i = 0; i < segments.Length; i++)
                {
                    segments[i]?.HideFloorNumber();
                }
            }
            // Initialization/recycling guarantees hidden numbers on reserves.
            // Only the previously numbered floor needs updating during a commit.
            if (numberedSegment != null) numberedSegment.HideFloorNumber();
            numberedSegment = null;

            FloorSegment current = loopManager?.CurrentSegment;
            if (current == null)
            {
                return;
            }

            if (FinalState == FinalRunState.EndingFloorEntered || FinalState == FinalRunState.Completed ||
                loopManager.IsEndingFloorEntered)
            {
                current.HideFloorNumber();
                return;
            }

            if (!current.HasFloorNumberDisplay)
            {
                if (reportConfigurationErrors)
                {
                    Debug.LogWarning($"FLOOR_NUMBER_MISSING: {current.SegmentId}/Floor_Number", current);
                }

                return;
            }

            current.ShowFloorNumber(DisplayedFloorNumber);
            numberedSegment = current;
            Debug.Log(
                $"FLOOR_NUMBER_APPLIED: current={current.SegmentId}, number={DisplayedFloorNumber}",
                current);
        }
    }
}
