using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Zero.Editor
{
    /// <summary>Explicit authoring only; creates a retargetable clip from a selected Humanoid.</summary>
    public static class SeatedPhoneAnimationTools
    {
        private const string ClipPath = "Assets/Game/Animation/SeatedPhoneScroll.anim";
        private const string ControllerPath = "Assets/Game/Animation/SeatedPhonePreview.controller";
        private const float Duration = 6f;
        private const int Samples = 13;

        [MenuItem("Tools/Zero/Animation/Create Seated Phone Clip")]
        private static void CreateClip()
        {
            Animator source = SelectedAnimator();
            if (source == null) return;
            GameObject temporary = UnityEngine.Object.Instantiate(source.gameObject);
            temporary.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                AnimationClip clip = Bake(temporary.GetComponent<Animator>());
                AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
                if (existing == null) AssetDatabase.CreateAsset(clip, ClipPath);
                else
                {
                    Undo.RecordObject(existing, "Update seated phone clip");
                    EditorUtility.CopySerialized(clip, existing);
                    AssetDatabase.SaveAssetIfDirty(existing);
                    UnityEngine.Object.DestroyImmediate(clip);
                    clip = existing;
                }
                Debug.Log($"PHONE_ANIMATION_CREATED: path={ClipPath}, seconds={Duration}, humanoid={clip.humanMotion}", source);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(temporary);
            }
        }

        private static Animator SelectedAnimator()
        {
            Animator animator = Selection.activeGameObject != null
                ? Selection.activeGameObject.GetComponentInChildren<Animator>(true) : null;
            if (animator != null && animator.avatar != null && animator.avatar.isValid && animator.isHuman)
                return animator;
            Debug.LogError("PHONE_ANIMATION_CONFIGURATION_INVALID: select a valid Humanoid Animator.");
            return null;
        }

        private static AnimationClip Bake(Animator animator)
        {
            animator.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            animator.runtimeAnimatorController = null;
            animator.Rebind();
            using (var neutralHandler = new HumanPoseHandler(animator.avatar, animator.transform))
            {
                var neutral = new HumanPose();
                neutralHandler.GetHumanPose(ref neutral);
                System.Array.Clear(neutral.muscles, 0, neutral.muscles.Length);
                neutral.bodyRotation = Quaternion.identity;
                neutralHandler.SetHumanPose(ref neutral);
            }
            Transform[] bones = animator.GetComponentsInChildren<Transform>();
            var bindPose = new Pose[bones.Length];
            for (int i = 0; i < bones.Length; i++)
            {
                bindPose[i] = new Pose(bones[i].localPosition, bones[i].localRotation);
            }
            var curves = CreateCurves(HumanTrait.MuscleCount + 7);
            using (var handler = new HumanPoseHandler(animator.avatar, animator.transform))
                CaptureFrames(animator, handler, bones, bindPose, curves);
            return BuildClip(curves);
        }

        private static AnimationCurve[] CreateCurves(int count)
        {
            var curves = new AnimationCurve[count];
            for (int i = 0; i < count; i++) curves[i] = new AnimationCurve();
            return curves;
        }

        private static void CaptureFrames(Animator animator, HumanPoseHandler handler,
            Transform[] bones, Pose[] bindPose, AnimationCurve[] curves)
        {
            var pose = new HumanPose();
            for (int frame = 0; frame < Samples; frame++)
            {
                for (int i = 0; i < bones.Length; i++)
                {
                    bones[i].localPosition = bindPose[i].position;
                    bones[i].localRotation = bindPose[i].rotation;
                }
                float time = Duration * frame / (Samples - 1);
                ApplyPose(animator, time);
                handler.GetHumanPose(ref pose);
                SetFingerMuscles(pose.muscles, time);
                AddKeys(curves, pose, time);
            }
        }

        private static void AddKeys(AnimationCurve[] curves, HumanPose pose, float time)
        {
            int count = HumanTrait.MuscleCount;
            for (int i = 0; i < count; i++) curves[i].AddKey(time, pose.muscles[i]);
            curves[count].AddKey(time, pose.bodyPosition.x);
            curves[count + 1].AddKey(time, pose.bodyPosition.y);
            curves[count + 2].AddKey(time, pose.bodyPosition.z);
            curves[count + 3].AddKey(time, pose.bodyRotation.x);
            curves[count + 4].AddKey(time, pose.bodyRotation.y);
            curves[count + 5].AddKey(time, pose.bodyRotation.z);
            curves[count + 6].AddKey(time, pose.bodyRotation.w);
        }

        private static AnimationClip BuildClip(AnimationCurve[] curves)
        {
            var clip = new AnimationClip { name = "SeatedPhoneScroll", frameRate = 30f };
            ReduceConstantCurves(curves);
            for (int i = 0; i < HumanTrait.MuscleCount; i++)
                AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Animator), MuscleBinding(HumanTrait.MuscleName[i])), curves[i]);
            string[] body = { "RootT.x", "RootT.y", "RootT.z", "RootQ.x", "RootQ.y", "RootQ.z", "RootQ.w" };
            for (int i = 0; i < body.Length; i++)
                AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Animator), body[i]), curves[HumanTrait.MuscleCount + i]);
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.keepOriginalPositionY = true;
            settings.keepOriginalPositionXZ = true;
            settings.keepOriginalOrientation = true;
            settings.loopBlendPositionY = true;
            settings.loopBlendPositionXZ = true;
            settings.loopBlendOrientation = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            return clip;
        }

        private static string MuscleBinding(string muscle)
        {
            string[] parts = muscle.Split(' ');
            if (parts.Length != 3 && parts.Length != 4) return muscle;
            if (parts[1] != "Thumb" && parts[1] != "Index" && parts[1] != "Middle" &&
                parts[1] != "Ring" && parts[1] != "Little") return muscle;
            return parts[0] + "Hand." + parts[1] + "." + string.Join(" ", parts, 2, parts.Length - 2);
        }

        private static void ApplyPose(Animator animator, float time)
        {
            Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            float height = (animator.GetBoneTransform(HumanBodyBones.Head).position -
                animator.GetBoneTransform(HumanBodyBones.LeftFoot).position).magnitude;
            float scale = height / 1.55f;
            hips.position = new Vector3(0f, .55f * scale, -.12f * scale);
            Turn(animator, HumanBodyBones.Spine, 9f, Vector3.right);
            Turn(animator, HumanBodyBones.Head, 23f, Vector3.right);
            Solve(animator, new[] { HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg,
                HumanBodyBones.LeftFoot }, new Vector3(-.13f, .10f, .34f) * scale, Vector3.forward);
            Solve(animator, new[] { HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg,
                HumanBodyBones.RightFoot }, new Vector3(.13f, .10f, .34f) * scale, Vector3.forward);
            Solve(animator, new[] { HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm,
                HumanBodyBones.LeftHand }, new Vector3(-.075f, .94f, .27f) * scale, Vector3.left + Vector3.down);
            Vector3 thigh = Vector3.Lerp(animator.GetBoneTransform(HumanBodyBones.RightUpperLeg).position,
                animator.GetBoneTransform(HumanBodyBones.RightLowerLeg).position, .6f);
            Solve(animator, new[] { HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm,
                HumanBodyBones.RightHand }, thigh + Vector3.up * .13f * scale, Vector3.right + Vector3.down);
            AimHand(animator, HumanBodyBones.RightHand, new[] { HumanBodyBones.RightMiddleProximal, HumanBodyBones.RightLittleProximal },
                Vector3.forward, Vector3.up);
            Turn(animator, HumanBodyBones.LeftHand, .8f * Mathf.Sin(time * Mathf.PI * 2f / 3f), Vector3.forward);
        }

        private static void Turn(Animator animator, HumanBodyBones bone, float angle, Vector3 axis)
        {
            Transform transform = animator.GetBoneTransform(bone);
            if (transform != null) transform.rotation = Quaternion.AngleAxis(angle, axis) * transform.rotation;
        }

        private static void Solve(Animator animator, HumanBodyBones[] chain, Vector3 target, Vector3 pole)
        {
            Transform a = animator.GetBoneTransform(chain[0]);
            Transform b = animator.GetBoneTransform(chain[1]);
            Transform c = animator.GetBoneTransform(chain[2]);
            float first = Vector3.Distance(a.position, b.position);
            float second = Vector3.Distance(b.position, c.position);
            Vector3 direction = (target - a.position).normalized;
            float distance = Mathf.Clamp(Vector3.Distance(a.position, target), Mathf.Abs(first - second) + .0001f, first + second - .0001f);
            float along = (first * first - second * second + distance * distance) / (2f * distance);
            Vector3 bend = Vector3.ProjectOnPlane(pole, direction).normalized;
            Vector3 joint = a.position + direction * along + bend * Mathf.Sqrt(Mathf.Max(0f, first * first - along * along));
            a.rotation = Quaternion.FromToRotation(b.position - a.position, joint - a.position) * a.rotation;
            b.rotation = Quaternion.FromToRotation(c.position - b.position, target - b.position) * b.rotation;
        }

        private static void AimHand(Animator animator, HumanBodyBones handBone, HumanBodyBones[] fingers,
            Vector3 direction, Vector3 palmNormal)
        {
            Transform hand = animator.GetBoneTransform(handBone);
            Vector3 middle = animator.GetBoneTransform(fingers[0]).position - hand.position;
            Vector3 little = animator.GetBoneTransform(fingers[1]).position - hand.position;
            Quaternion basis = Quaternion.LookRotation(middle, Vector3.Cross(middle, little));
            Quaternion target = Quaternion.LookRotation(direction, Vector3.ProjectOnPlane(palmNormal, direction));
            hand.rotation = target * Quaternion.Inverse(basis) * hand.rotation;
        }

        private static void SetFingerMuscles(float[] muscles, float time)
        {
            float swipe = Mathf.Sin(time * Mathf.PI * 2f / 3f);
            for (int i = 0; i < muscles.Length; i++)
            {
                string name = HumanTrait.MuscleName[i];
                if (name == "Left Hand Down-Up") muscles[i] = -.05f;
                if (name == "Left Hand In-Out") muscles[i] = 0f;
                if (name == "Left Forearm Twist In-Out") muscles[i] = -.55f;
                if (name == "Left Thumb Spread") muscles[i] = -.5f + .2f * swipe;
                if (!name.Contains("Stretched")) continue;
                muscles[i] = name.StartsWith("Left ") ? -.65f : .2f;
                if (name == "Left Thumb 1 Stretched") muscles[i] = -.35f + .25f * swipe;
                if (name == "Left Thumb 2 Stretched") muscles[i] = -.2f + .3f * swipe;
                if (name == "Left Thumb 3 Stretched") muscles[i] = -.1f + .18f * swipe;
            }
        }

        private static void ReduceConstantCurves(AnimationCurve[] curves)
        {
            for (int i = 0; i < curves.Length; i++)
            {
                Keyframe[] keys = curves[i].keys;
                float min = keys[0].value;
                float max = min;
                foreach (Keyframe key in keys)
                {
                    min = Mathf.Min(min, key.value);
                    max = Mathf.Max(max, key.value);
                }
                if (max - min < .0001f) curves[i] = AnimationCurve.Constant(0f, Duration, keys[0].value);
            }
        }

        [MenuItem("Tools/Zero/Animation/Create Seated Phone Preview")]
        private static void CreatePreview()
        {
            Animator source = SelectedAnimator();
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
            if (source == null || clip == null) return;
            var copy = UnityEngine.Object.Instantiate(source.gameObject);
            copy.name = "SeatedPhone_Test";
            copy.transform.SetParent(null);
            copy.transform.SetPositionAndRotation(new Vector3(-10f, 0f, 0f), Quaternion.identity);
            Undo.RegisterCreatedObjectUndo(copy, "Create seated phone preview");
            Animator animator = copy.GetComponent<Animator>();
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
                controller.AddMotion(clip);
            }
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            clip.SampleAnimation(copy, 0f);
            CreatePhone(animator);
            CreateCamera(copy.transform);
            Selection.activeGameObject = copy;
            Debug.Log("PHONE_ANIMATION_PREVIEW_CREATED: select clip in Animation window or press Play.", copy);
        }

        private static void CreatePhone(Animator animator)
        {
            Transform hand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            GameObject phone = GameObject.CreatePrimitive(PrimitiveType.Cube);
            phone.name = "Phone_Prototype";
            UnityEngine.Object.DestroyImmediate(phone.GetComponent<Collider>());
            phone.transform.SetPositionAndRotation(hand.position + new Vector3(.05f, .06f, .015f), Quaternion.Euler(25f, 0f, 0f));
            phone.transform.localScale = new Vector3(.075f, .145f, .010f);
            phone.transform.SetParent(hand, true);
            PositionPhone(animator, phone.transform);
        }

        private static void PositionPhone(Animator animator, Transform phone)
        {
            Transform hand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            Vector3 up = new Vector3(0f, .7f, .714f);
            Vector3 screenNormal = animator.transform.forward;
            phone.SetPositionAndRotation(hand.position + up * .07f + screenNormal * .015f,
                Quaternion.LookRotation(screenNormal, up));
        }

        [MenuItem("Tools/Zero/Animation/Refresh Seated Phone Preview")]
        private static void RefreshPreview()
        {
            PreviewSwipe();
            Animator animator = SelectedAnimator();
            if (animator == null) return;
            Transform phone = animator.GetBoneTransform(HumanBodyBones.LeftHand).Find("Phone_Prototype");
            if (phone == null) return;
            Undo.RecordObject(phone, "Adjust one-handed phone grip");
            PositionPhone(animator, phone);
        }

        private static void CreateCamera(Transform target)
        {
            var cameraObject = new GameObject("SeatedPhone_PreviewCamera", typeof(Camera));
            Undo.RegisterCreatedObjectUndo(cameraObject, "Create phone preview camera");
            cameraObject.transform.position = target.position + new Vector3(-1.25f, 1.25f, 1.7f);
            cameraObject.transform.LookAt(target.position + Vector3.up * .85f);
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.fieldOfView = 45f;
            camera.enabled = false;
        }

        [MenuItem("Tools/Zero/Animation/Preview Phone Swipe")]
        private static void PreviewSwipe()
        {
            Animator animator = SelectedAnimator();
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
            if (animator == null || clip == null) return;
            Transform[] bones = animator.GetComponentsInChildren<Transform>();
            Undo.RecordObjects(bones, "Preview phone swipe");
            Vector3 position = animator.transform.position;
            clip.SampleAnimation(animator.gameObject, .75f);
            animator.transform.position = position;
            Debug.Log($"PHONE_ANIMATION_SAMPLED: time=0.75, leftThumb={animator.GetBoneTransform(HumanBodyBones.LeftThumbDistal).position}", animator);
        }
    }
}
