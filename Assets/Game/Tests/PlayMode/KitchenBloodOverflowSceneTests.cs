#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Zero.AnomalyPrototype.Anomalies;
using Zero.AnomalyPrototype.Demo;
using Zero.AnomalyPrototype.Player;

namespace Zero.Tests.PlayMode
{
    public sealed class KitchenBloodOverflowSceneTests
    {
        [UnityTest]
        public IEnumerator AuthoredFlowFillsSpillsAndF2RestoresTheDryRoom()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/Game/Scenes/Demos/InfinityRoom4_HotelKitchen_Demo.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            var preview = Object.FindFirstObjectByType<LivingRoomAnomalyPreview>();
            var player = Object.FindFirstObjectByType<FirstPersonController>();
            var character = player.GetComponent<CharacterController>();
            Vector3 before = player.transform.position;
            player.enabled = false; character.enabled = false;
            try
            {
                for (int i = 0; i < 7; i++) Assert.That(preview.CycleNext(), Is.True);
                Assert.That(preview.Current.Data.Id, Is.EqualTo("IR4-09"));
                Assert.That(preview.Current.Data.CanKillPlayer, Is.False);
                var anomaly = (KitchenBloodOverflowAnomaly)preview.Current;
                Assert.That(anomaly.IsConfigured, Is.True);
                player.transform.position = anomaly.transform.position - Vector3.forward;
                anomaly.Tick(5f);
                Assert.That(anomaly.FillProgress, Is.InRange(0.49f, 0.55f));
                anomaly.Tick(14f);
                yield return null;
                Assert.That(anomaly.IsOverflowing, Is.True);
                foreach (var ps in anomaly.GetComponentsInChildren<ParticleSystem>(true)) Assert.That(ps.isPlaying, Is.True);
                preview.ResetPreview();
                Assert.That(anomaly.IsOverflowing, Is.False);
                Assert.That(anomaly.FillProgress, Is.Zero);
                foreach (var ps in anomaly.GetComponentsInChildren<ParticleSystem>(true)) Assert.That(ps.particleCount, Is.Zero);
            }
            finally
            {
                preview.ResetPreview(); player.transform.position = before;
                character.enabled = true; player.enabled = true;
            }
        }
    }
}
#endif
