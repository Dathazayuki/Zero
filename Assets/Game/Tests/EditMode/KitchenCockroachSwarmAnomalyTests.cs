using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Zero.AnomalyPrototype.Anomalies;

namespace Zero.Tests.EditMode
{
    public sealed class KitchenCockroachSwarmAnomalyTests
    {
        [Test]
        public void ApproachTriggersOnceAndRepeatedResetAllowsFreshEncounter()
        {
            var root = new GameObject("CockroachTest");
            try
            {
                Transform player = Child(root.transform, "Player");
                Transform insect = Child(root.transform, "Insect");
                Transform route = Child(root.transform, "Route");
                Transform start = Child(route, "Start");
                Child(route, "End").position = Vector3.up * 3f;
                var anomaly = root.AddComponent<KitchenCockroachSwarmAnomaly>();
                var serialized = new SerializedObject(anomaly);
                serialized.FindProperty("player").objectReferenceValue = player;
                Bind(serialized.FindProperty("insects"), insect);
                Bind(serialized.FindProperty("routes"), route);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                player.position = Vector3.forward * 20f;
                anomaly.Activate();
                anomaly.SendMessage("Update");
                Assert.That(anomaly.HasScattered, Is.False);
                player.position = Vector3.zero;
                anomaly.SendMessage("Update");
                Assert.That(anomaly.HasScattered, Is.True);
                player.position = Vector3.forward * 20f;
                anomaly.SendMessage("Update");
                Assert.That(anomaly.HasScattered, Is.True);
                anomaly.ResetAnomaly(); anomaly.ResetAnomaly();
                Assert.That(insect.gameObject.activeSelf || anomaly.HasScattered, Is.False);
                anomaly.Activate();
                Assert.That(insect.position, Is.EqualTo(start.position));
                Assert.That(insect.gameObject.activeSelf, Is.True);
                Assert.That(anomaly.HasScattered, Is.False);
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static Transform Child(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static void Bind(SerializedProperty property, Transform value)
        {
            property.arraySize = 1;
            property.GetArrayElementAtIndex(0).objectReferenceValue = value;
        }
    }
}
