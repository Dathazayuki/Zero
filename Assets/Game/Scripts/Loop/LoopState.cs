namespace Zero.AnomalyPrototype.Loop
{
    public enum LoopChoice
    {
        Normal,
        Anomaly
    }

    public enum LoopResult
    {
        Pending,
        Correct,
        Incorrect,
        Escaped
    }

    public sealed class LoopState
    {
        public int LoopNumber { get; internal set; }
        public bool HasAnomaly { get; internal set; }
        public string ActiveAnomalyName { get; internal set; } = "None";
        public string HorrorEventName { get; internal set; } = "None";
        public int Difficulty { get; internal set; }
        public bool IsExitLoop { get; internal set; }
        public LoopResult Result { get; internal set; } = LoopResult.Pending;
    }
}
