using UnityEngine;
using UnityEngine.Rendering;

namespace Zero.AnomalyPrototype.Utilities
{
    [DisallowMultipleComponent]
    public sealed class WaterFootstepRippleController : MonoBehaviour
    {
        [SerializeField] private CharacterController player;
        [SerializeField] private MeshRenderer waterSurface;
        [SerializeField] private Material rippleMaterial;
        [SerializeField, Range(4, 24)] private int poolSize = 12;
        [SerializeField, Min(0.1f)] private float stepDistance = 0.65f;
        [SerializeField, Min(0.1f)] private float lifetime = 1.7f;
        [SerializeField, Min(0.1f)] private float maxDiameter = 2.3f;
        [SerializeField, Min(0.001f)] private float surfaceOffset = 0.008f;

        private static readonly int ProgressId = Shader.PropertyToID("_Progress");
        private readonly WaterRippleStepSampler sampler = new WaterRippleStepSampler();
        private MeshRenderer[] renderers;
        private Transform[] poses;
        private float[] ages;
        private Mesh quadMesh;
        private MaterialPropertyBlock properties;
        private Bounds surfaceBounds;
        private int nextSlot;
        private bool ready;

        public int ActiveRippleCount { get; private set; }

        public void BindPlayer(CharacterController source)
        {
            ClearRipples();
            player = source;
            if (player != null) sampler.Reset(player.transform.position);
        }

        public void Configure(CharacterController source, MeshRenderer surface, Material material)
        {
            ClearRipples();
            player = source;
            waterSurface = surface;
            rippleMaterial = material;
            Initialize();
        }

        private void Start()
        {
            if (ready) return;
            Initialize();
        }

        private void Initialize()
        {
            ready = player != null && waterSurface != null && rippleMaterial != null;
            if (!ready)
            {
                Debug.LogError("WATER_RIPPLE_CONFIGURATION_INVALID: player, surface or material missing", this);
                return;
            }
            surfaceBounds = waterSurface.localBounds;
            if (renderers == null) CreatePool();
            for (int i = 0; i < renderers.Length; i++) renderers[i].sharedMaterial = rippleMaterial;
            sampler.Reset(player.transform.position);
        }

        private void CreatePool()
        {
            quadMesh = new Mesh { name = "Water Ripple Shared Quad" };
            quadMesh.vertices = new[] { new Vector3(-0.5f, 0, -0.5f), new Vector3(-0.5f, 0, 0.5f),
                new Vector3(0.5f, 0, 0.5f), new Vector3(0.5f, 0, -0.5f) };
            quadMesh.uv = new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right };
            quadMesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            quadMesh.RecalculateBounds();
            properties = new MaterialPropertyBlock();
            int count = Mathf.Clamp(poolSize, 4, 24);
            renderers = new MeshRenderer[count];
            poses = new Transform[count];
            ages = new float[count];
            for (int i = 0; i < count; i++) CreateSlot(i);
        }

        private void CreateSlot(int index)
        {
            GameObject slot = new GameObject("Water Ripple");
            slot.transform.SetParent(transform, false);
            slot.AddComponent<MeshFilter>().sharedMesh = quadMesh;
            MeshRenderer renderer = slot.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = rippleMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.enabled = false;
            renderers[index] = renderer;
            poses[index] = slot.transform;
        }

        private void LateUpdate()
        {
            if (!ready || Time.deltaTime <= 0f) return;
            if (player == null || waterSurface == null || rippleMaterial == null) { ClearRipples(); return; }
            UpdateRipples(Time.deltaTime);
            Vector3 position = player.transform.position;
            bool immersed = IsInWater();
            if (!sampler.Sample(position, immersed, stepDistance)) return;
            EmitRipple(position);
        }

        private bool IsInWater()
        {
            if (!player.enabled || !player.isGrounded || !waterSurface.enabled ||
                !waterSurface.gameObject.activeInHierarchy) return false;
            Transform surface = waterSurface.transform;
            Vector3 feet = player.bounds.center - Vector3.up * player.bounds.extents.y;
            Vector3 local = surface.InverseTransformPoint(feet);
            float depth = Vector3.Dot(surface.position - feet, surface.up);
            return local.x >= surfaceBounds.min.x && local.x <= surfaceBounds.max.x &&
                local.z >= surfaceBounds.min.z && local.z <= surfaceBounds.max.z &&
                depth >= 0.02f && depth <= 1f;
        }

        private void EmitRipple(Vector3 position)
        {
            Transform surface = waterSurface.transform;
            Vector3 offset = surface.up * Vector3.Dot(surface.position - position, surface.up);
            Transform pose = poses[nextSlot];
            pose.SetPositionAndRotation(position + offset + surface.up * surfaceOffset, surface.rotation);
            Vector3 scale = transform.lossyScale;
            pose.localScale = new Vector3(maxDiameter / scale.x, maxDiameter / scale.y, maxDiameter / scale.z);
            ages[nextSlot] = 0f;
            properties.SetFloat(ProgressId, 0f);
            renderers[nextSlot].SetPropertyBlock(properties);
            if (!renderers[nextSlot].enabled) ActiveRippleCount++;
            renderers[nextSlot].enabled = true;
            nextSlot = (nextSlot + 1) % renderers.Length;
        }

        private void UpdateRipples(float deltaTime)
        {
            ActiveRippleCount = 0;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (!renderers[i].enabled) continue;
                ages[i] += deltaTime;
                float progress = ages[i] / Mathf.Max(0.1f, lifetime);
                renderers[i].enabled = progress < 1f;
                if (progress >= 1f) continue;
                properties.SetFloat(ProgressId, progress);
                renderers[i].SetPropertyBlock(properties);
                ActiveRippleCount++;
            }
        }

        public void ClearRipples()
        {
            if (renderers != null)
                for (int i = 0; i < renderers.Length; i++)
                    if (renderers[i] != null) renderers[i].enabled = false;
            nextSlot = 0;
            ActiveRippleCount = 0;
            if (player != null) sampler.Reset(player.transform.position);
        }

        private void OnDisable() => ClearRipples();

        private void OnDestroy()
        {
            if (poses != null)
                for (int i = 0; i < poses.Length; i++)
                    if (poses[i] != null) Destroy(poses[i].gameObject);
            if (quadMesh != null) Destroy(quadMesh);
        }
    }
}
