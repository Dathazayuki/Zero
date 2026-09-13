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

        public EndFloorSequenceState State { get; private set; } = EndFloorSequenceState.Dormant;

        private void Awake()
        {
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
            backtrackBlocker?.SetActive(true);
            doorProximityTrigger?.SetArmed(true);
            Debug.Log("ENDING_BACKTRACK_BLOCKED", this);
        }

        public bool HandleTrigger(EndFloorDoorTriggerKind kind, FirstPersonController player)
        {
            if (player == null)
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
