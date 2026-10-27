using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Zero.AnomalyPrototype.Anomalies;
using Zero.AnomalyPrototype.Interaction;

namespace Zero.Tests.EditMode
{
    public sealed class KitchenOvenOpeningAnomalyTests
    {
        [Test]
        public void TrayWaitsForDoorThenSlidesOnceAndResetRestoresItsContents()
        {
            var root = new GameObject("OvenOpeningTest");
            try
            {
                var player = Child(root.transform, "Player");
                var door = Child(root.transform, "Door").gameObject.AddComponent<ApplianceDoor>();
                door.Configure(door.transform, new Vector3(-92f, 0f, 0f));
                var tray = Child(root.transform, "Tray");
                var plate = Child(tray, "UserPlate"); plate.localPosition = Vector3.up * 0.1f;
                var anomaly = root.AddComponent<KitchenOvenOpeningAnomaly>();
                var so = new SerializedObject(anomaly);
                so.FindProperty("player").objectReferenceValue = player;
                so.FindProperty("door").objectReferenceValue = door;
                so.FindProperty("tray").objectReferenceValue = tray;
                so.ApplyModifiedPropertiesWithoutUndo();
                player.localPosition = Vector3.forward * 10f;
                anomaly.Activate(); anomaly.Tick(1f);
                Assert.That(anomaly.HasTriggered, Is.False);
                player.localPosition = Vector3.forward;
                anomaly.Tick(0f); anomaly.Tick(0.1f);
                Assert.That(door.IsOpen, Is.True);
                Assert.That(tray.localPosition, Is.EqualTo(Vector3.zero));
                anomaly.Tick(1f); anomaly.Tick(10f);
                Assert.That(tray.localPosition.z, Is.EqualTo(-0.52f).Within(0.001f));
                Assert.That(plate.localPosition, Is.EqualTo(Vector3.up * 0.1f));
                Assert.That(plate.gameObject.activeSelf, Is.True);
                anomaly.ResetAnomaly(); anomaly.ResetAnomaly();
                Assert.That(tray.localPosition, Is.EqualTo(Vector3.zero));
                Assert.That(door.IsOpen, Is.False);
                Assert.That(door.InteractionEnabled, Is.True);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void SoupPresentationAppearsOnlyDuringAnomalyAndResetClearsParticles()
        {
            var root = new GameObject("SoupPresentationTest");
            try
            {
                var door = Child(root.transform, "Door").gameObject.AddComponent<ApplianceDoor>();
                door.Configure(door.transform, new Vector3(-92f, 0f, 0f));
                var soup = Child(root.transform, "Soup").gameObject;
                var steam = soup.AddComponent<ParticleSystem>();
                steam.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = steam.main; main.playOnAwake = false; soup.SetActive(false);
                var anomaly = root.AddComponent<KitchenOvenOpeningAnomaly>();
                var so = new SerializedObject(anomaly);
                so.FindProperty("player").objectReferenceValue = root.transform;
                so.FindProperty("door").objectReferenceValue = door;
                so.FindProperty("tray").objectReferenceValue = Child(root.transform, "Tray");
                so.FindProperty("presentation").objectReferenceValue = soup;
                so.ApplyModifiedPropertiesWithoutUndo();
                anomaly.Activate(); Assert.That(soup.activeSelf, Is.True);
                Assert.That(steam.isPlaying, Is.False);
                anomaly.Tick(0f); anomaly.Tick(1f);
                Assert.That(steam.isPlaying, Is.True);
                anomaly.ResetAnomaly(); anomaly.ResetAnomaly();
                Assert.That(soup.activeSelf, Is.False);
                Assert.That(steam.particleCount, Is.Zero);
                Assert.That(steam.isPlaying, Is.False);
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static Transform Child(Transform parent, string name)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            return go.transform;
        }
    }
}
