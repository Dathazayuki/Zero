using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Zero.AnomalyPrototype.Player;

namespace Zero.AnomalyPrototype.Flow
{
    public enum TrueEndingPhase { Inactive, Room1, Room2, BlueSky, Ending, Finished }

    public sealed class TrueEndingGameplayController : MonoBehaviour
    {
        [SerializeField] private BackroomGameplayManager backroomChase;
        [SerializeField] private Room1DontLookBackController room1;
        [SerializeField] private Room2MovementChallenge room2;
        [SerializeField] private Transform room1Checkpoint;
        [SerializeField] private Transform room2Checkpoint;
        [SerializeField] private BlueSkyTransitionController blueSkyTransition;
        [SerializeField] private BlueSkyEndingController ending;
        [SerializeField] private GameObject blueSkyBacktrackBlocker;
        public TrueEndingPhase Phase { get; private set; }
        private FirstPersonController player;
        private PlayerInteraction interaction;
        private bool interactionWasEnabled;
        private bool ownsInput;
        private bool resetting;
        private bool room2Started;
        private bool blueSkyEntered;
        private SceneFadeController fade;
        private MinigameContext minigameContext;
        private TrueEndingTriggerRelay[] minigameTriggers;
        private bool room2TutorialPlayed;
        public bool IsMinigameConfigured => room1 != null && room1.IsConfigured && room2 != null && room2.IsConfigured &&
            room1Checkpoint != null && room2Checkpoint != null;
        public event System.Action Completed;

        public bool BeginFinalPresentation(FirstPersonController target)
        {
            if (target == null || ending == null || blueSkyTransition == null ||
                GameFlowController.Instance?.Campaign?.IsActive != true ||
                !GameFlowController.Instance.Campaign.CanPresentEndings) return false;
            if (!GameFlowController.Instance.Campaign.Progress.TryChooseEnding(EndingIdentity.CurtainCall)) return false;
            minigameContext = null;
            player = target;
            fade = GameFlowController.Instance.FadeController;
            Phase = TrueEndingPhase.BlueSky;
            blueSkyEntered = false;
            blueSkyTransition.Begin();
            return true;
        }

        public void BeginMinigame(MinigameContext context)
        {
            minigameContext = context;
            player = context.Player;
            fade = context.Fade;
            minigameTriggers = transform.root.GetComponentsInChildren<TrueEndingTriggerRelay>(true);
            room2TutorialPlayed = false;
            ResetMinigameAttempt();
        }

        public void ResetMinigameAttempt()
        {
            StopAllCoroutines();
            if (resetting && fade != null) fade.SetImmediate(0f);
            resetting = false;
            UnlockInput();
            room1?.StopChallenge();
            room2?.StopChallenge();
            Phase = TrueEndingPhase.Inactive;
            room2Started = false;
            blueSkyEntered = false;
            if (minigameTriggers != null)
                foreach (TrueEndingTriggerRelay relay in minigameTriggers) relay.Rearm();
        }

        public bool TryEnter(TrueEndingTriggerKind kind, FirstPersonController target)
        {
            if (resetting || target == null || !target.InputEnabled || (player != null && player != target)) return false;
            switch (kind)
            {
                case TrueEndingTriggerKind.Room1Start:
                    if (!StartFirstRoom(target)) return false;
                    break;
                case TrueEndingTriggerKind.Room1Complete:
                    if (Phase != TrueEndingPhase.Room1) return false;
                    room1.StopChallenge();
                    Phase = TrueEndingPhase.Room2;
                    break;
                case TrueEndingTriggerKind.Room2Start:
                    if (Phase != TrueEndingPhase.Room2 || room2Started) return false;
                    room2Started = true;
                    room2.Begin(player, room2TutorialPlayed);
                    room2TutorialPlayed = true;
                    break;
                case TrueEndingTriggerKind.Room2Complete:
                    if (Phase != TrueEndingPhase.Room2 || !room2Started) return false;
                    CompleteSecondRoom();
                    break;
                case TrueEndingTriggerKind.BlueSkyEntry:
                    if (Phase != TrueEndingPhase.BlueSky || blueSkyEntered) return false;
                    blueSkyEntered = true;
                    if (blueSkyBacktrackBlocker != null) blueSkyBacktrackBlocker.SetActive(true);
                    break;
                case TrueEndingTriggerKind.EndingPlatform:
                    if (Phase != TrueEndingPhase.BlueSky || !blueSkyEntered || !ending.IsConfigured ||
                        GameFlowController.Instance?.Campaign?.CanPresentEndings == false) return false;
                    Phase = TrueEndingPhase.Ending;
                    LockInput();
                    ending.Begin(player, this, fade);
                    break;
                default: return false;
            }
            Debug.Log($"TRUE_ENDING_PROGRESS: trigger={kind}, phase={Phase}", this);
            return true;
        }

