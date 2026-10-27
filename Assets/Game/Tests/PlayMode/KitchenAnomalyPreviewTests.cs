#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Zero.AnomalyPrototype.Anomalies;
using Zero.AnomalyPrototype.Demo;
using Zero.AnomalyPrototype.Player;

namespace Zero.Tests.PlayMode
{
    public sealed class KitchenAnomalyPreviewTests
    {
        [UnityTest]
        public IEnumerator StoveFireAndFridgeStruggleStartStopAndResetInTheAuthoredScene()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/Game/Scenes/Demos/InfinityRoom4_HotelKitchen_Demo.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            var preview = Object.FindFirstObjectByType<LivingRoomAnomalyPreview>();
            var player = Object.FindFirstObjectByType<FirstPersonController>();
            player.enabled = false;
            try
            {
                for (int i = 0; i < 4; i++) Assert.That(preview.CycleNext(), Is.True);
                yield return CheckAppliances(preview, preview.transform.parent);
                preview.ResetPreview();
                Assert.That(preview.CurrentIndex, Is.EqualTo(-1));
                foreach (var effect in preview.GetComponentsInChildren<KitchenApplianceAnomaly>(true))
                    Assert.That(effect.IsPresenting, Is.False);
            }
            finally { preview.ResetPreview(); player.enabled = true; }
        }

        [UnityTest]
        public IEnumerator HeadTurnsGraduallyBothIntoViewAndBackToNormal()
        {
            var root = new GameObject("SmoothHeadTest");
            var head = new GameObject("Head").transform;
            head.SetParent(root.transform);
            var camera = new GameObject("Target").AddComponent<Camera>();
            var anomaly = root.AddComponent<HorrorDollAnomaly>();
            var serialized = new UnityEditor.SerializedObject(anomaly);
            serialized.FindProperty("headBone").objectReferenceValue = head;
            serialized.FindProperty("hideInNormalState").boolValue = false;
            serialized.FindProperty("full360Rotation").boolValue = false;
            serialized.FindProperty("turnSpeed").floatValue = 60f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            anomaly.BindPlayer(camera.transform, camera);
            anomaly.CaptureNormalState();
            try
            {
                camera.transform.position = new Vector3(2f, 0f, 2f);
                anomaly.Activate();
                yield return new WaitForSeconds(0.3f);
                float halfway = Quaternion.Angle(Quaternion.identity, head.localRotation);
                Assert.That(halfway, Is.InRange(5f, 40f));
                yield return new WaitForSeconds(0.8f);
                Assert.That(Quaternion.Angle(Quaternion.identity, head.localRotation), Is.EqualTo(45f).Within(1f));
                camera.transform.position = Vector3.back * 2f;
                yield return new WaitForSeconds(0.3f);
                Assert.That(Quaternion.Angle(Quaternion.identity, head.localRotation), Is.InRange(5f, 40f));
                yield return new WaitForSeconds(0.8f);
                Assert.That(Quaternion.Angle(Quaternion.identity, head.localRotation), Is.LessThan(1f));
                anomaly.ResetAnomaly();
                Assert.That(head.localRotation, Is.EqualTo(Quaternion.identity));
            }
            finally { Object.Destroy(root); Object.Destroy(camera.gameObject); }
        }

