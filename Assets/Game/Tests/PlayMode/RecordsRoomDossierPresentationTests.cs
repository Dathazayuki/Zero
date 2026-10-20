#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Zero.AnomalyPrototype.Anomalies;

namespace Zero.Tests.PlayMode
{
    public sealed class RecordsRoomDossierPresentationTests
    {
        [UnityTest]
        public IEnumerator FaceTrackingAndGapVisibilityRestoreOnResetAndDisable()
        {
            var root = new GameObject("PeekTest");
            var actor = new GameObject("Face");
            var observer = new GameObject("Observer");
            var normal = new GameObject("NormalFiles");
            var changed = new GameObject("GapFiles");
            try
            {
                actor.transform.SetParent(root.transform); actor.SetActive(false); changed.SetActive(false);
                observer.transform.position = new Vector3(2f, 0f, 3f);
                var anomaly = root.AddComponent<FamilyPeekAnomaly>();
                anomaly.Configure(actor.transform, observer.transform, Vector3.back * 0.4f);
                anomaly.ConfigureVisibility(new[] { changed }, new[] { normal });
                var settings = new SerializedObject(anomaly);
                settings.FindProperty("faceObserver").boolValue = true;
                settings.ApplyModifiedProperties();
                anomaly.Activate();
                yield return new WaitForSeconds(0.2f);
                Assert.That(changed.activeSelf, Is.True); Assert.That(normal.activeSelf, Is.False);
                Assert.That(Quaternion.Angle(actor.transform.rotation, Quaternion.identity), Is.GreaterThan(0.5f));
                anomaly.ResetAnomaly(); anomaly.ResetAnomaly();
                Assert.That(changed.activeSelf, Is.False); Assert.That(normal.activeSelf, Is.True);
                Assert.That(actor.activeSelf, Is.False);
                Assert.That(Quaternion.Angle(actor.transform.rotation, Quaternion.identity), Is.LessThan(0.01f));
                anomaly.Activate(); anomaly.enabled = false;
                Assert.That(normal.activeSelf, Is.True); Assert.That(changed.activeSelf, Is.False);
            }
            finally
            {
                Object.Destroy(root); Object.Destroy(observer); Object.Destroy(normal); Object.Destroy(changed);
            }
        }

        [UnityTest]
        public IEnumerator PortraitCanAdvanceWithPlayerColliderBetweenCameraAndPhoto()
        {
            var root = new GameObject("PortraitTest");
            var player = new GameObject("Player");
            GameObject photo = GameObject.CreatePrimitive(PrimitiveType.Quad);
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            Texture2D[] stages = { new Texture2D(8, 8), new Texture2D(8, 8), new Texture2D(8, 8) };
            try
            {
                photo.transform.SetParent(root.transform); photo.GetComponent<Renderer>().sharedMaterial = material;
                var camera = player.AddComponent<Camera>(); camera.transform.position = Vector3.back * 2f;
                body.transform.SetParent(player.transform, false); body.transform.localPosition = Vector3.forward * 0.3f;
                body.transform.localScale = Vector3.one * 0.2f;
                var anomaly = root.AddComponent<ApproachingPaintingAnomaly>();
                anomaly.Configure(photo.GetComponent<Renderer>(), camera, stages); anomaly.Activate();
                yield return new WaitForSeconds(0.15f);
                camera.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                yield return new WaitForSeconds(0.5f);
                camera.transform.rotation = Quaternion.identity;
                yield return new WaitForSeconds(0.15f);
                Assert.That(anomaly.StageIndex, Is.EqualTo(1));
                anomaly.ResetAnomaly(); anomaly.ResetAnomaly();
                Assert.That(anomaly.StageIndex, Is.EqualTo(-1));
                var block = new MaterialPropertyBlock(); photo.GetComponent<Renderer>().GetPropertyBlock(block);
                Assert.That(block.GetTexture("_BaseMap"), Is.Null);
            }
            finally
            {
                foreach (Texture2D texture in stages) Object.Destroy(texture);
                Object.Destroy(root); Object.Destroy(player); Object.Destroy(material);
            }
        }
    }
}
#endif
