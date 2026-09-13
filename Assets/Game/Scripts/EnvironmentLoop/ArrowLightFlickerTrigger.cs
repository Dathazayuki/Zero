using Zero.AnomalyPrototype.Anomalies;
using Zero.AnomalyPrototype.Player;
using UnityEngine;

namespace Zero.EnvironmentLoop
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class ArrowLightFlickerTrigger : MonoBehaviour
    {
        [SerializeField] private ArrowLightFlickerAnomaly anomaly;
        private BoxCollider triggerCollider;
        private bool consumed;

        public bool IsArmed => !consumed && enabled && GetTriggerCollider().enabled;

        public void Configure(ArrowLightFlickerAnomaly owner)
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

        public bool FireForValidation()
        {
            return TryFire();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsArmed || other.GetComponentInParent<FirstPersonController>() == null)
            {
                return;
            }

            TryFire();
        }

        private bool TryFire()
        {
            if (!IsArmed)
            {
                return false;
            }

            consumed = true;
            if (anomaly == null || !anomaly.TryBeginFlicker())
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
