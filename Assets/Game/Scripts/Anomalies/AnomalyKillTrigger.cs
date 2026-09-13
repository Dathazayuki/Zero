using Zero.AnomalyPrototype.Player;
using UnityEngine;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class AnomalyKillTrigger : MonoBehaviour
    {
        [SerializeField] private AnomalyBehaviour source;
        [SerializeField] private AnomalyDeathPresentation presentation;
        [SerializeField] private Collider triggerCollider;
        [SerializeField] private PlayerDeathController deathController;

        private bool consumed;

        public bool IsArmed => !consumed && enabled && triggerCollider != null && triggerCollider.enabled;

        private void Awake()
        {
            ResolveReferences();
            SetArmed(false);
        }

        public void Configure(AnomalyBehaviour anomaly, AnomalyDeathPresentation deathPresentation = null)
        {
            source = anomaly;
            presentation = deathPresentation != null
                ? deathPresentation
                : anomaly?.GetComponentInChildren<AnomalyDeathPresentation>(true);
            ResolveReferences();
            SetArmed(false);
        }

        public void SetArmed(bool armed)
        {
            ResolveReferences();
            consumed = false;
            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
                triggerCollider.enabled = armed;
            }

            enabled = armed;
            if (armed)
            {
                Debug.Log($"ANOMALY_KILL_TRIGGER_ARMED: trigger={name}, anomaly={source?.name ?? "Unbound"}", this);
            }
        }

        public bool FireForValidation(FirstPersonController player)
        {
            return TryKill(player);
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
                TryKill(player);
            }
        }

        private bool TryKill(FirstPersonController player)
        {
            if (!IsArmed || player == null)
            {
                return false;
            }

            consumed = true;
            DisableTrigger();
            ResolveReferences();

            if (deathController == null)
            {
                Debug.LogWarning(
                    $"DEATH_CONFIGURATION_INVALID: trigger={name} cannot find PlayerDeathController.",
                    this);
            }

            bool accepted = deathController != null && deathController.TryBeginDeath(source, presentation);
            if (!accepted)
            {
                consumed = false;
                if (triggerCollider != null)
                {
                    triggerCollider.enabled = true;
                }

                enabled = true;
                return false;
            }

            Debug.Log(
                $"ANOMALY_KILL_TRIGGER_CONSUMED: trigger={name}, anomaly={source?.name ?? "Unbound"}",
                this);
            return true;
        }

        private void ResolveReferences()
        {
            triggerCollider ??= GetComponent<Collider>();
            source ??= GetComponentInParent<AnomalyBehaviour>();
            if (presentation == null && source != null)
            {
                presentation = source.GetComponentInChildren<AnomalyDeathPresentation>(true);
            }

            deathController ??= FindAnyObjectByType<PlayerDeathController>();
            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
            }
        }

        private void DisableTrigger()
        {
            if (triggerCollider != null)
            {
                triggerCollider.enabled = false;
            }

            enabled = false;
        }
    }
}
