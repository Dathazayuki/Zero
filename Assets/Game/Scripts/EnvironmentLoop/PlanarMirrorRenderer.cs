using UnityEngine;

namespace Zero.EnvironmentLoop
{
    public sealed class PlanarMirrorRenderer : MonoBehaviour
    {
        [SerializeField] private Transform mirrorPlane;
        [SerializeField] private Renderer mirrorSurfaceRenderer;
        [SerializeField] private Camera sourceCamera;
        [SerializeField] private Camera mirrorCamera;
        [SerializeField] private LayerMask reflectionMask = ~0;
        [SerializeField, Range(128, 2048)] private int textureWidth = 768;
        [SerializeField, Range(128, 2048)] private int textureHeight = 768;
        [SerializeField, Min(0f)] private float clipPlaneOffset = 0.03f;
        [SerializeField] private bool useHdr = true;
        [SerializeField] private bool renderOnlyWhenVisible = true;
        [SerializeField, Min(1f)] private float maximumRenderDistance = 30f;
        [SerializeField] private bool overrideMaterialUvSettings;
        [SerializeField, Range(-180f, 180f)] private float uvRotationDegrees;
        [SerializeField] private bool flipTextureX;
        [SerializeField] private bool flipTextureY;

        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        private static readonly int UvRotationId = Shader.PropertyToID("_UVRotationDegrees");
        private static readonly int FlipXId = Shader.PropertyToID("_FlipX");
        private static readonly int FlipYId = Shader.PropertyToID("_FlipY");
        private RenderTexture reflectionTexture;
        private MaterialPropertyBlock propertyBlock;
        private Matrix4x4 originalProjection;

        public Camera MirrorCamera => mirrorCamera;
        public Renderer SurfaceRenderer => mirrorSurfaceRenderer;
        public Transform MirrorPlane => mirrorPlane != null ? mirrorPlane : transform;
        public bool IsConfigured => mirrorCamera != null && mirrorSurfaceRenderer != null && MirrorPlane != null;

        public void BindSourceCamera(Camera camera)
        {
            sourceCamera = camera;
            ConfigureCameraAndTexture();
        }

        public void SetReflectionMask(LayerMask mask)
        {
            reflectionMask = mask;
            if (mirrorCamera != null)
            {
                mirrorCamera.cullingMask = reflectionMask;
            }
        }

        public void ClearReflectionTexture()
        {
            if (reflectionTexture == null || !reflectionTexture.IsCreated()) return;
            RenderTexture previous = RenderTexture.active;
            try
            {
                RenderTexture.active = reflectionTexture;
                GL.Clear(true, true, Color.black);
            }
            finally { RenderTexture.active = previous; }
        }

        private void Awake()
        {
            ResolveReferences();
            ConfigureCameraAndTexture();
        }

        private void OnEnable()
        {
            ResolveReferences();
            ConfigureCameraAndTexture();
        }

        private void LateUpdate()
        {
            if (sourceCamera == null || mirrorCamera == null || MirrorPlane == null)
            {
                return;
            }

            bool withinRenderDistance = Vector3.SqrMagnitude(
                sourceCamera.transform.position - MirrorPlane.position) <=
                maximumRenderDistance * maximumRenderDistance;
            bool shouldRender = withinRenderDistance &&
                                (!renderOnlyWhenVisible ||
                                 (mirrorSurfaceRenderer != null && mirrorSurfaceRenderer.isVisible));
            mirrorCamera.enabled = shouldRender;
            if (!shouldRender)
            {
                return;
            }

            if (reflectionTexture == null || reflectionTexture.width != textureWidth ||
                reflectionTexture.height != textureHeight)
            {
                ConfigureCameraAndTexture();
            }

            UpdateReflectionCamera();
        }

        private void OnDisable()
        {
            if (mirrorCamera != null)
            {
                mirrorCamera.enabled = false;
            }
        }

        private void OnDestroy()
        {
            ReleaseTexture();
        }

