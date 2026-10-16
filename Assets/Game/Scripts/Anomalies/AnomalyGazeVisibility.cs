using UnityEngine;

namespace Zero.AnomalyPrototype.Anomalies
{
    public static class AnomalyGazeVisibility
    {
        public static bool IsVisible(
            Camera camera, Transform target, Renderer targetRenderer,
            LayerMask obstructionMask, float targetClearance)
        {
            if (camera == null || target == null) return false;

            Vector3 targetPoint = ResolveTargetPoint(target, targetRenderer);
            Vector3 viewport = camera.WorldToViewportPoint(targetPoint);
            if (!IsInFrustum(viewport)) return false;

            Vector3 origin = camera.transform.position;
            Vector3 direction = targetPoint - origin;
            float distance = direction.magnitude;
            float rayDistance = Mathf.Max(0f, distance - targetClearance);
            if (distance <= Mathf.Epsilon || rayDistance <= Mathf.Epsilon) return true;

            return !Physics.Raycast(origin, direction / distance, out RaycastHit hit,
                rayDistance, obstructionMask, QueryTriggerInteraction.Ignore) ||
                IsTargetOrChild(hit.transform, target);
        }

        private static Vector3 ResolveTargetPoint(Transform target, Renderer renderer) =>
            renderer != null && renderer.gameObject.activeInHierarchy
                ? renderer.bounds.center : target.position;

        private static bool IsInFrustum(Vector3 vp) =>
            vp.z > 0f && vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f;

        private static bool IsTargetOrChild(Transform hitTransform, Transform target) =>
            hitTransform == target || hitTransform.IsChildOf(target);
    }
}
