using System.Collections;
using Zero.AnomalyPrototype.Anomalies;
using Zero.EnvironmentLoop;
using UnityEngine;
using UnityEngine.UI;

namespace Zero.AnomalyPrototype.Player
{
    public sealed class PlayerDeathController : MonoBehaviour
    {
        public enum DeathState
        {
            Alive,
            Jumpscare,
            Blackout,
            Respawning
        }

        [Header("Player and world")]
        [SerializeField] private FirstPersonController playerController;
        [SerializeField] private PlayerInteraction playerInteraction;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private StairLoopManager stairLoopManager;
        [SerializeField] private DirectionalAnomalyGameManager anomalyGameManager;
        [SerializeField] private AudioSource jumpscareAudioSource;

        [Header("Death UI")]
        [SerializeField] private CanvasGroup blackoutCanvasGroup;
        [SerializeField] private Image bloodOverlay;
        [SerializeField] private CanvasGroup bloodCanvasGroup;
        [SerializeField] private Sprite[] bloodVariants;

        [Header("Jumpscare timing")]
        [SerializeField, Min(0f)] private float cameraApproachDuration = 0.45f;
        [SerializeField, Min(0f)] private float shakeHoldDuration = 0.65f;
        [SerializeField, Min(0f)] private float blackoutFadeDuration = 0.4f;
        [SerializeField, Min(0f)] private float blackoutHoldDuration = 2.5f;
        [SerializeField, Min(0f)] private float respawnFadeDuration = 0.55f;

        [Header("Jumpscare camera")]
        [SerializeField, Range(1f, 120f)] private float jumpscareFieldOfView = 38f;
        [SerializeField, Min(0f)] private float positionShake = 0.035f;
        [SerializeField, Min(0f)] private float rotationShake = 2.2f;

        [Header("Devour lunge")]
        [SerializeField, Min(0f)] private float devourLungeDuration = 0.24f;
        [SerializeField, Min(0f)] private float devourStopDistance = 0.06f;
        [SerializeField, Range(1f, 120f)] private float devourFieldOfView = 68f;
        [SerializeField, Min(0f)] private float devourShakeMultiplier = 2.5f;

        [Header("Blood")]
        [SerializeField, Min(0.01f)] private float suffocationDuration = 1.1f;
        [SerializeField, Min(0f)] private float suffocationSink = 0.12f;
        [SerializeField, Range(1f, 120f)] private float suffocationFieldOfView = 34f;

        [Header("Blood overlay")]
        [SerializeField, Min(0f)] private float bloodRevealDuration = 0.75f;
        [SerializeField, Range(0f, 1f)] private float bloodMaximumAlpha = 0.85f;
        [SerializeField, Min(0f)] private float bloodVerticalDrift = 12f;

        private readonly System.Random cosmeticRandom = new System.Random();
        private Coroutine deathRoutine;
        private Vector3 initialPlayerPosition;
        private Quaternion initialPlayerRotation;
        private Vector3 initialCameraLocalPosition;
        private Quaternion initialCameraLocalRotation;
        private float initialCameraFieldOfView;
        private Vector2 initialBloodAnchoredPosition;
        private Vector3 initialBloodScale = Vector3.one;
        private int lastBloodVariant = -1;
        private bool bloodConfigured;
        private bool interactionWasEnabled;

        public DeathState State { get; private set; } = DeathState.Alive;
        public bool IsDeathInProgress => State != DeathState.Alive;
        public bool IsScreenBlack => (State == DeathState.Blackout && blackoutCanvasGroup != null && blackoutCanvasGroup.alpha >= 0.99f) || State == DeathState.Respawning;

        private void Awake()
        {
            ResolveReferences();
            CaptureInitialState();
            ResetOverlayState();
        }

