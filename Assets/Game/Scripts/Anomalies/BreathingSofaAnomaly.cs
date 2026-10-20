using UnityEngine;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class BreathingSofaAnomaly : AnomalyBehaviour
    {
        [SerializeField] private Renderer normalSeat;
        [SerializeField] private SkinnedMeshRenderer occupiedSeat;
        [SerializeField] private Transform pillows;
        [SerializeField] private float breathSeconds = 3.8f;
        private Vector3 pillowPosition;
        private bool normalEnabled;
        [System.NonSerialized] private bool captured;
        private float elapsed;
        public SkinnedMeshRenderer OccupiedSeat => occupiedSeat;
        public bool IsConfigured => normalSeat != null && occupiedSeat != null && pillows != null;

        public void Configure(Renderer normal, SkinnedMeshRenderer occupied, Transform movedPillows)
        {
            normalSeat = normal;
            occupiedSeat = occupied;
            pillows = movedPillows;
        }

        public override void CaptureNormalState()
        {
            if (captured || !IsConfigured) return;
            pillowPosition = pillows.localPosition;
            normalEnabled = normalSeat.enabled;
            captured = true;
        }

        protected override void OnActivate()
        {
            CaptureNormalState();
            if (!captured || !IsConfigured) return;
            elapsed = 0f;
            normalSeat.enabled = false;
            occupiedSeat.gameObject.SetActive(true);
            pillows.localPosition = pillowPosition + Vector3.right * 0.14f;
            occupiedSeat.SetBlendShapeWeight(0, 85f);
        }

        private void Update()
        {
            if (!IsActive || !IsConfigured || !captured) return;
            elapsed += Time.deltaTime;
            occupiedSeat.SetBlendShapeWeight(0, 85f + 12f * Mathf.Sin(elapsed * Mathf.PI * 2f / Mathf.Max(1f, breathSeconds)));
        }

        protected override void OnResetAnomaly()
        {
            elapsed = 0f;
            if (!captured) return;
            if (normalSeat != null) normalSeat.enabled = normalEnabled;
            if (pillows != null) pillows.localPosition = pillowPosition;
            if (occupiedSeat == null) return;
            occupiedSeat.SetBlendShapeWeight(0, 0f);
            occupiedSeat.gameObject.SetActive(false);
        }

        private void OnDisable() => ResetAnomaly();
    }
}