        private void ResolveReferences()
        {
            mirrorPlane ??= transform;
            mirrorSurfaceRenderer ??= GetComponentInChildren<Renderer>(true);
            sourceCamera ??= Camera.main;

            if (mirrorCamera == null)
            {
                Camera[] cameras = GetComponentsInChildren<Camera>(true);
                for (int i = 0; i < cameras.Length; i++)
                {
                    if (cameras[i] != sourceCamera)
                    {
                        mirrorCamera = cameras[i];
                        break;
                    }
                }
            }
        }

        private void ConfigureCameraAndTexture()
        {
            if (sourceCamera == null || mirrorCamera == null || mirrorSurfaceRenderer == null)
            {
                return;
            }

            if (reflectionTexture == null || reflectionTexture.width != textureWidth ||
                reflectionTexture.height != textureHeight)
            {
                ReleaseTexture();
                RenderTextureFormat format = useHdr
                    ? RenderTextureFormat.DefaultHDR
                    : RenderTextureFormat.Default;
                reflectionTexture = new RenderTexture(textureWidth, textureHeight, 24, format)
                {
                    name = $"PlanarMirror_{name}",
                    hideFlags = HideFlags.DontSave,
                    useMipMap = false,
                    autoGenerateMips = false,
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear
                };
                reflectionTexture.Create();
            }

            mirrorCamera.CopyFrom(sourceCamera);
            mirrorCamera.cameraType = CameraType.Reflection;
            mirrorCamera.targetTexture = reflectionTexture;
            mirrorCamera.cullingMask = reflectionMask;
            mirrorCamera.stereoTargetEye = StereoTargetEyeMask.None;
            mirrorCamera.enabled = true;
            originalProjection = sourceCamera.projectionMatrix;

            PlanarMirrorCameraCulling cullingHook = mirrorCamera.GetComponent<PlanarMirrorCameraCulling>();
            if (cullingHook == null)
            {
                cullingHook = mirrorCamera.gameObject.AddComponent<PlanarMirrorCameraCulling>();
            }

            cullingHook.enabled = true;
            ApplyTextureToSurface();
        }

        private void UpdateReflectionCamera()
        {
            Transform plane = MirrorPlane;
            Vector3 planePosition = plane.position;
            Vector3 planeNormal = plane.forward.normalized;
            if (Vector3.Dot(sourceCamera.transform.position - planePosition, planeNormal) < 0f)
            {
                planeNormal = -planeNormal;
            }

            Vector3 reflectedPosition = ReflectPoint(sourceCamera.transform.position, planePosition, planeNormal);
            Vector3 reflectedForward = Vector3.Reflect(sourceCamera.transform.forward, planeNormal);
            Vector3 reflectedUp = Vector3.Reflect(sourceCamera.transform.up, planeNormal);
            mirrorCamera.transform.SetPositionAndRotation(
                reflectedPosition,
                Quaternion.LookRotation(reflectedForward, reflectedUp));

            Vector4 worldPlane = new Vector4(
                planeNormal.x,
                planeNormal.y,
                planeNormal.z,
                -Vector3.Dot(planeNormal, planePosition));
            Matrix4x4 reflectionMatrix = CalculateReflectionMatrix(worldPlane);
            mirrorCamera.worldToCameraMatrix = sourceCamera.worldToCameraMatrix * reflectionMatrix;

            mirrorCamera.fieldOfView = sourceCamera.fieldOfView;
            mirrorCamera.aspect = sourceCamera.aspect;
            mirrorCamera.nearClipPlane = sourceCamera.nearClipPlane;
            mirrorCamera.farClipPlane = sourceCamera.farClipPlane;
            mirrorCamera.orthographic = sourceCamera.orthographic;
            mirrorCamera.orthographicSize = sourceCamera.orthographicSize;
            mirrorCamera.cullingMask = reflectionMask;

            originalProjection = sourceCamera.projectionMatrix;
            Vector4 clipPlane = CameraSpacePlane(
                mirrorCamera,
                planePosition,
                planeNormal,
                clipPlaneOffset);
            mirrorCamera.projectionMatrix = originalProjection;
            mirrorCamera.projectionMatrix = mirrorCamera.CalculateObliqueMatrix(clipPlane);
        }

