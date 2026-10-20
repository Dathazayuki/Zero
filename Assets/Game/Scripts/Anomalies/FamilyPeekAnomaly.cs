using UnityEngine;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class FamilyPeekAnomaly : AnomalyBehaviour
    {
        private enum PeekState { Waiting, Retreating, Hidden }
        [SerializeField] private Transform actor;
        [SerializeField] private Transform observer;
        [SerializeField] private SkinnedMeshRenderer bulgingCurtain;
        [SerializeField] private Renderer normalCurtain;
        [SerializeField] private AudioSource retreatAudio;
        [SerializeField] private Vector3 retreatOffset = new Vector3(0f, 0f, 0.5f);
        [SerializeField, Min(0.1f)] private float approachDistance = 1.4f;
        [SerializeField, Min(0.1f)] private float retreatSeconds = 1.2f;
        [SerializeField] private bool reactToLookingDown;
        [SerializeField] private bool faceObserver;
        [SerializeField, Range(0f, 60f)] private float trackingYaw = 25f;
        [SerializeField] private GameObject[] shownObjects = System.Array.Empty<GameObject>();
        [SerializeField] private GameObject[] hiddenObjects = System.Array.Empty<GameObject>();
        private PeekState state;
        private Vector3 originalPosition;
        private Quaternion originalRotation;
        private bool[] shownState, hiddenState;
        private bool curtainEnabled;
        [System.NonSerialized] private bool captured;
        private float elapsed;
        public Transform Actor => actor;
        public bool HasRetreated => state == PeekState.Hidden;
        public bool IsConfigured => actor != null && observer != null;

        public void Configure(Transform model, Transform camera, Vector3 offset)
        {
            actor = model;
            observer = camera;
            retreatOffset = offset;
        }

        public void ConfigureVisibility(GameObject[] show, GameObject[] hide)
        {
            shownObjects = show;
            hiddenObjects = hide;
        }

        public override void CaptureNormalState()
        {
            if (captured || !IsConfigured) return;
            originalPosition = actor.localPosition;
            originalRotation = actor.localRotation;
            shownState = CaptureActive(shownObjects);
            hiddenState = CaptureActive(hiddenObjects);
            curtainEnabled = normalCurtain != null && normalCurtain.enabled;
            captured = true;
        }

        protected override void OnActivate()
        {
            CaptureNormalState();
            if (!captured || !IsConfigured) return;
            state = PeekState.Waiting;
            actor.localPosition = originalPosition;
            actor.gameObject.SetActive(true);
            SetActive(shownObjects, true);
            SetActive(hiddenObjects, false);
            if (bulgingCurtain == null || normalCurtain == null) return;
            normalCurtain.enabled = false;
            bulgingCurtain.gameObject.SetActive(true);
        }

        private void Update()
        {
            if (!IsActive || !captured || !IsConfigured || state == PeekState.Hidden) return;
            if (state == PeekState.Waiting)
            {
                if (faceObserver) TrackObserver();
                if (!ShouldRetreat()) return;
                state = PeekState.Retreating;
                elapsed = 0f;
                if (retreatAudio != null) retreatAudio.Play();
            }
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / Mathf.Max(0.1f, retreatSeconds));
            actor.localPosition = originalPosition + retreatOffset * Mathf.SmoothStep(0f, 1f, progress);
            if (bulgingCurtain != null) bulgingCurtain.SetBlendShapeWeight(0, Mathf.Sin(progress * Mathf.PI) * 100f);
            if (progress < 1f) return;
            actor.gameObject.SetActive(false);
            state = PeekState.Hidden;
        }

        private bool ShouldRetreat()
        {
            Vector3 offset = observer.position - actor.position;
            offset.y = 0f;
            bool close = offset.sqrMagnitude <= approachDistance * approachDistance;
            bool looking = reactToLookingDown && offset.sqrMagnitude < 5.76f && observer.forward.y < -0.25f;
            return close || looking;
        }

        private void TrackObserver()
        {
            Quaternion frame = actor.parent != null ? actor.parent.rotation * originalRotation : originalRotation;
            Vector3 direction = Quaternion.Inverse(frame) * (observer.position - actor.position);
            float yaw = Mathf.Clamp(Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg, -trackingYaw, trackingYaw);
            actor.localRotation = Quaternion.RotateTowards(actor.localRotation,
                originalRotation * Quaternion.Euler(0f, yaw, 0f), Time.deltaTime * 45f);
        }

        private static bool[] CaptureActive(GameObject[] objects)
        {
            var states = new bool[objects.Length];
            for (int i = 0; i < objects.Length; i++) states[i] = objects[i] != null && objects[i].activeSelf;
            return states;
        }

        private static void SetActive(GameObject[] objects, bool active)
        {
            foreach (GameObject item in objects) if (item != null) item.SetActive(active);
        }

        private static void RestoreActive(GameObject[] objects, bool[] states)
        {
            for (int i = 0; i < objects.Length; i++) if (objects[i] != null) objects[i].SetActive(states[i]);
        }

        protected override void OnResetAnomaly()
        {
            elapsed = 0f;
            if (retreatAudio != null) retreatAudio.Stop();
            state = PeekState.Waiting;
            if (!captured) return;
            RestoreActive(shownObjects, shownState);
            RestoreActive(hiddenObjects, hiddenState);
            if (actor != null)
            {
                actor.SetLocalPositionAndRotation(originalPosition, originalRotation);
                actor.gameObject.SetActive(false);
            }
            if (normalCurtain != null) normalCurtain.enabled = curtainEnabled;
            if (bulgingCurtain == null) return;
            bulgingCurtain.SetBlendShapeWeight(0, 0f);
            bulgingCurtain.gameObject.SetActive(false);
        }

        private void OnDisable() => ResetAnomaly();
    }
}
