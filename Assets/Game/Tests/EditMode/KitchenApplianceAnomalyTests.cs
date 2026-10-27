using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Zero.AnomalyPrototype.Anomalies;
using Zero.AnomalyPrototype.Interaction;

namespace Zero.Tests.EditMode
{
    public sealed class KitchenApplianceAnomalyTests
    {
        [Test]
        public void DisabledFridgeDoorIgnoresPlayerInteraction()
        {
            var root = new GameObject("DoorTest");
            try
            {
                var door = root.AddComponent<ApplianceDoor>();
                door.Configure(root.transform, new Vector3(0f, 90f, 0f));
                door.enabled = false;
                door.Interact(null);
                Assert.That(door.IsOpen, Is.False);
                Assert.That(door.InteractionPrompt, Is.Empty);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ApproachStartsEffectsAndLeavingHonorsLatchWhileResetRestoresTheAppliance(bool latch)
        {
            var root = new GameObject("ApplianceTest");
            var appliance = new GameObject("Appliance").transform;
            var player = new GameObject("Player").transform;
            var effects = new GameObject("Effects");
            effects.SetActive(false);
            try
            {
                var anomaly = root.AddComponent<KitchenApplianceAnomaly>();
                anomaly.Configure(appliance, player, effects);
                SetOptions(anomaly, latch);
                player.position = Vector3.forward * 10f;
                anomaly.Activate();
                anomaly.Tick(0.3f);
                Assert.That(anomaly.IsPresenting, Is.False);
                player.position = Vector3.forward;
                anomaly.Tick(0.3f);
                Assert.That(effects.activeSelf, Is.True);
                Assert.That(appliance.localPosition.sqrMagnitude, Is.GreaterThan(0f));
                player.position = Vector3.forward * 10f;
                anomaly.Tick(0.3f);
                Assert.That(anomaly.IsPresenting, Is.EqualTo(latch));
                anomaly.ResetAnomaly();
                anomaly.ResetAnomaly();
                Assert.That(effects.activeSelf, Is.False);
                Assert.That(appliance.localPosition, Is.EqualTo(Vector3.zero));
                Assert.That(appliance.localRotation, Is.EqualTo(Quaternion.identity));
            }
            finally
            {
                Object.DestroyImmediate(root); Object.DestroyImmediate(appliance.gameObject);
                Object.DestroyImmediate(player.gameObject); Object.DestroyImmediate(effects);
            }
        }

        private static void SetOptions(KitchenApplianceAnomaly anomaly, bool latch)
        {
            var serialized = new SerializedObject(anomaly);
            serialized.FindProperty("stayOnAfterApproach").boolValue = latch;
            serialized.FindProperty("shakeDistance").floatValue = 0.02f;
            serialized.FindProperty("shakeDegrees").floatValue = 0.6f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
