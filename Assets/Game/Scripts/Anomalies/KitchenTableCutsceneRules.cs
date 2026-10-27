using System;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class KitchenTableCutsceneRules
    {
        public enum Phase { Waiting, Fading, Turning, Chopping, Caught }
        private readonly float turnSeconds, impactSeconds, fadeSeconds;
        private float elapsed;
        public Phase State { get; private set; }

        public KitchenTableCutsceneRules(float turningSeconds, float knifeImpactSeconds, float captureFadeSeconds = 1f)
        {
            turnSeconds = Math.Max(0.05f, turningSeconds);
            impactSeconds = Math.Max(0.01f, knifeImpactSeconds);
            fadeSeconds = Math.Max(0.01f, captureFadeSeconds);
        }

        public void Tick(float deltaTime, bool nearby, float clipSeconds)
        {
            switch (State)
            {
                case Phase.Waiting:
                    if (nearby) { State = Phase.Fading; elapsed = 0f; }
                    break;
                case Phase.Fading:
                    elapsed += Math.Max(0f, deltaTime);
                    if (elapsed >= fadeSeconds) { State = Phase.Turning; elapsed = 0f; }
                    break;
                case Phase.Turning:
                    elapsed += Math.Max(0f, deltaTime);
                    if (elapsed >= turnSeconds) State = Phase.Chopping;
                    break;
                case Phase.Chopping:
                    if (clipSeconds >= impactSeconds) State = Phase.Caught;
                    break;
            }
        }

        public float TurnProgress => Math.Min(1f, elapsed / turnSeconds);
        public float FadeAlpha => State == Phase.Fading ? Math.Max(0f, 1f - elapsed / fadeSeconds) : 0f;
        public void Reset() { State = Phase.Waiting; elapsed = 0f; }
    }
}
