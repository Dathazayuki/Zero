using NUnit.Framework;
using System.Reflection;
using UnityEngine;
using Zero.AnomalyPrototype.Anomalies;

namespace Zero.Tests.EditMode
{
    public sealed class CabaretWatchingMannequinsAnomalyTests
    {
        [TestCase(1f)]
        [TestCase(-1f)]
        public void NecksFollowInsideRoomWithoutRotatingOrMovingBodies(float facingSign)
        {
            var root = new GameObject("CabaretNeckTest");
            try
            {
                var player = Child(root, "Player"); player.position = Vector3.right * 20;
                var a = Child(root, "BodyA"); var b = Child(root, "BodyB"); b.position = Vector3.right;
                b.rotation = Quaternion.Euler(0, 100, 0);
                var neckA = Child(a.gameObject, "NeckA"); var neckB = Child(b.gameObject, "NeckB");
                neckA.localPosition = neckB.localPosition = Vector3.up * 1.5f;
                neckA.localRotation = Quaternion.Euler(0, 30, 0);
                Quaternion normal = neckA.localRotation, bodyA = a.rotation, bodyB = b.rotation;
                var room = root.AddComponent<BoxCollider>(); room.size = Vector3.one * 10; room.enabled = false;
                var anomaly = root.AddComponent<CabaretWatchingMannequinsAnomaly>();
                var facing = Vector3.forward * facingSign;
                anomaly.Configure(player, new[] { neckA, neckB }, new[] { facing, facing }, room);
                anomaly.CaptureNormalState(); anomaly.Activate(); anomaly.Advance(1);
                AssertSame(neckA.localRotation, normal);
                player.position = new Vector3(2, 1.8f, 3);
                anomaly.Advance(.1f);
                Assert.That(Quaternion.Angle(neckA.localRotation, normal), Is.InRange(.01f, 9.01f));
                anomaly.Advance(4); AssertFaces(neckA, player, facing); AssertFaces(neckB, player, facing);
                AssertSame(a.rotation, bodyA); AssertSame(b.rotation, bodyB);
                Assert.That(a.position, Is.EqualTo(Vector3.zero)); Assert.That(b.position, Is.EqualTo(Vector3.right));
                anomaly.ResetAnomaly(); anomaly.ResetAnomaly(); AssertSame(neckA.localRotation, normal);
                anomaly.Activate(); anomaly.Advance(4);
                anomaly.SendMessage("OnDisable", SendMessageOptions.RequireReceiver);
                AssertSame(neckA.localRotation, normal);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void EmptyCachedRotationsAfterReloadAreRecapturedOnActivation()
        {
            var root = new GameObject("CabaretNeckReloadTest");
            try
            {
                var player = Child(root, "Player"); player.position = Vector3.right * 2;
                var neck = Child(root, "Neck"); neck.localRotation = Quaternion.Euler(0, 35, 0);
                var original = neck.localRotation;
                var room = root.AddComponent<BoxCollider>(); room.size = Vector3.one * 10;
                var anomaly = root.AddComponent<CabaretWatchingMannequinsAnomaly>();
                anomaly.Configure(player, new[] { neck }, new[] { Vector3.forward }, room);
                typeof(CabaretWatchingMannequinsAnomaly).GetField("normalRotations", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(anomaly, new Quaternion[0]);
                anomaly.ResetAnomaly(); anomaly.Activate(); anomaly.Advance(2); anomaly.ResetAnomaly();
                AssertSame(neck.localRotation, original);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void LookingDirectlyBehindTurnsAroundTheNeckWithoutFlippingTheHead()
        {
            var root = new GameObject("CabaretNeckBehindTest");
            try
            {
                var player = Child(root, "Player"); player.position = Vector3.back * 2;
                var neck = Child(root, "Neck");
                var room = root.AddComponent<BoxCollider>(); room.size = Vector3.one * 10;
                var anomaly = root.AddComponent<CabaretWatchingMannequinsAnomaly>();
                anomaly.Configure(player, new[] { neck }, new[] { Vector3.forward }, room);
                anomaly.CaptureNormalState(); anomaly.Activate(); anomaly.Advance(4);
                AssertFaces(neck, player, Vector3.forward);
                Assert.That(Vector3.Dot(neck.up, Vector3.up), Is.GreaterThan(.999f));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void HiddenOrDestroyedNecksAndMissingConfigurationAreSafe()
        {
            var root = new GameObject("CabaretNeckCleanupTest");
            try
            {
                var player = Child(root, "Player"); player.position = Vector3.right * 2;
                var figure = Child(root, "HiddenNeck"); figure.gameObject.SetActive(false);
                var room = root.AddComponent<BoxCollider>(); room.size = Vector3.one * 10;
                var anomaly = root.AddComponent<CabaretWatchingMannequinsAnomaly>();
                Assert.That(anomaly.IsConfigured, Is.False); anomaly.Advance(1); anomaly.ResetAnomaly();
                anomaly.Configure(player, new[] { figure }, new[] { Vector3.forward }, room);
                anomaly.CaptureNormalState(); anomaly.Activate(); anomaly.Advance(2);
                AssertSame(figure.localRotation, Quaternion.identity);
                Assert.That(figure.gameObject.activeSelf, Is.False);
                Object.DestroyImmediate(figure.gameObject); anomaly.Advance(1); anomaly.ResetAnomaly();
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static Transform Child(GameObject parent, string name)
        {
            var child = new GameObject(name).transform; child.SetParent(parent.transform, false); return child;
        }

        private static void AssertFaces(Transform neck, Transform player, Vector3 facing)
            => Assert.That(Vector3.Dot(neck.TransformDirection(facing), (player.position - neck.position).normalized), Is.GreaterThan(.999f));

        private static void AssertSame(Quaternion actual, Quaternion expected)
            => Assert.That(Mathf.Abs(Quaternion.Dot(actual, expected)), Is.GreaterThan(.99999f));
    }
}
