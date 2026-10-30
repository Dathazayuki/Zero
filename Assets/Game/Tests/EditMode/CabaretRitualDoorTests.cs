using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Zero.AnomalyPrototype.Anomalies;
using Zero.AnomalyPrototype.Interaction;

namespace Zero.Tests.EditMode
{
    public sealed class CabaretRitualDoorTests
    {
        private GameObject owner, entrance;

        [SetUp]
        public void SetUp()
        {
            owner = new GameObject("Ritual door owner");
            entrance = new GameObject("Shared entrance");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(owner);
            Object.DestroyImmediate(entrance);
        }

        [TestCase(0, false)]
        [TestCase(1, true)]
        [TestCase(2, true)]
        public void AcceptsSingleAndLegacyDoubleDoors(int count, bool configured)
        {
            CabaretRitualDoor door = Configure(count);
            Assert.That(door.IsConfigured, Is.EqualTo(configured));
        }

        [Test]
        public void SharedDoorLockAndRestorePreserveExternalLeafAndAutomaticControl()
        {
            CabaretRitualDoor door = Configure(1);
            var automatic = entrance.AddComponent<PushDoorController>();
            var data = new SerializedObject(door);
            data.FindProperty("automaticDoor").objectReferenceValue = automatic;
            data.ApplyModifiedPropertiesWithoutUndo();
            Quaternion normal = entrance.transform.localRotation = Quaternion.Euler(0, 70, 0);

            door.SetClosed(true);
            Assert.That(door.IsLocked, Is.True);
            Assert.That(automatic.enabled, Is.False);
            Assert.That(Quaternion.Angle(entrance.transform.localRotation, normal), Is.LessThan(.01f));
            entrance.transform.localRotation = Quaternion.identity;
            door.Restore();
            door.Restore();

            Assert.That(door.IsLocked, Is.False);
            Assert.That(automatic.enabled, Is.True);
            Assert.That(entrance.activeSelf, Is.True);
            Assert.That(owner.GetComponent<BoxCollider>().enabled, Is.False);
            Assert.That(Quaternion.Angle(entrance.transform.localRotation, normal), Is.LessThan(.01f));
        }

        private CabaretRitualDoor Configure(int count)
        {
            var blocker = owner.AddComponent<BoxCollider>();
            var door = owner.AddComponent<CabaretRitualDoor>();
            var data = new SerializedObject(door);
            data.FindProperty("blocker").objectReferenceValue = blocker;
            SerializedProperty leaves = data.FindProperty("leaves");
            leaves.arraySize = count;
            for (int i = 0; i < count; i++)
                leaves.GetArrayElementAtIndex(i).objectReferenceValue = entrance.transform;
            data.ApplyModifiedPropertiesWithoutUndo();
            return door;
        }

        [Test]
        public void CaptureBeforeAutomaticDoorStartsRestoresConfiguredOpenAngle()
        {
            CabaretRitualDoor door = Configure(1);
            var automatic = entrance.AddComponent<PushDoorController>();
            var data = new SerializedObject(door);
            data.FindProperty("automaticDoor").objectReferenceValue = automatic;
            data.ApplyModifiedPropertiesWithoutUndo();
            entrance.transform.localRotation = Quaternion.identity;

            door.Capture();
            door.SetClosed(true);
            door.Restore();

            Quaternion expected = Quaternion.Euler(0, automatic.OpenAngle, 0);
            Assert.That(Quaternion.Angle(entrance.transform.localRotation, expected), Is.LessThan(.01f));
        }
    }
}
