using System;
using Zero.AnomalyPrototype.Anomalies;
using Zero.AnomalyPrototype.Core;
using Zero.AnomalyPrototype.Demo;
using Zero.AnomalyPrototype.HorrorEvents;
using Zero.AnomalyPrototype.Loop;
using Zero.AnomalyPrototype.Utilities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Zero.AnomalyPrototype.Editor
{
    public static class AnomalyDemoSmokeValidator
    {
        [MenuItem("Tools/Anomaly Demo/Run Smoke Validation")]
        public static void ValidateFromMenu()
        {
            Validate();
        }

        private static void Validate()
        {
            Scene previousActive = SceneManager.GetActiveScene();
            Scene validationScene = default;
            GameObject root = null;
            try
            {
                validationScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                root = new GameObject("AnomalyDemoSmokeValidation");
                SceneManager.MoveGameObjectToScene(root, validationScene);
                AnomalyDemoBootstrap bootstrap = root.AddComponent<AnomalyDemoBootstrap>();
                bootstrap.BuildForValidation();

                AnomalyBehaviour[] anomalies = root.GetComponentsInChildren<AnomalyBehaviour>(true);
                HorrorEventBehaviour[] events = root.GetComponentsInChildren<HorrorEventBehaviour>(true);
                NormalStateSnapshot[] snapshots = root.GetComponentsInChildren<NormalStateSnapshot>(true);
                Require(anomalies.Length == 6, $"Expected 6 anomaly modules, found {anomalies.Length}.");
                Require(events.Length == 3, $"Expected 3 horror event modules, found {events.Length}.");
                Require(snapshots.Length >= 10, "Normal-state coverage is missing from environment props.");

                for (int i = 0; i < anomalies.Length; i++)
                {
                    Require(anomalies[i].Data != null, $"{anomalies[i].name} has no AnomalyData.");
                    anomalies[i].Activate();
                    anomalies[i].ResetAnomaly();
                }

                LoopManager loop = root.GetComponentInChildren<LoopManager>(true);
                ProgressManager progress = root.GetComponentInChildren<ProgressManager>(true);
                AudioManager audio = root.GetComponentInChildren<AudioManager>(true);
                Require(loop != null && progress != null && audio != null, "Core managers were not composed.");
                Require(loop.CurrentState != null, "Initial loop was not prepared.");
                Require(audio.GetFallbackFootsteps() != null, "Null AudioClip fallback was not generated.");

                loop.ForceNormalNextLoop();
                LoopChoice deliberatelyWrong = loop.CurrentState.HasAnomaly ? LoopChoice.Normal : LoopChoice.Anomaly;
                loop.SubmitChoice(deliberatelyWrong);
                Require(progress.CurrentProgress == 0 && !loop.CurrentState.HasAnomaly,
                    "Incorrect result or forced normal loop failed.");

                loop.ForceAnomalyNextLoop();
                loop.SubmitChoice(LoopChoice.Normal);
                Require(progress.CurrentProgress == 1 && loop.CurrentState.HasAnomaly,
                    "Normal + NORMAL did not increment or forced anomaly failed.");

                loop.ForceNormalNextLoop();
                loop.SubmitChoice(LoopChoice.Normal);
                Require(progress.CurrentProgress == 0 && !loop.CurrentState.HasAnomaly,
                    "Anomaly + NORMAL did not reset progress.");

                loop.ForceAnomalyNextLoop();
                loop.SubmitChoice(LoopChoice.Anomaly);
                Require(progress.CurrentProgress == 0 && loop.CurrentState.HasAnomaly,
                    "Normal + ANOMALY did not reset progress.");

                loop.ForceNormalNextLoop();
                loop.SubmitChoice(LoopChoice.Anomaly);
                Require(progress.CurrentProgress == 1 && !loop.CurrentState.HasAnomaly,
                    "Anomaly + ANOMALY did not increment progress.");

                loop.ForceAnomalyNextLoop();
                loop.SubmitChoice(LoopChoice.Normal);
                string firstAnomaly = loop.CurrentState.ActiveAnomalyName;
                loop.ForceAnomalyNextLoop();
                loop.SubmitChoice(LoopChoice.Anomaly);
                string secondAnomaly = loop.CurrentState.ActiveAnomalyName;
                Require(firstAnomaly != secondAnomaly, "Recent anomaly history allowed an immediate repeat.");

                while (!loop.CurrentState.IsExitLoop)
                {
                    loop.ForceNormalNextLoop();
                    loop.SubmitChoice(loop.CurrentState.HasAnomaly ? LoopChoice.Anomaly : LoopChoice.Normal);
                }

                Require(progress.CurrentProgress >= 5 && loop.CurrentState.IsExitLoop,
                    "Five correct decisions did not prepare the exit loop.");
                Debug.Log("ANOMALY_DEMO_SMOKE_PASS: composition, 6 anomalies, 3 horror events, four decision cases, history, reset calls, audio fallback, and exit loop validated.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                throw;
            }
            finally
            {
                if (root != null)
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }

                if (validationScene.IsValid() && validationScene.isLoaded)
                {
                    EditorSceneManager.CloseScene(validationScene, true);
                }

                if (previousActive.IsValid() && previousActive.isLoaded)
                {
                    SceneManager.SetActiveScene(previousActive);
                }
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
