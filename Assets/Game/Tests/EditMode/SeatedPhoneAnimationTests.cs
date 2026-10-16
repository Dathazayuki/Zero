using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Zero.Tests.EditMode
{
    public sealed class SeatedPhoneAnimationTests
    {
        private const string ClipPath = "Assets/Game/Animation/SeatedPhoneScroll.anim";

        [Test]
        public void ScrollUsesValidFingerBindingsAndLoopsWithoutPoseJump()
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
            Assert.That(clip, Is.Not.Null);
            Assert.That(clip.humanMotion, Is.True);
            Assert.That(AnimationUtility.GetAnimationClipSettings(clip).loopTime, Is.True);
            int movingThumbCurves = 0;
            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip))
            {
                AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
                float initial = curve.Evaluate(0f);
                Assert.That(curve.Evaluate(clip.length), Is.EqualTo(initial).Within(.0001f), binding.propertyName);
                Assert.That(binding.propertyName.StartsWith("Left Thumb"), Is.False, "HumanTrait finger names are not Animator curve bindings.");
                bool thumb = binding.propertyName.StartsWith("LeftHand.Thumb.");
                if (binding.propertyName == "Left Hand Down-Up")
                    Assert.That(initial, Is.InRange(-.15f, .15f), "The holding wrist must stay near straight.");
                foreach (Keyframe key in curve.keys)
                {
                    if (thumb) continue;
                    bool holdingArm = binding.propertyName.StartsWith("Left Hand") || binding.propertyName.StartsWith("Left Forearm") || binding.propertyName.StartsWith("Left Arm");
                    Assert.That(key.value, Is.EqualTo(initial).Within(holdingArm ? .03f : .0001f), binding.propertyName);
                }
                if (thumb && Mathf.Abs(curve.Evaluate(.75f) - initial) > .05f) movingThumbCurves++;
            }
            Assert.That(movingThumbCurves, Is.EqualTo(4), "Three thumb joints and spread must scroll.");
        }

        [Test]
        public void ScrollActuallyMovesThumbOnPlayerAvatar()
        {
            const string modelPath = "Assets/Game/Model/FullModel/PlayerHumanoidNoAnimation/playertpose/Meshy_AI_Character_output (3).fbx";
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            Assert.That(source, Is.Not.Null);
            GameObject instance = Object.Instantiate(source);
            instance.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                Animator animator = instance.GetComponentInChildren<Animator>();
                Assert.That(animator, Is.Not.Null);
                animator.runtimeAnimatorController = null;
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
                Transform thumb = animator.GetBoneTransform(HumanBodyBones.LeftThumbProximal);
                Transform restHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
                clip.SampleAnimation(animator.gameObject, 0f);
                Quaternion initialThumb = thumb.localRotation;
                Vector3 initialRestHand = restHand.position;
                clip.SampleAnimation(animator.gameObject, .75f);
                Assert.That(Quaternion.Angle(initialThumb, thumb.localRotation), Is.GreaterThan(3f));
                Assert.That(Vector3.Distance(initialRestHand, restHand.position), Is.LessThan(.0001f));
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }
    }
}
