#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Zero.AnomalyPrototype.Anomalies;
using Zero.AnomalyPrototype.Demo;
using Zero.AnomalyPrototype.Player;
using Phase = Zero.AnomalyPrototype.Anomalies.CabaretBlackHoleRules.Phase;

namespace Zero.Tests.PlayMode
{
    public sealed class CabaretBlackHoleSceneTests
    {
        private LivingRoomAnomalyPreview preview;
        private CabaretBlackHoleAnomaly anomaly;
        private FirstPersonController player;
        private PlayerDeathController death;
        private Transform observer;
        private Renderer floor;
        private Material normalFloor;
        private Vector3 initialCamera, spawn;

        [UnitySetUp]
        public IEnumerator Load()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/Game/Scenes/Demos/InfinityRoom5_CrimsonCabaret_Demo.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null;
            preview = Object.FindFirstObjectByType<LivingRoomAnomalyPreview>();
            player = Object.FindFirstObjectByType<FirstPersonController>();
            anomaly = Object.FindFirstObjectByType<CabaretBlackHoleAnomaly>();
            death = player.GetComponent<PlayerDeathController>(); observer = player.PlayerCamera.transform;
            floor = GameObject.Find("IR5_Floor").GetComponent<Renderer>(); normalFloor = floor.sharedMaterial;
            initialCamera = observer.localPosition; spawn = player.transform.position;
            player.enabled = false;
            Assert.That(preview.IsReady && anomaly.IsConfigured, Is.True);
        }

        private void Select()
        {
            preview.ResetPreview(); player.Teleport(new Vector3(0,.04f,0), Quaternion.identity);
            for (int i = 0; i < preview.Anomalies.Count; i++)
            {
                Assert.That(preview.CycleNext(), Is.True);
                if (preview.Current == anomaly) return;
            }
            Assert.Fail("IR5-16 missing from F3 roster.");
        }

        private IEnumerator WaitForPhase(Phase state, float timeout)
        {
            float end = Time.realtimeSinceStartup + timeout;
            while (anomaly.State != state && Time.realtimeSinceStartup < end) yield return null;
            Assert.That(anomaly.State, Is.EqualTo(state));
        }

        [UnityTest]
        public IEnumerator DodgeSurvivesResetRestoresAndStayingFallsThenRespawns()
        {
            Select();
            var actor = anomaly.GetComponentInChildren<Animator>(true);
            Vector3 authoredStart = actor.transform.position;
            yield return WaitForPhase(Phase.Warning, 8f);
            AssertRootTrajectory(actor, authoredStart);
            var view = anomaly.GetComponentInChildren<CabaretBlackHoleView>(true);
            Vector3 center = view.Center;
            Assert.That(new Vector2(center.x, center.z).magnitude, Is.LessThan(.05f));
            player.Teleport(new Vector3(1.4f,.04f,0), Quaternion.identity);
            yield return WaitForPhase(Phase.Open, 2f);
            Assert.That(death.IsDeathInProgress, Is.False);
            Assert.That(Vector3.Distance(center, view.Center), Is.LessThan(.001f));
            Assert.That(floor.sharedMaterial.shader.name, Is.EqualTo("Zero/Cabaret Portal Floor"));
            preview.ResetPreview(); preview.ResetPreview();
            Assert.That(Vector3.Distance(actor.transform.position, authoredStart), Is.LessThan(.001f));
            Assert.That(floor.sharedMaterial, Is.SameAs(normalFloor));
            Assert.That(observer.localPosition, Is.EqualTo(initialCamera));
            Select(); yield return WaitForPhase(Phase.Warning, 8f);
            yield return WaitForPhase(Phase.Falling, 2f);
            float cameraY = observer.position.y;
            yield return new WaitForSeconds(.35f);
            Assert.That(observer.position.y, Is.LessThan(cameraY - .3f));
            float end = Time.realtimeSinceStartup + 3f;
            while (!death.IsDeathInProgress && Time.realtimeSinceStartup < end) yield return null;
            Assert.That(death.IsDeathInProgress, Is.True);
            end = Time.realtimeSinceStartup + 6f;
            while (death.IsDeathInProgress && Time.realtimeSinceStartup < end) yield return null;
            Assert.That(death.IsDeathInProgress, Is.False);
            Assert.That(Vector3.Distance(player.transform.position, spawn), Is.LessThan(.1f));
            Assert.That(floor.sharedMaterial, Is.SameAs(normalFloor));
            Assert.That(Vector3.Distance(observer.localPosition, initialCamera), Is.LessThan(.02f));
            Assert.That(player.InputEnabled, Is.True);
            Assert.That(anomaly.IsActive, Is.False);
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (preview != null) preview.ResetPreview();
            yield return null;
        }

        private static void AssertRootTrajectory(Animator actor, Vector3 start)
        {
            Assert.That(actor.applyRootMotion, Is.True, "Animation must own the Host trajectory.");
            Assert.That(Vector3.ProjectOnPlane(actor.transform.position - start, Vector3.up).magnitude, Is.GreaterThan(.3f));
            Assert.That(actor.transform.position.y, Is.EqualTo(start.y).Within(.02f), "Body rise must not become a second root-height offset.");
        }
    }
}
#endif
