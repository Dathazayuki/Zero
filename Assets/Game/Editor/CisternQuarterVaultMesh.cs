using System.Collections.Generic;
using UnityEngine;

namespace Zero.Editor
{
    /// <summary>A closed vault quadrant; four rotated copies form a continuous curved four-way pier.</summary>
    public static class CisternQuarterVaultMesh
    {
        private const int Divisions = 32;
        private const float HalfSpan = 3f;
        private const float FootHalfWidth = 0.52f;
        private const float FootHeight = -0.35f;
        private const float CrownHeight = 3.5f;
        private const float RoofTop = 3.8f;

        public static Mesh Create()
        {
            MeshBuilder draft = new MeshBuilder();
            AddFoot(draft);
            List<Vector3> rim = new List<Vector3> { new Vector3(0, RoofTop, 0) };
            AddCurvedSurfaces(draft, rim);
            AddOuterFaces(draft);
            AddRoof(draft, rim);
            return draft.Build();
        }

        private static Vector3 CurvePoint(int step)
        {
            float angle = Mathf.PI * 0.5f * step / Divisions;
            float distance = FootHalfWidth + (HalfSpan - FootHalfWidth) * (1f - Mathf.Cos(angle));
            float height = FootHeight + (CrownHeight - FootHeight) * Mathf.Sin(angle);
            return new Vector3(distance, height, 0);
        }

        private static Vector3 CurveNormal(int step)
        {
            float angle = Mathf.PI * 0.5f * step / Divisions;
            return new Vector3((CrownHeight - FootHeight) * Mathf.Cos(angle),
                -(HalfSpan - FootHalfWidth) * Mathf.Sin(angle), 0).normalized;
        }

        private static void AddFoot(MeshBuilder draft)
        {
            Vector3 a = new Vector3(0, FootHeight, 0);
            Vector3 b = new Vector3(FootHalfWidth, FootHeight, 0);
            Vector3 c = new Vector3(FootHalfWidth, FootHeight, FootHalfWidth);
            Vector3 d = new Vector3(0, FootHeight, FootHalfWidth);
            draft.AddQuad(a, b, c, d, Vector3.down);
            draft.AddQuad(a, b, AtTop(b), AtTop(a), Vector3.back);
            draft.AddQuad(a, d, AtTop(d), AtTop(a), Vector3.left);
        }

        private static Vector3 AtTop(Vector3 point)
        {
            point.y = RoofTop;
            return point;
        }

        private static Vector3 SwapAxes(Vector3 point)
        {
            return new Vector3(point.z, point.y, point.x);
        }

        private static void AddCurvedSurfaces(MeshBuilder draft, List<Vector3> rim)
        {
            rim.Add(AtTop(CurvePoint(0)));
            for (int step = 0; step < Divisions; step++)
            {
                AddCurveStrip(draft, step, false);
                AddCurveStrip(draft, step, true);
                Vector3 a = CurvePoint(step);
                Vector3 b = CurvePoint(step + 1);
                draft.AddQuad(a, b, AtTop(b), AtTop(a), Vector3.back);
                draft.AddQuad(SwapAxes(a), SwapAxes(b), AtTop(SwapAxes(b)),
                    AtTop(SwapAxes(a)), Vector3.left);
                rim.Add(AtTop(b));
            }
        }

        private static void AddCurveStrip(MeshBuilder draft, int step, bool swap)
        {
            Vector3 a = CurvePoint(step);
            Vector3 b = CurvePoint(step + 1);
            Vector3[] points = { a, a + Vector3.forward * a.x, b + Vector3.forward * b.x, b };
            Vector3[] normals = { CurveNormal(step), CurveNormal(step),
                CurveNormal(step + 1), CurveNormal(step + 1) };
            if (swap)
            {
                for (int i = 0; i < points.Length; i++)
                {
                    points[i] = SwapAxes(points[i]);
                    normals[i] = SwapAxes(normals[i]);
                }
            }
            draft.AddQuad(points, normals);
        }

        private static void AddOuterFaces(MeshBuilder draft)
        {
            Vector3 a = new Vector3(HalfSpan, CrownHeight, 0);
            Vector3 b = new Vector3(HalfSpan, CrownHeight, HalfSpan);
            Vector3 c = new Vector3(0, CrownHeight, HalfSpan);
            draft.AddQuad(a, b, AtTop(b), AtTop(a), Vector3.right);
            draft.AddQuad(b, c, AtTop(c), AtTop(b), Vector3.forward);
        }

        private static void AddRoof(MeshBuilder draft, List<Vector3> rim)
        {
            rim.Add(new Vector3(HalfSpan, RoofTop, HalfSpan));
            for (int step = Divisions; step >= 0; step--)
                rim.Add(AtTop(SwapAxes(CurvePoint(step))));
            Vector3 center = new Vector3(HalfSpan * 0.5f, RoofTop, HalfSpan * 0.5f);
            for (int i = 0; i < rim.Count; i++)
                draft.AddTriangle(center, rim[i], rim[(i + 1) % rim.Count], Vector3.up);
        }

        private sealed class MeshBuilder
        {
            private readonly List<Vector3> vertices = new List<Vector3>();
            private readonly List<Vector3> normals = new List<Vector3>();
            private readonly List<int> triangles = new List<int>();

            public void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal)
            {
                AddQuad(new[] { a, b, c, d }, new[] { normal, normal, normal, normal });
            }

            public void AddQuad(Vector3[] points, Vector3[] directions)
            {
                int first = vertices.Count;
                vertices.AddRange(points);
                normals.AddRange(directions);
                bool forward = Vector3.Dot(Vector3.Cross(points[1] - points[0], points[2] - points[0]),
                    directions[0] + directions[2]) > 0;
                AddIndices(first, first + 1, first + 2, forward);
                AddIndices(first, first + 2, first + 3, forward);
            }

            public void AddTriangle(Vector3 a, Vector3 b, Vector3 c, Vector3 normal)
            {
                int first = vertices.Count;
                vertices.AddRange(new[] { a, b, c });
                normals.AddRange(new[] { normal, normal, normal });
                AddIndices(first, first + 1, first + 2,
                    Vector3.Dot(Vector3.Cross(b - a, c - a), normal) > 0);
            }

            private void AddIndices(int a, int b, int c, bool forward)
            {
                triangles.Add(a);
                triangles.Add(forward ? b : c);
                triangles.Add(forward ? c : b);
            }

            public Mesh Build()
            {
                Mesh mesh = new Mesh { name = "Cistern_QuarterVault" };
                mesh.SetVertices(vertices);
                mesh.SetNormals(normals);
                mesh.SetTriangles(triangles, 0);
                mesh.RecalculateBounds();
                return mesh;
            }
        }
    }
}
