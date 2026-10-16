#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using TMPro;
using Zero.AnomalyPrototype.Flow;
using Zero.AnomalyPrototype.Player;

namespace Zero.Tests.PlayMode
{
    public sealed class WatchfulHallGameplayTests
    {
        private WatchfulHallSession session;
        private FirstPersonController player;
        private CharacterController body;
        private WatchfulHallChargePad[] pads;
        private float originalTimeScale;

        [UnitySetUp]
        public IEnumerator LoadDemo()
        {
            originalTimeScale = Time.timeScale;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/Game/Scenes/Demos/Minigame4_WatchfulHall_Demo.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            session = Object.FindFirstObjectByType<WatchfulHallSession>();
            player = Object.FindFirstObjectByType<FirstPersonController>();
            Assert.That(session, Is.Not.Null, "Save the configured Watchful Hall scene first.");
            Assert.IsTrue(session.IsReady);
            WatchfulHallThreatDirector threats = Object.FindFirstObjectByType<WatchfulHallThreatDirector>();
            if (threats != null) threats.enabled = false;
            body = player.GetComponent<CharacterController>();
            pads = session.GetComponentsInChildren<WatchfulHallChargePad>();
            Assert.AreEqual(4, pads.Length);
            player.enabled = false;
            Time.timeScale = 10f;
            session.ResetAttempt();
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale = originalTimeScale;
            yield return null;
        }

        private void StandAt(int pad)
        {
            player.Teleport(pads[pad].transform.position + Vector3.up * 0.21f, Quaternion.identity);
            body.Move(Vector3.down * 0.08f);
        }

        private IEnumerator StandFor(float seconds)
        {
            float until = Time.time + seconds;
            float deadline = Time.realtimeSinceStartup + 15f;
            while (Time.time < until && Time.realtimeSinceStartup < deadline)
            {
                body.Move(Vector3.down * 0.03f);
                yield return null;
            }
            Assert.Less(Time.realtimeSinceStartup, deadline, "Game time did not advance.");
        }

        [UnityTest]
        public IEnumerator LeavingAndMovementRetainChargeButRequireSettlingAgain()
        {
            StandAt(0);
            yield return StandFor(1f);
            Assert.AreEqual(0f, session.GetProgress(0));
            yield return StandFor(4f);
            float accumulated = session.GetProgress(0);
            Assert.Greater(accumulated, 2f);
            player.Teleport(pads[0].transform.position + Vector3.forward * 3f, Quaternion.identity);
            yield return StandFor(2f);
            Assert.AreEqual(accumulated, session.GetProgress(0));
            StandAt(0);
            yield return StandFor(1f);
            Assert.AreEqual(accumulated, session.GetProgress(0));
            yield return StandFor(2f);
            Assert.Greater(session.GetProgress(0), accumulated);
            player.transform.position += Vector3.right * 0.1f;
            yield return null;
            float paused = session.GetProgress(0);
            yield return StandFor(1f);
            Assert.AreEqual(paused, session.GetProgress(0));
            session.enabled = false;
            yield return StandFor(2f);
            Assert.AreEqual(paused, session.GetProgress(0));
            session.enabled = true;
            yield return StandFor(1f);
            Assert.AreEqual(paused, session.GetProgress(0));
            session.ResetAttempt();
            session.ResetAttempt();
            Assert.AreEqual(0f, session.GetProgress(0));
        }

        [UnityTest]
        public IEnumerator ChargingPulseAndLabelFollowRealChargeAndCleanUp()
        {
            Transform signalObject = pads[0].transform.Find("WH_CS_Pad/WH_CS_StatusLEDX_1");
            Renderer signal = signalObject.GetComponent<Renderer>();
            TMP_Text label = pads[0].transform.Find("WH_CS_Console/WH_CS_DisplayCanvas/WH_CS_ChargeLabel").GetComponent<TMP_Text>();
            var block = new MaterialPropertyBlock();
            signal.GetPropertyBlock(block);
            Color originalEmission = block.GetColor("_EmissionColor");
            StandAt(0);
            yield return StandFor(1f);
            Assert.AreNotEqual("CHARGING", label.text);
            yield return StandFor(2f);
            Assert.AreEqual("CHARGING", label.text);
            signal.GetPropertyBlock(block);
            Assert.Greater(block.GetColor("_EmissionColor").r, 0.4f);
            Assert.That(block.GetColor("_EmissionColor").r, Is.GreaterThan(block.GetColor("_EmissionColor").g));
            player.Teleport(pads[0].transform.position + Vector3.forward * 3f, Quaternion.identity);
            yield return StandFor(0.5f);
            Assert.AreEqual("PAUSED", label.text);
            signal.GetPropertyBlock(block);
            Assert.AreEqual(originalEmission, block.GetColor("_EmissionColor"));
            StandAt(0);
            yield return StandFor(2f);
            Assert.AreEqual("CHARGING", label.text);
            session.enabled = false;
            signal.GetPropertyBlock(block);
            Assert.AreEqual(originalEmission, block.GetColor("_EmissionColor"));
            Assert.AreNotEqual("CHARGING", label.text);
            session.enabled = true;
            session.ResetAttempt();
            Assert.AreEqual(0f, session.GetProgress(0));
        }

        [UnityTest]
        public IEnumerator FourPadsChangeSignalsOpenGateAndCompleteOnce()
        {
            int completions = 0;
            session.Completed += () => completions++;
            WatchfulHallExitGate gate = session.GetComponentInChildren<WatchfulHallExitGate>();
            Transform left = gate.transform.Find("WH_EG_HingeLeft");
            for (int i = 0; i < pads.Length; i++)
            {
                StandAt(i);
                yield return StandFor(17.5f);
                Assert.AreEqual(15f, session.GetProgress(i));
                Assert.AreEqual(15f, pads[i].GetComponentInChildren<Slider>().value);
                Renderer signal = pads[i].transform.Find("WH_CS_Pad/WH_CS_PressureSurface").GetComponent<Renderer>();
                Assert.That(signal.sharedMaterial.name, Does.Contain("Green"));
                Assert.AreEqual("ACTIVATED", pads[i].transform.Find("WH_CS_Console/WH_CS_DisplayCanvas/WH_CS_ChargeLabel").GetComponent<TMP_Text>().text);
                if (i < 3) Assert.IsFalse(session.IsUnlocked);
            }
            Assert.IsTrue(session.IsUnlocked);
            yield return StandFor(1.5f);
            Assert.That(Quaternion.Angle(left.localRotation, Quaternion.identity), Is.EqualTo(90f).Within(0.5f));
            player.Teleport(gate.transform.TransformPoint(new Vector3(0f, 0.03f, 2f)), Quaternion.identity);
            yield return null;
            yield return null;
            Assert.AreEqual(1, completions);
            session.ResetAttempt();
            Assert.IsFalse(session.IsUnlocked);
            Assert.Less(Quaternion.Angle(left.localRotation, Quaternion.identity), 0.01f);
        }
    }
}
#endif
