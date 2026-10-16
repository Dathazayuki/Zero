using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Zero.AnomalyPrototype.Flow;

namespace Zero.Tests.PlayMode
{
    public sealed class CampaignMinigameContractTests
    {
        private GameObject root;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Campaign contract test");
            root.SetActive(false);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(root);

        [Test]
        public void FirstRoom2CycleIsSafeButRetryDoesNotRepeatTutorial()
        {
            var lights = root.AddComponent<Room2LightController>();
            lights.Begin(false);
            Call(lights, "Enter", Room2LightPhase.On);
            Assert.IsFalse(lights.IsPunishing);
            lights.Begin(true);
            Call(lights, "Enter", Room2LightPhase.On);
            Assert.IsTrue(lights.IsPunishing);
            lights.StopCycle();
            Assert.IsFalse(lights.IsPunishing);
        }

        [Test]
        public void ResetBothRoomsIsIdempotentAndRearmsConsumedRelays()
        {
            var flow = root.AddComponent<TrueEndingGameplayController>();
            var relay = root.AddComponent<TrueEndingTriggerRelay>();
            Set(flow, "minigameTriggers", new[] { relay });
            Set(flow, "room2Started", true);
            Set(flow, "room2TutorialPlayed", true);
            Set(flow, "<Phase>k__BackingField", TrueEndingPhase.Room2);
            Set(relay, "consumed", true);
            flow.ResetMinigameAttempt();
            flow.ResetMinigameAttempt();
            Assert.AreEqual(TrueEndingPhase.Inactive, flow.Phase);
            Assert.IsFalse((bool)Get(flow, "room2Started"));
            Assert.IsFalse((bool)Get(relay, "consumed"));
            Assert.IsTrue((bool)Get(flow, "room2TutorialPlayed"));
        }

        [Test]
        public void MissingReferencesCannotArmAContractOrAlignALevel()
        {
            var level = root.AddComponent<CampaignLevelBootstrap>();
            var rooms = root.AddComponent<RoomsMinigameSession>();
            Assert.IsFalse(level.AlignTo(root.transform));
            Assert.IsFalse(level.IsReady);
            Assert.IsFalse(rooms.IsReady);
            rooms.ResetAttempt();
            rooms.ResetAttempt();
        }

        private static void Set(object target, string name, object value) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

        private static object Get(object target, string name) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);

        private static void Call(object target, string name, object argument) =>
            target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, new[] { argument });
    }
}
