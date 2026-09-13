using Zero.AnomalyPrototype.Interaction;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Zero.AnomalyPrototype.Player
{
    public sealed class PlayerInteraction : MonoBehaviour
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField, Min(0.5f)] private float interactionDistance = 3f;
        [SerializeField] private LayerMask interactionMask = ~0;
        private bool inputEnabled = true;

        public string CurrentPrompt { get; private set; } = string.Empty;
        public bool InputEnabled => inputEnabled;

        private void Update()
        {
            if (!inputEnabled)
            {
                CurrentPrompt = string.Empty;
                return;
            }

            IInteractable interactable = FindFocusedInteractable();
            CurrentPrompt = interactable != null ? interactable.InteractionPrompt : string.Empty;

            Keyboard keyboard = Keyboard.current;
            if (interactable != null && keyboard != null && keyboard.eKey.wasPressedThisFrame)
            {
                interactable.Interact(this);
            }
        }

        public void Configure(Camera cameraReference, float distance = 3f)
        {
            playerCamera = cameraReference;
            interactionDistance = distance;
        }

        public void SetInputEnabled(bool enabled)
        {
            inputEnabled = enabled;
            if (!enabled)
            {
                CurrentPrompt = string.Empty;
            }
        }

        private IInteractable FindFocusedInteractable()
        {
            if (playerCamera == null || !Physics.Raycast(
                    playerCamera.transform.position,
                    playerCamera.transform.forward,
                    out RaycastHit hit,
                    interactionDistance,
                    interactionMask,
                    QueryTriggerInteraction.Ignore))
            {
                return null;
            }

            Transform current = hit.transform;
            while (current != null)
            {
                MonoBehaviour[] behaviours = current.GetComponents<MonoBehaviour>();
                for (int i = 0; i < behaviours.Length; i++)
                {
                    if (behaviours[i] is IInteractable found)
                    {
                        return found;
                    }
                }

                current = current.parent;
            }

            return null;
        }
    }
}
