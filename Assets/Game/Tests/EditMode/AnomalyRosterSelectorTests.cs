using NUnit.Framework;
using UnityEngine;
using Zero.EnvironmentLoop;
using Zero.AnomalyPrototype.Anomalies;
using Zero.AnomalyPrototype.Flow;

namespace Zero.Tests.EditMode
{
    public sealed class AnomalyRosterSelectorTests
    {
        [Test]
        public void SelectionPreviewDoesNotConsumeHistoryAndPrefersUnseenId()
        {
            var root = new GameObject("Roster test");
            var a = ScriptableObject.CreateInstance<AnomalyData>();
            var b = ScriptableObject.CreateInstance<AnomalyData>();
            var c = ScriptableObject.CreateInstance<AnomalyData>();
            Random.State randomState = Random.state;
            Random.InitState(997);
            try
            {
                a.Configure("a", "A", AnomalyType.ObjectChange, 1, 1f, false, "test");
                b.Configure("b", "B", AnomalyType.ObjectChange, 1, 1f, false, "test");
                c.Configure("c", "C", AnomalyType.ObjectChange, 1, 1f, false, "test");
                var seen = root.AddComponent<MissingClockAnomaly>(); seen.Initialize(a);
                var fresh = root.AddComponent<MissingClockAnomaly>(); fresh.Initialize(c);
                var history = new EncounterDiversityHistory();
                history.Record("a"); history.Record("b"); history.Record("other");
                var pacing = new AnomalyPacingDirector(); pacing.Reset(2, true);
                var selector = new AnomalyRosterSelector(pacing); selector.Configure(null, history);
                for (int i = 0; i < 100; i++) Assert.AreSame(fresh, selector.Select(new[] { seen, fresh }, 3, 3));
                Assert.IsFalse(history.HasEncountered("c"));
                Assert.AreEqual(1, pacing.NormalStreak);
            }
            finally
            {
                Random.state = randomState;
                Object.DestroyImmediate(root); Object.DestroyImmediate(a); Object.DestroyImmediate(b); Object.DestroyImmediate(c);
            }
        }
    }
}
