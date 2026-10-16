using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Zero.AnomalyPrototype.Flow
{
    public sealed class PauseSettingsView : MonoBehaviour
    {
        private readonly List<Vector2Int> resolutions = new List<Vector2Int>();
        private GameSettingsService settings;
        private TMP_Text resolutionLabel;
        private TMP_Text qualityLabel;
        private TMP_Text fullscreenLabel;
        private TMP_Text vSyncLabel;
        private readonly Slider[] volumes = new Slider[4];
        private readonly TMP_Text[] volumeLabels = new TMP_Text[4];
        private int resolutionIndex;
        public Button FirstButton { get; private set; }

        public void Build(GameSettingsService service)
        {
            settings = service;
            FirstButton = PauseMenuLayout.Button(transform, "", 235f, NextResolution);
            resolutionLabel = FirstButton.GetComponentInChildren<TMP_Text>();
            qualityLabel = AddOption(157f, NextQuality);
            fullscreenLabel = AddOption(79f, ToggleFullscreen);
            vSyncLabel = AddOption(1f, ToggleVSync);
            for (int i = 0; i < volumes.Length; i++) BuildVolume(i);
        }

        private TMP_Text AddOption(float y, UnityEngine.Events.UnityAction action)
        {
            return PauseMenuLayout.Button(transform, "", y, action).GetComponentInChildren<TMP_Text>();
        }

        private void BuildVolume(int index)
        {
            AudioBus bus = (AudioBus)index;
            volumes[index] = PauseMenuLayout.Volume(transform, bus.ToString().ToUpperInvariant(), -75f - index * 64f);
            volumeLabels[index] = volumes[index].transform.parent.GetComponentInChildren<TMP_Text>();
            volumes[index].onValueChanged.AddListener(value => ChangeVolume(index, value));
        }

        private void ChangeVolume(int index, float value)
        {
            settings.SetVolume((AudioBus)index, value);
            volumeLabels[index].text = $"{((AudioBus)index).ToString().ToUpperInvariant()}  {value:P0}";
        }

        public void Refresh()
        {
            resolutions.Clear();
            foreach (Resolution item in Screen.resolutions) AddResolution(new Vector2Int(item.width, item.height));
            Vector2Int current = new Vector2Int(settings.ResolutionWidth, settings.ResolutionHeight);
            AddResolution(current);
            resolutionIndex = resolutions.IndexOf(current);
            RefreshLabels();
            for (int i = 0; i < volumes.Length; i++)
            {
                float value = settings.GetVolume((AudioBus)i);
                volumes[i].SetValueWithoutNotify(value);
                volumeLabels[i].text = $"{((AudioBus)i).ToString().ToUpperInvariant()}  {value:P0}";
            }
        }

        private void AddResolution(Vector2Int size)
        {
            if (size.x >= 640 && size.y >= 480 && !resolutions.Contains(size)) resolutions.Add(size);
        }

        private void NextResolution()
        {
            if (resolutions.Count == 0) return;
            resolutionIndex = (resolutionIndex + 1) % resolutions.Count;
            Vector2Int size = resolutions[resolutionIndex];
            settings.SetResolution(size.x, size.y);
            RefreshLabels();
        }

        private void NextQuality()
        {
            settings.SetQualityLevel((settings.QualityLevel + 1) % QualitySettings.names.Length);
            RefreshLabels();
        }

        private void ToggleFullscreen() { settings.SetFullScreen(!settings.FullScreen); RefreshLabels(); }
        private void ToggleVSync() { settings.SetVSync(!settings.VSync); RefreshLabels(); }

        private void RefreshLabels()
        {
            resolutionLabel.text = $"RESOLUTION     {settings.ResolutionWidth} x {settings.ResolutionHeight}   >";
            qualityLabel.text = $"QUALITY     {QualitySettings.names[settings.QualityLevel]}   >";
            fullscreenLabel.text = $"FULLSCREEN     {(settings.FullScreen ? "ON" : "OFF")}   >";
            vSyncLabel.text = $"VSYNC     {(settings.VSync ? "ON" : "OFF")}   >";
        }

        private void OnDestroy()
        {
            foreach (Slider slider in volumes)
                if (slider != null) slider.onValueChanged.RemoveAllListeners();
        }
    }
}
