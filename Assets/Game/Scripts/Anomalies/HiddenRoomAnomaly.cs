using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Zero.AnomalyPrototype.Player;
using Zero.EnvironmentLoop;

namespace Zero.AnomalyPrototype.Anomalies
{
    /// <summary>
    /// Structural change anomaly:
    /// - Khi vòng lặp kích hoạt: Căn phòng ban đầu vẫn hoàn toàn bình thường.
    /// - Khi người chơi nhìn vào mũi tên chỉ hướng (CorrectArrow):
    ///   + Cửa chính ra vào phòng bị biến mất và thay bằng bức tường kín (Doorway_Sealed).
    ///   + Lối đi bí mật trên tường (Doorway_Revealed) mở ra.
    ///   + Toàn bộ căn phòng ẩn (HiddenRoom_Architecture) hiện lên.
    /// - Khi người chơi bước vào phòng ẩn:
    ///   + Camera Player làm hiệu ứng chớp mắt (Blink).
    ///   + Trong lúc nhắm mắt: Thay toàn bộ Material trong phòng ẩn thành Material Eye Devil.
    ///   + Khi mở mắt ra nhìn thấy phòng quỷ: Người chơi chết (Player Death).
    /// - Khi reset / kết thúc vòng: Trở lại trạng thái bình thường ban đầu.
    /// </summary>
    public sealed class HiddenRoomAnomaly : AnomalyBehaviour
    {
        [Header("Main Entrance Doorway")]
        [Tooltip("Cửa vào/ra bình thường (Doorway_Normal)")]
        [SerializeField] private GameObject entranceNormal;
        [Tooltip("Bức tường bịt kín cửa vào/ra (Doorway_Sealed)")]
        [SerializeField] private GameObject entranceSealed;

        [Header("Hidden Passage Variants")]
        [Tooltip("Bức tường che giấu lối đi bí mật (Doorway_Hidden)")]
        [SerializeField] private GameObject doorwayHidden;
        [Tooltip("Ô cửa mở thông vào phòng ẩn (Doorway_Revealed)")]
        [SerializeField] private GameObject doorwayRevealed;

        [Header("Hidden Room Architecture & Content")]
        [Tooltip("Root của căn phòng ẩn (HiddenRoom_Architecture)")]
        [SerializeField] private GameObject hiddenRoomRoot;

        [Header("Hidden Room Eye Devil Transformation")]
        [Tooltip("Material Eye Devil sẽ thay thế toàn bộ vật thể trong phòng ẩn.")]
        [SerializeField] private Material eyeDevilMaterial;
        [Tooltip("Trigger Collider ở lối vào hoặc bên trong phòng ẩn để phát hiện người chơi bước vào.")]
        [SerializeField] private Collider roomEntryTrigger;

        [Header("Blink Settings")]
        [Tooltip("Thời gian nhắm mắt lại (giây).")]
        [SerializeField, Range(0.05f, 1f)] private float blinkCloseDuration = 0.2f;
        [Tooltip("Thời gian giữ mắt nhắm (thay đổi Material phòng ẩn lúc này) (giây).")]
        [SerializeField, Range(0.05f, 1f)] private float blinkHoldDuration = 0.2f;
        [Tooltip("Thời gian mở mắt ra (giây).")]
        [SerializeField, Range(0.05f, 1f)] private float blinkOpenDuration = 0.25f;
        [Tooltip("Thời gian người chơi nhìn thấy phòng quỷ sau khi mở mắt trước khi chết (giây).")]
        [SerializeField, Range(0f, 1f)] private float delayBeforeDeath = 0.15f;
        [Tooltip("Âm thanh chớp mắt / âm thanh rùng rợn khi biến đổi (tùy chọn).")]
        [SerializeField] private AudioClip blinkAudioClip;

