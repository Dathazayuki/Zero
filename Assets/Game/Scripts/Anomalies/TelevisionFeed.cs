using UnityEngine;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class TelevisionFeed : MonoBehaviour
    {
        [SerializeField] private Renderer screen;
        [SerializeField] private Camera feedCamera;
        [SerializeField] private GameObject stage;
        [SerializeField] private Transform[] heads;
        [SerializeField] private GameObject cracks;
        private static readonly int TextureId = Shader.PropertyToID("_BaseMap");
        private static readonly int TintId = Shader.PropertyToID("_BaseColor");
        private RenderTexture output;
        private MaterialPropertyBlock normal, display;
        private Quaternion[] headRotations;
        private SkinnedMeshRenderer glass;
        private Vector3 screenPosition;
        private float elapsed, nextFrame, tension;
        private bool tiltHeads;
        public bool IsPlaying => output != null;

        public void Configure(Renderer surface, Camera camera, GameObject set, Transform[] actors, GameObject glass)
        {
            screen = surface;
            feedCamera = camera;
            stage = set;
            heads = actors;
            cracks = glass;
        }

        public void Begin(bool family)
        {
            Stop();
            if (screen == null || feedCamera == null || stage == null) return;
            normal = new MaterialPropertyBlock();
            display = new MaterialPropertyBlock();
            screen.GetPropertyBlock(normal);
            screen.GetPropertyBlock(display);
            screenPosition = screen.transform.localPosition;
            glass = screen as SkinnedMeshRenderer;
            headRotations = new Quaternion[heads.Length];
            for (int i = 0; i < heads.Length; i++) headRotations[i] = heads[i].localRotation;
            output = new RenderTexture(512, 384, 16) { name = "IR2_TelevisionFeed" };
            output.Create();
            feedCamera.targetTexture = output;
            display.SetTexture(TextureId, output);
            screen.SetPropertyBlock(display);
            screen.enabled = true;
            stage.SetActive(true);
            tiltHeads = family;
            elapsed = nextFrame = tension = 0f;
        }

        public void SetTension(float amount) => tension = Mathf.Clamp01(amount);

        private void LateUpdate()
        {
            if (!IsPlaying) return;
            elapsed += Time.deltaTime;
            float tilt = tiltHeads ? Mathf.SmoothStep(0f, 28f, (elapsed - 3f) / 1.2f) : 0f;
            for (int i = 0; i < heads.Length; i++) heads[i].localRotation = headRotations[i] * Quaternion.Euler(0f, 0f, tilt);
            if (glass != null) glass.SetBlendShapeWeight(0, tension * 100f);
            cracks.SetActive(tension >= 0.7f);
            float flicker = 0.85f + 0.15f * Mathf.Sin(elapsed * (31f + tension * 65f));
            display.SetColor(TintId, new Color(0.8f, 0.9f, 0.82f) * flicker);
            screen.SetPropertyBlock(display);
            if (elapsed < nextFrame) return;
            nextFrame = elapsed + 0.083f;
            RenderFrame();
        }

        public void RenderFrame()
        {
            if (IsPlaying) feedCamera.Render();
        }

        public void Stop()
        {
            if (output == null) return;
            feedCamera.targetTexture = null;
            output.Release();
            if (Application.isPlaying) Destroy(output);
            else DestroyImmediate(output);
            output = null;
            screen.SetPropertyBlock(normal);
            screen.enabled = false;
            screen.transform.localPosition = screenPosition;
            if (glass != null) glass.SetBlendShapeWeight(0, 0f);
            cracks.SetActive(false);
            stage.SetActive(false);
            for (int i = 0; i < heads.Length; i++) heads[i].localRotation = headRotations[i];
        }

        private void OnDisable() => Stop();
    }
}
