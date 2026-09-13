using Zero.AnomalyPrototype.Anomalies;
using Zero.AnomalyPrototype.Player;
using UnityEngine;

namespace Zero.EnvironmentLoop
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class ObserverStatueChaseTrigger : MonoBehaviour
    {
        [SerializeField] private ObserverStatueAnomaly observerStatue;
        private bool consumed;

        public bool IsArmed
        {
            get
            {
                BoxCollider triggerCollider = GetComponent<BoxCollider>();
                return !consumed && enabled && triggerCollider != null && triggerCollider.enabled;
            }
        }

        public void Configure(ObserverStatueAnomaly observer)
        {
            observerStatue = observer;
            BoxCollider triggerCollider = GetComponent<BoxCollider>();
            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
            }
        }

        public void SetArmed(bool armed)
        {
            consumed = false;
            BoxCollider triggerCollider = GetComponent<BoxCollider>();
            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
                triggerCollider.enabled = armed;
            }

            enabled = armed;
            if (armed)
            {
                Debug.Log($"OBSERVER_CHASE_TRIGGER_ARMED: trigger={name}", this);
            }
        }

        public bool FireForValidation(FirstPersonController player)
        {
            return TryFire(player);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsArmed)
            {
                return;
            }

            FirstPersonController player = other.GetComponentInParent<FirstPersonController>();
            if (player != null)
            {
                TryFire(player);
            }
        }

        private bool TryFire(FirstPersonController player)
        {
            if (!IsArmed || observerStatue == null || !observerStatue.TryStartChase(player))
            {
                return false;
            }

            consumed = true;
            BoxCollider triggerCollider = GetComponent<BoxCollider>();
            if (triggerCollider != null)
            {
                triggerCollider.enabled = false;
            }

            enabled = false;
            return true;
        }
    }
}
