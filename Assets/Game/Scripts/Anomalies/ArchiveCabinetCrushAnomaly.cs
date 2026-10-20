using UnityEngine;
using Zero.AnomalyPrototype.Player;
using CrushPhase = Zero.AnomalyPrototype.Anomalies.ArchiveCabinetCrushRules.Phase;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class ArchiveCabinetCrushAnomaly : AnomalyBehaviour
    {
        [SerializeField] private BoxCollider strikeVolume;
        [SerializeField] private Transform warningCabinet;
        [SerializeField] private Camera observer;
        [SerializeField] private PlayerDeathController death;
        [SerializeField] private AnomalyDeathPresentation presentation;
        [SerializeField, Min(0.1f)] private float triggerDistance = 1.9f;
        [SerializeField, Min(0.1f)] private float warningSeconds = 0.9f;
        [SerializeField, Min(0.1f)] private float lungeSeconds = 0.55f;
        private ArchiveCabinetCrushRules rules;
        private CharacterController victim;
        private Rigidbody movingBody;
        private Vector3 movingStart, warningStart, movingWorld, destination, centerOffset, halfSize;
        private Quaternion movingRotation, warningRotation, cameraStart, cameraLocal;
        private Bounds warningBounds;
        private float cameraElapsed;
        private bool ownsCamera;
        public CrushPhase State => rules != null ? rules.State : CrushPhase.Waiting;
        public bool IsConfigured => strikeVolume != null && warningCabinet != null && observer != null
            && death != null && presentation != null;

        public void Configure(BoxCollider strike, Transform warning, Camera camera,
            PlayerDeathController controller, AnomalyDeathPresentation view)
        {
            strikeVolume = strike;
            warningCabinet = warning;
            observer = camera;
            death = controller;
            presentation = view;
        }

        public override void CaptureNormalState()
        {
            if (rules != null || !IsConfigured) return;
            victim = observer.transform.root.GetComponent<CharacterController>();
            movingBody = strikeVolume.GetComponent<Rigidbody>();
            Transform moving = strikeVolume.transform;
            movingStart = moving.localPosition;
            movingRotation = moving.localRotation;
            movingWorld = moving.position;
            warningStart = warningCabinet.localPosition;
            warningRotation = warningCabinet.localRotation;
            warningBounds = CabinetBounds(warningCabinet);
            Bounds bounds = strikeVolume.bounds;
            centerOffset = bounds.center - moving.position;
            halfSize = bounds.extents;
            Vector3 offset = warningBounds.center - bounds.center;
            offset.y = 0f;
            Vector3 direction = offset.normalized;
            float padding = Vector3.Dot(Abs(direction), bounds.extents + warningBounds.extents) + 0.03f;
            destination = movingWorld + direction * Mathf.Max(0f, offset.magnitude - padding);
            rules = new ArchiveCabinetCrushRules(warningSeconds, lungeSeconds);
        }

        private static Bounds CabinetBounds(Transform cabinet)
        {
            Renderer[] renderers = cabinet.GetComponentsInChildren<Renderer>();
            var bounds = new Bounds(cabinet.position, Vector3.zero);
            bool first = true;
            foreach (Renderer renderer in renderers)
            {
                if (!renderer.enabled) continue;
                if (first) { bounds = renderer.bounds; first = false; }
                else bounds.Encapsulate(renderer.bounds);
            }
            return bounds;
        }

        private static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

        protected override void OnActivate()
        {
            CaptureNormalState();
            rules?.Reset();
        }

        private void FixedUpdate()
        {
            if (!IsActive || rules == null || !IsConfigured) return;
            if (State == CrushPhase.Caught)
            {
                if (death.IsScreenBlack || !death.IsDeathInProgress) ResetAnomaly();
                return;
            }
            if (death.IsDeathInProgress || State == CrushPhase.Spent) return;
            Advance();
        }

        private void Advance()
        {
            Vector3 point = observer.transform.root.position;
            Vector3 closest = warningBounds.ClosestPoint(point);
            Vector3 offset = point - closest;
            offset.y = 0f;
            CrushPhase previous = State;
            bool sameFloor = Mathf.Abs(point.y - warningBounds.min.y) < 1.5f;
            rules.Tick(Time.fixedDeltaTime, sameFloor && offset.sqrMagnitude < triggerDistance * triggerDistance);
            if (State == CrushPhase.Waiting || State == CrushPhase.Warning) Shake();
            if (previous != CrushPhase.Lunging && State == CrushPhase.Lunging) BeginLook();
            if (State == CrushPhase.Lunging) Move(rules.Progress);
            else if (previous == CrushPhase.Lunging && State == CrushPhase.Spent)
            {
                cameraStart = observer.transform.rotation;
                cameraElapsed = lungeSeconds;
                Move(1f);
            }
        }

        private void Shake()
        {
            float strength = State == CrushPhase.Warning ? 0.025f : 0.008f;
            float vibration = Mathf.Sin(Time.time * 43f);
            warningCabinet.localPosition = warningStart + new Vector3(vibration * strength, 0f, vibration * strength * 0.4f);
            warningCabinet.localRotation = warningRotation * Quaternion.Euler(0f, 0f, vibration * strength * 25f);
        }

        private void Move(float progress)
        {
            Vector3 next = Vector3.Lerp(movingWorld, destination, progress);
            Bounds sweep = new Bounds(strikeVolume.transform.position + centerOffset, halfSize * 2f);
            sweep.Encapsulate(new Bounds(next + centerOffset, halfSize * 2f));
            bool hit = victim != null && victim.enabled && sweep.Intersects(victim.bounds);
            if (movingBody != null) movingBody.MovePosition(next);
            else strikeVolume.transform.position = next;
            if (hit) death.TryBeginDeath(this, presentation);
        }

        private void BeginLook()
        {
            cameraStart = observer.transform.rotation;
            cameraLocal = observer.transform.localRotation;
            cameraElapsed = 0f;
            ownsCamera = true;
        }

        private void LateUpdate()
        {
            if (!IsActive || !ownsCamera || observer == null) return;
            if (death.IsScreenBlack) { ownsCamera = false; return; }
            cameraElapsed += Time.unscaledDeltaTime;
            Quaternion target = observer.transform.rotation;
            if (State == CrushPhase.Lunging || State == CrushPhase.Caught)
            {
                Vector3 direction = strikeVolume.bounds.center + Vector3.up * 0.3f - observer.transform.position;
                if (direction.sqrMagnitude > 0.001f) target = Quaternion.LookRotation(direction);
                observer.transform.rotation = Quaternion.Slerp(cameraStart, target, Mathf.SmoothStep(0f, 1f, cameraElapsed / 0.3f));
                return;
            }
            observer.transform.rotation = Quaternion.Slerp(cameraStart, target, Mathf.SmoothStep(0f, 1f, (cameraElapsed - lungeSeconds) / 0.35f));
            if (cameraElapsed >= lungeSeconds + 0.35f) ownsCamera = false;
        }

        public override void OnPlayerDeathAccepted() => rules?.Catch();

        protected override void OnResetAnomaly()
        {
            rules?.Reset();
            if (ownsCamera && observer != null && death != null && !death.IsDeathInProgress)
                observer.transform.localRotation = cameraLocal;
            ownsCamera = false;
            if (rules == null || strikeVolume == null || warningCabinet == null) return;
            strikeVolume.transform.SetLocalPositionAndRotation(movingStart, movingRotation);
            if (movingBody != null) movingBody.position = strikeVolume.transform.position;
            warningCabinet.SetLocalPositionAndRotation(warningStart, warningRotation);
        }

        private void OnDisable() => ResetAnomaly();
    }
}
