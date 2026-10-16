using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Zero.AnomalyPrototype.Flow
{
    public sealed class TitleGraphicsSettingsPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Dropdown resolutionDropdown;
        [SerializeField] private TMP_Dropdown qualityDropdown;
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private Toggle vSyncToggle;
        private readonly List<Vector2Int> resolutions = new List<Vector2Int>();
        private GameSettingsService settings;

        private void OnEnable()
        {
            ConfigureToggleLabels();
            settings = GameFlowController.Instance != null ? GameFlowController.Instance.Settings : null;
            if (settings == null) return;
            BuildResolutionOptions();
            BuildQualityOptions();
            if (fullscreenToggle != null) fullscreenToggle.SetIsOnWithoutNotify(settings.FullScreen);
            if (vSyncToggle != null) vSyncToggle.SetIsOnWithoutNotify(settings.VSync);
            if (resolutionDropdown != null) resolutionDropdown.onValueChanged.AddListener(OnResolution);
            if (qualityDropdown != null) qualityDropdown.onValueChanged.AddListener(OnQuality);
            if (fullscreenToggle != null) fullscreenToggle.onValueChanged.AddListener(OnFullscreen);
            if (vSyncToggle != null) vSyncToggle.onValueChanged.AddListener(OnVSync);
        }

        private void ConfigureToggleLabels()
        {
            SetupToggleLabel(fullscreenToggle, "FULLSCREEN (Toàn màn hình)");
            SetupToggleLabel(vSyncToggle, "VSYNC (Chống xé hình / Khóa FPS)");
        }

        private static void SetupToggleLabel(Toggle toggle, string text)
        {
            if (toggle == null) return;
            TMP_Text label = toggle.GetComponentInChildren<TMP_Text>();
            if (label == null) return;
            label.text = text;
            label.color = new Color(0.88f, 0.88f, 0.84f, 1f);
            label.fontSize = 18f;
        }

        private void OnDisable()
        {
            if (resolutionDropdown != null) resolutionDropdown.onValueChanged.RemoveListener(OnResolution);
            if (qualityDropdown != null) qualityDropdown.onValueChanged.RemoveListener(OnQuality);
            if (fullscreenToggle != null) fullscreenToggle.onValueChanged.RemoveListener(OnFullscreen);
            if (vSyncToggle != null) vSyncToggle.onValueChanged.RemoveListener(OnVSync);
        }

        private void BuildResolutionOptions()
        {
            if (resolutionDropdown == null) return;
            resolutions.Clear();
            Resolution[] available = Screen.resolutions;
            for (int i = 0; i < available.Length; i++)
                AddResolution(available[i].width, available[i].height);
            AddResolution(settings.ResolutionWidth, settings.ResolutionHeight);
            var labels = new List<string>(resolutions.Count);
            int selected = 0;
            for (int i = 0; i < resolutions.Count; i++)
            {
                Vector2Int size = resolutions[i];
                labels.Add($"{size.x} × {size.y}");
                if (size.x == settings.ResolutionWidth && size.y == settings.ResolutionHeight)
                    selected = i;
            }
            resolutionDropdown.ClearOptions();
            resolutionDropdown.AddOptions(labels);
            resolutionDropdown.SetValueWithoutNotify(selected);
            resolutionDropdown.RefreshShownValue();
        }

        private void AddResolution(int width, int height)
        {
            if (width < 640 || height < 480) return;
            Vector2Int size = new Vector2Int(width, height);
            if (!resolutions.Contains(size)) resolutions.Add(size);
        }

        private void BuildQualityOptions()
        {
            if (qualityDropdown == null) return;
            qualityDropdown.ClearOptions();
            qualityDropdown.AddOptions(new List<string>(QualitySettings.names));
            qualityDropdown.SetValueWithoutNotify(settings.QualityLevel);
            qualityDropdown.RefreshShownValue();
        }

        private void OnResolution(int index)
        {
            if (settings == null || index < 0 || index >= resolutions.Count) return;
            Vector2Int size = resolutions[index];
            settings.SetResolution(size.x, size.y);
        }

        private void OnQuality(int index) => settings?.SetQualityLevel(index);
        private void OnFullscreen(bool enabled) => settings?.SetFullScreen(enabled);
        private void OnVSync(bool enabled) => settings?.SetVSync(enabled);
    }
}
