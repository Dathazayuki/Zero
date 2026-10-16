using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using Zero.EnvironmentLoop;
using Zero.AnomalyPrototype.Player;

namespace Zero.AnomalyPrototype.Flow
{
    public sealed partial class GameFlowController : MonoBehaviour
    {
        [SerializeField] private string backroomSceneName = "BackRoomScene";
        private Coroutine backroomRoutine;
        private EndFloorElevatorController backroomSource;
        private BackroomSceneController backroomController;
        private bool backroomCancelled;
        private bool backroomHandoff;
        public bool IsBackroomReady => Campaign?.IsActive == true ? Campaign.IsDestinationReady :
            !backroomCancelled && backroomController != null && backroomController.IsPrepared;

        public bool BeginBackroomRide(EndFloorElevatorController source)
        {
            if (Campaign?.IsActive == true) return Campaign.BeginRide(source);
            if (source == null || IsTransitioning || endingTransitionActive ||
                Phase != GameFlowPhase.LoopGameplay ||
                !Application.CanStreamedLevelBeLoaded(backroomSceneName) ||
                SceneManager.GetSceneByName(backroomSceneName).isLoaded) return false;
            backroomSource = source;
            backroomCancelled = false;
            backroomHandoff = false;
            backroomRoutine = StartCoroutine(LoadBackroom());
            return true;
        }

        private IEnumerator LoadBackroom()
        {
            // Let the fully closed cabin be presented before starting integration.
            yield return null;
            if (backroomCancelled || backroomSource == null)
            {
                backroomRoutine = null;
                yield break;
            }
            AsyncOperation loading = null;
            try { loading = SceneManager.LoadSceneAsync(backroomSceneName, LoadSceneMode.Additive); }
            catch (System.Exception exception) { Debug.LogException(exception, this); }
            if (loading == null)
            {
                FailBackroom();
                yield break;
            }
            Debug.Log("BACKROOM_LOAD_STARTED", this);
            // Never park allowSceneActivation=false: this would block Office/reset loads.
            while (!loading.isDone) yield return null;
            Scene scene = SceneManager.GetSceneByName(backroomSceneName);
            int matches = 0;
            if (scene.IsValid() && scene.isLoaded)
                foreach (GameObject root in scene.GetRootGameObjects())
                    foreach (BackroomSceneController controller in root.GetComponentsInChildren<BackroomSceneController>(true))
                    {
                        backroomController = controller;
                        matches++;
                    }
            if (backroomCancelled || backroomSource == null || matches != 1 ||
                !backroomSource.IsRideActive || !backroomController.AlignTo(backroomSource.ArrivalAnchor))
            {
                if (backroomController != null) backroomController.HideEnvironment();
                if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
                FailBackroom();
                yield break;
            }

            // Prepare navigation after alignment, while the cabin is still closed.
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (var chase in root.GetComponentsInChildren<BackroomGameplayManager>(true))
                    yield return chase.PrepareNavigation();

            // Perform scene handoff while elevator doors are closed:
            // 1. Move Elevator_Cabin to BackroomScene
            // 2. Move Player to BackroomScene
            // 3. Move OldFilmGlobalVolume to BackroomScene
            // 4. Set BackroomScene active and unload old scene (InfiniteStairwayScene and all of EndFloor_PF_Segment)
            yield return HandoffCabinAndPlayerToBackroom(scene);

            // Allow multiple frames for hierarchy activation, physics sync and initial GPU render setup
            for (int i = 0; i < 5; i++) yield return null;
            Physics.SyncTransforms();
            if (backroomCancelled || backroomSource == null || !backroomSource.IsRideActive)
            {
                backroomController.HideEnvironment();
                yield return SceneManager.UnloadSceneAsync(scene);
                FailBackroom();
                yield break;
            }
            Debug.Log("BACKROOM_SCENE_READY", this);
            backroomRoutine = null;
        }

