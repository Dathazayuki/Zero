using System;
using UnityEngine;

namespace Zero.AnomalyPrototype.Core
{
    public sealed class ProgressManager : MonoBehaviour
    {
        [SerializeField, Min(1)] private int requiredCorrectLoops = 5;

        public int CurrentProgress { get; private set; }
        public int RequiredCorrectLoops => requiredCorrectLoops;
        public bool HasReachedGoal => CurrentProgress >= requiredCorrectLoops;

        public event Action<int, int> ProgressChanged;

        public void Configure(int requiredLoops)
        {
            requiredCorrectLoops = Mathf.Max(1, requiredLoops);
            ResetProgress();
        }

        public void RegisterChoice(bool correct)
        {
            CurrentProgress = correct ? CurrentProgress + 1 : 0;
            ProgressChanged?.Invoke(CurrentProgress, requiredCorrectLoops);
        }

        public void ResetProgress()
        {
            CurrentProgress = 0;
            ProgressChanged?.Invoke(CurrentProgress, requiredCorrectLoops);
        }
    }
}
