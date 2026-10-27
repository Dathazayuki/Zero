using NUnit.Framework;
using Zero.AnomalyPrototype.Anomalies;
using Phase = Zero.AnomalyPrototype.Anomalies.KitchenTableCutsceneRules.Phase;

namespace Zero.Tests.EditMode
{
    public sealed class KitchenTableCutsceneRulesTests
    {
        [Test]
        public void TurnsBeforeChoppingAndCatchesOnlyAtAnimationImpact()
        {
            var rules = new KitchenTableCutsceneRules(1.2f, 16f / 24f);
            rules.Tick(5f, false, 100f); Assert.That(rules.State, Is.EqualTo(Phase.Waiting));
            rules.Tick(0f, true, 100f); Assert.That(rules.State, Is.EqualTo(Phase.Fading));
            rules.Tick(1f, false, 100f); Assert.That(rules.State, Is.EqualTo(Phase.Turning));
            rules.Tick(1.1f, false, 100f); Assert.That(rules.State, Is.EqualTo(Phase.Turning));
            rules.Tick(0.2f, false, 0f); Assert.That(rules.State, Is.EqualTo(Phase.Chopping));
            rules.Tick(20f, false, 0.6f); Assert.That(rules.State, Is.EqualTo(Phase.Chopping));
            rules.Tick(0f, false, 16f / 24f); Assert.That(rules.State, Is.EqualTo(Phase.Caught));
            rules.Tick(100f, true, 0f); Assert.That(rules.State, Is.EqualTo(Phase.Caught));
            rules.Reset(); rules.Reset(); Assert.That(rules.State, Is.EqualTo(Phase.Waiting));
            Assert.That(rules.TurnProgress, Is.Zero);
        }

        [Test]
        public void CaptureBlacksOutImmediatelyThenRevealsTableOverOneSecond()
        {
            var rules = new KitchenTableCutsceneRules(1.2f, 0.7f);
            rules.Tick(0f, true, 0f);
            Assert.That(rules.State, Is.EqualTo(Phase.Fading));
            Assert.That(rules.FadeAlpha, Is.EqualTo(1f));
            rules.Tick(0.5f, false, 0f);
            Assert.That(rules.State, Is.EqualTo(Phase.Fading));
            Assert.That(rules.FadeAlpha, Is.EqualTo(0.5f).Within(0.001f));
            rules.Tick(0.49f, false, 100f);
            Assert.That(rules.State, Is.EqualTo(Phase.Fading));
            rules.Tick(0.02f, false, 0f);
            Assert.That(rules.State, Is.EqualTo(Phase.Turning));
            Assert.That(rules.FadeAlpha, Is.Zero);
            rules.Tick(0.25f, false, 0f);
            Assert.That(rules.FadeAlpha, Is.Zero);
            rules.Reset(); rules.Reset();
            Assert.That(rules.FadeAlpha, Is.Zero);
            rules.Tick(0f, true, 0f); rules.Tick(0.5f, true, 0f); rules.Reset();
            Assert.That(rules.State, Is.EqualTo(Phase.Waiting));
            Assert.That(rules.FadeAlpha, Is.Zero);
        }
    }
}
