using UnityEngine;
using UnityEngine.Serialization;
using Zero.AnomalyPrototype.Player;
using Phase = Zero.AnomalyPrototype.Anomalies.CabaretRitualRules.Phase;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class CabaretEyeRitualAnomaly : AnomalyBehaviour
    {
        [SerializeField] private CharacterController player;
        [SerializeField] private PlayerDeathController death;
        [SerializeField] private BoxCollider entryZone;
        [SerializeField] private CabaretRitualLighting lighting;
        [SerializeField] private CabaretRitualSwarm swarm;
        [SerializeField] private CabaretRitualHost host;
        [SerializeField, Min(.1f)] private float blackoutSeconds = 4f;
        [SerializeField, Min(.1f)] private float riseSeconds = 1.6f;
        [FormerlySerializedAs("chargeSeconds"), SerializeField, Min(2f)] private float trackingSeconds = 6f;
        [SerializeField, Min(0f)] private float trackingVariationSeconds = 1.5f;
        [FormerlySerializedAs("outsideLightGrace"), SerializeField, Min(.8f)] private float warningSeconds = 1.25f;
        [SerializeField, Min(1.5f)] private float releaseSeconds = 2.5f;
        private CabaretRitualRules rules;
        private float swarmTime;
        public Phase State => rules != null ? rules.State : Phase.Waiting;
        public int StabilizedMask => rules != null ? rules.StabilizedMask : 0;
        public Vector3 SafeCentre => lighting != null ? lighting.SafeCentre : Vector3.zero;
        public bool IsConfigured => player != null && death != null && entryZone != null && lighting != null
            && lighting.IsConfigured && swarm != null && swarm.IsConfigured && host != null && host.IsConfigured;

        public override void CaptureNormalState()
        {
            if (rules != null || !IsConfigured) return;
            rules = new CabaretRitualRules(blackoutSeconds, riseSeconds, trackingSeconds, warningSeconds, releaseSeconds);
            rules.SetRandomSeed(Random.Range(1, int.MaxValue));
            rules.SetTrackingVariation(trackingVariationSeconds);
            lighting.Capture(); swarm.Capture(); host.Capture();
        }

        protected override void OnActivate()
        {
            CaptureNormalState();
            if (rules == null) { Debug.LogError("CABARET_RITUAL_CONFIGURATION_INVALID", this); return; }
            rules.Reset(); swarmTime = 0f;
        }

        private void LateUpdate()
        {
            if (!IsActive || rules == null || !IsConfigured || Time.deltaTime <= 0f) return;
            if (death.IsDeathInProgress) { if (death.IsScreenBlack) ResetAnomaly(); return; }
            if (State == Phase.Caught || State == Phase.Safe) return;
            Advance(Time.deltaTime);
        }

        private void Advance(float delta)
        {
            Phase before = State;
            Vector3 feet = player.transform.position;
            lighting.Track(delta, feet, State, rules.Difficulty);
            rules.Tick(delta, entryZone.bounds.Contains(feet + Vector3.up),
                lighting.ContainsStrike(feet, player.radius), lighting.HitTargets());
            if (before != State) EnterPhase();
            Present(delta);
        }

        private void EnterPhase()
        {
            switch (State)
            {
                case Phase.Blackout:
                    lighting.Begin();
                    if (!host.Begin()) { Debug.LogError("CABARET_RITUAL_ANIMATION_MISSING", this); ResetAnomaly(); }
                    break;
                case Phase.Rising: RevealEncounter(); break;
                case Phase.Safe: swarm.RestoreDetail(); lighting.Unlock(); break;
                case Phase.Caught:
                    if (!death.TryBeginFreezeDeath(this, 0f)) ResetAnomaly(); break;
            }
            host.Enter(State);
        }

        private void RevealEncounter()
        {
            swarm.HideDetail();
            lighting.Reveal(player.transform.position);
            swarm.Reveal();
        }

        private void Present(float delta)
        {
            if (State == Phase.Blackout) { lighting.Blackout(rules.PhaseProgress); return; }
            if (State == Phase.Waiting || State == Phase.Safe || State == Phase.Caught) return;
            swarmTime += delta;
            float rise = State == Phase.Rising ? rules.PhaseProgress : 1f;
            lighting.Present(State, rules.PhaseProgress);
            lighting.PresentTargets(delta, swarmTime, StabilizedMask, State, player.transform.position);
            if (State == Phase.Releasing) host.PresentRelease(rules.PhaseProgress);
            swarm.Present(swarmTime, rise, StabilizedMask, SafeCentre + Vector3.up * .12f, State == Phase.Attacking);
        }

        public override void OnPlayerDeathAccepted()
        {
            lighting.Restore(); swarm.Restore(); host.Restore();
        }

        protected override void OnResetAnomaly()
        {
            rules?.Reset(); swarmTime = 0f;
            lighting?.Restore(); swarm?.Restore(); host?.Restore();
        }

        private void OnDisable() => ResetAnomaly();
    }
}
