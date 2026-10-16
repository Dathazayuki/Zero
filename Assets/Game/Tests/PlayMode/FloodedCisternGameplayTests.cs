#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Zero.AnomalyPrototype.Flow;
using Zero.AnomalyPrototype.Player;

namespace Zero.Tests.PlayMode
{
    public sealed class FloodedCisternGameplayTests
    {
        private FloodedCisternMinigameSession session;
        private FirstPersonController player;
        private CisternMannequinController threat;
        private Camera view;

        [UnitySetUp]
        public IEnumerator LoadDemo()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/Game/Scenes/Demos/Minigame5_FloodedCistern_Demo.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            session = Object.FindFirstObjectByType<FloodedCisternMinigameSession>();
            player = Object.FindFirstObjectByType<FirstPersonController>();
            Assert.That(session, Is.Not.Null, "Demo must contain the session after manual save.");
            Assert.That(session.IsPlaying, Is.True, "Demo navigation/references must prepare successfully.");
            player.enabled = false;
            view = player.PlayerCamera;
            threat = GameObject.Find("Cistern_Threat_01").GetComponent<CisternMannequinController>();
            session.ResetAttempt();
        }

        [UnityTest]
        public IEnumerator GazeFreezesPose_AndLookingAwayResumesChase()
        {
            LookAtThreat();
            Assert.That(threat.TryActivate(player), Is.True);
            Assert.That(threat.TryActivate(player), Is.False);
            yield return new WaitForSeconds(0.5f);
            Assert.That(threat.State, Is.EqualTo(CisternMannequinController.MotionState.Frozen));
            Animator animator = threat.GetComponent<Animator>();
            float pose = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            Vector3 position = threat.transform.position;
            yield return new WaitForSeconds(0.2f);
            Assert.That(animator.speed, Is.Zero);
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).normalizedTime, Is.EqualTo(pose).Within(0.001f));
            Assert.That(Vector3.Distance(position, threat.transform.position), Is.LessThan(0.01f));
            view.transform.rotation = Quaternion.LookRotation(-view.transform.forward);
            yield return new WaitForSeconds(0.25f);
            Assert.That(threat.State, Is.EqualTo(CisternMannequinController.MotionState.Chasing));
            Assert.That(animator.speed, Is.GreaterThan(0f));
            Assert.That(Vector3.Distance(position, threat.transform.position), Is.GreaterThan(0.05f));
        }

        [UnityTest]
        public IEnumerator OcclusionAllowsChase_ResetClearsEyesAndPose()
        {
            LookAtThreat();
            Assert.That(threat.TryActivate(player), Is.True);
            yield return new WaitForSeconds(0.45f);
            AssertEyes(true);
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = Vector3.Lerp(view.transform.position, threat.transform.position + Vector3.up, 0.5f);
            wall.transform.localScale = Vector3.one * 2f;
            Physics.SyncTransforms();
            yield return null;
            Assert.That(threat.State, Is.EqualTo(CisternMannequinController.MotionState.Chasing));
            Object.Destroy(wall);
            session.ResetAttempt();
            session.ResetAttempt();
            Assert.That(threat.State, Is.EqualTo(CisternMannequinController.MotionState.Dormant));
            AssertEyes(false);
            Assert.That(threat.TryActivate(player), Is.True);
        }

        [UnityTest]
        public IEnumerator CatchIsConsumedOnce_AndReplayReturnsToEntrance()
        {
            LookAtThreat();
            Vector3 entrance = GameObject.Find("CisternArrivalAnchor").transform.position;
            Assert.That(threat.TryActivate(player), Is.True);
            view.transform.rotation = Quaternion.LookRotation(-view.transform.forward);
            yield return new WaitForSeconds(0.45f);
            player.Teleport(threat.transform.position + Vector3.back * 0.6f, Quaternion.Euler(0f, 180f, 0f));
            Physics.SyncTransforms();
            yield return null;
            yield return null;
            Assert.That(session.State, Is.EqualTo(FloodedCisternMinigameSession.AttemptState.Caught));
            Assert.That(session.TryCatch(threat), Is.False);
            yield return new WaitForSeconds(1.3f);
            Assert.That(session.IsPlaying, Is.True);
            Assert.That(Vector3.Distance(player.transform.position, entrance), Is.LessThan(0.2f));
            Assert.That(player.InputEnabled, Is.True);
            AssertEyes(false);
        }

        [UnityTest]
        public IEnumerator ResetDuringCatchRestoresInput_CompletionIsOneShot()
        {
            LookAtThreat();
            threat.TryActivate(player);
            view.transform.rotation = Quaternion.LookRotation(-view.transform.forward);
            yield return new WaitForSeconds(0.45f);
            Assert.That(session.TryCatch(threat), Is.True);
            session.ResetAttempt();
            Assert.That(player.InputEnabled, Is.True);
            int completions = 0;
            System.Action callback = () => completions++;
            session.Completed += callback;
            Assert.That(session.TryComplete(player), Is.True);
            Assert.That(session.TryComplete(player), Is.False);
            session.Completed -= callback;
            session.ResetAttempt();
            yield return new WaitForSeconds(0.8f);
            Assert.That(completions, Is.EqualTo(1));
            Assert.That(session.State, Is.EqualTo(FloodedCisternMinigameSession.AttemptState.Completed));
            Assert.That(threat.State, Is.EqualTo(CisternMannequinController.MotionState.Stopped));
        }

        [UnityTest]
        public IEnumerator MissingNavigationFailsSafe()
        {
            GameObject invalid = new GameObject("Invalid Cistern Session");
            invalid.SetActive(false);
            var broken = invalid.AddComponent<FloodedCisternMinigameSession>();
            LogAssert.Expect(LogType.Error, "CISTERN_CONFIGURATION_INVALID: navigation, entrance, local fade or mannequins missing; bake NavMesh before Play");
            yield return broken.Prepare();
            Assert.That(broken.IsReady, Is.False);
            Assert.That(broken.TryComplete(player), Is.False);
            Object.Destroy(invalid);
        }

        private void LookAtThreat()
        {
            player.Teleport(threat.transform.position + Vector3.back * 6f, Quaternion.identity);
            view.transform.LookAt(threat.transform.position + Vector3.up * 1.2f);
            Physics.SyncTransforms();
        }

        private void AssertEyes(bool active)
        {
            Transform[] children = threat.GetComponentsInChildren<Transform>(true);
            int count = 0;
            foreach (Transform child in children)
            {
                if (child.name != "Eye1" && child.name != "Eye2") continue;
                Assert.That(child.gameObject.activeSelf, Is.EqualTo(active));
                count++;
            }
            Assert.That(count, Is.EqualTo(2));
        }
    }
}
#endif
