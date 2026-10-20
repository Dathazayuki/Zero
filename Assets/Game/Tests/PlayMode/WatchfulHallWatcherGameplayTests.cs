#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Zero.AnomalyPrototype.Flow;
using Zero.AnomalyPrototype.Player;

namespace Zero.Tests.PlayMode
{
    public sealed class WatchfulHallWatcherGameplayTests
    {
        private WatchfulHallSession session;
        private WatchfulHallThreatDirector director;
        private FirstPersonController player;
        private WatchfulHallWatcher[] watchers;
        private GameObject extra;
        private Transform cabinet;
        private float previousTimeScale;

        [UnitySetUp]
        public IEnumerator LoadDemo()
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 1f;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/Game/Scenes/Demos/Minigame4_WatchfulHall_Demo.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            session = Object.FindFirstObjectByType<WatchfulHallSession>();
            director = Object.FindFirstObjectByType<WatchfulHallThreatDirector>();
            player = Object.FindFirstObjectByType<FirstPersonController>();
            SerializedProperty refs = new SerializedObject(director).FindProperty("watchers");
            watchers = new WatchfulHallWatcher[refs.arraySize];
            for (int i = 0; i < watchers.Length; i++)
                watchers[i] = refs.GetArrayElementAtIndex(i).objectReferenceValue as WatchfulHallWatcher;
            Assert.IsTrue(director.IsReady, "Save the configured demo before running these tests.");
            for (int i = 0; i < watchers.Length; i++) watchers[i].enabled = false;
            player.enabled = false;
            cabinet = session.transform.parent.Find("WH_Cover/WH_WideHallCover/WH_HidingCabinet_Wide_Inner_-1_2");
            HidePlayer();
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (director != null) director.Suspend();
            if (extra != null) Object.Destroy(extra);
            Time.timeScale = previousTimeScale;
            yield return null;
        }

        [UnityTest]
        public IEnumerator WatcherArrayAcceptsOneAndThreeAndRejectsDuplicateReferences()
        {
            extra = Object.Instantiate(watchers[0].gameObject, watchers[0].transform.parent);
            WatchfulHallWatcher third = extra.GetComponent<WatchfulHallWatcher>();
            SetWatchers(new[] { watchers[0] });
            Assert.IsTrue(director.Bind(session, player));
            SetWatchers(new[] { watchers[0], watchers[1], third });
            Assert.IsTrue(director.Bind(session, player));
            SetWatchers(new[] { watchers[0], watchers[0], third });
            LogAssert.Expect(LogType.Error, "WATCHFUL_HALL_CONFIGURATION_INVALID: missing threat references");
            Assert.IsFalse(director.Bind(session, player));
            yield return null;
        }

        [UnityTest]
        public IEnumerator WatcherWalksBackAndForthOnThePlatformAndResetRestoresSpawn()
        {
            WatchfulHallWatcher watcher = watchers[0];
            Vector3 spawn = watcher.transform.position;
            var settings = new SerializedObject(watcher);
            Transform start = settings.FindProperty("patrolStart").objectReferenceValue as Transform;
            Transform end = settings.FindProperty("patrolEnd").objectReferenceValue as Transform;
            start.position = spawn - Vector3.right * 0.8f;
            end.position = spawn + Vector3.right * 0.8f;
            watcher.enabled = true;
            float lastX = spawn.x;
            bool wentRight = false;
            bool returned = false;
            float until = Time.time + 3f;
            while (Time.time < until)
            {
                yield return null;
                Vector3 point = watcher.transform.position;
                wentRight |= point.x > spawn.x + 0.2f;
                returned |= wentRight && point.x < lastX - 0.001f;
                Assert.That(point.x, Is.InRange(start.position.x - 0.01f, end.position.x + 0.01f));
                Assert.That(point.y, Is.EqualTo(spawn.y).Within(0.01f));
                Assert.That(point.z, Is.EqualTo(spawn.z).Within(0.01f));
                lastX = point.x;
            }
            Assert.IsTrue(wentRight && returned);
            watcher.ResetWatcher();
            watcher.ResetWatcher();
            Assert.That(Vector3.Distance(spawn, watcher.transform.position), Is.LessThan(0.001f));
            session.enabled = false;
            yield return new WaitForSeconds(0.2f);
            Assert.That(Vector3.Distance(spawn, watcher.transform.position), Is.LessThan(0.001f));
        }

        [UnityTest]
        public IEnumerator NeckScansAndLightFollowsWithoutSpinningBodyOrAccumulatingPose()
        {
            WatchfulHallWatcher watcher = watchers[0];
            var settings = new SerializedObject(watcher);
            settings.FindProperty("patrolSpeed").floatValue = 0f;
            settings.FindProperty("scanDegreesPerSecond").floatValue = 30f;
            settings.ApplyModifiedPropertiesWithoutUndo();
            Transform neck = settings.FindProperty("neckBone").objectReferenceValue as Transform;
            Transform eye = settings.FindProperty("eye").objectReferenceValue as Transform;
            watcher.GetComponent<Animator>().enabled = false;
            watcher.ResetWatcher();
            Quaternion body = watcher.transform.rotation;
            Quaternion pose = neck.localRotation;
            Vector3 beam = eye.forward;
            watcher.enabled = true;
            yield return new WaitForSeconds(0.4f);
            yield return new WaitForEndOfFrame();
            Assert.That(Quaternion.Angle(body, watcher.transform.rotation), Is.LessThan(0.01f));
            Assert.That(Quaternion.Angle(pose, neck.localRotation), Is.InRange(8f, 18f));
            Assert.That(Vector3.Angle(beam, eye.forward), Is.GreaterThan(4f));
            yield return new WaitForSeconds(0.4f);
            yield return new WaitForEndOfFrame();
            Assert.That(Quaternion.Angle(pose, neck.localRotation), Is.InRange(20f, 32f));
            watcher.enabled = false;
            Assert.That(Quaternion.Angle(pose, neck.localRotation), Is.LessThan(0.01f));
        }

