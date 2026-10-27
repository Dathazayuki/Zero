using UnityEngine;
using Zero.AnomalyPrototype.Player;
using Phase = Zero.AnomalyPrototype.Anomalies.ArchiveCabinetCrushRules.Phase;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class CabaretChandelierAnomaly : AnomalyBehaviour
    {
        [SerializeField] private Transform chandelier;
        [SerializeField] private GameObject hand;
        [SerializeField] private BoxCollider approachVolume;
        [SerializeField] private BoxCollider strikeVolume;
        [SerializeField] private FirstPersonController player;
        [SerializeField] private PlayerDeathController death;
        [SerializeField] private Transform glow;
        [SerializeField, Min(.1f)] private float warningSeconds = 1.1f;
        [SerializeField, Min(.1f)] private float fallSeconds = 1.05f;
        [SerializeField, Min(.1f)] private float lookSeconds = .35f;
        [SerializeField] private float floorHeight = .08f;
        private ArchiveCabinetCrushRules rules;
        private CharacterController victim;
        private Animator handAnimator;
        private PlayerInteraction interaction;
        private Transform observer;
        private Vector3 start, destination, glowStart, impactStart;
        private Quaternion normalRotation, cameraLocal, lookStart, lookTarget;
        private bool ownsCamera, handAnimated, interactionEnabled, inputEnabled;
        private float lookElapsed;
        public Phase State => rules != null ? rules.State : Phase.Waiting;
        public bool IsConfigured => chandelier != null && hand != null && approachVolume != null
            && strikeVolume != null && player != null && player.PlayerCamera != null && death != null;

        public override void CaptureNormalState()
        {
            if (rules != null || !IsConfigured) return;
            // Reuse the existing one-shot warning / strike / spent trap rules.
            rules = new ArchiveCabinetCrushRules(warningSeconds, fallSeconds);
            start = chandelier.position; normalRotation = chandelier.rotation;
            destination = start + Vector3.down * Mathf.Max(0f, strikeVolume.bounds.min.y - floorHeight);
            if (glow != null) glowStart = glow.position;
            observer = player.PlayerCamera.transform;
            victim = player.GetComponent<CharacterController>();
            interaction = player.GetComponent<PlayerInteraction>();
            handAnimator = hand.GetComponent<Animator>();
            handAnimated = handAnimator != null && handAnimator.enabled;
        }

        protected override void OnActivate()
        {
            CaptureNormalState();
            if (rules == null) { Debug.LogError("CABARET_CHANDELIER_CONFIGURATION_INVALID", this); return; }
            rules.Reset();
            hand.SetActive(true);
        }

        private void FixedUpdate()
        {
            if (!IsActive || rules == null || !IsConfigured || Time.timeScale <= 0f) return;
            if (CanAdvance()) Advance();
        }

        private bool CanAdvance()
        {
            if (State == Phase.Caught)
            {
                if (death.IsScreenBlack) ResetAnomaly();
                return false;
            }
            if (death.IsDeathInProgress) { ResetAnomaly(); return false; }
            return true;
        }

        private void Advance()
        {
            var previous = State;
            rules.Tick(Time.fixedDeltaTime, approachVolume.bounds.Contains(player.transform.position + Vector3.up));
            if (State == Phase.Waiting || State == Phase.Warning) Shake();
            if (previous != Phase.Lunging && State == Phase.Lunging) hand.SetActive(false);
            if (State == Phase.Lunging) Fall(rules.Progress);
            else if (previous == Phase.Lunging && State == Phase.Spent) Fall(1f);
        }

        private void Shake()
        {
            float strength = State == Phase.Warning ? 6f : .7f;
            chandelier.rotation = normalRotation * Quaternion.Euler(
                Mathf.Sin(Time.time * 23f) * strength, 0f, Mathf.Sin(Time.time * 31f) * strength);
        }

        private void Fall(float progress)
        {
            Bounds before = strikeVolume.bounds;
            chandelier.SetPositionAndRotation(Vector3.Lerp(start, destination, progress * progress), normalRotation);
            if (glow != null) glow.position = glowStart + chandelier.position - start;
            if (victim == null || !victim.enabled) return;
            Bounds head = victim.bounds;
            head.center = new Vector3(head.center.x, observer.position.y, head.center.z);
            head.extents = new Vector3(head.extents.x, .1f, head.extents.z);
            if (SweptHit(before, strikeVolume.bounds, head)) BeginImpact();
        }

        public static bool SweptHit(Bounds before, Bounds after, Bounds playerBounds)
        {
            before.Encapsulate(after);
            return before.Intersects(playerBounds);
        }

        private void BeginImpact()
        {
            if (ownsCamera || State == Phase.Caught) return;
            rules.Catch();
            cameraLocal = observer.localRotation; lookStart = observer.rotation;
            lookTarget = ImpactLook(player.transform.rotation);
            impactStart = chandelier.position;
            lookElapsed = 0f; ownsCamera = true;
            inputEnabled = player.InputEnabled;
            interactionEnabled = interaction != null && interaction.enabled;
            if (interaction != null) interaction.enabled = false;
            player.SetCinematicLock(true);
        }

        public static Quaternion ImpactLook(Quaternion bodyRotation)
            => bodyRotation * Quaternion.Euler(-85f, 0f, 0f);

        private void LateUpdate()
        {
            if (!IsActive || !ownsCamera || rules == null || Time.timeScale <= 0f) return;
            if (death.IsDeathInProgress) { ReleaseCamera(); return; }
            lookElapsed += Time.deltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, lookElapsed / lookSeconds);
            observer.rotation = Quaternion.Slerp(lookStart, lookTarget, progress);
            chandelier.position = impactStart + Vector3.down * (.15f * progress);
            if (glow != null) glow.position = glowStart + chandelier.position - start;
            if (lookElapsed < lookSeconds) return;
            if (interaction != null) interaction.enabled = interactionEnabled;
            if (death.TryBeginFreezeDeath(this, 0f)) ReleaseCamera();
            else ResetAnomaly();
        }

        private void ReleaseCamera()
        {
            if (!ownsCamera) return;
            ownsCamera = false;
            if (death != null && death.IsDeathInProgress) return;
            if (interaction != null) interaction.enabled = interactionEnabled;
            if (observer != null) observer.localRotation = cameraLocal;
            if (player == null) return;
            player.SetCinematicLock(false);
            player.SetInputEnabled(inputEnabled);
        }

        public override void OnPlayerDeathAccepted() => rules?.Catch();

        protected override void OnResetAnomaly()
        {
            ReleaseCamera(); rules?.Reset();
            if (rules != null && chandelier != null) chandelier.SetPositionAndRotation(start, normalRotation);
            if (rules != null && glow != null) glow.position = glowStart;
            if (hand != null) hand.SetActive(false);
            if (handAnimator != null) handAnimator.enabled = handAnimated;
        }

        private void OnDisable() => ResetAnomaly();
    }
}
