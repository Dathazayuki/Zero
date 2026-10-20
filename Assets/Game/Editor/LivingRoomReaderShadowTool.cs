using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Zero.AnomalyPrototype.Anomalies;

namespace Zero.EditorTools
{
    public static class LivingRoomReaderShadowTool
    {
        private const string Folder = "Assets/Game/Prefabs/InfinityRoom2Demo/Anomalies/";
        private const string Room = "InfinityRoom2_LivingRoom_Demo/";
        private const string Node = Room + "IR2_Anomalies/IR2_06_ExtraReaderShadow";

        [MenuItem("Tools/Zero/Living Room/Replace Reader Shadow From Father")]
        private static void Replace()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().name != "InfinityRoom2_LivingRoom_Demo") return;
            GameObject father = Require("Father");
            GameObject chair = Require(Room + "IR2_Furniture/IR2_DetailArmchairA");
            GameObject node = Require(Node);
            Light lamp = Require(Room + "IR2_Lighting/IR2_FloorLampLight").GetComponent<Light>();
            Mesh mesh = Bake(father, chair.transform);
            Install(node, chair.transform, mesh);
            ConfigureShadows(chair, lamp);
            EditorSceneManager.MarkSceneDirty(node.scene);
            Debug.Log("IR2_SHADOW_REPLACED: Father seated on IR2_DetailArmchairA; realtime lamp shadows; manual save required", node);
        }

        private static GameObject Require(string path)
        {
            GameObject item = GameObject.Find(path);
            if (item == null) throw new InvalidOperationException("IR2_SHADOW_REFERENCE_MISSING: " + path);
            return item;
        }

        private static Mesh Bake(GameObject father, Transform chair)
        {
            GameObject clone = UnityEngine.Object.Instantiate(father);
            var parts = new List<Mesh>();
            try
            {
                Pose(clone, chair);
                var combines = new List<CombineInstance>();
                foreach (SkinnedMeshRenderer skin in clone.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    var part = new Mesh();
                    parts.Add(part);
                    skin.BakeMesh(part);
                    Matrix4x4 matrix = chair.worldToLocalMatrix * skin.transform.localToWorldMatrix;
                    for (int sub = 0; sub < part.subMeshCount; sub++)
                        combines.Add(new CombineInstance { mesh = part, subMeshIndex = sub, transform = matrix });
                }
                if (combines.Count == 0) throw new InvalidOperationException("IR2_SHADOW_SKIN_MISSING");
                var combined = new Mesh { name = "IR2_SeatedFatherCaster", indexFormat = IndexFormat.UInt32 };
                combined.CombineMeshes(combines.ToArray(), true, true);
                string path = Folder + "IR2_SeatedFatherCaster.asset";
                Mesh asset = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (asset == null) AssetDatabase.CreateAsset(combined, path);
                else { EditorUtility.CopySerialized(combined, asset); UnityEngine.Object.DestroyImmediate(combined); }
                return AssetDatabase.LoadAssetAtPath<Mesh>(path);
            }
            finally
            {
                foreach (Mesh part in parts) UnityEngine.Object.DestroyImmediate(part);
                UnityEngine.Object.DestroyImmediate(clone);
            }
        }

        private static void Pose(GameObject clone, Transform chair)
        {
            clone.SetActive(true);
            Animator animator = clone.GetComponent<Animator>();
            if (animator == null || !animator.isHuman || animator.runtimeAnimatorController == null)
                throw new InvalidOperationException("IR2_SHADOW_CURRENT_HUMANOID_CONTROLLER_REQUIRED");
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.Rebind();
            animator.Update(0f);
            animator.Play("Sit", 0, 0.45f);
            animator.Update(0f);
            Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            Vector3 knee = (animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg).position + animator.GetBoneTransform(HumanBodyBones.RightLowerLeg).position) * 0.5f;
            Vector3 forward = Vector3.ProjectOnPlane(knee - hips.position, Vector3.up).normalized;
            clone.transform.rotation = Quaternion.FromToRotation(forward, chair.forward) * clone.transform.rotation;
            Renderer seat = chair.Find("SeatCushions").GetComponent<Renderer>();
            Vector3 target = seat.bounds.center + chair.forward * 0.05f;
            target.y = seat.bounds.max.y + 0.12f;
            clone.transform.position += target - hips.position;
        }

        private static void Install(GameObject node, Transform chair, Mesh mesh)
        {
            Transform old = node.transform.Find("SeatedReaderShadow");
            if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
            var root = new GameObject("SeatedReaderShadow", typeof(MeshFilter), typeof(MeshRenderer));
            Undo.RegisterCreatedObjectUndo(root, "Fit Father shadow to armchair");
            root.transform.SetParent(node.transform, false);
            root.transform.SetPositionAndRotation(chair.position, chair.rotation);
            root.transform.localScale = chair.lossyScale;
            root.GetComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = root.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = CasterMaterial();
            renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            renderer.receiveShadows = false;
            root.SetActive(false);
            var anomaly = node.GetComponent<LivingRoomArrangementAnomaly>();
            Undo.RecordObject(anomaly, "Wire reader shadow");
            anomaly.Configure(Array.Empty<Transform>(), Array.Empty<Vector3>(), Array.Empty<Vector3>(), new[] { root }, Array.Empty<GameObject>());
            EditorUtility.SetDirty(anomaly);
        }

        private static Material CasterMaterial()
        {
            string path = Folder + "M_IR2_FatherShadowCaster.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetFloat("_Cull", (float)CullMode.Off);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void ConfigureShadows(GameObject chair, Light lamp)
        {
            Undo.RecordObject(lamp, "Enable floor lamp shadows");
            lamp.shadows = LightShadows.Soft;
            lamp.shadowStrength = 0.85f;
            lamp.shadowBias = 0.05f;
            lamp.shadowNormalBias = 0.2f;
            lamp.range = 5f;
            lamp.intensity = 1.1f;
            lamp.renderMode = LightRenderMode.ForcePixel;
            Component data = lamp.GetComponent("UniversalAdditionalLightData");
            if (data != null)
            {
                Undo.RecordObject(data, "Use authored lamp shadow bias");
                var serialized = new SerializedObject(data);
                serialized.FindProperty("m_UsePipelineSettings").boolValue = false;
                serialized.ApplyModifiedProperties();
            }
            foreach (Light light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light.gameObject.scene != chair.scene) continue;
                Undo.RecordObject(light, "Restore room shadow visibility");
                light.cullingMask |= (1 << 31) | (1 << 30);
            }
            foreach (Renderer renderer in chair.GetComponentsInChildren<Renderer>())
            {
                Undo.RecordObject(renderer, "Enable armchair shadows");
                renderer.shadowCastingMode = ShadowCastingMode.On;
            }
            foreach (Renderer renderer in Require(Room + "IR2_Furniture/IR2_FloorLamp").GetComponentsInChildren<Renderer>())
            {
                Undo.RecordObject(renderer, "Prevent lampshade blocking its own bulb");
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        [MenuItem("Tools/Zero/Living Room/Review Reader Shadow")]
        private static void Review()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var anomaly = Require(Node).GetComponent<LivingRoomArrangementAnomaly>();
            var cameraObject = new GameObject("IR2_ShadowReview", typeof(Camera));
            var target = new RenderTexture(1100, 800, 24);
            var pixels = new Texture2D(1100, 800, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active;
            try
            {
                anomaly.Activate();
                Camera camera = cameraObject.GetComponent<Camera>();
                camera.enabled = false;
                camera.fieldOfView = 65f;
                camera.nearClipPlane = 0.02f;
                camera.transform.position = new Vector3(2.8f, 2.6f, 0.5f);
                camera.transform.LookAt(new Vector3(0.3f, 0.3f, -3.1f));
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 1100, 800), 0, 0);
                pixels.Apply();
                Directory.CreateDirectory("Assets/Game/.scratch/LivingRoomAnomalies");
                File.WriteAllBytes("Assets/Game/.scratch/LivingRoomAnomalies/IR2_06_SeatedFather.png", pixels.EncodeToPNG());
            }
            finally
            {
                anomaly.ResetAnomaly();
                RenderTexture.active = previous;
                UnityEngine.Object.DestroyImmediate(cameraObject);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(pixels);
            }
        }
    }
}
