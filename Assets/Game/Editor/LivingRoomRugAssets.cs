using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Zero.Editor
{
    internal static class LivingRoomRugAssets
    {
        internal static Animator Actor(string sourceName, Transform parent, string name)
        {
            GameObject clone = UnityEngine.Object.Instantiate(LivingRoomTelevisionAssets.Source(sourceName), parent);
            clone.name = name;
            clone.SetActive(true);
            Animator rig = clone.GetComponent<Animator>();
            rig.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Game/MixamoAnimation/FamilyAnimation.controller");
            rig.applyRootMotion = false;
            rig.keepAnimatorStateOnDisable = true;
            rig.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            LivingRoomTelevisionAssets.SamplePose(rig, "Sit", 0.45f);
            return rig;
        }

        internal static void Seat(Animator actor, Transform sofa, float x)
        {
            actor.transform.rotation = sofa.rotation;
            actor.transform.position += sofa.TransformPoint(new Vector3(x, 0.68f, 0.05f)) - actor.GetBoneTransform(HumanBodyBones.Hips).position;
        }

        internal static void ChildShadow(Transform family)
        {
            Animator child = Actor("Children", family, "ChildShadowUnderTable");
            child.transform.localScale *= 0.55f;
            child.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            child.transform.position += new Vector3(-0.65f, 0.14f, 0.05f) - child.GetBoneTransform(HumanBodyBones.Hips).position;
            foreach (SkinnedMeshRenderer skin in child.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                skin.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            Light light = LivingRoomTelevisionAssets.Node(family, "ChildShadowLight").AddComponent<Light>();
            light.transform.position = new Vector3(-0.7f, 0.42f, -0.95f);
            light.transform.LookAt(new Vector3(-0.65f, 0.08f, 0.15f));
            light.type = LightType.Spot;
            light.range = 2f;
            light.spotAngle = 70f;
            light.intensity = 0.6f;
            light.shadows = LightShadows.Hard;
        }

        internal static Transform Hands(Transform family)
        {
            Transform root = LivingRoomTelevisionAssets.Node(family, "GrabbingHands").transform;
            Animator template = Actor("Children", family, "HandTemplate");
            try
            {
                template.enabled = true;
                template.Rebind();
                template.enabled = false;
                foreach (HumanBodyBones bone in new[] { HumanBodyBones.LeftHand, HumanBodyBones.RightHand })
                {
                    Transform wrist = template.GetBoneTransform(bone);
                    Transform finger = template.GetBoneTransform(bone == HumanBodyBones.LeftHand ? HumanBodyBones.LeftMiddleDistal : HumanBodyBones.RightMiddleDistal);
                    Transform hand = LivingRoomTelevisionAssets.Node(root, bone.ToString()).transform;
                    Vector3 forward = wrist.InverseTransformDirection(finger.position - wrist.position).normalized;
                    hand.localRotation = Quaternion.FromToRotation(forward, Vector3.up);
                    hand.localPosition = new Vector3(bone == HumanBodyBones.LeftHand ? -0.18f : 0.18f, -0.04f, 0f);
                    foreach (SkinnedMeshRenderer skin in template.GetComponentsInChildren<SkinnedMeshRenderer>(true)) ExtractHand(skin, wrist, hand, forward);
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(template.gameObject); }
            root.gameObject.SetActive(false);
            return root;
        }

        private static void ExtractHand(SkinnedMeshRenderer skin, Transform wrist, Transform parent, Vector3 forward)
        {
            var baked = new Mesh();
            skin.BakeMesh(baked);
            Vector3[] vertices = baked.vertices;
            Vector3[] normals = baked.normals;
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = wrist.InverseTransformPoint(skin.transform.TransformPoint(vertices[i]));
                normals[i] = wrist.InverseTransformDirection(skin.transform.TransformDirection(normals[i]));
            }
            baked.vertices = vertices;
            baked.normals = normals;
            Transform[] joints = skin.bones;
            var selected = new bool[joints.Length];
            for (int i = 0; i < joints.Length; i++) selected[i] = joints[i] == wrist || joints[i].IsChildOf(wrist);
            BoneWeight[] weights = skin.sharedMesh.boneWeights;
            int count = 0;
            for (int sub = 0; sub < baked.subMeshCount; sub++)
            {
                int[] indices = baked.GetTriangles(sub);
                var kept = new List<int>();
                for (int i = 0; i < indices.Length; i += 3)
                {
                    float weight = HandWeight(weights[indices[i]], selected) + HandWeight(weights[indices[i + 1]], selected)
                        + HandWeight(weights[indices[i + 2]], selected);
                    if (weight < 1.35f) continue;
                    kept.Add(indices[i]); kept.Add(indices[i + 1]); kept.Add(indices[i + 2]);
                }
                baked.SetTriangles(kept, sub);
                count += kept.Count;
            }
            if (count > 0) SaveHand(baked, skin, parent);
            else UnityEngine.Object.DestroyImmediate(baked);
        }

        private static float HandWeight(BoneWeight weight, bool[] selected)
        {
            return (selected[weight.boneIndex0] ? weight.weight0 : 0f) + (selected[weight.boneIndex1] ? weight.weight1 : 0f)
                + (selected[weight.boneIndex2] ? weight.weight2 : 0f) + (selected[weight.boneIndex3] ? weight.weight3 : 0f);
        }

        private static void SaveHand(Mesh mesh, SkinnedMeshRenderer source, Transform parent)
        {
            mesh.RecalculateBounds();
            string path = LivingRoomTelevisionAssets.Folder + "IR2_Rug_" + parent.name + "_" + source.name + ".asset";
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null) AssetDatabase.CreateAsset(mesh, path);
            else
            {
                EditorUtility.CopySerialized(mesh, existing);
                UnityEngine.Object.DestroyImmediate(mesh);
                mesh = existing;
                EditorUtility.SetDirty(existing);
            }
            GameObject node = LivingRoomTelevisionAssets.Node(parent, source.name);
            node.AddComponent<MeshFilter>().sharedMesh = mesh;
            node.AddComponent<MeshRenderer>().sharedMaterials = source.sharedMaterials;
        }
    }
}
