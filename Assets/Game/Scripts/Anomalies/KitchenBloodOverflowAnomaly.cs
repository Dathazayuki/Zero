using UnityEngine;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class KitchenBloodOverflowAnomaly : AnomalyBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private GameObject visuals;
        [SerializeField] private Transform basinSurface;
        [SerializeField] private Transform tapStream;
        [SerializeField] private GameObject overflow;
        [SerializeField] private Transform floorPool;
        [SerializeField, Min(0.1f)] private float activationDistance = 2.8f;
        [SerializeField, Min(0.1f)] private float fillSeconds = 10f;
        [SerializeField, Min(0.1f)] private float spreadSeconds = 8f;
        [SerializeField, Min(0f)] private float fillHeight = 0.27f;
        private SinkOverflowProgress progress;
        private Vector3 basinPosition, streamScale, poolScale;
        private ParticleSystem[] tapParticles, spillParticles;

        public bool IsConfigured => player != null && visuals != null && basinSurface != null &&
            tapStream != null && overflow != null && floorPool != null;
        public float FillProgress => progress != null ? progress.Fill : 0f;
        public bool IsOverflowing => progress != null && progress.IsOverflowing;

        public override void CaptureNormalState()
        {
            if (progress != null || !IsConfigured) return;
            basinPosition = basinSurface.localPosition;
            streamScale = tapStream.localScale;
            poolScale = floorPool.localScale;
            tapParticles = basinSurface.GetComponentsInChildren<ParticleSystem>(true);
            spillParticles = overflow.GetComponentsInChildren<ParticleSystem>(true);
            progress = new SinkOverflowProgress(fillSeconds, spreadSeconds);
            visuals.SetActive(false);
        }

        protected override void OnActivate()
        {
            CaptureNormalState();
        }

        private void Update() => Tick(Time.deltaTime);

        public void Tick(float deltaTime)
        {
            if (!IsActive || progress == null || !IsConfigured) return;
            Vector3 distance = player.position - transform.position;
            distance.y = 0f;
            if (!progress.HasStarted && distance.sqrMagnitude <= activationDistance * activationDistance)
            {
                progress.Start();
                visuals.SetActive(true);
                for (int i = 0; i < tapParticles.Length; i++) tapParticles[i].Play(true);
            }
            if (!progress.HasStarted) return;
            progress.Advance(deltaTime);
            basinSurface.localPosition = basinPosition + Vector3.up * (fillHeight * progress.Fill);
            tapStream.localScale = new Vector3(streamScale.x,
                Mathf.Max(0.01f, streamScale.y - fillHeight * progress.Fill), streamScale.z);
            bool spilling = progress.IsOverflowing;
            if (overflow.activeSelf != spilling)
            {
                overflow.SetActive(spilling);
                if (spilling) for (int i = 0; i < spillParticles.Length; i++) spillParticles[i].Play(true);
            }
            floorPool.gameObject.SetActive(spilling);
            float spread = Mathf.SmoothStep(0.05f, 1f, progress.Spill);
            floorPool.localScale = new Vector3(poolScale.x * spread, poolScale.y, poolScale.z * spread);
        }

        protected override void OnResetAnomaly()
        {
            if (progress == null) return;
            progress.Reset();
            StopParticles(tapParticles);
            StopParticles(spillParticles);
            if (visuals != null) visuals.SetActive(false);
            if (overflow != null) overflow.SetActive(false);
            if (floorPool != null) { floorPool.localScale = poolScale; floorPool.gameObject.SetActive(false); }
            if (basinSurface != null) basinSurface.localPosition = basinPosition;
            if (tapStream != null) tapStream.localScale = streamScale;
        }

        private static void StopParticles(ParticleSystem[] particles)
        {
            for (int i = 0; i < particles.Length; i++)
                if (particles[i] != null) particles[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void OnDisable() => ResetAnomaly();
    }
}
