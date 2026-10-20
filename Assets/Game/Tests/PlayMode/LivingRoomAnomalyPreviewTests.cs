#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Zero.AnomalyPrototype.Anomalies;
using Zero.AnomalyPrototype.Demo;
using Zero.AnomalyPrototype.Player;

namespace Zero.Tests.PlayMode
{
    public sealed class LivingRoomAnomalyPreviewTests
    {
        private LivingRoomAnomalyPreview preview;
        private Transform[] baseline;
        private Vector3[] positions;
        private Quaternion[] rotations;
        private bool[] active;
        private Keyboard keyboard;

        [UnitySetUp]
        public IEnumerator LoadDemo()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/Game/Scenes/Demos/InfinityRoom2_LivingRoom_Demo.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            preview = Object.FindFirstObjectByType<LivingRoomAnomalyPreview>();
            Assert.That(preview, Is.Not.Null, "Save the configured anomalies in the demo first.");
            Assert.That(preview.IsReady, Is.True);
            Object.FindFirstObjectByType<FirstPersonController>().enabled = false;
            baseline = preview.transform.parent.GetComponentsInChildren<Transform>(true);
            positions = new Vector3[baseline.Length];
            rotations = new Quaternion[baseline.Length];
            active = new bool[baseline.Length];
            for (int i = 0; i < baseline.Length; i++)
            {
                positions[i] = baseline[i].localPosition;
                rotations[i] = baseline[i].localRotation;
                active[i] = baseline[i].gameObject.activeSelf;
            }
            keyboard = InputSystem.AddDevice<Keyboard>();
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (preview != null) preview.ResetPreview();
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            yield return null;
        }

