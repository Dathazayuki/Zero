using UnityEngine;
using Zero.AnomalyPrototype.Player;
using HeadPhase = Zero.AnomalyPrototype.Anomalies.ArchiveHeadRules.Phase;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class ArchiveHeadAnomaly : AnomalyBehaviour
    {
        [SerializeField] private Transform box;
        [SerializeField] private Transform lid;
        [SerializeField] private ArchiveHeadLegs creature;
        [SerializeField] private Camera observer;
        [SerializeField] private PlayerDeathController death;
        [SerializeField] private AnomalyDeathPresentation presentation;
        [SerializeField] private Vector3 landingOffset = new Vector3(0.95f, -2f, 0f);
        [SerializeField, Min(0.1f)] private float triggerDistance = 4f;
        [SerializeField, Min(0.1f)] private float warningSeconds = 1.2f;
        [SerializeField, Min(0.1f)] private float fallSeconds = 0.7f;
        [SerializeField, Min(0.1f)] private float growSeconds = 2.5f;
        [SerializeField, Range(0.1f, 1f)] private float lookRadius = 0.8f;
        [SerializeField] private LayerMask sightMask = ~0;
        private ArchiveHeadRules rules;
        private Vector3 boxPosition, headPosition, headScale, attackStart;
        private Quaternion boxRotation, lidRotation, headRotation;
        private bool captured;
        public bool IsConfigured => box != null && lid != null && creature != null && creature.Count == 6
            && observer != null && death != null && presentation != null;
        public HeadPhase State => rules != null ? rules.State : HeadPhase.Waiting;
        public bool IsCreatureObserved => observer != null && creature != null
            && LookingAt(creature.transform.position, lookRadius);

        public void Configure(Transform body, Transform hingedLid, ArchiveHeadLegs head)
        {
            box = body;
            lid = hingedLid;
            creature = head;
        }

        public void BindPlayer(Camera camera, PlayerDeathController controller, AnomalyDeathPresentation view)
        {
            observer = camera;
            death = controller;
            presentation = view;
        }

        public override void CaptureNormalState()
        {
            if (captured || !IsConfigured) return;
            boxPosition = box.localPosition;
            boxRotation = box.localRotation;
            lidRotation = lid.localRotation;
            headPosition = creature.transform.localPosition;
            headRotation = creature.transform.localRotation;
            headScale = creature.transform.localScale;
            rules = new ArchiveHeadRules(warningSeconds, fallSeconds, growSeconds);
            captured = true;
        }

        protected override void OnActivate()
        {
            CaptureNormalState();
            if (!captured) return;
            rules.Reset();
            creature.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!IsActive || !captured || !IsConfigured) return;
            if (State == HeadPhase.Caught)
            {
                if (death.IsScreenBlack || !death.IsDeathInProgress) ResetAnomaly();
                return;
            }
            if (death.IsDeathInProgress) return;
            Advance();
        }

        private void Advance()
        {
            HeadPhase previous = State;
            Vector3 offset = observer.transform.position - box.position;
            offset.y = 0f;
            bool near = offset.sqrMagnitude <= triggerDistance * triggerDistance;
            rules.Tick(Time.deltaTime, near,
                State == HeadPhase.Growing && IsCreatureObserved);
            if (previous != State) EnterPhase();
            switch (State)
            {
                case HeadPhase.Rattling: Rattle(); break;
                case HeadPhase.Falling: Fall(); break;
                case HeadPhase.Growing: Grow(); break;
                case HeadPhase.Lunging: Lunge(); break;
            }
        }

        private bool LookingAt(Vector3 point, float radius)
        {
            Vector3 screen = observer.WorldToViewportPoint(point);
            float distance = Vector3.Distance(point, observer.transform.position);
            float margin = radius / Mathf.Max(0.5f, distance);
            if (screen.z <= 0f || screen.x < -margin || screen.x > 1f + margin) return false;
            if (screen.y < -margin || screen.y > 1f + margin) return false;
            // Cast towards the observer so the Player's own collider is an accepted endpoint.
            if (!Physics.Linecast(point, observer.transform.position, out RaycastHit hit, sightMask, QueryTriggerInteraction.Ignore)) return true;
            return hit.transform.IsChildOf(observer.transform.root);
        }

        private void EnterPhase()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"IR3_HEAD_PHASE_CHANGED: phase={State}", this);
#endif
            if (State == HeadPhase.Growing)
            {
                box.localPosition = boxPosition + landingOffset;
                box.localRotation = boxRotation * Quaternion.Euler(0f, 0f, -70f);
                lid.localRotation = lidRotation * Quaternion.Euler(115f, 0f, 0f);
                creature.gameObject.SetActive(true);
                creature.Present(0f, 0f);
            }
            if (State == HeadPhase.Lunging) attackStart = creature.transform.position;
            if (State == HeadPhase.Escaped) creature.Present(1f, 0f);
        }

        private void Rattle()
        {
            float jerk = Mathf.Sin(rules.Elapsed * 43f);
            box.localPosition = boxPosition + new Vector3(jerk * 0.01f, 0f, 0f);
            lid.localRotation = lidRotation * Quaternion.Euler(Mathf.Abs(jerk) * 5f, 0f, 0f);
        }

        private void Fall()
        {
            float t = rules.Progress;
            box.localPosition = boxPosition + new Vector3(landingOffset.x * t, landingOffset.y * t * t, landingOffset.z * t);
            box.localRotation = boxRotation * Quaternion.Euler(0f, 0f, -70f * t);
            lid.localRotation = lidRotation * Quaternion.Euler(115f * t, 0f, 0f);
        }

        private void Grow()
        {
            float t = rules.Progress;
            creature.transform.localPosition = headPosition + new Vector3(0.2f * t, 0f, 0f);
            creature.transform.localRotation = Quaternion.Slerp(headRotation * Quaternion.Euler(0f, 0f, 70f), headRotation, t);
            creature.Present(Mathf.SmoothStep(0f, 1f, t), 0f);
        }

        private void Lunge()
        {
            Transform actor = creature.transform;
            Vector3 target = observer.transform.position + observer.transform.forward * 0.6f;
            float t = rules.Progress;
            actor.position = Vector3.Lerp(attackStart, target, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.45f;
            Vector3 direction = observer.transform.position - actor.position;
            if (direction.sqrMagnitude > 0.001f) actor.rotation = Quaternion.LookRotation(direction);
            creature.Present(1f, rules.Elapsed * 40f);
            if (t < 1f) return;
            if (!death.TryBeginDeath(this, presentation)) ResetAnomaly();
        }

        public override void OnPlayerDeathAccepted() => rules?.Catch();

        protected override void OnResetAnomaly()
        {
            rules?.Reset();
            if (!captured) return;
            if (box != null) box.SetLocalPositionAndRotation(boxPosition, boxRotation);
            if (lid != null) lid.localRotation = lidRotation;
            if (creature == null) return;
            creature.transform.SetLocalPositionAndRotation(headPosition, headRotation);
            creature.transform.localScale = headScale;
            creature.Present(0f, 0f);
            creature.gameObject.SetActive(false);
        }

        private void OnDisable() => ResetAnomaly();
    }
}
