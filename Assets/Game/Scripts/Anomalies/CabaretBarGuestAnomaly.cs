using UnityEngine;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class CabaretBarGuestAnomaly : AnomalyBehaviour
    {
        [SerializeField] private Animator actor;
        [SerializeField] private Camera observer;
        [SerializeField] private Transform neck;
        [SerializeField] private Transform stageTarget;
        [SerializeField] private Transform glass;
        [SerializeField, Min(.1f)] private float rattleSeconds = .7f;
        [SerializeField, Min(0f)] private float rattleDegrees = 3f;
        private static readonly int Sitting = Animator.StringToHash("Sitting");
        private readonly CabaretBarGuestRules rules = new CabaretBarGuestRules();
        private Vector3 actorPosition, neckAim;
        private Quaternion actorRotation, neckRotation, glassRotation;
        private bool captured, normalMotion;
        private float rattle;
        public CabaretBarGuestRules.Phase State => rules.State;
        public bool IsConfigured => actor != null && observer != null && neck != null && stageTarget != null && glass != null;

        public override void CaptureNormalState()
        {
            if (captured || !IsConfigured) return;
            actorPosition = actor.transform.position; actorRotation = actor.transform.rotation;
            neckRotation = neck.localRotation; glassRotation = glass.localRotation;
            normalMotion = actor.applyRootMotion;
            captured = true;
        }

        protected override void OnActivate()
        {
            CaptureNormalState();
            if (!captured) { Debug.LogError("CABARET_GUEST_CONFIGURATION_INVALID", this); return; }
            rules.Reset(); rattle = 0;
            actor.gameObject.SetActive(true); actor.enabled = true; actor.applyRootMotion = true;
            actor.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            actor.Rebind(); actor.Play(Sitting, 0, 0); actor.Update(0);
            neckAim = neck.InverseTransformDirection(actor.transform.forward);
        }

        private void LateUpdate()
        {
            if (!IsActive || !captured || Time.timeScale <= 0) return;
            if (rules.State == CabaretBarGuestRules.Phase.Vanished) { Rattle(); return; }
            AimNeck();
            bool visible = AnomalyGazeVisibility.IsVisible(observer, neck, null, ~0, .15f);
            if (!rules.Advance(Time.deltaTime, visible)) return;
            actor.gameObject.SetActive(false); rattle = rattleSeconds;
        }

        private void AimNeck()
        {
            Vector3 direction = stageTarget.position - neck.position;
            if (direction.sqrMagnitude < .001f) return;
            neck.rotation = Quaternion.FromToRotation(neck.TransformDirection(neckAim), direction.normalized) * neck.rotation;
        }

        private void Rattle()
        {
            if (rattle <= 0) return;
            rattle = Mathf.Max(0, rattle - Time.deltaTime);
            float angle = Mathf.Sin((rattleSeconds - rattle) * 50f) * rattleDegrees * (rattle / rattleSeconds);
            glass.localRotation = glassRotation * Quaternion.Euler(0, 0, angle);
            if (rattle <= 0) glass.localRotation = glassRotation;
        }

        protected override void OnResetAnomaly()
        {
            rules.Reset(); rattle = 0;
            if (captured)
            {
                if (glass != null) glass.localRotation = glassRotation;
                if (neck != null) neck.localRotation = neckRotation;
                if (actor != null)
                {
                    actor.applyRootMotion = normalMotion;
                    actor.transform.SetPositionAndRotation(actorPosition, actorRotation);
                }
            }
            if (actor != null) actor.gameObject.SetActive(false);
        }
        private void OnDisable() => ResetAnomaly();
    }
}
