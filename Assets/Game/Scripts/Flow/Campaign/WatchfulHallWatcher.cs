using UnityEngine;

namespace Zero.AnomalyPrototype.Flow
{
    public sealed class WatchfulHallWatcher : MonoBehaviour
    {
        [SerializeField] private Transform eye;
        [SerializeField] private Light gazeLight;
        [SerializeField, Min(1f)] private float viewDistance = 65f;
        [SerializeField, Range(10f, 180f)] private float viewAngle = 100f;
        [SerializeField, Min(0.1f)] private float detectionSeconds = 1.5f;
        [SerializeField, Min(1f)] private float scanDegreesPerSecond = 15f;
        private WatchfulHallThreatDirector director;
        private Quaternion initialRotation;
        private Color initialLightColor;
        private float observedSeconds;
        private float nextReport;
        public bool IsAlerted { get; private set; }
        public bool IsConfigured => eye != null && gazeLight != null;

        private void Awake()
        {
            initialRotation = transform.localRotation;
            if (gazeLight != null) initialLightColor = gazeLight.color;
        }

        public void Bind(WatchfulHallThreatDirector owner) => director = owner;

        private void Update()
        {
            if (director == null || !director.IsPlaying || !IsConfigured || Time.deltaTime <= 0f) return;
            transform.Rotate(0f, scanDegreesPerSecond * Time.deltaTime, 0f, Space.Self);
            bool visible = director.CanSee(eye.position, eye.forward, viewDistance, viewAngle);
            if (!visible) { ClearNotice(); return; }
            observedSeconds += Time.deltaTime;
            gazeLight.color = Color.Lerp(initialLightColor, Color.red, Mathf.Clamp01(observedSeconds / detectionSeconds));
            if (observedSeconds < detectionSeconds || Time.time < nextReport) return;
            director.ReportWatcher(director.Player.transform.position, !IsAlerted);
            IsAlerted = true;
            nextReport = Time.time + 0.35f;
        }

        public void ClearNotice()
        {
            observedSeconds = 0f;
            nextReport = 0f;
            IsAlerted = false;
            if (gazeLight != null) gazeLight.color = initialLightColor;
        }

        public void ResetWatcher()
        {
            ClearNotice();
            transform.localRotation = initialRotation;
        }

        private void OnDisable() => ResetWatcher();
    }
}
