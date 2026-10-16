using System;
using System.Collections;
using UnityEngine;

namespace Zero.AnomalyPrototype.Flow
{
    public sealed class RoomsMinigameSession : MonoBehaviour, IMinigameSession
    {
        [SerializeField] private TrueEndingGameplayController challenges;
        public bool IsReady => challenges != null && challenges.IsMinigameConfigured;
        public event Action Completed;
        private void OnEnable() { if (challenges != null) challenges.Completed += OnCompleted; }
        private void OnDisable() { if (challenges != null) challenges.Completed -= OnCompleted; }
        public IEnumerator Prepare() { yield break; }
        public void Begin(MinigameContext context) { challenges?.BeginMinigame(context); }
        public void ResetAttempt() { challenges?.ResetMinigameAttempt(); }
        private void OnCompleted() { Completed?.Invoke(); }
    }
}