        public bool TryBeginDeath(
            AnomalyBehaviour source,
            AnomalyDeathPresentation presentation)
        {
            if (State != DeathState.Alive)
            {
                return Reject(source, "death-already-in-progress");
            }

            if (source == null)
            {
                return Reject(null, "missing-anomaly-source");
            }

            if (!source.IsActive)
            {
                return Reject(source, "anomaly-is-not-active");
            }

            if (source.Data == null || !source.Data.CanKillPlayer)
            {
                return Reject(source, "can-kill-player-is-disabled");
            }

            ResolveReferences();
            if (playerController == null || playerCamera == null)
            {
                Debug.LogError(
                    $"DEATH_CONFIGURATION_INVALID: player={playerController != null}, camera={playerCamera != null}",
                    this);
                return Reject(source, "missing-player-or-camera");
            }

            if (presentation == null || (presentation.MotionStyle != DeathMotionStyle.Suffocation && !presentation.HasCameraPose))
            {
                Debug.LogWarning(
                    $"DEATH_CONFIGURATION_INVALID: anomaly={source.name}, " +
                    $"presentation={presentation != null}, cameraAnchor={presentation != null && presentation.HasCameraPose}",
                    source);
            }

            if (blackoutCanvasGroup == null)
            {
                Debug.LogWarning("DEATH_CONFIGURATION_INVALID: blackout CanvasGroup is missing.", this);
            }

            State = DeathState.Jumpscare;
            try
            {
                source.OnPlayerDeathAccepted();
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception, source);
            }

            LockPlayer();
            PrepareBlood();
            presentation?.PlayJumpscareAnimation();
            PlayJumpscareAudio(presentation?.JumpscareAudioClip);
            Debug.Log($"DEATH_REQUEST_ACCEPTED: anomaly={source.name}", source);
            Debug.Log($"DEATH_JUMPSCARE_STARTED: anomaly={source.name}", source);
            deathRoutine = StartCoroutine(DeathSequence(presentation));
            return true;
        }

        private IEnumerator DeathSequence(AnomalyDeathPresentation presentation)
        {
            Vector3 cameraStartPosition = playerCamera.transform.position;
            Quaternion cameraStartRotation = playerCamera.transform.rotation;
            float cameraStartFov = playerCamera.fieldOfView;
            float bloodElapsed = 0f;

            float elapsed = 0f;
            float duration;
            if (presentation != null && presentation.MotionStyle == DeathMotionStyle.Suffocation)
            {
                Debug.Log("DEATH_SUFFOCATION_STARTED", this);
                duration = Mathf.Max(0.01f, suffocationDuration);
                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    bloodElapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    float strength = t * t;
                    float noise = (Mathf.PerlinNoise(elapsed * 18f, 0.37f) - 0.5f) * 2f;
                    float noiseY = (Mathf.PerlinNoise(0.71f, elapsed * 21f) - 0.5f) * 2f;
                    playerCamera.transform.SetPositionAndRotation(
                        cameraStartPosition + Vector3.down * (suffocationSink * t) +
                        cameraStartRotation * new Vector3(noise, noiseY, 0f) * (positionShake * strength),
                        cameraStartRotation * Quaternion.Euler(noiseY * strength, noise * strength, noise * 3f * strength));
                    playerCamera.fieldOfView = Mathf.Lerp(cameraStartFov, suffocationFieldOfView, t) +
                        Mathf.Sin(elapsed * 24f) * 1.5f * Mathf.Sin(t * Mathf.PI);
                    UpdateBloodReveal(bloodElapsed);
                    yield return null;
                }
            }
            else
            {
                duration = Mathf.Max(0f, cameraApproachDuration);
                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    bloodElapsed += Time.unscaledDeltaTime;
                    float t = duration <= Mathf.Epsilon ? 1f : Mathf.Clamp01(elapsed / duration);
                    float eased = t * t * (3f - 2f * t);
                    if (presentation != null && presentation.TryGetCameraPose(out Vector3 targetPosition,
                            out Quaternion targetRotation))
                    {
                        playerCamera.transform.SetPositionAndRotation(
                            Vector3.Lerp(cameraStartPosition, targetPosition, eased),
                            Quaternion.Slerp(cameraStartRotation, targetRotation, eased));
                    }

                    playerCamera.fieldOfView = Mathf.Lerp(cameraStartFov, jumpscareFieldOfView, eased);
                    UpdateBloodReveal(bloodElapsed);
                    yield return null;
                }

