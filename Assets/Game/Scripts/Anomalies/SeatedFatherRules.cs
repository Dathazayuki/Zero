using System;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class SeatedFatherRules
    {
        public FatherState State { get; private set; }
        public float Elapsed { get; private set; }
        public float WarningSeconds { get; }
        public SeatedFatherRules(float warningSeconds) => WarningSeconds = Math.Max(0.1f, warningSeconds);
        public void Reset() => Enter(FatherState.Sitting);
        public void Catch() => Enter(FatherState.Caught);
        public void Returned() => Enter(FatherState.Sitting);

        public void Tick(float delta, bool near, bool inReach)
        {
            Elapsed += Math.Max(0f, delta);
            switch (State)
            {
                case FatherState.Sitting:
                    if (near) Enter(FatherState.Warning);
                    break;
                case FatherState.Warning:
                    if (Elapsed >= WarningSeconds) Enter(inReach ? FatherState.Rising : FatherState.Returning);
                    break;
                case FatherState.Rising:
                    if (Elapsed >= 0.7f) Enter(FatherState.Lunging);
                    break;
                case FatherState.Lunging:
                    if (Elapsed >= 0.65f) Enter(FatherState.Returning);
                    break;
            }
        }

        private void Enter(FatherState state)
        {
            State = state;
            Elapsed = 0f;
        }
    }
}
