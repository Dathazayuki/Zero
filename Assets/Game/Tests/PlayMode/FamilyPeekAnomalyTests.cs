#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Zero.AnomalyPrototype.Anomalies;
using Zero.AnomalyPrototype.Player;

namespace Zero.Tests.PlayMode
{
    public sealed class FamilyPeekAnomalyTests : LivingRoomFamilyAnomalyTests
    {
        [UnityTest]
        public IEnumerator CurtainFeetRetractOnlyOnApproachAndResetCanRepeat()
        {
            var feet = (FamilyPeekAnomaly)Select("IR2-15");
            Vector3 normal = feet.Actor.localPosition;
            yield return new WaitForSeconds(0.2f);
            Assert.That(feet.HasRetreated, Is.False);
            Move(new Vector3(feet.Actor.position.x, 0.03f, feet.Actor.position.z - 0.9f));
            yield return new WaitForSeconds(1.4f);
            Assert.That(feet.HasRetreated, Is.True);
            Assert.That(feet.Actor.gameObject.activeSelf, Is.False);
            preview.ResetPreview();
            feet.ResetAnomaly();
            Assert.That(feet.Actor.localPosition, Is.EqualTo(normal));
            Assert.That(feet.HasRetreated, Is.False);
        }

        [UnityTest]
        public IEnumerator ChildBelowTableRetreatsWhenPlayerLooksDownAndDisablingCleansIt()
        {
            var child = (FamilyPeekAnomaly)Select("IR2-16");
            Vector3 normal = child.Actor.localPosition;
            Vector3 worldStart = child.Actor.position;
            Transform table = preview.transform.parent.Find("IR2_Furniture/IR2_CoffeeTable/IR2_CoffeeTable_Top");
            Vector3 tableCenter = table.GetComponent<Renderer>().bounds.center;
            Move(new Vector3(-0.65f, 0.03f, -2.1f));
            camera.rotation = Quaternion.Euler(35f, 0f, 0f);
            yield return new WaitForSeconds(0.9f);
            Assert.That(child.HasRetreated, Is.True);
            Assert.That(child.Actor.position.y, Is.EqualTo(worldStart.y).Within(0.001f));
            Assert.That(Vector3.Distance(child.Actor.position, tableCenter), Is.LessThan(Vector3.Distance(worldStart, tableCenter)));
            preview.enabled = false;
            Assert.That(child.Actor.gameObject.activeSelf, Is.False);
            Assert.That(child.Actor.localPosition, Is.EqualTo(normal));
        }
    }
}
#endif
