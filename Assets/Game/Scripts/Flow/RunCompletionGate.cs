namespace Zero.AnomalyPrototype.Flow
{
    public sealed class RunCompletionGate
    {
        public bool IsCompleted { get; private set; }

        public bool TryComplete(int displayedFloorNumber, int requiredFloorNumber = 8)
        {
            if (IsCompleted || displayedFloorNumber < requiredFloorNumber)
            {
                return false;
            }

            IsCompleted = true;
            return true;
        }

        public void Reset()
        {
            IsCompleted = false;
        }
    }
}
