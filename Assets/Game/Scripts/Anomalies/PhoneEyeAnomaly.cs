using UnityEngine;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class PhoneEyeAnomaly : AnomalyBehaviour
    {
        [SerializeField] private Renderer screen;
        [SerializeField] private Transform observer;
        [SerializeField, Min(1f)] private float flickerInterval = 3f;
        [SerializeField, Range(0.05f, 0.3f)] private float flickerSeconds = 0.14f;
        private static readonly int TintId = Shader.PropertyToID("_BaseColor");
        private static readonly int TextureTransformId = Shader.PropertyToID("_BaseMap_ST");
        private MaterialPropertyBlock original;
        private MaterialPropertyBlock presentation;
        private bool screenEnabled;
        [System.NonSerialized] private bool captured;
        private float elapsed;
        private int sequence;
        public bool IsConfigured => screen != null && observer != null;
        public Renderer Screen => screen;

        public void Configure(Renderer display, Transform viewer)
        {
            screen = display;
            observer = viewer;
            CaptureNormalState();
        }

        public override void CaptureNormalState()
        {
            if (!IsConfigured || (captured && original != null && presentation != null)) return;
            original = new MaterialPropertyBlock();
            presentation = new MaterialPropertyBlock();
            screen.GetPropertyBlock(original);
            screenEnabled = screen.enabled;
            captured = true;
        }

        protected override void OnActivate()
        {
            CaptureNormalState();
            if (!captured || !IsConfigured) return;
            elapsed = flickerSeconds;
            sequence = 0;
            screen.enabled = true;
            SetGaze();
            Present(1f);
        }

        private void Update()
        {
            if (!IsActive || !captured || !IsConfigured) return;
            elapsed += Time.deltaTime;
            float interval = Mathf.Max(1f, flickerInterval) * (1f + (sequence % 3 - 1) * 0.23f);
            if (elapsed >= interval)
            {
                elapsed = 0f;
                sequence++;
                SetGaze();
            }
            float brightness = elapsed < flickerSeconds ? 0.18f : 1f;
            Present(brightness);
        }

        private void SetGaze()
        {
            Vector3 direction = screen.transform.InverseTransformPoint(observer.position);
            float shift = Mathf.Clamp(direction.x * 0.02f, -0.025f, 0.025f);
            presentation.SetVector(TextureTransformId, new Vector4(0.6f, 0.5f, 0.2f + shift, 0.25f));
        }

        private void Present(float brightness)
        {
            presentation.SetColor(TintId, new Color(0.72f, 0.88f, 0.72f) * brightness);
            screen.SetPropertyBlock(presentation);
        }

        protected override void OnResetAnomaly()
        {
            elapsed = 0f;
            sequence = 0;
            if (!captured || screen == null) return;
            screen.SetPropertyBlock(original);
            screen.enabled = screenEnabled;
        }

        private void OnDisable() => ResetAnomaly();
    }
}
