using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using Zero.AnomalyPrototype.Player;

namespace Zero.AnomalyPrototype.Flow
{
    public sealed class BackroomCheckpointLoader
    {
        private readonly string sceneName;
        public bool Succeeded { get; private set; }

        public BackroomCheckpointLoader(string sceneName)
        {
            this.sceneName = sceneName;
        }

        public IEnumerator Restore(FirstPersonController player)
        {
            Succeeded = false;
            if (player == null || !Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"CHECKPOINT_BACKROOM_CONFIG_INVALID: scene={sceneName}");
                yield break;
            }

            Scene oldScene = player.gameObject.scene;
            AsyncOperation load = StartLoading(sceneName);
            if (load == null) yield break;
            while (!load.isDone) yield return null;

            Scene scene = SceneManager.GetSceneByName(sceneName);
            if (!TryPrepareRoom(scene, out BackroomGameplayManager chase))
            {
                Debug.LogError("CHECKPOINT_BACKROOM_CONFIG_INVALID: controllers or root missing");
                yield return UnloadIfLoaded(scene);
                yield break;
            }

            yield return chase.PrepareNavigation();
            Transform respawn = chase.RespawnPoint;
            if (!chase.IsReady || respawn == null)
            {
                Debug.LogError("CHECKPOINT_BACKROOM_CONFIG_INVALID: navigation or respawn failed");
                yield return UnloadIfLoaded(scene);
                yield break;
            }

            player.transform.SetParent(null, true);
            SceneManager.MoveGameObjectToScene(player.gameObject, scene);
            MoveOldFilmVolume(oldScene, scene);
            SceneManager.SetActiveScene(scene);
            PlayerDeathController death = player.GetComponent<PlayerDeathController>();
            if (death != null) death.enabled = false;
            player.ResetLocomotionModifiers();
            player.Teleport(respawn.position, respawn.rotation);
            Physics.SyncTransforms();
            yield return SceneManager.UnloadSceneAsync(oldScene);
            Succeeded = true;
            Debug.Log("CHECKPOINT_BACKROOM_RESTORED", player);
        }

        private static AsyncOperation StartLoading(string name)
        {
            try { return SceneManager.LoadSceneAsync(name, LoadSceneMode.Additive); }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
                return null;
            }
        }

        private static bool TryPrepareRoom(Scene scene, out BackroomGameplayManager chase)
        {
            chase = null;
            return TryGetControllers(scene, out BackroomSceneController room, out chase) &&
                   room.PrepareStandalone();
        }

        private static IEnumerator UnloadIfLoaded(Scene scene)
        {
            if (scene.IsValid() && scene.isLoaded)
                yield return SceneManager.UnloadSceneAsync(scene);
        }

        private static bool TryGetControllers(Scene scene, out BackroomSceneController room,
            out BackroomGameplayManager chase)
        {
            room = null;
            chase = null;
            if (!scene.IsValid() || !scene.isLoaded) return false;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (BackroomSceneController item in root.GetComponentsInChildren<BackroomSceneController>(true))
                {
                    if (room != null) return false;
                    room = item;
                }
                foreach (BackroomGameplayManager item in root.GetComponentsInChildren<BackroomGameplayManager>(true))
                {
                    if (chase != null) return false;
                    chase = item;
                }
            }
            return room != null && chase != null;
        }

        private static void MoveOldFilmVolume(Scene oldScene, Scene destination)
        {
            if (!oldScene.IsValid() || !oldScene.isLoaded) return;
            foreach (GameObject root in oldScene.GetRootGameObjects())
            {
                if (!root.name.Contains("OldFilm")) continue;
                if (root.GetComponent<Volume>() == null) continue;
                SceneManager.MoveGameObjectToScene(root, destination);
                return;
            }
        }
    }
}
