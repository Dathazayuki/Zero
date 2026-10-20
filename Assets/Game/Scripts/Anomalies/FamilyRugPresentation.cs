using UnityEngine;
using UnityEngine.AI;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class FamilyRugPresentation : MonoBehaviour
    {
        [SerializeField] private GameObject family;
        [SerializeField] private Animator[] actors;
        [SerializeField] private NavMeshAgent[] agents;
        [SerializeField] private Transform chair;
        [SerializeField] private Renderer darkening;
        [SerializeField] private Transform hands;
        [SerializeField] private Vector3[] attackOrigins;
        private static readonly int Progress = Shader.PropertyToID("_Progress");
        private static readonly int Target = Shader.PropertyToID("_Target");
        private static readonly int Idle = Animator.StringToHash("Idle");
        private Transform[] bones, heads;
        private Vector3[] positions;
        private Quaternion[] rotations;
        private Vector3 chairPosition;
        private MaterialPropertyBlock normal, display;
        public Vector3 HandsFocus => hands.position + Vector3.up * 0.12f;
        public Vector3 AttackFocus => (heads[0].position + heads[1].position) * 0.5f;
        public bool IsConfigured => family != null && actors != null && actors.Length == 2 && agents != null
            && agents.Length == 2 && chair != null && darkening != null && hands != null && attackOrigins != null && attackOrigins.Length == 2;

        public void Capture()
        {
            if (!IsConfigured || normal != null) return;
            bones = family.GetComponentsInChildren<Transform>(true);
            positions = new Vector3[bones.Length];
            rotations = new Quaternion[bones.Length];
            for (int i = 0; i < bones.Length; i++) { positions[i] = bones[i].localPosition; rotations[i] = bones[i].localRotation; }
            heads = new Transform[actors.Length];
            for (int i = 0; i < actors.Length; i++) heads[i] = actors[i].GetBoneTransform(HumanBodyBones.Head);
            chairPosition = chair.position;
            normal = new MaterialPropertyBlock();
            display = new MaterialPropertyBlock();
            darkening.GetPropertyBlock(normal);
            darkening.GetPropertyBlock(display);
        }

        public void Begin()
        {
            Capture();
            if (normal == null) return;
            family.SetActive(true);
            hands.gameObject.SetActive(false);
            darkening.enabled = false;
        }

        public void Warn(float progress, Vector3 point)
        {
            darkening.enabled = progress > 0f;
            Vector3 uv = darkening.transform.InverseTransformPoint(point);
            display.SetFloat(Progress, progress);
            display.SetVector(Target, new Vector4(Mathf.Clamp01(uv.x + 0.5f), Mathf.Clamp01(uv.y + 0.5f), 0f, 0f));
            darkening.SetPropertyBlock(display);
            Vector3 offset = point - chairPosition;
            offset.y = 0f;
            chair.position = chairPosition + Vector3.ClampMagnitude(offset, 0.35f) * progress;
        }

        public void Grab(Vector3 point, float elapsed)
        {
            hands.gameObject.SetActive(true);
            hands.position = new Vector3(point.x, Mathf.Lerp(-0.22f, 0.025f, Mathf.Clamp01(elapsed / 0.6f)), point.z);
        }

        public void BeginAttack(Vector3 point, Vector3 forward)
        {
            point.y = chairPosition.y;
            forward = FindFront(point, forward);
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            for (int i = 0; i < actors.Length; i++)
            {
                if (agents[i].enabled && agents[i].isOnNavMesh) agents[i].ResetPath();
                agents[i].enabled = false;
                Vector3 position = point + forward * 1.05f + right * (i == 0 ? -0.34f : 0.34f);
                position.y = chairPosition.y;
                actors[i].transform.SetPositionAndRotation(position, Quaternion.LookRotation((point - position).normalized, Vector3.up));
                actors[i].enabled = true;
                actors[i].speed = 1f;
                actors[i].Rebind();
                actors[i].Play(Idle, 0, 0f);
                actors[i].Update(0f);
            }
        }

        private static Vector3 FindFront(Vector3 point, Vector3 forward)
        {
            forward = Vector3.ProjectOnPlane(forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
            for (int i = 0; i < 8; i++)
            {
                float angle = i == 7 ? 180f : ((i + 1) / 2) * 35f * (i % 2 == 0 ? -1f : 1f);
                Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * forward;
                Vector3 right = Vector3.Cross(Vector3.up, direction) * 0.34f;
                Vector3 center = point + direction * 1.05f;
                if (Clear(center - right) && Clear(center + right)) return direction;
            }
            return forward;
        }

        private static bool Clear(Vector3 point) => !Physics.CheckCapsule(point + Vector3.up * 0.32f,
            point + Vector3.up * 1.5f, 0.25f, ~0, QueryTriggerInteraction.Ignore);

        public void Restore()
        {
            if (normal == null) return;
            for (int i = 0; i < actors.Length; i++)
            {
                if (agents[i].enabled && agents[i].isOnNavMesh) agents[i].ResetPath();
                agents[i].enabled = false;
                actors[i].enabled = false;
            }
            family.SetActive(false);
            for (int i = 0; i < bones.Length; i++) { bones[i].localPosition = positions[i]; bones[i].localRotation = rotations[i]; }
            hands.gameObject.SetActive(false);
            darkening.enabled = false;
            darkening.SetPropertyBlock(normal);
            chair.position = chairPosition;
        }

        private void OnDisable() => Restore();
    }
}
