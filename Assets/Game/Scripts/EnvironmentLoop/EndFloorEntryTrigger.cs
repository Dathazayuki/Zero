using Zero.AnomalyPrototype.Player;
using UnityEngine;

namespace Zero.EnvironmentLoop
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class EndFloorEntryTrigger : MonoBehaviour
    {
        private StairLoopManager loopManager;
        private FloorSegment ownerSegment;
        private TravelDirection direction;
        private bool consumed;

        public bool IsArmed => !consumed && enabled && GetComponent<BoxCollider>().enabled;

        public void Configure(StairLoopManager manager, FloorSegment owner, TravelDirection entryDirection)
        {
            loopManager = manager;
            ownerSegment = owner;
            direction = entryDirection;
            consumed = false;
            BoxCollider triggerCollider = GetComponent<BoxCollider>();
            triggerCollider.isTrigger = true;
            triggerCollider.enabled = true;
            enabled = true;
        }

        public void Disarm()
        {
            consumed = true;
            BoxCollider triggerCollider = GetComponent<BoxCollider>();
            if (triggerCollider != null)
            {
                triggerCollider.enabled = false;
            }

            enabled = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsArmed || other.GetComponentInParent<FirstPersonController>() == null)
            {
                return;
            }

            consumed = true;
            if (loopManager != null && loopManager.TryEnterEndingFloor(ownerSegment, direction))
            {
                Disarm();
                return;
            }

            consumed = false;
            Debug.LogWarning(
                $"ENDING_FLOOR_ENTRY_REJECTED: reason={loopManager?.LastOperationMessage ?? "unbound"}",
                this);
        }
    }
}
