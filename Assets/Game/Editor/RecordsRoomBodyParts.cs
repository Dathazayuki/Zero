using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Zero.Editor
{
    internal static class RecordsRoomBodyParts
    {
        internal static Mesh Bake(SkinnedMeshRenderer source, string name, int legSide)
        {
            string path = RecordsRoomHorrorAssets.Folder + "/IR3_Body_" + name + ".asset";
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) return existing;
            var baked = new Mesh();
            source.BakeMesh(baked);
            Vector3[] points = baked.vertices;
            for (int i = 0; i < points.Length; i++)
                points[i] = source.transform.root.InverseTransformPoint(source.transform.TransformPoint(points[i]));
            Mesh mesh = Extract(baked, points, legSide);
            mesh.name = "IR3_Body_" + name;
            Object.DestroyImmediate(baked);
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        private static Mesh Extract(Mesh source, Vector3[] points, int legSide)
        {
            var indices = new int[source.subMeshCount][];
            var remap = new Dictionary<int, int>();
            Vector2[] sourceUv = source.uv;
            for (int sub = 0; sub < indices.Length; sub++)
                indices[sub] = Triangles(source.GetTriangles(sub), points, legSide, remap);
            // Enumerate explicitly by mapped index; Dictionary iteration order is not a mesh contract.
            var ordered = new Vector3[remap.Count];
            var orderedUv = new Vector2[remap.Count];
            foreach (KeyValuePair<int, int> pair in remap)
            {
                ordered[pair.Value] = points[pair.Key];
                orderedUv[pair.Value] = sourceUv.Length == points.Length ? sourceUv[pair.Key] : Vector2.zero;
            }
            var mesh = new Mesh { indexFormat = IndexFormat.UInt32, vertices = ordered, uv = orderedUv };
            mesh.subMeshCount = indices.Length;
            for (int i = 0; i < indices.Length; i++) mesh.SetTriangles(indices[i], i);
            mesh.RecalculateBounds();
            Vector3 center = mesh.bounds.center;
            for (int i = 0; i < ordered.Length; i++) ordered[i] -= center;
            mesh.vertices = ordered;
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static int[] Triangles(int[] triangles, Vector3[] points, int side, Dictionary<int, int> remap)
        {
            var selected = new List<int>();
            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 center = (points[triangles[i]] + points[triangles[i + 1]] + points[triangles[i + 2]]) / 3f;
                if (side == 2 ? center.y <= 1.05f : side != 0 && (center.y > 1.05f || center.x * side < 0f)) continue;
                for (int corner = 0; corner < 3; corner++)
                {
                    int original = triangles[i + corner];
                    if (!remap.TryGetValue(original, out int index))
                    {
                        index = remap.Count;
                        remap.Add(original, index);
                    }
                    selected.Add(index);
                }
            }
            return selected.ToArray();
        }

        internal static void Place(Transform content, Renderer floor, Mesh mesh, Material[] materials, Quaternion rotation)
        {
            GameObject part = RecordsRoomHorrorAssets.Node(content, mesh.name);
            part.AddComponent<MeshFilter>().sharedMesh = mesh;
            Renderer renderer = part.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = materials;
            Fit(renderer, floor, rotation);
        }

        internal static void Fit(Renderer renderer, Renderer floor, Quaternion rotation)
        {
            Transform part = renderer.transform;
            part.localScale = Vector3.one;
            part.rotation = rotation;
            Bounds available = floor.bounds;
            Vector3 size = renderer.bounds.size;
            float height = available.center.y > 1.4f ? 0.38f : 0.26f;
            float scale = Mathf.Min(1f, Mathf.Min((available.size.x - 0.05f) / size.x,
                Mathf.Min(height / size.y, (available.size.z - 0.08f) / size.z)));
            part.localScale = Vector3.one * scale;
            part.position = new Vector3(available.center.x,
                available.max.y + renderer.bounds.extents.y + 0.009f, available.center.z);
        }
    }
}
