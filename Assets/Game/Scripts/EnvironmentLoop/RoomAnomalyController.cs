using System.Collections.Generic;
using Zero.AnomalyPrototype.Anomalies;
using Zero.AnomalyPrototype.Player;
using UnityEngine;

namespace Zero.EnvironmentLoop
{
    public sealed class RoomAnomalyController : MonoBehaviour
    {
        [SerializeField] private Transform roomRoot;
        [SerializeField] private AnomalyBehaviour[] anomalySlots;
        [SerializeField] private CorrectArrowController correctArrowController;
        private bool runtimeNormalStateCaptured;
        private bool anomalyBaselineRestored;

        public Transform RoomRoot => roomRoot != null ? roomRoot : transform;
        public IReadOnlyList<AnomalyBehaviour> AnomalySlots => anomalySlots;
        public AnomalyBehaviour ActiveAnomaly { get; private set; }
        public bool HasActiveAnomaly => ActiveAnomaly != null;
        public CorrectArrowController CorrectArrow => correctArrowController;
        public TravelDirection ArrowDirection { get; private set; } = TravelDirection.Up;
        public TravelDirection PreparedArrowDirection { get; private set; } = TravelDirection.Up;
        public TravelDirection ExpectedDirection => HasActiveAnomaly
            ? Reverse(ArrowDirection)
            : ArrowDirection;
        public bool IsRoundPrepared { get; private set; }

        public void Configure(
            Transform configuredRoomRoot,
            AnomalyBehaviour[] configuredSlots,
            CorrectArrowController arrowController = null)
        {
            roomRoot = configuredRoomRoot;
            anomalySlots = configuredSlots;
            correctArrowController = arrowController != null
                ? arrowController
                : GetComponent<CorrectArrowController>();
            CaptureNormalState();
        }

        public void InitializeRoom(Transform player, Camera playerCamera)
        {
            if (player == null)
            {
                var fp = FindFirstObjectByType<FirstPersonController>();
                if (fp != null)
                {
                    player = fp.transform;
                    if (playerCamera == null) playerCamera = fp.PlayerCamera;
                }
            }

            if (correctArrowController == null)
            {
                correctArrowController = GetComponent<CorrectArrowController>();
            }

            if (anomalySlots == null || anomalySlots.Length == 0)
            {
                anomalySlots = GetComponentsInChildren<AnomalyBehaviour>(true);
            }

            for (int i = 0; i < anomalySlots.Length; i++)
            {
                AnomalyBehaviour anomaly = anomalySlots[i];
                if (anomaly == null)
                {
                    continue;
                }

                if (anomaly is SeaOfBloodAnomaly seaOfBlood)
                {
                    seaOfBlood.BindPlayer(player, playerCamera);
                }
                else if (anomaly is WatchingStatueAnomaly watching)
                {
                    watching.BindPlayer(player);
                }
                else if (anomaly is ObserverStatueAnomaly observer)
                {
                    observer.BindPlayer(player, playerCamera);
                }
                else if (anomaly is ArrowLightFlickerAnomaly lightFlicker)
                {
                    lightFlicker.BindRoomContext(playerCamera, correctArrowController);
                }
                else if (anomaly is ArrowFlipOnSightAnomaly arrowFlip)
                {
                    arrowFlip.BindRoomContext(playerCamera, this);
                }
                else if (anomaly is BalloonsAnomaly balloons)
                {
                    balloons.BindRoomContext(playerCamera, correctArrowController);
                }
                else if (anomaly is CeilingCreatureAnomaly ceilingCreature)
                {
                    ceilingCreature.BindPlayer(player, playerCamera, correctArrowController);
                }
                else if (anomaly is WrongReflectionAnomaly wrongReflection)
                {
                    wrongReflection.BindPlayer(player, playerCamera);
                }
                else if (anomaly is TwoChildrenAnomaly twoChildren)
                {
                    twoChildren.BindPlayer(player, playerCamera);
                }
                else if (anomaly is HiddenRoomAnomaly hiddenRoom)
                {
                    hiddenRoom.BindRoomContext(playerCamera, correctArrowController);
                    hiddenRoom.BindPlayer(player);
                }
                else if (anomaly is HorrorDollAnomaly horrorDoll)
                {
                    horrorDoll.BindPlayer(player, playerCamera);
                }
                else if (anomaly is RotatingArrowAnomaly rotatingArrow)
                {
                    rotatingArrow.BindRoomContext(correctArrowController, playerCamera);
                }
                else if (anomaly is EyePictureLookAnomaly eyePicture)
                {
                    eyePicture.BindPlayer(player);
                }

                anomaly.CaptureNormalState();
            }

            runtimeNormalStateCaptured = true;
            anomalyBaselineRestored = false;
            ResetRoom();
            ActiveAnomaly = null;
        }

