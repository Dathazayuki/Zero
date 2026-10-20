using System;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Zero.Editor
{
    internal static class RecordsRoomDossierAssets
    {
        private const string Folder = RecordsRoomHorrorAssets.Folder;

        internal static TextMeshPro Text(Transform parent, string name, string words, Vector2 size)
        {
            GameObject node = RecordsRoomHorrorAssets.Node(parent, name);
            var text = node.AddComponent<TextMeshPro>();
            text.font = Font();
            text.text = words;
            text.fontSize = 0.28f;
            text.color = new Color(0.16f, 0.12f, 0.09f);
            text.alignment = TextAlignmentOptions.Center;
            text.rectTransform.sizeDelta = size;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Truncate;
            return text;
        }

        private static TMP_FontAsset Font()
        {
            string path = Folder + "/F_IR3_ArchiveVietnamese.asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (existing != null) return existing;
            UnityEngine.Font source = AssetDatabase.LoadAssetAtPath<UnityEngine.Font>(
                "Assets/Game/Art/InfinityRoom3/Fonts/NotoSans-Regular.ttf");
            if (source == null) throw new InvalidOperationException("IR3_FONT_SOURCE_MISSING");
            TMP_FontAsset font = TMP_FontAsset.CreateFontAsset(source);
            if (font == null) throw new InvalidOperationException("IR3_FONT_CREATION_FAILED");
            font.name = "F_IR3_ArchiveVietnamese";
            font.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            if (!font.TryAddCharacters("ĐÃ ĐÓNG HỒ SƠARCHIVEPERSONNELRECORDDEPARTMENTSTAFFID047-0123456789", out string missing))
                throw new InvalidOperationException("IR3_FONT_GLYPHS_MISSING: " + missing);
            AssetDatabase.CreateAsset(font, path);
            AssetDatabase.AddObjectToAsset(font.material, font);
            foreach (Texture2D atlas in font.atlasTextures) if (atlas != null) AssetDatabase.AddObjectToAsset(atlas, font);
            AssetDatabase.SaveAssetIfDirty(font);
            return font;
        }

        internal static GameObject StampPrefab()
        {
            string path = Folder + "/PF_IR3_ClosedDossierStamp.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;
            Font();
            GameObject root = RecordsRoomHorrorAssets.Node(null, "PF_IR3_ClosedDossierStamp");
            TextMeshPro text = Text(root.transform, "ClosedStamp", "ĐÃ ĐÓNG\nHỒ SƠ", new Vector2(0.25f, 0.095f));
            text.fontSize = 0.24f;
            text.color = new Color(0.40f, 0.025f, 0.02f);
            text.fontStyle = FontStyles.Bold;
            try { return PrefabUtility.SaveAsPrefabAsset(root, path); }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        internal static Texture2D BlankPhoto()
        {
            string path = Folder + "/T_IR3_EmployeeBlank.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) return existing;
            var texture = new Texture2D(8, 8) { name = "BlankEmployeePhoto", wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color[64];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color(0.67f, 0.63f, 0.54f);
            texture.SetPixels(pixels); texture.Apply();
            AssetDatabase.CreateAsset(texture, path);
            return texture;
        }

        internal static Material PortraitMaterial()
        {
            Material material = RecordsRoomHorrorAssets.Material("EmployeePhoto", Color.white);
            material.SetTexture("_BaseMap", BlankPhoto());
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssetIfDirty(material);
            return material;
        }

        internal static Texture2D Portrait(bool open)
        {
            string path = "Assets/Game/Art/InfinityRoom3/IR3_11_Employee_0" + (open ? "2_EyesOpen.png" : "1_EyesClosed.png");
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("IR3_EMPLOYEE_PHOTO_MISSING: " + path);
            if (importer.maxTextureSize != 1024 || importer.wrapMode != TextureWrapMode.Clamp)
            {
                importer.maxTextureSize = 1024;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.mipmapEnabled = true;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
