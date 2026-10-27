using UnityEngine;
using Zero.AnomalyPrototype.Player;
using Phase = Zero.AnomalyPrototype.Anomalies.KitchenRatSwarmRules.Phase;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class KitchenRatSwarmAnomaly : AnomalyBehaviour
    {
        [SerializeField] private KitchenRatSwarmActor swarm;
        [SerializeField] private FirstPersonController player;
        [SerializeField] private PlayerDeathController death;
        [SerializeField] private BoxCollider trigger;
        [SerializeField] private Transform warningHood;
        [SerializeField, Min(0f)] private float warningShakeDistance = 0.015f;
        [SerializeField, Min(0f)] private float warningShakeDegrees = 1.2f;
        [SerializeField] private float floorHeight = -0.015f;
        private readonly KitchenRatSwarmRules rules = new KitchenRatSwarmRules();
        private Transform observer;
        private Vector3 localPosition, startPosition, fallenPosition, forward;
        private Quaternion localRotation, startRotation, fallenRotation;
        private bool ownsCamera;
        private Vector3 hoodPosition;
        private Quaternion hoodRotation;
        private bool hoodCaptured, hoodShaking;
        public override void CaptureNormalState()
        {
            if (swarm == null || player == null || player.PlayerCamera == null || !swarm.IsConfigured) return;
            swarm.Capture(); observer = player.PlayerCamera.transform;
            if (!hoodCaptured && warningHood != null)
            {
                hoodPosition = warningHood.localPosition; hoodRotation = warningHood.localRotation;
                hoodCaptured = true;
            }
        }
        protected override void OnActivate()
        {
            CaptureNormalState(); rules.Reset();
            if (observer != null) swarm.Begin();
        }
        private void LateUpdate()
        {
            if (!IsActive || observer == null || death == null || trigger == null || Time.timeScale <= 0f) return;
            if (death.IsDeathInProgress) { ResetAnomaly(); return; }
            bool running = rules.State == Phase.Running || ownsCamera;
            bool hit = running && swarm.Tick(Time.deltaTime, player.transform.position);
            Phase previous = rules.State;
            rules.Tick(Time.deltaTime, trigger.bounds.Contains(player.transform.position + Vector3.up * 0.8f), hit, swarm.Finished);
            if (previous != rules.State) EnterState();
            PresentWarning();
            if (ownsCamera) PresentCamera();
        }
        private void PresentWarning()
        {
            if (!hoodCaptured || warningHood == null) return;
            if (rules.State != Phase.Warning) { RestoreHood(); return; }
            hoodShaking = true;
            float t = rules.Elapsed;
            Vector3 shake = new Vector3(Mathf.Sin(t * 83f), Mathf.Sin(t * 97f), Mathf.Sin(t * 71f));
            warningHood.localPosition = hoodPosition + shake * warningShakeDistance;
            warningHood.localRotation = hoodRotation * Quaternion.Euler(shake * warningShakeDegrees);
        }
        private void RestoreHood()
        {
            if (!hoodShaking) return;
            hoodShaking = false;
            if (warningHood != null) warningHood.SetLocalPositionAndRotation(hoodPosition, hoodRotation);
        }
        private void EnterState()
        {
            if (rules.State == Phase.Falling) BeginFall();
            if (rules.State == Phase.Trampling) swarm.BeginTrampling();
            if (rules.State == Phase.Caught) Kill();
        }
        private void BeginFall()
        {
            localPosition = observer.localPosition; localRotation = observer.localRotation;
            startPosition = observer.position; startRotation = observer.rotation;
            forward = Vector3.ProjectOnPlane(observer.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.01f) forward = player.transform.forward;
            fallenPosition = player.transform.position; fallenPosition.y = floorHeight + 0.16f;
            fallenRotation = Quaternion.LookRotation(forward + Vector3.up * 0.12f) * Quaternion.Euler(0f, 0f, 72f);
            ownsCamera = true; player.SetCinematicLock(true);
        }
        private void PresentCamera()
        {
            float t = rules.State == Phase.Falling ? Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(rules.Elapsed / 0.65f)) : 1f;
            Vector3 position = Vector3.Lerp(startPosition, fallenPosition, t);
            Quaternion rotation = Quaternion.Slerp(startRotation, fallenRotation, t);
            if (rules.State == Phase.Trampling)
            {
                float time = rules.Elapsed;
                position += new Vector3(Mathf.Sin(time * 93f), Mathf.Sin(time * 107f), 0f) * 0.018f;
                rotation *= Quaternion.Euler(Mathf.Sin(time * 83f) * 4f, Mathf.Sin(time * 91f) * 5f, Mathf.Sin(time * 101f) * 5f);
                swarm.Trample(fallenPosition, forward, time);
            }
            observer.SetPositionAndRotation(position, rotation);
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
            observer.SetLocalPositionAndRotation(localPosition, localRotation);
            player.SetCinematicLock(false);
        }
        protected override void OnResetAnomaly()
        {
            ReleaseCamera(); rules.Reset(); swarm?.ResetSwarm();
            RestoreHood();
        }
        private void OnDisable() => ResetAnomaly();
    }
}
