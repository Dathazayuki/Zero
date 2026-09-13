using System;
using Zero.AnomalyPrototype.Anomalies;
using Zero.AnomalyPrototype.Core;
using Zero.AnomalyPrototype.HorrorEvents;
using Zero.AnomalyPrototype.Player;
using Zero.AnomalyPrototype.Utilities;
using UnityEngine;

namespace Zero.AnomalyPrototype.Loop
{
    public sealed class LoopManager : MonoBehaviour
    {
        private ProgressManager progressManager;
        private AnomalyManager anomalyManager;
        private HorrorEventManager horrorEventManager;
        private NormalStateRegistry normalStateRegistry;
        private FirstPersonController player;
        private Transform loopStart;
        private GameObject decisionRoot;
        private GameObject exitRoot;
        private DecisionTrigger[] decisionTriggers;
        private PlayerTrigger horrorTrigger;
        private PlayerTrigger exitTrigger;
        private bool processingChoice;

        public LoopState CurrentState { get; private set; }
        public LoopResult LastResult { get; private set; } = LoopResult.Pending;

        public event Action<LoopState> LoopPrepared;
        public event Action<LoopChoice, bool> ChoiceResolved;
        public event Action Escaped;

        public void Initialize(
            ProgressManager progress,
            AnomalyManager anomalies,
            HorrorEventManager horrorEvents,
            NormalStateRegistry normalStates,
            FirstPersonController playerController,
            Transform startPoint,
            GameObject decisions,
            GameObject exitPresentation,
            DecisionTrigger[] choices,
            PlayerTrigger horrorEventTrigger,
            PlayerTrigger escapeTrigger)
        {
            progressManager = progress;
            anomalyManager = anomalies;
            horrorEventManager = horrorEvents;
            normalStateRegistry = normalStates;
            player = playerController;
            loopStart = startPoint;
            decisionRoot = decisions;
            exitRoot = exitPresentation;
            decisionTriggers = choices;
            horrorTrigger = horrorEventTrigger;
            exitTrigger = escapeTrigger;

            if (horrorTrigger != null)
            {
                horrorTrigger.Entered += HandleHorrorTrigger;
            }

            if (exitTrigger != null)
            {
                exitTrigger.Entered += HandleExitTrigger;
            }
        }

        public void BeginGame()
        {
            CurrentState = null;
            LastResult = LoopResult.Pending;
            PrepareNextLoop();
        }

        public void SubmitChoice(LoopChoice choice)
        {
            if (processingChoice || CurrentState == null || CurrentState.IsExitLoop)
            {
                return;
            }

            processingChoice = true;
            bool correct = (choice == LoopChoice.Anomaly) == CurrentState.HasAnomaly;
            LastResult = correct ? LoopResult.Correct : LoopResult.Incorrect;
            CurrentState.Result = LastResult;
            progressManager.RegisterChoice(correct);
            ChoiceResolved?.Invoke(choice, correct);
            PrepareNextLoop();
        }

        public void ForceNormalNextLoop()
        {
            anomalyManager?.ForceNormalNextLoop();
        }

        public void ForceAnomalyNextLoop()
        {
            anomalyManager?.ForceAnomalyNextLoop();
        }

        private void PrepareNextLoop()
        {
            processingChoice = true;

            anomalyManager.ResetCurrent();
            horrorEventManager.ResetCurrent();
            normalStateRegistry.RestoreAll();
            ResetTriggers();
            TeleportPlayerToStart();

            int nextLoopNumber = CurrentState == null ? 1 : CurrentState.LoopNumber + 1;
            if (progressManager.HasReachedGoal)
            {
                CurrentState = new LoopState
                {
                    LoopNumber = nextLoopNumber,
                    IsExitLoop = true,
                    Difficulty = 3,
                    ActiveAnomalyName = "Exit",
                    HorrorEventName = "None",
                    Result = LoopResult.Pending
                };
                SetPresentation(showExit: true);
                processingChoice = false;
                LoopPrepared?.Invoke(CurrentState);
                return;
            }

            SetPresentation(showExit: false);
            int difficulty = Mathf.Clamp(1 + progressManager.CurrentProgress / 2, 1, 3);
            AnomalyBehaviour anomaly = anomalyManager.SelectAndActivate(difficulty);
            HorrorEventBehaviour horrorEvent = horrorEventManager.SelectAndArm();
            CurrentState = new LoopState
            {
                LoopNumber = nextLoopNumber,
                HasAnomaly = anomaly != null,
                ActiveAnomalyName = anomaly != null ? anomaly.Data.DisplayName : "None",
                HorrorEventName = horrorEvent != null ? horrorEvent.DisplayName : "None",
                Difficulty = difficulty,
                IsExitLoop = false,
                Result = LoopResult.Pending
            };

            processingChoice = false;
            LoopPrepared?.Invoke(CurrentState);
        }

        private void ResetTriggers()
        {
            if (decisionTriggers != null)
            {
                for (int i = 0; i < decisionTriggers.Length; i++)
                {
                    decisionTriggers[i]?.ResetTrigger();
                }
            }

            horrorTrigger?.ResetTrigger();
            exitTrigger?.ResetTrigger();
        }

        private void TeleportPlayerToStart()
        {
            if (player != null && loopStart != null)
            {
                player.Teleport(loopStart.position, loopStart.rotation);
            }
        }

        private void SetPresentation(bool showExit)
        {
            if (decisionRoot != null)
            {
                decisionRoot.SetActive(!showExit);
            }

            if (exitRoot != null)
            {
                exitRoot.SetActive(showExit);
            }
        }

        private void HandleHorrorTrigger(FirstPersonController _)
        {
            horrorEventManager.TryPlayCurrent();
        }

        private void HandleExitTrigger(FirstPersonController _)
        {
            if (CurrentState == null || !CurrentState.IsExitLoop)
            {
                return;
            }

            CurrentState.Result = LoopResult.Escaped;
            LastResult = LoopResult.Escaped;
            player.SetInputEnabled(false);
            Escaped?.Invoke();
        }

        private void OnDestroy()
        {
            if (horrorTrigger != null)
            {
                horrorTrigger.Entered -= HandleHorrorTrigger;
            }

            if (exitTrigger != null)
            {
                exitTrigger.Entered -= HandleExitTrigger;
            }
        }
    }
}