        private bool StartFirstRoom(FirstPersonController target)
        {
            if (Phase != TrueEndingPhase.Inactive) return false;
            if (!IsMinigameConfigured || (minigameContext == null &&
                (ending == null || blueSkyTransition == null || GameFlowController.Instance == null)))
            { Debug.LogError("TRUE_ENDING_CONFIGURATION_INVALID", this); return false; }
            if (minigameContext == null && backroomChase != null && !backroomChase.TryEnterSafeEnding(target)) return false;
            player = target;
            fade = minigameContext != null ? minigameContext.Fade : GameFlowController.Instance.FadeController;
            Phase = TrueEndingPhase.Room1;
            room1.Begin(player);
            return true;
        }

        private void CompleteSecondRoom()
        {
            room2.StopChallenge();
            if (minigameContext != null)
            {
                Phase = TrueEndingPhase.Finished;
                Completed?.Invoke();
                return;
            }
            Phase = TrueEndingPhase.BlueSky;
            blueSkyTransition.Begin();
        }

        public bool RequestReset(TrueEndingPhase source)
        {
            if (resetting || Phase != source || (source != TrueEndingPhase.Room1 && source != TrueEndingPhase.Room2) || fade == null) return false;
            resetting = true;
            room1.StopChallenge();
            room2.StopChallenge();
            LockInput();
            StartCoroutine(ResetCheckpoint(source));
            return true;
        }
        private IEnumerator ResetCheckpoint(TrueEndingPhase source)
        {
            yield return fade.FadeToColor(Color.black, 1f, 0.12f);
            yield return null; // A fully opaque rendered frame before moving the character.
            Transform checkpoint = minigameContext != null ? minigameContext.Entrance :
                source == TrueEndingPhase.Room1 ? room1Checkpoint : room2Checkpoint;
            Quaternion rotation = minigameContext != null || source != TrueEndingPhase.Room1
                ? checkpoint.rotation : room1.ForwardRotation;
            player.Teleport(checkpoint.position, rotation);
            room2.Rebase();
            Physics.SyncTransforms();
            yield return fade.FadeTo(0f, 0.18f);
            UnlockInput();
            resetting = false;
            if (minigameContext != null) ResetMinigameAttempt();
            else if (source == TrueEndingPhase.Room1) room1.Begin(player);
            else room2.Begin(player, true);
            Debug.Log($"TRUE_ENDING_CHECKPOINT_RESET: room={source}", this);
        }
        private void LockInput()
        {
            if (ownsInput || player == null) return;
            interaction = player.GetComponent<PlayerInteraction>();
            interactionWasEnabled = interaction != null && interaction.InputEnabled;
            if (interaction != null) interaction.SetInputEnabled(false);
            ownsInput = true;
            player.SetCinematicLock(true);
        }
        private void UnlockInput()
        {
            if (!ownsInput) return;
            if (player != null) player.SetCinematicLock(false);
            if (interaction != null) interaction.SetInputEnabled(interactionWasEnabled);
            ownsInput = false;
        }
        public void Finish()
        {
            if (Phase != TrueEndingPhase.Ending) return;
            Phase = TrueEndingPhase.Finished;
            GameFlowController.Instance?.FinishEnding(EndingIdentity.CurtainCall);
        }
        private void OnDisable()
        {
            StopAllCoroutines();
            if (room1 != null) room1.StopChallenge();
            if (room2 != null) room2.StopChallenge();
            if (resetting && fade != null) fade.SetImmediate(0f);
            resetting = false;
            UnlockInput();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void Update()
        {
            if (Keyboard.current == null || !Keyboard.current.gKey.wasPressedThisFrame) return;
            DebugTriggerEndingCutscene();
        }

        [ContextMenu("Test Cutscene (G)")]
        public void DebugTriggerEndingCutscene()
        {
            if (GameFlowController.Instance?.Campaign?.CanPresentEndings == false) return;
            if (Phase == TrueEndingPhase.Ending || Phase == TrueEndingPhase.Finished || ending == null || !ending.IsConfigured) return;
            if (player == null) player = Object.FindFirstObjectByType<FirstPersonController>();
            if (player == null) return;
            if (fade == null && GameFlowController.Instance != null) fade = GameFlowController.Instance.FadeController;
            if (backroomChase != null) backroomChase.TryEnterSafeEnding(player);
            if (room1 != null) room1.StopChallenge();
            if (room2 != null) room2.StopChallenge();
            Phase = TrueEndingPhase.Ending;
            LockInput();
            ending.Begin(player, this, fade);
            Debug.Log("TRUE_ENDING_DEBUG_TRIGGER: Key G activated True Ending cutscene test.", this);
        }
#endif
    }
}
