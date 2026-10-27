using System.Collections;
using UnityEngine;
using Zero.AnomalyPrototype.Player;

namespace Zero.AnomalyPrototype.Interaction
{
    public sealed class ApplianceDoor : MonoBehaviour, IInteractable
    {
        [SerializeField] private Transform doorPivot;
        [SerializeField] private Vector3 closedEuler;
        [SerializeField] private Vector3 openEuler = new Vector3(0f, 105f, 0f);
        [SerializeField, Min(0.05f)] private float duration = 0.65f;
        private enum DoorState { Closed, Opening, Open, Closing }
        private DoorState state;
        private Coroutine motion;
        private bool interactionEnabled = true;

        public bool IsOpen => state == DoorState.Open || state == DoorState.Opening;
        public bool InteractionEnabled => interactionEnabled;
        public string InteractionPrompt => !isActiveAndEnabled || !interactionEnabled ? string.Empty : IsOpen ? "[E] Close" : "[E] Open";

        public void SetInteractionEnabled(bool allowed) => interactionEnabled = allowed;

        public void Configure(Transform pivot, Vector3 rotation)
        {
            doorPivot = pivot;
            closedEuler = pivot.localEulerAngles;
            openEuler = rotation;
        }

        public void Interact(PlayerInteraction interactor)
        {
            if (isActiveAndEnabled && interactionEnabled) SetOpen(!IsOpen);
        }

        public void SetOpen(bool open) => SetOpen(open, duration);

        public void SetOpen(bool open, float seconds)
        {
            if (doorPivot == null || (open == IsOpen && seconds > 0f)) return;
            StopMotion();
            state = open ? DoorState.Opening : DoorState.Closing;
            if (!Application.isPlaying || !isActiveAndEnabled || seconds <= 0f)
            {
                Snap(open);
                return;
            }
            motion = StartCoroutine(Animate(open, seconds));
        }

        [ContextMenu("Preview/Open")]
        public void OpenImmediately() => Snap(true);

        [ContextMenu("Preview/Close")]
        public void ResetDoor() => Snap(false);

        private void OnDisable() => ResetDoor();

        private void StopMotion()
        {
            if (motion == null) return;
            StopCoroutine(motion);
            motion = null;
        }

        private void Snap(bool open)
        {
            StopMotion();
            if (doorPivot != null)
                doorPivot.localRotation = Quaternion.Euler(closedEuler) *
                    Quaternion.Euler(open ? openEuler : Vector3.zero);
            state = open ? DoorState.Open : DoorState.Closed;
        }

        private IEnumerator Animate(bool open, float seconds)
        {
            Quaternion start = doorPivot.localRotation;
            Quaternion end = Quaternion.Euler(closedEuler) * Quaternion.Euler(open ? openEuler : Vector3.zero);
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / Mathf.Max(0.05f, seconds)));
                doorPivot.localRotation = Quaternion.Slerp(start, end, t);
                yield return null;
            }
            doorPivot.localRotation = end;
            state = open ? DoorState.Open : DoorState.Closed;
            motion = null;
        }
    }
}