        private void ApplyTextureToSurface()
        {
            propertyBlock ??= new MaterialPropertyBlock();
            mirrorSurfaceRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetTexture(BaseMapId, reflectionTexture);
            propertyBlock.SetTexture(MainTexId, reflectionTexture);

            float resolvedUvRotation = uvRotationDegrees;
            float resolvedFlipX = flipTextureX ? 1f : 0f;
            float resolvedFlipY = flipTextureY ? 1f : 0f;
            Material sharedMaterial = mirrorSurfaceRenderer.sharedMaterial;
            if (!overrideMaterialUvSettings && sharedMaterial != null)
            {
                if (sharedMaterial.HasProperty(UvRotationId))
                {
                    resolvedUvRotation = sharedMaterial.GetFloat(UvRotationId);
                }

                if (sharedMaterial.HasProperty(FlipXId))
                {
                    resolvedFlipX = sharedMaterial.GetFloat(FlipXId);
                }

                if (sharedMaterial.HasProperty(FlipYId))
                {
                    resolvedFlipY = sharedMaterial.GetFloat(FlipYId);
                }
            }

            propertyBlock.SetFloat(UvRotationId, resolvedUvRotation);
            propertyBlock.SetFloat(FlipXId, resolvedFlipX);
            propertyBlock.SetFloat(FlipYId, resolvedFlipY);
            mirrorSurfaceRenderer.SetPropertyBlock(propertyBlock);
        }

        private void ReleaseTexture()
        {
            if (reflectionTexture == null)
            {
                return;
            }

            if (mirrorCamera != null && mirrorCamera.targetTexture == reflectionTexture)
            {
                mirrorCamera.targetTexture = null;
            }

            reflectionTexture.Release();
            Destroy(reflectionTexture);
            reflectionTexture = null;
        }

        private static Vector3 ReflectPoint(Vector3 point, Vector3 planePosition, Vector3 planeNormal)
        {
            float distance = Vector3.Dot(point - planePosition, planeNormal);
            return point - 2f * distance * planeNormal;
        }

        private static Matrix4x4 CalculateReflectionMatrix(Vector4 plane)
        {
            Matrix4x4 matrix = Matrix4x4.identity;
            matrix.m00 = 1f - 2f * plane.x * plane.x;
            matrix.m01 = -2f * plane.x * plane.y;
            matrix.m02 = -2f * plane.x * plane.z;
            matrix.m03 = -2f * plane.w * plane.x;

            matrix.m10 = -2f * plane.y * plane.x;
            matrix.m11 = 1f - 2f * plane.y * plane.y;
            matrix.m12 = -2f * plane.y * plane.z;
            matrix.m13 = -2f * plane.w * plane.y;

            matrix.m20 = -2f * plane.z * plane.x;
            matrix.m21 = -2f * plane.z * plane.y;
            matrix.m22 = 1f - 2f * plane.z * plane.z;
            matrix.m23 = -2f * plane.w * plane.z;
            return matrix;
        }

        private static Vector4 CameraSpacePlane(
            Camera camera,
            Vector3 planePosition,
            Vector3 planeNormal,
            float offset)
        {
            Vector3 offsetPosition = planePosition + planeNormal * offset;
            Matrix4x4 worldToCamera = camera.worldToCameraMatrix;
            Vector3 cameraPosition = worldToCamera.MultiplyPoint(offsetPosition);
            Vector3 cameraNormal = worldToCamera.MultiplyVector(planeNormal).normalized;
            return new Vector4(
                cameraNormal.x,
                cameraNormal.y,
                cameraNormal.z,
                -Vector3.Dot(cameraPosition, cameraNormal));
        }
    }

    [DisallowMultipleComponent]
    public sealed class PlanarMirrorCameraCulling : MonoBehaviour
    {
        private void OnPreCull()
        {
            GL.invertCulling = true;
        }

        private void OnPostRender()
        {
            GL.invertCulling = false;
        }

        private void OnDisable()
        {
            GL.invertCulling = false;
        }
    }
}
