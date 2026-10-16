using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Zero.AnomalyPrototype.Anomalies;
using Zero.EnvironmentLoop;

namespace Zero.Tests.EditMode
{
    public sealed class AnomalySelectionHistoryTests
    {
        private readonly List<AnomalyData> assets = new List<AnomalyData>();
        private GameObject root;
        private DirectionalAnomalyGameManager manager;
        private RoomAnomalyController room;
        private AnomalyPacingDirector history;
        private Random.State randomState;

        [SetUp]
        public void SetUp()
        {
            randomState = Random.state;
            Random.InitState(731);
            root = new GameObject("Selection test");
            manager = root.AddComponent<DirectionalAnomalyGameManager>();
            room = root.AddComponent<RoomAnomalyController>();
            history = (AnomalyPacingDirector)typeof(DirectionalAnomalyGameManager)
                .GetField("pacingDirector", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(manager);
            history.Reset(6, true);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            foreach (AnomalyData data in assets) Object.DestroyImmediate(data);
            assets.Clear();
            Random.state = randomState;
        }

        [Test]
        public void DifferentIdOfSameTypeRemainsSelectableAlongsideOtherTypes()
        {
            AnomalyBehaviour sameType = Create("new.object", AnomalyType.ObjectChange);
            room.Configure(root.transform, new[] { sameType, Create("other", AnomalyType.Environment) });
            history.RecordAnomaly("previous", AnomalyType.ObjectChange, AnomalyPacingTier.MinorAnomaly);
            bool selectedSameType = false;
            for (int i = 0; i < 100; i++) selectedSameType |= Pick(false) == sameType;
            Assert.IsTrue(selectedSameType, "A different ID must not be excluded by its content type.");
        }

        [Test]
        public void RecentIdsRemainExcludedEvenWhenTheyShareTheAvailableType()
        {
            AnomalyBehaviour fresh = Create("fresh", AnomalyType.ObjectChange);
            room.Configure(root.transform, new[] { Create("older", AnomalyType.ObjectChange),
                Create("latest", AnomalyType.ObjectChange), fresh });
            history.RecordAnomaly("older", AnomalyType.ObjectChange, AnomalyPacingTier.MinorAnomaly);
            history.RecordAnomaly("latest", AnomalyType.ObjectChange, AnomalyPacingTier.MinorAnomaly);
            history.RecordNormal();
            for (int i = 0; i < 100; i++) Assert.AreSame(fresh, Pick(true));
        }

        [Test]
        public void ExhaustedPoolNeverRelaxesImmediateIdExclusion()
        {
            room.Configure(root.transform, new[] { Create("latest", AnomalyType.ObjectChange) });
            history.RecordAnomaly("latest", AnomalyType.ObjectChange, AnomalyPacingTier.MinorAnomaly);
            Assert.IsNull(Pick(false));
            Assert.IsNull(Pick(true));
        }

        private AnomalyBehaviour Create(string id, AnomalyType type)
        {
            AnomalyData data = ScriptableObject.CreateInstance<AnomalyData>();
            data.Configure(id, id, type, 1, 1f, false, "Selection test");
            assets.Add(data);
            MissingClockAnomaly anomaly = root.AddComponent<MissingClockAnomaly>();
            anomaly.Initialize(data);
            return anomaly;
        }

        private AnomalyBehaviour Pick(bool relaxHistory)
        {
            MethodInfo method = typeof(DirectionalAnomalyGameManager).GetMethod(
                "SelectWeightedAnomaly", BindingFlags.Instance | BindingFlags.NonPublic);
            return (AnomalyBehaviour)method.Invoke(manager, new object[] { room, 3, relaxHistory });
        }
    }
}
