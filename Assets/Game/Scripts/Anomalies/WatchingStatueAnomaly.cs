using UnityEngine;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class WatchingStatueAnomaly : AnomalyBehaviour
    {
        [SerializeField] private Transform statue;
        [SerializeField] private Transform player;
        [SerializeField, Min(1f)] private float turnSpeed = 120f;
        private Quaternion normalLocalRotation;

        public void Configure(Transform statueTransform, Transform playerTransform, float degreesPerSecond = 120f)
        {
            statue = statueTransform;
            player = playerTransform;
            turnSpeed = degreesPerSecond;
            if (statue != null)
            {
                normalLocalRotation = statue.localRotation;
            }
        }

        public void BindPlayer(Transform playerTransform)
        {
            player = playerTransform;
        }

        public override void CaptureNormalState()
        {
            if (statue != null)
            {
                normalLocalRotation = statue.localRotation;
            }
        }

        private void Update()
        {
            if (!IsActive || statue == null || player == null)
            {
                return;
            }

            Vector3 direction = player.position - statue.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            statue.rotation = Quaternion.RotateTowards(statue.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        protected override void OnActivate()
        {
        }

        protected override void OnResetAnomaly()
        {
            if (statue != null)
            {
                statue.localRotation = normalLocalRotation;
            }
        }
    }
}
