using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Zero.AnomalyPrototype.Anomalies;

namespace Zero.Tests.EditMode
{
    public sealed class CabaretBlackHoleAnomalyTests
    {
        [Test]
        public void StandTravelsNaturallyAndDoesNotAddHeightAgainWhenSnapping()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TripoModels/tuxedo_suit_3d_model/tuxedo_suit_3d_model.fbx");
            var clone = Object.Instantiate(model);
            try
            {
                clone.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                var actor = clone.GetComponent<Animator>();
                actor.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                    "Assets/Game/MixamoAnimation/Host/IR5_16_HostSnap.controller");
                actor.applyRootMotion = true; actor.Rebind();
                actor.Play("Sitting", 0, 0); actor.Update(0);
                actor.CrossFadeInFixedTime("SitToStand", .12f, 0, 0);
                for (int i = 0; i < 140; i++) actor.Update(1f / 60);
                float height = actor.GetBoneTransform(HumanBodyBones.Hips).position.y;
                Assert.That(actor.transform.position.y, Is.EqualTo(0).Within(.02f));
                Assert.That(new Vector2(actor.transform.position.x, actor.transform.position.z).magnitude, Is.GreaterThan(.3f));
                Assert.That(actor.GetBoneTransform(HumanBodyBones.RightFoot).position.y, Is.GreaterThan(-.05f));
                actor.CrossFadeInFixedTime("StandingFingerSnap", .12f, 0, 0);
                for (int i = 0; i < 18; i++) actor.Update(1f / 60);
                Assert.That(actor.GetBoneTransform(HumanBodyBones.Hips).position.y, Is.EqualTo(height).Within(.1f));
            }
            finally { Object.DestroyImmediate(clone); }
        }

        [Test]
        public void OnlyFeetInsideTheFixedHoleAtTheSameLevelAreCaught()
        {
            var center = new Vector3(1f, .03f, 2f);
            Assert.That(CabaretBlackHoleAnomaly.IsInsideHole(new Vector3(1f, .04f, 2f), center, .95f), Is.True);
            Assert.That(CabaretBlackHoleAnomaly.IsInsideHole(new Vector3(2f, .04f, 2f), center, .95f), Is.False);
            Assert.That(CabaretBlackHoleAnomaly.IsInsideHole(new Vector3(1f, 2f, 2f), center, .95f), Is.False);
            Assert.That(CabaretBlackHoleAnomaly.IsInsideHole(new Vector3(1.6f, .04f, 2.6f), center, .95f), Is.True);
        }

        [Test]
        public void ResetAndDisableWithMissingReferencesAreSafe()
        {
            var root = new GameObject("BlackHoleResetTest");
            try
            {
                var anomaly = root.AddComponent<CabaretBlackHoleAnomaly>();
                Assert.That(anomaly.IsConfigured, Is.False);
                anomaly.ResetAnomaly(); anomaly.ResetAnomaly(); root.SetActive(false);
                Assert.That(anomaly.IsActive, Is.False);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
