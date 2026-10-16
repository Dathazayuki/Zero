using UnityEngine;

namespace Zero.AnomalyPrototype.Flow
{
    public sealed class BackroomSceneController : MonoBehaviour
    {
        [SerializeField] private Transform backroomRoot;
        [SerializeField] private Transform arrivalAnchor;
        [SerializeField] private GameObject destinationFacade;
        public bool IsPrepared { get; private set; }

        public bool PrepareStandalone()
        {
            if (backroomRoot == null || arrivalAnchor == null ||
                !arrivalAnchor.IsChildOf(backroomRoot)) return false;
            backroomRoot.gameObject.SetActive(true);
            IsPrepared = true;
            Debug.Log("BACKROOM_STANDALONE_PREPARED", this);
            return true;
        }

        public bool AlignTo(Transform cabinAnchor)
        {
            if (backroomRoot == null || arrivalAnchor == null || cabinAnchor == null ||
                !arrivalAnchor.IsChildOf(backroomRoot)) return false;
            // Authored arrival forward faces INTO the cabin; cabin forward faces OUT.
            Quaternion rotation = Quaternion.LookRotation(-cabinAnchor.forward, Vector3.up) *
                                  Quaternion.Inverse(arrivalAnchor.rotation);
            backroomRoot.rotation = rotation * backroomRoot.rotation;
            backroomRoot.position += cabinAnchor.position - arrivalAnchor.position;
            if (destinationFacade != null) destinationFacade.SetActive(false);
            backroomRoot.gameObject.SetActive(true);
            Physics.SyncTransforms();
            IsPrepared = true;
            Debug.Log("BACKROOM_ALIGNED", this);
            return true;
        }

        public void ShowDestinationFacade()
        {
            if (destinationFacade != null) destinationFacade.SetActive(true);
        }

        public void HideEnvironment()
        {
            if (backroomRoot != null) backroomRoot.gameObject.SetActive(false);
            IsPrepared = false;
        }
    }
}