        public void PrepareRound(TravelDirection arrowDirection)
        {
            ResetRoom();
            PreparedArrowDirection = arrowDirection;
            ArrowDirection = arrowDirection;
            correctArrowController?.SetDirection(arrowDirection);
            IsRoundPrepared = true;
        }

        public void CaptureNormalState()
        {
            if (anomalySlots == null)
            {
                return;
            }

            for (int i = 0; i < anomalySlots.Length; i++)
            {
                anomalySlots[i]?.CaptureNormalState();
            }
        }

        public void ResetRoom(bool forceRestore = false)
        {
            if (forceRestore) anomalyBaselineRestored = false;
            PreparedArrowDirection = TravelDirection.Up;
            if (!runtimeNormalStateCaptured)
            {
                ActiveAnomaly = null;
                IsRoundPrepared = false;
                return;
            }

            ResetAnomaliesOnly();
            ArrowDirection = TravelDirection.Up;
            correctArrowController?.ResetArrow();
            IsRoundPrepared = false;
        }

        public bool Activate(string anomalyId)
        {
            // The arrow and prepared-round lock belong to the segment round. Activating
            // anomaly content must never reroll or reset either of them.
            ResetAnomaliesOnly();
            if (string.IsNullOrEmpty(anomalyId) || anomalySlots == null)
            {
                return false;
            }

            for (int i = 0; i < anomalySlots.Length; i++)
            {
                AnomalyBehaviour anomaly = anomalySlots[i];
                if (anomaly == null || anomaly.Data == null || anomaly.Data.Id != anomalyId)
                {
                    continue;
                }

                ActiveAnomaly = anomaly;
                anomalyBaselineRestored = false;
                ActiveAnomaly.Activate();
                return true;
            }

            return false;
        }

        public void ApplyAnomalyArrowDirection(TravelDirection direction)
        {
            ArrowDirection = direction;
            correctArrowController?.SetLogicalDirection(direction);
        }

        public AnomalyBehaviour[] GetEligibleAnomalies(int maximumDifficulty)
        {
            var result = new List<AnomalyBehaviour>();
            if (anomalySlots == null)
            {
                return result.ToArray();
            }

            int difficulty = Mathf.Clamp(maximumDifficulty, 1, 3);
            for (int i = 0; i < anomalySlots.Length; i++)
            {
                AnomalyBehaviour anomaly = anomalySlots[i];
                if (anomaly != null && anomaly.Data != null && anomaly.Data.Difficulty <= difficulty)
                {
                    result.Add(anomaly);
                }
            }

            return result.ToArray();
        }

        private void ResetAnomaliesOnly()
        {
            // PrepareRound and Activate follow ResetSegment. Do not reset every
            // animator, mirror and navigation agent again when already clean.
            if (anomalyBaselineRestored && ActiveAnomaly == null)
            {
                return;
            }

            if (anomalySlots != null)
            {
                for (int i = 0; i < anomalySlots.Length; i++)
                {
                    anomalySlots[i]?.ResetAnomaly();
                }
            }

            ActiveAnomaly = null;
            anomalyBaselineRestored = true;
        }

        private static TravelDirection Reverse(TravelDirection direction)
        {
            return direction == TravelDirection.Up ? TravelDirection.Down : TravelDirection.Up;
        }

    }
}
