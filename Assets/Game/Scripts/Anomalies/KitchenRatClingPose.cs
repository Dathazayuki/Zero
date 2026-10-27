using UnityEngine;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class KitchenRatClingPose
    {
        private readonly Animator animator;
        private readonly Transform root;
        private readonly Transform[] bones;
        private readonly Vector3[] positions, restPositions, pivots = new Vector3[4], feet = new Vector3[4];
        private readonly Quaternion[] rotations, restRotations, limbRotations = new Quaternion[4];
        private readonly int[] groups;
        private readonly bool[] straighten, joints, tips;
        private readonly Vector3[] axes, directions = new Vector3[4];
        private float blendAmount;
        private Transform head, nose;
        private Vector3 headPosition, nosePosition;
        private Quaternion headRotation;
        private bool active, wasEnabled;

        public KitchenRatClingPose(Animator actor)
        {
            animator = actor; root = actor.transform;
            bones = root.GetComponentsInChildren<Transform>(true);
            positions = new Vector3[bones.Length]; rotations = new Quaternion[bones.Length];
            restPositions = new Vector3[bones.Length]; restRotations = new Quaternion[bones.Length];
            groups = new int[bones.Length];
            straighten = new bool[bones.Length]; joints = new bool[bones.Length]; axes = new Vector3[bones.Length];
            tips = new bool[bones.Length];
            for (int i = 0; i < bones.Length; i++) CacheBone(i);
        }
        private void CacheBone(int index)
        {
            Transform bone = bones[index]; string name = bone.name;
            restPositions[index] = bone.localPosition; restRotations[index] = bone.localRotation;
            groups[index] = LimbGroup(name);
            joints[index] = name.Contains("_Arm") || name.Contains("_Calf") || name.Contains("_Thigh") || name.StartsWith("LegThigh");
            straighten[index] = joints[index] || name.Contains("_Foot") || name.Contains("_Hand") || name.Contains(".ik.");
            tips[index] = name.Contains("Finger") || name.Contains("Toe");
            if (name == "Head_Head.001") head = bone;
            if (name == "Head_Nose.001") nose = bone;
        }
        private static int LimbGroup(string name)
        {
            bool left = name.Contains(".L"), right = name.Contains(".R");
            bool fore = name.StartsWith("Fore"), hind = name.StartsWith("Leg");
            return (left || right) && (fore || hind) ? (fore ? 0 : 2) + (right ? 1 : 0) : -1;
        }
        public void Begin()
        {
            if (active) return;
            wasEnabled = animator.enabled; animator.enabled = false; active = true;
            for (int i = 0; i < bones.Length; i++)
            {
                positions[i] = root.InverseTransformPoint(bones[i].position);
                rotations[i] = Quaternion.Inverse(root.rotation) * bones[i].rotation;
                if (bones[i].childCount > 0) axes[i] = root.InverseTransformVector(bones[i].GetChild(0).position - bones[i].position);
                CacheAnchor(bones[i].name, positions[i]);
            }
            if (head == null) return;
            headPosition = root.InverseTransformPoint(head.position);
            headRotation = Quaternion.Inverse(root.rotation) * head.rotation;
            nosePosition = nose != null ? root.InverseTransformPoint(nose.position) : headPosition + Vector3.forward * 0.1f;
        }
        private void CacheAnchor(string name, Vector3 point)
        {
            switch (name)
            {
                case "Fore_Clavicle.L.001": pivots[0] = point; break;
                case "Fore_Clavicle.R.001": pivots[1] = point; break;
                case "LegThigh.L.001": pivots[2] = point; break;
                case "LegThigh.R.001": pivots[3] = point; break;
                case "ForeHand.ik.L": feet[0] = point; break;
                case "ForeHand.ik.R": feet[1] = point; break;
                case "LegFoot.ik.L": feet[2] = point; break;
                case "LegFoot.ik.R": feet[3] = point; break;
            }
        }
        public void Apply(Transform camera, float amount)
        {
            if (!active) return;
            float blend = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(amount));
            blendAmount = blend;
            for (int i = 0; i < 4; i++)
            {
                Vector3 original = root.TransformVector(feet[i] - pivots[i]);
                Vector3 target = SpreadDirection(camera.right, camera.up, camera.forward, i);
                directions[i] = target;
                limbRotations[i] = original.sqrMagnitude > 0.000001f ?
                    Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(original, target), blend) : Quaternion.identity;
            }
            for (int i = 0; i < bones.Length; i++) ApplyBone(i);
            AimHead(camera.position, blend);
        }
        public static Vector3 SpreadDirection(Vector3 right, Vector3 up, Vector3 forward, int limb)
        {
            // Belly faces the camera; the limbs lie across the camera plane.
            float side = limb % 2 == 0 ? 1f : -1f;
            float height = limb < 2 ? 0.65f : -0.65f;
            return (right * side + up * height).normalized;
        }
        public static Quaternion BodyRotation(Transform camera) => Quaternion.LookRotation(camera.up, camera.forward);
        private void ApplyBone(int index)
        {
            int group = groups[index];
            if (group < 0 || bones[index] == null) return;
            Vector3 pivot = root.TransformPoint(pivots[group]);
            Vector3 point = pivot + limbRotations[group] * (root.TransformPoint(positions[index]) - pivot);
            Quaternion rotation = limbRotations[group] * root.rotation * rotations[index];
            if (straighten[index]) point = Vector3.Lerp(point, pivot + directions[group] * Vector3.Distance(pivot, root.TransformPoint(positions[index])), blendAmount);
            if (tips[index])
            {
                Vector3 foot = root.TransformPoint(feet[group]);
                point += (directions[group] * Vector3.Distance(pivot, foot) - limbRotations[group] * (foot - pivot)) * blendAmount;
            }
            if (joints[index] && axes[index].sqrMagnitude > 0.000001f)
                rotation = Quaternion.Slerp(rotation, Quaternion.FromToRotation(root.TransformVector(axes[index]), directions[group]) * root.rotation * rotations[index], blendAmount);
            bones[index].SetPositionAndRotation(point, rotation);
        }
        private void AimHead(Vector3 target, float blend)
        {
            if (head == null) return;
            head.SetPositionAndRotation(root.TransformPoint(headPosition), root.rotation * headRotation);
            Vector3 facing = root.TransformVector(nosePosition - headPosition);
            Vector3 direction = target - head.position;
            if (facing.sqrMagnitude < 0.000001f || direction.sqrMagnitude < 0.000001f) return;
            head.rotation = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(facing, direction), blend) * head.rotation;
            head.position += direction.normalized * (0.035f * blend);
        }
        public void Reset()
        {
            if (!active) return;
            for (int i = 1; i < bones.Length; i++)
                if (bones[i] != null) bones[i].SetLocalPositionAndRotation(restPositions[i], restRotations[i]);
            animator.enabled = wasEnabled; active = false;
        }
    }
}
