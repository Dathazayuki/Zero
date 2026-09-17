using UnityEngine;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class MissingClockAnomaly : AnomalyBehaviour
    {
        [Header("Target Clock")]
        [Tooltip("GameObject của đồng hồ sẽ bị ẩn/biến mất khi xảy ra Anomaly.")]
        [SerializeField] private GameObject clockObject;

        [Header("Optional Wall Details")]
        [Tooltip("Tùy chọn: GameObject hiển thị vết ố tường / đinh treo trống khi đồng hồ mất (nếu có).")]
        [SerializeField] private GameObject emptyWallMark;

        private bool normalClockWasActive;
        private bool normalMarkWasActive;
        private bool normalStateCaptured;

        public GameObject ClockObject => clockObject;
        public GameObject EmptyWallMark => emptyWallMark;

        public void Configure(GameObject clock, GameObject emptyMark = null)
        {
            clockObject = clock;
            emptyWallMark = emptyMark;
            CaptureNormalState();
        }

        public override void CaptureNormalState()
        {
            if (clockObject != null)
            {
                normalClockWasActive = clockObject.activeSelf;
            }

            if (emptyWallMark != null)
            {
                normalMarkWasActive = emptyWallMark.activeSelf;
            }

            normalStateCaptured = true;
        }

        protected override void OnActivate()
        {
            if (clockObject == null)
            {
                Debug.LogWarning($"MISSING_CLOCK_INVALID: Chưa gán Clock Object trên {name}", this);
                return;
            }

            clockObject.SetActive(false);

            if (emptyWallMark != null)
            {
                emptyWallMark.SetActive(true);
            }
        }

        protected override void OnResetAnomaly()
        {
            if (clockObject != null)
            {
                clockObject.SetActive(normalStateCaptured ? normalClockWasActive : true);
            }

            if (emptyWallMark != null)
            {
                emptyWallMark.SetActive(normalStateCaptured ? normalMarkWasActive : false);
            }
        }
    }
}