        [Header("Sight Detection (Nhìn vào mũi tên)")]
        [Tooltip("Controller mũi tên phòng (CorrectArrowController)")]
        [SerializeField] private CorrectArrowController arrowController;
        [Tooltip("Transform mục tiêu mũi tên (nếu để trống sẽ lấy từ arrowController)")]
        [SerializeField] private Transform arrowTarget;
        [Tooltip("Camera người chơi")]
        [SerializeField] private Camera playerCamera;
        [Tooltip("Thời gian cần nhìn liên tục vào mũi tên để kích hoạt (giây)")]
        [SerializeField, Min(0f)] private float requiredLookDuration = 0.12f;
        [Tooltip("Chỉ tính khi mũi tên nằm trong vùng trung tâm tầm nhìn")]
        [SerializeField] private bool requireCenterFocus = true;
        [Tooltip("Bán kính vùng trung tâm viewport (0.5 là nửa màn hình)")]
        [SerializeField, Range(0.1f, 0.5f)] private float focusViewportRadius = 0.35f;
        [Tooltip("Layer che khuất tầm nhìn (mặc định kiểm tra tất cả)")]
        [SerializeField] private LayerMask obstructionMask = ~0;

        [Header("Death Settings")]
        [Tooltip("Cấu hình presentation khi chết (nếu để trống script tự tạo)")]
        [SerializeField] private AnomalyDeathPresentation deathPresentation;
        [Tooltip("PlayerDeathController trong màn chơi")]
        [SerializeField] private PlayerDeathController deathController;
        [Tooltip("FirstPersonController của người chơi")]
        [SerializeField] private FirstPersonController player;

        private float lookTimer;
        private bool hasTransformed;
        private bool hasTriggeredRoomEntry;
        private Coroutine blinkRoutine;

        // Lưu trữ material gốc của các Renderer trong phòng ẩn để khôi phục
        private readonly List<RendererMaterialCache> cachedMaterials = new List<RendererMaterialCache>();

        // UI Mí mắt dùng cho hiệu ứng chớp mắt
        private Canvas blinkCanvas;
        private RectTransform topEyelid;
        private RectTransform bottomEyelid;
        private CanvasGroup blinkCanvasGroup;

        private struct RendererMaterialCache
        {
            public Renderer renderer;
            public Material[] materials;
        }

        public GameObject EntranceNormal => entranceNormal;
        public GameObject EntranceSealed => entranceSealed;
        public GameObject DoorwayHidden => doorwayHidden;
        public GameObject DoorwayRevealed => doorwayRevealed;
        public GameObject HiddenRoomRoot => hiddenRoomRoot;
        public Material EyeDevilMaterial => eyeDevilMaterial;
        public bool HasTransformed => hasTransformed;
        public bool HasTriggeredRoomEntry => hasTriggeredRoomEntry;

        public void BindPlayer(Transform playerTransform)
        {
            if (playerTransform != null)
            {
                player = playerTransform.GetComponent<FirstPersonController>()
                         ?? playerTransform.GetComponentInParent<FirstPersonController>();
            }
        }

        public void BindPlayer(FirstPersonController playerController)
        {
            player = playerController;
            if (player != null && playerCamera == null)
            {
                playerCamera = player.PlayerCamera;
            }
        }

        public void BindRoomContext(Camera camera, CorrectArrowController arrow)
        {
            playerCamera = camera;
            arrowController = arrow;
            if (arrow != null)
            {
                arrowTarget = arrow.ArrowTransform;
            }
        }

        public void Configure(
            GameObject normalEntrance,
            GameObject sealedEntrance,
            GameObject hiddenStemDoorway,
            GameObject revealedStemDoorway,
            GameObject roomRoot)
        {
            entranceNormal = normalEntrance;
            entranceSealed = sealedEntrance;
            doorwayHidden = hiddenStemDoorway;
            doorwayRevealed = revealedStemDoorway;
            hiddenRoomRoot = roomRoot;
            CaptureNormalState();
        }

        public void Configure(
            GameObject hiddenStemDoorway,
            GameObject revealedStemDoorway,
            GameObject roomRoot)
        {
            Configure(null, null, hiddenStemDoorway, revealedStemDoorway, roomRoot);
        }

        public override void CaptureNormalState()
        {
            StopBlinkRoutine();
            hasTransformed = false;
            hasTriggeredRoomEntry = false;
            lookTimer = 0f;

            RestoreOriginalMaterials();
            ApplyNormalState();
            ResetEyelids();
        }

