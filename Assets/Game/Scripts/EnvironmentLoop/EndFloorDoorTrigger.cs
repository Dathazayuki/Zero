using Zero.AnomalyPrototype.Player;
using UnityEngine;

namespace Zero.EnvironmentLoop
{
    public enum EndFloorDoorTriggerKind
    {
        DoorProximity,
        EndingPortal
    }

    [RequireComponent(typeof(BoxCollider))]
    public sealed class EndFloorDoorTrigger : MonoBehaviour
    {
        [SerializeField] private EndFloorSequenceController controller;
        [SerializeField] private EndFloorDoorTriggerKind kind;
        private bool consumed;

        public void Configure(
            EndFloorSequenceController sequenceController,
            EndFloorDoorTriggerKind triggerKind,
            bool armed)
        {
            controller = sequenceController;
            kind = triggerKind;
            BoxCollider triggerCollider = GetComponent<BoxCollider>();
            triggerCollider.isTrigger = true;
            SetArmed(armed);
        }

        public void SetArmed(bool armed)
        {
            consumed = !armed;
            BoxCollider triggerCollider = GetComponent<BoxCollider>();
            if (triggerCollider != null)
            {
                triggerCollider.enabled = armed;
                triggerCollider.isTrigger = true;
            }

            enabled = armed;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (consumed)
            {
                return;
            }

            FirstPersonController player = other.GetComponentInParent<FirstPersonController>();
            if (player == null)
            {
                return;
            }

            consumed = true;
            bool accepted = controller != null && controller.HandleTrigger(kind, player);
            SetArmed(!accepted);
            if (!accepted)
            {
                Debug.LogWarning("ENDING_PORTAL_REJECTED: trigger rearmed", this);
            }
        }
    }
}
