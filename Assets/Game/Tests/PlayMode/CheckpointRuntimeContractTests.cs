using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zero.AnomalyPrototype.Flow;
using Zero.EnvironmentLoop;

namespace Zero.Tests.PlayMode
{
    public sealed class CheckpointRuntimeContractTests
    {
        private GameObject runtimeObject;
        private GameObject contextObject;
        private CheckpointRuntime runtime;
        private CheckpointStore store;
        private StairLoopManager loop;
        private FloorProgressManager progress;
        private DirectionalAnomalyGameManager anomaly;
        private FloorSegment segment;
        private string directory;

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "ZeroCheckpointTests", Guid.NewGuid().ToString("N"));
            store = new CheckpointStore(directory);
            contextObject = new GameObject("CheckpointTestContext");
            contextObject.SetActive(false);
            var flow = contextObject.AddComponent<GameFlowController>();
            SetField(flow, "<Phase>k__BackingField", GameFlowPhase.LoopGameplay);
            SetField(flow, "loopSceneName", SceneManager.GetActiveScene().name);
            progress = contextObject.AddComponent<FloorProgressManager>();
            progress.Initialize();
            anomaly = contextObject.AddComponent<DirectionalAnomalyGameManager>();
            loop = contextObject.AddComponent<StairLoopManager>();
            segment = contextObject.AddComponent<FloorSegment>();
            SetField(loop, "progressManager", progress);
            SetField(loop, "anomalyGameManager", anomaly);
            SetField(loop, "<IsInitialized>k__BackingField", true);
            SetField(loop, "<CurrentSegment>k__BackingField", segment);
            runtimeObject = new GameObject("CheckpointTestRuntime");
            runtime = runtimeObject.AddComponent<CheckpointRuntime>();
            SetField(runtime, "store", store);
            SetField(runtime, "flow", flow);
            Call("BindLoop", loop);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(runtimeObject);
            UnityEngine.Object.DestroyImmediate(contextObject);
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }

        [Test]
        public void DirectLoopEntryEnablesSavingWithoutNewGame()
        {
            Call("OnSceneLoaded", SceneManager.GetActiveScene(), LoadSceneMode.Single);
            progress.RestoreLogicalFloor(-3);
            SetField(anomaly, "<DisplayedFloorNumber>k__BackingField", 5);
            Call("OnFloorCommitted", segment);
            Assert.IsTrue(store.TryLoad(out CheckpointData data));
            Assert.AreEqual(0, data.LogicalFloorIndex);
            Assert.AreEqual(0, data.DisplayedFloorNumber);
        }

        [Test]
        public void PendingContinueCannotBeOverwrittenByBootstrapCommit()
        {
            Assert.IsTrue(store.Save(CheckpointData.ForStairway(7, 8)));
            Assert.IsTrue(runtime.PrepareContinue());
            Assert.IsFalse(runtime.PrepareContinue());
            Call("OnFloorCommitted", segment);
            Assert.IsTrue(store.TryLoad(out CheckpointData data));
            Assert.AreEqual(0, data.DisplayedFloorNumber);
        }

        [Test]
        public void DeathResetReplacesPreviousProgressWithZero()
        {
            runtime.EnableAutosave();
            Assert.IsTrue(store.Save(CheckpointData.ForStairway(7, 7)));
            Call("OnRoundPrepared");
            Assert.IsTrue(store.TryLoad(out CheckpointData data));
            Assert.AreEqual(0, data.LogicalFloorIndex);
            Assert.AreEqual(0, data.DisplayedFloorNumber);
        }

        [Test]
        public void DisableUnsubscribesCommitAndRoundEvents()
        {
            runtime.enabled = false;
            Assert.IsNull(GetField(loop, "CurrentFloorCommitted"));
            Assert.IsNull(GetField(anomaly, "RoundPrepared"));
        }

        private void Call(string method, params object[] arguments)
        {
            typeof(CheckpointRuntime).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(runtime, arguments);
        }

        private static void SetField(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }

        private static object GetField(object target, string name)
        {
            return target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        }
    }
}
