using System;
using UnityEditor;
using UnityEngine;
using Zero.AnomalyPrototype.Anomalies;

namespace Zero.Editor
{
    internal static class LivingRoomTelevisionAssets
    {
        internal const string Folder = "Assets/Game/Prefabs/InfinityRoom2Demo/Anomalies/";
        internal static readonly Vector3 ScreenPosition = new Vector3(2.30f, 1.25f, 4.00f);

        internal static TelevisionFeed Feed(Transform parent, GameObject[] sources, bool family)
        {
            GameObject stage = Node(parent, "TelevisionStudio");
            stage.transform.position = new Vector3(family ? 0f : 10f, -100f, 0f);
            var heads = new Transform[sources.Length];
            for (int i = 0; i < sources.Length; i++)
                heads[i] = Cast(sources[i], stage.transform, family ? (i - 1) * 0.8f : 0f, family);
            Camera camera = StudioCamera(stage.transform, heads[0], family);
            GameObject surface = GameObject.CreatePrimitive(PrimitiveType.Quad);
            surface.name = "BroadcastScreen";
            surface.transform.SetParent(parent, false);
            surface.transform.position = ScreenPosition;
            surface.transform.localScale = new Vector3(0.49f, 0.37f, 1f);
            UnityEngine.Object.DestroyImmediate(surface.GetComponent<Collider>());
            Renderer renderer = surface.GetComponent<Renderer>();
            renderer.sharedMaterial = Material("M_IR2_TelevisionFeed", Color.white);
            renderer.enabled = false;
            GameObject cracks = Cracks(parent);
            var feed = Undo.AddComponent<TelevisionFeed>(parent.gameObject);
            feed.Configure(renderer, camera, stage, heads, cracks);
            stage.SetActive(false);
            return feed;
        }

        private static Camera StudioCamera(Transform stage, Transform head, bool family)
        {
            Camera camera = Node(stage, "FeedCamera").AddComponent<Camera>();
            camera.enabled = false;
            camera.cullingMask = 1 << 31;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.015f, 0.02f, 0.018f);
            camera.nearClipPlane = 0.03f;
            camera.farClipPlane = 10f;
            camera.fieldOfView = family ? 42f : 38f;
            camera.transform.position = family ? stage.position + new Vector3(0f, 1.1f, -3.6f) : head.position + Vector3.back * 0.65f;
            camera.transform.LookAt(family ? stage.position + Vector3.up * 1.1f : head.position);
            Light light = Node(stage, "StudioLight").AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 8f;
            light.intensity = 3f;
            light.cullingMask = 1 << 31;
            light.transform.localPosition = new Vector3(-1f, 2.5f, -2f);
            return camera;
        }

        private static Transform Cast(GameObject source, Transform stage, float x, bool seated)
        {
            GameObject clone = UnityEngine.Object.Instantiate(source, stage);
            clone.name = "Broadcast_" + source.name;
            clone.SetActive(true);
            Animator rig = clone.GetComponent<Animator>();
            return PoseCast(rig, stage, x, seated);
        }

        private static Transform PoseCast(Animator rig, Transform stage, float x, bool seated)
        {
            rig.enabled = true;
            rig.speed = 1f;
            rig.keepAnimatorStateOnDisable = true;
            rig.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Game/MixamoAnimation/FamilyAnimation.controller");
            rig.applyRootMotion = false;
            SamplePose(rig, "Sit", 0.45f);
            rig.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            Transform hips = rig.GetBoneTransform(HumanBodyBones.Hips);
            rig.transform.position += stage.position + new Vector3(x, rig.name == "Broadcast_Children" ? 0.6f : 0.68f, 0f) - hips.position;
            foreach (Transform part in rig.GetComponentsInChildren<Transform>(true)) part.gameObject.layer = 31;
            return rig.GetBoneTransform(HumanBodyBones.Head);
        }

        internal static void SamplePose(Animator rig, string stateName, float normalizedTime)
        {
            var controller = (UnityEditor.Animations.AnimatorController)rig.runtimeAnimatorController;
            AnimationClip clip = null;
            foreach (var entry in controller.layers[0].stateMachine.states)
                if (entry.state.name == stateName) clip = entry.state.motion as AnimationClip;
            if (clip == null) throw new InvalidOperationException("IR2_TV_CLIP_MISSING: " + stateName);
            Transform[] bones = rig.GetComponentsInChildren<Transform>(true);
            var positions = new Vector3[bones.Length];
            var rotations = new Quaternion[bones.Length];
            AnimationMode.StartAnimationMode();
            try
            {
                AnimationMode.BeginSampling();
                AnimationMode.SampleAnimationClip(rig.gameObject, clip, clip.length * normalizedTime);
                AnimationMode.EndSampling();
                for (int i = 0; i < bones.Length; i++) { positions[i] = bones[i].localPosition; rotations[i] = bones[i].localRotation; }
            }
            finally { AnimationMode.StopAnimationMode(); }
            rig.enabled = false;
            for (int i = 0; i < bones.Length; i++) { bones[i].localPosition = positions[i]; bones[i].localRotation = rotations[i]; }
        }

