using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Zero.Editor;

namespace Zero.Tests.EditMode
{
    public sealed class CisternQuarterVaultMeshTests
    {
        [Test]
        public void RepeatedGenerationNeverDependsOnDeletedSceneObjects()
        {
            Mesh first = CisternQuarterVaultMesh.Create();
            Mesh second = CisternQuarterVaultMesh.Create();
            try
            {
                Assert.That(first.vertexCount, Is.GreaterThan(0));
                Assert.That(second.vertices, Is.EqualTo(first.vertices));
                Assert.That(second.triangles, Is.EqualTo(first.triangles));
                Assert.That(first.bounds.size.x, Is.EqualTo(3f).Within(0.0001f));
                Assert.That(first.bounds.size.z, Is.EqualTo(3f).Within(0.0001f));
                Assert.That(first.bounds.max.y, Is.EqualTo(3.8f).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(first);
                Object.DestroyImmediate(second);
            }
        }

        [Test]
        public void QuarterIsClosedAndEveryTriangleFacesItsAuthoredNormal()
        {
            Mesh mesh = CisternQuarterVaultMesh.Create();
            try
            {
                Vector3[] vertices = mesh.vertices;
                Vector3[] normals = mesh.normals;
                int[] triangles = mesh.triangles;
                Dictionary<(Vector3Int, Vector3Int), int> edges = new Dictionary<(Vector3Int, Vector3Int), int>();
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    int a = triangles[i]; int b = triangles[i + 1]; int c = triangles[i + 2];
                    Vector3 face = Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]);
                    Assert.That(face.sqrMagnitude, Is.GreaterThan(0.00000001f), $"Degenerate face {i / 3}");
                    Assert.That(Vector3.Dot(face, normals[a] + normals[b] + normals[c]), Is.GreaterThan(0));
                    RecordEdge(edges, vertices[a], vertices[b]);
                    RecordEdge(edges, vertices[b], vertices[c]);
                    RecordEdge(edges, vertices[c], vertices[a]);
                }
                foreach (KeyValuePair<(Vector3Int, Vector3Int), int> edge in edges)
                    Assert.That(edge.Value, Is.EqualTo(2), $"Open or non-manifold edge {edge.Key}");
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }

        private static void RecordEdge(Dictionary<(Vector3Int, Vector3Int), int> edges, Vector3 a, Vector3 b)
        {
            Vector3Int first = Vector3Int.RoundToInt(a * 10000);
            Vector3Int second = Vector3Int.RoundToInt(b * 10000);
            bool reverse = first.x > second.x || (first.x == second.x && first.y > second.y) ||
                (first.x == second.x && first.y == second.y && first.z > second.z);
            (Vector3Int, Vector3Int) key = reverse ? (second, first) : (first, second);
            edges.TryGetValue(key, out int count);
            edges[key] = count + 1;
        }
    }
}
