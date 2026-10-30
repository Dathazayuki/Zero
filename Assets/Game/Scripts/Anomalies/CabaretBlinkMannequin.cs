using UnityEngine;
using UnityEngine.AI;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class CabaretBlinkMannequin : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private NavMeshAgent navigation;
        [SerializeField] private GameObject standingMannequin;
        [SerializeField] private LayerMask obstructionMask = 1;
        [SerializeField, Min(.1f)] private float radius = .23f;
        private NavMeshPath path;
        private readonly Vector3[] corners = new Vector3[64];
        private Vector3 home;
        private Quaternion rotation;
        private bool captured;
        private bool swapped;
        private bool standingWasActive;
        public bool IsConfigured => animator != null && navigation != null && standingMannequin != null;

        public void Prepare()
        {
            if (path == null) path = new NavMeshPath();
            if (!captured) { home = transform.position; rotation = transform.rotation; captured = true; }
            StopNavigation();
            if (animator == null) return;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.Rebind(); animator.Update(0f); animator.speed = 0f;
        }

        public void SetDark(bool dark)
        {
            if (animator != null) animator.speed = dark ? 1f : 0f;
        }

        public bool StepTowards(Transform target, float distance)
        {
            if (!IsConfigured || target == null || path == null) return false;
            if (!swapped) BeginChase();
            if (!navigation.enabled || !navigation.isOnNavMesh) return false;
            if (CanReachPlayer(target)) return true;
            return MoveAlongPath(target, distance) && CanReachPlayer(target);
        }

        private bool MoveAlongPath(Transform target, float distance)
        {
            if (!NavMesh.SamplePosition(target.position, out NavMeshHit goal, 2f, navigation.areaMask)) return false;
            if (!navigation.CalculatePath(goal.position, path)) return false;
            int count = path.GetCornersNonAlloc(corners);
            if (count == corners.Length) return false;
            Vector3 point = navigation.nextPosition;
            Vector3 start = point;
            float remaining = Mathf.Max(0f, distance);
            for (int i = 1; i < count && remaining > 0f; i++)
            {
                Vector3 next = Vector3.MoveTowards(point, corners[i], remaining);
                remaining -= Vector3.Distance(point, next);
                point = next;
            }
            if (!navigation.Warp(point)) return false;
            Vector3 heading = point - start; heading.y = 0f;
            transform.SetPositionAndRotation(point, heading.sqrMagnitude > .001f
                ? Quaternion.LookRotation(heading) : transform.rotation);
            return true;
        }

        private void BeginChase()
        {
            standingWasActive = standingMannequin.activeSelf;
            swapped = true;
            transform.SetPositionAndRotation(standingMannequin.transform.position, standingMannequin.transform.rotation);
            standingMannequin.SetActive(false);
            if (!NavMesh.SamplePosition(transform.position, out NavMeshHit start, 2f, NavMesh.AllAreas)) return;
            navigation.updatePosition = false; navigation.updateRotation = false;
            navigation.radius = radius;
            navigation.enabled = true;
            if (navigation.Warp(start.position)) navigation.isStopped = true;
        }

        private bool CanReachPlayer(Transform target)
            => (target.position - transform.position).sqrMagnitude < .85f * .85f && CanCatch(target);

        private bool CanCatch(Transform target)
        {
            if (!Physics.Linecast(transform.position + Vector3.up, target.position + Vector3.up,
                out RaycastHit hit, obstructionMask, QueryTriggerInteraction.Ignore)) return true;
            return hit.transform == target || hit.transform.IsChildOf(target);
        }

        private void StopNavigation()
        {
            if (navigation == null) return;
            if (navigation.enabled && navigation.isOnNavMesh) navigation.ResetPath();
            navigation.enabled = false;
        }

        private void RestoreStanding()
        {
            if (!swapped) return;
            swapped = false;
            if (standingMannequin != null) standingMannequin.SetActive(standingWasActive);
        }

        public void Restore()
        {
            SetDark(false);
            StopNavigation(); RestoreStanding();
            if (captured) transform.SetPositionAndRotation(home, rotation);
            gameObject.SetActive(false);
        }

        private void OnDisable() { SetDark(false); StopNavigation(); RestoreStanding(); }
    }
}
