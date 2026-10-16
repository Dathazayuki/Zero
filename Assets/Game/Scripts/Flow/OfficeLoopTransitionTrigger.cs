using System.Collections;
using Zero.AnomalyPrototype.Player;
using UnityEngine;

namespace Zero.AnomalyPrototype.Flow
{
    [RequireComponent(typeof(BoxCollider))]
    [DisallowMultipleComponent]
    public sealed class OfficeLoopTransitionTrigger : MonoBehaviour
    {
        private SourceSoundPlayback soundPlayback;
        [Header("Audio")]
        [SerializeField] private AudioSource transitionAudioSource;
        [SerializeField] private AudioClip transitionClip;
        [Tooltip("Sound clip of the body/head hitting the floor on collapse.")]
        [SerializeField] private AudioClip collapseThudClip;

        [Header("Stumble & Slowdown")]
        [Tooltip("Distance in meters player walks in the darkness before collapsing.")]
        [SerializeField, Min(1f)] private float collapseDistance = 4.5f;
        [Tooltip("Maximum walk duration before collapse triggers automatically if player stands still.")]
        [SerializeField, Min(2f)] private float maxWalkDurationBeforeCollapse = 5.0f;
        [Tooltip("Minimum walk speed multiplier at maximum exhaustion.")]
        [SerializeField, Range(0.1f, 1f)] private float minSpeedMultiplier = 0.25f;
        [Tooltip("Screen darkness ratio before collapse (0 to 1). Keep below 1 so the fall is visible.")]
        [SerializeField, Range(0.5f, 0.95f)] private float darknessBeforeCollapse = 0.75f;

        [Header("Collapse & Final Blackout")]
        [Tooltip("Duration of the camera falling to the ground.")]
        [SerializeField, Min(0.2f)] private float collapseDuration = 0.85f;
        [Tooltip("Final camera height off ground when collapsed.")]
        [SerializeField, Min(0.05f)] private float collapsedCameraHeight = 0.25f;
        [Tooltip("Final camera tilt angle (roll) when collapsed onto the floor.")]
        [SerializeField] private float collapsedRollAngle = -48f;
        [Tooltip("Duration player lies dazed on the ground before screen starts fading to complete darkness.")]
        [SerializeField, Min(0f)] private float groundDazeDuration = 1.0f;
        [Tooltip("Duration to smoothly fade from dim view to 100% pitch black while lying on the floor.")]
        [SerializeField, Min(0.2f)] private float finalFadeToBlackDuration = 1.2f;
        [Tooltip("Duration to hold pure pitch black before loading next scene.")]
        [SerializeField, Min(0.1f)] private float blackoutHoldDuration = 0.8f;

        private bool consumed;

        public bool IsConsumed => consumed;

        public void Configure(Light[] ignoredLights, AudioSource source = null, AudioClip clip = null)
        {
            _ = ignoredLights;
            transitionAudioSource = source;
            transitionClip = clip;
            BoxCollider trigger = GetComponent<BoxCollider>();
            trigger.isTrigger = true;
        }

        private void Awake()
        {
            GetComponent<BoxCollider>().isTrigger = true;
        }

        private void Reset()
        {
            BoxCollider trigger = GetComponent<BoxCollider>();
            trigger.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            FirstPersonController player = other.GetComponentInParent<FirstPersonController>();
            if (player != null)
            {
                TryTransition(player);
            }
        }

        public bool FireForValidation(FirstPersonController player)
        {
            return TryTransition(player);
        }

        private bool TryTransition(FirstPersonController player)
        {
            if (consumed || player == null || GameFlowController.Instance == null ||
                GameFlowController.Instance.Phase != GameFlowPhase.Opening)
            {
                return false;
            }

            GameFlowController flow = GameFlowController.Instance;
            if (!flow.HasLoopPreload)
            {
                Debug.LogWarning("OFFICE_LOOP_PRELOAD_NOT_STARTED", this);
                return false;
            }

            consumed = true;
            StartCoroutine(DarkStairTransition(player, flow));
            Debug.Log("OFFICE_LOOP_TRIGGER_CONSUMED", this);
            return true;
        }

