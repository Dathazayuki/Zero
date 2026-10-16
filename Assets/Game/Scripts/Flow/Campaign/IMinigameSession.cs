using System;
using System.Collections;

namespace Zero.AnomalyPrototype.Flow
{
    public interface IMinigameSession
    {
        bool IsReady { get; }
        IEnumerator Prepare();
        void Begin(MinigameContext context);
        void ResetAttempt();
        event Action Completed;
    }
}
