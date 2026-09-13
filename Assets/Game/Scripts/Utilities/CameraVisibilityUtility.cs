using UnityEngine;

namespace Zero.AnomalyPrototype.Utilities
{
    public static class CameraVisibilityUtility
    {
        public static bool IsVisibleFromCamera(
            Camera camera,
            Transform target,
            Vector3 targetPoint,
            LayerMask obstructionMask)
        {
            if (camera == null || target == null || !target.gameObject.activeInHierarchy)
            {
                return false;
            }

            Vector3 viewportPoint = camera.WorldToViewportPoint(targetPoint);
            if (viewportPoint.z <= 0f || viewportPoint.x < 0f || viewportPoint.x > 1f ||
                viewportPoint.y < 0f || viewportPoint.y > 1f)
            {
                return false;
            }

            Vector3 origin = camera.transform.position;
            Vector3 direction = targetPoint - origin;
            float distance = direction.magnitude;
            if (distance <= Mathf.Epsilon)
            {
                return true;
            }

            // Stop the obstruction ray just before the sampled point. The target is
            // not required to own a collider, and a wall immediately behind a
            // collider-less target must not make a visible target look occluded.
            float rayDistance = Mathf.Max(0f, distance - 0.05f);
            if (rayDistance <= Mathf.Epsilon)
            {
                return true;
            }

            if (!Physics.Raycast(
                    origin,
                    direction / distance,
                    out RaycastHit hit,
                    rayDistance,
                    obstructionMask,
                    QueryTriggerInteraction.Ignore))
            {
                return true;
            }

            return hit.transform == target ||
                   hit.transform.IsChildOf(target) ||
                   target.IsChildOf(hit.transform);
        }

        public static bool IsVisibleFromCamera(Camera camera, Transform target)
        {
            return IsVisibleFromCamera(camera, target, target.position, ~0);
        }
    }
}
