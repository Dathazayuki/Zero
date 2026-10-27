using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Zero.Tests.EditMode
{
    public sealed class LegMeshRepairTests
    {
        [TestCase("human_figure_3d_model_1")]
        [TestCase("human_character_3d_model")]
        [TestCase("human_3d_model")]
        public void RepairedMeshPreservesOriginalBindAndHasNoLowerLegBridge(string name)
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TripoModels/" + name + "/" + name + ".fbx");
            var source = root.GetComponentInChildren<SkinnedMeshRenderer>(true);
            Mesh fixedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(
                "Assets/Game/ModelV2/LegMeshRepairs/" + name + "_LegsFixed.asset");
            Assert.That(fixedMesh, Is.Not.Null);
            Assert.That(fixedMesh.bindposes, Is.EqualTo(source.sharedMesh.bindposes));
            Assert.That(fixedMesh.uv.Length, Is.EqualTo(fixedMesh.vertexCount));
            Assert.That(fixedMesh.triangles.Length, Is.LessThan(source.sharedMesh.triangles.Length));
            Assert.That(Vector3.Distance(fixedMesh.bounds.size, source.sharedMesh.bounds.size), Is.LessThan(.005f));
            ValidateLowerLegs(root.transform, source, fixedMesh);
        }

        private static void ValidateLowerLegs(Transform root, SkinnedMeshRenderer source, Mesh mesh)
        {
            Vector3 hip = root.InverseTransformPoint(Bone(source, "Hips").position);
            float knee = root.InverseTransformPoint(Bone(source, "LeftLeg").position).y;
            Matrix4x4 toRoot = root.worldToLocalMatrix * source.transform.localToWorldMatrix;
            Vector3[] points = mesh.vertices;
            float ceiling = knee + mesh.bounds.size.y * .04f;
            for (int i = 0; i < points.Length; i++) points[i] = toRoot.MultiplyPoint3x4(points[i]);
            int[] triangles = mesh.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 a = points[triangles[i]], b = points[triangles[i + 1]], c = points[triangles[i + 2]];
                if (Mathf.Max(a.y, Mathf.Max(b.y, c.y)) >= ceiling) continue;
                bool spans = Mathf.Min(a.x, Mathf.Min(b.x, c.x)) < hip.x - .00001f
                    && Mathf.Max(a.x, Mathf.Max(b.x, c.x)) > hip.x + .00001f;
                Assert.That(spans, Is.False, "A lower-leg triangle still connects both legs");
            }
            foreach (BoneWeight weight in mesh.boneWeights)
                Assert.That(weight.weight0 + weight.weight1 + weight.weight2 + weight.weight3, Is.EqualTo(1f).Within(.002f));
        }

        private static Transform Bone(SkinnedMeshRenderer source, string suffix)
            => Array.Find(source.bones, bone => bone.name == "mixamorig:" + suffix);
    }
}
