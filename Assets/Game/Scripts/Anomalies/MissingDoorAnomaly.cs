using UnityEngine;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class MissingDoorAnomaly : AnomalyBehaviour
    {
        [SerializeField] private GameObject door;
        [SerializeField] private GameObject replacementWall;

        public void Configure(GameObject doorObject, GameObject wallReplacement)
        {
            door = doorObject;
            replacementWall = wallReplacement;
        }

        protected override void OnActivate()
        {
            if (door != null) door.SetActive(false);
            if (replacementWall != null) replacementWall.SetActive(true);
        }

        protected override void OnResetAnomaly()
        {
            if (door != null) door.SetActive(true);
            if (replacementWall != null) replacementWall.SetActive(false);
        }
    }
}
