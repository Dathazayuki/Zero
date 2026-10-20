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
    public abstract class LivingRoomFamilyAnomalyTests
    {
        protected LivingRoomAnomalyPreview preview;
        protected FirstPersonController player;
        protected Transform camera;

        [UnitySetUp]
        public IEnumerator Load()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Game/Scenes/Demos/InfinityRoom2_LivingRoom_Demo.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            preview = Object.FindFirstObjectByType<LivingRoomAnomalyPreview>();
            player = Object.FindFirstObjectByType<FirstPersonController>();
            camera = player.GetComponentInChildren<Camera>().transform;
            player.enabled = false;
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            preview.ResetPreview();
            player.GetComponent<PlayerDeathController>().enabled = false;
            yield return null;
        }

        protected AnomalyBehaviour Select(string id)
        {
            preview.ResetPreview();
            for (int i = 0; i < preview.Anomalies.Count; i++)
            {
                preview.CycleNext();
                if (preview.Current.Data.Id == id) return preview.Current;
            }
            Assert.Fail("Missing authored anomaly " + id);
            return null;
        }

        protected void Move(Vector3 point)
        {
            player.Teleport(point, Quaternion.identity);
            Physics.SyncTransforms();
        }

    }
}
#endif