        protected override void OnActivate()
        {
            StopBlinkRoutine();
            hasTransformed = false;
            hasTriggeredRoomEntry = false;
            lookTimer = 0f;

            // Ban đầu khi kích hoạt, căn phòng VẪN BÌNH THƯỜNG
            RestoreOriginalMaterials();
            ApplyNormalState();
            ResetEyelids();

            ResolveReferences();
        }

        protected override void OnResetAnomaly()
        {
            StopBlinkRoutine();
            hasTransformed = false;
            hasTriggeredRoomEntry = false;
            lookTimer = 0f;

            RestoreOriginalMaterials();
            ApplyNormalState();
            ResetEyelids();
        }

        private void Update()
        {
            // Chỉ kiểm tra khi anomaly đang kích hoạt và chưa biến đổi
            if (!IsActive || hasTransformed)
            {
                return;
            }

            ResolveReferences();

            // Người chơi nhìn vào mũi tên để mở phòng ẩn
            if (IsLookingAtArrow())
            {
                lookTimer += Time.deltaTime;
                if (lookTimer >= requiredLookDuration)
                {
                    TriggerTransformation();
                }
            }
            else
            {
                lookTimer = 0f;
            }
        }

        [ContextMenu("Test Trigger Transformation")]
        public void TriggerTransformation()
        {
            hasTransformed = true;

            // 1. Bịt kín cửa ra vào phòng bằng tường
            if (entranceNormal != null) entranceNormal.SetActive(false);
            if (entranceSealed != null) entranceSealed.SetActive(true);

            // 2. Mở lối đi bí mật và hiện căn phòng ẩn lên
            if (doorwayHidden != null) doorwayHidden.SetActive(false);
            if (doorwayRevealed != null) doorwayRevealed.SetActive(true);
            if (hiddenRoomRoot != null)
            {
                hiddenRoomRoot.SetActive(true);
                CacheOriginalMaterials();
            }

            SetupRoomEntryTrigger();

            Debug.Log($"[HiddenRoomAnomaly] Player looked at arrow! Room transformed: Entrance sealed, Hidden Room revealed on '{name}'.", this);
        }

        /// <summary>
        /// Được gọi khi người chơi bước vào RoomEntryTrigger của phòng ẩn.
        /// </summary>
        public void OnPlayerEnteredHiddenRoom(FirstPersonController enteringPlayer)
        {
            if (!IsActive || !hasTransformed || hasTriggeredRoomEntry)
            {
                return;
            }

            hasTriggeredRoomEntry = true;
            if (roomEntryTrigger != null)
            {
                roomEntryTrigger.enabled = false;
            }

            if (player == null && enteringPlayer != null)
            {
                player = enteringPlayer;
            }

            hasTriggeredRoomEntry = true;
            Debug.Log($"[HiddenRoomAnomaly] Player entered Hidden Room! Starting blink & Eye Devil sequence.", this);

            StopBlinkRoutine();
            blinkRoutine = StartCoroutine(BlinkAndDevourSequence());
        }

        private IEnumerator BlinkAndDevourSequence()
        {
            EnsureBlinkOverlay();

            // 1. Mí mắt khép lại (Blink close)
            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, blinkCloseDuration);
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = t * t; // Smooth acceleration
                SetEyelidsProgress(eased);
                yield return null;
            }

            SetEyelidsProgress(1f); // Mắt đã nhắm hoàn toàn (màn hình tối đen)

            // 2. Trong lúc nhắm mắt: Thay đổi toàn bộ material của phòng ẩn thành Material Eye Devil
            ApplyEyeDevilMaterials();

            if (blinkAudioClip != null)
            {
                AudioSource.PlayClipAtPoint(blinkAudioClip, playerCamera != null ? playerCamera.transform.position : transform.position);
            }

            // Giữ mắt nhắm một khoảng ngắn
            if (blinkHoldDuration > 0f)
            {
                yield return new WaitForSecondsRealtime(blinkHoldDuration);
            }

