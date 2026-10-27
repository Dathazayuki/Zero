using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Zero.AnomalyPrototype.Anomalies;

namespace Zero.Tests.EditMode
{
    public sealed class HorrorDollAnomalyTests
    {
        [TestCase(45f, true)]
        [TestCase(135f, false)]
        public void LimitedTrackingUsesTheChefFacingInsteadOfTheAnomalyNode(float angle, bool visible)
        {
            var root = new GameObject("Anomaly");
            var chef = new GameObject("Chef");
            var target = new GameObject("Camera");
            try
            {
                chef.transform.rotation = Quaternion.Euler(0f, 150f, 0f);
                var head = new GameObject("Head").transform;
                head.SetParent(chef.transform);
                var anomaly = root.AddComponent<HorrorDollAnomaly>();
                var serialized = new SerializedObject(anomaly);
                serialized.FindProperty("headBone").objectReferenceValue = head;
                serialized.FindProperty("dollObject").objectReferenceValue = chef;
                serialized.FindProperty("full360Rotation").boolValue = false;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var camera = target.AddComponent<Camera>();
                anomaly.BindPlayer(target.transform, camera);
                target.transform.position = Quaternion.AngleAxis(angle, Vector3.up) * chef.transform.forward * 2f;
                Quaternion before = head.rotation;
                anomaly.ApplyLookAtCameraNow();
                Assert.That(Quaternion.Angle(before, head.rotation) > 1f, Is.EqualTo(visible));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(chef);
                Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void BoundPlayerAndCameraSurviveComponentSerialization()
        {
            var root = new GameObject("WatchingChefReferenceTest");
            GameObject clone = null;
            try
            {
                var player = new GameObject("Player");
                player.transform.SetParent(root.transform);
                var camera = player.AddComponent<Camera>();
                var original = root.AddComponent<HorrorDollAnomaly>();
                original.BindPlayer(player.transform, camera);
                var bound = new SerializedObject(original);
                Assert.That(bound.FindProperty("player").objectReferenceValue, Is.EqualTo(player.transform));
                clone = Object.Instantiate(root);
                Transform copiedPlayer = clone.transform.Find("Player");
                var serialized = new SerializedObject(clone.GetComponent<HorrorDollAnomaly>());
                Assert.That(serialized.FindProperty("player").objectReferenceValue, Is.EqualTo(copiedPlayer));
                Assert.That(serialized.FindProperty("playerCamera").objectReferenceValue,
                    Is.EqualTo(copiedPlayer.GetComponent<Camera>()));
            }
            finally
            {
                if (clone != null) Object.DestroyImmediate(clone);
                Object.DestroyImmediate(root);
            }
        }
    }
}
