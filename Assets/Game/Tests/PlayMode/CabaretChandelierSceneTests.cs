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
using Phase = Zero.AnomalyPrototype.Anomalies.ArchiveCabinetCrushRules.Phase;

namespace Zero.Tests.PlayMode
{
    public sealed class CabaretChandelierSceneTests
    {
        [UnityTest]
        public IEnumerator DodgeNeverTakesCameraAndOnlyConfirmedHitStartsCutscene()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/Game/Scenes/Demos/InfinityRoom5_CrimsonCabaret_Demo.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null;
            var trap = Object.FindFirstObjectByType<CabaretChandelierAnomaly>();
            var preview = Object.FindFirstObjectByType<LivingRoomAnomalyPreview>();
            var player = Object.FindFirstObjectByType<FirstPersonController>(); player.enabled = false;
            var death = player.GetComponent<PlayerDeathController>();
            Assert.That(trap.IsConfigured, Is.True); Assert.That(trap.Data.CanKillPlayer, Is.True);
            var lamp = Read<Transform>(trap, "chandelier"); var hand = Read<GameObject>(trap, "hand");
            var area = Read<BoxCollider>(trap, "approachVolume"); Vector3 normal = lamp.position;
            Move(player, area.bounds.center + Vector3.right * 3f);
            for (int i = 0; i < preview.Anomalies.Count && preview.Current != trap; i++) preview.CycleNext();
            Assert.That(hand.activeSelf, Is.True);
            Assert.That(hand.GetComponent<Animator>().enabled, Is.True, "IR5-13 must leave the default Hand animation enabled");
            yield return DodgeWithoutCameraOverride(trap, player, area);
            Assert.That(trap.State, Is.EqualTo(Phase.Spent)); Assert.That(death.IsDeathInProgress, Is.False);
            Assert.That(hand.activeSelf, Is.False); Assert.That(lamp.position.y, Is.LessThan(normal.y));
            preview.ResetPreview(); preview.ResetPreview(); Assert.That(lamp.position, Is.EqualTo(normal));
            yield return CancelCapturedCutscene(trap, player, area);
            trap.Activate(); Move(player, new Vector3(area.bounds.center.x, 0f, area.bounds.center.z));
            float deadline = Time.realtimeSinceStartup + 4f;
            while (trap.State != Phase.Caught && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(trap.State, Is.EqualTo(Phase.Caught));
            Assert.That(death.IsDeathInProgress, Is.False, "Only the confirmed hit begins the short camera cutscene");
            Assert.That(player.InputEnabled, Is.False);
            while (!death.IsDeathInProgress && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(death.IsDeathInProgress, Is.True);
            Assert.That(death.IsScreenBlack, Is.True, "Final impact cuts black without an additional freeze delay");
            var interaction = player.GetComponent<PlayerInteraction>();
            if (interaction != null) Assert.That(interaction.enabled, Is.False);
            Assert.That(hand.activeSelf, Is.False);
            yield return new WaitForSecondsRealtime(4f);
            Assert.That(Vector3.Dot(player.PlayerCamera.transform.up, Vector3.up), Is.GreaterThan(.9f));
            Assert.That(player.InputEnabled, Is.True);
        }

        private static IEnumerator CancelCapturedCutscene(CabaretChandelierAnomaly trap,
            FirstPersonController player, BoxCollider area)
        {
            Quaternion normal = player.PlayerCamera.transform.localRotation;
            trap.Activate(); Move(player, new Vector3(area.bounds.center.x, 0f, area.bounds.center.z));
            float deadline = Time.realtimeSinceStartup + 4f;
            while (trap.State != Phase.Caught && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(trap.State, Is.EqualTo(Phase.Caught));
            trap.ResetAnomaly(); trap.ResetAnomaly();
            Assert.That(player.InputEnabled, Is.True);
            Assert.That(Quaternion.Angle(normal, player.PlayerCamera.transform.localRotation), Is.LessThan(.01f));
        }

        private static IEnumerator DodgeWithoutCameraOverride(CabaretChandelierAnomaly trap,
            FirstPersonController player, BoxCollider area)
        {
            Quaternion camera = player.PlayerCamera.transform.rotation;
            Move(player, new Vector3(area.bounds.center.x, 0f, area.bounds.center.z));
            yield return new WaitForSeconds(.15f);
            Assert.That(trap.State, Is.EqualTo(Phase.Warning));
            Assert.That(Quaternion.Angle(camera, player.PlayerCamera.transform.rotation), Is.LessThan(.01f));
            Assert.That(player.InputEnabled, Is.True);
            Move(player, area.bounds.center + Vector3.right * 3f);
            yield return new WaitForSeconds(2.4f);
            Assert.That(Quaternion.Angle(camera, player.PlayerCamera.transform.rotation), Is.LessThan(.01f));
        }

        private static T Read<T>(Object target, string field) where T : Object
            => (T)new SerializedObject(target).FindProperty(field).objectReferenceValue;

        private static void Move(FirstPersonController player, Vector3 position)
        {
            var collider = player.GetComponent<CharacterController>(); collider.enabled = false;
            player.transform.position = position; collider.enabled = true; Physics.SyncTransforms();
        }
    }
}
#endif
