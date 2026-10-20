using UnityEngine;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class DeskLampFlickerAnomaly : AnomalyBehaviour
    {
        [SerializeField] private Light lamp;
        [SerializeField, Min(1)] private int seed = 1403;
        [SerializeField] private Vector2 litSeconds = new Vector2(0.08f, 0.4f);
        [SerializeField] private Vector2 darkSeconds = new Vector2(0.05f, 0.18f);
        private System.Random random;
        private float remaining;
        private float normalIntensity;
        private bool captured;
        private bool normalEnabled;
        private bool lit;
        public bool IsConfigured => lamp != null;

        public void Configure(Light target) => lamp = target;

        public override void CaptureNormalState()
        {
            if (captured || lamp == null) return;
            normalIntensity = lamp.intensity;
            normalEnabled = lamp.enabled;
            captured = true;
        }

        protected override void OnActivate()
        {
            CaptureNormalState();
            if (!captured || lamp == null) return;
            random = new System.Random(seed);
            lit = true;
            remaining = 0f;
        }

        private void Update()
        {
            if (!IsActive || !captured || lamp == null || random == null) return;
            remaining -= Time.deltaTime;
            if (remaining > 0f) return;
            lit = !lit;
            Vector2 range = lit ? litSeconds : darkSeconds;
            remaining = Mathf.Max(0.02f, Mathf.Lerp(range.x, range.y, (float)random.NextDouble()));
            lamp.enabled = lit;
            lamp.intensity = normalIntensity;
        }

        protected override void OnResetAnomaly()
        {
            random = null;
            remaining = 0f;
            if (!captured || lamp == null) return;
            lamp.enabled = normalEnabled;
            lamp.intensity = normalIntensity;
        }

        private void OnDisable() => ResetAnomaly();
    }
}
