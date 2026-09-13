using System;

namespace Zero.EnvironmentLoop
{
    [Serializable]
    public sealed class FloorRuntimeState
    {
        public int LogicalFloorIndex;

        public FloorRuntimeState(int logicalFloorIndex)
        {
            LogicalFloorIndex = logicalFloorIndex;
        }
    }
}
