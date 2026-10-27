using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Zero.AnomalyPrototype.Anomalies;
using Zero.AnomalyPrototype.Player;

namespace Zero.Tests.EditMode
{
    public sealed class CabaretChandelierAnomalyTests
    {
        [TestCase(true)]
        [TestCase(false)]
        public void ActivationPreservesAuthoredHandAnimatorState(bool animated)
        {
            var root = new GameObject("ChandelierHandAnimationTest");
            try
            {
                var hand = new GameObject("Hand"); hand.transform.SetParent(root.transform);
                var animator = hand.AddComponent<Animator>(); animator.enabled = animated;
                var anomaly = ConfigureHandFixture(root, hand);
                for (int i = 0; i < 2; i++)
                {
                    anomaly.Activate();
                    Assert.That(hand.activeSelf, Is.True);
                    Assert.That(animator.enabled, Is.EqualTo(animated), "Activating the trap must not freeze the hand animation");
                    anomaly.ResetAnomaly(); anomaly.ResetAnomaly();
                    Assert.That(hand.activeSelf, Is.False);
                    Assert.That(animator.enabled, Is.EqualTo(animated));
                }
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static CabaretChandelierAnomaly ConfigureHandFixture(GameObject root, GameObject hand)
        {
            var player = root.AddComponent<FirstPersonController>();
            var cameraObject = new GameObject("Camera"); cameraObject.transform.SetParent(root.transform);
            Assign(player, "playerCamera", cameraObject.AddComponent<Camera>());
            var anomaly = root.AddComponent<CabaretChandelierAnomaly>();
            Assign(anomaly, "chandelier", root.transform);
            Assign(anomaly, "hand", hand);
            Assign(anomaly, "approachVolume", root.AddComponent<BoxCollider>());
            Assign(anomaly, "strikeVolume", root.AddComponent<BoxCollider>());
            Assign(anomaly, "player", player);
            Assign(anomaly, "death", root.AddComponent<PlayerDeathController>());
            return anomaly;
        }

        private static void Assign(Object target, string field, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        [TestCase(0f)]
        [TestCase(90f)]
        [TestCase(180f)]
        [TestCase(270f)]
        public void ImpactLookFacesUpWithoutRollingForAnyPlayerYaw(float yaw)
        {
            Quaternion body = Quaternion.Euler(0f, yaw, 0f);
            Quaternion local = Quaternion.Inverse(body) * CabaretChandelierAnomaly.ImpactLook(body);
            Assert.That(Quaternion.Angle(local, Quaternion.Euler(-85f, 0f, 0f)), Is.LessThan(.01f));
            Assert.That(Vector3.Dot(local * Vector3.up, Vector3.up), Is.GreaterThan(0f));
        }

        [Test]
        public void FastFallCannotSkipPlayerButLeavingFootprintIsSafe()
        {
            var before = new Bounds(new Vector3(0, 3, 0), new Vector3(1.6f, 1, 1.4f));
            var after = new Bounds(new Vector3(0, .6f, 0), before.size);
            var player = new Bounds(new Vector3(0, .9f, 0), new Vector3(.5f, 1.8f, .5f));
            Assert.That(CabaretChandelierAnomaly.SweptHit(before, after, player), Is.True);
            player.center += Vector3.right * 2f;
            Assert.That(CabaretChandelierAnomaly.SweptHit(before, after, player), Is.False);
        }

        [Test]
        public void MissingReferencesResetAndDisableAreSafe()
        {
            var root = new GameObject("ChandelierResetTest");
            try
            {
                var anomaly = root.AddComponent<CabaretChandelierAnomaly>();
                Assert.That(anomaly.IsConfigured, Is.False);
                anomaly.ResetAnomaly(); anomaly.ResetAnomaly(); root.SetActive(false);
                Assert.That(anomaly.IsActive, Is.False);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
