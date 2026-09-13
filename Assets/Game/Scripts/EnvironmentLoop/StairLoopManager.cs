using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Zero.EnvironmentLoop
{
    public sealed class StairLoopManager : MonoBehaviour
    {
        public const int MinimumPoolSize = 9;
        public const int ActiveWindowSize = 5;
        public const int RequiredPoolSize = MinimumPoolSize;

        [SerializeField] private FloorProgressManager progressManager;
        [SerializeField] private FloorSegment[] initialSegments;
        [SerializeField, Min(2)] private int initialCurrentIndex = 2;
        [SerializeField] private DirectionalAnomalyGameManager anomalyGameManager;
        [SerializeField] private FloorSegment endingSegmentPrefab;
        [SerializeField, Min(0)] private int maximumCurrentShadowCasters = 4;

        private readonly List<FloorSegment> pooledSegments = new List<FloorSegment>();
        private readonly List<FloorSegment> inactiveSegments = new List<FloorSegment>();
        private readonly LinkedList<FloorSegment> activeSegments = new LinkedList<FloorSegment>();
        private readonly Dictionary<FloorSegment, Pose> initialRootPoses = new Dictionary<FloorSegment, Pose>();
        private FloorSegment endingSegmentInstance;
        private FloorSegment displacedEndingRouteSegment;
        private TravelDirection endingRouteDirection;
        private bool endingRouteArmed;
        private bool endingFloorEntered;
        private Coroutine endingCleanup;
        public bool IsEndingCleanupPending => endingCleanup != null;

        public FloorSegment CurrentSegment { get; private set; }
        public FloorSegment BottomSegment => activeSegments.First?.Value;
        public FloorSegment TopSegment => activeSegments.Last?.Value;
        public FloorSegment Below2Segment => activeSegments.First?.Value;
        public FloorSegment Below1Segment => activeSegments.First?.Next?.Value;
        public FloorSegment Above1Segment => activeSegments.Last?.Previous?.Value;
        public FloorSegment Above2Segment => activeSegments.Last?.Value;
        public int SegmentCount => activeSegments.Count;
        public int ActiveSegmentCount => activeSegments.Count;
        public int InactiveSegmentCount => inactiveSegments.Count;
        public int PoolSize => pooledSegments.Count;
        public int RecycleCount { get; private set; }
        public bool IsInitialized { get; private set; }
        public string LastOperationMessage { get; private set; } = "Not initialized";
        public string PoolOrder => BuildPoolOrder();
        public FloorProgressManager ProgressManager => progressManager;
        public DirectionalAnomalyGameManager AnomalyGameManager => anomalyGameManager;
        public bool IsEndingRouteArmed => endingRouteArmed;
        public bool IsEndingFloorEntered => endingFloorEntered;

        public void Configure(
            FloorProgressManager progress,
            FloorSegment[] segments,
            int currentIndex = 2)
        {
            progressManager = progress;
            initialSegments = segments;
            initialCurrentIndex = currentIndex;
        }

        public void SetAnomalyGameManager(DirectionalAnomalyGameManager manager)
        {
            UnbindFinalFloorEvents();
            anomalyGameManager = manager;
            BindFinalFloorEvents();
        }

        public void SetEndingSegmentPrefab(FloorSegment prefab)
        {
            endingSegmentPrefab = prefab;
        }

        public void Initialize()
        {
            if (IsInitialized)
            {
                return;
            }

            if (progressManager == null)
            {
                Debug.LogError("StairLoopManager requires FloorProgressManager.", this);
                return;
            }

            if (initialSegments == null || initialSegments.Length < MinimumPoolSize)
            {
                Debug.LogError($"Environment loop requires a pool of at least {MinimumPoolSize} FloorSegments.", this);
                return;
            }

            pooledSegments.Clear();
            inactiveSegments.Clear();
            activeSegments.Clear();
            var uniqueSegments = new HashSet<FloorSegment>();
            for (int i = 0; i < initialSegments.Length; i++)
            {
                FloorSegment segment = initialSegments[i];
                if (segment == null || !uniqueSegments.Add(segment))
                {
                    Debug.LogError($"FloorSegment pool entry {i} is missing or duplicated.", this);
                    ClearRuntimeCollections();
                    return;
                }

                segment.gameObject.SetActive(true);
                if (!initialRootPoses.ContainsKey(segment))
                {
                    initialRootPoses.Add(segment, new Pose(segment.transform.position, segment.transform.rotation));
                }

                segment.InitializeSegment();
                segment.ConfigureNavigation(this);
                int triggerCount = segment.ConfigureCommitTriggers(this);
                if (triggerCount != 1)
                {
                    Debug.LogError(
                        $"FLOOR_TRIGGER_CONFIGURATION_INVALID: {segment.SegmentId} requires exactly one " +
                        $"SegmentCommitTrigger, found {triggerCount}.",
                        segment);
                    ClearRuntimeCollections();
                    return;
                }

                pooledSegments.Add(segment);
            }

            int currentIndex = Mathf.Clamp(initialCurrentIndex, 2, pooledSegments.Count - 3);
            FloorSegment below2 = pooledSegments[currentIndex - 2];
            FloorSegment below1 = pooledSegments[currentIndex - 1];
            CurrentSegment = pooledSegments[currentIndex];
            FloorSegment above1 = pooledSegments[currentIndex + 1];
            FloorSegment above2 = pooledSegments[currentIndex + 2];

            below1.PlaceBelow(CurrentSegment);
            below2.PlaceBelow(below1);
            above1.PlaceAbove(CurrentSegment);
            above2.PlaceAbove(above1);
            activeSegments.AddLast(below2);
            activeSegments.AddLast(below1);
            activeSegments.AddLast(CurrentSegment);
            activeSegments.AddLast(above1);
            activeSegments.AddLast(above2);

            for (int i = 0; i < pooledSegments.Count; i++)
            {
                FloorSegment segment = pooledSegments[i];
                if (segment == below2 || segment == below1 || segment == CurrentSegment ||
                    segment == above1 || segment == above2)
                {
                    continue;
                }

                ReleaseToInactivePool(segment);
            }

            progressManager.Initialize();
            BindFinalFloorEvents();
            CurrentSegment.ConsumeCommitTriggers();
            ApplyNavigationRoles();
            ApplyLightingBudgets();
            Physics.SyncTransforms();
            IsInitialized = true;
            LastOperationMessage = "Ready: Below2 -> Below1 -> Current -> Above1 -> Above2";
            Debug.Log(
                $"ENV_LOOP_READY: current={CurrentSegment.SegmentId}, active={PoolOrder}, " +
                $"pool={PoolSize}, inactive={InactiveSegmentCount}",
                this);
        }

        public void ResetToInitialState()
        {
            Zero.AnomalyPrototype.Flow.GameFlowController.Instance?.ResetEndingForRun();
            ClearEndingRoute(true);
            if (!IsInitialized)
            {
                Initialize();
                return;
            }

            inactiveSegments.Clear();
            activeSegments.Clear();
            for (int i = 0; i < pooledSegments.Count; i++)
            {
                FloorSegment segment = pooledSegments[i];
                if (segment == null)
                {
                    continue;
                }

                segment.gameObject.SetActive(true);
                if (initialRootPoses.TryGetValue(segment, out Pose pose))
                {
                    segment.transform.SetPositionAndRotation(pose.position, pose.rotation);
                }

                segment.ResetSegment();
                segment.ConfigureNavigation(this);
                segment.ConfigureCommitTriggers(this);
            }

            int currentIndex = Mathf.Clamp(initialCurrentIndex, 2, pooledSegments.Count - 3);
            FloorSegment below2 = pooledSegments[currentIndex - 2];
            FloorSegment below1 = pooledSegments[currentIndex - 1];
            CurrentSegment = pooledSegments[currentIndex];
            FloorSegment above1 = pooledSegments[currentIndex + 1];
            FloorSegment above2 = pooledSegments[currentIndex + 2];

            below1.PlaceBelow(CurrentSegment);
            below2.PlaceBelow(below1);
            above1.PlaceAbove(CurrentSegment);
            above2.PlaceAbove(above1);
            activeSegments.AddLast(below2);
            activeSegments.AddLast(below1);
            activeSegments.AddLast(CurrentSegment);
            activeSegments.AddLast(above1);
            activeSegments.AddLast(above2);

            for (int i = 0; i < pooledSegments.Count; i++)
            {
                FloorSegment segment = pooledSegments[i];
                if (segment != below2 && segment != below1 && segment != CurrentSegment &&
                    segment != above1 && segment != above2)
                {
                    ReleaseToInactivePool(segment);
                }
            }

            progressManager.Initialize();
            RecycleCount = 0;
            CurrentSegment.ConsumeCommitTriggers();
            ApplyNavigationRoles();
            ApplyLightingBudgets();
            Physics.SyncTransforms();
            LastOperationMessage = "Reset to initial five-segment window";
            Debug.Log(
                $"ENV_LOOP_RESET_TO_INITIAL: current={CurrentSegment.SegmentId}, active={PoolOrder}, " +
                $"pool={PoolSize}, inactive={InactiveSegmentCount}",
                this);
        }

        public bool TryCommit(FloorSegment targetSegment)
        {
            if (endingFloorEntered) return Reject("Terminal floor has locked commits");
            if (!IsInitialized || endingFloorEntered)
            {
                return Reject("Loop manager is not initialized or ending floor already entered");
            }

            if (targetSegment == null)
            {
                return Reject("Commit trigger has no FloorSegment owner");
            }

            LinkedListNode<FloorSegment> currentNode = activeSegments.Find(CurrentSegment);
            if (currentNode == null)
            {
                return Reject("Current FloorSegment is not present in the active window");
            }

            TravelDirection direction;
            if (targetSegment == currentNode.Next?.Value)
            {
                direction = TravelDirection.Up;
            }
            else if (targetSegment == currentNode.Previous?.Value)
            {
                direction = TravelDirection.Down;
            }
            else
            {
                return Reject(
                    $"Commit owner {targetSegment.SegmentId} is not adjacent to current {CurrentSegment.SegmentId}");
            }

            if (endingRouteArmed)
            {
                Debug.Log($"ENDING_ROUTE_WRONG: direction={direction}, reset=0", this);
                ClearEndingRoute(true);
            }

            if (activeSegments.Count != ActiveWindowSize)
            {
                return Reject($"Active window requires exactly {ActiveWindowSize} segments");
            }

            if (inactiveSegments.Count < 4)
            {
                return Reject("Five-segment active window requires four inactive reserves before every commit");
            }

            if (anomalyGameManager != null && !anomalyGameManager.RegisterPendingDecision(direction))
            {
                return Reject("Current segment has no valid locked gameplay round");
            }

            FloorSegment origin = CurrentSegment;
            FloorSegment[] oldWindow = GetOrderedSegments();
            FloorSegment newBelow2 = TakeRandomInactiveSegment();
            FloorSegment newBelow1 = TakeRandomInactiveSegment();
            FloorSegment newAbove1 = TakeRandomInactiveSegment();
            FloorSegment newAbove2 = TakeRandomInactiveSegment();
            if (newBelow2 == null || newBelow1 == null || newAbove1 == null || newAbove2 == null)
            {
                return Reject("Dynamic pool transaction could not acquire four distinct reserve segments");
            }

            CurrentSegment = targetSegment;
            CurrentSegment.ConsumeCommitTriggers();
            anomalyGameManager?.RecordCurrentAnomalyEncounter();
            progressManager.Commit(direction);

            for (int i = 0; i < oldWindow.Length; i++)
            {
                if (oldWindow[i] != targetSegment)
                {
                    ReleaseToInactivePool(oldWindow[i]);
                }
            }

            ActivateBelow(newBelow1, CurrentSegment);
            ActivateBelow(newBelow2, newBelow1);
            ActivateAbove(newAbove1, CurrentSegment);
            ActivateAbove(newAbove2, newAbove1);
            activeSegments.Clear();
            activeSegments.AddLast(newBelow2);
            activeSegments.AddLast(newBelow1);
            activeSegments.AddLast(CurrentSegment);
            activeSegments.AddLast(newAbove1);
            activeSegments.AddLast(newAbove2);

            anomalyGameManager?.PrepareSegment(newBelow2);
            anomalyGameManager?.PrepareSegment(newBelow1);
            anomalyGameManager?.PrepareSegment(newAbove1);
            anomalyGameManager?.PrepareSegment(newAbove2);
            ApplyNavigationRoles();
            ApplyLightingBudgets();
            Physics.SyncTransforms();
            RecycleCount++;
            anomalyGameManager?.CompletePendingDecision();

            FloorSegment replacementBehind = direction == TravelDirection.Up ? newBelow1 : newAbove1;
            LastOperationMessage =
                $"Committed {direction}; {replacementBehind.SegmentId} replaced {origin.SegmentId} behind the Player";
            Debug.Log(
                $"ENV_LOOP_DYNAMIC_COMMIT: direction={direction}, logicalFloor={progressManager.LogicalFloorIndex}, " +
                $"current={CurrentSegment.SegmentId}, previous={origin.SegmentId}, " +
                $"replacementBehind={replacementBehind.SegmentId}, active={PoolOrder}, inactive={InactiveSegmentCount}",
                this);
            return true;
        }

        public bool TryEnterEndingFloor(FloorSegment endingSegment, TravelDirection direction)
        {
            if (!IsInitialized || !endingRouteArmed || endingFloorEntered ||
                endingSegment == null || endingSegment != endingSegmentInstance ||
                direction != endingRouteDirection || anomalyGameManager == null)
            {
                return Reject("Ending floor entry is not armed for this segment and direction");
            }

            if (!anomalyGameManager.RegisterPendingDecision(direction) ||
                !anomalyGameManager.MarkEndingFloorEntered())
            {
                anomalyGameManager.CancelPendingDecision();
                return Reject("Final floor decision could not be registered");
            }

            endingFloorEntered = true;
            endingRouteArmed = false;
            CurrentSegment = endingSegmentInstance;
            progressManager.Commit(direction);

            activeSegments.Clear();

            endingSegmentInstance.HideFloorNumber();
            anomalyGameManager.CompletePendingDecision();
            endingSegmentInstance.GetComponentInChildren<EndFloorSequenceController>(true)?.EnterEndingFloor();
            endingCleanup = StartCoroutine(DeactivateEndingNeighbours());
            LastOperationMessage = "Entered terminal ending floor; all other floors hidden and loop completed";
            Debug.Log("ENDING_FLOOR_ENTERED", endingSegmentInstance);
            return true;
        }

        private IEnumerator DeactivateEndingNeighbours()
        {
            // Let the closed blocker render before changing anything behind it.
            yield return null;
            for (int i = 0; i < pooledSegments.Count; i++)
            {
                FloorSegment segment = pooledSegments[i];
                if (segment == null || !segment.gameObject.activeInHierarchy) continue;
                segment.SetNavigationActive(false);
                segment.gameObject.SetActive(false);
                yield return null;
            }
            endingCleanup = null;
            Debug.Log("ENDING_FLOOR_CLEANUP_COMPLETE", this);
            var flow = Zero.AnomalyPrototype.Flow.GameFlowController.Instance;
            if (flow != null && !flow.DeferEndingLoadUntilWhiteout && !flow.IsTransitioning)
                flow.BeginEndingPreload();
        }

        public FloorSegment GetAdjacentCurrent(TravelDirection direction)
        {
            LinkedListNode<FloorSegment> currentNode = activeSegments.Find(CurrentSegment);
            return direction == TravelDirection.Up ? currentNode?.Next?.Value : currentNode?.Previous?.Value;
        }

        public FloorSegment[] GetOrderedSegments()
        {
            var result = new FloorSegment[activeSegments.Count];
            int index = 0;
            LinkedListNode<FloorSegment> node = activeSegments.First;
            while (node != null)
            {
                result[index++] = node.Value;
                node = node.Next;
            }

            return result;
        }

        public FloorSegment[] GetAllPooledSegments()
        {
            return pooledSegments.ToArray();
        }

        public FloorSegment[] GetInactiveSegments()
        {
            return inactiveSegments.ToArray();
        }

        public bool ValidateConnections(float positionTolerance = 0.002f, float angleTolerance = 0.1f)
        {
            LinkedListNode<FloorSegment> node = activeSegments.First;
            while (node != null && node.Next != null)
            {
                if (!node.Next.Value.IsConnectedAbove(node.Value, positionTolerance, angleTolerance))
                {
                    return false;
                }

                node = node.Next;
            }

            return true;
        }

        private FloorSegment TakeRandomInactiveSegment()
        {
            if (inactiveSegments.Count == 0)
            {
                return null;
            }

            int index = Random.Range(0, inactiveSegments.Count);
            FloorSegment selected = inactiveSegments[index];
            inactiveSegments.RemoveAt(index);
            return selected;
        }

        private void ActivateBelow(FloorSegment segment, FloorSegment target)
        {
            segment.ResetSegment();
            segment.PlaceBelow(target);
            segment.gameObject.SetActive(true);
        }

        private void ActivateAbove(FloorSegment segment, FloorSegment target)
        {
            segment.ResetSegment();
            segment.PlaceAbove(target);
            segment.gameObject.SetActive(true);
        }

        private void ReleaseToInactivePool(FloorSegment segment)
        {
            if (segment == null)
            {
                return;
            }

            segment.ResetSegment();
            segment.gameObject.SetActive(false);
            if (!inactiveSegments.Contains(segment))
            {
                inactiveSegments.Add(segment);
            }
        }

        private void ApplyLightingBudgets()
        {
            int enabledLights = 0;
            int shadowCastingLights = 0;
            FloorSegment[] ordered = GetOrderedSegments();
            for (int i = 0; i < ordered.Length; i++)
            {
                FloorSegment segment = ordered[i];
                bool isCurrent = segment == CurrentSegment;
                bool isAdjacent = i == 1 || i == 3;
                segment.ApplyLightingBudget(
                    isCurrent || isAdjacent,
                    isCurrent,
                    isCurrent ? maximumCurrentShadowCasters : 0);
                enabledLights += segment.CountEnabledLights();
                shadowCastingLights += segment.CountEnabledLights(true);
            }

            Debug.Log(
                $"ENV_LOOP_LIGHT_BUDGET: current={CurrentSegment?.SegmentId ?? "None"}, " +
                $"enabledLights={enabledLights}, shadowCasters={shadowCastingLights}/" +
                $"{maximumCurrentShadowCasters}, " +
                "policy=current-shadows/adjacent-no-shadows/far-fill-only",
                this);
        }

        private void ApplyNavigationRoles()
        {
            int enabledSurfaces = 0;
            for (int i = 0; i < pooledSegments.Count; i++)
            {
                FloorSegment segment = pooledSegments[i];
                segment.SetNavigationActive(segment == CurrentSegment);
                enabledSurfaces += segment.CountEnabledNavigationSurfaces();
            }

            Debug.Log(
                $"ENV_LOOP_NAVIGATION_ROLE: current={CurrentSegment?.SegmentId ?? "None"}, " +
                $"enabledSurfaces={enabledSurfaces}",
                this);
        }

        private void ClearRuntimeCollections()
        {
            pooledSegments.Clear();
            inactiveSegments.Clear();
            activeSegments.Clear();
            CurrentSegment = null;
        }

        private void BindFinalFloorEvents()
        {
            if (anomalyGameManager == null)
            {
                return;
            }

            anomalyGameManager.FinalFloorReady -= HandleFinalFloorReady;
            anomalyGameManager.FinalFloorReady += HandleFinalFloorReady;
        }

        private void UnbindFinalFloorEvents()
        {
            if (anomalyGameManager != null)
            {
                anomalyGameManager.FinalFloorReady -= HandleFinalFloorReady;
            }
        }

        private void HandleFinalFloorReady(TravelDirection expectedDirection)
        {
            if (endingFloorEntered)
            {
                return;
            }

            ClearEndingRoute(true);
            if (endingSegmentPrefab == null || CurrentSegment == null)
            {
                Debug.LogError("ENDING_FLOOR_CONFIGURATION_INVALID: endingSegmentPrefab is missing.", this);
                return;
            }

            displacedEndingRouteSegment = GetAdjacentCurrent(expectedDirection);
            if (displacedEndingRouteSegment == null)
            {
                Debug.LogError($"ENDING_FLOOR_SPAWN_REJECTED: no adjacent segment for {expectedDirection}.", this);
                return;
            }

            displacedEndingRouteSegment.gameObject.SetActive(false);
            endingSegmentInstance = Instantiate(endingSegmentPrefab, transform);
            endingSegmentInstance.name = endingSegmentPrefab.name + " (Runtime)";
            if (expectedDirection == TravelDirection.Up)
            {
                endingSegmentInstance.PlaceAbove(CurrentSegment);
            }
            else
            {
                endingSegmentInstance.PlaceBelow(CurrentSegment);
            }

            endingSegmentInstance.HideFloorNumber();
            SegmentCommitTrigger[] legacyTriggers =
                endingSegmentInstance.GetComponentsInChildren<SegmentCommitTrigger>(true);
            for (int i = 0; i < legacyTriggers.Length; i++)
            {
                legacyTriggers[i].enabled = false;
            }

            EndFloorEntryTrigger entryTrigger = endingSegmentInstance.GetComponentInChildren<EndFloorEntryTrigger>(true);
            EndFloorSequenceController sequenceController =
                endingSegmentInstance.GetComponentInChildren<EndFloorSequenceController>(true);
            if (entryTrigger == null || sequenceController == null)
            {
                Debug.LogError(
                    $"ENDING_FLOOR_CONFIGURATION_INVALID: entry={entryTrigger != null}, " +
                    $"sequence={sequenceController != null}.",
                    endingSegmentInstance);
                ClearEndingRoute(true);
                return;
            }

            endingRouteDirection = expectedDirection;
            endingRouteArmed = true;
            endingFloorEntered = false;
            entryTrigger.Configure(this, endingSegmentInstance, expectedDirection);
            endingSegmentInstance.gameObject.SetActive(true);
            Physics.SyncTransforms();
            Debug.Log($"ENDING_FLOOR_SPAWNED: direction={expectedDirection}", endingSegmentInstance);
        }

        private void ClearEndingRoute(bool restoreDisplacedSegment)
        {
            if (endingCleanup != null)
            {
                StopCoroutine(endingCleanup);
                endingCleanup = null;
            }
            if (restoreDisplacedSegment && displacedEndingRouteSegment != null)
            {
                displacedEndingRouteSegment.gameObject.SetActive(true);
            }

            displacedEndingRouteSegment = null;
            if (endingSegmentInstance != null)
            {
                endingSegmentInstance.GetComponentInChildren<EndFloorEntryTrigger>(true)?.Disarm();
                endingSegmentInstance.gameObject.SetActive(false);
                Destroy(endingSegmentInstance.gameObject);
            }

            endingSegmentInstance = null;
            endingRouteArmed = false;
            endingFloorEntered = false;
        }

        private void OnDestroy()
        {
            UnbindFinalFloorEvents();
        }

        private bool Reject(string reason)
        {
            LastOperationMessage = reason;
            return false;
        }

        private string BuildPoolOrder()
        {
            var builder = new StringBuilder();
            LinkedListNode<FloorSegment> node = activeSegments.First;
            while (node != null)
            {
                if (builder.Length > 0)
                {
                    builder.Append(" -> ");
                }

                builder.Append(node.Value != null ? node.Value.SegmentId : "Missing");
                node = node.Next;
            }

            return builder.ToString();
        }
    }
}
