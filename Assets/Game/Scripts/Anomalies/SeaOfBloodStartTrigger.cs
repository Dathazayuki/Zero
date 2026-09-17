using UnityEngine;

namespace Zero.AnomalyPrototype.Anomalies
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class SeaOfBloodStartTrigger : MonoBehaviour
    {
        private SeaOfBloodAnomaly owner;
        private BoxCollider volume;
        public bool IsArmed { get; private set; }

        public void Configure(SeaOfBloodAnomaly anomaly)
        {
            owner = anomaly;
            volume = GetComponent<BoxCollider>();
            volume.isTrigger = true;
        }

        public void SetArmed(bool armed)
        {
            if (volume == null) volume = GetComponent<BoxCollider>();
            IsArmed = armed;
            volume.enabled = armed;
            enabled = armed;
        }

        private void OnTriggerEnter(Collider other) { TryEnter(other); }
        // Also handles activation/commit while the Player is already overlapping.
        private void OnTriggerStay(Collider other) { TryEnter(other); }
        private void TryEnter(Collider other)
        {
            if (IsArmed && owner != null && owner.TryStartRising(other)) SetArmed(false);
        }
    }
}
