using System;
using UnityEngine;

namespace Zero.EnvironmentLoop
{
    public sealed class FloorProgressManager : MonoBehaviour
    {
        [SerializeField] private int initialLogicalFloor;

        public FloorRuntimeState CurrentState { get; private set; }
        public int LogicalFloorIndex => CurrentState?.LogicalFloorIndex ?? initialLogicalFloor;
        public TravelDirection LastTravelDirection { get; private set; }
        public bool HasTravelDirection { get; private set; }
        public int CommitCount { get; private set; }

        public event Action<int, TravelDirection> FloorChanged;

        public void Configure(int startingFloor = 0)
        {
            initialLogicalFloor = startingFloor;
        }

        public void Initialize()
        {
            CurrentState = new FloorRuntimeState(initialLogicalFloor);
            HasTravelDirection = false;
            CommitCount = 0;
        }

        public void Commit(TravelDirection direction)
        {
            if (CurrentState == null)
            {
                Initialize();
            }

            CurrentState.LogicalFloorIndex += direction == TravelDirection.Up ? 1 : -1;
            LastTravelDirection = direction;
            HasTravelDirection = true;
            CommitCount++;
            FloorChanged?.Invoke(CurrentState.LogicalFloorIndex, direction);
        }
    }
}
