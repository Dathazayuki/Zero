using UnityEngine;
using UnityEngine.Rendering;
using Zero.EnvironmentLoop;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class RotatingArrowAnomaly : AnomalyBehaviour
    {
        [Header("Target References")]
        [Tooltip("Controller quản lý mũi tên phòng (nếu trống sẽ tự tìm).")]
        [SerializeField] private CorrectArrowController arrowController;

        [Tooltip("Gương phẳng trong phòng (nếu trống sẽ tự tìm).")]
        [SerializeField] private PlanarMirrorRenderer planarMirror;

        [Tooltip("Transform mũi tên giả xoay tròn được tạo sẵn trong scene / prefab.")]
        [SerializeField] private Transform fakeArrowTransform;

        [Tooltip("Camera chính của người chơi (nếu trống sẽ tự bind từ phòng).")]
        [SerializeField] private Camera playerCamera;

        [Header("Layer Settings")]
        [Tooltip("Layer để mũi tên thật chỉ hiển thị qua gương và vô hình với mắt thường.")]
        [SerializeField] private string reflectionOnlyLayerName = "MirrorReflectionOnly";

        [Tooltip("Tên layer riêng cho mũi tên giả nếu muốn loại trừ bằng LayerMask (tùy chọn, để trống sẽ dùng Default).")]
        [SerializeField] private string fakeArrowLayerName = "";

        [Header("Rotation Settings")]
        [Tooltip("Trục xoay của mũi tên giả (mặc định trục Z / forward theo local space).")]
        [SerializeField] private Vector3 rotationAxis = new Vector3(0f, 0f, 1f);

        [Tooltip("Tốc độ xoay liên tục (độ / giây).")]
        [SerializeField, Min(0f)] private float rotationSpeed = 360f;

        [Tooltip("Xoay theo chiều kim đồng hồ (true) hoặc ngược chiều (false).")]
        [SerializeField] private bool rotateClockwise = true;

        private Transform realArrowTransform;
        private Transform[] realArrowHierarchy;
        private int[] realArrowOriginalLayers;
        private Renderer[] fakeArrowRenderers;
        private bool callbacksRegistered;
        private LayerMask originalMirrorMask;
        private bool mirrorMaskModified;

        public Transform RealArrow => realArrowTransform;
        public Transform FakeArrow => fakeArrowTransform;
        public float RotationSpeed => rotationSpeed;

        public void BindRoomContext(CorrectArrowController arrowCtrl, Camera cam = null)
        {
            if (arrowCtrl != null) arrowController = arrowCtrl;
            if (cam != null) playerCamera = cam;
        }

        public void Configure(
            CorrectArrowController controller,
            PlanarMirrorRenderer mirror,
            Transform fakeArrow,
            AnomalyData anomalyData = null)
        {
            if (controller != null) arrowController = controller;
            if (mirror != null) planarMirror = mirror;
            if (fakeArrow != null) fakeArrowTransform = fakeArrow;
            if (anomalyData != null) Initialize(anomalyData);
        }

        public override void CaptureNormalState()
        {
            ResolveReferences();
            CaptureRealArrowLayers();

            if (fakeArrowTransform != null)
            {
                fakeArrowRenderers = fakeArrowTransform.GetComponentsInChildren<Renderer>(true);
                fakeArrowTransform.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (!IsActive || fakeArrowTransform == null || !fakeArrowTransform.gameObject.activeInHierarchy)
            {
                return;
            }

            float directionSign = rotateClockwise ? -1f : 1f;
            fakeArrowTransform.Rotate(rotationAxis, directionSign * rotationSpeed * Time.deltaTime, Space.Self);
        }

        protected override void OnActivate()
        {
            ResolveReferences();
            CaptureRealArrowLayers();

            // 1. Chuyển mũi tên thật sang layer MirrorReflectionOnly (chỉ nhìn thấy qua gương)
            int reflectionLayer = LayerMask.NameToLayer(reflectionOnlyLayerName);
            if (reflectionLayer >= 0 && realArrowHierarchy != null)
            {
                for (int i = 0; i < realArrowHierarchy.Length; i++)
                {
                    if (realArrowHierarchy[i] != null)
                    {
                        realArrowHierarchy[i].gameObject.layer = reflectionLayer;
                    }
                }
            }
            else
            {
                Debug.LogWarning($"ROTATING_ARROW_LAYER_MISSING: Layer '{reflectionOnlyLayerName}' không tồn tại trong Tags & Layers!", this);
            }

            // 2. Kích hoạt mũi tên giả xoay vòng
            if (fakeArrowTransform != null)
            {
                if (realArrowTransform != null)
                {
                    fakeArrowTransform.position = realArrowTransform.position;
                    fakeArrowTransform.rotation = realArrowTransform.rotation;
                }

                if (!string.IsNullOrEmpty(fakeArrowLayerName))
                {
                    int fakeLayer = LayerMask.NameToLayer(fakeArrowLayerName);
                    if (fakeLayer >= 0)
                    {
                        SetHierarchyLayer(fakeArrowTransform, fakeLayer);
                        ExcludeLayerFromMirror(fakeLayer);
                    }
                }

                fakeArrowRenderers = fakeArrowTransform.GetComponentsInChildren<Renderer>(true);
                fakeArrowTransform.gameObject.SetActive(true);
            }
            else
            {
                Debug.LogWarning($"ROTATING_ARROW_NO_FAKE_ARROW: Chưa gán Fake Arrow Transform trên {name}!", this);
            }

            // 3. Đăng ký callback ẩn mũi tên giả khi gương phản chiếu
            RegisterCameraCallbacks();

            Debug.Log($"ROTATING_ARROW_ACTIVATED: anomaly={name}, realArrowLayer={reflectionOnlyLayerName}, speed={rotationSpeed}", this);
        }

        protected override void OnResetAnomaly()
        {
            UnregisterCameraCallbacks();
            RestoreMirrorMask();

            // 1. Trả lại layer gốc cho mũi tên thật
            RestoreRealArrowLayers();

            // 2. Tắt mũi tên giả
            if (fakeArrowTransform != null)
            {
                SetFakeArrowRenderersVisible(true);
                fakeArrowTransform.gameObject.SetActive(false);
            }
        }

        private void OnDisable()
        {
            OnResetAnomaly();
        }

        private void OnDestroy()
        {
            UnregisterCameraCallbacks();
            RestoreMirrorMask();
            RestoreRealArrowLayers();
        }

        private void ResolveReferences()
        {
            if (arrowController == null)
            {
                arrowController = GetComponentInParent<RoomAnomalyController>()?.CorrectArrow
                               ?? GetComponentInParent<CorrectArrowController>()
                               ?? FindAnyObjectByType<CorrectArrowController>();
            }

            if (arrowController != null && realArrowTransform == null)
            {
                realArrowTransform = arrowController.ArrowTransform;
            }

            if (planarMirror == null)
            {
                planarMirror = GetComponentInParent<RoomAnomalyController>()?.GetComponentInChildren<PlanarMirrorRenderer>(true)
                            ?? GetComponentInChildren<PlanarMirrorRenderer>(true)
                            ?? FindAnyObjectByType<PlanarMirrorRenderer>();
            }

            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }
        }

        private void CaptureRealArrowLayers()
        {
            if (realArrowTransform == null)
            {
                return;
            }

            realArrowHierarchy = realArrowTransform.GetComponentsInChildren<Transform>(true);
            realArrowOriginalLayers = new int[realArrowHierarchy.Length];
            for (int i = 0; i < realArrowHierarchy.Length; i++)
            {
                realArrowOriginalLayers[i] = realArrowHierarchy[i].gameObject.layer;
            }
        }

        private void RestoreRealArrowLayers()
        {
            if (realArrowHierarchy == null || realArrowOriginalLayers == null)
            {
                return;
            }

            int count = Mathf.Min(realArrowHierarchy.Length, realArrowOriginalLayers.Length);
            for (int i = 0; i < count; i++)
            {
                if (realArrowHierarchy[i] != null)
                {
                    realArrowHierarchy[i].gameObject.layer = realArrowOriginalLayers[i];
                }
            }
        }

        private void ExcludeLayerFromMirror(int layer)
        {
            if (planarMirror == null || layer <= 0) return;
            if (!mirrorMaskModified)
            {
                originalMirrorMask = planarMirror.ReflectionMask;
            }
            planarMirror.SetReflectionMask(originalMirrorMask & ~(1 << layer));
            mirrorMaskModified = true;
        }

        private void RestoreMirrorMask()
        {
            if (mirrorMaskModified && planarMirror != null)
            {
                planarMirror.SetReflectionMask(originalMirrorMask);
                mirrorMaskModified = false;
            }
        }

        private static void SetHierarchyLayer(Transform root, int layer)
        {
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                all[i].gameObject.layer = layer;
            }
        }

        private void RegisterCameraCallbacks()
        {
            if (callbacksRegistered) return;
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
            Camera.onPreCull += OnPreCullCamera;
            Camera.onPostRender += OnPostRenderCamera;
            callbacksRegistered = true;
        }

        private void UnregisterCameraCallbacks()
        {
            if (!callbacksRegistered) return;
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
            Camera.onPreCull -= OnPreCullCamera;
            Camera.onPostRender -= OnPostRenderCamera;
            callbacksRegistered = false;
        }

        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera cam)
        {
            HandlePreCameraRender(cam);
        }

        private void OnEndCameraRendering(ScriptableRenderContext context, Camera cam)
        {
            HandlePostCameraRender(cam);
        }

        private void OnPreCullCamera(Camera cam)
        {
            if (GraphicsSettings.currentRenderPipeline == null)
            {
                HandlePreCameraRender(cam);
            }
        }

        private void OnPostRenderCamera(Camera cam)
        {
            if (GraphicsSettings.currentRenderPipeline == null)
            {
                HandlePostCameraRender(cam);
            }
        }

        private void HandlePreCameraRender(Camera cam)
        {
            if (!IsActive || planarMirror == null || planarMirror.MirrorCamera == null) return;
            if (cam == planarMirror.MirrorCamera)
            {
                SetFakeArrowRenderersVisible(false);
            }
        }

        private void HandlePostCameraRender(Camera cam)
        {
            if (!IsActive || planarMirror == null || planarMirror.MirrorCamera == null) return;
            if (cam == planarMirror.MirrorCamera)
            {
                SetFakeArrowRenderersVisible(true);
            }
        }

        private void SetFakeArrowRenderersVisible(bool visible)
        {
            if (fakeArrowRenderers == null) return;
            for (int i = 0; i < fakeArrowRenderers.Length; i++)
            {
                if (fakeArrowRenderers[i] != null)
                {
                    fakeArrowRenderers[i].enabled = visible;
                }
            }
        }
    }
}
