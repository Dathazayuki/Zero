using System.Collections;
using Zero.AnomalyPrototype.Player;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.UI;

namespace Zero.AnomalyPrototype.Flow
{
    [DisallowMultipleComponent]
    public sealed class OfficeSequenceController : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private FirstPersonController player;
        [SerializeField] private OfficeComputerDisplay computerDisplay;
        [SerializeField] private Transform openingControlPoint;
        [SerializeField] private GameObject timelineCamerasRoot;
        [SerializeField] private PlayableDirector openingDirector;
        [SerializeField] private PlayableDirector endingDirector;

        [Header("UI")]
        [SerializeField] private CanvasGroup objectiveGroup;
        [SerializeField] private Text objectiveText;
        [SerializeField] private CanvasGroup messageGroup;
        [SerializeField] private Text messageText;
        [SerializeField] private CanvasGroup endGroup;

        [Header("Timing")]
        [SerializeField, Min(0f)] private float openingDuration = 26f;
        [SerializeField, Min(0f)] private float endingDuration = 11f;

        [Header("Gameplay Camera Handoff")]
        [SerializeField, Min(0f)] private float gameplayCameraBlendDuration = 0.65f;
        [SerializeField] private Vector3 gameplayCameraLocalPosition = new Vector3(0f, 1.62f, 0f);
        [SerializeField, Range(1f, 179f)] private float gameplayCameraFieldOfView = 65f;

        private GameFlowController flow;
        private PlayerInteraction playerInteraction;
        private OfficeAudioController officeAudio;
        private bool endingFinished;
        private Coroutine messageRoutine;

        private void Awake()
        {
            playerInteraction = player != null ? player.GetComponent<PlayerInteraction>() : null;
            officeAudio = GetComponent<OfficeAudioController>();
            if (officeAudio == null)
            {
                officeAudio = gameObject.AddComponent<OfficeAudioController>();
            }

            player?.SetCameraRestPose(gameplayCameraLocalPosition);
            PrepareInitialShot();
        }

        public void Configure(
            FirstPersonController playerController,
            OfficeComputerDisplay display,
            Transform controlPoint,
            GameObject cameraRoot,
            PlayableDirector opening,
            PlayableDirector ending,
            CanvasGroup objective,
            Text objectiveLabel,
            CanvasGroup message,
            Text messageLabel,
            CanvasGroup end)
        {
            player = playerController;
            computerDisplay = display;
            openingControlPoint = controlPoint;
            timelineCamerasRoot = cameraRoot;
            openingDirector = opening;
            endingDirector = ending;
            objectiveGroup = objective;
            objectiveText = objectiveLabel;
            messageGroup = message;
            messageText = messageLabel;
            endGroup = end;
            PrepareInitialShot();
        }

        private IEnumerator Start()
        {
            if (playerInteraction == null && player != null)
            {
                playerInteraction = player.GetComponent<PlayerInteraction>();
            }

            if (officeAudio == null)
            {
                officeAudio = GetComponent<OfficeAudioController>();
                if (officeAudio == null)
                {
                    officeAudio = gameObject.AddComponent<OfficeAudioController>();
                }
            }

            officeAudio.BeginAmbience();
            SetGroup(objectiveGroup, false);
            SetGroup(messageGroup, false);
            SetGroup(endGroup, false);

            ResolveFlow();
            PrepareInitialShot();

            if (flow != null && flow.Phase == GameFlowPhase.Ending)
            {
                yield return EndingSequence();
            }
            else
            {
                yield return OpeningSequence();
            }
        }

