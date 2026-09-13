using Zero.AnomalyPrototype.Anomalies;
using Zero.AnomalyPrototype.Player;
using UnityEngine;

namespace Zero.EnvironmentLoop
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class CeilingCreatureEntryTrigger : MonoBehaviour
    {
        [SerializeField] private CeilingCreatureAnomaly anomaly;
        private BoxCollider triggerCollider;
        private bool consumed;

        public bool IsArmed => !consumed && enabled && GetTriggerCollider().enabled;

        public void Configure(CeilingCreatureAnomaly owner)
        {
            anomaly = owner;
            GetTriggerCollider().isTrigger = true;
        }

        public void SetArmed(bool armed)
        {
            consumed = false;
            BoxCollider box = GetTriggerCollider();
            box.isTrigger = true;
            box.enabled = armed;
            enabled = armed;
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
            if (!IsArmed)
            {
                return false;
            }

            consumed = true;
            if (anomaly == null || !anomaly.TryStartCeilingPass(player))
            {
                consumed = false;
                return false;
            }

            GetTriggerCollider().enabled = false;
            enabled = false;
            return true;
        }

        private BoxCollider GetTriggerCollider()
        {
            if (triggerCollider == null)
            {
                triggerCollider = GetComponent<BoxCollider>();
            }

            return triggerCollider;
        }
    }
}
