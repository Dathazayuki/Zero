using System.IO;
using UnityEditor;
using UnityEngine;

namespace Zero.Editor
{
    internal static class RecordsRoomBloodMarkAssets
    {
        internal static Material Material(string sourceName)
        {
            string path = RecordsRoomHorrorAssets.Folder + "/M_IR3_" + sourceName + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "IR3_" + sourceName };
            material.SetTexture("_BaseMap", RedInk(sourceName));
            material.SetColor("_BaseColor", new Color(0.5f, 0.48f, 0.48f));
            material.SetFloat("_Smoothness", 0.24f);
            material.SetFloat("_AlphaClip", 1f);
            material.SetFloat("_Cutoff", 0.12f);
            material.EnableKeyword("_ALPHATEST_ON");
            material.renderQueue = 2450;
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Texture2D RedInk(string sourceName)
        {
            string path = RecordsRoomHorrorAssets.Folder + "/T_IR3_" + sourceName + "_Ink.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) return existing;
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, true) { name = sourceName + "_Ink" };
            try
            {
                texture.LoadImage(File.ReadAllBytes("Assets/Game/Art/InfinityRoom3/" + sourceName + ".png"));
                Color32[] pixels = texture.GetPixels32();
                for (int i = 0; i < pixels.Length; i++) pixels[i] = Mask(pixels[i]);
                texture.SetPixels32(pixels);
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.Apply(true, false);
                AssetDatabase.CreateAsset(texture, path);
                return texture;
            }
            catch
            {
                Object.DestroyImmediate(texture);
                throw;
            }
        }

        private static Color32 Mask(Color32 pixel)
        {
            float red = (pixel.r - Mathf.Max(pixel.g, pixel.b)) / 255f;
            pixel.a = (byte)(pixel.a * Mathf.Clamp01(red * 6f));
            return pixel;
        }
    }
}
