using UnityEngine;
using Zero.AnomalyPrototype.Player;
using Phase = Zero.AnomalyPrototype.Anomalies.CabaretBlackHoleRules.Phase;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class CabaretBlackHoleAnomaly : AnomalyBehaviour
    {
        [SerializeField] private Animator actor;
        [SerializeField] private GameObject encounter;
        [SerializeField] private FirstPersonController player;
        [SerializeField] private PlayerDeathController death;
        [SerializeField] private CabaretBlackHoleView hole;
        [SerializeField, Min(.5f)] private float approachDistance = 3f;
        [SerializeField, Min(.1f)] private float standSeconds = 2.266667f;
        [SerializeField, Min(.01f)] private float snapClipSeconds = 1.1f;
        [SerializeField, Min(.1f)] private float warningSeconds = 1.2f;
        [SerializeField, Min(.1f)] private float openSeconds = 2f;
        [SerializeField, Min(.1f)] private float fallSeconds = 1.15f;
        [SerializeField, Min(.2f)] private float holeRadius = .95f;
        [SerializeField, Min(.1f)] private float fallGravity = 15f;
        [SerializeField] private LayerMask sightMask = ~0;
        private static readonly int Sitting = Animator.StringToHash("Sitting");
        private static readonly int Stand = Animator.StringToHash("SitToStand");
        private static readonly int Snap = Animator.StringToHash("StandingFingerSnap");
        private static readonly int Idle = Animator.StringToHash("Idle");
        private CabaretBlackHoleRules rules;
        private Transform observer;
        private PlayerInteraction interaction;
        private Vector3 actorPosition, cameraLocalPosition, fallStart;
        private Quaternion actorRotation, cameraLocalRotation, fallRotation;
        private bool ownsCamera, interactionEnabled, normalRootMotion;
        public Phase State => rules != null ? rules.State : Phase.Waiting;
        public bool IsConfigured => actor != null && encounter != null && player != null && player.PlayerCamera != null
            && death != null && hole != null && hole.IsConfigured;

        public override void CaptureNormalState()
        {
            if (rules != null || !IsConfigured) return;
            rules = new CabaretBlackHoleRules(standSeconds, snapClipSeconds, warningSeconds, openSeconds, fallSeconds);
            observer = player.PlayerCamera.transform; interaction = player.GetComponent<PlayerInteraction>();
            actorPosition = actor.transform.position; actorRotation = actor.transform.rotation;
            normalRootMotion = actor.applyRootMotion;
            hole.Capture();
        }

        protected override void OnActivate()
        {
            CaptureNormalState();
            if (rules == null) { Debug.LogError("CABARET_HOLE_CONFIGURATION_INVALID", this); return; }
            rules.Reset(); encounter.SetActive(true);
            actor.enabled = true; actor.applyRootMotion = true; actor.speed = 1f;
            actor.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            actor.Rebind(); actor.Update(0f);
            if (!HasStates()) { Debug.LogError("CABARET_HOLE_ANIMATION_MISSING", this); ResetAnomaly(); return; }
            actor.Play(Sitting, 0, 0f); actor.Update(0f);
        }

        private bool HasStates() => actor.HasState(0, Sitting) && actor.HasState(0, Stand)
            && actor.HasState(0, Snap) && actor.HasState(0, Idle);

        private void LateUpdate()
        {
            if (!IsActive || rules == null || !IsConfigured || Time.timeScale <= 0f) return;
            if (State == Phase.Caught)
            {
                if (death.IsScreenBlack || !death.IsDeathInProgress) ResetAnomaly();
                return;
            }
            if (death.IsDeathInProgress) { ResetAnomaly(); return; }
            Advance();
            if (ownsCamera) PresentFall();
        }

        private void Advance()
        {
            var info = actor.IsInTransition(0) ? actor.GetNextAnimatorStateInfo(0) : actor.GetCurrentAnimatorStateInfo(0);
            int expected = State == Phase.Rising ? Stand : Snap;
            float clipTime = info.shortNameHash == expected ? info.normalizedTime * info.length : 0f;
            var previous = State;
            bool nearby = State == Phase.Waiting && IsNearby();
            rules.Tick(Time.deltaTime, nearby, IsInsideHole(player.transform.position, hole.Center, holeRadius), clipTime);
            if (previous != State) EnterState();
            if (State == Phase.Warning) hole.PresentWarning(rules.WarningProgress);
            if (State != Phase.Snapping && info.shortNameHash == Snap && info.normalizedTime >= 1f) Play(Idle);
        }

        private bool IsNearby()
        {
            Vector3 offset = player.transform.position - actorPosition;
            if (Mathf.Abs(offset.y) > 1.5f || new Vector2(offset.x, offset.z).sqrMagnitude > approachDistance * approachDistance) return false;
            return !Physics.Linecast(observer.position, actorPosition + Vector3.up * 1.2f, sightMask, QueryTriggerInteraction.Ignore);
        }

        private void EnterState()
        {
            switch (State)
            {
                case Phase.Rising: Play(Stand); break;
                case Phase.Snapping: Play(Snap); break;
                case Phase.Warning: PlaceHole(); break;
                case Phase.Open: hole.OpenHole(holeRadius); break;
                case Phase.Falling: hole.OpenHole(holeRadius); BeginFall(); break;
                case Phase.Caught: Strike(); break;
                case Phase.Finished: hole.Restore(); Play(Idle); break;
            }
        }

        private void Play(int state) => actor.CrossFadeInFixedTime(state, .12f, 0, 0f);

        private void PlaceHole()
        {
            Vector3 start = player.transform.position + Vector3.up * .5f;
            if (!Physics.Raycast(start, Vector3.down, out var hit, 2f, sightMask, QueryTriggerInteraction.Ignore)
                || !hole.CanOpenAt(hit, holeRadius))
            { rules.Cancel(); hole.Restore(); Play(Idle); return; }
            hole.ShowWarning(hit.point + Vector3.up * .03f, holeRadius);
        }

        public static bool IsInsideHole(Vector3 feet, Vector3 center, float radius)
        {
            Vector3 offset = feet - center;
            return Mathf.Abs(offset.y) < 1f && offset.x * offset.x + offset.z * offset.z < radius * radius;
        }

        private void BeginFall()
        {
            cameraLocalPosition = observer.localPosition; cameraLocalRotation = observer.localRotation;
            fallStart = observer.position; fallRotation = observer.rotation;
            interactionEnabled = interaction != null && interaction.enabled;
            if (interaction != null) interaction.enabled = false;
            player.SetCinematicLock(true); ownsCamera = true;
        }

        private void PresentFall()
        {
            float seconds = State == Phase.Caught ? fallSeconds : rules.Elapsed;
            Vector3 center = hole.Center; center.y = fallStart.y;
            Vector3 position = Vector3.Lerp(fallStart, center, Mathf.SmoothStep(0f, 1f, seconds / .25f));
            position.y -= .5f * fallGravity * seconds * seconds;
            Quaternion target = Quaternion.LookRotation(Vector3.down + player.transform.forward * .2f, player.transform.forward);
            observer.SetPositionAndRotation(position, Quaternion.Slerp(fallRotation, target, Mathf.SmoothStep(0f, 1f, seconds / .55f)));
        }

        private void Strike()
        {
            PresentFall();
            if (interaction != null) interaction.enabled = interactionEnabled;
            if (death.TryBeginFreezeDeath(this, 0f)) ownsCamera = false;
            else ResetAnomaly();
        }

        private void ReleaseCamera()
        {
            if (!ownsCamera) return;
            ownsCamera = false;
            if (death != null && death.IsDeathInProgress) return;
            if (observer != null) observer.SetLocalPositionAndRotation(cameraLocalPosition, cameraLocalRotation);
            if (interaction != null) interaction.enabled = interactionEnabled;
            if (player != null) player.SetCinematicLock(false);
        }

        protected override void OnResetAnomaly()
        {
            ReleaseCamera(); rules?.Reset(); hole?.Restore();
            if (rules != null && actor != null)
            {
                actor.applyRootMotion = normalRootMotion;
                actor.transform.SetPositionAndRotation(actorPosition, actorRotation);
            }
            if (encounter != null) encounter.SetActive(false);
        }

        private void OnDisable() => ResetAnomaly();
    }
}
