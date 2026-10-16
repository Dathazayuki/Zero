using UnityEngine;

namespace Zero.AnomalyPrototype.Flow
{
    public sealed class WatchfulHallHidingSpots : MonoBehaviour
    {
        [SerializeField] private Transform[] cabinets;
        public int Count => cabinets != null ? cabinets.Length : 0;

        public Transform FindCabinet(Vector3 feet)
        {
            if (!isActiveAndEnabled || cabinets == null) return null;
            for (int i = 0; i < cabinets.Length; i++)
                if (Contains(cabinets[i], feet)) return cabinets[i];
            return null;
        }

        private static bool Contains(Transform cabinet, Vector3 feet)
        {
            if (cabinet == null || !cabinet.gameObject.activeInHierarchy) return false;
            Vector3 local = cabinet.InverseTransformPoint(feet);
            return Mathf.Abs(local.x) < 0.66f && local.z > -0.55f && local.z < 0.6f
                && local.y > -0.1f && local.y < 0.35f;
        }

        public static Vector3 Entrance(Transform cabinet) => cabinet.TransformPoint(new Vector3(0f, 0f, -1.1f));
    }
}
