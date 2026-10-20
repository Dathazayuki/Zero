#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
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
    public sealed class RecordsRoomAnomalyPreviewTests
    {
        private Transform room;
        private LivingRoomAnomalyPreview preview;
        private Transform[] objects;
        private Vector3[] positions;
        private Quaternion[] rotations;
        private bool[] active;
        private Keyboard keyboard;

        [UnitySetUp]
        public IEnumerator LoadDemo()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/Game/Scenes/Demos/InfinityRoom3_RecordsRoom_Demo.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            preview = Object.FindFirstObjectByType<LivingRoomAnomalyPreview>();
            Assert.That(preview, Is.Not.Null, "Save the authored IR3 demo before running this test.");
            Assert.That(preview.IsReady, Is.True);
            Assert.That(preview.Anomalies.Count, Is.EqualTo(18));
            room = preview.transform.parent;
            Object.FindFirstObjectByType<FirstPersonController>().enabled = false;
            objects = room.GetComponentsInChildren<Transform>(true);
            positions = new Vector3[objects.Length];
            rotations = new Quaternion[objects.Length];
            active = new bool[objects.Length];
            for (int i = 0; i < objects.Length; i++)
            {
                positions[i] = objects[i].position;
                rotations[i] = objects[i].rotation;
                active[i] = objects[i].gameObject.activeSelf;
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

        [UnityTest]
        public IEnumerator F3PresentsFiveDistinctChangesAndF2RestoresTheNormalRoom()
        {
            Transform a12 = room.Find("IR3_Furniture/IR3_Cabinet1/IR3_Cabinet1_Drawer2/IR3_Cabinet1_Label2/ArchiveCode");
            Transform a13 = room.Find("IR3_Furniture/IR3_Cabinet1/IR3_Cabinet1_Drawer3/IR3_Cabinet1_Label3/ArchiveCode");
            Vector3 a12Before = a12.position, a13Before = a13.position;
            yield return Next(0);
            AssertPosition(a12, a13Before);
            AssertPosition(a13, a12Before);
            Transform pen = room.Find("IR3_Decor/IR3_DetailFolder_Desk/Pen/Pen");
            Vector3 penPosition = pen.position;
            Quaternion penRotation = pen.rotation;
            yield return Next(1);
            AssertPosition(pen, penPosition);
            Assert.That(Quaternion.Angle(pen.rotation, Quaternion.AngleAxis(180f, Vector3.up) * penRotation), Is.LessThan(0.05f));
            Transform binder = room.Find("IR3_Furniture/IR3_DetailedFiles/IR3_File_A_L3_S0_B2");
            Vector3 binderPosition = binder.position;
            yield return Next(2);
            Assert.That(preview.Current, Is.TypeOf<ArchiveFallingFilesAnomaly>());
            Assert.That(((ArchiveFallingFilesAnomaly)preview.Current).IsConfigured, Is.True);
            Rigidbody fallingBody = binder.GetComponent<Rigidbody>();
            Assert.That(fallingBody.isKinematic, Is.False);
            Assert.That(fallingBody.useGravity, Is.True);
            yield return AssertDrawer();
            yield return Next(4);
            Transform notice = room.Find("IR3_Decor/IR3_Noticeboard/IR3_Notice_0");
            Assert.That(notice.Find("FoldTopRight").gameObject.activeSelf, Is.False);
            Assert.That(notice.Find("FoldTopLeft").gameObject.activeSelf, Is.True);
            yield return Next(5);
            Assert.That(preview.Current.Data.Id, Is.EqualTo("IR3-06"));
            yield return Next(6);
            Assert.That(preview.Current.Data.Id, Is.EqualTo("IR3-07"));
            yield return Next(7);
            Assert.That(preview.Current, Is.TypeOf<EndlessPrinterAnomaly>());
            yield return AssertAdditionalAnomalies();
            yield return Next(0);
            yield return Press(Key.F2);
            AssertBaseline();
            preview.ResetPreview();
            AssertBaseline();
            yield return Next(0);
            preview.enabled = false;
            AssertBaseline();
            Assert.That(preview.CycleNext(), Is.False);
        }

        [UnityTest]
        public IEnumerator CabinetImpactBlacksOutImmediatelyWithoutWaitingForTheFreezeDuration()
        {
            for (int i = 0; i <= 16; i++) Assert.That(preview.CycleNext(), Is.True);
            var crush = (ArchiveCabinetCrushAnomaly)preview.Current;
            var view = crush.GetComponent<AnomalyDeathPresentation>();
            view.SetFreezeDuration(0f);
            var player = Object.FindFirstObjectByType<FirstPersonController>();
            var death = player.GetComponent<PlayerDeathController>();
            var controller = player.GetComponent<CharacterController>();
            Transform warning = room.Find("IR3_Furniture/IR3_Cabinet1 (8)");
            var bounds = new Bounds(warning.position, Vector3.zero);
            Renderer[] renderers = warning.GetComponentsInChildren<Renderer>();
            bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
            var strike = room.Find("IR3_Furniture/IR3_Cabinet1 (5)").GetComponent<BoxCollider>();
            Vector3 direction = strike.bounds.center - bounds.center;
            direction.y = 0f;
            Vector3 target = bounds.ClosestPoint(strike.bounds.center) + direction.normalized * 0.35f;
            target.y = player.transform.position.y;
            controller.enabled = false;
            player.transform.position = target;
            controller.enabled = true;
            Physics.SyncTransforms();
            float deadline = Time.realtimeSinceStartup + 3f;
            while (!death.IsDeathInProgress && Time.realtimeSinceStartup < deadline)
                yield return new WaitForFixedUpdate();
            Assert.That(death.IsDeathInProgress, Is.True, "The cabinet must hit the Player placed in its path.");
            Assert.That(death.IsScreenBlack, Is.True, "A zero-duration FreezeBlackout presentation must black out in the same physics tick.");
        }

        private IEnumerator AssertDrawer()
        {
            Transform cabinet = room.Find("IR3_Furniture/IR3_Cabinet1");
            string[] names = { "IR3_Cabinet1_Drawer3", "IR3_Cabinet1_Drawer3/IR3_Cabinet1_Handle3", "IR3_Cabinet1_Drawer3/IR3_Cabinet1_Label3" };
            var before = new Vector3[names.Length];
            for (int i = 0; i < names.Length; i++) before[i] = cabinet.Find(names[i]).position;
            yield return Next(3);
            for (int i = 0; i < names.Length; i++) AssertPosition(cabinet.Find(names[i]), before[i] + Vector3.right * 0.025f);
        }

        private IEnumerator AssertAdditionalAnomalies()
        {
            yield return Next(8);
            yield return Next(9);
            Assert.That(((FamilyPeekAnomaly)preview.Current).IsConfigured, Is.True);
            yield return Next(10);
            Assert.That(((ApproachingPaintingAnomaly)preview.Current).StageIndex, Is.Zero);
            yield return Next(11);
            Assert.That(preview.Current, Is.TypeOf<ArchiveBoxShakeAnomaly>());
            yield return Next(12);
            Assert.That(preview.Current, Is.TypeOf<ArchiveBodyDrawersAnomaly>());
            Assert.That(((ArchiveBodyDrawersAnomaly)preview.Current).IsConfigured, Is.True);
            yield return Next(13);
            Assert.That(preview.Current, Is.TypeOf<DeskLampFlickerAnomaly>());
            Assert.That(((DeskLampFlickerAnomaly)preview.Current).IsConfigured, Is.True);
            yield return Next(14);
            Assert.That(preview.Current, Is.TypeOf<ArchiveHeadAnomaly>());
            yield return Next(15);
            Assert.That(room.Find("IR3_Anomalies/IR3_16_CeilingPentagram/SatanCircle").gameObject.activeSelf, Is.True);
            yield return Next(16);
            Assert.That(((ArchiveCabinetCrushAnomaly)preview.Current).IsConfigured, Is.True);
            yield return Next(17);
            Assert.That(((ArchiveAssemblyAnomaly)preview.Current).IsConfigured, Is.True);
        }

        private IEnumerator Next(int index)
        {
            yield return Press(Key.F3);
            Assert.That(preview.CurrentIndex, Is.EqualTo(index));
            string[] ids = { "IR3-01", "IR3-02", "IR3-03", "IR3-04", "IR3-05", "IR3-06", "IR3-07", "IR3-08", "IR3-09", "IR3-10", "IR3-11", "IR3-12", "IR3-13", "IR3-14", "IR3-15", "IR3-16", "IR3-17", "IR3-18" };
            Assert.That(preview.Current.Data.Id, Is.EqualTo(ids[index]));
            for (int i = 0; i < preview.Anomalies.Count; i++)
            {
                Assert.That(preview.Anomalies[i].Data.CanKillPlayer, Is.EqualTo(i == 14 || i >= 16));
                if (preview.Anomalies[i] is LivingRoomArrangementAnomaly arrangement)
                    Assert.That(arrangement.IsConfigured, Is.True);
                if (preview.Anomalies[i] is EndlessPrinterAnomaly printer)
                    Assert.That(printer.IsConfigured, Is.True);
                Assert.That(preview.Anomalies[i].IsActive, Is.EqualTo(i == index));
            }
        }

        private IEnumerator Press(Key key)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
        }

        private void AssertBaseline()
        {
            Assert.That(preview.CurrentIndex, Is.EqualTo(-1));
            for (int i = 0; i < objects.Length; i++)
            {
                AssertPosition(objects[i], positions[i]);
                Assert.That(Quaternion.Angle(objects[i].rotation, rotations[i]), Is.LessThan(0.05f), objects[i].name);
                Assert.That(objects[i].gameObject.activeSelf, Is.EqualTo(active[i]), objects[i].name);
            }
        }

        private static void AssertPosition(Transform target, Vector3 expected)
        {
            Assert.That(Vector3.Distance(target.position, expected), Is.LessThan(0.0001f), target.name);
        }
    }
}
#endif
