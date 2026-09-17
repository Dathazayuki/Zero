using UnityEngine;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class ScreenSmartPhoneAnomaly : AnomalyBehaviour
    {
        [Header("Smartphone Screen GameObjects")]
        [Tooltip("GameObject màn hình điện thoại ở trạng thái bình thường.")]
        [SerializeField] private GameObject normalObject;

        [Tooltip("GameObject màn hình điện thoại khi xảy ra Anomaly.")]
        [SerializeField] private GameObject anomalyObject;

        private bool normalWasActive;
        private bool anomalyWasActive;
        private bool normalStateCaptured;

        public GameObject NormalObject => normalObject;
        public GameObject AnomalyObject => anomalyObject;

        public void Configure(GameObject normal, GameObject anomaly)
        {
            normalObject = normal;
            anomalyObject = anomaly;
            CaptureNormalState();
        }

        public override void CaptureNormalState()
        {
            normalWasActive = normalObject != null && normalObject.activeSelf;
            anomalyWasActive = anomalyObject != null && anomalyObject.activeSelf;
            normalStateCaptured = true;

            // Đảm bảo trạng thái bình thường chuẩn: Normal bật, Anomaly tắt
            if (normalObject != null && !normalWasActive)
            {
                normalObject.SetActive(true);
                normalWasActive = true;
            }

            if (anomalyObject != null && anomalyWasActive)
            {
                anomalyObject.SetActive(false);
                anomalyWasActive = false;
            }
        }

        protected override void OnActivate()
        {
            if (normalObject == null || anomalyObject == null)
            {
                Debug.LogWarning($"SCREEN_SMARTPHONE_INVALID: Chưa gán đủ Normal hoặc Anomaly object trên {name}", this);
                return;
            }

            normalObject.SetActive(false);
            anomalyObject.SetActive(true);

            Debug.Log($"SCREEN_SMARTPHONE_ACTIVATED: anomaly={name}, normal={normalObject.name}, anomalyObj={anomalyObject.name}", this);
        }

        protected override void OnResetAnomaly()
        {
            if (normalObject != null)
            {
                normalObject.SetActive(normalStateCaptured ? normalWasActive : true);
            }

            if (anomalyObject != null)
            {
                anomalyObject.SetActive(normalStateCaptured ? anomalyWasActive : false);
            }
        }
    }
}