        private IEnumerator DarkStairTransition(
            FirstPersonController player,
            GameFlowController flow)
        {
            if (transitionAudioSource != null && transitionClip != null)
            {
                GameAudioBootstrap.BindSource(ref soundPlayback, transitionAudioSource, AudioBus.Sfx, false).Play(null, transitionClip);
            }
            else
            {
                FindFirstObjectByType<OfficeAudioController>()?.PlayTransition();
            }

            // Bắt đầu tiếng thở dốc liên tục cùng lúc với màn hình bắt đầu tối dần
            player.SetForcedBreathing(true);

            if (!flow.BeginHiddenCutCameraFade())
            {
                RestoreAfterRejectedTransition(player);
                yield break;
            }

            Debug.Log("OFFICE_CAMERA_DARKEN_STARTED", this);

            float darkElapsed = 0f;
            float travelledDistance = 0f;
            Vector3 previousPosition = player.transform.position;

            // GIAI ĐOẠN 1: Bước đi loạng choạng, chậm dần, thở dốc và màn hình tối dần tới ngưỡng
            while (travelledDistance < collapseDistance && darkElapsed < maxWalkDurationBeforeCollapse)
            {
                if (player == null || flow == null || flow.Phase != GameFlowPhase.Opening)
                {
                    RestoreAfterRejectedTransition(player);
                    yield break;
                }

                Vector3 currentPosition = player.transform.position;
                Vector3 movement = currentPosition - previousPosition;
                movement.y = 0f;
                travelledDistance += movement.magnitude;
                previousPosition = currentPosition;
                darkElapsed += Time.unscaledDeltaTime;

                float distanceNorm = collapseDistance > 0f ? Mathf.Clamp01(travelledDistance / collapseDistance) : 1f;
                float timeNorm = maxWalkDurationBeforeCollapse > 0f ? Mathf.Clamp01(darkElapsed / maxWalkDurationBeforeCollapse) : 1f;
                float stumbleProgress = Mathf.Max(distanceNorm, timeNorm);

                // Giảm tốc độ di chuyển và tăng độ chao đảo loạng choạng
                float currentSpeedMult = Mathf.Lerp(1f, minSpeedMultiplier, stumbleProgress);
                player.SetSpeedMultiplier(currentSpeedMult);
                player.SetStumbleIntensity(stumbleProgress);

                // Màn hình tối dần từ 0 đến ngưỡng mờ ảo (chưa tối đen 100%)
                float currentDarkness = Mathf.Lerp(0f, darknessBeforeCollapse, Mathf.SmoothStep(0f, 1f, stumbleProgress));
                flow.SetHiddenCutCameraFade(currentDarkness);

                yield return null;
            }

            // GIAI ĐOẠN 2: Ngã quỵ xuống đất (Collapse to Floor)
            player.SetCinematicLock(true);
            player.GetComponent<PlayerInteraction>()?.SetInputEnabled(false);
            player.ResetLocomotionModifiers();

            Camera cam = player.PlayerCamera;
            if (cam != null)
            {
                Vector3 startCamPos = cam.transform.localPosition;
                Quaternion startCamRot = cam.transform.localRotation;
                Vector3 targetCamPos = new Vector3(startCamPos.x, collapsedCameraHeight, startCamPos.z);
                Quaternion targetCamRot = startCamRot * Quaternion.Euler(16f, 0f, collapsedRollAngle);

                float fallElapsed = 0f;

                while (fallElapsed < collapseDuration)
                {
                    fallElapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(fallElapsed / collapseDuration);
                    float fallEase = t * t; // Rơi gia tốc theo trọng lực

                    cam.transform.localPosition = Vector3.Lerp(startCamPos, targetCamPos, fallEase);
                    cam.transform.localRotation = Quaternion.Slerp(startCamRot, targetCamRot, fallEase);

                    // Trong lúc ngã, màn hình vẫn giữ độ tối lờ mờ (CHƯA tối đen)
                    flow.SetHiddenCutCameraFade(darknessBeforeCollapse);

                    yield return null;
                }

                cam.transform.localPosition = targetCamPos;
                cam.transform.localRotation = targetCamRot;
            }

            // Đầu chạm đất: Phát âm thanh va chạm mặt sàn (màn hình lúc này VẪN CHƯA tối đen)
            PlayCollapseAudio();

            // GIAI ĐOẠN 3: Nằm trên sàn 1-2s trong cảnh lờ mờ rồi mới tối đen hoàn toàn
            // 3a. Nằm bất động trên sàn trong groundDazeDuration (~1.0s), mắt vẫn mở lờ mờ nhìn sàn nhà
            float dazeElapsed = 0f;
            while (dazeElapsed < groundDazeDuration)
            {
                dazeElapsed += Time.unscaledDeltaTime;
                flow.SetHiddenCutCameraFade(darknessBeforeCollapse);
                yield return null;
            }

            // 3b. Màn hình từ từ khép mắt / tối đen hoàn toàn trong finalFadeToBlackDuration (~1.2s)
            float fadeElapsed = 0f;
            while (fadeElapsed < finalFadeToBlackDuration)
            {
                fadeElapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(fadeElapsed / finalFadeToBlackDuration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);
                flow.SetHiddenCutCameraFade(Mathf.Lerp(darknessBeforeCollapse, 1f, smoothT));
                yield return null;
            }

            flow.SetHiddenCutCameraFade(1f);
            player.SetForcedBreathing(false);

            // 3c. Giữ màn đen kịt trước khi load scene mới
            while (!flow.IsLoopPreloadReady)
            {
                yield return null;
            }

            yield return new WaitForSecondsRealtime(blackoutHoldDuration);

            Debug.Log(
                $"OFFICE_DARK_STAIR_COLLAPSED_EXIT: distance={travelledDistance:0.##}, " +
                $"duration={darkElapsed:0.##}",
                this);

            if (!flow.EnterLoopGameplay())
            {
                RestoreAfterRejectedTransition(player);
            }
        }

