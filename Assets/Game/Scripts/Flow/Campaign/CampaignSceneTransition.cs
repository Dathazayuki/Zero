using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using Zero.EnvironmentLoop;
using Zero.AnomalyPrototype.Player;

namespace Zero.AnomalyPrototype.Flow
{
    /// <summary>One additive operation at a time. Cancellation drains Unity's non-cancellable load.</summary>
    public sealed class CampaignSceneTransition : MonoBehaviour
    {
        private Coroutine routine;
        private int generation;
        private EndFloorElevatorController cabin;
        public bool IsBusy => routine != null;
        public bool IsReady { get; private set; }
        public FirstPersonController Player { get; private set; }

        public bool Begin(CampaignRuntime campaign, EndFloorElevatorController source, string scene, bool infinity)
        {
            if (IsBusy || source == null || source.Player == null || SceneManager.GetSceneByName(scene).isLoaded) return false;
            cabin = source;
            Player = source.Player;
            IsReady = false;
            routine = StartCoroutine(Travel(campaign, scene, infinity, ++generation));
            return true;
        }

        private IEnumerator Travel(CampaignRuntime campaign, string name, bool infinity, int token)
        {
            yield return null; // Present closed doors before activating the destination.
            AsyncOperation operation = StartLoad(name);
            if (operation == null) { Fail(token); yield break; }
            while (!operation.isDone) yield return null;
            Scene scene = SceneManager.GetSceneByName(name);
            CampaignLevelBootstrap level = FindLevel(scene);
            if (!ValidRide(token) || level == null || !level.AlignTo(cabin.ArrivalAnchor))
            { yield return Discard(scene); Fail(token); yield break; }
            yield return level.Prepare(campaign, Player, infinity);
            if (!ValidRide(token) || !level.IsReady)
            { yield return Discard(scene); Fail(token); yield break; }
            yield return null;
            yield return null;
            if (!ValidRide(token) || !campaign.AcceptArrival(level, infinity))
            { yield return Discard(scene); Fail(token); yield break; }
            Scene old = cabin.gameObject.scene;
            MoveRoot(cabin.gameObject, scene);
            MovePlayer(Player, scene, infinity);
            MoveFilm(old, scene);
            SceneManager.SetActiveScene(scene);
            yield return Discard(old);
            Physics.SyncTransforms();
            IsReady = token == generation;
            routine = null;
            Debug.Log("CAMPAIGN_DESTINATION_READY", level);
        }

        public IEnumerator Restore(CampaignRuntime campaign, FirstPersonController player, string name)
        {
            if (IsBusy || player == null) yield break;
            cabin = null;
            IsReady = false;
            Player = player;
            routine = StartCoroutine(RestoreLoaded(campaign, player, name, ++generation));
            yield return routine;
            routine = null;
        }

        private IEnumerator RestoreLoaded(CampaignRuntime campaign, FirstPersonController player, string name, int token)
        {
            Scene old = player.gameObject.scene;
            AsyncOperation operation = StartLoad(name);
            if (operation == null) yield break;
            while (!operation.isDone) yield return null;
            Scene scene = SceneManager.GetSceneByName(name);
            CampaignLevelBootstrap level = FindLevel(scene);
            if (token != generation || level == null) { yield return Discard(scene); yield break; }
            yield return level.Prepare(campaign, player, false);
            if (token != generation || !level.IsReady) { yield return Discard(scene); yield break; }
            MovePlayer(player, scene, false);
            MoveFilm(old, scene);
            player.ResetLocomotionModifiers();
            player.Teleport(level.Entrance.position, level.Entrance.rotation);
            SceneManager.SetActiveScene(scene);
            campaign.AcceptRestoredLevel(level);
            yield return Discard(old);
            Physics.SyncTransforms();
            IsReady = token == generation;
        }

        public void Cancel() { generation++; IsReady = false; }

        private bool ValidRide(int token) => token == generation && cabin != null && cabin.IsRideActive && Player != null;

        private void Fail(int token)
        {
            IsReady = false;
            routine = null;
            if (token != generation) return;
            Debug.LogError("CAMPAIGN_LOAD_FAILED", this);
            if (cabin != null) cabin.RecoverFailedRide();
        }

        private static AsyncOperation StartLoad(string name)
        {
            try { return SceneManager.LoadSceneAsync(name, LoadSceneMode.Additive); }
            catch (System.Exception exception) { Debug.LogException(exception); return null; }
        }

        private static CampaignLevelBootstrap FindLevel(Scene scene)
        {
            CampaignLevelBootstrap result = null;
            if (!scene.IsValid() || !scene.isLoaded) return null;
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (CampaignLevelBootstrap candidate in root.GetComponentsInChildren<CampaignLevelBootstrap>(true))
                {
                    if (result != null) return null;
                    result = candidate;
                }
            return result;
        }

        private static IEnumerator Discard(Scene scene)
        {
            if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
        }

        private static void MoveRoot(GameObject root, Scene scene)
        {
            root.transform.SetParent(null, true);
            SceneManager.MoveGameObjectToScene(root, scene);
        }

        private static void MovePlayer(FirstPersonController player, Scene scene, bool infinity)
        {
            MoveRoot(player.gameObject, scene);
            PlayerDeathController death = player.GetComponent<PlayerDeathController>();
            if (death != null) death.enabled = infinity;
            player.ClearElevatorRideFeedback();
        }

        private static void MoveFilm(Scene old, Scene scene)
        {
            foreach (GameObject root in old.GetRootGameObjects())
                if (root.name.Contains("OldFilm") && root.GetComponent<Volume>() != null)
                    MoveRoot(root, scene);
        }

        private void OnDisable() => Cancel();
    }
}