        private IEnumerator Press(Key key)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
        }

        [UnityTest]
        public IEnumerator F3CyclesAuthoredAnomaliesAndF2RestoresTheWholeRoom()
        {
            Assert.That(preview.CurrentIndex, Is.EqualTo(-1));
            string[] ids = { "IR2-01", "IR2-02", "IR2-03", "IR2-04", "IR2-05", "IR2-06", "IR2-07", "IR2-08", "IR2-09", "IR2-10", "IR2-11", "IR2-12", "IR2-13", "IR2-15", "IR2-16", "IR2-17", "IR2-18", "IR2-20" };
            Assert.That(preview.Anomalies.Count, Is.EqualTo(ids.Length));
            for (int step = 0; step < ids.Length + 2; step++)
            {
                yield return Press(Key.F3);
                Assert.That(preview.CurrentIndex, Is.EqualTo(step % ids.Length));
                Assert.That(preview.Current.Data.Id, Is.EqualTo(ids[step % ids.Length]));
                Assert.That(preview.Current.Data.CanKillPlayer, Is.EqualTo(preview.Current.Data.Id == "IR2-17" || preview.Current.Data.Id == "IR2-18" || preview.Current.Data.Id == "IR2-20"));
                for (int i = 0; i < ids.Length; i++) Assert.That(preview.Anomalies[i].IsActive, Is.EqualTo(i == step % ids.Length));
                if (step < 6) CaptureSelection(step);
            }
            yield return Press(Key.F2);
            preview.ResetPreview();
            Assert.That(preview.Current, Is.Null);
            AssertBaseline();
            yield return Press(Key.F3);
            Assert.That(preview.CurrentIndex, Is.Zero);
        }

        private void AssertBaseline()
        {
            for (int i = 0; i < baseline.Length; i++)
            {
                Assert.That(Vector3.Distance(baseline[i].localPosition, positions[i]), Is.LessThan(0.0001f), baseline[i].name);
                Assert.That(Quaternion.Angle(baseline[i].localRotation, rotations[i]), Is.LessThan(0.05f), baseline[i].name);
                Assert.That(baseline[i].gameObject.activeSelf, Is.EqualTo(active[i]), baseline[i].name);
            }
        }

        [UnityTest]
        public IEnumerator BothClockHandsTickBackwardsAndStopCleanly()
        {
            Assert.That(preview.CycleNext(), Is.True);
            var clock = (ReverseClockAnomaly)preview.Current;
            Assert.That(clock.IsConfigured, Is.True);
            var serialized = new SerializedObject(clock);
            Transform hour = (Transform)serialized.FindProperty("hourPivot").objectReferenceValue;
            Transform minute = (Transform)serialized.FindProperty("minutePivot").objectReferenceValue;
            float hourStart = hour.localEulerAngles.z, minuteStart = minute.localEulerAngles.z;
            yield return new WaitForSeconds(0.95f);
            Assert.That(Mathf.DeltaAngle(hourStart, hour.localEulerAngles.z), Is.GreaterThan(6f));
            Assert.That(Mathf.DeltaAngle(minuteStart, minute.localEulerAngles.z), Is.GreaterThan(24f));
            preview.ResetPreview();
            yield return new WaitForSeconds(0.4f);
            AssertBaseline();
        }

        [UnityTest]
        public IEnumerator ExtraTeaCupProducesSteamOnlyWhileSelected()
        {
            preview.CycleNext();
            preview.CycleNext();
            ParticleSystem steam = preview.Current.GetComponentInChildren<ParticleSystem>();
            Assert.That(steam, Is.Not.Null);
            yield return new WaitForSeconds(0.6f);
            Assert.That(steam.isPlaying, Is.True);
            Assert.That(steam.particleCount, Is.GreaterThan(0));
            preview.CycleNext();
            Assert.That(steam.isPlaying, Is.False);
            Assert.That(steam.particleCount, Is.Zero);
            preview.ResetPreview();
            AssertBaseline();
        }

        [UnityTest]
        public IEnumerator InvertedVasesStayOnTheirShelvesAndChairCollisionMovesWithVisual()
        {
            var chair = (LivingRoomArrangementAnomaly)preview.Anomalies[2];
            var serialized = new SerializedObject(chair);
            Transform visual = (Transform)serialized.FindProperty("targets").GetArrayElementAtIndex(0).objectReferenceValue;
            Transform collision = (Transform)serialized.FindProperty("targets").GetArrayElementAtIndex(1).objectReferenceValue;
            Vector3 before = visual.position;
            for (int i = 0; i < 3; i++) preview.CycleNext();
            Assert.That(Vector3.Distance(visual.position, collision.position), Is.LessThan(0.001f));
            Assert.That(visual.position.x - before.x, Is.EqualTo(1.15f).Within(0.001f));
            var vases = new SerializedObject(preview.Anomalies[3]).FindProperty("targets");
            Assert.That(vases.arraySize, Is.EqualTo(6));
            var bottoms = new float[6];
            Transform cabinet = preview.transform.parent.Find("IR2_Furniture/IR2_DisplayCabinet");
            for (int i = 0; i < 6; i++)
                bottoms[i] = SupportingShelfTop(cabinet, VaseBounds((Transform)vases.GetArrayElementAtIndex(i).objectReferenceValue).min.y);
            preview.CycleNext();
            for (int i = 0; i < 6; i++)
                Assert.That(VaseBounds((Transform)vases.GetArrayElementAtIndex(i).objectReferenceValue).min.y, Is.EqualTo(bottoms[i]).Within(0.002f));
            preview.ResetPreview();
            AssertBaseline();
            yield return null;
        }

        private static float SupportingShelfTop(Transform cabinet, float bottom)
        {
            float top = float.NegativeInfinity;
            foreach (Transform shelf in cabinet)
            {
                if (!shelf.name.StartsWith("IR2_Cabinet_Shelf_")) continue;
                float candidate = VaseBounds(shelf).max.y;
                if (candidate <= bottom + 0.005f) top = Mathf.Max(top, candidate);
            }
            Assert.That(float.IsNegativeInfinity(top), Is.False, "Vase must have a physical shelf underneath.");
            return top + 0.001f;
        }

        private static Bounds VaseBounds(Transform vase)
        {
            Renderer[] renderers = vase.GetComponentsInChildren<Renderer>();
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        [UnityTest]
        public IEnumerator BooksHideNormalRowsAndDisablingPreviewCleansEveryAnomaly()
        {
            for (int i = 0; i < 5; i++) preview.CycleNext();
            var variant = new SerializedObject(preview.Current);
            SerializedProperty hidden = variant.FindProperty("hiddenObjects");
            Assert.That(hidden.arraySize, Is.EqualTo(4));
            for (int i = 0; i < hidden.arraySize; i++)
                Assert.That(((GameObject)hidden.GetArrayElementAtIndex(i).objectReferenceValue).activeSelf, Is.False);
            int books = 0;
            foreach (Transform part in preview.Current.GetComponentsInChildren<Transform>())
                if (part.name == "PF_IR2_ReversedBook") books++;
            Assert.That(books, Is.GreaterThanOrEqualTo(24));
            preview.ResetPreview();
            AssertBaseline();
            for (int i = 0; i < preview.Anomalies.Count; i++)
            {
                for (int step = 0; step <= i; step++) preview.CycleNext();
                preview.enabled = false;
                AssertBaseline();
                Assert.That(preview.CycleNext(), Is.False);
                preview.enabled = true;
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator ReaderShadowUsesArmchairAndRemainsInvisibleWhenActivated()
        {
            Transform chair = preview.transform.parent.Find("IR2_Furniture/IR2_DetailArmchairA");
            Transform caster = preview.Anomalies[5].transform.Find("SeatedReaderShadow");
            var renderer = caster.GetComponent<MeshRenderer>();
            Assert.That(renderer, Is.Not.Null);
            Assert.That(renderer.shadowCastingMode, Is.EqualTo(UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly));
            Assert.That(caster.GetComponent<MeshFilter>().sharedMesh.vertexCount, Is.GreaterThan(100));
            Assert.That(Vector3.Distance(caster.position, chair.position), Is.LessThan(0.001f));
            Assert.That(caster.gameObject.activeSelf, Is.False);
            for (int i = 0; i < 6; i++) preview.CycleNext();
            Assert.That(caster.gameObject.activeInHierarchy, Is.True);
            Assert.That(renderer.shadowCastingMode, Is.EqualTo(UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly));
            yield return null;
            preview.ResetPreview();
            preview.ResetPreview();
            Assert.That(caster.gameObject.activeSelf, Is.False);
            AssertBaseline();
        }

        private static void CaptureSelection(int index)
        {
            Vector3[] targets = { new Vector3(1.6f, 2.46f, 4.89f), new Vector3(-0.22f, 0.56f, -0.5f),
                new Vector3(2.8f, 0.65f, 0.05f), new Vector3(-3.45f, 1.4f, 4.38f),
                new Vector3(4.1f, 1.05f, 2.8f), new Vector3(-4.39f, 1f, 1.65f) };
            Vector3[] viewpoints = { new Vector3(1.6f, 2.46f, 3.75f), new Vector3(-0.22f, 1.05f, -1.2f),
                new Vector3(0.5f, 1.5f, -1.8f), new Vector3(-3.3f, 1.5f, 2.6f),
                new Vector3(2.4f, 1.4f, 2.8f), new Vector3(-2.8f, 1.2f, 1.65f) };
            var cameraRoot = new GameObject("AnomalyTestCapture", typeof(Camera));
            var render = new RenderTexture(768, 768, 24);
            var pixels = new Texture2D(768, 768, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active;
            try
            {
                Camera camera = cameraRoot.GetComponent<Camera>();
                camera.enabled = false;
                camera.fieldOfView = 60f;
                camera.nearClipPlane = 0.03f;
                camera.transform.SetPositionAndRotation(viewpoints[index], Quaternion.LookRotation(targets[index] - viewpoints[index]));
                camera.targetTexture = render;
                camera.Render();
                RenderTexture.active = render;
                pixels.ReadPixels(new Rect(0, 0, 768, 768), 0, 0);
                pixels.Apply();
                string directory = System.IO.Path.Combine(Application.dataPath, "Game/.scratch/LivingRoomAnomalies");
                System.IO.Directory.CreateDirectory(directory);
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(directory, $"IR2-{index + 1:00}.png"), pixels.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                Object.DestroyImmediate(cameraRoot);
                render.Release();
                Object.DestroyImmediate(render);
                Object.DestroyImmediate(pixels);
            }
        }
    }
}
#endif
