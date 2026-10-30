using UnityEngine;
using Zero.AnomalyPrototype.Flow;
using Zero.AnomalyPrototype.Player;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class CabaretBlinkChaseAnomaly : AnomalyBehaviour
    {
        [SerializeField] private FirstPersonController player;
        [SerializeField] private PlayerDeathController death;
        [SerializeField] private SceneFadeController vision;
        [SerializeField] private BoxCollider stageZone;
        [SerializeField] private BoxCollider escapeZone;
        [SerializeField] private CabaretBlinkMannequin[] mannequins;
        [SerializeField, Min(.05f)] private float darkSeconds = .18f;
        [SerializeField, Min(.1f)] private float lightSeconds = .8f;
        [SerializeField, Min(.1f)] private float stepDistance = 1.1f;
        private CabaretBlinkRules rules;
        private bool ownsVision;
        private float originalAlpha;
        private Color originalColour;
        public CabaretBlinkRules.Phase State => rules != null ? rules.State : CabaretBlinkRules.Phase.Waiting;
        public bool IsConfigured
        {
            get
            {
                if (player == null || death == null || vision == null || stageZone == null
                    || escapeZone == null || mannequins == null || mannequins.Length != 6) return false;
                foreach (var actor in mannequins) if (actor == null || !actor.IsConfigured) return false;
                return true;
            }
        }

        protected override void OnActivate()
        {
            if (!IsConfigured) { Debug.LogError("CABARET_BLINK_CONFIGURATION_INVALID", this); return; }
            rules = new CabaretBlinkRules(darkSeconds, lightSeconds);
            foreach (var actor in mannequins)
            {
                if (actor == null) continue;
                actor.gameObject.SetActive(true); actor.Prepare(); actor.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (!IsActive || rules == null || !IsConfigured || Time.timeScale <= 0f) return;
            if (death.IsDeathInProgress) { if (death.IsScreenBlack) ResetAnomaly(); return; }
            Advance();
        }

        private void Advance()
        {
            Vector3 point = player.transform.position + Vector3.up;
            bool darkStarted = rules.Tick(Time.deltaTime, stageZone.bounds.Contains(point), escapeZone.bounds.Contains(point));
            if (State == CabaretBlinkRules.Phase.Escaped) { ReleaseVision(); Freeze(); return; }
            if (State == CabaretBlinkRules.Phase.Waiting || State == CabaretBlinkRules.Phase.Caught) return;
            ApplyVision(rules.IsDark);
            if (!darkStarted) return;
            StepMannequins();
        }

        private void StepMannequins()
        {
            bool caught = false;
            foreach (var actor in mannequins)
            {
                if (actor == null) continue;
                actor.gameObject.SetActive(true);
                caught |= actor.StepTowards(player.transform, stepDistance);
            }
            if (!caught) return;
            rules.Catch();
            if (!death.TryBeginFreezeDeath(this, 0f)) ResetAnomaly();
        }

        private void ApplyVision(bool dark)
        {
            if (!ownsVision)
            {
                originalAlpha = vision.Alpha; originalColour = vision.Color; ownsVision = true;
                vision.SetColor(Color.black);
            }
            vision.SetImmediate(dark ? 1f : 0f);
            foreach (var actor in mannequins) if (actor != null) actor.SetDark(dark);
        }

        private void Freeze()
        {
            if (mannequins == null) return;
            foreach (var actor in mannequins) if (actor != null) actor.SetDark(false);
        }

        private void ReleaseVision()
        {
            if (!ownsVision) return;
            ownsVision = false;
            if (vision != null) { vision.SetColor(originalColour); vision.SetImmediate(originalAlpha); }
        }

        public override void OnPlayerDeathAccepted() { ReleaseVision(); Freeze(); }

        protected override void OnResetAnomaly()
        {
            ReleaseVision(); rules = null;
            if (mannequins == null) return;
            foreach (var actor in mannequins) if (actor != null) actor.Restore();
        }

        private void OnDisable() => ResetAnomaly();
    }
}
