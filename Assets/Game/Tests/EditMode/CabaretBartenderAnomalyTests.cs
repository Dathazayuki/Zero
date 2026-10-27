using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Zero.AnomalyPrototype.Anomalies;
using Phase = Zero.AnomalyPrototype.Anomalies.CabaretBartenderRules.Phase;

namespace Zero.Tests.EditMode
{
    public sealed class CabaretBartenderAnomalyTests
    {
        [Test]
        public void EmptyHotReloadCachesAreRecapturedBeforeService()
        {
            var root = CreateFixture();
            try
            {
                var anomaly = root.GetComponent<CabaretBartenderAnomaly>();
                const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                typeof(CabaretBartenderAnomaly).GetField("moved", flags).SetValue(anomaly, new Transform[0]);
                typeof(CabaretBartenderAnomaly).GetField("positions", flags).SetValue(anomaly, new Vector3[0]);
                typeof(CabaretBartenderAnomaly).GetField("scales", flags).SetValue(anomaly, new Vector3[0]);
                typeof(CabaretBartenderAnomaly).GetField("rotations", flags).SetValue(anomaly, new Quaternion[0]);
                anomaly.ResetAnomaly(); anomaly.Activate();
                for (int i = 0; i < 220; i++) anomaly.Advance(.05f);
                Assert.That(root.transform.Find("glass/liquid").gameObject.activeInHierarchy, Is.True);
                anomaly.ResetAnomaly();
                Assert.That(root.transform.Find("bottle").localPosition, Is.EqualTo(new Vector3(-2f, 1.5f, 0f)));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void FilledWaterFollowsGlassWithEitherSiblingOrChildHierarchy(bool sibling)
        {
            var root = CreateFixture();
            try
            {
                var anomaly = root.GetComponent<CabaretBartenderAnomaly>();
                var glass = root.transform.Find("glass");
                var liquid = glass.Find("liquid");
                if (sibling) liquid.SetParent(root.transform, true);
                anomaly.Activate();
                for (int i = 0; i < 220; i++) anomaly.Advance(.05f);
                Assert.That(anomaly.State, Is.EqualTo(Phase.Finished));
                Assert.That(liquid.gameObject.activeInHierarchy, Is.True);
                Assert.That(Vector3.Distance(liquid.position, glass.position), Is.LessThan(.0001f));
                Assert.That(liquid.localScale.y, Is.EqualTo(1f).Within(.0001f));
                anomaly.ResetAnomaly();
                Assert.That(liquid.gameObject.activeSelf, Is.False);
                Assert.That(glass.localPosition, Is.EqualTo(Vector3.up));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void CapArcLiftsAboveStraightPathAndReturnsToExactMouthPosition()
        {
            Vector3 mouth = Vector3.up * 2f;
            Vector3 parked = mouth + new Vector3(.22f, .16f, 0f);
            Vector3 midpoint = CabaretBartenderAnomaly.CapArc(parked, mouth, .5f, .18f);
            Assert.That(midpoint.y, Is.GreaterThan(Vector3.Lerp(parked, mouth, .5f).y + .17f));
            Assert.That(CabaretBartenderAnomaly.CapArc(mouth, parked, 0f, .18f), Is.EqualTo(mouth));
            Assert.That(Vector3.Distance(CabaretBartenderAnomaly.CapArc(parked, mouth, 1f, .18f), mouth), Is.LessThan(.0001f));
        }

        [TestCase(1f)]
        [TestCase(2.8f)]
        [TestCase(4.8f)]
        [TestCase(7f)]
        [TestCase(8.7f)]
        public void InterruptedServiceRestoresPropsAndHidesWine(float seconds)
        {
            var root = CreateFixture();
            try
            {
                var anomaly = root.GetComponent<CabaretBartenderAnomaly>();
                var bottle = root.transform.Find("bottle");
                var glass = root.transform.Find("glass");
                var cap = bottle.Find("cap");
                var liquid = glass.Find("liquid");
                Vector3 start = bottle.localPosition;
                anomaly.Activate(); anomaly.Advance(seconds);
                anomaly.ResetAnomaly(); anomaly.ResetAnomaly();
                Assert.That(bottle.localPosition, Is.EqualTo(start));
                Assert.That(glass.localPosition, Is.EqualTo(Vector3.up));
                Assert.That(cap.localPosition, Is.EqualTo(Vector3.up * .435f));
                Assert.That(liquid.gameObject.activeSelf, Is.False);
                Assert.That(root.GetComponent<LineRenderer>().enabled, Is.False);
                anomaly.Activate(); anomaly.Advance(seconds);
                root.SetActive(false);
                // EditMode fixtures do not receive the PlayMode OnDisable lifecycle.
                typeof(CabaretBartenderAnomaly).GetMethod("OnDisable",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(anomaly, null);
                Assert.That(bottle.localPosition, Is.EqualTo(start));
                Assert.That(liquid.gameObject.activeSelf, Is.False);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void RearOfCounterCannotTriggerServiceButCustomerSideCan()
        {
            var root = CreateFixture();
            try
            {
                var anomaly = root.GetComponent<CabaretBartenderAnomaly>();
                var observer = root.transform.Find("observer");
                observer.position = new Vector3(0f, 2f, -1f);
                anomaly.Activate(); anomaly.Advance(1f);
                Assert.That(anomaly.State, Is.EqualTo(Phase.Waiting));
                observer.position = new Vector3(0f, 2f, 1f);
                anomaly.Advance(.1f);
                Assert.That(anomaly.State, Is.EqualTo(Phase.Fetching));
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static GameObject CreateFixture()
        {
            var root = new GameObject("BartenderTest");
            var anomaly = root.AddComponent<CabaretBartenderAnomaly>();
            var serialized = new SerializedObject(anomaly);
            var observer = Child(root.transform, "observer", new Vector3(0f, 2f, 1f));
            var bottle = Child(root.transform, "bottle", new Vector3(-2f, 1.5f, 0f));
            var glass = Child(root.transform, "glass", Vector3.up);
            var liquid = Child(glass, "liquid", Vector3.zero); liquid.gameObject.SetActive(false);
            Set(serialized, "observer", observer); Set(serialized, "bottle", bottle);
            Set(serialized, "glass", glass); Set(serialized, "liquid", liquid);
            Set(serialized, "cap", Child(bottle, "cap", Vector3.up * .435f));
            Set(serialized, "servingSurface", Child(root.transform, "surface", Vector3.up));
            var line = root.AddComponent<LineRenderer>(); line.enabled = false; line.positionCount = 8;
            Set(serialized, "stream", line);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }

        private static Transform Child(Transform parent, string name, Vector3 position)
        {
            var child = new GameObject(name).transform;
            child.SetParent(parent, false); child.localPosition = position;
            return child;
        }

        private static void Set(SerializedObject owner, string name, Object value) =>
            owner.FindProperty(name).objectReferenceValue = value;
    }
}
