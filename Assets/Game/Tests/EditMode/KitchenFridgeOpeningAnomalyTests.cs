using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Zero.AnomalyPrototype.Anomalies;
using Zero.AnomalyPrototype.Interaction;

namespace Zero.Tests.EditMode
{
    public sealed class KitchenFridgeOpeningAnomalyTests
    {
        [Test]
        public void NearPlayerOpensBothDoorsOnceAndResetRestoresInteractionWithoutChangingContents()
        {
            var root = new GameObject("FridgeOpeningTest");
            try
            {
                var player = Child(root, "Player");
                var contents = Child(root, "FutureContents");
                var left = Child(root, "Left").AddComponent<ApplianceDoor>();
                var right = Child(root, "Right").AddComponent<ApplianceDoor>();
                left.Configure(left.transform, new Vector3(0f, -105f, 0f));
                right.Configure(right.transform, new Vector3(0f, 105f, 0f));
                var anomaly = root.AddComponent<KitchenFridgeOpeningAnomaly>();
                var so = new SerializedObject(anomaly);
                so.FindProperty("player").objectReferenceValue = player.transform;
                var doors = so.FindProperty("doors"); doors.arraySize = 2;
                doors.GetArrayElementAtIndex(0).objectReferenceValue = left;
                doors.GetArrayElementAtIndex(1).objectReferenceValue = right;
                so.ApplyModifiedPropertiesWithoutUndo();
                player.transform.localPosition = Vector3.forward * 10f;
                anomaly.Activate(); anomaly.CheckApproach();
                Assert.That(left.IsOpen || right.IsOpen, Is.False);
                left.Interact(null);
                Assert.That(left.IsOpen, Is.False);
                player.transform.localPosition = Vector3.forward;
                anomaly.CheckApproach(); anomaly.CheckApproach();
                Assert.That(left.IsOpen && right.IsOpen && anomaly.HasOpened, Is.True);
                Assert.That(contents.activeSelf, Is.True);
                anomaly.ResetAnomaly(); anomaly.ResetAnomaly();
                Assert.That(left.IsOpen || right.IsOpen || anomaly.HasOpened, Is.False);
                Assert.That(left.InteractionEnabled && right.InteractionEnabled, Is.True);
                Assert.That(contents.activeSelf, Is.True);
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static GameObject Child(GameObject parent, string name)
        {
            var go = new GameObject(name); go.transform.SetParent(parent.transform, false);
            return go;
        }
    }
}
