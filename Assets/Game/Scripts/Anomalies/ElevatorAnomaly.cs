using UnityEngine;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class ElevatorAnomaly : AnomalyBehaviour
    {
        [Header("Elevator GameObjects")]
        [Tooltip("GameObject thang máy ở trạng thái bình thường.")]
        [SerializeField] private GameObject elevatorNormal;

        [Tooltip("GameObject thang máy khi xảy ra Anomaly.")]
        [SerializeField] private GameObject elevatorAnomaly;

        private bool normalWasActive;
        private bool anomalyWasActive;
        private bool normalStateCaptured;

        public GameObject ElevatorNormal => elevatorNormal;
        public GameObject ElevatorAnomalyObject => elevatorAnomaly;

        public void Configure(GameObject normal, GameObject anomaly)
        {
            elevatorNormal = normal;
            elevatorAnomaly = anomaly;
            CaptureNormalState();
        }

        public override void CaptureNormalState()
        {
            normalWasActive = elevatorNormal != null && elevatorNormal.activeSelf;
            anomalyWasActive = elevatorAnomaly != null && elevatorAnomaly.activeSelf;
            normalStateCaptured = true;

            // Đảm bảo trạng thái bình thường: ElevatorNormal bật, ElevatorAnomaly tắt
            if (elevatorNormal != null && !normalWasActive)
            {
                elevatorNormal.SetActive(true);
                normalWasActive = true;
            }

            if (elevatorAnomaly != null && anomalyWasActive)
            {
                elevatorAnomaly.SetActive(false);
                anomalyWasActive = false;
            }
        }

        protected override void OnActivate()
        {
            if (elevatorNormal == null || elevatorAnomaly == null)
            {
                Debug.LogWarning($"ELEVATOR_ANOMALY_INVALID: Chưa gán đủ ElevatorNormal hoặc ElevatorAnomaly trên {name}", this);
                return;
            }

            elevatorNormal.SetActive(false);
            elevatorAnomaly.SetActive(true);

            Debug.Log($"ELEVATOR_ANOMALY_ACTIVATED: anomaly={name}, normal={elevatorNormal.name}, anomalyObj={elevatorAnomaly.name}", this);
        }

        protected override void OnResetAnomaly()
        {
            if (elevatorNormal != null)
            {
                elevatorNormal.SetActive(normalStateCaptured ? normalWasActive : true);
            }

            if (elevatorAnomaly != null)
            {
                elevatorAnomaly.SetActive(normalStateCaptured ? anomalyWasActive : false);
            }
        }
    }
}
