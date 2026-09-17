using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zero.AnomalyPrototype.Anomalies;
using Zero.AnomalyPrototype.Player;
using Zero.EnvironmentLoop;

namespace Zero.EditorTools
{
    public static class SeaOfBloodTools
    {
        [MenuItem("Tools/Zero/Sea of Blood/Create Missing Assets")]
        public static void CreateMissingAssets()
        {
            const string dataPath = "Assets/Game/ScriptableObjects/Anomalies/A18_SeaOfBlood.asset";
            if (AssetDatabase.LoadMainAssetAtPath(dataPath) == null)
            {
                var data = ScriptableObject.CreateInstance<AnomalyData>();
                data.Configure("environment.sea_of_blood", "Sea of Blood", AnomalyType.Environment,
                    AnomalyPacingTier.MajorAnomaly, 3, 0.65f, true,
                    "Blood rises on current-room entry. Commit to another floor within 12 seconds or suffocate.");
                AssetDatabase.CreateAsset(data, dataPath);
                AssetDatabase.SaveAssetIfDirty(data);
            }
            const string materialPath = "Assets/Game/Materials/M_SeaOfBlood.mat";
            var shader = Shader.Find("Zero/Sea Of Blood URP");
            if (shader == null) throw new InvalidOperationException("SeaOfBloodURP shader has not imported.");
            if (AssetDatabase.LoadMainAssetAtPath(materialPath) == null)
            {
                var material = new Material(shader);
                AssetDatabase.CreateAsset(material, materialPath);
                AssetDatabase.SaveAssetIfDirty(material);
            }
            Debug.Log("SEA_OF_BLOOD_ASSETS_READY: existing assets preserved; assign noise/normal textures and prefab references manually.");
        }