        internal static void RepairStudio(Transform node, bool family)
        {
            Transform stage = node.Find("TelevisionStudio");
            stage.gameObject.SetActive(true);
            Animator[] cast = stage.GetComponentsInChildren<Animator>(true);
            Transform head = null;
            for (int i = 0; i < cast.Length; i++) head = PoseCast(cast[i], stage, family ? (i - 1) * 0.8f : 0f, family);
            Camera camera = stage.Find("FeedCamera").GetComponent<Camera>();
            camera.transform.position = family ? stage.position + new Vector3(0f, 1.1f, -3.6f) : head.position + Vector3.back * 0.65f;
            camera.transform.LookAt(family ? stage.position + Vector3.up * 1.1f : head.position);
            if (family && stage.Find("BroadcastSofa") == null)
            {
                var sofa = UnityEngine.Object.Instantiate(GameObject.Find("InfinityRoom2_LivingRoom_Demo/IR2_Furniture/IR2_DetailSofa"), stage);
                sofa.name = "BroadcastSofa";
                sofa.transform.localPosition = new Vector3(0f, 0f, 0.12f);
                sofa.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                foreach (Transform part in sofa.GetComponentsInChildren<Transform>(true)) part.gameObject.layer = 31;
            }
            stage.gameObject.SetActive(false);
        }

        internal static void UpgradeGlass(Transform node)
        {
            Transform screen = node.Find("BroadcastScreen");
            Renderer old = screen.GetComponent<Renderer>();
            Material material = old.sharedMaterial;
            var renderer = old as SkinnedMeshRenderer;
            if (renderer == null)
            {
                Undo.DestroyObjectImmediate(old);
                Undo.DestroyObjectImmediate(screen.GetComponent<MeshFilter>());
                renderer = Undo.AddComponent<SkinnedMeshRenderer>(screen.gameObject);
            }
            screen.position = ScreenPosition;
            screen.localScale = new Vector3(0.58f, 0.43f, 1f);
            renderer.sharedMaterial = material;
            renderer.sharedMesh = GlassMesh();
            renderer.localBounds = new Bounds(Vector3.zero, new Vector3(1f, 1f, 0.2f));
            renderer.updateWhenOffscreen = true;
            renderer.enabled = false;
            node.Find("CrackedGlass").position = ScreenPosition + Vector3.back * 0.09f;
            var feed = new SerializedObject(node.GetComponent<TelevisionFeed>());
            feed.FindProperty("screen").objectReferenceValue = renderer;
            feed.ApplyModifiedProperties();
        }

        private static Mesh GlassMesh()
        {
            string path = Folder + "IR2_TelevisionBulgingGlass.asset";
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            var vertices = new Vector3[13 * 13];
            var delta = new Vector3[vertices.Length];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[12 * 12 * 6];
            for (int y = 0; y <= 12; y++)
            for (int x = 0; x <= 12; x++)
            {
                int i = y * 13 + x;
                uv[i] = new Vector2(x / 12f, y / 12f);
                float row = Mathf.Abs(uv[i].y - 0.5f);
                float halfWidth = row <= 0.42f ? 0.5f : 0.42f + Mathf.Sqrt(Mathf.Max(0f, 0.0064f - (row - 0.42f) * (row - 0.42f)));
                vertices[i] = new Vector3((uv[i].x - 0.5f) * halfWidth * 2f, uv[i].y - 0.5f, 0f);
                delta[i] = Vector3.back * (0.08f * (1f - Mathf.Pow(vertices[i].x * 2f, 2f)) * (1f - Mathf.Pow(vertices[i].y * 2f, 2f)));
            }
            int index = 0;
            for (int y = 0; y < 12; y++)
            for (int x = 0; x < 12; x++)
            {
                int a = y * 13 + x;
                foreach (int vertex in new[] { a, a + 13, a + 1, a + 1, a + 13, a + 14 }) triangles[index++] = vertex;
            }
            var mesh = new Mesh { name = "IR2_TelevisionBulgingGlass", vertices = vertices, uv = uv, triangles = triangles };
            mesh.RecalculateNormals();
            mesh.AddBlendShapeFrame("Pressure", 100f, delta, null, null);
            if (existing == null) { AssetDatabase.CreateAsset(mesh, path); return mesh; }
            EditorUtility.CopySerialized(mesh, existing);
            UnityEngine.Object.DestroyImmediate(mesh);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        private static GameObject Cracks(Transform parent)
        {
            GameObject root = Node(parent, "CrackedGlass");
            root.transform.position = ScreenPosition + Vector3.back * 0.03f;
            Material glass = Material("M_IR2_TelevisionCrack", new Color(0.6f, 0.67f, 0.62f));
            for (int i = 0; i < 7; i++)
            {
                LineRenderer line = Node(root.transform, "Fracture_" + i).AddComponent<LineRenderer>();
                float angle = i * Mathf.PI * 2f / 7f;
                Vector3 direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                line.useWorldSpace = false;
                line.positionCount = 4;
                line.SetPositions(new[] { Vector3.zero, direction * 0.035f + Vector3.left * 0.009f, direction * 0.07f, direction * 0.14f });
                line.widthMultiplier = 0.0012f;
                line.sharedMaterial = glass;
            }
            root.SetActive(false);
            return root;
        }

        internal static GameObject Node(Transform parent, string name)
        {
            var node = new GameObject(name);
            node.transform.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(node, "Add television anomaly");
            return node;
        }

        private static Material Material(string name, Color tint)
        {
            string path = Folder + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = name };
            material.SetColor("_BaseColor", tint);
            material.SetFloat("_Cull", 0f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        internal static GameObject Source(string name)
        {
            foreach (GameObject source in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                if (source.name == name) return source;
            throw new InvalidOperationException("IR2_TELEVISION_SOURCE_MISSING: " + name);
        }
    }
}
