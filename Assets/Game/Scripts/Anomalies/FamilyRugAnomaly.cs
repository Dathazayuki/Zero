using UnityEngine;
using Zero.AnomalyPrototype.Player;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class FamilyRugAnomaly : AnomalyBehaviour
    {
        [SerializeField] private FamilyRugPresentation family;
        [SerializeField] private Transform observer;
        [SerializeField] private FirstPersonController player;
        [SerializeField] private PlayerDeathController death;
        [SerializeField] private AnomalyDeathPresentation presentation;
        [SerializeField] private BoxCollider rug;
        [SerializeField, Min(0.1f)] private float warningSeconds = 3f;
        [SerializeField, Min(0.1f)] private float lookDownSeconds = 0.45f;
        [SerializeField, Min(0f)] private float handsHoldSeconds = 0.35f;
        [SerializeField, Min(0.1f)] private float lookUpSeconds = 0.65f;
        private FamilyRugRules rules;
        private bool ownsCamera;
        private float deathElapsed;
        private Quaternion cameraStart, cameraLocalStart;
        private Vector3 deathForward;
        public bool IsConfigured => family != null && family.IsConfigured && observer != null && player != null
            && death != null && presentation != null && rug != null;
        public FamilyRugRules.PhaseState Phase => rules != null ? rules.Phase : FamilyRugRules.PhaseState.Waiting;

        public override void CaptureNormalState()
        {
            if (!IsConfigured || rules != null) return;
            rules = new FamilyRugRules(warningSeconds);
            family.Capture();
        }

        protected override void OnActivate()
        {
            CaptureNormalState();
            if (rules == null) return;
            rules.Reset();
            family.Begin();
        }

        private void Update()
        {
            if (!IsActive || !IsConfigured || rules == null) return;
            if (Phase == FamilyRugRules.PhaseState.Caught)
            {
                if (!death.IsDeathInProgress) ResetAnomaly();
                return;
            }
            if (death.IsDeathInProgress) return;
            Advance();
        }

        private void Advance()
        {
            var previous = Phase;
            Vector3 point = player.transform.position;
            bool inside = new Bounds(rug.center, rug.size).Contains(rug.transform.InverseTransformPoint(point));
            rules.Tick(Time.deltaTime, inside);
            family.Warn(rules.Progress, point);
            if (Phase == FamilyRugRules.PhaseState.Waiting)
            {
                if (previous != Phase) { family.Restore(); family.Begin(); }
            }
            if (Phase == FamilyRugRules.PhaseState.Grabbing)
            {
                if (!death.TryBeginDeath(this, presentation)) ResetAnomaly();
            }
            if (Phase == FamilyRugRules.PhaseState.Finished) ResetAnomaly();
        }

        private void LateUpdate()
        {
            if (!IsActive || !ownsCamera) return;
            if (death.State != PlayerDeathController.DeathState.Frozen || death.IsScreenBlack)
            { ownsCamera = false; return; }
            deathElapsed += Time.unscaledDeltaTime;
            family.Grab(player.transform.position, deathElapsed);
            Quaternion down = LookAt(family.HandsFocus + deathForward * 0.12f);
            float riseStart = lookDownSeconds + handsHoldSeconds;
            if (deathElapsed < riseStart)
            {
                observer.rotation = Quaternion.Slerp(cameraStart, down, Mathf.SmoothStep(0f, 1f, deathElapsed / lookDownSeconds));
                return;
            }
            float progress = Mathf.SmoothStep(0f, 1f, (deathElapsed - riseStart) / lookUpSeconds);
            observer.rotation = Quaternion.Slerp(down, LookAt(family.AttackFocus), progress);
        }

        private Quaternion LookAt(Vector3 target)
        {
            Vector3 direction = target - observer.position;
            return direction.sqrMagnitude > 0.001f ? Quaternion.LookRotation(direction, Vector3.up) : observer.rotation;
        }

        public override void OnPlayerDeathAccepted()
        {
            if (rules == null || Phase == FamilyRugRules.PhaseState.Caught) return;
            rules.Catch();
            cameraStart = observer.rotation;
            cameraLocalStart = observer.localRotation;
            deathForward = Vector3.ProjectOnPlane(observer.forward, Vector3.up).normalized;
            if (deathForward.sqrMagnitude < 0.01f) deathForward = player.transform.forward;
            deathElapsed = 0f;
            ownsCamera = true;
            family.Grab(player.transform.position, 0f);
            family.BeginAttack(player.transform.position, deathForward);
        }

        protected override void OnResetAnomaly()
        {
            rules?.Reset();
            family?.Restore();
            if (ownsCamera && observer != null && death != null && death.State == PlayerDeathController.DeathState.Frozen)
                observer.localRotation = cameraLocalStart;
            ownsCamera = false;
        }

        private void OnDisable() => ResetAnomaly();
    }
}
