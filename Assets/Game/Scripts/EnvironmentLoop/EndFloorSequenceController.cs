using System.Collections;
using Zero.AnomalyPrototype.Flow;
using Zero.AnomalyPrototype.Player;
using UnityEngine;

namespace Zero.EnvironmentLoop
{
    public enum EndFloorSequenceState
    {
        Dormant,
        Entered,
        DoorOpening,
        Transitioning,
        Consumed
    }

    public sealed class EndFloorSequenceController : MonoBehaviour
    {
        [SerializeField] private GameObject backtrackBlocker;
        [SerializeField] private Animator doorAnimator;
        [SerializeField] private string doorOpenTrigger = "Open";
        [SerializeField, Min(0f)] private float doorOpenDuration = 0.8f;
        [SerializeField] private EndFloorDoorTrigger doorProximityTrigger;
        [SerializeField] private EndFloorDoorTrigger endingPortalTrigger;
        [SerializeField] private EndFloorElevatorController elevator;
        private bool elevatorSelected;

        public void SetElevatorSelected(bool selected)
        {
            elevatorSelected = selected;
            if (selected)
            {
                StopAllCoroutines();
                doorProximityTrigger?.SetArmed(false);
                endingPortalTrigger?.SetArmed(false);
            }
            else if (State != EndFloorSequenceState.Consumed)
            {
                State = EndFloorSequenceState.Entered;
                doorProximityTrigger?.SetArmed(true);
            }
        }

        public EndFloorSequenceState State { get; private set; } = EndFloorSequenceState.Dormant;

        private void Awake()
        {
            if (elevator == null) elevator = GetComponentInChildren<EndFloorElevatorController>(true);
            backtrackBlocker?.SetActive(false);
            doorProximityTrigger?.Configure(this, EndFloorDoorTriggerKind.DoorProximity, false);
            endingPortalTrigger?.Configure(this, EndFloorDoorTriggerKind.EndingPortal, false);
        }

        public void EnterEndingFloor()
        {
            if (State != EndFloorSequenceState.Dormant)
            {
                return;
            }

            State = EndFloorSequenceState.Entered;
            elevator?.MakeAvailable(this);
            backtrackBlocker?.SetActive(true);
            doorProximityTrigger?.SetArmed(GameFlowController.Instance?.Campaign?.CanPresentEndings != false);
            Debug.Log("ENDING_BACKTRACK_BLOCKED", this);
        }

        public bool HandleTrigger(EndFloorDoorTriggerKind kind, FirstPersonController player)
        {
            if (player == null || elevatorSelected)
            {
                return false;
            }

            if (kind == EndFloorDoorTriggerKind.DoorProximity && State == EndFloorSequenceState.Entered)
            {
                StartCoroutine(OpenDoor());
                return true;
            }
            else if (kind == EndFloorDoorTriggerKind.EndingPortal &&
                     State == EndFloorSequenceState.Transitioning)
            {
                DirectionalAnomalyGameManager gameManager =
                    FindFirstObjectByType<DirectionalAnomalyGameManager>();
                if (gameManager != null && gameManager.CompleteRunFromEndingDoor())
                {
                    State = EndFloorSequenceState.Consumed;
                    elevator?.DisableRoute();
                    endingPortalTrigger?.SetArmed(false);
                    return true;
                }
            }
            return false;
        }

        private IEnumerator OpenDoor()
        {
            State = EndFloorSequenceState.DoorOpening;
            doorProximityTrigger?.SetArmed(false);
            if (doorAnimator != null && !string.IsNullOrEmpty(doorOpenTrigger))
            {
                doorAnimator.SetTrigger(doorOpenTrigger);
            }

            yield return new WaitForSecondsRealtime(doorOpenDuration);
            State = EndFloorSequenceState.Transitioning;
            endingPortalTrigger?.SetArmed(true);
            Debug.Log("ENDING_DOOR_OPENED", this);
        }
    }
}
