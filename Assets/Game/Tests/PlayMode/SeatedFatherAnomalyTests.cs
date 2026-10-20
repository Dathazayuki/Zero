#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Zero.AnomalyPrototype.Anomalies;
using Zero.AnomalyPrototype.Player;

namespace Zero.Tests.PlayMode
{
    public sealed class SeatedFatherAnomalyTests : LivingRoomFamilyAnomalyTests
    {
        [UnityTest]
        public IEnumerator FatherTracksCameraPositionInsteadOfAFixedHeadAngle()
        {
            var father = (SeatedFatherAnomaly)Select("IR2-17");
            Transform head = father.HeadBone;
            Assert.That(head, Is.Not.Null);
            Vector3 faceAxis = head.InverseTransformDirection(father.Actor.transform.forward);
            Move(new Vector3(-0.75f, 0.03f, -4.05f));
            yield return new WaitForSeconds(2.15f);
            Assert.That(father.State, Is.EqualTo(FatherState.Warning));
            Assert.That(Vector3.Dot(head.TransformDirection(faceAxis), (camera.position - head.position).normalized), Is.GreaterThan(0.95f));
            Move(new Vector3(0.15f, 0.03f, -4.05f));
            yield return new WaitForSeconds(0.15f);
            Assert.That(Vector3.Dot(head.TransformDirection(faceAxis), (camera.position - head.position).normalized), Is.GreaterThan(0.95f));
            preview.ResetPreview();
            Assert.That(father.Actor.gameObject.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator FatherTurnsHisHeadButRetreatBeforeWarningEndsIsSafe()
        {
            var father = (SeatedFatherAnomaly)Select("IR2-17");
            Vector3 seat = father.transform.Find("SeatAnchor").position;
            Assert.That(Vector3.Distance(father.Actor.GetBoneTransform(HumanBodyBones.Hips).position, seat), Is.LessThan(0.025f));
            Transform head = father.Actor.GetBoneTransform(HumanBodyBones.Head);
            Quaternion start = head.localRotation;
            Move(new Vector3(-0.35f, 0.03f, -4.05f));
            yield return new WaitForSeconds(1.4f);
            Assert.That(father.State, Is.EqualTo(FatherState.Warning));
            Assert.That(Quaternion.Angle(head.localRotation, start), Is.GreaterThan(40f));
            Move(new Vector3(2.8f, 0.03f, -4.2f));
            yield return new WaitForSeconds(1.5f);
            Assert.That(player.GetComponent<PlayerDeathController>().IsDeathInProgress, Is.False);
            Assert.That(father.State, Is.EqualTo(FatherState.Sitting));
            preview.ResetPreview();
            Assert.That(father.Actor.gameObject.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator FatherContactStartsOneDeathAndRespawnsAtTheEntrance()
        {
            var father = (SeatedFatherAnomaly)Select("IR2-17");
            Move(new Vector3(-0.35f, 0.03f, -4.05f));
            yield return new WaitForSeconds(4f);
            var death = player.GetComponent<PlayerDeathController>();
            Assert.That(death.IsDeathInProgress, Is.True);
            Assert.That(father.State, Is.EqualTo(FatherState.Caught));
            yield return new WaitForSeconds(5f);
            Assert.That(death.IsDeathInProgress, Is.False);
            Assert.That(Vector3.Distance(player.transform.position, new Vector3(3.4f, 0.03f, -4.35f)), Is.LessThan(0.15f));
            Assert.That(father.Actor.gameObject.activeSelf, Is.False);
        }
    }
}
#endif
