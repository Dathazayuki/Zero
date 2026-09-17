using UnityEngine;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class ScreamTraceAnomaly : AnomalyBehaviour
    {
        [Header("Target GameObject")]
        [Tooltip("GameObject dấu vết sẽ được bật (true) khi xảy ra Anomaly và tắt (false) khi bình thường.")]
        [SerializeField] private GameObject traceObject;

        private bool normalWasActive;
        private bool normalStateCaptured;

        public GameObject TraceObject => traceObject;

        public void Configure(GameObject targetObject)
        {
            traceObject = targetObject;
            CaptureNormalState();
        }

        public override void CaptureNormalState()
        {
            if (traceObject != null)
            {
                normalWasActive = traceObject.activeSelf;
                traceObject.SetActive(false);
            }

            normalStateCaptured = true;
        }

        protected override void OnActivate()
        {
            if (traceObject == null)
            {
                Debug.LogWarning($"SCREAM_TRACE_INVALID: Chưa gán Trace Object trên {name}", this);
                return;
            }

            traceObject.SetActive(true);
            Debug.Log($"SCREAM_TRACE_ACTIVATED: anomaly={name}, target={traceObject.name}", this);
        }

        protected override void OnResetAnomaly()
        {
            if (traceObject != null)
            {
                traceObject.SetActive(normalStateCaptured ? normalWasActive : false);
            }
        }
    }
}
