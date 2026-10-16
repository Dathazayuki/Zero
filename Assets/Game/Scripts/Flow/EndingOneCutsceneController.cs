using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using Zero.AnomalyPrototype.Player;

namespace Zero.AnomalyPrototype.Flow
{
    [DisallowMultipleComponent]
    public sealed class EndingOneCutsceneController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Player chính trong gameplay (tự tìm nếu để trống)")]
        [SerializeField] private FirstPersonController player;

        [Tooltip("Animator của Cutscene (chứa clip Replay)")]
        [SerializeField] private Animator cutsceneAnimator;

        [Tooltip("Camera quay của Ending I cutscene")]
        [SerializeField] private Camera cutsceneCamera;

        [Header("Settings")]
        [Tooltip("Thời lượng chạy test cutscene (giây)")]
        [SerializeField, Min(1f)] private float testDuration = 27.55f;

        [Tooltip("Tên state trong Animator để phát")]
        [SerializeField] private string animationStateName = "Cutscene";

        [Tooltip("Phím tắt để kích hoạt test (Mặc định: Phím T)")]
        [SerializeField] private KeyCode testKey = KeyCode.T;
        private KeyCode TestKey => testKey;

        private bool isRunning;
        private Camera playerCamera;
        [SerializeField] private GameObject endingRoot;
        private bool endingPlayback;
        private bool endingRootWasActive;
        public bool IsRunning => isRunning;
        public float Duration => testDuration;

        public bool ReserveEnding()
        {
            ResolveReferences();
            if (!IsConfigured())
            {
                Debug.LogError("ENDING_I_CONFIGURATION_INVALID", this);
                return false;
            }
            endingRootWasActive = endingRoot != null && endingRoot.activeSelf;
            endingPlayback = true;
            return true;
        }

        private bool IsConfigured()
        {
            if (isRunning || endingPlayback || cutsceneAnimator == null || cutsceneCamera == null) return false;
            if (player == null || playerCamera == null || playerCamera == cutsceneCamera) return false;
            return cutsceneAnimator.runtimeAnimatorController != null && !string.IsNullOrEmpty(animationStateName);
        }

        public bool PlayEnding()
        {
            if (!endingPlayback || isRunning) return false;
            if (endingRoot != null) endingRoot.SetActive(true);
            // Activate ancestors as well when the replay was authored inside an inactive cluster.
            for (Transform ancestor = transform; ancestor != null; ancestor = ancestor.parent)
                ancestor.gameObject.SetActive(true);
            enabled = true;
            StartCoroutine(CutsceneRoutine());
            return true;
        }

        public void CancelEnding()
        {
            if (!endingPlayback) return;
            StopAllCoroutines();
            endingPlayback = false;
            CleanupCutscene();
            if (endingRoot != null) endingRoot.SetActive(endingRootWasActive);
        }

        private void Awake()
        {
            ResolveReferences();
        }

        private void Start()
        {
            if (isRunning || endingPlayback) return;
            ResolveReferences();
            if (cutsceneCamera != null)
            {
                cutsceneCamera.gameObject.SetActive(false);
            }
            if (cutsceneAnimator != null)
            {
                cutsceneAnimator.enabled = false;
            }
        }

        public void ResolveReferences()
        {
            if (endingRoot == null) endingRoot = FindEndingRoot();
            if (player == null) player = FindFirstObjectByType<FirstPersonController>();
            if (player != null && playerCamera == null) playerCamera = player.PlayerCamera;
            if (cutsceneAnimator == null) cutsceneAnimator = GetComponentInChildren<Animator>(true);
            if (cutsceneCamera == null) cutsceneCamera = GetComponentInChildren<Camera>(true);
        }

        private GameObject FindEndingRoot()
        {
            for (Transform ancestor = transform; ancestor != null; ancestor = ancestor.parent)
            {
                string n = ancestor.name;
                if (n == "Ending1_CutsceneRoot" || n == "EndingCutscene2" || n == "EndingCutsscene2" || n == "EndingCutsceneB")
                    return ancestor.gameObject;
            }
            return null;
        }

        private void Update()
        {
            if (endingPlayback) return;
            bool keyPressed = false;

#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            if (kb != null && kb.tKey.wasPressedThisFrame)
            {
                keyPressed = true;
            }
#else
            if (Input.GetKeyDown(TestKey))
            {
                keyPressed = true;
            }
#endif

            if (keyPressed)
            {
                if (!isRunning)
                {
                    StartTest();
                }
                else
                {
                    StopTest();
                }
            }
        }

        [ContextMenu("Play Cutscene Test")]
        public void StartTest()
        {
            if (isRunning || endingPlayback) return;
            ResolveReferences();
            StartCoroutine(CutsceneRoutine());
        }

        [ContextMenu("Stop Cutscene Test")]
        public void StopTest()
        {
            if (!isRunning || endingPlayback) return;
            StopAllCoroutines();
            CleanupCutscene();
        }

        private IEnumerator CutsceneRoutine()
        {
            isRunning = true;
            Debug.Log("ENDING_I_CUTSCENE_TEST_STARTED", this);

            // 1. Khóa điều khiển Player
            if (player != null)
            {
                player.SetCinematicLock(true);
            }

            // 2. Chuyển Camera: Tắt camera Player, bật camera Cutscene
            if (playerCamera != null)
            {
                playerCamera.gameObject.SetActive(false);
            }

            if (cutsceneCamera != null)
            {
                cutsceneCamera.gameObject.SetActive(true);
                cutsceneCamera.enabled = true;
                cutsceneCamera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            }

            // 3. Kích hoạt Animator và phát Animation Clip từ đầu
            if (cutsceneAnimator != null)
            {
                cutsceneAnimator.gameObject.SetActive(true);
                cutsceneAnimator.enabled = true;
                cutsceneAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
                cutsceneAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                cutsceneAnimator.speed = 1f;
                if (!string.IsNullOrEmpty(animationStateName))
                {
                    cutsceneAnimator.Play(animationStateName, 0, 0f);
                    cutsceneAnimator.Update(0f);
                }
            }

            if (!endingPlayback)
            {
                SceneFadeController fade = FindFirstObjectByType<SceneFadeController>();
                if (fade != null)
                {
                    fade.SetImmediate(1f);
                    StartCoroutine(fade.FadeFromBlackGradual(3.5f));
                }
            }

            // 4. Chờ hết thời lượng cutscene
            float elapsed = 0f;
            while (elapsed < testDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (endingPlayback)
            {
                cutsceneAnimator.speed = 0f;
                isRunning = false;
                Debug.Log("ENDING_I_CUTSCENE_FINISHED", this);
                yield break;
            }
            CleanupCutscene();
            Debug.Log("ENDING_I_CUTSCENE_TEST_FINISHED", this);
        }

        private void CleanupCutscene()
        {
            if (cutsceneCamera != null)
            {
                cutsceneCamera.gameObject.SetActive(false);
            }

            if (cutsceneAnimator != null)
            {
                cutsceneAnimator.enabled = false;
            }

            if (playerCamera != null)
            {
                playerCamera.gameObject.SetActive(true);
            }

            if (player != null)
            {
                player.SetCinematicLock(false);
            }

            if (!endingPlayback)
            {
                SceneFadeController fade = FindFirstObjectByType<SceneFadeController>();
                if (fade != null)
                {
                    fade.SetImmediate(0f);
                }
            }

            isRunning = false;
        }
    }
}
