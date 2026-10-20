#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Zero.AnomalyPrototype.Flow;
using Zero.AnomalyPrototype.Player;

namespace Zero.Tests.PlayMode
{
    public sealed class WatchfulHallThreatGameplayTests
    {
        private WatchfulHallSession session;
        private WatchfulHallThreatDirector director;
        private WatchfulHallWatcher watcher;
        private WatchfulHallPatrol[] patrols;
        private FirstPersonController player;
        private Transform cabinet;
        private float previousTimeScale;

        [UnitySetUp]
        public IEnumerator LoadDemo()
        {
            previousTimeScale = Time.timeScale;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/Game/Scenes/Demos/Minigame4_WatchfulHall_Demo.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            session = Object.FindFirstObjectByType<WatchfulHallSession>();
            director = Object.FindFirstObjectByType<WatchfulHallThreatDirector>();
            Assert.IsNotNull(director, "Save the configured Watchful Hall scene before testing.");
            watcher = new SerializedObject(director).FindProperty("watchers").GetArrayElementAtIndex(0)
                .objectReferenceValue as WatchfulHallWatcher;
            WatchfulHallWatcher[] watchers = Object.FindObjectsByType<WatchfulHallWatcher>(FindObjectsSortMode.None);
            player = Object.FindFirstObjectByType<FirstPersonController>();
            Assert.IsTrue(director.IsReady, "Bake navigation and bind both patrols.");
            Assert.AreEqual(new SerializedObject(director).FindProperty("watchers").arraySize, watchers.Length);
            for (int i = 0; i < watchers.Length; i++) watchers[i].enabled = false;
            patrols = director.GetComponentsInChildren<WatchfulHallPatrol>();
            Assert.AreEqual(2, patrols.Length);
            Assert.AreEqual(39, director.Hiding.Count);
            cabinet = session.transform.parent.Find("WH_Cover/WH_WideHallCover/WH_HidingCabinet_Wide_Inner_-1_2");
            player.enabled = false;
            watcher.enabled = false;
            session.ResetAttempt();
            Time.timeScale = 3f;
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (director != null) director.Suspend();
            Time.timeScale = previousTimeScale;
            yield return null;
        }

        private void Hide()
        {
            player.Teleport(cabinet.TransformPoint(new Vector3(0f, 0.2f, 0f)), Quaternion.identity);
            Physics.SyncTransforms();
            Assert.AreEqual(cabinet, director.Hiding.FindCabinet(player.transform.position));
        }

        private void Warp(WatchfulHallPatrol patrol, Vector3 position)
        {
            Assert.IsTrue(NavMesh.SamplePosition(position, out NavMeshHit hit, 2f, NavMesh.AllAreas));
            Assert.IsTrue(patrol.GetComponent<NavMeshAgent>().Warp(hit.position));
            Physics.SyncTransforms();
        }

        [UnityTest]
        public IEnumerator PatrolMovesAndUsesLoopingHumanoidAnimation()
        {
            Hide();
            Vector3 before = patrols[0].transform.position;
            yield return new WaitForSeconds(1.5f);
            Assert.Greater(Vector3.Distance(before, patrols[0].transform.position), 1f);
            Animator animator = patrols[0].GetComponentInChildren<Animator>();
            Assert.IsFalse(animator.applyRootMotion);
            Assert.IsTrue(animator.avatar.isValid);
            Assert.IsTrue(animator.avatar.isHuman);
            AnimationClip clip = animator.GetCurrentAnimatorClipInfo(0)[0].clip;
            Assert.IsTrue(clip.isLooping);
            Assert.IsTrue(clip.isHumanMotion);
        }

