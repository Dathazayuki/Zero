using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Zero.Tests.EditMode
{
    public sealed class CyberpunkMaterialTextureTests
    {
        private const string Root = "Assets/Game/Scenes/cyberpunk-city/";

        [TestCase("Material.007", 0)]
        [TestCase("Material.001", 1)]
        [TestCase("advertising_screens_texture_01", 3)]
        [TestCase("Material.003", 6)]
        [TestCase("Material.009", 7)]
        [TestCase("Material.004", 8)]
        [TestCase("Material.008", 9)]
        [TestCase("Material.005", 10)]
        [TestCase("Material.002", 11)]
        public void BaseMapMatchesOriginalGltfMaterial(string name, int image)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(Root + "source/" + name + ".mat");
            Assert.That(material, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(material.GetTexture("_BaseMap")),
                Is.EqualTo(Root + "textures/gltf_embedded_" + image + ".jpeg"));
            Assert.That(material.GetColor("_BaseColor"), Is.EqualTo(Color.white));
        }

        [TestCase("advertising_screens_texture_01", 2)]
        [TestCase("Material.003", 5)]
        public void MetallicSmoothnessMaskIsLinearAndEnabled(string name, int image)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(Root + "source/" + name + ".mat");
            string path = Root + "textures/gltf_embedded_" + image + "_URP_MetallicSmoothness.png";
            Assert.That(AssetDatabase.GetAssetPath(material.GetTexture("_MetallicGlossMap")), Is.EqualTo(path));
            Assert.That(((TextureImporter)AssetImporter.GetAtPath(path)).sRGBTexture, Is.False);
            Assert.That(material.IsKeywordEnabled("_METALLICSPECGLOSSMAP"), Is.True);
            Assert.That(material.GetFloat("_Smoothness"), Is.EqualTo(1f));
        }

        [Test]
        public void RoadNormalAndNeonEmissionAreRestored()
        {
            string normal = Root + "textures/gltf_embedded_4.jpeg";
            Assert.That(((TextureImporter)AssetImporter.GetAtPath(normal)).textureType, Is.EqualTo(TextureImporterType.NormalMap));
            foreach (string name in new[] { "Material.007", "Material.001", "Material.008", "Material.002" })
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(Root + "source/" + name + ".mat");
                Assert.That(material.GetTexture("_EmissionMap"), Is.SameAs(material.GetTexture("_BaseMap")));
                Assert.That(material.IsKeywordEnabled("_EMISSION"), Is.True);
            }
        }
    }
}
