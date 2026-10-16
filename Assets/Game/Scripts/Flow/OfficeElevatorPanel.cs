using System.Collections;
using Zero.AnomalyPrototype.Interaction;
using Zero.AnomalyPrototype.Player;
using UnityEngine;

namespace Zero.AnomalyPrototype.Flow
{
    [DisallowMultipleComponent]
    public sealed class OfficeElevatorPanel : MonoBehaviour, IInteractable
    {
        [SerializeField] private OfficeSequenceController sequenceController;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip errorClip;
        [SerializeField, Min(0.1f)] private float cooldown = 1f;

        private bool coolingDown;
        private SourceSoundPlayback soundPlayback;

        public string InteractionPrompt => "GỌI THANG MÁY";

        public void Configure(OfficeSequenceController sequence, AudioSource source = null, AudioClip clip = null)
        {
            sequenceController = sequence;
            audioSource = source;
            errorClip = clip;
        }

        public void Interact(PlayerInteraction interactor)
        {
            if (coolingDown)
            {
                return;
            }

            coolingDown = true;
            sequenceController?.ShowTemporaryMessage("OUT OF SERVICE", 2.2f);
            sequenceController?.PlayElevatorError();
            if (audioSource != null && errorClip != null)
            {
                GameAudioBootstrap.BindSource(ref soundPlayback, audioSource).Play(null, errorClip);
            }

            StartCoroutine(ResetCooldown());
            Debug.Log("OFFICE_ELEVATOR_OUT_OF_SERVICE", this);
        }

        private IEnumerator ResetCooldown()
        {
            yield return new WaitForSecondsRealtime(cooldown);
            coolingDown = false;
        }
    }
}