        [UnityTest]
        public IEnumerator SixAnomaliesCycleAndDisableRestoresTheNormalRoomWhileTheChefKeepsAnimating()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/Game/Scenes/Demos/InfinityRoom4_HotelKitchen_Demo.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            var preview = Object.FindFirstObjectByType<LivingRoomAnomalyPreview>();
            Assert.That(preview, Is.Not.Null);
            Assert.That(preview.IsReady, Is.True);
            Assert.That(preview.Anomalies.Count, Is.EqualTo(13));
            Transform room = preview.transform.parent;
            Transform station = room.Find("IR4_Props/IR4_Detail_4_ServiceCart");
            Transform meat = room.Find("IR4_Props/IR4_Detail_8_ChoppingStation/Ceramic"), sink = room.Find("IR4_Equipment/IR4_Sink");
            Vector3 stationBefore = station.localPosition, sinkBefore = sink.localScale;
            try
            {
                AssertNext(preview, "IR4-01");
                Assert.That(meat.gameObject.activeSelf, Is.False);
                AssertNext(preview, "IR4-02");
                Assert.That(meat.gameObject.activeSelf, Is.True);
                Assert.That(sink.localScale.x, Is.EqualTo(-1f).Within(0.001f));
                AssertNext(preview, "IR4-03");
                Assert.That(sink.localScale, Is.EqualTo(sinkBefore));
                Assert.That(Vector3.Distance(station.localPosition, stationBefore), Is.GreaterThan(0.01f));
                yield return CheckWatcher(preview);
                yield return CheckAppliances(preview, room);
                Assert.That(station.localPosition, Is.EqualTo(stationBefore));
                preview.ResetPreview();
                AssertNext(preview, "IR4-01");
                AssertNext(preview, "IR4-02");
                preview.enabled = false;
                Assert.That(sink.localScale, Is.EqualTo(sinkBefore));
                Assert.That(meat.gameObject.activeSelf, Is.True);
                Assert.That(preview.CurrentIndex, Is.EqualTo(-1));
            }
            finally { preview.ResetPreview(); }
        }

        private static void AssertNext(LivingRoomAnomalyPreview preview, string id)
        {
            Assert.That(preview.CycleNext(), Is.True);
            Assert.That(preview.Current.Data.Id, Is.EqualTo(id));
            Assert.That(preview.Current.Data.CanKillPlayer, Is.False);
        }

        private static IEnumerator CheckWatcher(LivingRoomAnomalyPreview preview)
        {
            var player = Object.FindFirstObjectByType<FirstPersonController>();
            player.enabled = false;
            Transform camera = player.transform.Find("IR4_PreviewCamera");
            Vector3 normal = camera.localPosition;
            AssertNext(preview, "IR4-07");
            var watcher = (HorrorDollAnomaly)preview.Current;
            var serialized = new UnityEditor.SerializedObject(watcher);
            var head = (Transform)serialized.FindProperty("headBone").objectReferenceValue;
            var chef = (GameObject)serialized.FindProperty("dollObject").objectReferenceValue;
            var animator = chef.GetComponent<Animator>();
            RuntimeAnimatorController controller = animator.runtimeAnimatorController;
            try
            {
                camera.position = head.position + chef.transform.forward * 2f - chef.transform.right * 2f;
                yield return new WaitForSeconds(1.6f);
                Quaternion first = head.rotation;
                camera.position = head.position + chef.transform.forward * 2f + chef.transform.right * 2f;
                yield return new WaitForSeconds(1.6f);
                Assert.That(Quaternion.Angle(first, head.rotation), Is.GreaterThan(25f));
                Assert.That(animator.enabled, Is.True);
                Assert.That(animator.runtimeAnimatorController, Is.SameAs(controller));
            }
            finally { camera.localPosition = normal; }
        }

        private static IEnumerator CheckAppliances(LivingRoomAnomalyPreview preview, Transform room)
        {
            var player = Object.FindFirstObjectByType<FirstPersonController>();
            Vector3 normal = player.transform.position;
            Transform range = room.Find("IR4_Equipment/IR4_Range"), fridge = room.Find("IR4_Equipment/IR4_Fridge");
            Vector3 fridgeNormal = fridge.localPosition;
            var character = player.GetComponent<CharacterController>();
            character.enabled = false;
            try
            {
                AssertNext(preview, "IR4-05");
                player.transform.position = range.position - range.forward;
                yield return new WaitForSeconds(0.3f);
                var fire = (KitchenApplianceAnomaly)preview.Current;
                Assert.That(fire.IsPresenting, Is.True);
                foreach (var ps in fire.GetComponentsInChildren<ParticleSystem>(true))
                    Assert.That(ps.isPlaying, Is.True);
                AssertNext(preview, "IR4-19");
                Assert.That(fire.IsPresenting, Is.False);
                player.transform.position = fridge.position - fridge.forward;
                yield return new WaitForSeconds(0.3f);
                Assert.That(((KitchenApplianceAnomaly)preview.Current).IsPresenting, Is.True);
                foreach (var audio in preview.Current.GetComponentsInChildren<AudioSource>(true))
                    Assert.That(audio.isPlaying, Is.True);
                foreach (var door in fridge.GetComponentsInChildren<Zero.AnomalyPrototype.Interaction.ApplianceDoor>())
                    Assert.That(door.enabled || door.IsOpen, Is.False);
                player.transform.position = fridge.position + Vector3.left * 8f;
                yield return null;
                yield return null;
                Assert.That(((KitchenApplianceAnomaly)preview.Current).IsPresenting, Is.False);
                foreach (var audio in preview.Current.GetComponentsInChildren<AudioSource>(true))
                    Assert.That(audio.isPlaying, Is.False);
                Assert.That(fridge.localPosition, Is.EqualTo(fridgeNormal));
            }
            finally { player.transform.position = normal; character.enabled = true; }
        }
    }
}
#endif
