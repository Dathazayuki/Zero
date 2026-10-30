using UnityEngine;
using Zero.AnomalyPrototype.Flow;
using Zero.AnomalyPrototype.Player;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class CabaretBandAnomaly : AnomalyBehaviour
    {
        [SerializeField] private FirstPersonController player;
        [SerializeField] private PlayerDeathController death;
        [SerializeField] private SceneFadeController vision;
        [SerializeField] private CabaretBandPerformance performance;
        [SerializeField] private BoxCollider centreZone;
        [SerializeField] private BoxCollider escapeZone;
        [SerializeField, Min(1f)] private float escapeSeconds = 18f;
        [SerializeField, Range(0f, .95f)] private float maximumDarkness = .9f;
        private CabaretBandRules rules;
        private bool ownsEffects;
        private Color originalColour;
        private float originalAlpha;
        public CabaretBandRules.Phase State => rules != null ? rules.State : CabaretBandRules.Phase.Waiting;
        public float Progress => rules != null ? rules.Progress : 0f;
        public bool IsConfigured => player != null && death != null && vision != null
            && performance != null && performance.IsConfigured && centreZone != null && escapeZone != null;

        public override void CaptureNormalState() => performance?.CaptureNormalState();

        protected override void OnActivate()
        {
            if (!IsConfigured)
            {
                Debug.LogError("CABARET_BAND_CONFIGURATION_INVALID", this);
                return;
            }
            CaptureNormalState();
            rules = new CabaretBandRules(escapeSeconds);
        }

        private void Update()
        {
            if (!IsActive || rules == null || !IsConfigured || Time.timeScale <= 0f) return;
            if (!CanAdvance()) return;
            Advance();
        }

        private bool CanAdvance()
        {
            if (!death.IsDeathInProgress) return true;
            ClearPresentation();
            if (death.IsScreenBlack) ResetAnomaly();
            return false;
        }

        private void Advance()
        {
            var before = State;
            Vector3 point = player.transform.position + Vector3.up;
            rules.Tick(Time.deltaTime, centreZone.bounds.Contains(point), escapeZone.bounds.Contains(point));
            if (before == CabaretBandRules.Phase.Waiting && State == CabaretBandRules.Phase.Playing)
                BeginPresentation();
            if (State == CabaretBandRules.Phase.Playing) UpdateSymptoms();
            if (before != State && State == CabaretBandRules.Phase.Escaped) ClearPresentation();
            if (before != State && State == CabaretBandRules.Phase.DeathRequested) RequestDeath();
        }

        private void BeginPresentation()
        {
            originalAlpha = vision.Alpha;
            originalColour = vision.Color;
            vision.SetColor(Color.black);
            ownsEffects = true;
            performance.Begin();
            player.SetForcedBreathing(true);
        }

        private void UpdateSymptoms()
        {
            float amount = Mathf.SmoothStep(0f, 1f, Progress);
            vision.SetImmediate(Mathf.Lerp(originalAlpha, maximumDarkness, amount));
            player.SetStumbleIntensity(amount * .8f);
        }

        private void RequestDeath()
        {
            ClearPresentation();
            if (!death.TryBeginFreezeDeath(this, 0f)) ResetAnomaly();
        }

        public override void OnPlayerDeathAccepted() => ClearPresentation();

        private void ClearPresentation()
        {
            performance?.StopAndRestore();
            if (!ownsEffects) return;
            ownsEffects = false;
            if (player != null)
            {
                player.SetForcedBreathing(false);
                player.SetStumbleIntensity(0f);
            }
            if (vision != null)
            {
                vision.SetColor(originalColour);
                vision.SetImmediate(originalAlpha);
            }
        }

        protected override void OnResetAnomaly()
        {
            ClearPresentation();
            rules = null;
        }

        private void OnDisable() => ResetAnomaly();
    }
}
