using System;
using System.Collections;
using Zero.AnomalyPrototype.Player;
using Zero.EnvironmentLoop;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace Zero.AnomalyPrototype.Flow
{
    [DefaultExecutionOrder(-2000)]
    [DisallowMultipleComponent]
    public sealed partial class GameFlowController : MonoBehaviour
    {
        public const string DefaultOfficeSceneName = "OfficeLoopScene";
        public const string DefaultLoopSceneName = "InfiniteStairwayScene";
        public const string DefaultTitleSceneName = "TitleScene";

        [SerializeField] private string titleSceneName = DefaultTitleSceneName;
        [SerializeField] private string officeSceneName = DefaultOfficeSceneName;
        [SerializeField] private string loopSceneName = DefaultLoopSceneName;
        [SerializeField, Min(0f)] private float fadeOutDuration = 1.5f;
        [SerializeField, Min(0f)] private float fadeInDuration = 1.5f;
        [SerializeField, Min(0f)] private float endingWhiteFadeDuration = 0.8f;
        [SerializeField, Min(0f)] private float endingWhiteHoldDuration = 0.35f;
        [SerializeField, Min(0f)] private float endingWhiteToBlackDuration = 0.6f;
        [SerializeField, Min(0f)] private float endingBlackHoldDuration = 2f;
        [FormerlySerializedAs("ending2CoveredLeadInDuration"), SerializeField, Min(0f)]
        private float endingOneCoveredLeadInDuration = 0.2f;
        [FormerlySerializedAs("ending2FadeInDuration"), SerializeField, Min(0.1f)]
        private float endingOneFadeInDuration = 3.5f;
        private EndingOneCutsceneController endingOneCutscene;
        [SerializeField, Range(0, 5)] private int hiddenCutSettleFrames = 2;
        [SerializeField, Min(0f)] private float hiddenCutRevealMinimumDuration = 0f;
        [SerializeField, Min(0f)] private float hiddenCutRevealDistance = 8f;

        private SceneFadeController fadeController;
        private CheckpointRuntime checkpoints;
        private GameSettingsService settings;
        private DirectionalAnomalyGameManager boundAnomalyManager;
        private Coroutine transitionRoutine;
        private AsyncOperation preloadedLoopOperation;
        private bool endingTransitionActive;
        [SerializeField] private bool deferEndingLoadUntilWhiteout;
        public bool DeferEndingLoadUntilWhiteout => deferEndingLoadUntilWhiteout;
        private int lastHandledSceneHandle = -1;
        private bool hiddenCutCameraFadeActive;

        public static GameFlowController Instance { get; private set; }
        public GameFlowPhase Phase { get; private set; } = GameFlowPhase.Opening;
        public bool IsTransitioning => transitionRoutine != null || backroomRoutine != null || Campaign?.IsTravelling == true;
        public bool HasLoopPreload => preloadedLoopOperation != null;
        public bool IsLoopPreloading => preloadedLoopOperation != null && !preloadedLoopOperation.isDone;
        public bool IsLoopPreloadReady => preloadedLoopOperation != null && preloadedLoopOperation.progress >= 0.9f;
        public bool IsEndingPresentationReady { get; private set; }
        public bool IsOpeningPresentationReady { get; private set; }
        public SceneFadeController FadeController => fadeController;
        public string LoopSceneName => loopSceneName;
        public CheckpointRuntime Checkpoints => checkpoints;
        public GameSettingsService Settings => settings;
        public AudioPlaybackService Audio { get; private set; }
        public CampaignRuntime Campaign { get; private set; }

        public event Action<GameFlowPhase> PhaseChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null || FindFirstObjectByType<GameFlowController>() != null)
            {
                return;
            }

            GameObject root = new GameObject(nameof(GameFlowController));
            root.AddComponent<GameFlowController>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Instance.CopySettingsFrom(this);
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Campaign = GetComponent<CampaignRuntime>();
            if (Campaign == null) Campaign = gameObject.AddComponent<CampaignRuntime>();
            fadeController = GetComponent<SceneFadeController>();
            if (fadeController == null)
            {
                fadeController = gameObject.AddComponent<SceneFadeController>();
            }

            checkpoints = GetComponent<CheckpointRuntime>();
            if (checkpoints == null) checkpoints = gameObject.AddComponent<CheckpointRuntime>();
            settings = GetComponent<GameSettingsService>();
            if (settings == null) settings = gameObject.AddComponent<GameSettingsService>();
            Audio = gameObject.AddComponent<GameAudioBootstrap>().Initialize(settings);
            gameObject.AddComponent<GameplayPauseController>().Initialize(this);

            string activeScene = SceneManager.GetActiveScene().name;
            if (activeScene == titleSceneName)
            {
                Phase = GameFlowPhase.Title;
            }
            else if (activeScene == backroomSceneName)
            {
                Phase = GameFlowPhase.BackroomGameplay;
            }
            else if (!string.IsNullOrEmpty(activeScene) && activeScene != officeSceneName)
            {
                Phase = GameFlowPhase.LoopGameplay;
            }

            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void Start()
        {
            HandleSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        public void CopySettingsFrom(GameFlowController source)
        {
            if (source == null)
            {
                return;
            }

            officeSceneName = source.officeSceneName;
            titleSceneName = source.titleSceneName;
            loopSceneName = source.loopSceneName;
            backroomSceneName = source.backroomSceneName;
            fadeOutDuration = source.fadeOutDuration;
            fadeInDuration = source.fadeInDuration;
            endingWhiteFadeDuration = source.endingWhiteFadeDuration;
            endingWhiteHoldDuration = source.endingWhiteHoldDuration;
            endingWhiteToBlackDuration = source.endingWhiteToBlackDuration;
            endingBlackHoldDuration = source.endingBlackHoldDuration;
            endingOneCoveredLeadInDuration = source.endingOneCoveredLeadInDuration;
            endingOneFadeInDuration = source.endingOneFadeInDuration;
            deferEndingLoadUntilWhiteout = source.deferEndingLoadUntilWhiteout;
            hiddenCutSettleFrames = source.hiddenCutSettleFrames;
            hiddenCutRevealMinimumDuration = source.hiddenCutRevealMinimumDuration;
            hiddenCutRevealDistance = source.hiddenCutRevealDistance;
            CampaignRuntime authoredCampaign = source.GetComponent<CampaignRuntime>();
            if (authoredCampaign?.Definition != null && !Campaign.IsActive)
                Campaign.Configure(authoredCampaign.Definition);
            Debug.Log($"GAME_FLOW_SETTINGS_SYNCED: revealDistance={hiddenCutRevealDistance}, revealMinDuration={hiddenCutRevealMinimumDuration}");
        }

        public bool StartNewGame()
        {
            if (IsTransitioning || !Application.CanStreamedLevelBeLoaded(officeSceneName) ||
                (checkpoints != null && !checkpoints.BeginNewGame()))
            {
                return false;
            }

            IsOpeningPresentationReady = false;
            Campaign.BeginNewGame();
            return StartSceneTransition(GameFlowPhase.Opening, officeSceneName);
        }

        public bool ContinueFromTitle()
        {
            if (Phase != GameFlowPhase.Title || IsTransitioning || checkpoints == null ||
                !checkpoints.PrepareContinue()) return false;
            if (StartSceneTransition(GameFlowPhase.LoopGameplay, loopSceneName)) return true;
            checkpoints.CancelPendingContinue();
            return false;
        }

        public bool EnterLoopGameplay()
        {
            if (IsTransitioning || Phase != GameFlowPhase.Opening)
            {
                return false;
            }

            if (preloadedLoopOperation != null)
            {
                checkpoints?.EnableAutosave();
                transitionRoutine = StartCoroutine(ActivatePreloadedLoopScene());
                return true;
            }

            bool started = StartSceneTransition(GameFlowPhase.LoopGameplay, loopSceneName);
            if (started) checkpoints?.EnableAutosave();
            return started;
        }

        public bool BeginLoopGameplayPreload()
        {
            if (IsTransitioning || Phase != GameFlowPhase.Opening)
            {
                return false;
            }

            if (preloadedLoopOperation != null)
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(loopSceneName) ||
                !Application.CanStreamedLevelBeLoaded(loopSceneName))
            {
                Debug.LogError($"GAME_FLOW_SCENE_MISSING: {loopSceneName}", this);
                return false;
            }

            preloadedLoopOperation = SceneManager.LoadSceneAsync(loopSceneName, LoadSceneMode.Single);
            if (preloadedLoopOperation == null)
            {
                Debug.LogError($"GAME_FLOW_PRELOAD_FAILED: {loopSceneName}", this);
                return false;
            }

            preloadedLoopOperation.allowSceneActivation = false;
            Debug.Log($"GAME_FLOW_PRELOAD_STARTED: scene={loopSceneName}", this);
            return true;
        }



        public bool RestartLoop()
        {
            return StartNewGame();
        }

        public void FadeToBlack(float duration = 0.75f)
        {
            if (fadeController != null)
            {
                fadeController.SetColor(Color.black);
                StartCoroutine(fadeController.FadeTo(1f, duration));
            }
        }

        public bool BeginHiddenCutCameraFade()
        {
            if (Phase != GameFlowPhase.Opening || fadeController == null)
            {
                return false;
            }

            fadeController.SetImmediate(0f);
            hiddenCutCameraFadeActive = true;
            Debug.Log("GAME_FLOW_HIDDEN_CUT_CAMERA_FADE_STARTED", this);
            return true;
        }

        public void SetHiddenCutCameraFade(float darkness)
        {
            if (!hiddenCutCameraFadeActive || fadeController == null)
            {
                return;
            }

            fadeController.SetImmediate(Mathf.Clamp01(darkness));
        }

        public void CancelHiddenCutCameraFade()
        {
            if (!hiddenCutCameraFadeActive)
            {
                return;
            }

            hiddenCutCameraFadeActive = false;
            fadeController?.SetImmediate(0f);
            Debug.Log("GAME_FLOW_HIDDEN_CUT_CAMERA_FADE_CANCELLED", this);
        }

        public void ExitApplication()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private bool StartSceneTransition(GameFlowPhase targetPhase, string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"GAME_FLOW_SCENE_MISSING: {sceneName}", this);
                return false;
            }

            transitionRoutine = StartCoroutine(TransitionScene(targetPhase, sceneName));
            return true;
        }

        private IEnumerator TransitionScene(GameFlowPhase targetPhase, string sceneName)
        {
            Debug.Log($"GAME_FLOW_TRANSITION_STARTED: {Phase} -> {targetPhase}", this);
            fadeController.SetColor(Color.black);
            yield return fadeController.FadeTo(1f, fadeOutDuration);

            UnbindLoopCompletion();
            SetPhase(targetPhase);
            IsEndingPresentationReady = false;
            IsOpeningPresentationReady = false;
            if (preloadedLoopOperation != null)
            {
                preloadedLoopOperation.allowSceneActivation = true;
                yield return preloadedLoopOperation;
                preloadedLoopOperation = null;
            }
            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (loadOperation == null)
            {
                Debug.LogError($"GAME_FLOW_LOAD_FAILED: {sceneName}", this);
                transitionRoutine = null;
                yield break;
            }

            while (!loadOperation.isDone)
            {
                yield return null;
            }

            if (targetPhase == GameFlowPhase.LoopGameplay)
            {
                yield return RestoreLoopCheckpointIfNeeded();
                targetPhase = Phase;
            }

            yield return null;
            yield return fadeController.FadeTo(0f, fadeInDuration);
            if (targetPhase == GameFlowPhase.LoopGameplay || targetPhase == GameFlowPhase.BackroomGameplay)
                checkpoints?.ReleaseRestoredPlayer();
            if (targetPhase == GameFlowPhase.Ending)
            {
                IsEndingPresentationReady = true;
            }
            else if (targetPhase == GameFlowPhase.Opening)
            {
                IsOpeningPresentationReady = true;
            }
            transitionRoutine = null;
            Debug.Log($"GAME_FLOW_TRANSITION_COMPLETE: phase={Phase}, scene={sceneName}", this);
        }

        private IEnumerator ActivatePreloadedLoopScene()
        {
            AsyncOperation loadOperation = preloadedLoopOperation;
            if (loadOperation == null)
            {
                transitionRoutine = null;
                yield break;
            }

            while (loadOperation.progress < 0.9f)
            {
                yield return null;
            }

            Debug.Log("GAME_FLOW_DARK_STAIR_ACTIVATION_STARTED", this);
            fadeController.SetImmediate(1f);
            hiddenCutCameraFadeActive = true;

            UnbindLoopCompletion();
            SetPhase(GameFlowPhase.LoopGameplay);
            loadOperation.allowSceneActivation = true;
            while (!loadOperation.isDone)
            {
                yield return null;
            }

            preloadedLoopOperation = null;
            FirstPersonController loopPlayer = PlacePlayerAtLoopEntry(false);

            int settleFrames = Mathf.Max(0, hiddenCutSettleFrames);
            for (int i = 0; i < settleFrames; i++)
            {
                yield return null;
            }

            SetLoopPlayerInput(loopPlayer, true);
            Debug.Log("GAME_FLOW_HIDDEN_CUT_REVEAL_STARTED", this);
            yield return RevealHiddenCutWhileMoving(loopPlayer);
            hiddenCutCameraFadeActive = false;
            transitionRoutine = null;
            Debug.Log($"GAME_FLOW_TRANSITION_COMPLETE: phase={Phase}, scene={loopSceneName}", this);
        }

        private IEnumerator RevealHiddenCutWhileMoving(FirstPersonController player)
        {
            if (player == null)
            {
                fadeController.SetImmediate(0f);
                Debug.LogWarning("GAME_FLOW_HIDDEN_CUT_REVEAL_PLAYER_MISSING", this);
                yield break;
            }

            float elapsed = 0f;
            float travelledDistance = 0f;
            Vector3 previousPosition = player.transform.position;
            while (fadeController.Alpha > 0.001f)
            {
                if (player == null)
                {
                    fadeController.SetImmediate(0f);
                    yield break;
                }

                Vector3 currentPosition = player.transform.position;
                Vector3 movement = currentPosition - previousPosition;
                movement.y = 0f;
                travelledDistance += movement.magnitude;
                previousPosition = currentPosition;
                elapsed += Time.unscaledDeltaTime;

                float timeProgress = hiddenCutRevealMinimumDuration <= 0f
                    ? 1f
                    : Mathf.Clamp01(elapsed / hiddenCutRevealMinimumDuration);
                float distanceProgress = hiddenCutRevealDistance <= 0f
                    ? 1f
                    : Mathf.Clamp01(travelledDistance / hiddenCutRevealDistance);

                // Nếu đặt thời gian <= 0: Sáng dần thuần theo bước đi. Nếu đặt khoảng cách <= 0: Sáng dần thuần theo thời gian.
                float revealProgress = hiddenCutRevealMinimumDuration <= 0f
                    ? distanceProgress
                    : (hiddenCutRevealDistance <= 0f
                        ? timeProgress
                        : Mathf.Min(timeProgress, distanceProgress));

                float easedProgress = Mathf.SmoothStep(0f, 1f, revealProgress);
                fadeController.SetImmediate(1f - easedProgress);
                yield return null;
            }

            fadeController.SetImmediate(0f);
            Debug.Log(
                $"GAME_FLOW_HIDDEN_CUT_REVEAL_COMPLETE: distance={travelledDistance:0.##}, " +
                $"duration={elapsed:0.##}",
                this);
        }

        private FirstPersonController PlacePlayerAtLoopEntry(bool unlockPlayer)
        {
            LoopEntrySpawnPoint entry = FindFirstObjectByType<LoopEntrySpawnPoint>();
            FirstPersonController player = FindFirstObjectByType<FirstPersonController>();
            if (entry == null || player == null)
            {
                Debug.LogWarning(
                    $"GAME_FLOW_LOOP_ENTRY_SPAWN_MISSING: marker={entry != null}, player={player != null}",
                    this);
                return player;
            }

            Transform spawn = entry.SpawnTransform;
            player.Teleport(spawn.position, spawn.rotation);
            SetLoopPlayerInput(player, unlockPlayer);
            Debug.Log($"GAME_FLOW_LOOP_ENTRY_APPLIED: marker={entry.name}", entry);
            return player;
        }

        private static void SetLoopPlayerInput(FirstPersonController player, bool enabled)
        {
            if (player == null)
            {
                return;
            }

            player.SetCinematicLock(!enabled);
            player.GetComponent<PlayerInteraction>()?.SetInputEnabled(enabled);
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Additive) return;
            if (!scene.IsValid())
            {
                return;
            }

            if (scene.handle == lastHandledSceneHandle)
            {
                return;
            }

            lastHandledSceneHandle = scene.handle;

            var names = new GameFlowScenePhaseResolver.SceneNames(titleSceneName,
                officeSceneName, loopSceneName);
            SetPhase(GameFlowScenePhaseResolver.Resolve(scene.name, Phase, names, IsTransitioning));

            if (scene.name == loopSceneName)
            {
                StartCoroutine(BindLoopCompletionNextFrame());
            }

            if (transitionRoutine == null)
            {
                StartCoroutine(InitialSceneFadeIn());
            }
        }

        private IEnumerator InitialSceneFadeIn()
        {
            if (fadeController != null)
            {
                yield return fadeController.FadeTo(0f, fadeInDuration);
            }

            if (Phase == GameFlowPhase.Opening)
            {
                IsOpeningPresentationReady = true;
            }
            else if (Phase == GameFlowPhase.Ending)
            {
                IsEndingPresentationReady = true;
            }
        }

        private IEnumerator BindLoopCompletionNextFrame()
        {
            yield return null;
            UnbindLoopCompletion();
            boundAnomalyManager = FindFirstObjectByType<DirectionalAnomalyGameManager>();
            if (boundAnomalyManager == null)
            {
                Debug.LogWarning("GAME_FLOW_COMPLETION_SOURCE_MISSING", this);
                yield break;
            }

            boundAnomalyManager.RunCompleted += HandleRunCompleted;
            Debug.Log("GAME_FLOW_COMPLETION_SOURCE_BOUND", boundAnomalyManager);
        }

        private void HandleRunCompleted()
        {
            if (Campaign.IsActive) return;
            if (boundAnomalyManager != null && boundAnomalyManager.SelectedEndingRoute == EndingRoute.BackroomElevator)
                return;
            // Normal portal flow has already acquired the transition before publishing
            // RunCompleted. Retain the event adapter for the debug completion command.
            if (!endingTransitionActive)
                BeginEndingFromEndDoor();
        }

        private IEnumerator RestoreLoopCheckpointIfNeeded()
        {
            if (checkpoints == null || !checkpoints.HasPendingRestore) yield break;
            yield return checkpoints.RestoreLoadedLoop(backroomSceneName);
            if (checkpoints.RestoreSucceeded && checkpoints.RestoredBackroom)
            {
                UnbindLoopCompletion();
                SetPhase(GameFlowPhase.BackroomGameplay);
            }
            else if (!checkpoints.RestoreSucceeded)
            {
                UnbindLoopCompletion();
                SetPhase(GameFlowPhase.Title);
                AsyncOperation titleLoad = SceneManager.LoadSceneAsync(titleSceneName, LoadSceneMode.Single);
                if (titleLoad != null)
                    while (!titleLoad.isDone) yield return null;
            }
        }

        private void SetPhase(GameFlowPhase phase)
        {
            if (Phase == phase)
            {
                return;
            }

            Phase = phase;
            PhaseChanged?.Invoke(phase);
            Debug.Log($"GAME_FLOW_PHASE_CHANGED: {phase}", this);
        }

        internal void SetCampaignPhase(GameFlowPhase phase) => SetPhase(phase);

        private void UnbindLoopCompletion()
        {
            if (boundAnomalyManager != null)
            {
                boundAnomalyManager.RunCompleted -= HandleRunCompleted;
                boundAnomalyManager = null;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                SceneManager.sceneLoaded -= HandleSceneLoaded;
                UnbindLoopCompletion();
                Instance = null;
            }
        }
    }
}
