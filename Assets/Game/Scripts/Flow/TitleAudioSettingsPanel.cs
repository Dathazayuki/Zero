using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace Zero.AnomalyPrototype.Flow
{
    public sealed class TitleAudioSettingsPanel : MonoBehaviour
    {
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private Slider ambienceSlider;
        private GameSettingsService settings;

        private void OnEnable()
        {
            settings = GameFlowController.Instance != null ? GameFlowController.Instance.Settings : null;
            if (settings == null) return;
            settings.BindMixer(audioMixer);
            SetSlider(masterSlider, AudioBus.Master);
            SetSlider(musicSlider, AudioBus.Music);
            SetSlider(sfxSlider, AudioBus.Sfx);
            SetSlider(ambienceSlider, AudioBus.Ambience);
            if (masterSlider != null) masterSlider.onValueChanged.AddListener(OnMaster);
            if (musicSlider != null) musicSlider.onValueChanged.AddListener(OnMusic);
            if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(OnSfx);
            if (ambienceSlider != null) ambienceSlider.onValueChanged.AddListener(OnAmbience);
        }

        private void OnDisable()
        {
            if (masterSlider != null) masterSlider.onValueChanged.RemoveListener(OnMaster);
            if (musicSlider != null) musicSlider.onValueChanged.RemoveListener(OnMusic);
            if (sfxSlider != null) sfxSlider.onValueChanged.RemoveListener(OnSfx);
            if (ambienceSlider != null) ambienceSlider.onValueChanged.RemoveListener(OnAmbience);
        }

        private void SetSlider(Slider slider, AudioBus bus)
        {
            if (slider == null) return;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.SetValueWithoutNotify(settings.GetVolume(bus));
        }

        private void OnMaster(float value) => settings?.SetVolume(AudioBus.Master, value);
        private void OnMusic(float value) => settings?.SetVolume(AudioBus.Music, value);
        private void OnSfx(float value) => settings?.SetVolume(AudioBus.Sfx, value);
        private void OnAmbience(float value) => settings?.SetVolume(AudioBus.Ambience, value);
    }
}
