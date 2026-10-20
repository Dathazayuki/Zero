#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Zero.AnomalyPrototype.Anomalies;
using Zero.AnomalyPrototype.Demo;

namespace Zero.Tests.PlayMode
{
    public sealed class LivingRoomExtraAnomalyTests
    {
        private LivingRoomAnomalyPreview preview;

        [UnitySetUp]
        public IEnumerator Load()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/Game/Scenes/Demos/InfinityRoom2_LivingRoom_Demo.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            preview = Object.FindFirstObjectByType<LivingRoomAnomalyPreview>();
            Assert.That(preview, Is.Not.Null);
            Assert.That(preview.IsReady, Is.True);
            Assert.That(preview.Anomalies.Count, Is.EqualTo(18));
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (preview != null) preview.ResetPreview();
            yield return null;
        }

        [UnityTest]
        public IEnumerator TeapotAndPictureChangeOnlyTheirAuthoredPosesAndRestoreOnSwitch()
        {
            Transform pot = Target(preview.Anomalies[6]);
            Transform picture = Target(preview.Anomalies[7]);
            Quaternion potNormal = pot.localRotation, pictureNormal = picture.localRotation;
            Select(6);
            Assert.That(preview.Current.Data.Id, Is.EqualTo("IR2-07"));
            Assert.That(Quaternion.Angle(pot.localRotation, potNormal), Is.EqualTo(180f).Within(0.01f));
            Assert.That(picture.localRotation, Is.EqualTo(pictureNormal));
            preview.CycleNext();
            Assert.That(preview.Current.Data.Id, Is.EqualTo("IR2-08"));
            Assert.That(Quaternion.Angle(pot.localRotation, potNormal), Is.LessThan(0.01f));
            Assert.That(Quaternion.Angle(picture.localRotation, pictureNormal), Is.EqualTo(5f).Within(0.01f));
            preview.ResetPreview();
            Assert.That(Quaternion.Angle(picture.localRotation, pictureNormal), Is.LessThan(0.01f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator TeaCupLiftsTiltsReturnsAndCleansUpWhenPreviewIsDisabled()
        {
            var tea = (FloatingTeaCupAnomaly)preview.Anomalies[8];
            Vector3 normal = tea.Cup.localPosition;
            Quaternion rotation = tea.Cup.localRotation;
            Transform rest = tea.Cup.parent.Find("OtherCupsAndSaucers");
            Vector3 restPosition = rest.position;
            Select(8);
            Assert.That(preview.Current.Data.Id, Is.EqualTo("IR2-09"));
            yield return new WaitForSeconds(2.9f);
            Assert.That(tea.Cup.localPosition.y - normal.y, Is.EqualTo(0.3f).Within(0.01f));
            Assert.That(Quaternion.Angle(tea.Cup.localRotation, rotation), Is.GreaterThan(15f));
            Assert.That(rest.position, Is.EqualTo(restPosition));
            yield return new WaitForSeconds(2.6f);
            Assert.That(Vector3.Distance(tea.Cup.localPosition, normal), Is.LessThan(0.001f));
            yield return new WaitForSeconds(3f);
            preview.enabled = false;
            Assert.That(tea.Cup.localPosition, Is.EqualTo(normal));
            Assert.That(Quaternion.Angle(tea.Cup.localRotation, rotation), Is.LessThan(0.01f));
            Assert.That(tea.IsActive, Is.False);
        }

        [UnityTest]
        public IEnumerator PhoneUsesExistingEyeImageFlickersAndReturnsToItsNormalScreen()
        {
            var phone = (PhoneEyeAnomaly)preview.Anomalies[10];
            Assert.That(phone.Screen.enabled, Is.False);
            Assert.That(AssetDatabase.GetAssetPath(phone.Screen.sharedMaterial.GetTexture("_BaseMap")), Is.EqualTo("Assets/Game/Art/Eye.png"));
            Transform display = phone.Screen.transform;
            Vector3 position = display.position;
            var block = new MaterialPropertyBlock();
            Select(10);
            Assert.That(preview.Current.Data.Id, Is.EqualTo("IR2-11"));
            Assert.That(phone.Screen.enabled, Is.True);
            bool dimmed = false, bright = false;
            float end = Time.time + 3f;
            while (Time.time < end)
            {
                phone.Screen.GetPropertyBlock(block);
                float green = block.GetColor("_BaseColor").g;
                dimmed |= green < 0.3f;
                bright |= green > 0.7f;
                yield return null;
            }
            Assert.That(dimmed && bright, Is.True, "LCD must show both the eye and a brief dim flicker.");
            Assert.That(display.position, Is.EqualTo(position));
            preview.ResetPreview();
            Assert.That(phone.Screen.enabled, Is.False);
            phone.Screen.GetPropertyBlock(block);
            Assert.That(block.isEmpty, Is.True);
        }

        private void Select(int index)
        {
            preview.ResetPreview();
            for (int i = 0; i <= index; i++) Assert.That(preview.CycleNext(), Is.True);
        }

        private static Transform Target(AnomalyBehaviour anomaly) =>
            (Transform)new SerializedObject(anomaly).FindProperty("targets").GetArrayElementAtIndex(0).objectReferenceValue;
    }
}
#endif
