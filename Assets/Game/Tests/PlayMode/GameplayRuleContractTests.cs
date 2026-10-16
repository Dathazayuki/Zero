using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Zero.AnomalyPrototype.Flow;
using Zero.EnvironmentLoop;

namespace Zero.Tests.PlayMode
{
    public sealed class GameplayRuleContractTests
    {
        [UnityTest]
        public IEnumerator DontLookBackRule_IgnoresVerticalCameraDirection()
        {
            yield return null;
            Assert.That(Room1DontLookBackController.IsLookingBack(Vector3.up, Vector3.forward, 120f), Is.False);
        }

        [UnityTest]
        public IEnumerator MovementRule_IgnoresVerticalDisplacement()
        {
            yield return null;
            float speed = Room2MovementChallenge.HorizontalSpeed(new Vector3(0f, 10f, 0f), 0.1f);
            Assert.That(speed, Is.Zero);
        }

        [UnityTest]
        public IEnumerator BlueSkyEnding_TotalCutsceneDuration_IsWithinTargetRange()
        {
            yield return null;
            var go = new GameObject("Test_BlueSkyEnding");
            var ending = go.AddComponent<BlueSkyEndingController>();
            Assert.That(ending.TotalCutsceneSeconds, Is.InRange(20f, 25f));
            Assert.That(ending.AnimationSeconds, Is.EqualTo(3.75f).Within(0.01f));
            Assert.That(ending.WalkSeconds, Is.GreaterThan(10f));
            Object.DestroyImmediate(go);
        }

        [UnityTest]
        public IEnumerator FloorSegmentReset_RestoresBaselineOnRepeatedCalls()
        {
            var root = new GameObject("Test_FloorSegment");
            var child = new GameObject("Test_Child");
            child.transform.SetParent(root.transform, false);
            child.transform.localPosition = Vector3.right;
            var segment = root.AddComponent<FloorSegment>();
            segment.InitializeSegment();

            child.transform.localPosition = Vector3.forward;
            child.SetActive(false);
            segment.ResetSegment();
            segment.ResetSegment();

            Assert.That(child.activeSelf, Is.True);
            Assert.That(child.transform.localPosition, Is.EqualTo(Vector3.right));
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PreparedArrowDirection_RemainsStableWhenPresentationFlips()
        {
            var root = new GameObject("Test_PreparedArrowRoom");
            var room = root.AddComponent<RoomAnomalyController>();
            room.PrepareRound(TravelDirection.Up);
            room.ApplyAnomalyArrowDirection(TravelDirection.Down);

            Assert.That(room.PreparedArrowDirection, Is.EqualTo(TravelDirection.Up));
            Assert.That(room.ArrowDirection, Is.EqualTo(TravelDirection.Down));
            Object.Destroy(root);
            yield return null;
        }
    }
}
