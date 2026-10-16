using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Zero.AnomalyPrototype.Flow
{
    [CreateAssetMenu(menuName = "Zero/Audio/Sound Catalog")]
    public sealed class SoundCatalog : ScriptableObject
    {
        [SerializeField] private AudioMixer mixer;
        [SerializeField] private AudioMixerGroup master;
        [SerializeField] private AudioMixerGroup music;
        [SerializeField] private AudioMixerGroup sfx;
        [SerializeField] private AudioMixerGroup ambience;
        [SerializeField] private List<SoundDefinition> sounds = new List<SoundDefinition>();

        public AudioMixer Mixer => mixer;
        public IReadOnlyList<SoundDefinition> Sounds => sounds;

        public AudioMixerGroup GetGroup(AudioBus bus)
        {
            switch (bus)
            {
                case AudioBus.Master: return master;
                case AudioBus.Music: return music;
                case AudioBus.Ambience: return ambience;
                default: return sfx;
            }
        }

        public void Configure(AudioMixer audioMixer, AudioMixerGroup[] groups, List<SoundDefinition> definitions)
        {
            mixer = audioMixer;
            master = System.Array.Find(groups, group => group.name == "Master");
            music = System.Array.Find(groups, group => group.name == "Music");
            sfx = System.Array.Find(groups, group => group.name == "SFX");
            ambience = System.Array.Find(groups, group => group.name == "Ambience");
            sounds = new List<SoundDefinition>(definitions);
        }
    }
}
