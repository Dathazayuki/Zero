using UnityEngine;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class KitchenWhiteRatActor : MonoBehaviour
    {
        [SerializeField] private Animator chef;
        [SerializeField] private Animator rat;
        [SerializeField] private Transform hat;
        [SerializeField] private Transform head;
        [SerializeField] private GameObject normalChef;
        [SerializeField] private Transform rollingHat;
        [SerializeField] private float floorHeight = 0.03f;
        private static readonly int Idle = Animator.StringToHash("Idle");
        private static readonly int Death = Animator.StringToHash("Death");
        private static readonly int Walk = Animator.StringToHash("Walk");
        private Transform[] bones;
        private Vector3[] positions;
        private Quaternion[] rotations;
        private Vector3 ratPosition, hatPosition, walkStart, walkEnd, hatStart;
        private Quaternion ratRotation, hatRotation;
        private bool chefActive, ratActive, normalActive;
        private bool hatActive, normalRootMotion;
        private float normalSpeed;
        private Vector3 chefPosition;
        private Quaternion chefRotation;
        private Quaternion rollingStartRotation;
        private Transform hips;
        private SkinnedMeshRenderer hatRenderer;
        private MeshFilter rollingFilter;
        private Mesh hatMesh;
        private KitchenRatClingPose clingPose;
        private readonly System.Collections.Generic.List<Vector3> hatVertices = new System.Collections.Generic.List<Vector3>();
        public Vector3 RatPosition => rat.transform.position;
        public bool DeathFinished => chef.GetCurrentAnimatorStateInfo(0).shortNameHash == Death &&
            chef.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f;

        public void Capture()
        {
            if (bones != null) return;
            bones = chef.GetComponentsInChildren<Transform>(true);
            positions = new Vector3[bones.Length]; rotations = new Quaternion[bones.Length];
            for (int i = 0; i < bones.Length; i++)
            { positions[i] = bones[i].localPosition; rotations[i] = bones[i].localRotation; }
            ratPosition = rat.transform.position; ratRotation = rat.transform.rotation;
            hatPosition = hat.localPosition; hatRotation = hat.localRotation;
            chefActive = chef.gameObject.activeSelf; ratActive = rat.gameObject.activeSelf;
            normalActive = normalChef.activeSelf;
            chefPosition = chef.transform.position; hatActive = hat.gameObject.activeSelf;
            chefRotation = chef.transform.rotation;
            normalRootMotion = chef.applyRootMotion; normalSpeed = chef.speed;
            hips = chef.isHuman ? chef.GetBoneTransform(HumanBodyBones.Hips) : chef.transform;
            hatRenderer = hat.GetComponent<SkinnedMeshRenderer>();
            if (rollingHat != null) rollingFilter = rollingHat.GetComponent<MeshFilter>();
            hatMesh = new Mesh { name = "IR4_16_HatSnapshot" };
            clingPose = new KitchenRatClingPose(rat);
        }

        public void Begin()
        {
            Capture(); normalChef.SetActive(false); chef.gameObject.SetActive(true);
            chef.applyRootMotion = false; chef.speed = 1f;
            chef.Play(Idle, 0, 0f); rat.gameObject.SetActive(false);
        }

        public void Fall()
        {
            chef.applyRootMotion = true;
            chef.speed = 1f;
            chef.Play(Death, 0, 0f);
        }
        public void BeginHat()
        {
            chef.speed = 0f;
            if (rollingHat == null || hatRenderer == null || rollingFilter == null) return;
            hatRenderer.BakeMesh(hatMesh);
            Vector3 center = hatMesh.bounds.center;
            hatMesh.GetVertices(hatVertices);
            for (int i = 0; i < hatVertices.Count; i++) hatVertices[i] -= center;
            hatMesh.SetVertices(hatVertices); hatMesh.RecalculateBounds();
            rollingFilter.sharedMesh = hatMesh;
            hatStart = hat.TransformPoint(center);
            rollingStartRotation = hat.rotation;
            rollingHat.SetPositionAndRotation(hatStart, rollingStartRotation);
            rollingHat.localScale = hat.lossyScale;
            rollingHat.gameObject.SetActive(true); hat.gameObject.SetActive(false);
        }
        public void RollHat(float elapsed)
        {
            float t = Mathf.Clamp01(elapsed);
            if (rollingHat == null) return;
            Vector3 target = hatStart - chef.transform.right * 0.85f;
            target.y = floorHeight + 0.12f;
            rollingHat.position = Vector3.Lerp(hatStart, target, t);
            rollingHat.rotation = Quaternion.AngleAxis(-t * 280f, chef.transform.forward) * rollingStartRotation;
        }

        public void BeginWalk()
        {
            RollHat(1f);
            walkStart = head.position + Vector3.up * 0.08f;
            Vector3 outward = ExitDirection(head.position, hips.position, chef.transform.forward);
            walkEnd = walkStart + outward * 0.6f;
            walkEnd.y = Mathf.Max(floorHeight + 0.04f, walkStart.y - 0.12f);
            rat.transform.SetPositionAndRotation(walkStart, Quaternion.LookRotation(outward));
            rat.gameObject.SetActive(true); rat.Play(Walk, 0, 0f);
        }

        public void WalkOut(float elapsed) => rat.transform.position = Vector3.Lerp(walkStart, walkEnd, Mathf.Clamp01(elapsed / 2f));
        public static Vector3 ExitDirection(Vector3 headPosition, Vector3 hipsPosition, Vector3 forward)
        {
            Vector3 direction = Vector3.ProjectOnPlane(headPosition - hipsPosition, Vector3.up);
            return direction.sqrMagnitude < 0.001f ? -forward : direction.normalized;
        }
        public void Rest() => rat.Play(Idle, 0, 0f);
        public void MoveRat(Vector3 point, Quaternion rotation) => rat.transform.SetPositionAndRotation(point, rotation);
        public void BeginCling() => clingPose?.Begin();
        public void PresentCling(Transform camera, float amount) => clingPose?.Apply(camera, amount);

        public void Restore()
        {
            if (bones == null) return;
            clingPose?.Reset();
            if (rollingHat != null) rollingHat.gameObject.SetActive(false);
            chef.applyRootMotion = normalRootMotion; chef.speed = normalSpeed;
            chef.Rebind();
            for (int i = 0; i < bones.Length; i++)
                if (bones[i] != null) bones[i].SetLocalPositionAndRotation(positions[i], rotations[i]);
            chef.transform.SetPositionAndRotation(chefPosition, chefRotation);
            hat.SetLocalPositionAndRotation(hatPosition, hatRotation);
            hat.gameObject.SetActive(hatActive);
            rat.Rebind(); rat.transform.SetPositionAndRotation(ratPosition, ratRotation);
            chef.gameObject.SetActive(chefActive); rat.gameObject.SetActive(ratActive);
            normalChef.SetActive(normalActive);
        }
        private void OnDestroy()
        {
            if (hatMesh == null) return;
            if (Application.isPlaying) Destroy(hatMesh);
            else DestroyImmediate(hatMesh);
        }
    }
}
