using System;
using Zero.AnomalyPrototype.Loop;
using UnityEngine;

namespace Zero.AnomalyPrototype.Core
{
    public sealed class GameManager : MonoBehaviour
    {
        [SerializeField] private LoopManager loopManager;

        public bool IsEscaped { get; private set; }
        public event Action GameEscaped;

        public void Initialize(LoopManager loops)
        {
            loopManager = loops;
            if (loopManager != null)
            {
                loopManager.Escaped += HandleEscaped;
            }
        }

        public void StartGame()
        {
            IsEscaped = false;
            loopManager?.BeginGame();
        }

        private void HandleEscaped()
        {
            IsEscaped = true;
            GameEscaped?.Invoke();
        }

        private void OnDestroy()
        {
            if (loopManager != null)
            {
                loopManager.Escaped -= HandleEscaped;
            }
        }
    }
}
