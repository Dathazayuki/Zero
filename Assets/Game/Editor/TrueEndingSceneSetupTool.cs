using System;
using System.IO;
using System.Linq;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Zero.AnomalyPrototype.Flow;

namespace Zero.EditorTools
{
    public static class TrueEndingSceneSetupTool
    {
        private const string SceneName = "BackRoomScene";
        private const string RootPath = "BackRoomRoot/TrueEnding_Root";
        private const string LegacyRootPath = "BackRoomRoot/EndingC_Root";

        [MenuItem("Tools/Zero/Test True Ending Cutscene (G)")]
        public static void TestCutsceneMenu()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("TRUE_ENDING_TEST_WARNED: Enter Play Mode before testing the cutscene.");
                return;
            }
            var controller = UnityEngine.Object.FindFirstObjectByType<TrueEndingGameplayController>();
            if (controller != null) controller.DebugTriggerEndingCutscene();
            else Debug.LogError("TRUE_ENDING_TEST_FAILED: TrueEndingGameplayController not found in scene.");
        }

        [MenuItem("Tools/Zero/Setup True Ending In Scene")]
        public static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("TRUE_ENDING_SETUP_FAILED: Exit play mode before running setup.");
                return;
            }

            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!scene.name.Equals(SceneName, StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogError($"TRUE_ENDING_SETUP_FAILED: Active scene must be {SceneName}. Scene unchanged.");
                return;
            }

            Transform trueEndingRoot = FindTarget(RootPath) ?? FindTarget(LegacyRootPath);
            if (trueEndingRoot == null)
            {
                Debug.LogError($"TRUE_ENDING_SETUP_FAILED: Could not find {RootPath} or {LegacyRootPath}. Scene unchanged.");
                return;
            }

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Setup True Ending In Scene");

            try
            {
                var controller = GetOrAdd<TrueEndingGameplayController>(trueEndingRoot.gameObject);
                SetupTriggersAndMarkers(trueEndingRoot, controller);
                var room1 = SetupRoom1(trueEndingRoot, controller);
                var room2 = SetupRoom2(trueEndingRoot, controller);
                var (transition, ending, blocker) = SetupBlueSky(trueEndingRoot, controller, room2.lightController);
                WireGameplayController(trueEndingRoot, controller, room1, room2.movement, transition, ending, blocker);

                EditorSceneManager.MarkSceneDirty(scene);
                Undo.CollapseUndoOperations(undoGroup);
                Debug.Log("TRUE_ENDING_SETUP_COMPLETE: Scene configured with triggers, rotations, references, and Cinemachine. Review then press Ctrl+S. Undo is available.", controller);
                Selection.activeGameObject = trueEndingRoot.gameObject;
            }
            catch (Exception ex)
            {
                Undo.RevertAllDownToGroup(undoGroup);
                Debug.LogException(ex);
            }
        }

        private static void SetupTriggersAndMarkers(Transform root, TrueEndingGameplayController controller)
        {
            var triggers = new (string path, TrueEndingTriggerKind kind)[]
            {
                ("Room1/r1_activate", TrueEndingTriggerKind.Room1Start),
                ("Room1/r1_finish", TrueEndingTriggerKind.Room1Complete),
                ("Room2/r2_activate", TrueEndingTriggerKind.Room2Start),
                ("Room2/r2_finish", TrueEndingTriggerKind.Room2Complete),
                ("Connector2/c2_bluesky_trigger", TrueEndingTriggerKind.BlueSkyEntry),
                ("BlueSky/bs_top_trigger", TrueEndingTriggerKind.EndingPlatform)
            };

            foreach (var (relPath, kind) in triggers)
            {
                Transform t = root.Find(relPath);
                if (t == null) continue;
                if (relPath.Contains("bs_top_trigger"))
                {
                    Undo.RecordObject(t, "Move Ending Trigger to Stairs Foot");
                    t.position = new Vector3(-84.5f, 1.2f, 79.5f);
                    var col = t.GetComponent<BoxCollider>();
                    if (col != null) { Undo.RecordObject(col, "Resize Trigger"); col.size = new Vector3(4f, 3f, 2f); col.center = Vector3.zero; }
                }
                ConfigureTrigger(t.gameObject);
                var relay = GetOrAdd<TrueEndingTriggerRelay>(t.gameObject);
                var so = new SerializedObject(relay);
                so.FindProperty("controller").objectReferenceValue = controller;
                so.FindProperty("kind").enumValueIndex = (int)kind;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            DisableMarkerRenderers(root);
        }

        private static void ConfigureTrigger(GameObject go)
        {
            var col = go.GetComponent<BoxCollider>();
            if (col != null)
            {
                Undo.RecordObject(col, "Make Trigger");
                col.isTrigger = true;
            }
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                Undo.RecordObject(mr, "Disable Renderer");
                mr.enabled = false;
            }
        }

        private static void DisableMarkerRenderers(Transform root)
        {
            string[] markerPaths = { "Room1/r1_spawn", "Room2/r2_spawn", "BlueSky/bs_stairs_start", "BlueSky/bs_player_mark", "BlueSky/bs_camera_mark", "BlueSky/bs_ambient_zone" };
            foreach (string rel in markerPaths)
            {
                Transform t = root.Find(rel);
                if (t == null) continue;
                var mr = t.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    Undo.RecordObject(mr, "Disable Marker Renderer");
                    mr.enabled = false;
                }
                var col = t.GetComponent<Collider>();
                if (col != null && rel.Contains("ambient_zone"))
                {
                    Undo.RecordObject(col, "Make Ambient Trigger");
                    col.isTrigger = true;
                }
            }

            Transform r1Mech = root.Find("Room1/r1_mechanic_zone");
            if (r1Mech != null) Undo.RecordObject(r1Mech.gameObject, "Deactivate R1 Mechanic Zone"); r1Mech?.gameObject.SetActive(false);
            Transform r2Mech = root.Find("Room2/r2_mechanic_zone");
            if (r2Mech != null) Undo.RecordObject(r2Mech.gameObject, "Deactivate R2 Mechanic Zone"); r2Mech?.gameObject.SetActive(false);
        }

        private static Room1DontLookBackController SetupRoom1(Transform root, TrueEndingGameplayController controller)
        {
            Transform r1 = root.Find("Room1");
            Transform start = root.Find("Room1/r1_activate");
            Transform finish = root.Find("Room1/r1_finish");
            Transform spawn = root.Find("Room1/r1_spawn");

            Quaternion faceExit = Quaternion.LookRotation(Vector3.left, Vector3.up);
            if (spawn != null)
            {
                Undo.RecordObject(spawn, "Rotate Room 1 Checkpoint");
                spawn.rotation = faceExit;
            }

            Transform forwardRef = r1.Find("ForwardReference");
            if (forwardRef == null)
            {
                var go = new GameObject("ForwardReference");
                Undo.RegisterCreatedObjectUndo(go, "Create ForwardReference");
                forwardRef = go.transform;
                forwardRef.SetParent(r1, false);
            }
            Undo.RecordObject(forwardRef, "Position ForwardReference");
            forwardRef.position = start != null ? start.position : r1.position;
            forwardRef.rotation = faceExit;

            var baitSources = SetupRoom1Audio(r1);

            var r1Ctrl = GetOrAdd<Room1DontLookBackController>(r1.gameObject);
            var so = new SerializedObject(r1Ctrl);
            so.FindProperty("flow").objectReferenceValue = controller;
            so.FindProperty("forwardReference").objectReferenceValue = forwardRef;
            so.FindProperty("startPoint").objectReferenceValue = start;
            so.FindProperty("completePoint").objectReferenceValue = finish;
            var baitProp = so.FindProperty("baitSources");
            baitProp.arraySize = baitSources.Length;
            for (int i = 0; i < baitSources.Length; i++)
                baitProp.GetArrayElementAtIndex(i).objectReferenceValue = baitSources[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            return r1Ctrl;
        }

        private static AudioSource[] SetupRoom1Audio(Transform r1)
        {
            AudioClip footstepClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/Audio/HorrorGirl_FootStep.wav") ?? AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/Audio/SoundWalk.mp3");
            AudioClip breathClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/Audio/RunBreath2.mp3");

            Transform b1 = r1.Find("r1_bait_audio_01");
            Transform b2 = r1.Find("r1_bait_audio_02");
            Transform b3 = r1.Find("r1_bait_audio_03");

            if (b3 == null)
            {
                var go = new GameObject("r1_bait_audio_03");
                Undo.RegisterCreatedObjectUndo(go, "Create r1_bait_audio_03");
                b3 = go.transform;
                b3.SetParent(r1, false);
                b3.position = new Vector3(-1f, 1.6f, 90.1f);
            }

            var src1 = ConfigureAudioSource(b1 != null ? b1.gameObject : null, footstepClip);
            var src2 = ConfigureAudioSource(b2 != null ? b2.gameObject : null, breathClip);
            var src3 = ConfigureAudioSource(b3 != null ? b3.gameObject : null, footstepClip);

            return new[] { src1, src2, src3 }.Where(s => s != null).ToArray();
        }

        private static AudioSource ConfigureAudioSource(GameObject go, AudioClip clip)
        {
            if (go == null) return null;
            var src = GetOrAdd<AudioSource>(go);
            Undo.RecordObject(src, "Configure AudioSource");
            src.playOnAwake = false;
            src.spatialBlend = 1f;
            src.clip = clip;
            return src;
        }

        private static (Room2LightController lightController, Room2MovementChallenge movement) SetupRoom2(Transform root, TrueEndingGameplayController controller)
        {
            Transform r2 = root.Find("Room2");
            Transform spawn = root.Find("Room2/r2_spawn");
            if (spawn != null) { Undo.RecordObject(spawn, "Rotate Spawn"); spawn.rotation = Quaternion.LookRotation(Vector3.left, Vector3.up); }

            AudioSource warningSrc = ConfigureWarningAudio(r2);
            Light[] r2Lights = FindR2Lights(root.Find("EndingB_Lights"));

            var lightCtrl = GetOrAdd<Room2LightController>(r2.gameObject);
            var soLight = new SerializedObject(lightCtrl);
            var propLights = soLight.FindProperty("lights");
            propLights.arraySize = 5;
            for (int i = 0; i < 5; i++) propLights.GetArrayElementAtIndex(i).objectReferenceValue = r2Lights[i];
            soLight.FindProperty("warningSource").objectReferenceValue = warningSrc;
            soLight.FindProperty("beforeOn").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/Audio/LightOnOff.wav");
            soLight.FindProperty("beforeOff").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/Audio/LightOnOff.wav");
            soLight.ApplyModifiedPropertiesWithoutUndo();

            var moveCtrl = GetOrAdd<Room2MovementChallenge>(r2.gameObject);
            var soMove = new SerializedObject(moveCtrl);
            soMove.FindProperty("flow").objectReferenceValue = controller;
            soMove.FindProperty("lightController").objectReferenceValue = lightCtrl;
            soMove.ApplyModifiedPropertiesWithoutUndo();

            return (lightCtrl, moveCtrl);
        }

        private static Light[] FindR2Lights(Transform lightsFolder)
        {
            var r2Lights = new Light[5];
            if (lightsFolder == null) return r2Lights;
            var allLights = lightsFolder.GetComponentsInChildren<Light>(true);
            for (int i = 0; i < 5; i++)
            {
                string targetName = $"r2_light_0{i + 1}";
                for (int j = 0; j < allLights.Length; j++)
                    if (allLights[j] != null && allLights[j].gameObject.name == targetName) { r2Lights[i] = allLights[j]; break; }
            }
            return r2Lights;
        }

        private static AudioSource ConfigureWarningAudio(Transform r2)
        {
            if (r2 == null) return null;
            Transform warningAudioObj = r2.Find("r2_warning_audio");
            if (warningAudioObj == null)
            {
                var go = new GameObject("r2_warning_audio");
                Undo.RegisterCreatedObjectUndo(go, "Create r2_warning_audio");
                warningAudioObj = go.transform;
                warningAudioObj.SetParent(r2, false);
                warningAudioObj.localPosition = new Vector3(0f, 1.5f, 0f);
            }
            return ConfigureAudioSource(warningAudioObj.gameObject, null);
        }

        private static (BlueSkyTransitionController transition, BlueSkyEndingController ending, GameObject blocker) SetupBlueSky(
            Transform root, TrueEndingGameplayController controller, Room2LightController room2Lights)
        {
            Transform bs = root.Find("BlueSky");
            Transform lightsFolder = root.Find("EndingB_Lights");

            Light skyAmb = lightsFolder?.Find("bs_light_ambient")?.GetComponent<Light>();
            Light skyPlat = lightsFolder?.Find("bs_light_platform")?.GetComponent<Light>();

            Transform r2 = root.Find("Room2");
            Transform humObj = r2?.Find("r2_hum_audio");
            if (humObj == null && r2 != null)
            {
                var go = new GameObject("r2_hum_audio");
                Undo.RegisterCreatedObjectUndo(go, "Create r2_hum_audio");
                humObj = go.transform;
                humObj.SetParent(r2, false);
            }
            var humSrc = ConfigureAudioSource(humObj != null ? humObj.gameObject : null, null);
            if (humSrc != null) humSrc.loop = true;

            Transform skyAudio = bs.Find("bs_sky_audio");
            var windSrc = ConfigureAudioSource(skyAudio != null ? skyAudio.gameObject : null, null);

            var transition = GetOrAdd<BlueSkyTransitionController>(bs.gameObject);
            var soTrans = new SerializedObject(transition);
            soTrans.FindProperty("room2Lights").objectReferenceValue = room2Lights;
            var propSky = soTrans.FindProperty("skyLights");
            propSky.arraySize = 2;
            propSky.GetArrayElementAtIndex(0).objectReferenceValue = skyAmb;
            propSky.GetArrayElementAtIndex(1).objectReferenceValue = skyPlat;
            soTrans.FindProperty("fluorescentHum").objectReferenceValue = humSrc;
            soTrans.FindProperty("wind").objectReferenceValue = windSrc;
            soTrans.ApplyModifiedPropertiesWithoutUndo();

            Transform c2 = root.Find("Connector2");
            Transform blocker = c2?.Find("c2_blocker");
            if (blocker == null && c2 != null)
            {
                var go = new GameObject("c2_blocker");
                Undo.RegisterCreatedObjectUndo(go, "Create c2_blocker");
                blocker = go.transform;
                blocker.SetParent(c2, false);
                blocker.localPosition = new Vector3(0f, 1.5f, 0f);
                var col = go.AddComponent<BoxCollider>();
                col.size = new Vector3(0.5f, 3f, 5.5f);
                col.isTrigger = false;
                go.SetActive(false);
            }

            var ending = SetupEndingPresentation(root, bs);
            return (transition, ending, blocker != null ? blocker.gameObject : null);
        }

        private static BlueSkyEndingController SetupEndingPresentation(Transform root, Transform bs)
        {
            Transform stairsStart = bs.Find("bs_stairs_start");
            if (stairsStart == null)
            {
                var go = new GameObject("bs_stairs_start");
                Undo.RegisterCreatedObjectUndo(go, "Create bs_stairs_start");
                stairsStart = go.transform;
                stairsStart.SetParent(bs, false);
            }
            stairsStart.position = new Vector3(-83.76f, 0f, 96.0f);
            stairsStart.rotation = Quaternion.identity;

            Transform playerMark = bs.Find("bs_player_mark");
            if (playerMark != null) playerMark.position = new Vector3(-83.76f, 2.55f, 107.0f);
            Transform cameraMark = bs.Find("bs_camera_mark");

            if (playerMark != null && cameraMark != null)
            {
                Undo.RecordObject(playerMark, "Orient Player Mark");
                Vector3 toCam = cameraMark.position - playerMark.position;
                toCam.y = 0f;
                if (toCam.sqrMagnitude > 0.01f) playerMark.rotation = Quaternion.LookRotation(toCam.normalized, Vector3.up);

                Undo.RecordObject(cameraMark, "Orient Camera Mark");
                Vector3 toPlayer = playerMark.position - cameraMark.position;
                toPlayer.y = 0f;
                if (toPlayer.sqrMagnitude > 0.01f) cameraMark.rotation = Quaternion.LookRotation(toPlayer.normalized, Vector3.up);
            }

            CinemachineCamera cmCam = null;
            if (cameraMark != null)
            {
                cmCam = cameraMark.GetComponent<CinemachineCamera>();
                if (cmCam == null)
                {
                    cmCam = Undo.AddComponent<CinemachineCamera>(cameraMark.gameObject);
                    cmCam.Priority = 10;
                    cmCam.enabled = false;
                }
            }

            Canvas endCanvas = SetupEndingCanvas(root);

            var ending = GetOrAdd<BlueSkyEndingController>(bs.gameObject);
            var soEnding = new SerializedObject(ending);
            soEnding.FindProperty("stairsStartMarker").objectReferenceValue = stairsStart;
            soEnding.FindProperty("poseMarker").objectReferenceValue = playerMark;
            soEnding.FindProperty("endingCamera").objectReferenceValue = cmCam;
            soEnding.FindProperty("endCanvas").objectReferenceValue = endCanvas;
            soEnding.FindProperty("walkState").stringValue = "Walk";
            soEnding.FindProperty("presentationState").stringValue = "CelebrateBow";
            soEnding.FindProperty("enterFadeSeconds").floatValue = 0.4f;
            soEnding.FindProperty("walkSeconds").floatValue = 14.5f;
            soEnding.FindProperty("pauseBeforeBowSeconds").floatValue = 0.5f;
            soEnding.FindProperty("animationSeconds").floatValue = 3.75f;
            soEnding.FindProperty("holdSeconds").floatValue = 1.5f;
            soEnding.FindProperty("whiteFadeSeconds").floatValue = 1.5f;
            soEnding.ApplyModifiedPropertiesWithoutUndo();

            return ending;
        }

        private static Canvas SetupEndingCanvas(Transform root)
        {
            Transform canvasTransform = root.Find("TrueEnding_EndCanvas") ?? root.Find("EndingC_EndCanvas");
            if (canvasTransform == null)
            {
                var canvasGo = new GameObject("TrueEnding_EndCanvas");
                Undo.RegisterCreatedObjectUndo(canvasGo, "Create TrueEnding_EndCanvas");
                canvasTransform = canvasGo.transform;
                canvasTransform.SetParent(root, false);

                var canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 32760;
                canvasGo.AddComponent<CanvasScaler>();
                canvasGo.AddComponent<GraphicRaycaster>();
                canvasGo.SetActive(false);
                return canvas;
            }

            var existingCanvas = canvasTransform.GetComponent<Canvas>();
            canvasTransform.gameObject.SetActive(false);
            return existingCanvas;
        }

        private static void WireGameplayController(
            Transform root,
            TrueEndingGameplayController controller,
            Room1DontLookBackController r1,
            Room2MovementChallenge r2,
            BlueSkyTransitionController trans,
            BlueSkyEndingController ending,
            GameObject blocker)
        {
            Transform r1Spawn = root.Find("Room1/r1_spawn");
            Transform r2Spawn = root.Find("Room2/r2_spawn");
            var chase = UnityEngine.Object.FindFirstObjectByType<BackroomGameplayManager>();

            var so = new SerializedObject(controller);
            so.FindProperty("backroomChase").objectReferenceValue = chase;
            so.FindProperty("room1").objectReferenceValue = r1;
            so.FindProperty("room2").objectReferenceValue = r2;
            so.FindProperty("room1Checkpoint").objectReferenceValue = r1Spawn;
            so.FindProperty("room2Checkpoint").objectReferenceValue = r2Spawn;
            so.FindProperty("blueSkyTransition").objectReferenceValue = trans;
            so.FindProperty("ending").objectReferenceValue = ending;
            so.FindProperty("blueSkyBacktrackBlocker").objectReferenceValue = blocker;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var comp = go.GetComponent<T>();
            if (comp == null) comp = Undo.AddComponent<T>(go);
            return comp;
        }

        private static Transform FindTarget(string path)
        {
            var go = GameObject.Find(path);
            return go != null ? go.transform : null;
        }
    }
}
