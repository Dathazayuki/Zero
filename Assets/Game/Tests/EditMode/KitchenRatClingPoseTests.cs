using NUnit.Framework;
using UnityEngine;
using Zero.AnomalyPrototype.Anomalies;

namespace Zero.Tests.EditMode
{
    public sealed class KitchenRatClingPoseTests
    {
        [Test]
        public void ClingSpreadsFourFeetAimsHeadAndRestoresAnimatorOnReset()
        {
            var root = new GameObject("ClingRig"); var camera = new GameObject("Observer");
            try
            {
                var animator = root.AddComponent<Animator>();
                var feet = BuildLegs(root.transform);
                Transform head = Bone(root.transform, "Head_Head.001", new Vector3(0f, 0.1f, 0.2f));
                Transform nose = Bone(head, "Head_Nose.001", Vector3.forward * 0.1f);
                camera.transform.position = new Vector3(0.2f, 0.2f, -1f);
                root.transform.rotation = KitchenRatClingPose.BodyRotation(camera.transform);
                var pose = new KitchenRatClingPose(animator);
                pose.Begin(); pose.Apply(camera.transform, 1f);
                Assert.That(animator.enabled, Is.False);
                for (int i = 0; i < feet.Length; i++)
                {
                    Assert.That(feet[i].position.x * (i % 2 == 0 ? 1f : -1f), Is.GreaterThan(0.05f));
                    Assert.That(i < 2 ? feet[i].position.y > 0.05f : feet[i].position.y < 0.05f, Is.True);
                    float pivotZ = -0.05f;
                    Assert.That(feet[i].position.z, Is.EqualTo(pivotZ).Within(0.00001f));
                }
                Vector3 facing = (nose.position - head.position).normalized;
                Assert.That(Vector3.Dot(facing, (camera.transform.position - head.position).normalized), Is.GreaterThan(0.999f));
                pose.Reset(); pose.Reset();
                Assert.That(animator.enabled, Is.True);
                Assert.That(feet[0].localPosition, Is.EqualTo(new Vector3(-0.05f, -0.05f, 0.2f)));
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(camera); }
        }
        [Test]
        public void BellyFacesCameraAndAllLegDirectionsLieInCameraPlane()
        {
            var camera = new GameObject("PoseCamera");
            try
            {
                Quaternion body = KitchenRatClingPose.BodyRotation(camera.transform);
                Assert.That(Vector3.Dot(body * Vector3.down, -camera.transform.forward), Is.GreaterThan(0.999f));
                for (int i = 0; i < 4; i++)
                {
                    Vector3 direction = KitchenRatClingPose.SpreadDirection(Vector3.right, Vector3.up, Vector3.forward, i);
                    Assert.That(Vector3.Dot(direction, Vector3.forward), Is.EqualTo(0f).Within(0.00001f));
                }
            }
            finally { Object.DestroyImmediate(camera); }
        }
        private static Transform[] BuildLegs(Transform root)
        {
            string[] pivots = { "Fore_Clavicle.L.001", "Fore_Clavicle.R.001", "LegThigh.L.001", "LegThigh.R.001" };
            string[] names = { "ForeHand.ik.L", "ForeHand.ik.R", "LegFoot.ik.L", "LegFoot.ik.R" };
            var feet = new Transform[4];
            for (int i = 0; i < feet.Length; i++)
            {
                float side = i % 2 == 0 ? -0.05f : 0.05f;
                float z = i < 2 ? 0.1f : -0.1f;
                Bone(root, pivots[i], new Vector3(side, 0.05f, z));
                feet[i] = Bone(root, names[i], new Vector3(side, -0.05f, z + 0.1f));
            }
            return feet;
        }
        private static Transform Bone(Transform parent, string name, Vector3 position)
        {
            var bone = new GameObject(name).transform;
            bone.SetParent(parent, false); bone.localPosition = position;
            return bone;
        }
    }
}