        [UnityTest]
        public IEnumerator WarmupStaysAmberAndAcceptedRedAlarmMakesTheResponderApproach()
        {
            Light light = PrepareAlarm(watchers[0], new Vector3(-5f, 0.03f, -14f));
            yield return new WaitForSeconds(1f);
            Assert.IsFalse(watchers[0].IsAlerted);
            Assert.Greater(light.color.g, 0.5f, "A pending detection must remain distinct from a red alarm.");
            yield return new WaitForSeconds(0.8f);
            Assert.IsTrue(watchers[0].IsAlerted);
            Assert.Less(light.color.g, 0.05f);
            WatchfulHallPatrol responder = director.Responder;
            Assert.IsNotNull(responder);
            Assert.AreEqual(WatchfulHallPursuitRules.Phase.Investigating, responder.State);
            float before = responder.PathDistance(player.transform.position);
            Assert.IsFalse(float.IsInfinity(before));
            Assert.Greater(responder.GetComponent<NavMeshAgent>().speed, 2.2f);
            yield return new WaitForSeconds(0.8f);
            Assert.Less(responder.PathDistance(player.transform.position), before - 0.5f,
                "The responder must actually approach, not merely be selected by the Director.");
        }

        [UnityTest]
        public IEnumerator UnreachablePlayerDoesNotProduceAnAcknowledgedRedAlarm()
        {
            Light light = PrepareAlarm(watchers[0], new Vector3(0f, 4.5f, 0f));
            yield return new WaitForSeconds(1.8f);
            Assert.IsFalse(watchers[0].IsAlerted);
            Assert.IsNull(director.Responder);
            Assert.Greater(light.color.g, 0.5f);
        }

        [UnityTest]
        public IEnumerator AssignedResponderWithDisabledNavigationIsReplaced()
        {
            Vector3 point = new Vector3(-19f, 0.03f, -34f);
            WatchfulHallPatrol first = director.ReportWatcher(point, null);
            Assert.IsNotNull(first);
            first.GetComponent<NavMeshAgent>().enabled = false;
            WatchfulHallPatrol replacement = director.ReportWatcher(point, first);
            Assert.IsNotNull(replacement);
            Assert.AreNotEqual(first, replacement);
            Assert.AreEqual(WatchfulHallPursuitRules.Phase.Investigating, replacement.State);
            yield return null;
        }

        [UnityTest]
        public IEnumerator WatcherReportIsAcceptedDuringTheLostSightChaseGracePeriod()
        {
            WatchfulHallPatrol patrol = director.GetComponentsInChildren<WatchfulHallPatrol>()[0];
            player.Teleport(patrol.transform.position + patrol.transform.forward * 5f, Quaternion.identity);
            Physics.SyncTransforms();
            float deadline = Time.realtimeSinceStartup + 2f;
            while (patrol.State != WatchfulHallPursuitRules.Phase.Chasing && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.AreEqual(WatchfulHallPursuitRules.Phase.Chasing, patrol.State);
            HidePlayer();
            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual(WatchfulHallPursuitRules.Phase.Chasing, patrol.State);
            Assert.IsTrue(NavMesh.SamplePosition(patrol.transform.position + Vector3.right * 3f,
                out NavMeshHit hit, 2f, NavMesh.AllAreas));
            Assert.AreEqual(patrol, director.ReportWatcher(hit.position, patrol));
            Assert.That(Vector3.Distance(hit.position, patrol.LastKnownPosition), Is.LessThan(0.001f));
            Assert.AreEqual(WatchfulHallPursuitRules.Phase.Investigating, patrol.State);
        }

        private Light PrepareAlarm(WatchfulHallWatcher watcher, Vector3 position)
        {
            foreach (WatchfulHallPatrol patrol in director.GetComponentsInChildren<WatchfulHallPatrol>())
            {
                var settings = new SerializedObject(patrol);
                settings.FindProperty("viewDistance").floatValue = 1f;
                settings.ApplyModifiedPropertiesWithoutUndo();
            }
            var authoring = new SerializedObject(watcher);
            authoring.FindProperty("scanDegreesPerSecond").floatValue = 0f;
            authoring.FindProperty("patrolSpeed").floatValue = 0f;
            authoring.ApplyModifiedPropertiesWithoutUndo();
            watcher.GetComponent<Animator>().enabled = false;
            watcher.ResetWatcher();
            player.Teleport(position, Quaternion.identity);
            Physics.SyncTransforms();
            Transform eye = authoring.FindProperty("eye").objectReferenceValue as Transform;
            eye.LookAt(player.transform.position + Vector3.up * 0.05f);
            watcher.enabled = true;
            return eye.GetComponent<Light>();
        }

        private void HidePlayer()
        {
            player.Teleport(cabinet.TransformPoint(new Vector3(0f, 0.2f, 0f)), Quaternion.identity);
            Physics.SyncTransforms();
        }

        private void SetWatchers(WatchfulHallWatcher[] values)
        {
            var settings = new SerializedObject(director);
            SerializedProperty array = settings.FindProperty("watchers");
            array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            settings.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