        [UnityTest]
        public IEnumerator WatcherRoutesOnlyNearestPatrolAndKeepsResponderForUpdates()
        {
            Hide();
            Vector3 reported = new Vector3(-19f, 0.01f, -34f);
            int nearest = patrols[0].PathDistance(reported) < patrols[1].PathDistance(reported) ? 0 : 1;
            WatchfulHallPatrol assigned = director.ReportWatcher(reported, null);
            Assert.AreEqual(patrols[nearest], director.Responder);
            Assert.AreEqual(WatchfulHallPursuitRules.Phase.Investigating, patrols[nearest].State);
            Assert.AreEqual(WatchfulHallPursuitRules.Phase.Patrolling, patrols[1 - nearest].State);
            WatchfulHallPatrol other = director.ReportWatcher(patrols[1 - nearest].transform.position, null);
            Assert.AreEqual(patrols[1 - nearest], other);
            Vector3 updated = reported + Vector3.forward * 2f;
            director.ReportWatcher(updated, assigned);
            Assert.AreEqual(updated, patrols[nearest].LastKnownPosition);
            Assert.AreEqual(patrols[nearest], director.Responder);
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(updated, patrols[nearest].LastKnownPosition, "A hidden Player must not update last-known position.");
        }

        [UnityTest]
        public IEnumerator WatcherMustActuallySeePlayerForDetectionDelay()
        {
            player.Teleport(new Vector3(-5f, 0.03f, -14f), Quaternion.identity);
            Transform eye = WatcherEye(watcher);
            AimWatcherAtPlayer(watcher, eye);
            Physics.SyncTransforms();
            Assert.IsTrue(director.ClearLine(eye.position, player.transform.position + Vector3.up * 0.05f,
                player.transform), "Watcher light must reach the Player's feet.");
            watcher.enabled = true;
            yield return new WaitForSeconds(0.7f);
            Assert.IsFalse(watcher.IsAlerted);
            yield return new WaitForSeconds(1.2f);
            Assert.IsTrue(watcher.IsAlerted);
            Assert.IsNotNull(director.Responder);
            Hide();
            yield return null;
            WatchfulHallPatrol responder = director.Responder;
            Vector3 lastSeen = responder.LastKnownPosition;
            yield return new WaitForSeconds(0.5f);
            Assert.IsFalse(watcher.IsAlerted);
            Assert.AreEqual(lastSeen, responder.LastKnownPosition);
        }

        [UnityTest]
        public IEnumerator LeavingSpotlightClearsWatcherAndStopsUpdatingLastSeen()
        {
            player.Teleport(new Vector3(-5f, 0.03f, -14f), Quaternion.identity);
            Transform eye = WatcherEye(watcher);
            Light light = eye.GetComponent<Light>();
            AimWatcherAtPlayer(watcher, eye);
            Physics.SyncTransforms();
            watcher.enabled = true;
            yield return new WaitForSeconds(1.7f);
            Assert.IsTrue(watcher.IsAlerted);
            WatchfulHallPatrol responder = director.Responder;
            Vector3 lastSeen = responder.LastKnownPosition;
            // Same horizontal bearing, but the beam now points above the Player.
            eye.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(eye.forward, Vector3.up))
                * Quaternion.Euler(-60f, 0f, 0f);
            Assert.IsTrue(director.CanSee(eye.position, eye.forward, 65f, 100f));
            yield return new WaitForSeconds(0.5f);
            Assert.IsFalse(watcher.IsAlerted);
            Assert.AreEqual(lastSeen, responder.LastKnownPosition);
            light.enabled = false;
            eye.LookAt(player.transform.position);
            yield return new WaitForSeconds(1.7f);
            Assert.IsFalse(watcher.IsAlerted, "An unlit spotlight cannot reveal the Player.");
        }

        [UnityTest]
        public IEnumerator AdditionalWatcherDetectsPlayerAndAlertsNearestPatrol()
        {
            WatchfulHallWatcher additional = new SerializedObject(director)
                .FindProperty("watchers").GetArrayElementAtIndex(1).objectReferenceValue as WatchfulHallWatcher;
            Assert.IsNotNull(additional);
            player.Teleport(new Vector3(-5f, 0.03f, -14f), Quaternion.identity);
            Transform eye = WatcherEye(additional);
            AimWatcherAtPlayer(additional, eye);
            Physics.SyncTransforms();
            additional.enabled = true;
            yield return new WaitForSeconds(1.7f);
            Assert.IsTrue(additional.IsAlerted);
            Assert.IsNotNull(director.Responder);
        }

