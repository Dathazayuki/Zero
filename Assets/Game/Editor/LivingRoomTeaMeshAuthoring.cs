using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Zero.Editor
{
    internal static class LivingRoomTeaMeshAuthoring
    {
        internal static Transform[] Articulate(Transform teaSet)
        {
            Transform existing = teaSet.Find("IvoryPorcelain/IR2_ArticulatedTeaSet");
            if (existing != null) return new[] { existing.Find("Teapot"), existing.Find("DrinkingCup") };
            MeshFilter source = teaSet.Find("IvoryPorcelain").GetComponent<MeshFilter>();
            var root = new GameObject("IR2_ArticulatedTeaSet");
            root.transform.SetParent(source.transform, false);
            Undo.RegisterCreatedObjectUndo(root, "Articulate original Living Room tea set");
            Material material = source.GetComponent<Renderer>().sharedMaterial;
            Transform pot = Part(root.transform, source.sharedMesh, material, 0, Vector3.zero);
            Transform cup = Part(root.transform, source.sharedMesh, material, 1, new Vector3(0.22f, 0.014f, 0.2f));
            Part(root.transform, source.sharedMesh, material, 2, Vector3.zero);
            Undo.RecordObject(source.GetComponent<Renderer>(), "Use articulated tea meshes");
            source.GetComponent<Renderer>().enabled = false;
            return new[] { pot, cup };
        }

        private static Transform Part(Transform parent, Mesh source, Material material, int index, Vector3 pivot)
        {
            string[] names = { "Teapot", "DrinkingCup", "OtherCupsAndSaucers" };
            string path = LivingRoomAnomalyProps.Folder + "IR2_" + names[index] + ".asset";
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null)
            {
                mesh = Slice(source, index, pivot);
                mesh.name = names[index];
                AssetDatabase.CreateAsset(mesh, path);
            }
            var part = new GameObject(names[index], typeof(MeshFilter), typeof(MeshRenderer));
            part.transform.SetParent(parent, false);
            part.transform.localPosition = pivot;
            part.GetComponent<MeshFilter>().sharedMesh = mesh;
            part.GetComponent<MeshRenderer>().sharedMaterial = material;
            return part.transform;
        }

        private static int Category(Vector3 point)
        {
            if (point.x > 0.13f && point.z > 0.14f && point.y > 0.013f) return 1;
            if (Mathf.Abs(point.z) < 0.14f && (Mathf.Abs(point.x) < 0.13f || point.z < 0.1f)) return 0;
            return 2;
        }

        private static Mesh Slice(Mesh source, int category, Vector3 pivot)
        {
            Vector3[] sourceVertices = source.vertices;
            Vector3[] sourceNormals = source.normals;
            Vector2[] sourceUv = source.uv;
            int[] sourceTriangles = source.triangles;
            var triangles = new List<int>();
            var map = new Dictionary<int, int>();
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uv = new List<Vector2>();
            for (int i = 0; i < sourceTriangles.Length; i += 3)
            {
                Vector3 center = (sourceVertices[sourceTriangles[i]] + sourceVertices[sourceTriangles[i + 1]] + sourceVertices[sourceTriangles[i + 2]]) / 3f;
                if (Category(center) != category) continue;
                for (int corner = 0; corner < 3; corner++)
                {
                    int old = sourceTriangles[i + corner];
                    if (!map.TryGetValue(old, out int current))
                    {
                        current = vertices.Count;
                        map.Add(old, current);
                        vertices.Add(sourceVertices[old] - pivot);
                        normals.Add(sourceNormals.Length == sourceVertices.Length ? sourceNormals[old] : Vector3.up);
                        uv.Add(sourceUv.Length == sourceVertices.Length ? sourceUv[old] : Vector2.zero);
                    }
                    triangles.Add(current);
                }
            }
            if (triangles.Count == 0) throw new System.InvalidOperationException("LIVING_ROOM_TEA_PART_EMPTY: " + category);
            var mesh = new Mesh();
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
