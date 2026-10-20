using UnityEditor;
using UnityEngine;

namespace Zero.Editor
{
    internal static class RecordsRoomMarkAssets
    {
        internal static GameObject Quad(Transform parent, string name, Material material, Vector2 size)
        {
            GameObject node = GameObject.CreatePrimitive(PrimitiveType.Quad);
            node.name = name;
            node.transform.SetParent(parent, false);
            node.transform.localScale = new Vector3(size.x, size.y, 1f);
            Object.DestroyImmediate(node.GetComponent<Collider>());
            var renderer = node.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Undo.RegisterCreatedObjectUndo(node, "Add archive surface mark");
            return node;
        }

        internal static Material Ink(int glyph)
        {
            string name = glyph < 0 ? "BlackHandprint" : "Blood_" + "RUN"[glyph];
            string path = RecordsRoomHorrorAssets.Folder + "/M_IR3_" + name + ".mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = name };
            material.SetTexture("_BaseMap", Texture(name, glyph));
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_AlphaClip", 1f);
            material.SetFloat("_Cutoff", 0.15f);
            material.EnableKeyword("_ALPHATEST_ON");
            material.renderQueue = 2450;
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Texture2D Texture(string name, int glyph)
        {
            string path = RecordsRoomHorrorAssets.Folder + "/T_IR3_" + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) return existing;
            const int size = 512;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, true) { name = name, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++) pixels[y * size + x] = Pixel(x, y, glyph);
            texture.SetPixels32(pixels);
            texture.Apply(true, false);
            AssetDatabase.CreateAsset(texture, path);
            return texture;
        }

        private static Color32 Pixel(int x, int y, int glyph)
        {
            Vector2 p = new Vector2(x / 511f, y / 511f);
            float grain = Mathf.Repeat(Mathf.Sin(x * 12.9898f + y * 78.233f) * 43758.5453f, 1f);
            float mask = glyph < 0 ? Hand(p) : Letter(p, glyph);
            float edge = glyph < 0 ? 1f : 0.055f;
            float alpha = mask < edge && grain > 0.09f ? 1f : 0f;
            if (glyph < 0) return new Color32(9, 8, 7, (byte)(alpha * 235f));
            byte red = (byte)Mathf.Lerp(63f, 112f, grain);
            return new Color32(red, (byte)(red * 0.035f), (byte)(red * 0.045f), (byte)(alpha * 255f));
        }

        private static float Hand(Vector2 p)
        {
            float palm = Ellipse(p, new Vector2(0.50f, 0.36f), new Vector2(0.23f, 0.24f));
            float wrist = Ellipse(p, new Vector2(0.52f, 0.12f), new Vector2(0.13f, 0.15f));
            float fingers = Mathf.Min(Finger(p, new Vector2(0.33f, 0.47f), new Vector2(0.24f, 0.78f), 0.052f),
                Finger(p, new Vector2(0.44f, 0.50f), new Vector2(0.42f, 0.94f), 0.055f));
            fingers = Mathf.Min(fingers, Finger(p, new Vector2(0.56f, 0.50f), new Vector2(0.59f, 0.90f), 0.052f));
            fingers = Mathf.Min(fingers, Finger(p, new Vector2(0.66f, 0.46f), new Vector2(0.75f, 0.77f), 0.046f));
            fingers = Mathf.Min(fingers, Finger(p, new Vector2(0.32f, 0.31f), new Vector2(0.12f, 0.49f), 0.065f));
            float shape = Mathf.Min(Mathf.Min(palm, wrist), fingers);
            float crease = Mathf.Abs(p.y - (0.29f + 0.18f * Mathf.Sin(p.x * 4.5f)));
            if (p.x > 0.30f && p.x < 0.68f && crease < 0.008f) return 2f;
            return shape;
        }

        private static float Ellipse(Vector2 p, Vector2 center, Vector2 radius)
        {
            Vector2 delta = (p - center) / radius;
            return delta.sqrMagnitude;
        }

        private static float Finger(Vector2 p, Vector2 a, Vector2 b, float radius) => Line(p, a, b) / radius;

        private static float Letter(Vector2 p, int glyph)
        {
            p.x += Mathf.Sin(p.y * 41f) * 0.007f;
            float distance;
            if (glyph == 0) distance = R(p);
            else if (glyph == 1) distance = U(p);
            else distance = N(p);
            float drip = Line(p, new Vector2(0.25f, 0.24f), new Vector2(0.26f, 0.08f)) * 4f;
            return Mathf.Min(distance, drip);
        }

        private static float R(Vector2 p)
        {
            float d = Line(p, new Vector2(0.24f, 0.19f), new Vector2(0.25f, 0.83f));
            d = Mathf.Min(d, Line(p, new Vector2(0.25f, 0.83f), new Vector2(0.67f, 0.81f)));
            d = Mathf.Min(d, Line(p, new Vector2(0.67f, 0.81f), new Vector2(0.73f, 0.58f)));
            d = Mathf.Min(d, Line(p, new Vector2(0.73f, 0.58f), new Vector2(0.25f, 0.51f)));
            return Mathf.Min(d, Line(p, new Vector2(0.44f, 0.53f), new Vector2(0.77f, 0.19f)));
        }

        private static float U(Vector2 p)
        {
            float d = Line(p, new Vector2(0.24f, 0.83f), new Vector2(0.26f, 0.30f));
            d = Mathf.Min(d, Line(p, new Vector2(0.26f, 0.30f), new Vector2(0.46f, 0.19f)));
            d = Mathf.Min(d, Line(p, new Vector2(0.46f, 0.19f), new Vector2(0.71f, 0.30f)));
            return Mathf.Min(d, Line(p, new Vector2(0.71f, 0.30f), new Vector2(0.75f, 0.83f)));
        }

        private static float N(Vector2 p)
        {
            float d = Line(p, new Vector2(0.25f, 0.19f), new Vector2(0.25f, 0.83f));
            d = Mathf.Min(d, Line(p, new Vector2(0.25f, 0.83f), new Vector2(0.75f, 0.19f)));
            return Mathf.Min(d, Line(p, new Vector2(0.75f, 0.19f), new Vector2(0.75f, 0.83f)));
        }

        private static float Line(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 d = b - a;
            return Vector2.Distance(p, a + d * Mathf.Clamp01(Vector2.Dot(p - a, d) / d.sqrMagnitude));
        }
    }
}
