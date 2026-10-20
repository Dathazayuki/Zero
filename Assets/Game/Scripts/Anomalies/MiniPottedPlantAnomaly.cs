using UnityEngine;

namespace Zero.AnomalyPrototype.Anomalies
{
    [DisallowMultipleComponent]
    public sealed class MiniPottedPlantAnomaly : AnomalyBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField, Min(0.01f)] private float anomalyScale = 0.6f;

        [System.NonSerialized] private Vector3 normalScale = Vector3.one;
        [System.NonSerialized] private bool captured;

        public Transform Target => target;
        public float AnomalyScale => anomalyScale;

        public void Configure(Transform plantTarget, float scale = 0.6f)
        {
            ResetAnomaly();
            target = plantTarget;
            anomalyScale = Mathf.Max(0.01f, scale);
            captured = false;
            CaptureNormalState();
        }

        public override void CaptureNormalState()
        {
            if (captured || target == null) return;
            normalScale = target.localScale;
            captured = true;
        }

        protected override void OnActivate()
        {
            CaptureNormalState();
            if (target != null) target.localScale = normalScale * anomalyScale;
        }

        protected override void OnResetAnomaly()
        {
            if (target != null && captured) target.localScale = normalScale;
        }

        private void OnDisable() => ResetAnomaly();
    }
}