        private void PlayCollapseAudio()
        {
            AudioClip clip = collapseThudClip ?? CreateFallbackThud();

            if (transitionAudioSource == null)
            {
                transitionAudioSource = GetComponent<AudioSource>();
                if (transitionAudioSource == null)
                {
                    transitionAudioSource = gameObject.AddComponent<AudioSource>();
                    transitionAudioSource.playOnAwake = false;
                    transitionAudioSource.spatialBlend = 0f;
                }
            }

            if (clip != null && transitionAudioSource != null)
            {
                GameAudioBootstrap.BindSource(ref soundPlayback, transitionAudioSource, AudioBus.Sfx, false).Play(null, clip, true, 0.85f);
            }
        }

        private static AudioClip CreateFallbackThud()
        {
            const int sampleRate = 44100;
            const float duration = 0.45f;
            int count = Mathf.CeilToInt(duration * sampleRate);
            float[] samples = new float[count];
            var random = new System.Random(42);
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)count;
                float envelope = Mathf.Pow(1f - t, 2.5f);
                float freq = Mathf.Lerp(65f, 32f, t);
                float noise = (float)(random.NextDouble() * 2.0 - 1.0) * 0.15f;
                samples[i] = (Mathf.Sin(i * Mathf.PI * 2f * freq / sampleRate) * 0.7f + noise) * envelope;
            }

            AudioClip result = AudioClip.Create("BodyThudFallback", count, 1, sampleRate, false);
            result.SetData(samples, 0);
            return result;
        }

        private void RestoreAfterRejectedTransition(FirstPersonController player)
        {
            consumed = false;
            GameFlowController.Instance?.CancelHiddenCutCameraFade();
            if (player != null)
            {
                player.ResetLocomotionModifiers();
                player.SetForcedBreathing(false);
                player.SetCinematicLock(false);
                player.GetComponent<PlayerInteraction>()?.SetInputEnabled(true);
            }
            Debug.LogWarning("OFFICE_LOOP_TRANSITION_REJECTED", this);
        }
    }
}
