using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class CabaretGlitchFieldView : MonoBehaviour
    {
        [SerializeField] private Renderer boundary;
        [SerializeField] private Renderer screenTearing;
        [SerializeField] private Camera observer;
        [SerializeField] private UniversalAdditionalCameraData cameraData;
        private MaterialPropertyBlock boundaryBlock, screenBlock;
        private CameraOverrideOption originalColourOption;
        private bool ownsCamera;
        private static readonly int Intensity = Shader.PropertyToID("_Intensity");
        private static readonly int Observer = Shader.PropertyToID("_Observer");
        public bool IsConfigured => boundary != null && screenTearing != null && observer != null && cameraData != null;

        public void Capture()
        {
            if (boundaryBlock != null) return;
            boundaryBlock = new MaterialPropertyBlock(); screenBlock = new MaterialPropertyBlock();
        }

        public void Present(Vector3 centre, float radius, float intensity)
        {
            if (!IsConfigured || boundaryBlock == null) return;
            if (!ownsCamera)
            {
                originalColourOption = cameraData.requiresColorOption;
                cameraData.requiresColorOption = CameraOverrideOption.On; ownsCamera = true;
            }
            boundary.gameObject.SetActive(radius > 0f);
            boundary.transform.position = centre;
            boundary.transform.localScale = Vector3.one * ((radius + .2f) * 2f);
            boundaryBlock.SetFloat(Intensity, 1f); boundary.SetPropertyBlock(boundaryBlock);
            screenTearing.gameObject.SetActive(intensity > 0f);
            float distance = Mathf.Max(.08f, observer.nearClipPlane * 1.5f);
            float height = Mathf.Tan(observer.fieldOfView * Mathf.Deg2Rad * .5f) * distance * 2.1f;
            screenTearing.transform.SetPositionAndRotation(observer.transform.position + observer.transform.forward * distance,
                observer.transform.rotation);
            screenTearing.transform.localScale = new Vector3(height * observer.aspect, height, 1f);
            screenBlock.SetFloat(Intensity, Mathf.Clamp01(intensity));
            screenBlock.SetVector(Observer, observer.transform.position);
            screenTearing.SetPropertyBlock(screenBlock);
        }

        public void Restore()
        {
            if (ownsCamera && cameraData != null) cameraData.requiresColorOption = originalColourOption;
            ownsCamera = false;
            if (boundary != null) boundary.gameObject.SetActive(false);
            if (screenTearing != null) screenTearing.gameObject.SetActive(false);
        }

        private void OnDisable() => Restore();
    }
}
