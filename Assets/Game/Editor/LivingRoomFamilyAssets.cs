using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Zero.Editor
{
    internal static class LivingRoomFamilyAssets
    {
        private const string Folder = "Assets/Game/Prefabs/InfinityRoom2Demo/Anomalies/";

        internal static SkinnedMeshRenderer Deformed(Renderer source, Transform parent, bool sofa)
        {
            Mesh original = source.GetComponent<MeshFilter>().sharedMesh;
            Mesh mesh = UnityEngine.Object.Instantiate(original);
            mesh.name = sofa ? "IR2_OccupiedSofa" : "IR2_BulgingCurtain";
            if (sofa) { SubdivideSeat(mesh); SubdivideSeat(mesh); }
            Vector3[] vertices = mesh.vertices;
            var deltas = new Vector3[vertices.Length];
            Bounds bounds = mesh.bounds;
            for (int i = 0; i < vertices.Length; i++)
                deltas[i] = sofa ? SofaDelta(vertices[i], bounds) : CurtainDelta(vertices[i]);
            AddShape(mesh, deltas);
            mesh = Save(mesh.name, mesh);
            Transform existing = parent.Find(mesh.name);
            GameObject item = existing != null ? existing.gameObject : new GameObject(mesh.name, typeof(SkinnedMeshRenderer));
            item.transform.SetParent(parent, false);
            item.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
            item.transform.localScale = source.transform.lossyScale;
            var renderer = item.GetComponent<SkinnedMeshRenderer>();
            renderer.sharedMesh = mesh;
            renderer.sharedMaterials = source.sharedMaterials;
            renderer.localBounds = bounds;
            renderer.updateWhenOffscreen = true;
            item.SetActive(false);
            return renderer;
        }

        private static Vector3 SofaDelta(Vector3 point, Bounds bounds)
        {
            float x = (point.x - bounds.center.x) / 0.38f;
            float z = (point.z - 0.05f) / 0.36f;
            if (point.y > bounds.min.y + 0.22f) return Vector3.zero;
            float top = Mathf.InverseLerp(bounds.min.y, bounds.min.y + 0.18f, point.y);
            float depression = Mathf.Exp(-(x * x + z * z) * 1.5f) * Mathf.SmoothStep(0f, 1f, top);
            return Vector3.down * (0.13f * depression);
        }

        private static Vector3 CurtainDelta(Vector3 point)
        {
            float x = (point.x + 0.25f) / 0.36f;
            float y = (point.y - 0.95f) / 0.7f;
            return Vector3.back * (0.24f * Mathf.Exp(-(x * x + y * y) * 1.5f));
        }

        private static void AddShape(Mesh mesh, Vector3[] deltas)
        {
            Mesh target = UnityEngine.Object.Instantiate(mesh);
            try
            {
                Vector3[] vertices = target.vertices;
                Vector3[] normalDeltas = mesh.normals;
                for (int i = 0; i < vertices.Length; i++) vertices[i] += deltas[i];
                target.vertices = vertices;
                target.RecalculateNormals();
                Vector3[] normals = target.normals;
                for (int i = 0; i < normals.Length; i++) normalDeltas[i] = normals[i] - normalDeltas[i];
                mesh.AddBlendShapeFrame("Presence", 100f, deltas, normalDeltas, null);
            }
            finally { UnityEngine.Object.DestroyImmediate(target); }
        }

        private static void SubdivideSeat(Mesh mesh)
        {
            var points = new List<Vector3>(mesh.vertices);
            var normals = new List<Vector3>(mesh.normals);
            var uv = new List<Vector2>(mesh.uv);
            var groups = new List<int[]>();
            for (int sub = 0; sub < mesh.subMeshCount; sub++)
            {
                int[] source = mesh.GetTriangles(sub);
                var triangles = new List<int>();
                for (int i = 0; i < source.Length; i += 3)
                {
                    int a = source[i], b = source[i + 1], c = source[i + 2];
                    bool top = points[a].y < 0.61f && points[b].y < 0.61f && points[c].y < 0.61f;
                    if (!top || Mathf.Max((points[a] - points[b]).sqrMagnitude, (points[b] - points[c]).sqrMagnitude) < 0.04f)
                    { triangles.AddRange(new[] { a, b, c }); continue; }
                    int ab = Midpoint(a, b, points, normals, uv), bc = Midpoint(b, c, points, normals, uv), ca = Midpoint(c, a, points, normals, uv);
                    triangles.AddRange(new[] { a, ab, ca, ab, b, bc, ca, bc, c, ab, bc, ca });
                }
                groups.Add(triangles.ToArray());
            }
            mesh.SetVertices(points); mesh.SetNormals(normals); mesh.SetUVs(0, uv);
            for (int sub = 0; sub < groups.Count; sub++) mesh.SetTriangles(groups[sub], sub);
        }

        private static int Midpoint(int a, int b, List<Vector3> points, List<Vector3> normals, List<Vector2> uv)
        {
            int index = points.Count;
            points.Add((points[a] + points[b]) * 0.5f);
            normals.Add((normals[a] + normals[b]).normalized);
            uv.Add((uv[a] + uv[b]) * 0.5f);
            return index;
        }

        internal static GameObject ChildPart(GameObject source, Transform parent, bool feet)
        {
            Animator rig = source.GetComponent<Animator>();
            if (rig == null || !rig.isHuman) throw new InvalidOperationException("IR2_CHILD_HUMANOID_REQUIRED");
            Vector3 origin = rig.GetBoneTransform(feet ? HumanBodyBones.LeftFoot : HumanBodyBones.Head).position;
            float cutoff = feet ? origin.y + 0.12f : rig.GetBoneTransform(HumanBodyBones.Neck).position.y - 0.025f;
            var root = new GameObject(feet ? "ChildFeet" : "ChildFace");
            root.transform.SetParent(parent, false);
            foreach (SkinnedMeshRenderer skin in source.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                Extract(skin, root.transform, origin, cutoff, feet);
            return root;
        }

        private static void Extract(SkinnedMeshRenderer skin, Transform root, Vector3 origin, float cutoff, bool feet)
        {
            var baked = new Mesh();
            try
            {
                skin.BakeMesh(baked);
                Vector3[] vertices = baked.vertices;
                Vector3[] normals = baked.normals;
                for (int i = 0; i < vertices.Length; i++)
                {
                    vertices[i] = skin.transform.TransformPoint(vertices[i]) - origin;
                    if (normals.Length > i) normals[i] = skin.transform.TransformDirection(normals[i]);
                }
                baked.vertices = vertices;
                baked.normals = normals;
                Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
                for (int sub = 0; sub < baked.subMeshCount; sub++)
                {
                    int[] indices = baked.GetTriangles(sub);
                    var kept = new List<int>();
                    Filter(indices, vertices, kept, cutoff - origin.y, feet);
                    baked.SetTriangles(kept, sub);
                    foreach (int index in kept) bounds.Encapsulate(vertices[index]);
                }
                baked.bounds = bounds;
                Mesh asset = Save((feet ? "IR2_ChildFeet_" : "IR2_ChildFace_") + skin.name, UnityEngine.Object.Instantiate(baked));
                var item = new GameObject(skin.name, typeof(MeshFilter), typeof(MeshRenderer));
                item.transform.SetParent(root, false);
                item.GetComponent<MeshFilter>().sharedMesh = asset;
                item.GetComponent<MeshRenderer>().sharedMaterials = skin.sharedMaterials;
            }
            finally { UnityEngine.Object.DestroyImmediate(baked); }
        }

        private static void Filter(int[] triangles, Vector3[] vertices, List<int> kept, float cutoff, bool feet)
        {
            for (int i = 0; i < triangles.Length; i += 3)
            {
                float average = (vertices[triangles[i]].y + vertices[triangles[i + 1]].y + vertices[triangles[i + 2]].y) / 3f;
                if (feet ? average > cutoff : average < cutoff) continue;
                kept.Add(triangles[i]);
                kept.Add(triangles[i + 1]);
                kept.Add(triangles[i + 2]);
            }
        }

        internal static void RestoreOriginalMaterials(GameObject source, Transform face)
        {
            foreach (SkinnedMeshRenderer skin in source.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                Transform part = face.Find(skin.name);
                if (part == null) continue;
                Undo.RecordObject(part.GetComponent<MeshRenderer>(), "Restore original child texture");
                part.GetComponent<MeshRenderer>().sharedMaterials = skin.sharedMaterials;
            }
        }

        private static Mesh Save(string name, Mesh mesh)
        {
            string path = Folder + name + ".asset";
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null) { AssetDatabase.CreateAsset(mesh, path); return mesh; }
            EditorUtility.CopySerialized(mesh, existing);
            UnityEngine.Object.DestroyImmediate(mesh);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        internal static Animator SeatedFather(GameObject source, Transform parent, Transform chair)
        {
            GameObject item = UnityEngine.Object.Instantiate(source, parent);
            item.name = "SeatedFather";
            item.SetActive(true);
            Animator rig = item.GetComponent<Animator>();
            rig.applyRootMotion = false;
            rig.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            rig.Rebind();
            rig.Play("Sit", 0, 0.45f);
            rig.Update(0f);
            Transform hips = rig.GetBoneTransform(HumanBodyBones.Hips);
            Vector3 knee = (rig.GetBoneTransform(HumanBodyBones.LeftLowerLeg).position + rig.GetBoneTransform(HumanBodyBones.RightLowerLeg).position) * 0.5f;
            Vector3 forward = Vector3.ProjectOnPlane(knee - hips.position, Vector3.up).normalized;
            item.transform.rotation = Quaternion.FromToRotation(forward, chair.forward) * item.transform.rotation;
            Vector3 target = SeatPoint(chair);
            item.transform.position += target - hips.position;
            item.SetActive(false);
            return rig;
        }

        internal static Vector3 SeatPoint(Transform chair)
        {
            Transform cushions = chair.Find("SeatCushions");
            Bounds bounds = cushions.GetComponent<MeshFilter>().sharedMesh.bounds;
            // This mesh contains the backrest too; max.y is not the seat height.
            return cushions.TransformPoint(new Vector3(bounds.center.x, bounds.min.y + 0.30f, 0.05f));
        }

        internal static AudioClip ChairScrape()
        {
            string path = Folder + "IR2_ChairScrape.wav";
            AudioClip existing = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (existing != null) return existing;
            AudioClip clip = Zero.AnomalyPrototype.Flow.OfficeAudioController.CreateChairScrape();
            try
            {
                var samples = new float[clip.samples * clip.channels];
                clip.GetData(samples, 0);
                using (var stream = System.IO.File.Create(path))
                using (var writer = new System.IO.BinaryWriter(stream))
                {
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + samples.Length * 2);
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16);
                    writer.Write((short)1); writer.Write((short)clip.channels); writer.Write(clip.frequency);
                    writer.Write(clip.frequency * clip.channels * 2); writer.Write((short)(clip.channels * 2)); writer.Write((short)16);
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(samples.Length * 2);
                    foreach (float sample in samples) writer.Write((short)(Mathf.Clamp(sample, -1f, 1f) * short.MaxValue));
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(clip); }
            AssetDatabase.ImportAsset(path);
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
    }
}
