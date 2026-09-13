using UnityEngine;

namespace Zero.EnvironmentLoop
{
    public sealed class CorrectArrowController : MonoBehaviour
    {
        [SerializeField] private Transform arrowTransform;
        [SerializeField] private Quaternion upLocalRotation = Quaternion.identity;
        [SerializeField] private Quaternion downLocalRotation = Quaternion.Euler(0f, 0f, 180f);

        public Transform ArrowTransform => arrowTransform;
        public TravelDirection Direction { get; private set; } = TravelDirection.Up;
        public bool IsConfigured => arrowTransform != null;

        public void Configure(Transform arrow, Quaternion upRotation, Quaternion downRotation)
        {
            arrowTransform = arrow;
            upLocalRotation = Normalize(upRotation);
            downLocalRotation = Normalize(downRotation);
            ResetArrow();
        }

        public void SetDirection(TravelDirection direction)
        {
            Direction = direction;
            if (arrowTransform == null)
            {
                return;
            }

            arrowTransform.localRotation = direction == TravelDirection.Up
                ? upLocalRotation
                : downLocalRotation;
        }

        public Quaternion GetLocalRotation(TravelDirection direction)
        {
            return direction == TravelDirection.Up
                ? upLocalRotation
                : downLocalRotation;
        }

        public void SetLogicalDirection(TravelDirection direction)
        {
            Direction = direction;
        }

        public void ResetArrow()
        {
            SetDirection(TravelDirection.Up);
        }

        private static Quaternion Normalize(Quaternion value)
        {
            float magnitude = Mathf.Sqrt(
                value.x * value.x + value.y * value.y + value.z * value.z + value.w * value.w);
            if (magnitude <= 0.0001f)
            {
                return Quaternion.identity;
            }

            return new Quaternion(
                value.x / magnitude,
                value.y / magnitude,
                value.z / magnitude,
                value.w / magnitude);
        }
    }
}
