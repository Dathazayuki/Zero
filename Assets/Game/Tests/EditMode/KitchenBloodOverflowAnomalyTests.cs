using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Zero.AnomalyPrototype.Anomalies;

namespace Zero.Tests.EditMode
{
    public sealed class KitchenBloodOverflowAnomalyTests
    {
        [Test]
        public void ResetWithoutRuntimeProgressIsSafeAfterReloadOrMissingReferences()
        {
            var root = new GameObject("UnconfiguredBlood");
            try
            {
                var anomaly = root.AddComponent<KitchenBloodOverflowAnomaly>();
                Assert.DoesNotThrow(() => { anomaly.ResetAnomaly(); anomaly.ResetAnomaly(); anomaly.Tick(1f); });
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void ApproachingStartsFillThenSpillAndResetClearsAllPresentation()
        {
            var root = new GameObject("BloodOverflowTest");
            try
            {
                var anomaly = root.AddComponent<KitchenBloodOverflowAnomaly>();
                var player = Child(root, "Player"); var visuals = Child(root, "Visuals");
                var basin = Child(visuals, "Basin"); var stream = Child(visuals, "Stream");
                var spill = Child(visuals, "Spill"); var pool = Child(visuals, "Pool");
                stream.transform.localScale = new Vector3(1f, 0.5f, 1f);
                var so = new SerializedObject(anomaly);
                so.FindProperty("player").objectReferenceValue = player.transform;
                so.FindProperty("visuals").objectReferenceValue = visuals;
                so.FindProperty("basinSurface").objectReferenceValue = basin.transform;
                so.FindProperty("tapStream").objectReferenceValue = stream.transform;
                so.FindProperty("overflow").objectReferenceValue = spill;
                so.FindProperty("floorPool").objectReferenceValue = pool.transform;
                so.ApplyModifiedPropertiesWithoutUndo();
                player.transform.localPosition = Vector3.forward * 10f;
                anomaly.Activate(); anomaly.Tick(5f);
                Assert.That(visuals.activeSelf, Is.False);
                player.transform.localPosition = Vector3.forward;
                anomaly.Tick(5f);
                Assert.That(anomaly.FillProgress, Is.EqualTo(0.5f));
                Assert.That(anomaly.IsOverflowing, Is.False);
                Assert.That(stream.transform.localScale.y, Is.LessThan(0.5f));
                anomaly.Tick(10f);
                Assert.That(spill.activeSelf && pool.activeSelf, Is.True);
                anomaly.ResetAnomaly(); anomaly.ResetAnomaly();
                Assert.That(visuals.activeSelf || spill.activeSelf || pool.activeSelf, Is.False);
                Assert.That(basin.transform.localPosition, Is.EqualTo(Vector3.zero));
                Assert.That(stream.transform.localScale.y, Is.EqualTo(0.5f));
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static GameObject Child(GameObject parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent.transform, false);
            return child;
        }
    }
}