        [MenuItem("Tools/Zero/Sea of Blood/Validate Lifecycle (Isolated)")]
        public static void ValidateLifecycle()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Run Sea of Blood lifecycle validation outside Play Mode.");
                return;
            }
            Scene preview = EditorSceneManager.NewPreviewScene();
            AnomalyData data = null;
            try
            {
                var root = new GameObject("SeaOfBloodValidation");
                SceneManager.MoveGameObjectToScene(root, preview);
                var manager = Child(root, "Manager").AddComponent<StairLoopManager>();
                var floor = Child(root, "Floor").AddComponent<FloorSegment>();
                var otherFloor = Child(root, "OtherFloor").AddComponent<FloorSegment>();
                var player = Child(root, "Player");
                var movement = player.AddComponent<FirstPersonController>();
                player.AddComponent<PlayerDeathController>();
                var collider = player.GetComponent<CharacterController>();
                var anomaly = Child(root, "Blood").AddComponent<SeaOfBloodAnomaly>();
                var surface = Child(root, "Surface");
                var renderer = surface.AddComponent<MeshRenderer>();
                var start = Child(root, "Start").transform;
                var end = Child(root, "End").transform;
                end.localPosition = Vector3.up * 4f;
                var trigger = Child(root, "Trigger").AddComponent<SeaOfBloodStartTrigger>();
                var presentation = Child(root, "Presentation").AddComponent<AnomalyDeathPresentation>();
                Set(presentation, "motionStyle", DeathMotionStyle.Suffocation);
                Set(anomaly, "bloodSurface", surface.transform);
                Set(anomaly, "bloodRenderer", renderer);
                Set(anomaly, "bloodStartPoint", start);
                Set(anomaly, "bloodEndPoint", end);
                Set(anomaly, "entryTrigger", trigger);
                Set(anomaly, "deathPresentation", presentation);
                Set(anomaly, "scaleVolumeHeight", false);
                data = ScriptableObject.CreateInstance<AnomalyData>();
                // Rejection is intentional: this test must never run a real death/reset coroutine.
                data.Configure("test.blood", "Test", AnomalyType.Environment, 3, 1f, false, "");
                anomaly.Initialize(data);
                anomaly.BindPlayer(player.transform, null);
                anomaly.BindFloorContext(manager, floor);
                Set(manager, "<CurrentSegment>k__BackingField", otherFloor);
                anomaly.Activate();
                Check(!renderer.enabled && trigger.IsArmed, "reserve must stay hidden and armed");
                Check(!anomaly.TryStartRising(collider), "reserve cannot start countdown");
                Set(manager, "<CurrentSegment>k__BackingField", floor);
                Check(anomaly.TryStartRising(collider), "current entry must start");
                Check(!anomaly.TryStartRising(collider) && !trigger.IsArmed, "entry must consume once");
                Invoke(anomaly, "SetSurfaceProgress", 1f);
                Check(surface.transform.position == end.position, "surface must reach end marker");
                Set(anomaly, "elapsed", 12f);
                Set(manager, "<CurrentSegment>k__BackingField", otherFloor);
                Invoke(anomaly, "LateUpdate");
                Check(anomaly.State == SeaOfBloodAnomaly.FloodState.Escaped, "escape must win over deadline");
                Check(!renderer.enabled && surface.transform.position == start.position, "escape must clear surface");
                anomaly.ResetAnomaly();
                Set(manager, "<CurrentSegment>k__BackingField", floor);
                anomaly.Activate();
                Check(trigger.IsArmed && anomaly.TryStartRising(collider), "recycle must rearm");
                player.transform.position = Vector3.forward * 10f;
                Set(anomaly, "elapsed", 0.5f);
                Invoke(anomaly, "LateUpdate");
                Check(anomaly.State == SeaOfBloodAnomaly.FloodState.Escaped, "stepping outside cube collider must escape without floor change");
                player.transform.position = Vector3.zero;
                anomaly.ResetAnomaly();
                Set(manager, "<CurrentSegment>k__BackingField", floor);
                anomaly.Activate();
                Check(trigger.IsArmed && anomaly.TryStartRising(collider), "recycle must rearm");
                Set(anomaly, "elapsed", 12f);
                Invoke(anomaly, "LateUpdate");
                Check(anomaly.State == SeaOfBloodAnomaly.FloodState.DeathRequested, "timeout must request death");
                Check(!renderer.enabled && !trigger.IsArmed, "rejected death must stop effects");
                Invoke(anomaly, "LateUpdate");
                Check(player.GetComponent<PlayerDeathController>().State == PlayerDeathController.DeathState.Alive,
                    "CanKillPlayer=false must not lock Player");
                anomaly.ResetAnomaly();
                Check(!(bool)Get(movement, "forcedBreathing") && (float)Get(movement, "stumbleIntensity") == 0f,
                    "reset must clear owned effects");
                movement.SetForcedBreathing(true);
                movement.SetStumbleIntensity(0.2f);
                anomaly.ResetAnomaly();
                Check((bool)Get(movement, "forcedBreathing") && (float)Get(movement, "stumbleIntensity") == 0.2f,
                    "inactive reserve reset must not clear another effect owner");
                Check(presentation.MotionStyle == DeathMotionStyle.Suffocation && !presentation.HasCameraPose,
                    "Suffocation needs no anchor");
                Debug.Log("SEA_OF_BLOOD_VALIDATION_PASS: lifecycle assertions passed; physics, timing and visual death require Play Mode.");
            }
            catch (Exception exception)
            {
                Debug.LogError("SEA_OF_BLOOD_VALIDATION_FAIL: " + exception.GetBaseException().Message);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(preview);
                if (data != null) UnityEngine.Object.DestroyImmediate(data);
            }
        }

        private static GameObject Child(GameObject root, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(root.transform, false);
            return child;
        }
        private static void Set(object target, string field, object value) =>
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        private static object Get(object target, string field) =>
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        private static void Invoke(object target, string method, params object[] args) =>
            target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);
        private static void Check(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }
    }
}
