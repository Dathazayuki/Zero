using UnityEngine;
using Zero.AnomalyPrototype.Player;
using Phase = Zero.AnomalyPrototype.Anomalies.CabaretCurtainCallRules.Phase;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class CabaretCurtainCallAnomaly : AnomalyBehaviour
    {
        [SerializeField] private Animator actor;
        [SerializeField] private CharacterController playerBody;
        [SerializeField] private PlayerDeathController death;
        [SerializeField] private CabaretGlitchFieldView field;
        [SerializeField] private BoxCollider escapeZone;
        [SerializeField, Min(.5f)] private float approachDistance = 3.5f;
        [SerializeField, Min(.1f)] private float spreadSpeed = 1.8f;
        [SerializeField, Min(.1f)] private float initialRadius = .35f;
        [SerializeField, Min(1f)] private float maximumRadius = 18f;
        [SerializeField, Range(0f, 1f)] private float maximumScreenGlitch = .55f;
        private static readonly int Idle = Animator.StringToHash("IR5_12_HostIdle");
        private static readonly int Bow = Animator.StringToHash("CelebrateBow");
        private CabaretCurtainCallRules rules;
        private Transform head;
        private Vector3 actorPosition, fieldCentre;
        private Quaternion actorRotation;
        private bool rootMotion;
        private float animationSpeed;
        public Phase State => rules != null ? rules.State : Phase.Waiting;
        public float FieldRadius => rules != null ? rules.Radius : 0f;
        public bool IsConfigured => actor != null && playerBody != null && death != null
            && field != null && field.IsConfigured && escapeZone != null;

        public override void CaptureNormalState()
        {
            if (rules != null || !IsConfigured) return;
            rules = new CabaretCurtainCallRules(spreadSpeed, initialRadius, maximumRadius);
            actorPosition = actor.transform.position; actorRotation = actor.transform.rotation;
            rootMotion = actor.applyRootMotion; animationSpeed = actor.speed;
            if (actor.isHuman) head = actor.GetBoneTransform(HumanBodyBones.Head);
            field.Capture();
        }

        protected override void OnActivate()
        {
            CaptureNormalState();
            if (rules == null) { Debug.LogError("CABARET_CURTAIN_CONFIGURATION_INVALID", this); return; }
            rules.Reset(); actor.gameObject.SetActive(true);
            actor.enabled = true; actor.speed = 1f; actor.applyRootMotion = true;
            actor.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            actor.Rebind(); actor.Update(0f);
            if (!actor.HasState(0, Idle) || !actor.HasState(0, Bow))
            { Debug.LogError("CABARET_CURTAIN_ANIMATION_MISSING: Host idle and CelebrateBow required", this); ResetAnomaly(); return; }
            actor.Play(Idle, 0, 0f); actor.Update(0f);
        }

        private void LateUpdate()
        {
            if (!IsActive || rules == null || !IsConfigured || Time.deltaTime <= 0f) return;
            if (State == Phase.Caught || death.IsDeathInProgress)
            { if (death.IsScreenBlack) ResetAnomaly(); return; }
            Advance();
        }

        private void Advance()
        {
            Phase previous = State;
            var animation = actor.GetCurrentAnimatorStateInfo(0);
            bool finished = animation.shortNameHash == Bow && animation.normalizedTime >= 1f;
            if (State == Phase.Bowing && finished) fieldCentre = HeadPosition();
            rules.Tick(Time.deltaTime, IsNearby(), finished, DistanceFromPlayer(),
                escapeZone.bounds.Contains(playerBody.transform.position + Vector3.up));
            if (previous != State) EnterState();
            if (State != Phase.Bowing && State != Phase.Spreading) return;
            float intensity = State == Phase.Bowing ? .12f : Mathf.Lerp(.2f, maximumScreenGlitch,
                1f - Mathf.Clamp01((DistanceFromPlayer() - FieldRadius) / 6f));
            field.Present(fieldCentre, FieldRadius, intensity);
        }

        private Vector3 HeadPosition() => head != null ? head.position : actor.transform.position + Vector3.up * 2f;

        private bool IsNearby()
        {
            Vector3 offset = playerBody.transform.position - actor.transform.position;
            if (Mathf.Abs(offset.y) > 2f) return false;
            offset.y = 0f;
            return offset.sqrMagnitude <= approachDistance * approachDistance;
        }

        private float DistanceFromPlayer()
        {
            Vector3 scale = playerBody.transform.lossyScale;
            float radius = playerBody.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
            Vector3 centre = playerBody.transform.TransformPoint(playerBody.center);
            Vector3 axis = playerBody.transform.up * Mathf.Max(0f, playerBody.height * Mathf.Abs(scale.y) * .5f - radius);
            return CabaretCurtainCallRules.DistanceToCapsule(fieldCentre, centre - axis, centre + axis, radius);
        }

        private void EnterState()
        {
            switch (State)
            {
                case Phase.Bowing:
                    fieldCentre = HeadPosition(); actor.CrossFadeInFixedTime(Bow, .12f, 0, 0f); break;
                case Phase.Spreading: actor.speed = 0f; break;
                case Phase.Escaped: field.Restore(); actor.speed = 0f; break;
                case Phase.Caught:
                    if (!death.TryBeginFreezeDeath(this, 0f)) ResetAnomaly(); break;
            }
        }

        public override void OnPlayerDeathAccepted() { field.Restore(); actor.speed = 0f; }

        protected override void OnResetAnomaly()
        {
            field?.Restore(); rules?.Reset();
            if (actor == null) return;
            if (rules != null)
            {
                actor.applyRootMotion = rootMotion; actor.speed = animationSpeed;
                actor.transform.SetPositionAndRotation(actorPosition, actorRotation);
            }
            actor.gameObject.SetActive(false);
        }

        private void OnDisable() => ResetAnomaly();
    }
}
