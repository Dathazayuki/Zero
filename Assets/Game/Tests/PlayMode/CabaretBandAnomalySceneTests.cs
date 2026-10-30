#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Zero.AnomalyPrototype.Anomalies;
using Zero.AnomalyPrototype.Player;
using Zero.AnomalyPrototype.Flow;

namespace Zero.Tests.PlayMode
{
    public sealed class CabaretBandAnomalySceneTests
    {
        [UnityTest]
        public IEnumerator CentreStartsBandEscapeRestoresVisionAndTimeoutKills()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/Game/Scenes/Demos/InfinityRoom5_CrimsonCabaret_Demo.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            var anomaly = Object.FindFirstObjectByType<CabaretBandAnomaly>();
            var player = Object.FindFirstObjectByType<FirstPersonController>();
            var death = player.GetComponent<PlayerDeathController>();
            var performance = Read<CabaretBandPerformance>(anomaly, "performance");
            var bow = Read<Transform>(performance, "bow");
            Vector3 restPosition = bow.position;
            Quaternion restRotation = bow.rotation;
            var vision = Read<SceneFadeController>(anomaly, "vision");
            var centre = Read<BoxCollider>(anomaly, "centreZone");
            var exit = Read<BoxCollider>(anomaly, "escapeZone");
            Assert.That(anomaly.IsConfigured, Is.True);
            Assert.That(anomaly.Data.CanKillPlayer, Is.True);
            player.enabled = false;
            anomaly.Activate();
            yield return null;
            Assert.That(performance.IsPlaying, Is.False);
            Move(player, centre.bounds.center);
            yield return new WaitForSeconds(.4f);
            Assert.That(performance.IsPlaying, Is.True);
            Assert.That(vision.Alpha, Is.GreaterThan(0f));
            Assert.That(player.BreathingAudioSource.isPlaying, Is.True);
            Assert.That(Mathf.Abs(Vector3.Dot(bow.up, Vector3.up)), Is.LessThan(.01f));
            Vector3 playingPosition = bow.position;
            yield return new WaitForSeconds(.2f);
            Assert.That(bow.position.y, Is.EqualTo(playingPosition.y).Within(.001f));
            Assert.That(Mathf.Abs(bow.position.x - playingPosition.x), Is.GreaterThan(.005f));
            Move(player, exit.bounds.center);
            yield return null;
            Assert.That(anomaly.State, Is.EqualTo(CabaretBandRules.Phase.Escaped));
            Assert.That(performance.IsPlaying, Is.False);
            Assert.That(Vector3.Distance(bow.position, restPosition), Is.LessThan(.001f));
            Assert.That(Quaternion.Angle(bow.rotation, restRotation), Is.LessThan(.01f));
            Assert.That(vision.Alpha, Is.Zero);
            Assert.That(death.IsDeathInProgress, Is.False);
            anomaly.ResetAnomaly(); anomaly.ResetAnomaly();
            var serialized = new SerializedObject(anomaly);
            serialized.FindProperty("escapeSeconds").floatValue = 1f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            anomaly.Activate(); Move(player, centre.bounds.center);
            yield return new WaitForSeconds(1.2f);
            Assert.That(death.IsDeathInProgress, Is.True);
            Assert.That(performance.IsPlaying, Is.False);
            Assert.That(vision.Alpha, Is.Zero);
            Assert.That(player.BreathingAudioSource.isPlaying, Is.False);
        }

        private static T Read<T>(Object target, string field) where T : Object
            => (T)new SerializedObject(target).FindProperty(field).objectReferenceValue;

        private static void Move(FirstPersonController player, Vector3 point)
        {
            var collider = player.GetComponent<CharacterController>();
            collider.enabled = false;
            player.transform.position = new Vector3(point.x, .04f, point.z);
            collider.enabled = true;
            Physics.SyncTransforms();
        }
    }
}
#endif

