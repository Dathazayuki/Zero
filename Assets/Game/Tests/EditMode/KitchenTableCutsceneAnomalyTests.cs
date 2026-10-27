using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.TestTools;
using Zero.AnomalyPrototype.Anomalies;
using Zero.AnomalyPrototype.Player;

namespace Zero.Tests.EditMode
{
    public sealed class KitchenTableCutsceneAnomalyTests
    {
        [Test]
        public void PigOnlyAppearsDuringAnomalyAndNormalChefReturnsOnReset()
        {
            var root = new GameObject("ChefVisibilityTest");
            var controller = new AnimatorController(); controller.AddLayer("Base Layer");
            var machine = controller.layers[0].stateMachine; var idle = machine.AddState("Idle"); machine.defaultState = idle;
            try
            {
                var normal = Child(root, "NormalChef"); var pig = Child(root, "PigChef"); pig.SetActive(false);
                var animator = pig.AddComponent<Animator>(); animator.runtimeAnimatorController = controller;
                var playerObject = Child(root, "Player"); var player = playerObject.AddComponent<FirstPersonController>();
                var camera = Child(playerObject, "Camera").AddComponent<Camera>();
                Set(player, "playerCamera", camera);
                var anomaly = root.AddComponent<KitchenTableCutsceneAnomaly>();
                Set(anomaly, "actor", animator); Set(anomaly, "player", player);
                Set(anomaly, "death", playerObject.AddComponent<PlayerDeathController>());
                Set(anomaly, "cameraAnchor", Child(root, "Anchor").transform);
                Set(anomaly, "cutsceneController", controller); Set(anomaly, "captureFade", Child(root, "Fade").AddComponent<CanvasGroup>());
                var so = new SerializedObject(anomaly); var props = so.FindProperty("hiddenProps"); props.arraySize = 1;
                props.GetArrayElementAtIndex(0).objectReferenceValue = normal; so.ApplyModifiedPropertiesWithoutUndo();
                anomaly.Activate(); Assert.That(pig.activeSelf, Is.True); Assert.That(normal.activeSelf, Is.False);
                anomaly.ResetAnomaly(); anomaly.ResetAnomaly();
                Assert.That(pig.activeSelf, Is.False); Assert.That(normal.activeSelf, Is.True);
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(idle); Object.DestroyImmediate(machine); Object.DestroyImmediate(controller); }
        }

        private static GameObject Child(GameObject parent, string name)
        {
            var go = new GameObject(name); go.transform.SetParent(parent.transform, false); return go;
        }

        private static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target); so.FindProperty(field).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo();
        }

        [Test]
        public void MissingReferencesFailWithoutLockingPlayerAndDuplicateResetIsSafe()
        {
            var root = new GameObject("UnconfiguredTableCutscene");
            try
            {
                var anomaly = root.AddComponent<KitchenTableCutsceneAnomaly>();
                Assert.That(anomaly.IsConfigured, Is.False);
                LogAssert.Expect(LogType.Error, "KITCHEN_TABLE_CONFIGURATION_INVALID");
                anomaly.Activate();
                anomaly.ResetAnomaly(); anomaly.ResetAnomaly();
                Assert.That(anomaly.IsActive, Is.False);
                Assert.That(anomaly.State, Is.EqualTo(KitchenTableCutsceneRules.Phase.Waiting));
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
