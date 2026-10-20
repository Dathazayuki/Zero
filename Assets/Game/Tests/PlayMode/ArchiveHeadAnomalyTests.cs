using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Zero.AnomalyPrototype.Anomalies;
using Zero.AnomalyPrototype.Player;

namespace Zero.Tests.PlayMode
{
    public sealed class ArchiveHeadAnomalyTests
    {
        [UnityTest]
        public IEnumerator ResetDuringWarningRestoresHingedBoxAndHidesCreature()
        {
            ArchiveHeadAnomaly anomaly = Create(out GameObject root, out GameObject inactivePlayer);
            Transform box = root.transform.Find("Box");
            Transform lid = box.Find("Lid");
            Transform head = root.transform.Find("Head");
            anomaly.Activate();
            yield return new WaitForSeconds(0.15f);
            Assert.That(anomaly.State, Is.EqualTo(ArchiveHeadRules.Phase.Rattling));
            anomaly.ResetAnomaly(); anomaly.ResetAnomaly();
            Assert.That(box.localPosition, Is.EqualTo(Vector3.zero));
            Assert.That(head.gameObject.activeSelf, Is.False);
            Assert.That(Quaternion.Angle(lid.localRotation, Quaternion.identity), Is.LessThan(0.001f));
            anomaly.Activate();
            yield return null;
            anomaly.enabled = false;
            Assert.That(anomaly.IsActive, Is.False);
            Assert.That(anomaly.State, Is.EqualTo(ArchiveHeadRules.Phase.Waiting));
            Object.Destroy(root); Object.Destroy(inactivePlayer);
        }

        [UnityTest]
        public IEnumerator ApproachingBehindAnObstructionWhileLookingAwayStillDropsTheBox()
        {
            ArchiveHeadAnomaly anomaly = Create(out GameObject root, out GameObject inactivePlayer);
            GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                Transform box = root.transform.Find("Box");
                Transform camera = root.transform.Find("Camera");
                box.position = Vector3.up * 1.92f;
                camera.SetPositionAndRotation(new Vector3(0f, 1.68f, -2f), Quaternion.Euler(0f, 180f, 0f));
                obstacle.transform.position = new Vector3(0f, 1.8f, -1f);
                anomaly.Activate();
                yield return null;
                Assert.That(anomaly.State, Is.EqualTo(ArchiveHeadRules.Phase.Rattling));
                yield return new WaitForSeconds(1.45f);
                Assert.That(anomaly.State, Is.EqualTo(ArchiveHeadRules.Phase.Falling));
                Assert.That(box.position.y, Is.LessThan(1.92f));
                anomaly.ResetAnomaly();
                Assert.That(box.position.y, Is.EqualTo(1.92f).Within(0.001f));
            }
            finally
            {
                Object.Destroy(obstacle); Object.Destroy(root); Object.Destroy(inactivePlayer);
            }
        }

        [UnityTest]
        public IEnumerator PlayerColliderDoesNotHideTheHeadButWorldObstaclesStillDo()
        {
            ArchiveHeadAnomaly anomaly = Create(out GameObject root, out GameObject inactivePlayer);
            Transform camera = root.transform.Find("Camera");
            GameObject playerCollider = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                playerCollider.transform.SetParent(camera, false);
                playerCollider.transform.localPosition = Vector3.forward * 0.3f;
                playerCollider.transform.localScale = Vector3.one * 0.2f;
                obstacle.transform.position = new Vector3(10f, 0f, 0f);
                Physics.SyncTransforms();
                Assert.That(anomaly.IsCreatureObserved, Is.True, "Player collider must not block its own view.");
                obstacle.transform.position = new Vector3(0f, 0f, -1f);
                Physics.SyncTransforms();
                Assert.That(anomaly.IsCreatureObserved, Is.False, "A world obstacle must still hide the head.");
                obstacle.SetActive(false);
                camera.rotation = Quaternion.Euler(0f, 180f, 0f);
                Assert.That(anomaly.IsCreatureObserved, Is.False, "Turning away must remain safe.");
                yield return null;
            }
            finally
            {
                Object.Destroy(playerCollider); Object.Destroy(obstacle);
                Object.Destroy(root); Object.Destroy(inactivePlayer);
            }
        }

        private static ArchiveHeadAnomaly Create(out GameObject root, out GameObject inactivePlayer)
        {
            root = new GameObject("HeadTest");
            Transform box = Child(root.transform, "Box");
            Transform lid = Child(box, "Lid");
            Transform head = Child(root.transform, "Head");
            var rig = head.gameObject.AddComponent<ArchiveHeadLegs>();
            var joints = new Transform[6];
            for (int i = 0; i < 6; i++) joints[i] = Child(head, "Leg" + i);
            rig.Configure(joints);
            head.gameObject.SetActive(false);
            var camera = Child(root.transform, "Camera").gameObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 0f, -2f);
            inactivePlayer = new GameObject("InactiveDeath");
            inactivePlayer.SetActive(false);
            var death = inactivePlayer.AddComponent<PlayerDeathController>();
            var anomaly = root.AddComponent<ArchiveHeadAnomaly>();
            anomaly.Configure(box, lid, rig);
            anomaly.BindPlayer(camera, death, root.AddComponent<AnomalyDeathPresentation>());
            return anomaly;
        }

        private static Transform Child(Transform parent, string name)
        {
            var child = new GameObject(name).transform;
            child.SetParent(parent, false);
            return child;
        }
    }
}
