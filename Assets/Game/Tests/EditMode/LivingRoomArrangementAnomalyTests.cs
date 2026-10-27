using NUnit.Framework;
using UnityEngine;
using Zero.AnomalyPrototype.Anomalies;

namespace Zero.Tests.EditMode
{
    public sealed class LivingRoomArrangementAnomalyTests
    {
        [Test]
        public void MirroredScalePreservesOtherAxesAndRestoresAfterRepeatedActivationAndReset()
        {
            var root = new GameObject("MirrorScaleTest");
            try
            {
                var target = new GameObject("Sink").transform;
                target.SetParent(root.transform);
                Vector3 normal = new Vector3(2f, 3f, 4f);
                target.localScale = normal;
                var anomaly = root.AddComponent<LivingRoomArrangementAnomaly>();
                anomaly.ConfigureScale(target, new Vector3(-0.5f, 1f, 1f));
                anomaly.Activate();
                anomaly.Activate();
                Assert.That(target.localScale, Is.EqualTo(new Vector3(-1f, 3f, 4f)));
                anomaly.ResetAnomaly();
                Assert.That(target.localScale, Is.EqualTo(normal));
                anomaly.ResetAnomaly();
                Assert.That(target.localScale, Is.EqualTo(normal));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void MissingTargetFailsSafeAndDestroyedExtraDoesNotBreakCleanup()
        {
            var root = new GameObject("MissingReferenceTest");
            try
            {
                var anomaly = root.AddComponent<LivingRoomArrangementAnomaly>();
                anomaly.Configure(new Transform[] { null }, new[] { Vector3.zero }, new[] { Vector3.zero },
                    System.Array.Empty<GameObject>(), System.Array.Empty<GameObject>());
                Assert.That(anomaly.IsConfigured, Is.False);
                Assert.DoesNotThrow(() => anomaly.Activate());
                var extra = new GameObject("Extra");
                extra.transform.SetParent(root.transform);
                extra.SetActive(false);
                anomaly.Configure(System.Array.Empty<Transform>(), System.Array.Empty<Vector3>(), System.Array.Empty<Vector3>(), new[] { extra }, System.Array.Empty<GameObject>());
                anomaly.Activate();
                Object.DestroyImmediate(extra);
                Assert.DoesNotThrow(() => anomaly.ResetAnomaly());
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void AuthoredBooksShareOnePrefabAcrossAllTwentyFourPositions()
        {
            string guid = UnityEditor.AssetDatabase.AssetPathToGUID(
                "Assets/Game/Prefabs/InfinityRoom2Demo/Anomalies/PF_IR2_ReversedBook.prefab");
            Assert.That(guid, Is.Not.Empty);
            string scene = System.IO.File.ReadAllText("Assets/Game/Scenes/Demos/InfinityRoom2_LivingRoom_Demo.unity");
            string pattern = @"m_SourcePrefab: \{fileID: \d+, guid: " + guid + @", type: 3\}";
            Assert.That(System.Text.RegularExpressions.Regex.Matches(scene, pattern).Count, Is.EqualTo(24));
        }

        [Test]
        public void RepeatedActivationAndBaselineCaptureNeverAccumulateOffsets()
        {
            var root = new GameObject("ArrangementTest");
            try
            {
                var moved = new GameObject("Chair").transform;
                moved.SetParent(root.transform);
                moved.localPosition = new Vector3(2f, 0f, 1f);
                Quaternion rotation = Quaternion.Euler(0f, 30f, 0f);
                moved.localRotation = rotation;
                var anomaly = root.AddComponent<LivingRoomArrangementAnomaly>();
                anomaly.Configure(new[] { moved }, new[] { Vector3.right }, new[] { new Vector3(0f, 180f, 0f) },
                    System.Array.Empty<GameObject>(), System.Array.Empty<GameObject>());
                anomaly.Activate();
                anomaly.CaptureNormalState();
                anomaly.Activate();
                Assert.That(moved.localPosition, Is.EqualTo(new Vector3(3f, 0f, 1f)));
                anomaly.ResetAnomaly();
                anomaly.ResetAnomaly();
                Assert.That(moved.localPosition, Is.EqualTo(new Vector3(2f, 0f, 1f)));
                Assert.That(Quaternion.Angle(moved.localRotation, rotation), Is.LessThan(0.01f));
                Assert.That(anomaly.IsActive, Is.False);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void ResetRestoresAuthoredActiveStatesForSwappedObjects()
        {
            var root = new GameObject("ArrangementTest");
            try
            {
                var extra = new GameObject("ExtraCup");
                extra.transform.SetParent(root.transform);
                extra.SetActive(false);
                var original = new GameObject("OriginalBooks");
                original.transform.SetParent(root.transform);
                var anomaly = root.AddComponent<LivingRoomArrangementAnomaly>();
                anomaly.Configure(System.Array.Empty<Transform>(), System.Array.Empty<Vector3>(), System.Array.Empty<Vector3>(), new[] { extra }, new[] { original });
                anomaly.Activate();
                Assert.That(extra.activeSelf, Is.True);
                Assert.That(original.activeSelf, Is.False);
                anomaly.ResetAnomaly();
                Assert.That(extra.activeSelf, Is.False);
                Assert.That(original.activeSelf, Is.True);
                Assert.That(anomaly.IsActive, Is.False);
            }
            finally { Object.DestroyImmediate(root); }
        }

    }
}
