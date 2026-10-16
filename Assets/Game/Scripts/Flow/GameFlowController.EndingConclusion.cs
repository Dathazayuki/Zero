using System.Collections;
using UnityEngine;
using Zero.EnvironmentLoop;
using Zero.AnomalyPrototype.Player;

namespace Zero.AnomalyPrototype.Flow
{
    public sealed partial class GameFlowController : MonoBehaviour
    {
        // Compatibility for older scene callbacks; explicit routes use the typed overload.
        public void FinishEnding()
        {
            FinishEnding(EndingIdentity.AnotherShift);
        }

        public void FinishEnding(EndingIdentity ending)
        {
            if (Campaign?.CanPresentEndings == false) return;
            if (Campaign?.IsActive == true && Campaign.Progress.SelectedEnding.HasValue &&
                Campaign.Progress.SelectedEnding.Value != ending) return;
            if (Phase == GameFlowPhase.Finished) return;
            if (Phase != GameFlowPhase.Ending && Phase != GameFlowPhase.BackroomGameplay)
            {
                Debug.LogWarning($"GAME_FLOW_ENDING_REJECTED: phase={Phase}, ending={ending}", this);
                return;
            }

            EndingTitlePresenter presenter = GetComponent<EndingTitlePresenter>();
            if (presenter == null) presenter = gameObject.AddComponent<EndingTitlePresenter>();
            SetPhase(GameFlowPhase.Finished);
            checkpoints?.FinishRun();
            if (!presenter.Show(ending, this))
                Debug.LogError($"ENDING_TITLE_PRESENTATION_FAILED: ending={ending}", this);
            Debug.Log($"GAME_FLOW_FINISHED: ending={ending}", this);
        }

        public bool ReturnToTitleAfterEnding()
        {
            if (Phase != GameFlowPhase.Finished || IsTransitioning) return false;
            if (StartSceneTransition(GameFlowPhase.Title, titleSceneName)) return true;
            Debug.LogError("GAME_FLOW_TITLE_UNAVAILABLE: falling back to Office opening", this);
            return StartSceneTransition(GameFlowPhase.Opening, officeSceneName);
        }

        public bool ReturnToTitleFromPause()
        {
            return !IsTransitioning && Phase != GameFlowPhase.Title &&
                StartSceneTransition(GameFlowPhase.Title, titleSceneName);
        }

        public bool BeginEnding() => BeginEndingFromEndDoor();

        public bool BeginCampaignEnding(EndingIdentity ending)
        {
            if (Campaign?.IsActive != true || !Campaign.CanPresentEndings || IsTransitioning) return false;
            if (!Campaign.Progress.TryChooseEnding(ending)) return false;
            bool accepted = ending == EndingIdentity.AnotherShift
                ? StartSceneTransition(GameFlowPhase.Ending, officeSceneName)
                : ending == EndingIdentity.TheCast && BeginEndingFromEndDoor();
            if (!accepted) Campaign.Progress.ReleaseEnding(ending);
            return accepted;
        }

        public bool BeginEndingFromEndDoor()
        {
            if (Campaign?.CanPresentEndings == false) return false;
            if (IsTransitioning || endingTransitionActive || !CanStartEndingPresentation() || fadeController == null)
                return false;

            endingOneCutscene = FindFirstObjectByType<EndingOneCutsceneController>(FindObjectsInactive.Include);
            if (endingOneCutscene == null || !endingOneCutscene.ReserveEnding())
            {
                Debug.LogError("ENDING_I_CUTSCENE_NOT_READY", this);
                return false;
            }
            IsEndingPresentationReady = false;
            endingTransitionActive = true;
            SetLoopPlayerInput(FindFirstObjectByType<FirstPersonController>(), false);
            transitionRoutine = StartCoroutine(PlayEndingOneCutscene());
            return true;
        }

        private bool CanStartEndingPresentation() => Phase == GameFlowPhase.LoopGameplay ||
            (Phase == GameFlowPhase.BackroomGameplay && Campaign?.CanPresentEndings == true);

        public void ResetEndingForRun()
        {
            CancelBackroomTransition();
            if (endingOneCutscene != null)
            {
                if (transitionRoutine != null) StopCoroutine(transitionRoutine);
                transitionRoutine = null;
                endingOneCutscene.CancelEnding();
                endingOneCutscene = null;
                fadeController.SetImmediate(0f);
                fadeController.SetColor(Color.black);
                SetPhase(GameFlowPhase.LoopGameplay);
                SetLoopPlayerInput(FindFirstObjectByType<FirstPersonController>(), true);
            }
            if (Phase != GameFlowPhase.LoopGameplay) return;

            if (endingTransitionActive && transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
                transitionRoutine = null;
                fadeController.SetImmediate(0f);
                fadeController.SetColor(Color.black);
                SetLoopPlayerInput(FindFirstObjectByType<FirstPersonController>(), true);
            }

            endingTransitionActive = false;
            IsEndingPresentationReady = false;
        }

        public bool BeginEndingPreload() => Phase == GameFlowPhase.LoopGameplay && !IsTransitioning;

        private IEnumerator PlayEndingOneCutscene()
        {
            IsEndingPresentationReady = false;
            Debug.Log("ENDING_WHITEOUT_STARTED", this);
            yield return fadeController.FadeToColor(Color.white, 1f, endingWhiteFadeDuration);
            yield return null;
            var loop = FindFirstObjectByType<StairLoopManager>();
            while (loop != null && loop.IsEndingCleanupPending) yield return null;
            yield return new WaitForSecondsRealtime(endingWhiteHoldDuration);
            yield return fadeController.TransitionOpaqueColor(Color.white, Color.black, endingWhiteToBlackDuration);
            Debug.Log("ENDING_BLACKOUT", this);

            yield return new WaitForSecondsRealtime(endingBlackHoldDuration);
            if (endingOneCutscene == null || !endingOneCutscene.PlayEnding())
            {
                Debug.LogError("ENDING_I_PLAYBACK_FAILED", this);
                endingOneCutscene?.CancelEnding();
                endingTransitionActive = false;
                transitionRoutine = null;
                fadeController.SetImmediate(0f);
                SetLoopPlayerInput(FindFirstObjectByType<FirstPersonController>(), true);
                yield break;
            }

            SetPhase(GameFlowPhase.Ending);
            Debug.Log("ENDING_I_CUTSCENE_STARTED_UNDER_BLACKOUT", this);
            if (endingOneCoveredLeadInDuration > 0f)
                yield return new WaitForSecondsRealtime(endingOneCoveredLeadInDuration);
            yield return fadeController.FadeFromBlackGradual(endingOneFadeInDuration);
            IsEndingPresentationReady = true;
            Debug.Log("ENDING_PRESENTATION_READY", this);
            while (endingOneCutscene != null && endingOneCutscene.IsRunning) yield return null;
            endingTransitionActive = false;
            transitionRoutine = null;
            FinishEnding(EndingIdentity.TheCast);
        }
    }
}
