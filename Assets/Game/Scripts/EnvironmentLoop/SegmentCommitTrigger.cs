using Zero.AnomalyPrototype.Player;
using UnityEngine;

namespace Zero.EnvironmentLoop
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class SegmentCommitTrigger : MonoBehaviour
    {
        [SerializeField] private StairLoopManager loopManager;
        [SerializeField] private FloorSegment ownerSegment;
        private bool consumed;

        public FloorSegment OwnerSegment => ownerSegment;
        public bool IsArmed
        {
            get
            {
                BoxCollider triggerCollider = GetComponent<BoxCollider>();
                return !consumed && enabled && triggerCollider != null && triggerCollider.enabled;
            }
        }
        public bool IsConfigured => loopManager != null && ownerSegment != null &&
                                    GetComponent<BoxCollider>()?.isTrigger == true;

        public void Configure(StairLoopManager manager, FloorSegment owner)
        {
            loopManager = manager;
            ownerSegment = owner;
            BoxCollider triggerCollider = GetComponent<BoxCollider>();
            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
            }
        }

        public void ResetTrigger()
        {
            bool wasArmed = IsArmed;
            consumed = false;
            BoxCollider triggerCollider = GetComponent<BoxCollider>();
            if (triggerCollider != null)
            {
                triggerCollider.enabled = true;
                triggerCollider.isTrigger = true;
            }

            enabled = true;
            if (!wasArmed)
            {
                Debug.Log($"FLOOR_TRIGGER_ARMED: trigger={name}, segment={ownerSegment?.SegmentId ?? "Unbound"}", this);
            }
        }

        public void ConsumeWithoutCommit()
        {
            bool wasConsumed = consumed;
            consumed = true;
            DisableTrigger();
            if (!wasConsumed)
            {
                Debug.Log(
                    $"FLOOR_TRIGGER_CONSUMED: trigger={name}, segment={ownerSegment?.SegmentId ?? "Unbound"}, initial=true",
                    this);
            }
        }

        public bool FireForValidation()
        {
            return TryConsumeAndCommit();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsArmed)
            {
                return;
            }

            FirstPersonController player = other.GetComponentInParent<FirstPersonController>();
            if (player == null)
            {
                return;
            }

            TryConsumeAndCommit();
        }

        private bool TryConsumeAndCommit()
        {
            if (!IsArmed)
            {
                return false;
            }

            consumed = true;
            bool committed = loopManager != null && ownerSegment != null && loopManager.TryCommit(ownerSegment);
            if (committed)
            {
                DisableTrigger();
                Debug.Log(
                    $"FLOOR_TRIGGER_CONSUMED: trigger={name}, segment={ownerSegment.SegmentId}, initial=false",
                    this);
                return true;
            }

            consumed = false;
            BoxCollider triggerCollider = GetComponent<BoxCollider>();
            if (triggerCollider != null)
            {
                triggerCollider.enabled = true;
            }

            enabled = true;
            string reason = loopManager != null ? loopManager.LastOperationMessage : "Trigger is not bound";
            Debug.LogWarning(
                $"FLOOR_TRIGGER_REJECTED: trigger={name}, segment={ownerSegment?.SegmentId ?? "Unbound"}, reason={reason}",
                this);
            return false;
        }

        private void DisableTrigger()
        {
            BoxCollider triggerCollider = GetComponent<BoxCollider>();
            if (triggerCollider != null)
            {
                triggerCollider.enabled = false;
            }

            enabled = false;
        }

        private void OnDrawGizmosSelected()
        {
            DrawGizmo(IsArmed ? Color.green : Color.gray);
        }

        private void DrawGizmo(Color color)
        {
            BoxCollider triggerCollider = GetComponent<BoxCollider>();
            if (triggerCollider == null)
            {
                return;
            }

            Gizmos.color = color;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(triggerCollider.center, triggerCollider.size);
        }
    }
}
