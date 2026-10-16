using System.Collections;
using Zero.AnomalyPrototype.Interaction;
using Zero.AnomalyPrototype.Player;
using UnityEngine;

namespace Zero.AnomalyPrototype.Flow
{
    [DisallowMultipleComponent]
    public sealed class OfficeExitDoor : MonoBehaviour, IInteractable
    {
        [SerializeField] private Transform doorPivot;
        [SerializeField] private float openAngle = 92f;
        [SerializeField, Min(0.1f)] private float openDuration = 0.65f;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip openClip;

        private Quaternion closedRotation;
        private bool isOpen;
        private SourceSoundPlayback soundPlayback;

        public string InteractionPrompt => isOpen ? string.Empty : "MỞ CỬA CẦU THANG";

        public void Configure(Transform pivot, AudioSource source = null, AudioClip clip = null)
        {
            doorPivot = pivot;
            audioSource = source;
            openClip = clip;
            closedRotation = doorPivot != null ? doorPivot.localRotation : Quaternion.identity;
        }

        private void Awake()
        {
            if (doorPivot == null)
            {
                doorPivot = transform;
            }

            closedRotation = doorPivot.localRotation;
            openClip = GameAudioBootstrap.ResolveDefaultClip(openClip, "clip.Door_Open");
        }

        public void Interact(PlayerInteraction interactor)
        {
            if (!isOpen)
            {
                StartCoroutine(OpenDoor());
            }
        }

        private IEnumerator OpenDoor()
        {
            isOpen = true;
            if (audioSource != null && openClip != null)
            {
                GameAudioBootstrap.BindSource(ref soundPlayback, audioSource).Play(null, openClip);
            }
            Quaternion target = closedRotation * Quaternion.Euler(0f, openAngle, 0f);
            float elapsed = 0f;

            while (elapsed < openDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / openDuration);
                t = t * t * (3f - 2f * t);
                doorPivot.localRotation = Quaternion.Slerp(closedRotation, target, t);
                yield return null;
            }

            doorPivot.localRotation = target;
            Debug.Log("OFFICE_STAIRWELL_DOOR_OPENED", this);
        }
    }
}