        private void Update()
        {
            if (!endingFinished && messageRoutine == null && flow != null &&
                flow.Phase == GameFlowPhase.Opening && playerInteraction != null)
            {
                string prompt = playerInteraction.CurrentPrompt;
                if (messageText != null)
                {
                    messageText.text = string.IsNullOrEmpty(prompt) ? string.Empty : $"[E] {prompt}";
                }

                SetGroup(messageGroup, !string.IsNullOrEmpty(prompt));
            }

            if (!endingFinished || flow == null)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.rKey.wasPressedThisFrame)
            {
                endingFinished = false;
                flow.RestartLoop();
            }
            else if (keyboard.escapeKey.wasPressedThisFrame)
            {
                flow.ExitApplication();
            }
        }

        public void ShowTemporaryMessage(string content, float duration = 2.2f)
        {
            if (messageRoutine != null)
            {
                StopCoroutine(messageRoutine);
            }

            messageRoutine = StartCoroutine(TemporaryMessage(content, duration));
        }

        public void PlayElevatorError()
        {
            officeAudio?.PlayElevatorError();
        }

        private void ResolveFlow()
        {
            if (flow != null)
            {
                return;
            }

            flow = GameFlowController.Instance;
            if (flow == null)
            {
                flow = FindFirstObjectByType<GameFlowController>();
            }
        }

        private void PrepareInitialShot()
        {
            ResolveFlow();
            if (flow != null && flow.Phase == GameFlowPhase.Ending)
            {
                PrepareEndingShot();
            }
            else
            {
                PrepareOpeningShot();
            }
        }

        private void PrepareOpeningShot()
        {
            SetPlayerCinematicLock(true);
            endingDirector?.Stop();
            timelineCamerasRoot?.SetActive(true);
            if (openingDirector != null)
            {
                openingDirector.time = 0d;
                openingDirector.Evaluate();
                openingDirector.Pause();
            }
            computerDisplay?.ShowWorking();
        }

        private void PrepareEndingShot()
        {
            SetPlayerCinematicLock(true);
            openingDirector?.Stop();
            timelineCamerasRoot?.SetActive(true);
            if (endingDirector != null)
            {
                endingDirector.time = 0d;
                endingDirector.Evaluate();
                endingDirector.Pause();
            }
            computerDisplay?.ShowUnfinished();
        }

        private bool IsFadeComplete()
        {
            if (flow == null || flow.FadeController == null)
            {
                return true;
            }

            return flow.FadeController.Alpha <= 0.001f;
        }

        private IEnumerator OpeningSequence()
        {
            Debug.Log("OFFICE_OPENING_STARTED", this);
            PrepareOpeningShot();

            yield return new WaitUntil(() => flow == null || flow.IsOpeningPresentationReady || IsFadeComplete());

            officeAudio?.PlayTyping();
            openingDirector?.Play();
            float sequenceDuration = ResolveOpeningDuration();

            yield return new WaitForSecondsRealtime(Mathf.Min(10f, sequenceDuration));
            computerDisplay?.ShowCompleted();
            officeAudio?.PlayConfirmation();
            ShowTemporaryMessage("Tin nhắn: Tôi về trước nhé. Nhớ khóa cửa tầng mình.", 6f);

            float untilChair = Mathf.Max(0f, Mathf.Min(21f, sequenceDuration) - 10f);
            if (untilChair > 0f)
            {
                yield return new WaitForSecondsRealtime(untilChair);
            }

            officeAudio?.PlayChair();
            float afterChair = Mathf.Max(0f, sequenceDuration - 21f);
            if (afterChair > 0f)
            {
                yield return new WaitForSecondsRealtime(afterChair);
            }

            BeginLoopGameplayPreload();
            yield return BlendToGameplayCamera();
            SetPlayerCinematicLock(false);
            if (objectiveText != null)
            {
                objectiveText.text = string.Empty;
            }

            Debug.Log("OFFICE_OPENING_CONTROL_GRANTED", this);
        }

        private void BeginLoopGameplayPreload()
        {
            if (flow == null || !flow.BeginLoopGameplayPreload())
            {
                Debug.LogWarning("OFFICE_OPENING_PRELOAD_REJECTED", this);
                return;
            }

            Debug.Log("OFFICE_OPENING_PRELOAD_REQUESTED", this);
        }

        private float ResolveOpeningDuration()
        {
            if (openingDirector == null || openingDirector.playableAsset == null)
            {
                return openingDuration;
            }

            double directorDuration = openingDirector.duration;
            if (double.IsNaN(directorDuration) || double.IsInfinity(directorDuration) ||
                directorDuration <= 0d)
            {
                return openingDuration;
            }

            return (float)directorDuration;
        }

        private IEnumerator BlendToGameplayCamera()
        {
            Camera gameplayCamera = player != null ? player.PlayerCamera : null;
            if (gameplayCamera == null || openingControlPoint == null)
            {
                openingDirector?.Stop();
                timelineCamerasRoot?.SetActive(false);

                if (player != null && openingControlPoint != null)
                {
                    player.SetCameraRestPose(gameplayCameraLocalPosition);
                    player.Teleport(openingControlPoint.position, openingControlPoint.rotation);
                }

                Debug.LogWarning(
                    $"OFFICE_CAMERA_HANDOFF_FALLBACK: camera={gameplayCamera != null}, " +
                    $"controlPoint={openingControlPoint != null}",
                    this);
                yield break;
            }

            Vector3 cinematicPosition = gameplayCamera.transform.position;
            Quaternion cinematicRotation = gameplayCamera.transform.rotation;
            float cinematicFieldOfView = gameplayCamera.fieldOfView;

            CinemachineBrain brain = gameplayCamera.GetComponent<CinemachineBrain>();
            bool brainWasEnabled = brain != null && brain.enabled;

            openingDirector?.Stop();
            timelineCamerasRoot?.SetActive(false);

            // Prevent a final Cinemachine LateUpdate from overwriting the manual handoff.
            if (brain != null)
            {
                brain.enabled = false;
            }

            yield return null;

            player.SetCameraRestPose(gameplayCameraLocalPosition);
            player.Teleport(openingControlPoint.position, openingControlPoint.rotation);
            gameplayCamera.transform.localPosition = gameplayCameraLocalPosition;
            gameplayCamera.transform.localRotation = Quaternion.identity;
            gameplayCamera.fieldOfView = gameplayCameraFieldOfView;

            Vector3 gameplayPosition = gameplayCamera.transform.position;
            Quaternion gameplayRotation = gameplayCamera.transform.rotation;

            gameplayCamera.transform.SetPositionAndRotation(cinematicPosition, cinematicRotation);
            gameplayCamera.fieldOfView = cinematicFieldOfView;

            float duration = Mathf.Max(0f, gameplayCameraBlendDuration);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float normalizedTime = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
                float easedTime = Mathf.SmoothStep(0f, 1f, normalizedTime);

                gameplayCamera.transform.position = Vector3.Lerp(
                    cinematicPosition,
                    gameplayPosition,
                    easedTime);
                gameplayCamera.transform.rotation = Quaternion.Slerp(
                    cinematicRotation,
                    gameplayRotation,
                    easedTime);
                gameplayCamera.fieldOfView = Mathf.Lerp(
                    cinematicFieldOfView,
                    gameplayCameraFieldOfView,
                    easedTime);

                yield return null;
            }

            gameplayCamera.transform.localPosition = gameplayCameraLocalPosition;
            gameplayCamera.transform.localRotation = Quaternion.identity;
            gameplayCamera.fieldOfView = gameplayCameraFieldOfView;

            if (brain != null)
            {
                brain.enabled = brainWasEnabled;
            }

            Debug.Log(
                $"OFFICE_CAMERA_HANDOFF_COMPLETE: duration={duration:0.###}",
                this);
        }

        private IEnumerator EndingSequence()
        {
            Debug.Log("OFFICE_ENDING_STARTED", this);
            PrepareEndingShot();

            yield return new WaitUntil(() => flow == null || flow.IsEndingPresentationReady || IsFadeComplete());

            endingDirector?.Play();

            yield return new WaitForSecondsRealtime(Mathf.Min(4f, endingDuration));
            ShowTemporaryMessage("Tin nhắn: Tôi về trước nhé. Nhớ khóa cửa tầng mình.", 5f);

            float beforeTyping = Mathf.Max(0f, Mathf.Min(8f, endingDuration) - 4f);
            if (beforeTyping > 0f)
            {
                yield return new WaitForSecondsRealtime(beforeTyping);
            }

            computerDisplay?.ShowLoopRestarting();
            officeAudio?.PlayTyping();
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, endingDuration - 8f));

            flow?.FadeToBlack(0.75f);
            yield return new WaitForSecondsRealtime(0.8f);
            endingDirector?.Stop();
            timelineCamerasRoot?.SetActive(false);
            SetGroup(messageGroup, false);
            SetGroup(endGroup, true);
            endingFinished = true;
            flow?.FinishEnding();
            Debug.Log("OFFICE_ENDING_COMPLETE", this);
        }

        private IEnumerator TemporaryMessage(string content, float duration)
        {
            if (messageText != null)
            {
                messageText.text = content;
            }

            SetGroup(messageGroup, true);
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, duration));
            SetGroup(messageGroup, false);
            messageRoutine = null;
        }

        private static void SetGroup(CanvasGroup group, bool visible)
        {
            if (group == null)
            {
                return;
            }

            group.alpha = visible ? 1f : 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
        }

        private void SetPlayerCinematicLock(bool locked)
        {
            player?.SetCinematicLock(locked);
            playerInteraction?.SetInputEnabled(!locked);
        }
    }
}
