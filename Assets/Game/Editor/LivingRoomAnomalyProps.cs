using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Zero.Editor
{
    public static class LivingRoomAnomalyProps
    {
        public const string Folder = "Assets/Game/Prefabs/InfinityRoom2Demo/Anomalies/";

        public static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder(Folder.TrimEnd('/')))
                AssetDatabase.CreateFolder("Assets/Game/Prefabs/InfinityRoom2Demo", "Anomalies");
        }

        public static Material Material(string name, Color color)
        {
            string path = Folder + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            material.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        public static GameObject Part(string name, Transform parent, Vector3 position, Vector3 size, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            UnityEngine.Object.DestroyImmediate(part.GetComponent<Collider>());
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = size;
            part.GetComponent<Renderer>().sharedMaterial = material;
            return part;
        }

        private static GameObject Prefab(string name, Action<GameObject> build)
        {
            string path = Folder + name + ".prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null) return prefab;
            var temporary = new GameObject(name);
            try { build(temporary); return PrefabUtility.SaveAsPrefabAsset(temporary, path); }
            finally { UnityEngine.Object.DestroyImmediate(temporary); }
        }

        public static GameObject Cup(Transform parent)
        {
            GameObject prefab = Prefab("PF_IR2_ExtraTeaCup", root =>
            {
                Material ivory = Material("M_IR2_AnomalyPorcelain", new Color(0.85f, 0.81f, 0.67f));
                MeshPart("Cup", root.transform, Lathe(), ivory);
                MeshPart("Handle", root.transform, Handle(), ivory);
                GameObject saucer = Part("Saucer", root.transform, Vector3.zero, new Vector3(0.13f, 0.008f, 0.13f), ivory);
                GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                saucer.GetComponent<MeshFilter>().sharedMesh = cylinder.GetComponent<MeshFilter>().sharedMesh;
                UnityEngine.Object.DestroyImmediate(cylinder);
                saucer.transform.localScale = new Vector3(0.13f, 0.003f, 0.13f);
                GameObject tea = Part("Tea", root.transform, new Vector3(0f, 0.07f, 0f), new Vector3(0.073f, 0.001f, 0.073f),
                    Material("M_IR2_AnomalyTea", new Color(0.19f, 0.08f, 0.02f)));
                tea.GetComponent<MeshFilter>().sharedMesh = saucer.GetComponent<MeshFilter>().sharedMesh;
            });
            return (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        }

        private static void MeshPart(string name, Transform parent, Mesh mesh, Material material)
        {
            var part = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            part.transform.SetParent(parent, false);
            part.GetComponent<MeshFilter>().sharedMesh = mesh;
            part.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static Mesh Lathe()
        {
            string path = Folder + "TeaCupMesh.asset";
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) return existing;
            Vector2[] profile = { new Vector2(0f, 0f), new Vector2(0.03f, 0f), new Vector2(0.036f, 0.01f),
                new Vector2(0.045f, 0.078f), new Vector2(0.044f, 0.083f), new Vector2(0.039f, 0.08f),
                new Vector2(0.029f, 0.01f), new Vector2(0f, 0.01f) };
            const int count = 48;
            var vertices = new Vector3[count * profile.Length];
            var triangles = new List<int>();
            for (int ring = 0; ring < profile.Length; ring++)
            {
                for (int i = 0; i < count; i++)
                {
                    float angle = i * Mathf.PI * 2f / count;
                    vertices[ring * count + i] = new Vector3(Mathf.Cos(angle) * profile[ring].x, profile[ring].y, Mathf.Sin(angle) * profile[ring].x);
                    if (ring == profile.Length - 1) continue;
                    int a = ring * count + i, b = ring * count + (i + 1) % count, c = a + count, d = b + count;
                    triangles.AddRange(new[] { a, c, b, b, c, d });
                }
            }
            return SaveMesh(path, vertices, triangles.ToArray());
        }

        private static Mesh Handle()
        {
            string path = Folder + "TeaCupHandle.asset";
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) return existing;
            var vertices = new Vector3[32 * 8];
            var triangles = new List<int>();
            for (int i = 0; i < 32; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    float angle = i * Mathf.PI / 16f, tube = j * Mathf.PI / 4f;
                    float radius = 0.024f + Mathf.Cos(tube) * 0.005f;
                    vertices[i * 8 + j] = new Vector3(0.044f + Mathf.Cos(angle) * radius, 0.046f + Mathf.Sin(angle) * radius, Mathf.Sin(tube) * 0.005f);
                    int a = i * 8 + j, b = (i + 1) % 32 * 8 + j, c = i * 8 + (j + 1) % 8, d = (i + 1) % 32 * 8 + (j + 1) % 8;
                    triangles.AddRange(new[] { a, b, c, c, b, d });
                }
            }
            return SaveMesh(path, vertices, triangles.ToArray());
        }

        private static Mesh SaveMesh(string path, Vector3[] vertices, int[] triangles)
        {
            var mesh = new Mesh { name = System.IO.Path.GetFileNameWithoutExtension(path), vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        public static GameObject Book(Transform parent, bool open)
        {
            string name = open ? "PF_IR2_OpenBook" : "PF_IR2_ReversedBook";
            GameObject prefab = Prefab(name, root =>
            {
                Material paper = Material("M_IR2_AnomalyPages", new Color(0.75f, 0.7f, 0.53f));
                Material cover = Material("M_IR2_AnomalyBookLeather", new Color(0.12f, 0.045f, 0.025f));
                if (open)
                {
                    Part("Cover", root.transform, Vector3.zero, new Vector3(0.28f, 0.008f, 0.2f), cover);
                    Part("LeftPages", root.transform, new Vector3(-0.065f, 0.014f, 0f), new Vector3(0.125f, 0.012f, 0.185f), paper).transform.localRotation = Quaternion.Euler(0f, 0f, 8f);
                    Part("RightPages", root.transform, new Vector3(0.065f, 0.014f, 0f), new Vector3(0.125f, 0.012f, 0.185f), paper).transform.localRotation = Quaternion.Euler(0f, 0f, -8f);
                    return;
                }
                Part("PageBlock", root.transform, new Vector3(0f, 0.125f, 0f), new Vector3(0.09f, 0.23f, 0.18f), paper);
                Part("CoverLeft", root.transform, new Vector3(-0.05f, 0.125f, 0f), new Vector3(0.008f, 0.25f, 0.21f), cover);
                Part("CoverRight", root.transform, new Vector3(0.05f, 0.125f, 0f), new Vector3(0.008f, 0.25f, 0.21f), cover);
                Part("SpineAtBack", root.transform, new Vector3(0f, 0.125f, -0.10f), new Vector3(0.105f, 0.25f, 0.01f), cover);
            });
            return (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        }

        public static void DetailPages()
        {
            string path = Folder + "BookPageLines.asset";
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null)
            {
                texture = new Texture2D(32, 128, TextureFormat.RGB24, false) { name = "BookPageLines", wrapMode = TextureWrapMode.Repeat };
                for (int y = 0; y < 128; y++)
                    for (int x = 0; x < 32; x++)
                        texture.SetPixel(x, y, y % 4 == 0 ? new Color(0.62f, 0.57f, 0.44f) : new Color(0.92f, 0.89f, 0.78f));
                texture.Apply();
                AssetDatabase.CreateAsset(texture, path);
            }
            Material paper = Material("M_IR2_AnomalyPages", new Color(0.75f, 0.7f, 0.53f));
            paper.SetTexture("_BaseMap", texture);
            EditorUtility.SetDirty(paper);
            AssetDatabase.SaveAssetIfDirty(paper);
        }

        public static void Steam(Transform parent)
        {
            var go = new GameObject("TeaSteam", typeof(ParticleSystem));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.up * 0.08f;
            ParticleSystem system = go.GetComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.playOnAwake = false;
            main.startLifetime = 2f;
            main.startSpeed = 0.06f;
            main.startSize = 0.035f;
            main.startColor = new Color(0.85f, 0.85f, 0.8f, 0.18f);
            main.maxParticles = 16;
            var emission = system.emission;
            emission.rateOverTime = 5f;
            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.radius = 0.012f;
            shape.angle = 6f;
            shape.rotation = new Vector3(-90f, 0f, 0f);
            system.GetComponent<ParticleSystemRenderer>().sharedMaterial = SteamMaterial();
        }

        private static Material SteamMaterial()
        {
            string path = Folder + "M_IR2_TeaSteam.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false) { name = "TeaSteamAlpha", wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Pow(Mathf.Clamp01(1f - Vector2.Distance(new Vector2(x, y), Vector2.one * 15.5f) / 15.5f), 2f)));
            texture.Apply();
            AssetDatabase.CreateAsset(texture, Folder + "TeaSteamAlpha.asset");
            material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = "M_IR2_TeaSteam" };
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetTexture("_BaseMap", texture);
            material.renderQueue = 3000;
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        public static GameObject Shadow(Transform parent)
        {
            string path = Folder + "SeatedReaderShadow.asset";
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null)
            {
                var vertices = new List<Vector3>();
                var triangles = new List<int>();
                Ellipse(vertices, triangles, new Vector2(0f, 1.60f), new Vector2(0.14f, 0.18f));
                Ellipse(vertices, triangles, new Vector2(-0.05f, 1.17f), new Vector2(0.22f, 0.34f));
                Ellipse(vertices, triangles, new Vector2(0.20f, 0.81f), new Vector2(0.38f, 0.13f));
                Ellipse(vertices, triangles, new Vector2(0.51f, 0.44f), new Vector2(0.085f, 0.37f));
                Ellipse(vertices, triangles, new Vector2(0.59f, 0.09f), new Vector2(0.17f, 0.06f));
                Ellipse(vertices, triangles, new Vector2(0.23f, 1.20f), new Vector2(0.30f, 0.065f));
                Ellipse(vertices, triangles, new Vector2(0.44f, 1.30f), new Vector2(0.06f, 0.14f));
                mesh = SaveMesh(path, vertices.ToArray(), triangles.ToArray());
            }
            var root = new GameObject("SeatedReaderShadow");
            root.transform.SetParent(parent, false);
            Material dark = Material("M_IR2_ReaderShadow", new Color(0.045f, 0.04f, 0.03f));
            MeshPart("ReaderSilhouette", root.transform, mesh, dark);
            Part("OpenBookShadow", root.transform, new Vector3(0.51f, 1.40f, 0f), new Vector3(0.30f, 0.04f, 0.001f), dark).transform.localRotation = Quaternion.Euler(0f, 0f, -18f);
            Part("ChairBackShadow", root.transform, new Vector3(-0.25f, 0.87f, 0f), new Vector3(0.04f, 0.75f, 0.001f), dark);
            Part("ChairLegShadow", root.transform, new Vector3(-0.17f, 0.4f, 0f), new Vector3(0.045f, 0.8f, 0.001f), dark);
            return root;
        }

        private static void Ellipse(List<Vector3> vertices, List<int> triangles, Vector2 center, Vector2 radius)
        {
            int start = vertices.Count;
            vertices.Add(new Vector3(center.x, center.y, 0f));
            for (int i = 0; i < 32; i++)
            {
                float angle = i * Mathf.PI / 16f;
                vertices.Add(new Vector3(center.x + Mathf.Cos(angle) * radius.x, center.y + Mathf.Sin(angle) * radius.y, 0f));
                triangles.AddRange(new[] { start, start + 1 + (i + 1) % 32, start + 1 + i });
            }
        }
    }
}