            // 3. Mở mí mắt ra (Blink open)
            elapsed = 0f;
            duration = Mathf.Max(0.01f, blinkOpenDuration);
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - (1f - t) * (1f - t); // Deceleration
                SetEyelidsProgress(1f - eased);
                yield return null;
            }

            SetEyelidsProgress(0f); // Mắt đã mở hoàn toàn
            ResetEyelids();

            // 4. Chờ một khoảnh khắc ngắn người chơi nhìn thấy cảnh tượng kinh dị toàn phòng đầy mắt
            if (delayBeforeDeath > 0f)
            {
                yield return new WaitForSecondsRealtime(delayBeforeDeath);
            }

            // 5. Player Death
            TriggerPlayerDeath();
            blinkRoutine = null;
        }

        private void ApplyEyeDevilMaterials()
        {
            if (eyeDevilMaterial == null)
            {
                Debug.LogWarning("[HiddenRoomAnomaly] Chưa gán 'Eye Devil Material'! Vui lòng kéo Material vào Inspector.", this);
                return;
            }

            if (hiddenRoomRoot == null)
            {
                return;
            }

            // Đảm bảo đã lưu material gốc trước khi đổi
            CacheOriginalMaterials();

            Renderer[] renderers = hiddenRoomRoot.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                if (r == null) continue;

                Material[] eyeMats = new Material[r.sharedMaterials.Length];
                for (int m = 0; m < eyeMats.Length; m++)
                {
                    eyeMats[m] = eyeDevilMaterial;
                }
                r.sharedMaterials = eyeMats;
            }

            // Đổi cả trên doorwayRevealed nếu có
            if (doorwayRevealed != null)
            {
                Renderer[] doorRenderers = doorwayRevealed.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < doorRenderers.Length; i++)
                {
                    Renderer r = doorRenderers[i];
                    if (r == null) continue;

                    Material[] eyeMats = new Material[r.sharedMaterials.Length];
                    for (int m = 0; m < eyeMats.Length; m++)
                    {
                        eyeMats[m] = eyeDevilMaterial;
                    }
                    r.sharedMaterials = eyeMats;
                }
            }

            Debug.Log("[HiddenRoomAnomaly] ALL materials in Hidden Room changed to Eye Devil Material!", this);
        }

        private void CacheOriginalMaterials()
        {
            if (cachedMaterials.Count > 0)
            {
                return; // Đã lưu rồi, không ghi đè
            }

            if (hiddenRoomRoot != null)
            {
                Renderer[] renderers = hiddenRoomRoot.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < renderers.Length; i++)
                {
                    Renderer r = renderers[i];
                    if (r != null)
                    {
                        cachedMaterials.Add(new RendererMaterialCache
                        {
                            renderer = r,
                            materials = r.sharedMaterials
                        });
                    }
                }
            }

            if (doorwayRevealed != null)
            {
                Renderer[] doorRenderers = doorwayRevealed.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < doorRenderers.Length; i++)
                {
                    Renderer r = doorRenderers[i];
                    if (r != null)
                    {
                        cachedMaterials.Add(new RendererMaterialCache
                        {
                            renderer = r,
                            materials = r.sharedMaterials
                        });
                    }
                }
            }
        }

        private void RestoreOriginalMaterials()
        {
            for (int i = 0; i < cachedMaterials.Count; i++)
            {
                RendererMaterialCache cache = cachedMaterials[i];
                if (cache.renderer != null && cache.materials != null)
                {
                    cache.renderer.sharedMaterials = cache.materials;
                }
            }
            cachedMaterials.Clear();
        }

        private void TriggerPlayerDeath()
        {
            ResolveReferences();

            if (deathPresentation == null)
            {
                deathPresentation = GetComponentInChildren<AnomalyDeathPresentation>(true);
                if (deathPresentation == null)
                {
                    // Tự động tạo component Presentation chế độ Suffocation nếu chưa có
                    deathPresentation = gameObject.AddComponent<AnomalyDeathPresentation>();
                }
            }

            if (deathController == null)
            {
                deathController = FindAnyObjectByType<PlayerDeathController>();
            }

            if (deathController != null)
            {
                bool accepted = deathController.TryBeginDeath(this, deathPresentation);
                Debug.Log($"[HiddenRoomAnomaly] TriggerPlayerDeath called: accepted={accepted}", this);
            }
            else
            {
                Debug.LogError("[HiddenRoomAnomaly] PlayerDeathController not found in scene!", this);
            }
        }

        [ContextMenu("Test Reset Normal State")]
        public void ApplyNormalState()
        {
            // 1. Mở lại cửa ra vào phòng bình thường
            if (entranceNormal != null) entranceNormal.SetActive(true);
            if (entranceSealed != null) entranceSealed.SetActive(false);

            // 2. Che giấu lối đi và ẩn căn phòng ẩn đi
            if (doorwayHidden != null) doorwayHidden.SetActive(true);
            if (doorwayRevealed != null) doorwayRevealed.SetActive(false);
            if (hiddenRoomRoot != null) hiddenRoomRoot.SetActive(false);

            // 3. Tắt trigger vào phòng ẩn
            if (roomEntryTrigger != null) roomEntryTrigger.enabled = false;
        }

        private void StopBlinkRoutine()
        {
            if (blinkRoutine != null)
            {
                StopCoroutine(blinkRoutine);
                blinkRoutine = null;
            }
        }

        #region Blink Eyelids UI Overlay

        private void EnsureBlinkOverlay()
        {
            if (blinkCanvas != null)
            {
                blinkCanvas.gameObject.SetActive(true);
                return;
            }

            GameObject canvasGo = new GameObject("HiddenRoom_BlinkOverlay");
            canvasGo.transform.SetParent(transform, false);

            blinkCanvas = canvasGo.AddComponent<Canvas>();
            blinkCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            blinkCanvas.sortingOrder = 32500; // Nằm trên hầu hết gameplay UI
            canvasGo.AddComponent<CanvasScaler>();

            blinkCanvasGroup = canvasGo.AddComponent<CanvasGroup>();
            blinkCanvasGroup.blocksRaycasts = false;
            blinkCanvasGroup.interactable = false;

            // Mí mắt trên (Top Eyelid)
            GameObject topGo = new GameObject("TopEyelid");
            topGo.transform.SetParent(canvasGo.transform, false);
            Image topImg = topGo.AddComponent<Image>();
            topImg.color = Color.black;
            topImg.raycastTarget = false;
            topEyelid = topGo.GetComponent<RectTransform>();
            topEyelid.anchorMin = new Vector2(0f, 1f);
            topEyelid.anchorMax = new Vector2(1f, 1f);
            topEyelid.pivot = new Vector2(0.5f, 1f);
            topEyelid.anchoredPosition = Vector2.zero;
            topEyelid.sizeDelta = new Vector2(0f, 0f);

            // Mí mắt dưới (Bottom Eyelid)
            GameObject bottomGo = new GameObject("BottomEyelid");
            bottomGo.transform.SetParent(canvasGo.transform, false);
            Image bottomImg = bottomGo.AddComponent<Image>();
            bottomImg.color = Color.black;
            bottomImg.raycastTarget = false;
            bottomEyelid = bottomGo.GetComponent<RectTransform>();
            bottomEyelid.anchorMin = new Vector2(0f, 0f);
            bottomEyelid.anchorMax = new Vector2(1f, 0f);
            bottomEyelid.pivot = new Vector2(0.5f, 0f);
            bottomEyelid.anchoredPosition = Vector2.zero;
            bottomEyelid.sizeDelta = new Vector2(0f, 0f);
        }

        private void SetEyelidsProgress(float progress)
        {
            EnsureBlinkOverlay();

            progress = Mathf.Clamp01(progress);
            float screenHeight = Screen.height > 0 ? Screen.height : 1080f;
            float halfHeight = screenHeight * 0.52f; // Khép kín hoàn toàn ở giữa

            float currentHeight = halfHeight * progress;

            if (topEyelid != null)
            {
                topEyelid.sizeDelta = new Vector2(0f, currentHeight);
            }

            if (bottomEyelid != null)
            {
                bottomEyelid.sizeDelta = new Vector2(0f, currentHeight);
            }
        }

        private void ResetEyelids()
        {
            if (topEyelid != null) topEyelid.sizeDelta = Vector2.zero;
            if (bottomEyelid != null) bottomEyelid.sizeDelta = Vector2.zero;
            if (blinkCanvas != null) blinkCanvas.gameObject.SetActive(false);
        }

        #endregion

        #region Trigger & Entry Detection

        private void SetupRoomEntryTrigger()
        {
            if (roomEntryTrigger != null)
            {
                roomEntryTrigger.isTrigger = true;
                roomEntryTrigger.enabled = true;
                HiddenRoomTriggerProxy proxy = roomEntryTrigger.GetComponent<HiddenRoomTriggerProxy>();
                if (proxy == null)
                {
                    proxy = roomEntryTrigger.gameObject.AddComponent<HiddenRoomTriggerProxy>();
                }
                proxy.Initialize(this);
            }
            else
            {
                Debug.LogWarning("[HiddenRoomAnomaly] 'Room Entry Trigger' chưa được gán trên Inspector!", this);
            }
        }

        #endregion

        private void ResolveReferences()
        {
            if (player == null)
            {
                player = FindAnyObjectByType<FirstPersonController>();
            }

            if (playerCamera == null)
            {
                playerCamera = player != null ? player.PlayerCamera : Camera.main;
            }

            if (arrowController == null)
            {
                arrowController = GetComponentInParent<CorrectArrowController>();
            }

            if (arrowTarget == null && arrowController != null)
            {
                arrowTarget = arrowController.ArrowTransform;
            }

            if (deathController == null)
            {
                deathController = FindAnyObjectByType<PlayerDeathController>();
            }

            if (deathPresentation == null)
            {
                deathPresentation = GetComponentInChildren<AnomalyDeathPresentation>(true);
            }
        }

        private bool IsLookingAtArrow()
        {
            if (playerCamera == null)
            {
                return false;
            }

            Transform target = arrowTarget;
            if (target == null)
            {
                return false;
            }

            Renderer renderer = target.GetComponentInChildren<Renderer>();
            Vector3 targetPoint = renderer != null ? renderer.bounds.center : target.position;

            // 1. Kiểm tra vị trí mục tiêu trong viewport của Camera
            Vector3 viewportPoint = playerCamera.WorldToViewportPoint(targetPoint);
            if (viewportPoint.z <= 0f || viewportPoint.x < 0f || viewportPoint.x > 1f ||
                viewportPoint.y < 0f || viewportPoint.y > 1f)
            {
                return false;
            }

            // 2. Nếu yêu cầu nhìn về hướng trung tâm màn hình
            if (requireCenterFocus)
            {
                Vector2 centerOffset = new Vector2(viewportPoint.x - 0.5f, viewportPoint.y - 0.5f);
                if (centerOffset.sqrMagnitude > focusViewportRadius * focusViewportRadius)
                {
                    return false;
                }
            }

            // 3. Raycast kiểm tra có bị vật cản che khuất không
            Vector3 origin = playerCamera.transform.position;
            Vector3 direction = targetPoint - origin;
            float distance = direction.magnitude;
            if (distance <= Mathf.Epsilon)
            {
                return true;
            }

            float rayDistance = Mathf.Max(0f, distance - 0.08f);
            if (rayDistance <= Mathf.Epsilon || !Physics.Raycast(
                    origin,
                    direction / distance,
                    out RaycastHit hit,
                    rayDistance,
                    obstructionMask,
                    QueryTriggerInteraction.Ignore))
            {
                return true;
            }

            return hit.transform == target || hit.transform.IsChildOf(target);
        }
    }

    /// <summary>
    /// Component proxy gắn vào Collider Trigger để bắt sự kiện người chơi bước vào phòng ẩn.
    /// </summary>
    public sealed class HiddenRoomTriggerProxy : MonoBehaviour
    {
        private HiddenRoomAnomaly anomaly;

        public void Initialize(HiddenRoomAnomaly owner)
        {
            anomaly = owner;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (anomaly == null) return;

            FirstPersonController player = other.GetComponentInParent<FirstPersonController>();
            if (player != null)
            {
                anomaly.OnPlayerEnteredHiddenRoom(player);
            }
        }
    }
}
