using UnityEngine;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class ApproachingPaintingAnomaly : AnomalyBehaviour
    {
        [SerializeField] private Renderer painting;
        [SerializeField] private Camera observer;
        [SerializeField] private Texture2D[] stages;
        [SerializeField, Min(0.1f)] private float lookAwaySeconds = 0.4f;
        private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
        private static readonly int MainTex = Shader.PropertyToID("_MainTex");
        private readonly Plane[] planes = new Plane[6];
        private MaterialPropertyBlock original, display;
        private bool seen, armed;
        private float away;
        public int StageIndex { get; private set; } = -1;
        public bool IsConfigured => painting != null && observer != null && stages != null && stages.Length == 3;

        public void Configure(Renderer target, Camera viewer, Texture2D[] textures)
        {
            painting = target;
            observer = viewer;
            stages = textures;
        }

        public override void CaptureNormalState()
        {
            if (!IsConfigured || original != null) return;
            original = new MaterialPropertyBlock();
            display = new MaterialPropertyBlock();
            painting.GetPropertyBlock(original);
            painting.GetPropertyBlock(display);
        }

        protected override void OnActivate()
        {
            CaptureNormalState();
            if (original == null || !IsConfigured) return;
            StageIndex = 0;
            Present();
        }

        private void Update()
        {
            if (!IsActive || original == null || !IsConfigured || StageIndex >= stages.Length - 1) return;
            if (!IsViewed())
            {
                if (seen) away += Time.deltaTime;
                if (away >= lookAwaySeconds) armed = true;
                return;
            }
            seen = true;
            away = 0f;
            if (!armed) return;
            armed = false;
            StageIndex++;
            Present();
        }

        private bool IsViewed()
        {
            GeometryUtility.CalculateFrustumPlanes(observer, planes);
            if (!GeometryUtility.TestPlanesAABB(planes, painting.bounds)) return false;
            Vector3 target = painting.bounds.center;
            if (!Physics.Linecast(target, observer.transform.position, out RaycastHit hit, ~0, QueryTriggerInteraction.Ignore)) return true;
            return hit.transform.IsChildOf(observer.transform.root);
        }

        private void Present()
        {
            display.SetTexture(BaseMap, stages[StageIndex]);
            display.SetTexture(MainTex, stages[StageIndex]);
            painting.SetPropertyBlock(display);
        }

        protected override void OnResetAnomaly()
        {
            StageIndex = -1;
            seen = armed = false;
            away = 0f;
            if (painting != null && original != null) painting.SetPropertyBlock(original);
        }

        private void OnDisable() => ResetAnomaly();
    }
}
