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

namespace Zero.Tests.PlayMode
{
    public sealed class CabaretCurtainCallAnomalySceneTests
    {
        [UnityTest]
        public IEnumerator HostIdlesBowsThenFieldExpandsAndExitRestoresCamera()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/Game/Scenes/Demos/InfinityRoom5_CrimsonCabaret_Demo.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            var anomaly = Object.FindFirstObjectByType<CabaretCurtainCallAnomaly>();
            var actor = Read<Animator>(anomaly, "actor");
            var body = Read<CharacterController>(anomaly, "playerBody");
            body.GetComponent<FirstPersonController>().enabled = false;
            var view = Read<CabaretGlitchFieldView>(anomaly, "field");
            var cameraData = Read<Component>(view, "cameraData");
            int original = ColourOption(cameraData);
            Assert.That(anomaly.IsConfigured, Is.True);
            anomaly.Activate(); yield return null;
            Assert.That(actor.GetCurrentAnimatorStateInfo(0).shortNameHash, Is.EqualTo(Animator.StringToHash("IR5_12_HostIdle")));
            Move(body, actor.transform.position + Vector3.back * 3f);
            yield return null; yield return null;
            Assert.That(anomaly.State, Is.EqualTo(CabaretCurtainCallRules.Phase.Bowing));
            float timeout = Time.time + 20f;
            while (anomaly.State == CabaretCurtainCallRules.Phase.Bowing && Time.time < timeout) yield return null;
            Assert.That(anomaly.State, Is.EqualTo(CabaretCurtainCallRules.Phase.Spreading));
            Assert.That(anomaly.FieldRadius, Is.GreaterThan(0f));
            Assert.That(ColourOption(cameraData), Is.EqualTo(1));
            Move(body, Read<BoxCollider>(anomaly, "escapeZone").bounds.center - Vector3.up);
            yield return null;
            Assert.That(anomaly.State, Is.EqualTo(CabaretCurtainCallRules.Phase.Escaped));
            Assert.That(ColourOption(cameraData), Is.EqualTo(original));
            anomaly.ResetAnomaly(); anomaly.ResetAnomaly();
            Assert.That(actor.gameObject.activeSelf, Is.False);
        }

        private static T Read<T>(Object target, string field) where T : Object
            => (T)new SerializedObject(target).FindProperty(field).objectReferenceValue;

        private static int ColourOption(Object cameraData)
            => new SerializedObject(cameraData).FindProperty("m_RequiresOpaqueTextureOption").enumValueIndex;

        private static void Move(CharacterController body, Vector3 position)
        {
            body.enabled = false; body.transform.position = position;
            body.enabled = true; Physics.SyncTransforms();
        }
    }
}
#endif
