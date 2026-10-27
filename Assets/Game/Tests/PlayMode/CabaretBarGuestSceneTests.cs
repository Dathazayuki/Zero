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

namespace Zero.Tests.PlayMode
{
    public sealed class CabaretBarGuestSceneTests
    {
        [UnityTest]
        public IEnumerator SittingNeckTracksStageAndLookAwayReturnVanishesThenResetRestores()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/Game/Scenes/Demos/InfinityRoom5_CrimsonCabaret_Demo.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null;
            var preview = Object.FindFirstObjectByType<LivingRoomAnomalyPreview>();
            var guest = Object.FindFirstObjectByType<CabaretBarGuestAnomaly>();
            var player = Object.FindFirstObjectByType<FirstPersonController>(); player.enabled = false;
            for (int i = 0; i < preview.Anomalies.Count && preview.Current != guest; i++) preview.CycleNext();
            Assert.That(preview.Current, Is.SameAs(guest)); Assert.That(guest.Data.CanKillPlayer, Is.False);
            var so = new SerializedObject(guest);
            var actor = (Animator)so.FindProperty("actor").objectReferenceValue;
            var neck = (Transform)so.FindProperty("neck").objectReferenceValue;
            var target = (Transform)so.FindProperty("stageTarget").objectReferenceValue;
            var glass = (Transform)so.FindProperty("glass").objectReferenceValue;
            Quaternion normalGlass = glass.localRotation;
            Vector3 aim = neck.InverseTransformDirection(actor.transform.forward);
            var camera = player.PlayerCamera.transform;
            camera.position = neck.position + Vector3.right * 1.8f;
            camera.LookAt(neck);
            yield return new WaitForSeconds(.15f);
            Assert.That(Vector3.Angle(neck.TransformDirection(aim), target.position - neck.position), Is.LessThan(1f));
            Assert.That(guest.State, Is.EqualTo(CabaretBarGuestRules.Phase.Watching));
            camera.rotation = Quaternion.LookRotation(Vector3.right);
            yield return new WaitForSeconds(.4f);
            Assert.That(guest.State, Is.EqualTo(CabaretBarGuestRules.Phase.Armed));
            camera.LookAt(neck); yield return null; yield return null;
            Assert.That(guest.State, Is.EqualTo(CabaretBarGuestRules.Phase.Vanished));
            Assert.That(actor.gameObject.activeSelf, Is.False);
            yield return new WaitForSeconds(.8f);
            Assert.That(Quaternion.Angle(glass.localRotation, normalGlass), Is.LessThan(.01f));
            preview.ResetPreview(); preview.ResetPreview();
            Assert.That(guest.IsActive, Is.False); Assert.That(actor.gameObject.activeSelf, Is.False);
        }
    }
}
#endif
