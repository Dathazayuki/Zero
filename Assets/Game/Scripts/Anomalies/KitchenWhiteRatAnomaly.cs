using UnityEngine;
using Zero.AnomalyPrototype.Player;
using Phase = Zero.AnomalyPrototype.Anomalies.KitchenWhiteRatRules.Phase;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class KitchenWhiteRatAnomaly : AnomalyBehaviour
    {
        [SerializeField] private KitchenWhiteRatActor actor;
        [SerializeField] private FirstPersonController player;
        [SerializeField] private PlayerDeathController death;
        [SerializeField] private BoxCollider trigger;
        [SerializeField, Min(0.1f)] private float attackDistance = 2.4f;
        private readonly KitchenWhiteRatRules rules = new KitchenWhiteRatRules();
        private Transform cameraTransform;
        private Vector3 cameraPosition, jumpStart;
        private Quaternion cameraRotation;
        private bool ownsCamera;
        public override void CaptureNormalState()
        {
            if (actor == null || player == null || player.PlayerCamera == null) return;
            actor.Capture(); cameraTransform = player.PlayerCamera.transform;
        }
        protected override void OnActivate()
        {
            CaptureNormalState(); rules.Reset();
            if (cameraTransform != null) actor.Begin();
        }
        private void LateUpdate()
        {
            if (!IsActive || cameraTransform == null || death == null || trigger == null || Time.timeScale <= 0f) return;
            if (death.IsDeathInProgress) { ResetAnomaly(); return; }
            Phase previous = rules.State;
            bool nearby = (player.transform.position - actor.RatPosition).sqrMagnitude <= attackDistance * attackDistance;
            rules.Tick(Time.deltaTime, trigger.bounds.Contains(player.transform.position + Vector3.up * 0.8f), actor.DeathFinished, nearby);
            if (previous != rules.State) EnterState();
            Present();
        }
        private void EnterState()
        {
            switch (rules.State)
            {
                case Phase.Falling: actor.Fall(); break;
                case Phase.HatRolling: actor.BeginHat(); break;
                case Phase.Walking: actor.BeginWalk(); break;
                case Phase.Idle: actor.Rest(); break;
                case Phase.Jumping: BeginAttack(); break;
                case Phase.Caught: Kill(); break;
            }
        }
        private void BeginAttack()
        {
            cameraPosition = cameraTransform.localPosition; cameraRotation = cameraTransform.localRotation;
            jumpStart = actor.RatPosition; ownsCamera = true; player.SetCinematicLock(true);
            actor.BeginCling();
        }
        private void Present()
        {
            if (rules.State == Phase.HatRolling) actor.RollHat(rules.Elapsed);
            if (rules.State == Phase.Walking) actor.WalkOut(rules.Elapsed);
            if (!ownsCamera) return;
            if (rules.State == Phase.Shaking)
            {
                float t = rules.Elapsed;
                cameraTransform.localPosition = cameraPosition + new Vector3(Mathf.Sin(t * 91f), Mathf.Sin(t * 113f), 0f) * 0.035f;
                cameraTransform.localRotation = cameraRotation * Quaternion.Euler(Mathf.Sin(t * 79f) * 6f, Mathf.Sin(t * 101f) * 5f, Mathf.Sin(t * 83f) * 7f);
            }
            Vector3 target = cameraTransform.position + cameraTransform.forward * 0.22f + cameraTransform.up * 0.06f;
            float progress = rules.State == Phase.Jumping ? Mathf.Clamp01(rules.Elapsed / 0.45f) : 1f;
            Vector3 point = Vector3.Lerp(jumpStart, target, progress) + Vector3.up * Mathf.Sin(progress * Mathf.PI) * 0.25f;
            Quaternion facing = Quaternion.LookRotation(-cameraTransform.forward, cameraTransform.up);
            actor.MoveRat(point, Quaternion.Slerp(facing, KitchenRatClingPose.BodyRotation(cameraTransform), progress));
            actor.PresentCling(cameraTransform, progress);
        }
        private void Kill()
        {
            ReleaseCamera();
            if (!death.TryBeginFreezeDeath(this, 0f)) ResetAnomaly();
        }
        private void ReleaseCamera()
        {
            if (!ownsCamera) return;
            ownsCamera = false;
            if (death != null && death.IsDeathInProgress) return;
            cameraTransform.SetLocalPositionAndRotation(cameraPosition, cameraRotation);
            player.SetCinematicLock(false);
        }
        protected override void OnResetAnomaly()
        {
            ReleaseCamera(); rules.Reset(); actor?.Restore();
        }
        private void OnDisable() => ResetAnomaly();
    }
}