        private void AimWatcherAtPlayer(WatchfulHallWatcher targetWatcher, Transform eye)
        {
            var settings = new SerializedObject(targetWatcher);
            settings.FindProperty("scanDegreesPerSecond").floatValue = 0f;
            settings.FindProperty("patrolSpeed").floatValue = 0f;
            settings.ApplyModifiedPropertiesWithoutUndo();
            targetWatcher.GetComponent<Animator>().enabled = false;
            eye.LookAt(player.transform.position + Vector3.up * 0.05f);
        }

        private static Transform WatcherEye(WatchfulHallWatcher targetWatcher) =>
            new SerializedObject(targetWatcher).FindProperty("eye").objectReferenceValue as Transform;

        [UnityTest]
        public IEnumerator DirectSightChasesThenSearchesLastPositionAndReturns()
        {
            Hide();
            yield return new WaitForSeconds(0.6f);
            WatchfulHallPatrol patrol = patrols[0];
            Vector3 target = patrol.transform.position + patrol.transform.forward * 6f;
            player.Teleport(target, Quaternion.identity);
            Physics.SyncTransforms();
            float deadline = Time.realtimeSinceStartup + 2f;
            while (patrol.State != WatchfulHallPursuitRules.Phase.Chasing && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.AreEqual(WatchfulHallPursuitRules.Phase.Chasing, patrol.State);
            Vector3 lastSeen = patrol.LastKnownPosition;
            Hide();
            yield return new WaitForSeconds(0.95f);
            Assert.AreNotEqual(WatchfulHallPursuitRules.Phase.Chasing, patrol.State);
            Assert.AreEqual(lastSeen, patrol.LastKnownPosition);
            yield return new WaitForSeconds(12f);
            Assert.AreEqual(WatchfulHallPursuitRules.Phase.Patrolling, patrol.State);
        }

        [UnityTest]
        public IEnumerator DistantCabinetHidesButNearbyInspectionCatchesAndResets()
        {
            Hide();
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(0, director.CatchCount);
            Vector3 entrance = WatchfulHallHidingSpots.Entrance(cabinet);
            Vector3 outward = -cabinet.forward;
            Warp(patrols[0], entrance + outward * 2f);
            yield return new WaitForSeconds(0.8f);
            Assert.AreEqual(1, director.CatchCount, "A nearby patrol must inspect, not grant cabinet invulnerability.");
            Assert.IsFalse(player.InputEnabled);
            float deadline = Time.realtimeSinceStartup + 5f;
            while (!director.IsPlaying && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsTrue(director.IsPlaying, "Capture replay must finish and reset the attempt.");
            Assert.That(Vector3.Distance(player.transform.position, new Vector3(-5f, 0.02f, -44f)), Is.LessThan(0.1f));
            Assert.IsTrue(player.InputEnabled);
            Assert.IsFalse(session.IsUnlocked);
            Assert.IsTrue(director.IsPlaying);
            for (int i = 0; i < 4; i++) Assert.AreEqual(0f, session.GetProgress(i));
        }

        [UnityTest]
        public IEnumerator InvalidAIReferencesReportErrorButDoNotSoftLockChargePads()
        {
            var serialized = new SerializedObject(director);
            serialized.FindProperty("watchers").GetArrayElementAtIndex(0).objectReferenceValue = null;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            LogAssert.Expect(LogType.Error, "WATCHFUL_HALL_CONFIGURATION_INVALID: missing threat references");
            Assert.IsFalse(director.Bind(session, player));
            Transform pad = session.transform.Find("WH_ActivationPad_01");
            player.Teleport(pad.position + Vector3.up * 0.21f, Quaternion.identity);
            CharacterController body = player.GetComponent<CharacterController>();
            float until = Time.time + 3f;
            while (Time.time < until)
            {
                body.Move(Vector3.down * 0.03f);
                yield return null;
            }
            Assert.Greater(session.GetProgress(0), 0.5f);
        }

        [UnityTest]
        public IEnumerator CatchSmoothlyTurnsFromCurrentViewThenTracksHeadUntilReset()
        {
            Time.timeScale = 1f;
            Hide();
            WatchfulHallPatrol attacker = patrols[0];
            attacker.transform.rotation = Quaternion.Euler(0f, 210f, 0f);
            player.transform.rotation = Quaternion.Euler(0f, 70f, 0f);
            Transform view = player.PlayerCamera.transform;
            view.rotation = Quaternion.LookRotation(view.position - attacker.AttackLookPoint);
            Quaternion initialView = view.rotation;
            Quaternion initialBody = player.transform.rotation;
            Assert.IsTrue(director.TryCatch(attacker));
            Assert.IsFalse(director.TryCatch(patrols[1]));
            Assert.That(Quaternion.Angle(view.rotation, initialView), Is.LessThan(0.01f), "Catch must not snap the camera.");
            Assert.That(Quaternion.Angle(player.transform.rotation, initialBody), Is.LessThan(0.01f), "Body yaw must not snap the child camera.");
            yield return new WaitForSeconds(0.1f);
            yield return new WaitForEndOfFrame();
            float remaining = Vector3.Angle(view.forward, attacker.AttackLookPoint - view.position);
            Assert.That(Quaternion.Angle(view.rotation, initialView), Is.GreaterThan(1f));
            Assert.That(remaining, Is.InRange(5f, 175f), "Early capture must still be turning toward the face.");
            yield return new WaitForSeconds(0.3f);
            yield return new WaitForEndOfFrame();
            AssertFacing(attacker);
            Assert.IsFalse(player.InputEnabled);
            float deadline = Time.realtimeSinceStartup + 4f;
            while (!director.IsPlaying && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsTrue(director.IsPlaying);
            Assert.That(Vector3.Distance(player.transform.position, new Vector3(-5f, 0.02f, -44f)), Is.LessThan(0.1f));
            var entrance = (Transform)new SerializedObject(director).FindProperty("entrance").objectReferenceValue;
            Assert.That(Quaternion.Angle(player.transform.rotation, entrance.rotation), Is.LessThan(0.1f));
            Assert.That(Quaternion.Angle(player.PlayerCamera.transform.localRotation, Quaternion.identity), Is.LessThan(0.1f));
            Assert.IsTrue(player.InputEnabled);
        }

        private void AssertFacing(WatchfulHallPatrol attacker)
        {
            Vector3 towardPlayer = Vector3.ProjectOnPlane(player.transform.position - attacker.transform.position, Vector3.up).normalized;
            Assert.That(Vector3.Dot(attacker.transform.forward, towardPlayer), Is.GreaterThan(0.999f));
            Assert.That(Vector3.Dot(player.transform.forward, -towardPlayer), Is.GreaterThan(0.999f));
            Transform view = player.PlayerCamera.transform;
            Vector3 gaze = (attacker.AttackLookPoint - view.position).normalized;
            Assert.That(Vector3.Dot(view.forward, gaze), Is.GreaterThan(0.999f));
        }

        [UnityTest]
        public IEnumerator CancelledCatchDoesNotTeleportLaterOrLeaveInputLocked()
        {
            Hide();
            Vector3 position = player.transform.position;
            Quaternion facing = Quaternion.Euler(0f, 123f, 0f);
            Quaternion cameraFacing = Quaternion.Euler(35f, 0f, 7f);
            player.transform.rotation = facing;
            player.PlayerCamera.transform.localRotation = cameraFacing;
            Assert.IsTrue(director.TryCatch(patrols[0]));
            Assert.IsFalse(director.TryCatch(patrols[1]));
            yield return new WaitForSeconds(0.1f);
            yield return new WaitForEndOfFrame();
            session.enabled = false;
            Assert.IsTrue(player.InputEnabled);
            Assert.That(Quaternion.Angle(player.transform.rotation, facing), Is.LessThan(0.1f));
            Assert.That(Quaternion.Angle(player.PlayerCamera.transform.localRotation, cameraFacing), Is.LessThan(0.1f));
            yield return new WaitForSeconds(2f);
            Assert.AreEqual(position, player.transform.position);
        }
    }
}
#endif
