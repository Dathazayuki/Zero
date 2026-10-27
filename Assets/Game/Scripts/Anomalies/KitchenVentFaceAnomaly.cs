using UnityEngine;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class KitchenVentFaceAnomaly : AnomalyBehaviour
    {
        [SerializeField] private Camera observer;
        [SerializeField] private Transform face;
        [SerializeField] private Transform gazePoint;
        [SerializeField] private GameObject grille;
        [SerializeField, Tooltip("Length controls retreat distance. Movement is always vertically upward, including old horizontal offsets.")]
        private Vector3 retreatOffset = new Vector3(0f, 0.8f, 0f);
        [SerializeField, Min(0.01f)] private float retreatSeconds = 0.4f;
        [SerializeField, Range(1f, 60f)] private float gazeAngle = 18f;
        [SerializeField, Min(0f)] private float gazeHoldSeconds = 0.12f;
        [SerializeField] private LayerMask obstructionMask = ~0;
        private Vector3 initialPosition;
        private bool captured;
        private float watched, elapsed;
        public bool IsRetreating { get; private set; }
        public override void CaptureNormalState()
        {
            if (captured || face == null) return;
            initialPosition = face.localPosition; captured = true;
        }
        protected override void OnActivate()
        {
            CaptureNormalState();
            if (!captured) return;
            face.localPosition = initialPosition; face.gameObject.SetActive(true);
            if (grille != null) grille.SetActive(true);
        }
        private void Update() => Advance(Time.deltaTime);
        public void Advance(float delta)
        {
            if (!IsActive || !captured || observer == null || gazePoint == null || !face.gameObject.activeSelf) return;
            if (!IsRetreating)
            {
                bool visible = Vector3.Angle(observer.transform.forward, gazePoint.position - observer.transform.position) <= gazeAngle &&
                    AnomalyGazeVisibility.IsVisible(observer, gazePoint, null, obstructionMask, 0.06f);
                watched = visible ? watched + Mathf.Max(0f, delta) : 0f;
                if (!visible || watched < gazeHoldSeconds) return;
                IsRetreating = true;
            }
            elapsed += Mathf.Max(0f, delta);
            float progress = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, retreatSeconds));
            Vector3 start = face.parent != null ? face.parent.TransformPoint(initialPosition) : initialPosition;
            face.position = start + Vector3.up * retreatOffset.magnitude * Mathf.SmoothStep(0f, 1f, progress);
            if (progress >= 1f) face.gameObject.SetActive(false);
        }
        protected override void OnResetAnomaly()
        {
            watched = 0f; elapsed = 0f; IsRetreating = false;
            if (grille != null) grille.SetActive(false);
            if (face == null) return;
            if (captured) face.localPosition = initialPosition;
            face.gameObject.SetActive(false);
        }
        private void OnDisable() => ResetAnomaly();
    }
}
