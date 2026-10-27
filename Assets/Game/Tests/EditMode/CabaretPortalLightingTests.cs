using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Zero.Tests.EditMode
{
    public sealed class CabaretPortalLightingTests
    {
        [Test]
        public static void OutsideHoleKeepsOriginalFloorLighting()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            var original = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            original.SetColor("_BaseColor", new Color(.5f, .3f, .1f));
            var portal = new Material(original) { shader = Shader.Find("Zero/Cabaret Portal Floor") };
            var texture = RenderTexture.GetTemporary(256, 256, 24);
            var image = new Texture2D(256, 256, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            try { Compare(scene, original, portal, texture, image); }
            finally
            {
                RenderTexture.active = previous; RenderTexture.ReleaseTemporary(texture);
                Object.DestroyImmediate(image); Object.DestroyImmediate(original); Object.DestroyImmediate(portal);
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        private static void Compare(Scene scene, Material original, Material portal, RenderTexture texture, Texture2D image)
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            SceneManager.MoveGameObjectToScene(floor, scene);
            var renderer = floor.GetComponent<Renderer>(); renderer.sharedMaterial = original;
            var cameraObject = new GameObject("PortalTestCamera", typeof(Camera));
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            var camera = cameraObject.GetComponent<Camera>(); camera.scene = scene;
            camera.transform.position = new Vector3(0, 4, -4); camera.transform.LookAt(Vector3.zero);
            camera.targetTexture = texture;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
            var lamp = new GameObject("PointLight", typeof(Light)); SceneManager.MoveGameObjectToScene(lamp, scene);
            lamp.transform.position = new Vector3(0, 2, 0);
            var light = lamp.GetComponent<Light>(); light.type = LightType.Point; light.intensity = 15; light.range = 10;
            Color before = Sample(camera, texture, image, new Vector3(1.5f, 0, 0));
            renderer.sharedMaterial = portal;
            var block = new MaterialPropertyBlock(); block.SetVector("_Hole", new Vector4(0, 0, 0, .5f));
            block.SetFloat("_HoleOpen", 1); renderer.SetPropertyBlock(block);
            Color after = Sample(camera, texture, image, new Vector3(1.5f, 0, 0));
            Assert.That(before.grayscale, Is.GreaterThan(.01f));
            Assert.That(Mathf.Abs(after.grayscale - before.grayscale), Is.LessThan(.015f), "Opening the hole must not darken the surrounding floor.");
            Assert.That(Sample(camera, texture, image, Vector3.zero).grayscale, Is.LessThan(.01f), "The center must still be cut out.");
            Debug.Log("PORTAL_LIGHTING_PASS: original=" + before + "; portal=" + after);
            camera.targetTexture = null;
        }

        private static Color Sample(Camera camera, RenderTexture target, Texture2D image, Vector3 world)
        {
            camera.Render(); RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, 256, 256), 0, 0);
            Vector3 pixel = camera.WorldToScreenPoint(world);
            return image.GetPixel(Mathf.RoundToInt(pixel.x), Mathf.RoundToInt(pixel.y));
        }
    }
}
