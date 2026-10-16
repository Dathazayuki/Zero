using Zero.AnomalyPrototype.Flow;
using System;
using UnityEngine;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class DemonHandsOverlay : MonoBehaviour
    {
        private SourceSoundPlayback soundPlayback;
        public enum OverlayState
        {
            Hidden,
            Creeping,
            Snapping,
            Clutched,
            Retracting
        }

        [Header("UI Hand Elements")]
        [Tooltip("RectTransform của bàn tay bên trái trên Canvas.")]
        [SerializeField] private RectTransform leftHand;
        [Tooltip("RectTransform của bàn tay bên phải trên Canvas.")]
        [SerializeField] private RectTransform rightHand;
        [Tooltip("CanvasGroup để điều khiển mờ/hiện toàn bộ 2 bàn tay.")]
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Hand Positioning & Offsets")]
        [Tooltip("Khoảng cách từ tâm màn hình đến đầu ngón tay khi bò vào hết mức (pixel).")]
        [SerializeField] private float handCenterOffset = 60f;
        [Tooltip("Độ thụt/vồ qua tâm màn hình khi kết liễu (pixel).")]
        [SerializeField, Min(10f)] private float snapOvershoot = 130f;
        [Tooltip("Biên độ nhúc nhích run rẩy sống động của bàn tay (pixel).")]
        [SerializeField, Min(0f)] private float handWobbleAmount = 14f;
        [Tooltip("Tần số rung lắc của bàn tay.")]
        [SerializeField, Min(0.1f)] private float wobbleSpeed = 8f;

        [Header("Timings (Có thể chỉnh tại đây hoặc trên Anomaly)")]
        [Tooltip("Thời gian 2 bàn tay bò từ 2 mép màn hình vào giữa (giây).")]
        [SerializeField, Min(1f)] private float creepDuration = 8f;
        [Tooltip("Thời gian 2 tay nhanh chóng rút ngược về khi người chơi kịp chạy thoát sang tầng khác (giây).")]
        [SerializeField, Min(0.1f)] private float retractDuration = 0.7f;
        [Tooltip("Thời gian của cú thụt/vồ vào kết liễu chớp nhoáng (giây).")]
        [SerializeField, Min(0.05f)] private float snapDuration = 0.12f;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [Tooltip("Âm thanh thở rít / cào xé lặp lại khi 2 tay đang bò vào.")]
        [SerializeField] private AudioClip handsCreepAudio;
        [Tooltip("Âm thanh rút tay về khi người chơi thoát thân thành công (tùy chọn).")]
        [SerializeField] private AudioClip handsRetractAudio;
        [Tooltip("Âm thanh jumpscare / bóp nghẹt khi 2 tay thụt vào kết liễu.")]
        [SerializeField] private AudioClip handsSnapJumpscareAudio;

        public OverlayState State { get; private set; } = OverlayState.Hidden;
        public float CreepProgress { get; private set; }

        private float creepTimer;
        private float snapTimer;
        private bool snapCompletedFired;
        private float retractTimer;
        private float retractStartProgress;
        private Action onSnapCompleted;
        private Action onCreepFinished;
        private Action onRetractCompleted;

        private Canvas rootCanvas;
        private RectTransform canvasRect;

        private void Awake()
        {
            ResolveReferences();
            ResetOverlay();
        }

        public void ResolveReferences()
        {
            rootCanvas ??= GetComponentInParent<Canvas>();
            if (rootCanvas != null)
            {
                canvasRect = rootCanvas.GetComponent<RectTransform>();
            }
            if (canvasRect == null)
            {
                canvasRect = GetComponent<RectTransform>();
            }

            canvasGroup ??= GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.playOnAwake = false;
                    audioSource.spatialBlend = 0f;
                }
            }

            handsCreepAudio = GameAudioBootstrap.ResolveDefaultClip(handsCreepAudio, "clip.creepyDemonHand");
        }

        public void BeginCreep(float duration = -1f, Action onFinished = null)
        {
            gameObject.SetActive(true);
            ResolveReferences();
            State = OverlayState.Creeping;
            if (duration > 0f)
            {
                creepDuration = Mathf.Max(0.1f, duration);
            }
            creepTimer = 0f;
            CreepProgress = 0f;
            onCreepFinished = onFinished;

            SetVisible(true);
            UpdateHandPositions(0f);

            if (audioSource != null && handsCreepAudio != null)
            {
                audioSource.clip = handsCreepAudio;
                audioSource.loop = true;
                GameAudioBootstrap.BindSource(ref soundPlayback, audioSource, AudioBus.Sfx, false).Play(null, audioSource.clip, false);
            }
        }

        public void BeginRetract(float duration = -1f, Action onCompleted = null)
        {
            if (State == OverlayState.Hidden || State == OverlayState.Clutched)
            {
                onCompleted?.Invoke();
                return;
            }

            // Nếu đang trong quá trình rút về rồi thì không reset lại tiến trình để tay rút mượt mà
            if (State == OverlayState.Retracting)
            {
                return;
            }

            gameObject.SetActive(true);
            SetVisible(true);

            State = OverlayState.Retracting;
            if (duration > 0f)
            {
                retractDuration = Mathf.Max(0.1f, duration);
            }
            retractTimer = 0f;
            retractStartProgress = CreepProgress;
            onRetractCompleted = onCompleted;

            if (audioSource != null && audioSource.isPlaying && audioSource.clip == handsCreepAudio)
            {
                audioSource.Stop();
            }

            if (audioSource != null && handsRetractAudio != null)
            {
                GameAudioBootstrap.BindSource(ref soundPlayback, audioSource, AudioBus.Sfx, false).Play(null, handsRetractAudio);
            }
        }

        public void BeginSnap(float duration = -1f, Action onCompleted = null)
        {
            gameObject.SetActive(true);
            SetVisible(true);
            State = OverlayState.Snapping;
            if (duration > 0f)
            {
                snapDuration = Mathf.Max(0.01f, duration);
            }
            snapTimer = 0f;
            snapCompletedFired = false;
            onSnapCompleted = onCompleted;

            if (audioSource != null && audioSource.isPlaying && audioSource.clip == handsCreepAudio)
            {
                audioSource.Stop();
            }

            if (audioSource != null && handsSnapJumpscareAudio != null)
            {
                GameAudioBootstrap.BindSource(ref soundPlayback, audioSource, AudioBus.Sfx, false).Play(null, handsSnapJumpscareAudio);
            }
        }

        public void ResetOverlay()
        {
            State = OverlayState.Hidden;
            CreepProgress = 0f;
            creepTimer = 0f;
            snapTimer = 0f;
            snapCompletedFired = false;
            retractTimer = 0f;

            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }

            SetVisible(false);
            UpdateHandPositions(0f);
        }

        private void Update()
        {
            switch (State)
            {
                case OverlayState.Creeping:
                    UpdateCreeping();
                    break;
                case OverlayState.Snapping:
                    UpdateSnapping();
                    break;
                case OverlayState.Clutched:
                    UpdateClutched();
                    break;
                case OverlayState.Retracting:
                    UpdateRetracting();
                    break;
                case OverlayState.Hidden:
                    break;
            }
        }

        private void UpdateCreeping()
        {
            creepTimer += Time.deltaTime;
            CreepProgress = Mathf.Clamp01(creepTimer / creepDuration);
            UpdateHandPositions(CreepProgress);

            if (CreepProgress >= 1f)
            {
                onCreepFinished?.Invoke();
            }
        }

        private void UpdateSnapping()
        {
            snapTimer += Time.deltaTime;
            float t = Mathf.Clamp01(snapTimer / snapDuration);
            float eased = t * t;
            UpdateSnapPositions(eased);

            if (t >= 1f)
            {
                State = OverlayState.Clutched;
                UpdateSnapPositions(1f, addTremor: true);
                if (!snapCompletedFired)
                {
                    snapCompletedFired = true;
                    onSnapCompleted?.Invoke();
                }
            }
        }

        private void UpdateClutched()
        {
            // Giữ nguyên 2 bàn tay vồ qua tâm màn hình với độ rung lắc dữ dội trong suốt jumpscare và quá trình Player chết
            UpdateSnapPositions(1f, addTremor: true);
        }

        private void UpdateRetracting()
        {
            retractTimer += Time.deltaTime;
            float t = Mathf.Clamp01(retractTimer / retractDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            float currentProgress = Mathf.Lerp(retractStartProgress, 0f, smoothT);
            CreepProgress = currentProgress;
            UpdateHandPositions(currentProgress);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Clamp01(1f - t);
            }

            if (t >= 1f)
            {
                ResetOverlay();
                onRetractCompleted?.Invoke();
            }
        }

        public void SetVisible(bool visible)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
            }
            gameObject.SetActive(visible);
        }

        private void UpdateHandPositions(float progress)
        {
            if (leftHand == null || rightHand == null) return;

            float halfWidth = GetHalfWidth();
            float handLength = leftHand.sizeDelta.y > 0f ? leftHand.sizeDelta.y : 600f;

            float startLeftX = -halfWidth - (handLength * 0.55f);
            float startRightX = halfWidth + (handLength * 0.55f);

            float targetLeftX = -handCenterOffset - (handLength * 0.42f);
            float targetRightX = handCenterOffset + (handLength * 0.42f);

            float currentLeftX = Mathf.Lerp(startLeftX, targetLeftX, progress);
            float currentRightX = Mathf.Lerp(startRightX, targetRightX, progress);

            float wobble = handWobbleAmount > 0f ? Mathf.Sin(Time.time * wobbleSpeed) * handWobbleAmount : 0f;
            float wobbleX = handWobbleAmount > 0f ? Mathf.Cos(Time.time * (wobbleSpeed * 0.7f)) * (handWobbleAmount * 0.5f) : 0f;

            leftHand.anchoredPosition = new Vector2(currentLeftX + wobbleX, wobble);
            rightHand.anchoredPosition = new Vector2(currentRightX - wobbleX, -wobble);

            leftHand.localScale = new Vector3(1f, -1f, 1f);
            rightHand.localScale = new Vector3(1f, 1f, 1f);
        }

        private void UpdateSnapPositions(float snapT, bool addTremor = false)
        {
            if (leftHand == null || rightHand == null) return;

            float handLength = leftHand.sizeDelta.y > 0f ? leftHand.sizeDelta.y : 600f;
            float targetLeftX = -handCenterOffset - (handLength * 0.42f);
            float targetRightX = handCenterOffset + (handLength * 0.42f);

            float snappedLeftX = targetLeftX + (snapOvershoot * snapT);
            float snappedRightX = targetRightX - (snapOvershoot * snapT);

            float tremorY = 0f;
            float tremorX = 0f;
            if (addTremor)
            {
                // Rung giật dữ dội khi đang bóp nghẹt người chơi
                tremorY = Mathf.Sin(Time.unscaledTime * 35f) * 8f;
                tremorX = Mathf.Cos(Time.unscaledTime * 28f) * 6f;
            }

            leftHand.anchoredPosition = new Vector2(snappedLeftX + tremorX, tremorY);
            rightHand.anchoredPosition = new Vector2(snappedRightX - tremorX, -tremorY);

            float scaleMul = Mathf.Lerp(1f, 1.18f, snapT);
            leftHand.localScale = new Vector3(scaleMul, -scaleMul, 1f);
            rightHand.localScale = new Vector3(scaleMul, scaleMul, 1f);
        }

        private float GetHalfWidth()
        {
            if (canvasRect != null && canvasRect.rect.width > 100f)
            {
                return canvasRect.rect.width * 0.5f;
            }
            return 960f;
        }

        [ContextMenu("Apply Default Hand Orientations")]
        public void ApplyDefaultHandOrientations()
        {
            if (leftHand != null)
            {
                leftHand.localEulerAngles = new Vector3(0f, 0f, 90f);
                leftHand.localScale = new Vector3(1f, -1f, 1f);
            }

            if (rightHand != null)
            {
                rightHand.localEulerAngles = new Vector3(0f, 0f, 90f);
                rightHand.localScale = new Vector3(1f, 1f, 1f);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ResolveReferences();
        }
#endif
    }
}
