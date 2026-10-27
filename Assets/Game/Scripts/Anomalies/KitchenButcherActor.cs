using UnityEngine;
using UnityEngine.AI;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class KitchenButcherActor : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private NavMeshAgent agent;
        [SerializeField] private RuntimeAnimatorController chaseController;
        [SerializeField] private Transform neckBone;
        [SerializeField, Min(0.1f)] private float chaseSpeed = 3.8f;
        [SerializeField, Min(0.1f)] private float catchDistance = 0.95f;
        private static readonly int Run = Animator.StringToHash("Run");
        private static readonly int Grab = Animator.StringToHash("Grab");
        private readonly RaycastHit[] hits = new RaycastHit[16];
        private Transform head, grabTarget;
        private Transform[] bones;
        private Vector3[] positions;
        private Quaternion[] rotations;
        private RuntimeAnimatorController normalController;
        private Vector3 normalPosition;
        private Quaternion normalRotation, grabNeckLocal;
        private bool normalActive, normalEnabled, normalRootMotion;
        private float normalSpeed, nextRepath;
        private AnimatorCullingMode normalCulling;
        public bool IsConfigured => animator != null && animator.isHuman && agent != null
            && chaseController != null && neckBone != null;
        public Vector3 FacePoint => head != null ? head.position + transform.forward * 0.12f : transform.position + Vector3.up * 1.7f;
        public Vector3 Forward => transform.forward;

        public void Capture()
        {
            if (bones != null || !IsConfigured) return;
            normalActive = gameObject.activeSelf; normalPosition = transform.position; normalRotation = transform.rotation;
            normalController = animator.runtimeAnimatorController; normalEnabled = animator.enabled;
            normalRootMotion = animator.applyRootMotion; normalSpeed = animator.speed; normalCulling = animator.cullingMode;
            head = animator.GetBoneTransform(HumanBodyBones.Head);
            bones = animator.GetComponentsInChildren<Transform>(true);
            positions = new Vector3[bones.Length]; rotations = new Quaternion[bones.Length];
            for (int i = 0; i < bones.Length; i++) { positions[i] = bones[i].localPosition; rotations[i] = bones[i].localRotation; }
        }

        public void Hide()
        {
            Stop(); gameObject.SetActive(false);
        }

        public bool BeginChase(Transform entrance)
        {
            if (!IsConfigured || !NavMesh.SamplePosition(entrance.position, out NavMeshHit hit, 1f, NavMesh.AllAreas)) return false;
            transform.SetPositionAndRotation(hit.position, entrance.rotation);
            gameObject.SetActive(true); animator.enabled = true; animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate; animator.speed = 1f;
            animator.runtimeAnimatorController = chaseController; animator.Rebind(); animator.Play(Run, 0, 0f);
            agent.enabled = true;
            if (!agent.Warp(hit.position)) { Stop(); return false; }
            agent.speed = chaseSpeed; agent.isStopped = false; nextRepath = 0f;
            return agent.isOnNavMesh;
        }

        public bool Pursue(Transform player, Transform observer)
        {
            if (!agent.enabled || !agent.isOnNavMesh) return false;
            if (Time.time >= nextRepath)
            {
                nextRepath = Time.time + 0.2f;
                if (NavMesh.SamplePosition(player.position, out NavMeshHit hit, 1f, agent.areaMask)) agent.SetDestination(hit.position);
            }
            Vector3 offset = player.position - transform.position;
            if (Mathf.Abs(offset.y) > 1.2f) return false;
            offset.y = 0f;
            return offset.sqrMagnitude <= catchDistance * catchDistance && ClearSight(player, observer);
        }

        private bool ClearSight(Transform player, Transform observer)
        {
            Vector3 direction = observer.position - FacePoint;
            int count = Physics.RaycastNonAlloc(FacePoint, direction.normalized, hits, direction.magnitude, ~0, QueryTriggerInteraction.Ignore);
            if (count == hits.Length) return false;
            for (int i = 0; i < count; i++)
                if (!hits[i].transform.IsChildOf(transform) && !hits[i].transform.IsChildOf(player)) return false;
            return true;
        }

        public void BeginGrab(Transform observer)
        {
            Stop();
            Vector3 direction = observer.position - transform.position; direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(direction);
            animator.Play(Grab, 0, 0f); animator.Update(0f);
            grabTarget = observer; grabNeckLocal = neckBone.localRotation;
        }

        public void TrackNeck()
        {
            if (grabTarget == null || neckBone == null) return;
            Vector3 direction = grabTarget.position - neckBone.position;
            if (direction.sqrMagnitude < 0.001f) return;
            Quaternion rest = neckBone.parent.rotation * grabNeckLocal;
            Quaternion delta = Quaternion.FromToRotation(transform.forward, direction.normalized);
            neckBone.rotation = delta * rest;
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (grabTarget == null || animator == null) return;
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1f);
            animator.SetIKPosition(AvatarIKGoal.LeftHand, grabTarget.position - Vector3.up * 0.14f);
        }

        private void Stop()
        {
            grabTarget = null;
            if (agent == null) return;
            if (agent.enabled && agent.isOnNavMesh) agent.ResetPath();
            agent.enabled = false;
        }

        public void Restore()
        {
            Stop();
            if (bones == null || animator == null) return;
            animator.runtimeAnimatorController = normalController; animator.applyRootMotion = normalRootMotion;
            animator.speed = normalSpeed; animator.cullingMode = normalCulling;
            gameObject.SetActive(normalActive); animator.enabled = normalEnabled;
            if (Application.isPlaying && normalActive && normalEnabled) { animator.Rebind(); animator.Update(0f); }
            for (int i = 0; i < bones.Length; i++)
                if (bones[i] != null) bones[i].SetLocalPositionAndRotation(positions[i], rotations[i]);
            transform.SetPositionAndRotation(normalPosition, normalRotation);
        }

        private void OnDisable() => Stop();
    }
}
