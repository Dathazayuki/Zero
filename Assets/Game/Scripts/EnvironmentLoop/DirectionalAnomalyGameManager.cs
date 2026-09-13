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
        [SerializeField, Range(0f, 1f)] private float anomalyChance = 0.55f;
        [SerializeField, Range(1, 3)] private int maximumDifficulty = 3;
        [SerializeField, Min(1)] private int recentHistorySize = 2;

        private readonly Queue<string> recentAnomalyIds = new Queue<string>();
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
        private readonly RunCompletionGate completionGate = new RunCompletionGate();

        private const int MinimumFloorNumber = 0;
        private const int MaximumFloorNumber = 8;

        public bool CurrentRoomHasAnomaly => currentRoom != null && currentRoom.HasActiveAnomaly;
        public TravelDirection CurrentArrowDirection => currentRoom != null
            ? currentRoom.ArrowDirection
            : TravelDirection.Up;
        public TravelDirection CurrentExpectedDirection => currentRoom != null
            ? currentRoom.ExpectedDirection
            : TravelDirection.Up;
        public string ActiveAnomalyName => currentRoom?.ActiveAnomaly?.Data != null
            ? currentRoom.ActiveAnomaly.Data.DisplayName
            : "None";
        public int RoundNumber { get; private set; }
        public int CorrectDecisions { get; private set; }
        public int IncorrectDecisions { get; private set; }
        public int DisplayedFloorNumber { get; private set; }
        public bool HasPendingDecision => pendingDecision;
        public bool LastDecisionWasCorrect { get; private set; }
        public TravelDirection LastPlayerDirection { get; private set; }
        public bool HasCompletedRun => completionGate.IsCompleted;
        public FinalRunState FinalState { get; private set; }

        [Header("Arrow Direction Constraints")]
        [SerializeField, Range(1, 4)] private int maximumConsecutiveArrowDirection = 2;

        private FloorSegment lastRecordedFloorSegment;
        private TravelDirection lastRecordedArrowDirection = TravelDirection.Up;
        private int consecutiveArrowDirectionCount = 1;
        private TravelDirection nextPlannedArrowDirection = TravelDirection.Up;

        public int ConsecutiveArrowCount => consecutiveArrowDirectionCount;
        public int MaximumConsecutiveArrowDirection => maximumConsecutiveArrowDirection;
        public TravelDirection NextPlannedArrowDirection => nextPlannedArrowDirection;

        public event Action RoundPrepared;
        public event Action<bool, TravelDirection> DecisionResolved;
        public event Action RunCompleted;
        public event Action<TravelDirection> FinalFloorReady;

        public void Initialize(StairLoopManager manager, FirstPersonController playerController)
        {
            loopManager = manager;
            player = playerController;
            RoundNumber = 1;
            CorrectDecisions = 0;
            IncorrectDecisions = 0;
            DisplayedFloorNumber = MinimumFloorNumber;
            pendingDecision = false;
            completionGate.Reset();
            FinalState = FinalRunState.Normal;
            recentAnomalyIds.Clear();

            RoomAnomalyController[] rooms = loopManager != null
                ? loopManager.transform.root.GetComponentsInChildren<RoomAnomalyController>(true)
                : Array.Empty<RoomAnomalyController>();
            Camera playerCamera = player != null ? player.PlayerCamera : null;
            for (int i = 0; i < rooms.Length; i++)
            {
                rooms[i].InitializeRoom(player != null ? player.transform : null, playerCamera);
            }

            ResetArrowStreakTracking();

            FloorSegment[] activeSegments = loopManager?.GetOrderedSegments() ?? Array.Empty<FloorSegment>();
            FloorSegment current = loopManager?.CurrentSegment;
            for (int i = 0; i < activeSegments.Length; i++)
            {
                FloorSegment seg = activeSegments[i];
                if (seg == current)
                {
                    PrepareSegment(seg, false, lastRecordedArrowDirection);
                }
                else
                {
                    PrepareSegment(seg, UnityEngine.Random.value <= anomalyChance, nextPlannedArrowDirection);
                }
            }

            RefreshCurrentRoom();
            ApplyFloorNumberToCurrentSegment(true);
            RoundPrepared?.Invoke();
        }

        public void PrepareSegment(FloorSegment segment)
        {
            UpdateArrowStreakForCurrentSegment();
            PrepareSegment(segment,
                UnityEngine.Random.value <= anomalyChance,
                nextPlannedArrowDirection);
        }

        public void ResetRun()
        {
            GameFlowController.Instance?.ResetEndingForRun();
            if (loopManager != null && (loopManager.IsEndingFloorEntered || loopManager.IsEndingRouteArmed))
            {
                loopManager.ResetToInitialState();
            }
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
            recentAnomalyIds.Clear();

            FloorSegment[] segments = loopManager?.GetAllPooledSegments() ?? Array.Empty<FloorSegment>();
            for (int i = 0; i < segments.Length; i++)
            {
                segments[i]?.GetComponentInChildren<RoomAnomalyController>(true)?.ResetRoom();
                segments[i]?.HideFloorNumber();
            }

            ResetArrowStreakTracking();

            FloorSegment[] activeSegments = loopManager?.GetOrderedSegments() ?? Array.Empty<FloorSegment>();
            FloorSegment current = loopManager?.CurrentSegment;
            for (int i = 0; i < activeSegments.Length; i++)
            {
                FloorSegment seg = activeSegments[i];
                if (seg == current)
                {
                    PrepareSegment(seg, false, lastRecordedArrowDirection);
                }
                else
                {
                    PrepareSegment(seg, UnityEngine.Random.value <= anomalyChance, nextPlannedArrowDirection);
                }
            }

            RefreshCurrentRoom();
            ApplyFloorNumberToCurrentSegment(true);
            RoundPrepared?.Invoke();
            Debug.Log(
                $"ANOMALY_RUN_RESET: round={RoundNumber}, floor={DisplayedFloorNumber}, " +
                $"current={loopManager?.CurrentSegment?.SegmentId ?? "None"}, arrow={lastRecordedArrowDirection}",
                this);
        }

        public void PrepareSegment(
            FloorSegment segment,
            bool shouldHaveAnomaly,
            TravelDirection arrowDirection)
        {
            RoomAnomalyController room = GetRoom(segment);
            if (room == null)
            {
                Debug.LogError($"ANOMALY_ROOM_MISSING: {segment?.SegmentId ?? "No segment"}", this);
                return;
            }

            room.PrepareRound(arrowDirection);
            if (shouldHaveAnomaly && !ActivateWeightedAnomaly(room))
            {
                Debug.LogError(
                    $"ANOMALY_PREPARE_FALLBACK_NORMAL: {segment.SegmentId} has no eligible anomaly.",
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
            if (FinalState != FinalRunState.EndingFloorEntered || HasCompletedRun ||
                DisplayedFloorNumber != MaximumFloorNumber || GameFlowController.Instance == null ||
                !GameFlowController.Instance.BeginEndingFromEndDoor())
            {
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
                $"consecutive={consecutiveArrowDirectionCount}, nextPlanned={nextPlannedArrowDirection}",
                this);
        }

        private bool ActivateWeightedAnomaly(RoomAnomalyController room)
        {
            AnomalyBehaviour[] pool = room.GetEligibleAnomalies(maximumDifficulty);
            AnomalyBehaviour selected = WeightedPick(pool, true);
            if (selected == null || selected.Data == null || !room.Activate(selected.Data.Id))
            {
                return false;
            }

            return true;
        }

        // Called once per successful regular-floor entry, before preparing reserves.
        // Normal rooms do not erase history: A -> Normal -> A is also a repeat.
        public void RecordCurrentAnomalyEncounter()
        {
            RoomAnomalyController room = GetRoom(loopManager?.CurrentSegment);
            if (room == null || !room.IsRoundPrepared || !room.HasActiveAnomaly)
                return;

            string id = room.ActiveAnomaly?.Data?.Id;
            if (string.IsNullOrEmpty(id))
                return;

            if (recentAnomalyIds.Contains(id))
            {
                // A reserve may have been prepared before the last encounter.
                // Preserve its authored arrow when replacing its anomaly.
                TravelDirection arrow = room.ArrowDirection;
                room.PrepareRound(arrow);
                if (!ActivateWeightedAnomaly(room))
                {
                    Debug.Log("ANOMALY_HISTORY_EXHAUSTED: using Normal", room);
                    return;
                }
                id = room.ActiveAnomaly.Data.Id;
            }

            recentAnomalyIds.Enqueue(id);
            while (recentAnomalyIds.Count > Mathf.Max(1, recentHistorySize))
                recentAnomalyIds.Dequeue();
            Debug.Log($"ANOMALY_ENCOUNTER_RECORDED: id={id}", room);
        }

        private AnomalyBehaviour WeightedPick(AnomalyBehaviour[] pool, bool excludeRecent)
        {
            float totalWeight = 0f;
            for (int i = 0; i < pool.Length; i++)
            {
                AnomalyBehaviour anomaly = pool[i];
                if (anomaly == null || anomaly.Data == null ||
                    (excludeRecent && recentAnomalyIds.Contains(anomaly.Data.Id)))
                {
                    continue;
                }

                totalWeight += Mathf.Max(0.01f, anomaly.Data.SpawnWeight);
            }

            if (totalWeight <= 0f)
            {
                return null;
            }

            float roll = UnityEngine.Random.value * totalWeight;
            for (int i = 0; i < pool.Length; i++)
            {
                AnomalyBehaviour anomaly = pool[i];
                if (anomaly == null || anomaly.Data == null ||
                    (excludeRecent && recentAnomalyIds.Contains(anomaly.Data.Id)))
                {
                    continue;
                }

                roll -= Mathf.Max(0.01f, anomaly.Data.SpawnWeight);
                if (roll <= 0f)
                {
                    return anomaly;
                }
            }

            return null;
        }

        private void RefreshCurrentRoom()
        {
            currentRoom = GetRoom(loopManager?.CurrentSegment);
        }

        private static RoomAnomalyController GetRoom(FloorSegment segment)
        {
            return segment != null
                ? segment.GetComponentInChildren<RoomAnomalyController>(true)
                : null;
        }

        private void ApplyFloorNumberToCurrentSegment(bool reportConfigurationErrors = false)
        {
            FloorSegment[] segments = loopManager?.GetAllPooledSegments() ?? Array.Empty<FloorSegment>();
            for (int i = 0; i < segments.Length; i++)
            {
                segments[i]?.HideFloorNumber();
            }

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
            Debug.Log(
                $"FLOOR_NUMBER_APPLIED: current={current.SegmentId}, number={DisplayedFloorNumber}",
                current);
        }
    }
}
