#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Zero.AnomalyPrototype.Anomalies;
using Zero.AnomalyPrototype.Player;
using Zero.AnomalyPrototype.Flow;

namespace Zero.Tests.PlayMode
{
    public sealed class CabaretBlinkChaseAnomalySceneTests
    {
        [UnityTest]
        public IEnumerator SixActorsMoveInDarkFreezeInLightAndStopAtExit()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/Game/Scenes/Demos/InfinityRoom5_CrimsonCabaret_Demo.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            var anomaly = Object.FindFirstObjectByType<CabaretBlinkChaseAnomaly>();
            var player = Object.FindFirstObjectByType<FirstPersonController>();
            player.enabled = false;
            var serialized = new SerializedObject(anomaly);
            var array = serialized.FindProperty("mannequins");
            Assert.That(array.arraySize, Is.EqualTo(6));
            var actor = (CabaretBlinkMannequin)array.GetArrayElementAtIndex(0).objectReferenceValue;
            var standing = Read<GameObject>(actor, "standingMannequin");
            var navigation = Read<NavMeshAgent>(actor, "navigation");
            Assert.That(actor.IsConfigured, Is.True);
            Assert.That(standing.activeSelf, Is.True);
            Vector3 home = actor.transform.position;
            var stage = Read<BoxCollider>(anomaly, "stageZone");
            var exit = Read<BoxCollider>(anomaly, "escapeZone");
            var vision = Read<SceneFadeController>(anomaly, "vision");
            Assert.That(anomaly.IsConfigured, Is.True);
            anomaly.Activate();
            Move(player, stage.bounds.center);
            yield return null; yield return null;
            Assert.That(vision.Alpha, Is.EqualTo(1f));
            Assert.That(actor.gameObject.activeSelf, Is.True);
            Assert.That(standing.activeSelf, Is.False);
            Assert.That(navigation.isOnNavMesh, Is.True);
            Assert.That(Vector3.Distance(home, actor.transform.position), Is.GreaterThan(.1f));
            yield return new WaitForSeconds(.25f);
            Assert.That(anomaly.State, Is.EqualTo(CabaretBlinkRules.Phase.Light));
            Vector3 frozen = actor.transform.position;
            var animator = actor.GetComponent<Animator>();
            Assert.That(animator.speed, Is.Zero);
            yield return new WaitForSeconds(.15f);
            Assert.That(actor.transform.position, Is.EqualTo(frozen));
            Move(player, exit.bounds.center);
            yield return null;
            Assert.That(anomaly.State, Is.EqualTo(CabaretBlinkRules.Phase.Escaped));
            Assert.That(vision.Alpha, Is.Zero);
            anomaly.ResetAnomaly(); anomaly.ResetAnomaly();
            Assert.That(actor.gameObject.activeSelf, Is.False);
            Assert.That(standing.activeSelf, Is.True);
            Assert.That(navigation.enabled, Is.False);
            Assert.That(actor.transform.position, Is.EqualTo(home));
            anomaly.Activate(); Move(player, stage.bounds.center);
            yield return null; yield return null;
            Assert.That(NavMesh.SamplePosition(player.transform.position, out NavMeshHit near, 1f, NavMesh.AllAreas), Is.True);
            navigation.Warp(near.position);
            actor.transform.position = near.position;
            yield return new WaitForSeconds(1.05f);
            Assert.That(player.GetComponent<PlayerDeathController>().IsDeathInProgress, Is.True);
            Assert.That(animator.speed, Is.Zero);
        }

        private static T Read<T>(Object target, string field) where T : Object
            => (T)new SerializedObject(target).FindProperty(field).objectReferenceValue;

        private static void Move(FirstPersonController player, Vector3 point)
        {
            var collider = player.GetComponent<CharacterController>();
            collider.enabled = false;
            player.transform.position = new Vector3(point.x, .04f, point.z);
            collider.enabled = true; Physics.SyncTransforms();
        }
    }
}
#endif
