using System.Collections;
using UnityEngine;
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
    /// - Khi reset / kết thúc vòng: Trở lại trạng thái bình thường.
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

        private float lookTimer;
        private bool hasTransformed;

        public GameObject EntranceNormal => entranceNormal;
        public GameObject EntranceSealed => entranceSealed;
        public GameObject DoorwayHidden => doorwayHidden;
        public GameObject DoorwayRevealed => doorwayRevealed;
        public GameObject HiddenRoomRoot => hiddenRoomRoot;
        public bool HasTransformed => hasTransformed;

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
            hasTransformed = false;
            lookTimer = 0f;
            ApplyNormalState();
        }

        protected override void OnActivate()
        {
            hasTransformed = false;
            lookTimer = 0f;

            // Ban đầu khi kích hoạt, căn phòng VẪN BÌNH THƯỜNG
            ApplyNormalState();

            ResolveReferences();
        }

        protected override void OnResetAnomaly()
        {
            hasTransformed = false;
            lookTimer = 0f;
            ApplyNormalState();
        }

        private void Update()
        {
            // Chỉ kiểm tra khi anomaly đang kích hoạt và phòng chưa bị biến đổi
            if (!IsActive || hasTransformed)
            {
                return;
            }

            ResolveReferences();

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
            if (hiddenRoomRoot != null) hiddenRoomRoot.SetActive(true);

            Debug.Log($"[HiddenRoomAnomaly] Player looked at arrow! Room transformed: Entrance sealed, Hidden Room revealed on '{name}'.", this);
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
        }

        private void ResolveReferences()
        {
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }

            if (arrowController == null)
            {
                arrowController = GetComponentInParent<CorrectArrowController>();
            }

            if (arrowTarget == null && arrowController != null)
            {
                arrowTarget = arrowController.ArrowTransform;
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
}
