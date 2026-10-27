using UnityEngine;
using Zero.AnomalyPrototype.Player;
using Phase = Zero.AnomalyPrototype.Anomalies.KitchenButcherChaseRules.Phase;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class KitchenButcherChaseAnomaly : AnomalyBehaviour
    {
        [SerializeField] private KitchenButcherActor butcher;
        [SerializeField] private FirstPersonController player;
        [SerializeField] private PlayerDeathController death;
        [SerializeField] private BoxCollider activationZone;
        [SerializeField] private Transform entranceSpawn;
        [SerializeField] private BoxCollider roomVolume;
        [SerializeField] private GameObject normalChef;
        [SerializeField, Min(0.05f)] private float faceSeconds = 0.35f;
        [SerializeField, Min(0.05f)] private float liftSeconds = 1.2f;
        [SerializeField, Min(0.1f)] private float liftAboveFace = 0.45f;
        [SerializeField, Min(0.5f)] private float grabCameraDistance = 0.85f;
        private KitchenButcherChaseRules rules;
        private Camera observer;
        private PlayerInteraction interaction;
        private Vector3 cameraLocalPosition, cameraStart, facingPosition;
        private Quaternion cameraLocalRotation, cameraStartRotation;
        private bool ownsCamera, normalInteraction;
        private bool normalChefActive;
        public Phase State => rules != null ? rules.State : Phase.Waiting;
        public bool IsConfigured => butcher != null && butcher.IsConfigured && player != null && player.PlayerCamera != null
            && death != null && activationZone != null && entranceSpawn != null && roomVolume != null;

        public override void CaptureNormalState()
        {
            if (rules != null || !IsConfigured) return;
            butcher.Capture(); observer = player.PlayerCamera; interaction = player.GetComponent<PlayerInteraction>();
            normalChefActive = normalChef != null && normalChef.activeSelf;
            rules = new KitchenButcherChaseRules(faceSeconds, liftSeconds);
        }

        protected override void OnActivate()
        {
            CaptureNormalState();
            if (rules == null) { Debug.LogError("KITCHEN_CHASE_CONFIGURATION_INVALID", this); return; }
            rules.Reset(); butcher.Hide();
            if (normalChef != null) normalChef.SetActive(false);
        }

        private void Update()
        {
            if (!IsActive || rules == null || !IsConfigured || Time.timeScale <= 0f) return;
            if (State == Phase.Caught)
            {
                if (death.IsScreenBlack || !death.IsDeathInProgress) ResetAnomaly();
                return;
            }
            if (death.IsDeathInProgress) { ResetAnomaly(); return; }
            Advance();
        }

        private void Advance()
        {
            Vector3 point = player.transform.position + Vector3.up * 0.8f;
            Phase previous = State;
            rules.Tick(Time.deltaTime, activationZone.bounds.Contains(point), roomVolume.bounds.Contains(point));
            if (previous != State) EnterPhase();
            if (State == Phase.Chasing && butcher.Pursue(player.transform, observer.transform) && rules.Catch()) BeginGrab();
        }

        private void EnterPhase()
        {
            if (State == Phase.Chasing && !butcher.BeginChase(entranceSpawn))
            { Debug.LogWarning("KITCHEN_CHASE_NAVIGATION_UNAVAILABLE: bake room NavMesh", this); ResetAnomaly(); }
            else if (State == Phase.Escaped) butcher.Hide();
            else if (State == Phase.Caught) Strike();
        }

        private void BeginGrab()
        {
            cameraLocalPosition = observer.transform.localPosition; cameraLocalRotation = observer.transform.localRotation;
            cameraStart = observer.transform.position; cameraStartRotation = observer.transform.rotation;
            normalInteraction = interaction != null && interaction.enabled;
            if (interaction != null) interaction.enabled = false;
            player.SetCinematicLock(true); ownsCamera = true;
            butcher.BeginGrab(observer.transform);
            facingPosition = butcher.FacePoint + butcher.Forward * grabCameraDistance;
            facingPosition.y = cameraStart.y;
        }

        private void LateUpdate()
        {
            if (!IsActive || !ownsCamera || observer == null || Time.timeScale <= 0f) return;
            PresentGrab(); butcher.TrackNeck();
        }

        private void PresentGrab()
        {
            Vector3 position;
            Quaternion rotation;
            if (State == Phase.Facing)
            {
                float t = Mathf.SmoothStep(0f, 1f, rules.FaceProgress);
                position = Vector3.Lerp(cameraStart, facingPosition, t);
                rotation = Quaternion.Slerp(cameraStartRotation, Quaternion.LookRotation(butcher.FacePoint - position), t);
            }
            else
            {
                position = facingPosition;
                position.y = Mathf.Lerp(facingPosition.y, butcher.FacePoint.y + liftAboveFace, Mathf.SmoothStep(0f, 1f, rules.LiftProgress));
                rotation = Quaternion.LookRotation(butcher.FacePoint - position);
            }
            observer.transform.SetPositionAndRotation(position, rotation);
        }

        private void Strike()
        {
            if (interaction != null) interaction.enabled = normalInteraction;
            if (death.TryBeginFreezeDeath(this, 0f)) ownsCamera = false;
            else ResetAnomaly();
        }

        private void ReleaseCamera()
        {
            if (!ownsCamera) return;
            ownsCamera = false;
            if (death != null && death.IsDeathInProgress) return;
            if (observer != null) observer.transform.SetLocalPositionAndRotation(cameraLocalPosition, cameraLocalRotation);
            if (interaction != null) interaction.enabled = normalInteraction;
            if (player != null) player.SetCinematicLock(false);
        }

        protected override void OnResetAnomaly()
        {
            ReleaseCamera(); rules?.Reset(); butcher?.Restore();
            if (rules != null && normalChef != null) normalChef.SetActive(normalChefActive);
        }

        private void OnDisable() => ResetAnomaly();
    }
}
