using Zero.AnomalyPrototype.Flow;
using Zero.EnvironmentLoop;
using Zero.AnomalyPrototype.Player;
using Zero.AnomalyPrototype.Utilities;
using UnityEngine;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class TwoChildrenAnomaly : AnomalyBehaviour
    {
        private SourceSoundPlayback soundPlayback;
        private enum AnomalyPhase
        {
            WaitingForGaze,
            DelayBeforeFly,
            ChildFlyingUp,
            HandsCreeping,
            HandsSnapping,
            HandsRetracting,
            Completed
        }

        [Header("Children References")]
        [Tooltip("Đứa trẻ sẽ đứng yên tại chỗ.")]
        [SerializeField] private Transform remainingChild;
        [Tooltip("Đứa trẻ sẽ vụt bay thẳng lên trần nhà rồi biến mất khi nhìn vào.")]
        [SerializeField] private Transform disappearingChild;
        [Tooltip("Nếu tích chọn, cả 2 đứa trẻ sẽ ẩn trong trạng thái phòng bình thường (Normal) và chỉ hiện khi Anomaly kích hoạt.")]
        [SerializeField] private bool hideInNormalState = true;

        [Header("Gaze & Detection")]
        [Tooltip("Khoảng cách tối đa người chơi nhìn thấy cặp song sinh để kích hoạt.")]
        [SerializeField, Min(1f)] private float maxGazeDistance = 18f;
        [Tooltip("Thời gian nhìn duy trì để kích hoạt (mặc định rất nhạy: 0.05s).")]
        [SerializeField, Min(0.01f)] private float lookDurationRequirement = 0.05f;
        [Tooltip("LayerMask các vật cản che khuất tầm nhìn (mặc định kiểm tra va chạm tường/vật cản).")]
        [SerializeField] private LayerMask gazeObstructionMask = ~0;

        [Header("Fly Up Parameters & Delay")]
        [Tooltip("Thời gian chờ (delay) sau khi nhìn thấy bé gái trước khi 1 bé vụt bay lên và kích hoạt bàn tay quỷ (giây). Mặc định là 2 giây.")]
        [SerializeField, Min(0f)] private float disappearDelay = 2f;
        [Tooltip("Âm thanh rùng rợn / căng thẳng phát ra trong lúc chờ delay (tùy chọn).")]
        [SerializeField] private AudioClip delayTensionAudio;
        [Tooltip("Tốc độ bay vụt lên của đứa trẻ (m/s).")]
        [SerializeField, Min(5f)] private float flySpeed = 24f;
        [Tooltip("Khoảng cách bay lên trước khi biến mất hoàn toàn (mét).")]
        [SerializeField, Min(1f)] private float flyDistance = 8f;
        [Tooltip("Âm thanh vút bay / ai oán khi đứa trẻ biến mất (tùy chọn).")]
        [SerializeField] private AudioClip flyWhooshAudio;

        [Header("Demon Hands Overlay Settings")]
        [Tooltip("Thời gian 2 bàn tay bò từ 2 mép màn hình vào giữa (giây). Đây là thời gian người chơi phải chạy thoát.")]
        [SerializeField, Min(1f)] private float creepDuration = 8f;
        [Tooltip("Thời gian 2 tay nhanh chóng rút ngược về khi người chơi kịp chạy thoát sang tầng khác (giây).")]
        [SerializeField, Min(0.1f)] private float retractDuration = 0.7f;
        [Tooltip("Thời gian của cú thụt/vồ vào kết liễu chớp nhoáng (giây).")]
        [SerializeField, Min(0.05f)] private float snapDuration = 0.12f;
        [Tooltip("Bắt đầu bò ngay khi Anomaly kích hoạt (không cần chờ nhìn bé gái). Mặc định false: đợi nhìn bé gái rồi mới bò.")]
        [SerializeField] private bool triggerCreepImmediately = false;

        [Header("Death & Presentation")]
        [SerializeField] private AnomalyDeathPresentation deathPresentation;
        [SerializeField] private PlayerDeathController deathController;

        // References
        private Transform player;
        private Camera playerCamera;
        private StairLoopManager loopManager;
        private FloorSegment ownerSegment;
        private AudioSource audioSource;
        private DemonHandsOverlay demonHandsOverlay;

        // State tracking
        private AnomalyPhase phase = AnomalyPhase.WaitingForGaze;
        private bool baselineCaptured;
        private Vector3 disappearingChildInitialLocalPos;
        private Quaternion disappearingChildInitialLocalRot;
        private bool disappearingChildInitialActive;
        private bool remainingChildInitialActive;

        private float gazeTimer;
        private float delayTimer;
        private Vector3 flyStartPos;
        private float flyElapsed;
        private bool deathTriggered;

        public void BindPlayer(Transform playerTransform, Camera camera)
        {
            player = playerTransform;
            playerCamera = camera;
        }

        public override void CaptureNormalState()
        {
            ResolveReferences();
            if (baselineCaptured) return;

            if (disappearingChild != null)
            {
                disappearingChildInitialLocalPos = disappearingChild.localPosition;
                disappearingChildInitialLocalRot = disappearingChild.localRotation;
                disappearingChildInitialActive = disappearingChild.gameObject.activeSelf;
            }

            if (remainingChild != null)
            {
                remainingChildInitialActive = remainingChild.gameObject.activeSelf;
            }

            baselineCaptured = true;

            if (hideInNormalState)
            {
                if (disappearingChild != null) disappearingChild.gameObject.SetActive(false);
                if (remainingChild != null) remainingChild.gameObject.SetActive(false);
            }
        }

        protected override void OnActivate()
        {
            ResolveReferences();
            phase = AnomalyPhase.WaitingForGaze;
            gazeTimer = 0f;
            delayTimer = 0f;
            deathTriggered = false;

            if (disappearingChild != null)
            {
                disappearingChild.localPosition = disappearingChildInitialLocalPos;
                disappearingChild.localRotation = disappearingChildInitialLocalRot;
                disappearingChild.gameObject.SetActive(true);
            }

            if (remainingChild != null)
            {
                remainingChild.gameObject.SetActive(true);
            }

            // Tự động tìm DemonHandsOverlay có sẵn trên Canvas và đặt về trạng thái ẩn
            FindHandsOverlay();
            if (demonHandsOverlay != null)
            {
                demonHandsOverlay.ResetOverlay();
            }

            // Nếu không có model bé gái hoặc bật tùy chọn kích hoạt ngay:
            if (triggerCreepImmediately || (disappearingChild == null && remainingChild == null))
            {
                StartHandsCreeping();
            }

            Debug.Log($"TWO_CHILDREN_ACTIVATED: anomaly={name}, handsOverlayFound={demonHandsOverlay != null}", this);
        }

        protected override void OnResetAnomaly()
        {
            phase = AnomalyPhase.WaitingForGaze;
            gazeTimer = 0f;
            delayTimer = 0f;

            if (disappearingChild != null)
            {
                disappearingChild.localPosition = disappearingChildInitialLocalPos;
                disappearingChild.localRotation = disappearingChildInitialLocalRot;
                disappearingChild.gameObject.SetActive(!hideInNormalState && disappearingChildInitialActive);
            }

            if (remainingChild != null)
            {
                remainingChild.gameObject.SetActive(!hideInNormalState && remainingChildInitialActive);
            }

            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }

            if (demonHandsOverlay == null)
            {
                FindHandsOverlay();
            }

            if (demonHandsOverlay != null)
            {
                // Nếu người chơi chưa chết mà tay đang bò hoặc đang rút dở (thoát sang tầng khác),
                // cho phép tay tiếp tục rút về mép màn hình một cách mượt mà thay vì tắt đột ngột
                if (!deathTriggered && (demonHandsOverlay.State == DemonHandsOverlay.OverlayState.Creeping || demonHandsOverlay.State == DemonHandsOverlay.OverlayState.Retracting))
                {
                    demonHandsOverlay.BeginRetract(retractDuration);
                }
                else
                {
                    demonHandsOverlay.ResetOverlay();
                }
            }

            deathTriggered = false;
        }

        private void OnDisable()
        {
            OnResetAnomaly();
        }

        public override void OnPlayerDeathAccepted()
        {
            base.OnPlayerDeathAccepted();
            deathTriggered = true;
            // Không tắt DemonHandsOverlay ở đây để 2 bàn tay giữ nguyên trạng thái vồ/bóp nghẹt (Clutched)
            // trên màn hình trong suốt jumpscare, rung lắc và máu chảy cho đến khi màn hình tối đen (Blackout) và Player thực sự chết / respawn.
        }

        private void Update()
        {
            if (!IsActive) return;

            bool isCurrentFloor = loopManager == null || ownerSegment == null || loopManager.CurrentSegment == ownerSegment;

            switch (phase)
            {
                case AnomalyPhase.WaitingForGaze:
                    if (!isCurrentFloor) return;
                    if (CheckPlayerGaze())
                    {
                        if (disappearDelay <= 0f)
                        {
                            StartFlyingUp();
                        }
                        else
                        {
                            phase = AnomalyPhase.DelayBeforeFly;
                            delayTimer = 0f;
                            if (delayTensionAudio != null && audioSource != null)
                            {
                                GameAudioBootstrap.BindSource(ref soundPlayback, audioSource).Play(null, delayTensionAudio);
                            }
                            Debug.Log($"TWO_CHILDREN_GAZE_TRIGGERED: Player looked at children. Delaying {disappearDelay:F1}s before disappearing.", this);
                        }
                    }
                    break;

                case AnomalyPhase.DelayBeforeFly:
                    if (!isCurrentFloor) return;
                    delayTimer += Time.deltaTime;
                    if (delayTimer >= disappearDelay)
                    {
                        StartFlyingUp();
                    }
                    break;

                case AnomalyPhase.ChildFlyingUp:
                    UpdateFlyingUp();
                    break;

                case AnomalyPhase.HandsCreeping:
                    if (!isCurrentFloor)
                    {
                        // Người chơi đã chạy thoát sang tầng khác!
                        StartRetractingHands();
                    }
                    break;

                case AnomalyPhase.HandsSnapping:
                case AnomalyPhase.HandsRetracting:
                case AnomalyPhase.Completed:
                    break;
            }
        }

        private bool CheckPlayerGaze()
        {
            if (playerCamera == null)
            {
                ResolveReferences();
                if (playerCamera == null) return false;
            }

            Transform target = disappearingChild != null ? disappearingChild : remainingChild;
            if (target == null) return true; // Không có target thì xem như đã thấy để tiếp tục event

            Vector3 cameraPos = playerCamera.transform.position;
            Vector3 targetPos = target.position + Vector3.up * 0.9f;
            float dist = Vector3.Distance(cameraPos, targetPos);
            if (dist > maxGazeDistance) return false;

            // 1. Kiểm tra góc nhìn trong viewport camera
            Vector3 vp = playerCamera.WorldToViewportPoint(targetPos);
            bool inFrustum = vp.z > 0f && vp.x >= 0.05f && vp.x <= 0.95f && vp.y >= 0.05f && vp.y <= 0.95f;
            if (!inFrustum && remainingChild != null && remainingChild != target)
            {
                Vector3 vp2 = playerCamera.WorldToViewportPoint(remainingChild.position + Vector3.up * 0.9f);
                inFrustum = vp2.z > 0f && vp2.x >= 0.05f && vp2.x <= 0.95f && vp2.y >= 0.05f && vp2.y <= 0.95f;
            }

            if (!inFrustum)
            {
                gazeTimer = 0f;
                return false;
            }

            // 2. Kiểm tra vật cản (bỏ qua layer Player để không bị collider của chính mình chắn)
            int mask = gazeObstructionMask.value & ~(1 << LayerMask.NameToLayer("Player"));
            Vector3 dir = (targetPos - cameraPos).normalized;
            bool occluded = false;

            if (dist > 0.6f && Physics.Raycast(cameraPos, dir, out RaycastHit hit, dist - 0.3f, mask, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform != target && !hit.transform.IsChildOf(target) && !target.IsChildOf(hit.transform))
                {
                    occluded = true;
                }
            }

            if (!occluded)
            {
                gazeTimer += Time.deltaTime;
                if (gazeTimer >= lookDurationRequirement)
                {
                    return true;
                }
            }
            else
            {
                gazeTimer = 0f;
            }

            return false;
        }

        private void StartFlyingUp()
        {
            phase = AnomalyPhase.ChildFlyingUp;
            flyElapsed = 0f;
            if (disappearingChild != null)
            {
                flyStartPos = disappearingChild.position;
            }

            if (flyWhooshAudio != null && audioSource != null)
            {
                GameAudioBootstrap.BindSource(ref soundPlayback, audioSource).Play(null, flyWhooshAudio);
            }

            Debug.Log($"TWO_CHILDREN_FLY_TRIGGERED: Player looked at children. Launching upward.", this);
        }

        private void UpdateFlyingUp()
        {
            if (disappearingChild == null)
            {
                FinishFlyingUp();
                return;
            }

            flyElapsed += Time.deltaTime;
            disappearingChild.position += Vector3.up * (flySpeed * Time.deltaTime);

            float currentHeight = disappearingChild.position.y - flyStartPos.y;
            if (currentHeight >= flyDistance || flyElapsed >= (flyDistance / flySpeed))
            {
                FinishFlyingUp();
            }
        }

        private void FinishFlyingUp()
        {
            if (disappearingChild != null)
            {
                disappearingChild.gameObject.SetActive(false);
            }

            StartHandsCreeping();
        }

        private void StartHandsCreeping()
        {
            phase = AnomalyPhase.HandsCreeping;

            FindHandsOverlay();
            if (demonHandsOverlay != null)
            {
                demonHandsOverlay.BeginCreep(creepDuration, onFinished: OnCreepFinished);
            }
            else
            {
                Debug.LogError("TWO_CHILDREN_ANOMALY: Cannot find DemonHandsOverlay on Canvas in scene!", this);
            }

            Debug.Log($"TWO_CHILDREN_HANDS_CREEPING: Demon hands started creeping.", this);
        }

        private void OnCreepFinished()
        {
            if (phase != AnomalyPhase.HandsCreeping) return;

            phase = AnomalyPhase.HandsSnapping;
            Debug.Log($"TWO_CHILDREN_HANDS_SNAPPING: Creep finished. Snapping inward violently!", this);

            if (demonHandsOverlay != null)
            {
                demonHandsOverlay.BeginSnap(snapDuration, onCompleted: OnSnapCompleted);
            }
            else
            {
                OnSnapCompleted();
            }
        }

        private void OnSnapCompleted()
        {
            if (deathTriggered) return;

            deathTriggered = true;
            phase = AnomalyPhase.Completed;
            TriggerPlayerDeath();
        }

        private void StartRetractingHands()
        {
            phase = AnomalyPhase.HandsRetracting;
            Debug.Log($"TWO_CHILDREN_HANDS_RETRACTING: Player escaped floor! Demon hands retracting.", this);

            if (demonHandsOverlay == null)
            {
                FindHandsOverlay();
            }

            if (demonHandsOverlay != null)
            {
                demonHandsOverlay.BeginRetract(retractDuration, onCompleted: () =>
                {
                    phase = AnomalyPhase.Completed;
                });
            }
        }

        private void TriggerPlayerDeath()
        {
            deathController ??= FindAnyObjectByType<PlayerDeathController>();
            if (deathController != null)
            {
                deathController.TryBeginDeath(this, deathPresentation);
            }
            else
            {
                Debug.LogWarning("TWO_CHILDREN_DEATH_CONTROLLER_MISSING: PlayerDeathController not found in scene!", this);
            }
        }

        public DemonHandsOverlay FindHandsOverlay()
        {
            if (demonHandsOverlay == null)
            {
                demonHandsOverlay = FindAnyObjectByType<DemonHandsOverlay>(FindObjectsInactive.Include);
                if (demonHandsOverlay != null)
                {
                    Debug.Log($"TWO_CHILDREN_ANOMALY: Auto-found DemonHandsOverlay on GameObject '{demonHandsOverlay.name}'.", this);
                }
            }
            return demonHandsOverlay;
        }

        private void ResolveReferences()
        {
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }

            if (player == null && playerCamera != null)
            {
                player = playerCamera.transform.root;
            }

            loopManager ??= FindAnyObjectByType<StairLoopManager>();
            ownerSegment ??= GetComponentInParent<FloorSegment>();
            deathPresentation ??= GetComponentInChildren<AnomalyDeathPresentation>(true);
            deathController ??= FindAnyObjectByType<PlayerDeathController>();

            FindHandsOverlay();

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
        }

        [ContextMenu("Debug Trigger Hands Creeping")]
        public void DebugTriggerHandsCreeping()
        {
            StartHandsCreeping();
        }
    }
}
