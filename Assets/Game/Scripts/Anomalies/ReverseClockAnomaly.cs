using UnityEngine;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class ReverseClockAnomaly : AnomalyBehaviour
    {
        [SerializeField] private GameObject normalHands;
        [SerializeField] private Transform hourPivot;
        [SerializeField] private Transform minutePivot;
        [SerializeField, Min(0.05f)] private float stepSeconds = 0.3f;
        private Quaternion hourRotation;
        private Quaternion minuteRotation;
        private bool normalActive;
        private bool hourActive;
        private bool minuteActive;
        private bool captured;
        private float elapsed;
        public bool IsConfigured => normalHands != null && hourPivot != null && minutePivot != null;

        public void Configure(GameObject normal, Transform hour, Transform minute)
        {
            normalHands = normal;
            hourPivot = hour;
            minutePivot = minute;
            CaptureNormalState();
        }

        public override void CaptureNormalState()
        {
            if (captured || !IsConfigured) return;
            normalActive = normalHands.activeSelf;
            hourActive = hourPivot.gameObject.activeSelf;
            minuteActive = minutePivot.gameObject.activeSelf;
            hourRotation = hourPivot.localRotation;
            minuteRotation = minutePivot.localRotation;
            captured = true;
        }

        protected override void OnActivate()
        {
            CaptureNormalState();
            if (!captured || !IsConfigured) return;
            elapsed = 0f;
            normalHands.SetActive(false);
            hourPivot.gameObject.SetActive(true);
            minutePivot.gameObject.SetActive(true);
        }

        private void Update()
        {
            if (!IsActive || !captured || !IsConfigured) return;
            elapsed += Time.deltaTime;
            float steps = Mathf.Floor(elapsed / Mathf.Max(0.05f, stepSeconds));
            hourPivot.localRotation = hourRotation * Quaternion.Euler(0f, 0f, steps * 3f);
            minutePivot.localRotation = minuteRotation * Quaternion.Euler(0f, 0f, steps * 12f);
        }

        protected override void OnResetAnomaly()
        {
            if (!captured) return;
            elapsed = 0f;
            if (normalHands != null) normalHands.SetActive(normalActive);
            if (hourPivot != null)
            {
                hourPivot.localRotation = hourRotation;
                hourPivot.gameObject.SetActive(hourActive);
            }
            if (minutePivot != null)
            {
                minutePivot.localRotation = minuteRotation;
                minutePivot.gameObject.SetActive(minuteActive);
            }
        }

        private void OnDisable() => ResetAnomaly();
    }
}
