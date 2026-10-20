using UnityEngine;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class ArchiveBoxShakeAnomaly : AnomalyBehaviour
    {
        [SerializeField] private Transform box;
        [SerializeField] private Transform lid;
        [SerializeField, Min(0f)] private float shakeDistance = 0.012f;
        [SerializeField, Min(0f)] private float lidDegrees = 3f;
        private Vector3 position;
        private Quaternion rotation, lidRotation;
        private float elapsed;
        private bool captured;
        public bool IsConfigured => box != null && lid != null;

        public void Configure(Transform body, Transform hingedLid)
        {
            box = body;
            lid = hingedLid;
        }

        public override void CaptureNormalState()
        {
            if (captured || !IsConfigured) return;
            position = box.localPosition;
            rotation = box.localRotation;
            lidRotation = lid.localRotation;
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
            float struggle = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(elapsed * 2.7f)), 2f);
            float jerk = Mathf.Sin(elapsed * 37f) * struggle;
            box.localPosition = position + new Vector3(jerk, Mathf.Abs(jerk) * 0.3f, -jerk * 0.6f) * shakeDistance;
            box.localRotation = rotation * Quaternion.Euler(jerk * 1.2f, 0f, jerk * 1.8f);
            lid.localRotation = lidRotation * Quaternion.Euler(Mathf.Abs(jerk) * lidDegrees, 0f, 0f);
        }

        protected override void OnResetAnomaly()
        {
            elapsed = 0f;
            if (!captured) return;
            if (box != null) box.SetLocalPositionAndRotation(position, rotation);
            if (lid != null) lid.localRotation = lidRotation;
        }

        private void OnDisable() => ResetAnomaly();
    }
}
