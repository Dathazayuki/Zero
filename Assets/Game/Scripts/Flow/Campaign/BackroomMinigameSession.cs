using System;
using System.Collections;
using UnityEngine;

namespace Zero.AnomalyPrototype.Flow
{
    public sealed class BackroomMinigameSession : MonoBehaviour, IMinigameSession
    {
        [SerializeField] private BackroomGameplayManager chase;
        public bool IsReady => chase != null && chase.IsReady;
        public event Action Completed;

        private void OnEnable() { if (chase != null) chase.Completed += OnCompleted; }
        private void OnDisable() { if (chase != null) chase.Completed -= OnCompleted; }
        public IEnumerator Prepare() { if (chase != null) yield return chase.PrepareNavigation(); }
        public void Begin(MinigameContext context) { chase?.ResetAttempt(); }
        public void ResetAttempt() { chase?.ResetAttempt(); }
        private void OnCompleted() { Completed?.Invoke(); }
    }
}
