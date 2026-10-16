using UnityEngine;
using UnityEngine.Audio;

namespace Zero.AnomalyPrototype.Flow
{
    [DisallowMultipleComponent]
    public sealed class GameSettingsService : MonoBehaviour
    {
        private const string Prefix = "Zero.Settings.";
        private readonly float[] volumes = new float[4];
        private AudioMixer mixer;
        private int resolutionWidth;
        private int resolutionHeight;
        private bool fullScreen;
        private bool vSync;
        private int qualityLevel;

        public int ResolutionWidth => resolutionWidth;
        public int ResolutionHeight => resolutionHeight;
        public bool FullScreen => fullScreen;
        public bool VSync => vSync;
        public int QualityLevel => qualityLevel;

        private void Awake()
        {
            resolutionWidth = PlayerPrefs.GetInt(Prefix + "Width", Screen.width);
            resolutionHeight = PlayerPrefs.GetInt(Prefix + "Height", Screen.height);
            fullScreen = PlayerPrefs.GetInt(Prefix + "Fullscreen", Screen.fullScreen ? 1 : 0) != 0;
            vSync = PlayerPrefs.GetInt(Prefix + "VSync", QualitySettings.vSyncCount > 0 ? 1 : 0) != 0;
            qualityLevel = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "Quality",
                QualitySettings.GetQualityLevel()), 0, QualitySettings.names.Length - 1);
            for (int i = 0; i < volumes.Length; i++)
                volumes[i] = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey((AudioBus)i), 1f));
            AudioListener.volume = volumes[(int)AudioBus.Master];
            ApplyVideo();
        }

        public void BindMixer(AudioMixer audioMixer)
        {
            if (audioMixer == null)
            {
                Debug.LogWarning("SETTINGS_AUDIO_MIXER_MISSING", this);
                return;
            }
            mixer = audioMixer;
            AudioListener.volume = 1f;
            for (int i = 0; i < volumes.Length; i++) ApplyVolume((AudioBus)i);
        }

        public float GetVolume(AudioBus bus) => volumes[(int)bus];

        public void SetVolume(AudioBus bus, float value)
        {
            int index = (int)bus;
            if (index < 0 || index >= volumes.Length) return;
            volumes[index] = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(VolumeKey(bus), volumes[index]);
            ApplyVolume(bus);
        }

        public void SetQualityLevel(int value)
        {
            qualityLevel = Mathf.Clamp(value, 0, QualitySettings.names.Length - 1);
            QualitySettings.SetQualityLevel(qualityLevel, true);
            PlayerPrefs.SetInt(Prefix + "Quality", qualityLevel);
        }

        public void SetVSync(bool enabled)
        {
            vSync = enabled;
            QualitySettings.vSyncCount = enabled ? 1 : 0;
            PlayerPrefs.SetInt(Prefix + "VSync", enabled ? 1 : 0);
        }

        public void SetFullScreen(bool enabled)
        {
            fullScreen = enabled;
            ApplyResolution();
            PlayerPrefs.SetInt(Prefix + "Fullscreen", enabled ? 1 : 0);
        }

        public void SetResolution(int width, int height)
        {
            if (width < 640 || height < 480) return;
            resolutionWidth = width;
            resolutionHeight = height;
            ApplyResolution();
            PlayerPrefs.SetInt(Prefix + "Width", width);
            PlayerPrefs.SetInt(Prefix + "Height", height);
        }

        public void Flush() => PlayerPrefs.Save();

        private void OnApplicationQuit() => Flush();

        private void ApplyVideo()
        {
            QualitySettings.SetQualityLevel(qualityLevel, true);
            QualitySettings.vSyncCount = vSync ? 1 : 0;
            if (PlayerPrefs.HasKey(Prefix + "Width")) ApplyResolution();
        }

        private void ApplyResolution()
        {
            FullScreenMode mode = fullScreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            Screen.SetResolution(resolutionWidth, resolutionHeight, mode);
        }

        private void ApplyVolume(AudioBus bus)
        {
            if (bus == AudioBus.Master && mixer == null)
                AudioListener.volume = volumes[(int)AudioBus.Master];
            if (mixer == null) return;
            float value = volumes[(int)bus];
            float decibels = value <= 0.0001f ? -80f : 20f * Mathf.Log10(value);
            if (!mixer.SetFloat(ParameterName(bus), decibels))
                Debug.LogError($"SETTINGS_AUDIO_PARAMETER_MISSING: bus={bus}", this);
        }

        private static string VolumeKey(AudioBus bus) => Prefix + "Volume." + bus;

        private static string ParameterName(AudioBus bus)
        {
            switch (bus)
            {
                case AudioBus.Music: return "MusicVolume";
                case AudioBus.Sfx: return "SfxVolume";
                case AudioBus.Ambience: return "AmbienceVolume";
                default: return "MasterVolume";
            }
        }
    }
}