        private IEnumerator HandoffCabinAndPlayerToBackroom(Scene newScene)
        {
            if (backroomSource == null || !newScene.IsValid() || !newScene.isLoaded) yield break;

            Scene oldScene = backroomSource.gameObject.scene;

            // 1. Move Elevator_Cabin to newScene
            GameObject cabin = backroomSource.gameObject;
            cabin.transform.SetParent(null, true);
            SceneManager.MoveGameObjectToScene(cabin, newScene);
            Debug.Log("ELEVATOR_CABIN_MOVED_TO_BACKROOM", cabin);

            // 2. Move Player to newScene
            FirstPersonController player = backroomSource.Player;
            if (player != null)
            {
                player.transform.SetParent(null, true);
                SceneManager.MoveGameObjectToScene(player.gameObject, newScene);
                var death = player.GetComponent<PlayerDeathController>();
                if (death != null) death.enabled = false;
                Debug.Log("PLAYER_MOVED_TO_BACKROOM", player);
            }

            // 3. Find and move OldFilmGlobalVolume to newScene
            GameObject oldFilm = null;
            if (oldScene.IsValid() && oldScene.isLoaded)
            {
                foreach (GameObject root in oldScene.GetRootGameObjects())
                {
                    if (root.name.Equals("OldFilmGlobalVolume", System.StringComparison.OrdinalIgnoreCase) ||
                        root.name.Contains("OldFilm", System.StringComparison.OrdinalIgnoreCase))
                    {
                        oldFilm = root;
                        break;
                    }
                }
            }

            if (oldFilm == null)
            {
                var volumes = Object.FindObjectsByType<Volume>(FindObjectsSortMode.None);
                foreach (var v in volumes)
                {
                    if (v != null && v.isGlobal && (v.name.Contains("OldFilm", System.StringComparison.OrdinalIgnoreCase) ||
                        (v.profile != null && v.profile.name.Contains("OldFilm", System.StringComparison.OrdinalIgnoreCase))))
                    {
                        oldFilm = v.gameObject;
                        break;
                    }
                }
            }

            if (oldFilm != null)
            {
                oldFilm.transform.SetParent(null, true);
                SceneManager.MoveGameObjectToScene(oldFilm, newScene);
                Debug.Log("OLDFILM_VOLUME_MOVED_TO_BACKROOM", oldFilm);
            }

            // 4. Set BackroomScene as active scene (REQUIRED before unloading oldScene)
            SceneManager.SetActiveScene(newScene);

            // 5. Complete manager run state & unbind loop completion before oldScene unloads
            if (boundAnomalyManager != null)
            {
                boundAnomalyManager.CompleteRunFromBackroomExit();
                UnbindLoopCompletion();
            }

            backroomHandoff = true;

            // 6. Unload oldScene (InfiniteStairwayScene and the entire EndFloor_PF_Segment)
            if (oldScene.IsValid() && oldScene.isLoaded && oldScene != newScene)
            {
                Debug.Log($"UNLOADING_OLD_SCENE: {oldScene.name}", this);
                yield return SceneManager.UnloadSceneAsync(oldScene);
                Debug.Log("OLD_LOOP_SCENE_UNLOADED_DURING_ELEVATOR_RIDE", this);
            }
        }

        private void FailBackroom()
        {
            var source = backroomSource;
            bool notify = !backroomCancelled;
            backroomRoutine = null;
            backroomController = null;
            backroomSource = null;
            if (notify)
            {
                Debug.LogError("BACKROOM_LOAD_FAILED", this);
                if (source != null) source.RecoverFailedRide();
            }
        }

        public bool CommitBackroomHandoff(FirstPersonController player)
        {
            if (Campaign?.IsActive == true) return Campaign.CommitRide(player);
            if (player == null || !IsBackroomReady || backroomRoutine != null || backroomSource == null ||
                backroomSource.Player != player || backroomSource.State != ElevatorEndingState.PlayerExited) return false;
            SetPhase(GameFlowPhase.BackroomGameplay);
            Debug.Log("BACKROOM_GAMEPLAY_STARTED", this);
            return true;
        }

        public bool FinishBackroomHandoff(FirstPersonController player)
        {
            if (Phase != GameFlowPhase.BackroomGameplay || !IsBackroomReady || backroomSource == null || backroomSource.Player != player) return false;
            Debug.Log("BACKROOM_HANDOFF_FINISHED", this);
            return true;
        }

        public void CancelBackroomTransition()
        {
            Campaign?.CancelTravel();
            if (backroomHandoff) return;
            backroomCancelled = true;
            if (backroomController != null) backroomController.HideEnvironment();
            // Unity scene loads cannot be cancelled: drain then unload in the coroutine.
            if (backroomRoutine == null && backroomController != null)
                backroomRoutine = StartCoroutine(DiscardBackroom());
        }

        private IEnumerator DiscardBackroom()
        {
            Scene scene = backroomController.gameObject.scene;
            backroomController = null;
            backroomSource = null;
            if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            backroomRoutine = null;
        }
    }
}
