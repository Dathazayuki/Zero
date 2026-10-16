using System.Collections;
using UnityEngine;
using Zero.EnvironmentLoop;
using Zero.AnomalyPrototype.Player;

namespace Zero.AnomalyPrototype.Flow
{
    public sealed class CampaignLevelBootstrap : MonoBehaviour
    {
        [SerializeField] private Transform environmentRoot;
        [SerializeField] private Transform arrivalAnchor;
        [SerializeField] private Transform entranceCheckpoint;
        [SerializeField] private GameObject destinationFacade;
        [SerializeField] private MonoBehaviour minigameSession;
        [SerializeField] private InfiniteBuildingManager building;
        [SerializeField] private EndFloorElevatorController exitElevator;
        public bool IsReady { get; private set; }
        public Transform Entrance => entranceCheckpoint;
        public IMinigameSession Session => minigameSession as IMinigameSession;
        public InfiniteBuildingManager Building => building;
        public EndFloorElevatorController ExitElevator => exitElevator;
        public EncounterDiversityHistory RoomHistory { get; private set; }

        public bool AlignTo(Transform cabinAnchor)
        {
            if (environmentRoot == null || arrivalAnchor == null || cabinAnchor == null ||
                !arrivalAnchor.IsChildOf(environmentRoot)) return false;
            Quaternion delta = Quaternion.LookRotation(-cabinAnchor.forward, Vector3.up) *
                Quaternion.Inverse(arrivalAnchor.rotation);
            environmentRoot.rotation = delta * environmentRoot.rotation;
            environmentRoot.position += cabinAnchor.position - arrivalAnchor.position;
            if (destinationFacade != null) destinationFacade.SetActive(false);
            Physics.SyncTransforms();
            return true;
        }

        public IEnumerator Prepare(CampaignRuntime campaign, FirstPersonController player, bool infinityRoom)
        {
            IsReady = false;
            if (environmentRoot == null || entranceCheckpoint == null || player == null) yield break;
            DisableScenePlayers(player);
            environmentRoot.gameObject.SetActive(true);
            if (infinityRoom)
            {
                var bootstrap = gameObject.AddComponent<CampaignRoomBootstrap>();
                RoomHistory = new EncounterDiversityHistory();
                IsReady = bootstrap.Initialize(campaign.DestinationRoom, building, player, RoomHistory);
                if (IsReady) RebindDeath(player);
            }
            else if (Session != null)
            {
                yield return Session.Prepare();
                IsReady = Session.IsReady;
                if (IsReady) Session.Begin(new MinigameContext(player, campaign.Flow.FadeController, entranceCheckpoint));
            }
            Physics.SyncTransforms();
        }

        private void DisableScenePlayers(FirstPersonController actual)
        {
            foreach (GameObject root in gameObject.scene.GetRootGameObjects())
                foreach (FirstPersonController candidate in root.GetComponentsInChildren<FirstPersonController>(true))
                    if (candidate != actual) candidate.gameObject.SetActive(false);
        }

        private void RebindDeath(FirstPersonController player)
        {
            PlayerDeathController death = player.GetComponent<PlayerDeathController>();
            if (death != null) death.BindCampaignWorld(building.StairLoopManager, building.AnomalyGameManager, entranceCheckpoint);
        }

        public void HideEnvironment()
        {
            IsReady = false;
            if (environmentRoot != null) environmentRoot.gameObject.SetActive(false);
        }
    }
}
