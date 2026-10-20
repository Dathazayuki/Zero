using UnityEngine;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class FloatingTeaCupAnomaly : AnomalyBehaviour
    {
        [SerializeField] private Transform cup;
        [SerializeField] private Transform ripple;
        [SerializeField, Min(0.05f)] private float liftHeight = 0.3f;
        [SerializeField, Min(1f)] private float cycleSeconds = 6f;
        [SerializeField, Range(0f, 35f)] private float drinkingTilt = 18f;
        private Vector3 position;
        private Quaternion rotation;
        private Vector3 rippleScale;
        private bool captured;
        private float elapsed;
        public bool IsConfigured => cup != null;
        public Transform Cup => cup;

        public void Configure(Transform movingCup, Transform teaRipple)
        {
            cup = movingCup;
            ripple = teaRipple;
            CaptureNormalState();
        }

        public override void CaptureNormalState()
        {
            if (captured || !IsConfigured) return;
            position = cup.localPosition;
            rotation = cup.localRotation;
            if (ripple != null) rippleScale = ripple.localScale;
            captured = true;
        }

        protected override void OnActivate()
        {
            CaptureNormalState();
            elapsed = 0f;
        }

        private void Update()
        {
            if (!IsActive || !captured || !IsConfigured) return;
            elapsed += Time.deltaTime;
            float phase = Mathf.Repeat(elapsed / Mathf.Max(1f, cycleSeconds), 1f);
            float lift = Ramp(phase, 0.15f, 0.35f) * (1f - Ramp(phase, 0.65f, 0.85f));
            float tilt = Ramp(phase, 0.35f, 0.47f) * (1f - Ramp(phase, 0.58f, 0.7f));
            cup.localPosition = position + Vector3.up * (liftHeight * lift) + Vector3.right * (0.055f * tilt);
            cup.localRotation = rotation * Quaternion.Euler(0f, 0f, -drinkingTilt * tilt);
            if (ripple != null) ripple.localScale = rippleScale * (1f + Mathf.Sin(elapsed * 5f) * 0.045f * lift);
        }

        private static float Ramp(float value, float start, float end) =>
            Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(start, end, value));

        protected override void OnResetAnomaly()
        {
            elapsed = 0f;
            if (!captured) return;
            if (cup != null) cup.SetLocalPositionAndRotation(position, rotation);
            if (ripple != null) ripple.localScale = rippleScale;
        }

        private void OnDisable() => ResetAnomaly();
    }
}