                elapsed = 0f;
                duration = Mathf.Max(0f, shakeHoldDuration);
                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    bloodElapsed += Time.unscaledDeltaTime;
                    ApplyJumpscareCamera(
                        presentation,
                        elapsed,
                        cameraStartPosition,
                        cameraStartRotation);
                    UpdateBloodReveal(bloodElapsed);
                    yield return null;
                }

                if (presentation != null && presentation.TryGetDevourTarget(out _))
                {
                    Vector3 lungeStartPosition = playerCamera.transform.position;
                    Quaternion lungeStartRotation = playerCamera.transform.rotation;
                    float lungeStartFov = playerCamera.fieldOfView;
                    elapsed = 0f;
                    duration = Mathf.Max(0f, devourLungeDuration);
                    Debug.Log("DEATH_DEVOUR_LUNGE_STARTED", this);

                    if (duration <= Mathf.Epsilon)
                    {
                        ApplyDevourCamera(
                            presentation,
                            lungeStartPosition,
                            lungeStartRotation,
                            lungeStartFov,
                            1f);
                    }
                    else
                    {
                        while (elapsed < duration)
                        {
                            elapsed += Time.unscaledDeltaTime;
                            bloodElapsed += Time.unscaledDeltaTime;
                            float t = Mathf.Clamp01(elapsed / duration);
                            ApplyDevourCamera(
                                presentation,
                                lungeStartPosition,
                                lungeStartRotation,
                                lungeStartFov,
                                t);
                            UpdateBloodReveal(bloodElapsed);
                            yield return null;
                        }
                    }

                    Debug.Log("DEATH_DEVOUR_LUNGE_COMPLETED", this);
                }
                else
                {
                    Debug.LogWarning("DEATH_DEVOUR_LUNGE_SKIPPED: FocusTarget is missing.", this);
                }

            }

            State = DeathState.Blackout;
            elapsed = 0f;
            duration = Mathf.Max(0f, blackoutFadeDuration);
            float startingBlackAlpha = blackoutCanvasGroup != null ? blackoutCanvasGroup.alpha : 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                bloodElapsed += Time.unscaledDeltaTime;
                float t = duration <= Mathf.Epsilon ? 1f : Mathf.Clamp01(elapsed / duration);
                SetBlackoutAlpha(Mathf.Lerp(startingBlackAlpha, 1f, t));
                UpdateBloodReveal(bloodElapsed);
                yield return null;
            }

            SetBlackoutAlpha(1f);
            ClearBlood();
            Debug.Log("DEATH_BLACKOUT", this);

            // Delay 2-3 giây ở màn hình đen trước khi hồi sinh cho mọi thứ kịp reset sạch sẽ về trạng thái ban đầu
            float totalHold = Mathf.Max(blackoutHoldDuration, 2.5f);
            float preResetHold = Mathf.Min(0.8f, totalHold * 0.35f);
            float postResetHold = totalHold - preResetHold;

            if (preResetHold > 0f)
            {
                yield return new WaitForSecondsRealtime(preResetHold);
            }

            ResetWorldAndPlayer();
            Debug.Log("DEATH_WORLD_RESET", this);

            if (postResetHold > 0f)
            {
                yield return new WaitForSecondsRealtime(postResetHold);
            }

            State = DeathState.Respawning;

            elapsed = 0f;
            duration = Mathf.Max(0f, respawnFadeDuration);
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = duration <= Mathf.Epsilon ? 1f : Mathf.Clamp01(elapsed / duration);
                SetBlackoutAlpha(1f - t);
                yield return null;
            }

            SetBlackoutAlpha(0f);
            ClearBlood();
            RestoreCamera();
            UnlockPlayer();
            State = DeathState.Alive;
            deathRoutine = null;
            Debug.Log("DEATH_RESPAWN_COMPLETE", this);
        }

        private void ApplyJumpscareCamera(
            AnomalyDeathPresentation presentation,
            float elapsed,
            Vector3 fallbackPosition,
            Quaternion fallbackRotation)
        {
            Vector3 position = fallbackPosition;
            Quaternion rotation = fallbackRotation;
            if (presentation != null && presentation.TryGetCameraPose(out Vector3 targetPosition, out Quaternion targetRotation))
            {
                position = targetPosition;
                rotation = targetRotation;
            }

            float noiseTime = Time.unscaledTime * 31.7f + elapsed;
            Vector3 positionNoise = new Vector3(
                Mathf.PerlinNoise(noiseTime, 0.17f) - 0.5f,
                Mathf.PerlinNoise(0.43f, noiseTime) - 0.5f,
                Mathf.PerlinNoise(noiseTime, 0.79f) - 0.5f) * (positionShake * 2f);
            Vector3 rotationNoise = new Vector3(
                Mathf.PerlinNoise(noiseTime, 1.17f) - 0.5f,
                Mathf.PerlinNoise(1.43f, noiseTime) - 0.5f,
                Mathf.PerlinNoise(noiseTime, 1.79f) - 0.5f) * (rotationShake * 2f);
            playerCamera.transform.SetPositionAndRotation(
                position + positionNoise,
                rotation * Quaternion.Euler(rotationNoise));
            playerCamera.fieldOfView = jumpscareFieldOfView;
        }

        private void ApplyDevourCamera(
            AnomalyDeathPresentation presentation,
            Vector3 startPosition,
            Quaternion startRotation,
            float startFieldOfView,
            float progress)
        {
            if (presentation == null || !presentation.TryGetDevourTarget(out Vector3 focusPosition))
            {
                return;
            }

            float t = Mathf.Clamp01(progress);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            Vector3 approachDirection = focusPosition - startPosition;
            Vector3 endPosition = focusPosition;
            if (approachDirection.sqrMagnitude > 0.0001f)
            {
                endPosition -= approachDirection.normalized * devourStopDistance;
            }

            Vector3 position = Vector3.Lerp(startPosition, endPosition, eased);
            Vector3 lookDirection = focusPosition - position;
            Quaternion rotation = lookDirection.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(lookDirection.normalized, Vector3.up)
                : startRotation;

            float noiseTime = Time.unscaledTime * 47.3f;
            float shakeStrength = devourShakeMultiplier * t;
            Vector3 positionNoise = new Vector3(
                Mathf.PerlinNoise(noiseTime, 2.17f) - 0.5f,
                Mathf.PerlinNoise(2.43f, noiseTime) - 0.5f,
                Mathf.PerlinNoise(noiseTime, 2.79f) - 0.5f) *
                                    (positionShake * 2f * shakeStrength);
            Vector3 rotationNoise = new Vector3(
                Mathf.PerlinNoise(noiseTime, 3.17f) - 0.5f,
                Mathf.PerlinNoise(3.43f, noiseTime) - 0.5f,
                Mathf.PerlinNoise(noiseTime, 3.79f) - 0.5f) *
                                    (rotationShake * 2f * shakeStrength);

            playerCamera.transform.SetPositionAndRotation(
                position + positionNoise,
                Quaternion.Slerp(startRotation, rotation, eased) * Quaternion.Euler(rotationNoise));
            playerCamera.fieldOfView = Mathf.Lerp(startFieldOfView, devourFieldOfView, eased);
        }

        private void ResetWorldAndPlayer()
        {
            try
            {
                stairLoopManager?.ResetToInitialState();
                anomalyGameManager?.ResetRun();
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception, this);
            }

            if (jumpscareAudioSource != null)
            {
                jumpscareAudioSource.Stop();
            }
            try
            {
                playerController.Teleport(initialPlayerPosition, initialPlayerRotation);
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception, playerController);
            }

            Physics.SyncTransforms();
            RestoreCamera();
            ClearBlood();
        }

        private void LockPlayer()
        {
            interactionWasEnabled = playerInteraction != null && playerInteraction.enabled;
            if (playerInteraction != null)
            {
                playerInteraction.enabled = false;
            }

            playerController.SetCinematicLock(true);
        }

        private void UnlockPlayer()
        {
            playerController?.SetCinematicLock(false);
            if (playerInteraction != null)
            {
                playerInteraction.enabled = interactionWasEnabled;
            }
        }

        private void PrepareBlood()
        {
            bloodConfigured = bloodOverlay != null && bloodCanvasGroup != null &&
                              bloodVariants != null && bloodVariants.Length > 0;
            if (!bloodConfigured)
            {
                Debug.LogWarning(
                    $"DEATH_BLOOD_CONFIGURATION_INVALID: image={bloodOverlay != null}, " +
                    $"canvasGroup={bloodCanvasGroup != null}, variants={bloodVariants?.Length ?? 0}",
                    this);
                return;
            }


            if (bloodVariants.Length != 3)
            {
                Debug.LogWarning(
                    $"DEATH_BLOOD_CONFIGURATION_INVALID: expected=3, assigned={bloodVariants.Length}; " +
                    "available sprites will still be used.",
                    this);
            }

            int selected = SelectBloodVariant();
            if (selected < 0)
            {
                bloodConfigured = false;
                Debug.LogWarning("DEATH_BLOOD_CONFIGURATION_INVALID: every blood variant is missing.", this);
                return;
            }
            bloodOverlay.sprite = bloodVariants[selected];
            bloodOverlay.type = Image.Type.Filled;
            bloodOverlay.fillMethod = Image.FillMethod.Vertical;
            bloodOverlay.fillOrigin = (int)Image.OriginVertical.Top;
            bloodOverlay.fillAmount = 0f;
            bloodOverlay.raycastTarget = false;
            bloodCanvasGroup.alpha = 0f;
            bloodCanvasGroup.blocksRaycasts = false;
            bloodCanvasGroup.interactable = false;
            RectTransform rect = bloodOverlay.rectTransform;
            rect.anchoredPosition = initialBloodAnchoredPosition;
            Vector3 scale = initialBloodScale;
            scale.x = Mathf.Abs(scale.x) * (cosmeticRandom.Next(0, 2) == 0 ? -1f : 1f);
            rect.localScale = scale;
            lastBloodVariant = selected;
            Debug.Log($"DEATH_BLOOD_STARTED: variant={selected}", this);
        }

        private int SelectBloodVariant()
        {
            int validCount = CountValidBloodVariants();
            int eligibleCount = 0;
            for (int i = 0; i < bloodVariants.Length; i++)
            {
                if (bloodVariants[i] != null && (i != lastBloodVariant || validCount == 1))
                {
                    eligibleCount++;
                }
            }

            if (eligibleCount == 0)
            {
                return -1;
            }

            int ordinal = cosmeticRandom.Next(0, eligibleCount);
            for (int i = 0; i < bloodVariants.Length; i++)
            {
                if (bloodVariants[i] == null || (i == lastBloodVariant && validCount > 1))
                {
                    continue;
                }

                if (ordinal-- == 0)
                {
                    return i;
                }
            }

            return -1;
        }

        private int CountValidBloodVariants()
        {
            int count = 0;
            for (int i = 0; i < bloodVariants.Length; i++)
            {
                if (bloodVariants[i] != null)
                {
                    count++;
                }
            }

            return count;
        }

        private void UpdateBloodReveal(float elapsed)
        {
            if (!bloodConfigured)
            {
                return;
            }

            float t = bloodRevealDuration <= Mathf.Epsilon
                ? 1f
                : Mathf.Clamp01(elapsed / bloodRevealDuration);
            float eased = t * t * (3f - 2f * t);
            bloodOverlay.fillAmount = eased;
            bloodCanvasGroup.alpha = bloodMaximumAlpha * eased;
            bloodOverlay.rectTransform.anchoredPosition = initialBloodAnchoredPosition +
                                                         Vector2.down * (bloodVerticalDrift * eased);
        }

        private void ClearBlood()
        {
            if (bloodOverlay != null)
            {
                bloodOverlay.fillAmount = 0f;
                bloodOverlay.sprite = null;
                bloodOverlay.rectTransform.anchoredPosition = initialBloodAnchoredPosition;
                bloodOverlay.rectTransform.localScale = initialBloodScale;
            }

            if (bloodCanvasGroup != null)
            {
                bloodCanvasGroup.alpha = 0f;
            }

            bloodConfigured = false;
            Debug.Log("DEATH_BLOOD_CLEARED", this);
        }

        private void ResolveReferences()
        {
            if (playerController == null)
            {
                playerController = GetComponent<FirstPersonController>();
            }

            if (playerInteraction == null)
            {
                playerInteraction = GetComponent<PlayerInteraction>();
            }

            if (playerCamera == null)
            {
                playerCamera = playerController != null
                    ? playerController.PlayerCamera
                    : GetComponentInChildren<Camera>(true);
            }

            if (stairLoopManager == null)
            {
                stairLoopManager = FindAnyObjectByType<StairLoopManager>();
            }

            if (anomalyGameManager == null)
            {
                anomalyGameManager = stairLoopManager != null
                    ? stairLoopManager.AnomalyGameManager
                    : FindAnyObjectByType<DirectionalAnomalyGameManager>();
            }

            if (jumpscareAudioSource == null)
            {
                jumpscareAudioSource = GetComponent<AudioSource>();
            }

            if (blackoutCanvasGroup != null)
            {
                Canvas blackoutCanvas = blackoutCanvasGroup.GetComponentInParent<Canvas>();
                if (blackoutCanvas != null)
                {
                    blackoutCanvas.sortingOrder = 1000;
                }
            }
        }

        private void CaptureInitialState()
        {
            Transform playerTransform = playerController != null ? playerController.transform : transform;
            initialPlayerPosition = playerTransform.position;
            initialPlayerRotation = playerTransform.rotation;
            if (playerCamera != null)
            {
                initialCameraLocalPosition = playerCamera.transform.localPosition;
                initialCameraLocalRotation = playerCamera.transform.localRotation;
                initialCameraFieldOfView = playerCamera.fieldOfView;
            }

            if (bloodOverlay != null)
            {
                initialBloodAnchoredPosition = bloodOverlay.rectTransform.anchoredPosition;
                initialBloodScale = bloodOverlay.rectTransform.localScale;
            }
        }

        private void RestoreCamera()
        {
            if (playerCamera == null)
            {
                return;
            }

            playerCamera.transform.localPosition = initialCameraLocalPosition;
            playerCamera.transform.localRotation = initialCameraLocalRotation;
            playerCamera.fieldOfView = initialCameraFieldOfView;
        }

        private void ResetOverlayState()
        {
            SetBlackoutAlpha(0f);
            if (bloodCanvasGroup != null)
            {
                bloodCanvasGroup.alpha = 0f;
                bloodCanvasGroup.blocksRaycasts = false;
                bloodCanvasGroup.interactable = false;
            }

            if (bloodOverlay != null)
            {
                bloodOverlay.fillAmount = 0f;
                bloodOverlay.raycastTarget = false;
            }
        }

        private void SetBlackoutAlpha(float alpha)
        {
            if (blackoutCanvasGroup != null)
            {
                blackoutCanvasGroup.alpha = Mathf.Clamp01(alpha);
                blackoutCanvasGroup.blocksRaycasts = false;
                blackoutCanvasGroup.interactable = false;
            }
        }

        private void PlayJumpscareAudio(AudioClip clip)
        {
            if (clip == null || jumpscareAudioSource == null)
            {
                return;
            }

            jumpscareAudioSource.Stop();
            jumpscareAudioSource.spatialBlend = 0f;
            jumpscareAudioSource.PlayOneShot(clip);
        }

        private bool Reject(AnomalyBehaviour source, string reason)
        {
            Debug.LogWarning(
                $"DEATH_REQUEST_REJECTED: anomaly={source?.name ?? "None"}, reason={reason}",
                source != null ? source : this);
            return false;
        }

        private void OnDisable()
        {
            if (deathRoutine == null)
            {
                return;
            }

            StopCoroutine(deathRoutine);
            deathRoutine = null;
            RestoreCamera();
            ResetOverlayState();
            UnlockPlayer();
            State = DeathState.Alive;
        }
    }
}
