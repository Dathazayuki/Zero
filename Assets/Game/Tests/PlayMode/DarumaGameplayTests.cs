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
    public sealed class DarumaGameplayTests
    {
        private DarumaSession session;
        private FirstPersonController player;
        private Vector3 initialMonsterPosition;
        private float initialRemaining;
        private float authoredCatchDistance;

        [UnitySetUp]
        public IEnumerator LoadDemo()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/Game/Scenes/Demos/Minigame2_DarumaHall_Demo.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            session = Object.FindFirstObjectByType<DarumaSession>();
            player = Object.FindFirstObjectByType<FirstPersonController>();
            Assert.That(session, Is.Not.Null, "Save the configured demo scene first.");
            Assert.That(session.IsReady, Is.True);
            player.enabled = false;
            initialMonsterPosition = session.transform.Find("DH_Actors/DH_MonsterPivot").position;
            session.ResetAttempt();
            initialRemaining = session.Remaining;
            authoredCatchDistance = new UnityEditor.SerializedObject(session).FindProperty("catchDistance").floatValue;
        }

        private IEnumerator WaitForWatch()
        {
            player.Teleport(new Vector3(0f, 0.03f, 5f), Quaternion.identity);
            float deadline = Time.realtimeSinceStartup + 6f;
            while (session.State != DarumaRules.Phase.Watching && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.AreEqual(DarumaRules.Phase.Watching, session.State);
            Assert.AreEqual(Color.red, session.transform.Find("DH_Lighting/DH_WallLamp_L0/DH_WarmBulb").GetComponent<Light>().color);
        }

        [UnityTest]
        public IEnumerator LookingAroundDoesNotCountAsMovement()
        {
            yield return WaitForWatch();
            player.PlayerCamera.transform.localRotation = Quaternion.Euler(15f, 65f, 0f);
            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual(DarumaRules.Phase.Watching, session.State);
            Assert.That(session.Remaining, Is.LessThan(initialRemaining));
        }

        [UnityTest]
        public IEnumerator MovementTeleportsMonsterThenResetsEntireAttempt()
        {
            yield return WaitForWatch();
            player.transform.position += Vector3.forward * 0.2f;
            yield return null;
            yield return null;
            Assert.AreEqual(DarumaRules.Phase.Caught, session.State);
            Transform monster = session.transform.Find("DH_Actors/DH_MonsterPivot");
            Assert.That(Vector3.Distance(monster.position, player.transform.position), Is.EqualTo(authoredCatchDistance).Within(0.05f));
            Assert.That(Vector3.Dot(monster.rotation * Vector3.back, player.transform.forward), Is.LessThan(-0.99f));
            Assert.IsFalse(player.InputEnabled);
            yield return new WaitForSeconds(1.6f);
            Assert.AreEqual(DarumaRules.Phase.Waiting, session.State);
            Assert.AreEqual(initialRemaining, session.Remaining);
            Assert.That(player.transform.position.z, Is.EqualTo(-1.1f).Within(0.05f));
            Assert.IsTrue(player.InputEnabled);
            Assert.AreNotEqual(Color.red, session.transform.Find("DH_Lighting/DH_WallLamp_L0/DH_WarmBulb").GetComponent<Light>().color);
            AssertTimersMatch();
        }

        [UnityTest]
        public IEnumerator CompletionOnlyFiresOnce()
        {
            int completions = 0;
            session.Completed += () => completions++;
            player.Teleport(new Vector3(0f, 0.03f, 5f), Quaternion.identity);
            yield return null;
            Vector3 finish = session.transform.Find("DH_Markers/DH_FinishMarker").position;
            player.Teleport(finish + Vector3.forward * 0.1f, Quaternion.identity);
            yield return null;
            yield return null;
            Assert.AreEqual(DarumaRules.Phase.Completed, session.State);
            Assert.AreEqual(1, completions);
            AssertTimersMatch();
        }

        private void AssertTimersMatch()
        {
            DarumaTimerDisplay[] displays = session.GetComponentsInChildren<DarumaTimerDisplay>();
            Assert.AreEqual(9, displays.Length, "One end display and eight wall displays must be bound.");
            foreach (DarumaTimerDisplay display in displays)
            {
                Assert.AreEqual(Mathf.CeilToInt(session.Remaining), display.DisplayedSeconds, display.name);
            }
        }

        [UnityTest]
        public IEnumerator RedWarningPrecedesDetectionWhileMonsterTurns()
        {
            player.Teleport(new Vector3(0f, 0.03f, 5f), Quaternion.identity);
            float deadline = Time.realtimeSinceStartup + 6f;
            while (session.State != DarumaRules.Phase.TurningToWatch && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.AreEqual(DarumaRules.Phase.TurningToWatch, session.State);
            Light bulb = session.transform.Find("DH_Lighting/DH_WallLamp_L0/DH_WarmBulb").GetComponent<Light>();
            Assert.AreEqual(Color.red, bulb.color);
            player.transform.position += Vector3.forward * 0.2f;
            yield return null;
            Assert.AreEqual(DarumaRules.Phase.TurningToWatch, session.State);
            yield return new WaitForSeconds(0.1f);
            Assert.IsTrue(player.InputEnabled);
        }

        [UnityTest]
        public IEnumerator DisableDuringCatchRestoresInputAndMonster()
        {
            yield return WaitForWatch();
            player.transform.position += Vector3.forward * 0.2f;
            yield return null;
            yield return null;
            session.enabled = false;
            Assert.IsTrue(player.InputEnabled);
            Assert.AreEqual(DarumaRules.Phase.Waiting, session.State);
            Transform monster = session.transform.Find("DH_Actors/DH_MonsterPivot");
            Assert.That(Vector3.Distance(monster.position, initialMonsterPosition), Is.LessThan(0.01f));
            yield return new WaitForSeconds(1.6f);
            Assert.That(player.transform.position.z, Is.GreaterThan(4f), "Old replay must not teleport after disable.");
        }
    }
}
#endif
