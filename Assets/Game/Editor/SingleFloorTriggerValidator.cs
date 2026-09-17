using System;
using System.Collections.Generic;
using System.Linq;
using Zero.AnomalyPrototype.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Zero.EnvironmentLoop.Editor
{
    public static class SingleFloorTriggerValidator
    {
        private const string ScenePath = "Assets/Game/Scenes/InfiniteStairwayScene.unity";
        private const int MaximumCurrentShadowCasters = 4;

        [MenuItem("Tools/Environment Loop/Validate Single Floor Trigger")]
        public static void RunFromScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Stop Play Mode before running Single Floor Trigger validation.");
                return;
            }

            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedForValidation = !scene.IsValid() || !scene.isLoaded;
            if (openedForValidation)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            }

            try
            {
                GameObject root = scene.GetRootGameObjects()
                    .FirstOrDefault(candidate => candidate.name == "InfiniteStairwayScene");
                Require(root != null, "InfiniteStairwayScene root is missing.");
                ValidateHierarchy(root);
                Debug.Log(
                    "SINGLE_TRIGGER_VALIDATION_PASS: one trigger per floor, current-only Number, " +
                    "single-use trigger lifecycle, bidirectional commits, stable pooled identities, " +
                    "and bounded realtime shadows.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"SINGLE_TRIGGER_VALIDATION_FAIL: {exception.Message}\n{exception.StackTrace}");
            }
            finally
            {
                if (openedForValidation && scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        public static void ValidateHierarchy(GameObject authoredRoot)
        {
            Require(authoredRoot != null, "Authored scene root is null.");
            InfiniteBuildingManager authoredBuilding =
                authoredRoot.GetComponentInChildren<InfiniteBuildingManager>(true);
            FirstPersonController authoredPlayer =
                authoredRoot.GetComponentInChildren<FirstPersonController>(true);
            Require(authoredBuilding != null, "InfiniteBuildingManager is missing.");
            Require(authoredPlayer != null, "Player is missing.");

            FloorSegment[] authoredSegments = authoredRoot.GetComponentsInChildren<FloorSegment>(true);
            Require(authoredSegments.Length >= StairLoopManager.MinimumPoolSize,
                $"Scene requires at least {StairLoopManager.MinimumPoolSize} FloorSegments.");
            for (int i = 0; i < authoredSegments.Length; i++)
            {
                RequireSingleTrigger(authoredSegments[i], "authored hierarchy");
            }

            GameObject cloneRoot = null;
            try
            {
                cloneRoot = UnityEngine.Object.Instantiate(authoredRoot);
                cloneRoot.name = "SingleFloorTriggerValidationClone";
                InfiniteBuildingManager building =
                    cloneRoot.GetComponentInChildren<InfiniteBuildingManager>(true);
                FirstPersonController player = cloneRoot.GetComponentInChildren<FirstPersonController>(true);
                building.InitializeEnvironment();

                StairLoopManager manager = building.StairLoopManager;
                DirectionalAnomalyGameManager anomalyGame = building.AnomalyGameManager;
                Require(manager != null && manager.IsInitialized, "StairLoopManager failed to initialize.");
                Require(anomalyGame != null, "DirectionalAnomalyGameManager failed to initialize.");

                FloorSegment[] pool = manager.GetAllPooledSegments();
                int[] stableIds = pool.Select(segment => segment.GetInstanceID()).OrderBy(id => id).ToArray();
                int transformCount = cloneRoot.GetComponentsInChildren<Transform>(true).Length;
                Vector3 playerPosition = player.transform.position;
                Quaternion playerRotation = player.transform.rotation;

                ValidateRuntimeState(manager, anomalyGame.DisplayedFloorNumber, "initialization");
                ValidateArrowTruthTable(manager, anomalyGame);
                ValidateFloorNumberRule(manager, anomalyGame);

                for (int i = 0; i < 20; i++)
                {
                    AdvanceOneFloor(manager, TravelDirection.Up, $"20-UP step {i + 1}");
                }

                for (int i = 0; i < 20; i++)
                {
                    AdvanceOneFloor(manager, TravelDirection.Down, $"20-DOWN step {i + 1}");
                }

                for (int i = 0; i < 40; i++)
                {
                    TravelDirection direction = i % 2 == 0 ? TravelDirection.Up : TravelDirection.Down;
                    AdvanceOneFloor(manager, direction, $"40-direction-change step {i + 1}");
                }

                int[] finalIds = manager.GetAllPooledSegments()
                    .Select(segment => segment.GetInstanceID()).OrderBy(id => id).ToArray();
                Require(stableIds.SequenceEqual(finalIds), "Pooled FloorSegment identities changed.");
                Require(cloneRoot.GetComponentsInChildren<Transform>(true).Length == transformCount,
                    "Hierarchy count changed during runtime transactions.");
                Require(Vector3.Distance(playerPosition, player.transform.position) < 0.0001f,
                    "Player position changed during pool transactions.");
                Require(Quaternion.Angle(playerRotation, player.transform.rotation) < 0.001f,
                    "Player rotation changed during pool transactions.");
            }
            finally
            {
                if (cloneRoot != null)
                {
                    UnityEngine.Object.DestroyImmediate(cloneRoot);
                }
            }
        }

        private static void ValidateArrowTruthTable(
            StairLoopManager manager,
            DirectionalAnomalyGameManager anomalyGame)
        {
            ValidateDecisionCase(manager, anomalyGame, false, TravelDirection.Up, TravelDirection.Up, true,
                "Normal + Arrow Up + Up");
            ValidateDecisionCase(manager, anomalyGame, false, TravelDirection.Up, TravelDirection.Down, false,
                "Normal + Arrow Up + Down");
            ValidateDecisionCase(manager, anomalyGame, false, TravelDirection.Down, TravelDirection.Down, true,
                "Normal + Arrow Down + Down");
            ValidateDecisionCase(manager, anomalyGame, false, TravelDirection.Down, TravelDirection.Up, false,
                "Normal + Arrow Down + Up");
            ValidateDecisionCase(manager, anomalyGame, true, TravelDirection.Up, TravelDirection.Down, true,
                "Anomaly + Arrow Up + Down");
            ValidateDecisionCase(manager, anomalyGame, true, TravelDirection.Up, TravelDirection.Up, false,
                "Anomaly + Arrow Up + Up");
            ValidateDecisionCase(manager, anomalyGame, true, TravelDirection.Down, TravelDirection.Up, true,
                "Anomaly + Arrow Down + Up");
            ValidateDecisionCase(manager, anomalyGame, true, TravelDirection.Down, TravelDirection.Down, false,
                "Anomaly + Arrow Down + Down");
        }

        private static void ValidateDecisionCase(
            StairLoopManager manager,
            DirectionalAnomalyGameManager anomalyGame,
            bool hasAnomaly,
            TravelDirection arrow,
            TravelDirection playerDirection,
            bool expectedCorrect,
            string context)
        {
            anomalyGame.ForceCurrentRound(hasAnomaly, arrow);
            RoomAnomalyController room = GetRoom(manager.CurrentSegment);
            Require(room != null && room.ArrowDirection == arrow, $"{context}: arrow was not applied.");
            Require(room.HasActiveAnomaly == hasAnomaly, $"{context}: anomaly state was not applied.");
            AdvanceOneFloor(manager, playerDirection, context);
            Require(anomalyGame.LastDecisionWasCorrect == expectedCorrect,
                $"{context}: decision result was {anomalyGame.LastDecisionWasCorrect}.");
        }

        private static void ValidateFloorNumberRule(
            StairLoopManager manager,
            DirectionalAnomalyGameManager anomalyGame)
        {
            anomalyGame.SetFloorNumberForValidation(0);
            ValidateRuntimeState(manager, 0, "Floor Number reset for validation");
            for (int number = 1; number <= 8; number++)
            {
                anomalyGame.ForceCurrentRound(false, TravelDirection.Up);
                AdvanceOneFloor(manager, TravelDirection.Up, $"Floor Number correct step {number}");
                Require(anomalyGame.DisplayedFloorNumber == number,
                    $"Correct step {number} displayed {anomalyGame.DisplayedFloorNumber}.");
            }

            anomalyGame.ForceCurrentRound(false, TravelDirection.Up);
            AdvanceOneFloor(manager, TravelDirection.Up, "Floor Number clamp at 8");
            Require(anomalyGame.DisplayedFloorNumber == 8, "Floor Number did not remain clamped at 8.");

            anomalyGame.ForceCurrentRound(false, TravelDirection.Up);
            AdvanceOneFloor(manager, TravelDirection.Down, "Floor Number wrong reset");
            Require(!anomalyGame.LastDecisionWasCorrect, "Wrong direction was unexpectedly accepted.");
            Require(anomalyGame.DisplayedFloorNumber == 0, "Wrong direction did not reset Floor Number to 0.");
        }

        private static void AdvanceOneFloor(
            StairLoopManager manager,
            TravelDirection direction,
            string context)
        {
            FloorSegment origin = manager.CurrentSegment;
            FloorSegment[] oldWindow = manager.GetOrderedSegments();
            FloorSegment target = manager.GetAdjacentCurrent(direction);
            Require(target != null, $"{context}: adjacent target is missing.");

            RoomAnomalyController targetRoom = GetRoom(target);
            Require(targetRoom != null && targetRoom.IsRoundPrepared,
                $"{context}: target Room was not prepared before entry.");
            TravelDirection targetArrow = targetRoom.ArrowDirection;
            bool targetAnomaly = targetRoom.HasActiveAnomaly;
            string targetAnomalyId = targetRoom.ActiveAnomaly?.Data?.Id;

            SegmentCommitTrigger trigger = target.GetCommitTrigger();
            Require(trigger != null && trigger.IsConfigured, $"{context}: target trigger is not configured.");
            Require(trigger.IsArmed, $"{context}: target trigger is not armed.");
            int recycleCount = manager.RecycleCount;
            Require(trigger.FireForValidation(), $"{context}: commit failed: {manager.LastOperationMessage}");

            Require(manager.CurrentSegment == target, $"{context}: target did not become Current.");
            Require(manager.RecycleCount == recycleCount + 1, $"{context}: recycle count is wrong.");
            Require(!trigger.IsArmed, $"{context}: Current trigger remained armed after commit.");
            Require(!trigger.FireForValidation(), $"{context}: consumed trigger committed more than once.");
            Require(manager.RecycleCount == recycleCount + 1,
                $"{context}: repeated trigger fire changed recycle count.");
            Require(!origin.gameObject.activeSelf, $"{context}: previous Current remained active.");

            FloorSegment[] newWindow = manager.GetOrderedSegments();
            Require(newWindow.Count(segment => oldWindow.Contains(segment)) == 1 && newWindow.Contains(target),
                $"{context}: target was not the only retained old-window segment.");
            Require(targetRoom.ArrowDirection == targetArrow && targetRoom.HasActiveAnomaly == targetAnomaly &&
                    targetRoom.ActiveAnomaly?.Data?.Id == targetAnomalyId,
                $"{context}: target Room was rerolled when it became Current.");
            Require(manager.ValidateConnections(0.001f, 0.05f), $"{context}: anchor alignment failed.");
            int expectedNumber = manager.AnomalyGameManager?.DisplayedFloorNumber ?? 0;
            ValidateRuntimeState(manager, expectedNumber, context);
        }

        private static void ValidateRuntimeState(
            StairLoopManager manager,
            int expectedCurrentNumber,
            string context)
        {
            Require(manager.PoolSize >= StairLoopManager.MinimumPoolSize,
                $"{context}: pool is smaller than the minimum.");
            Require(manager.ActiveSegmentCount == StairLoopManager.ActiveWindowSize,
                $"{context}: active window does not contain three segments.");
            Require(manager.InactiveSegmentCount == manager.PoolSize - StairLoopManager.ActiveWindowSize,
                $"{context}: inactive pool count is inconsistent.");

            FloorSegment[] ordered = manager.GetOrderedSegments();
            Require(ordered.Length == StairLoopManager.ActiveWindowSize && ordered[1] == manager.CurrentSegment,
                $"{context}: active order is not Below1-Current-Above1.");
            Require(CountEnabledShadowCasters(manager.CurrentSegment) <= MaximumCurrentShadowCasters,
                $"{context}: Current exceeds its realtime shadow-caster budget.");
            for (int i = 0; i < ordered.Length; i++)
            {
                if (ordered[i] != manager.CurrentSegment)
                {
                    Require(CountEnabledShadowCasters(ordered[i]) == 0,
                        $"{context}: non-Current {ordered[i].SegmentId} still casts realtime shadows.");
                }
            }

            var activeSet = new HashSet<FloorSegment>(ordered);
            FloorSegment[] pool = manager.GetAllPooledSegments();
            for (int i = 0; i < pool.Length; i++)
            {
                FloorSegment segment = pool[i];
                SegmentCommitTrigger trigger = RequireSingleTrigger(segment, context);
                Require(trigger.IsConfigured, $"{context}: {segment.SegmentId} trigger is not runtime-bound.");

                if (segment == manager.CurrentSegment)
                {
                    Require(!trigger.IsArmed, $"{context}: Current trigger is armed.");
                    ValidateFloorNumber(segment, expectedCurrentNumber, context);
                }
                else
                {
                    Require(trigger.IsArmed, $"{context}: non-Current {segment.SegmentId} trigger is not armed.");
                    if (activeSet.Contains(segment))
                    {
                        Require(trigger.gameObject.activeInHierarchy,
                            $"{context}: active neighbor {segment.SegmentId} trigger GameObject is disabled.");
                    }
                    ValidateFloorNumber(segment, null, context);
                }

                Require(segment.gameObject.activeSelf == activeSet.Contains(segment),
                    $"{context}: {segment.SegmentId} active state does not match pool role.");
            }
        }

        private static SegmentCommitTrigger RequireSingleTrigger(FloorSegment segment, string context)
        {
            Require(segment != null, $"{context}: FloorSegment reference is null.");
            SegmentCommitTrigger[] triggers = segment.GetComponentsInChildren<SegmentCommitTrigger>(true);
            Require(triggers.Length == 1,
                $"{context}: {segment.SegmentId} requires exactly one SegmentCommitTrigger, found {triggers.Length}.");
            BoxCollider triggerCollider = triggers[0].GetComponent<BoxCollider>();
            Require(triggerCollider != null && triggerCollider.isTrigger,
                $"{context}: {segment.SegmentId} trigger requires an isTrigger BoxCollider.");
            return triggers[0];
        }

        private static void ValidateFloorNumber(FloorSegment segment, int? expectedNumber, string context)
        {
            Transform root = FindNamedTransform(segment.transform, "Floor_Number");
            Require(root != null, $"{context}: {segment.SegmentId}/Floor_Number is missing.");
            int activeCount = 0;
            int activeNumber = -1;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (TryParseFloorNumber(child.name, out int number) && child.gameObject.activeSelf)
                {
                    activeCount++;
                    activeNumber = number;
                }
            }

            if (expectedNumber.HasValue)
            {
                Require(activeCount == 1 && activeNumber == expectedNumber.Value,
                    $"{context}: Current {segment.SegmentId} expected only Number {expectedNumber.Value} active.");
            }
            else
            {
                Require(activeCount == 0,
                    $"{context}: non-Current {segment.SegmentId} exposed Number {activeNumber}.");
            }
        }

        private static RoomAnomalyController GetRoom(FloorSegment segment)
        {
            return segment?.GetComponentInChildren<RoomAnomalyController>(true);
        }

        private static int CountEnabledShadowCasters(FloorSegment segment)
        {
            return segment.GetComponentsInChildren<Light>(true).Count(light =>
                light.enabled && light.gameObject.activeInHierarchy && light.shadows != LightShadows.None);
        }

        private static bool TryParseFloorNumber(string objectName, out int number)
        {
            string value = objectName.StartsWith("Number", StringComparison.OrdinalIgnoreCase)
                ? objectName.Substring("Number".Length)
                : objectName;
            return int.TryParse(value, out number) && number >= 0 && number <= 8;
        }

        private static Transform FindNamedTransform(Transform root, string objectName)
        {
            if (root == null || root.name == objectName)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindNamedTransform(root.GetChild(i), objectName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
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
