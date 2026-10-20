#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Zero.AnomalyPrototype.Anomalies;

namespace Zero.Tests.PlayMode
{
    public sealed class FamilyRugAnomalyTests : LivingRoomFamilyAnomalyTests
    {
        [UnityTest]
        public IEnumerator DeathShowsChildHandsThenFatherAndMotherIdle()
        {
            var anomaly = (FamilyRugAnomaly)Select("IR2-20");
            var display = anomaly.GetComponent<FamilyRugPresentation>();
            Move(new Vector3(0.9f, 0.03f, 1.5f));
            camera.rotation = Quaternion.identity;
            yield return new WaitForSeconds(3.65f);
            Assert.That(anomaly.Phase, Is.EqualTo(FamilyRugRules.PhaseState.Caught));
            Assert.That(Vector3.Angle(camera.forward, display.HandsFocus - camera.position), Is.LessThan(12f));
            yield return new WaitForSeconds(1f);
            Assert.That(Vector3.Angle(camera.forward, display.AttackFocus - camera.position), Is.LessThan(12f));
            Transform cast = anomaly.transform.Find("Family");
            Animator father = cast.Find("SeatedFather").GetComponent<Animator>();
            Animator mother = cast.Find("SeatedMother").GetComponent<Animator>();
            Assert.That(father.GetCurrentAnimatorStateInfo(0).shortNameHash, Is.EqualTo(Animator.StringToHash("Idle")));
            Assert.That(mother.GetCurrentAnimatorStateInfo(0).shortNameHash, Is.EqualTo(Animator.StringToHash("Idle")));
            preview.ResetPreview();
        }

        [UnityTest]
        public IEnumerator LeavingBeforeGrabRestoresChairAndCancelsTheWarning()
        {
            var anomaly = (FamilyRugAnomaly)Select("IR2-20");
            Assert.That(anomaly.IsConfigured, Is.True);
            Transform chair = GameObject.Find("InfinityRoom2_LivingRoom_Demo/IR2_Furniture/IR2_DetailArmchairB").transform;
            Vector3 normal = chair.position;
            Move(new Vector3(0.9f, 0.03f, 1.5f));
            yield return new WaitForSeconds(1f);
            Assert.That(anomaly.Phase, Is.EqualTo(FamilyRugRules.PhaseState.Warning));
            Move(new Vector3(3.4f, 0.03f, -4.35f));
            yield return null;
            Assert.That(anomaly.Phase, Is.EqualTo(FamilyRugRules.PhaseState.Waiting));
            Assert.That(Vector3.Distance(chair.position, normal), Is.LessThan(0.001f));
            preview.ResetPreview();
            anomaly.ResetAnomaly();
            Assert.That(anomaly.transform.Find("Family").gameObject.activeSelf, Is.False);
        }
    }
}
#endif
