using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using Zero.AnomalyPrototype.Anomalies;

namespace Zero.Tests.EditMode
{
    public sealed class KitchenWhiteRatAnomalyTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void FallUsesRootMotionAndResetRestoresOriginalSettingsAndPose(bool originalRootMotion)
        {
            var root = new GameObject("RootMotionRegression");
            var controller = new AnimatorController(); controller.AddLayer("Base Layer");
            var machine = controller.layers[0].stateMachine;
            var idle = machine.AddState("Idle"); var deathState = machine.AddState("Death"); machine.defaultState = idle;
            try
            {
                var chef = Child(root, "Chef").AddComponent<Animator>(); chef.runtimeAnimatorController = controller;
                var rat = Child(root, "Rat").AddComponent<Animator>(); rat.runtimeAnimatorController = controller;
                var hat = Child(chef.gameObject, "Hat").AddComponent<SkinnedMeshRenderer>();
                var normal = Child(root, "Normal");
                chef.applyRootMotion = originalRootMotion; chef.speed = 0.7f;
                var actor = root.AddComponent<KitchenWhiteRatActor>();
                BindActor(actor, chef, rat, hat.transform, normal);
                actor.Begin(); actor.Fall();
                Assert.That(chef.applyRootMotion, Is.True);
                Vector3 moved = new Vector3(0.3f, -0.4f, 0.2f);
                chef.transform.position = moved;
                actor.BeginHat();
                Assert.That(chef.transform.position, Is.EqualTo(moved));
                Assert.That(chef.speed, Is.Zero);
                actor.Restore(); actor.Restore();
                Assert.That(chef.transform.position, Is.EqualTo(Vector3.zero));
                Assert.That(chef.applyRootMotion, Is.EqualTo(originalRootMotion));
                Assert.That(chef.speed, Is.EqualTo(0.7f));
            }
            finally
            {
                Object.DestroyImmediate(root); Object.DestroyImmediate(idle); Object.DestroyImmediate(deathState);
                Object.DestroyImmediate(machine); Object.DestroyImmediate(controller);
            }
        }
        private static void BindActor(KitchenWhiteRatActor actor, Animator chef, Animator rat, Transform hat, GameObject normal)
        {
            var so = new SerializedObject(actor);
            so.FindProperty("chef").objectReferenceValue = chef;
            so.FindProperty("rat").objectReferenceValue = rat;
            so.FindProperty("hat").objectReferenceValue = hat;
            so.FindProperty("head").objectReferenceValue = chef.transform;
            so.FindProperty("normalChef").objectReferenceValue = normal;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        private static GameObject Child(GameObject root, string name)
        {
            var child = new GameObject(name); child.transform.SetParent(root.transform, false);
            return child;
        }
        [Test]
        public void MissingSceneReferencesDoNotLockPlayerAndRepeatedResetIsSafe()
        {
            var root = new GameObject("UnconfiguredWhiteRat");
            try
            {
                var anomaly = root.AddComponent<KitchenWhiteRatAnomaly>();
                Assert.DoesNotThrow(() => anomaly.Activate());
                Assert.DoesNotThrow(() => anomaly.SendMessage("LateUpdate"));
                anomaly.ResetAnomaly(); anomaly.ResetAnomaly();
                Assert.That(anomaly.IsActive, Is.False);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
