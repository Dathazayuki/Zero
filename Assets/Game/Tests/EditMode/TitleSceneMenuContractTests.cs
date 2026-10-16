using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using Zero.AnomalyPrototype.Flow;

namespace Zero.Tests.EditMode
{
    public sealed class TitleSceneMenuContractTests
    {
        private const string ScenePath = "Assets/Game/Scenes/TitleScene.unity";

        [Test]
        public void TitleScene_HasCompleteSettingsAndExitBindings()
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedForTest = !scene.IsValid() || !scene.isLoaded;
            if (openedForTest) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                TitleMenuController menu = FindInScene<TitleMenuController>(scene);
                Assert.NotNull(menu, "TitleMenuController is missing.");
                AssertReferences(menu, "newGameButton", "continueButton", "settingsButton",
                    "exitButton", "mainPanel", "settingsPanel", "confirmationPanel");
                Assert.That(menu.transform.localScale, Is.EqualTo(Vector3.one),
                    "TitleCanvas scale must remain visible and interactable.");
                AssertPanelDefaults(menu);
                AssertReferences(FindInScene<TitleSettingsPanel>(scene), "backButton");
                AssertReferences(FindInScene<TitleExitConfirmPanel>(scene), "confirmButton", "cancelButton");
                AssertReferences(FindInScene<TitleGraphicsSettingsPanel>(scene), "resolutionDropdown",
                    "qualityDropdown", "fullscreenToggle", "vSyncToggle");
                VerifyAudioSettings(scene);
            }
            finally
            {
                if (openedForTest) EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void AssertPanelDefaults(TitleMenuController menu)
        {
            SerializedObject data = new SerializedObject(menu);
            GameObject main = data.FindProperty("mainPanel").objectReferenceValue as GameObject;
            GameObject settings = data.FindProperty("settingsPanel").objectReferenceValue as GameObject;
            GameObject confirmation = data.FindProperty("confirmationPanel").objectReferenceValue as GameObject;
            Assert.IsTrue(main.activeSelf, "Title main panel must start active.");
            Assert.IsFalse(settings.activeSelf, "Title settings panel must start inactive.");
            Assert.IsFalse(confirmation.activeSelf, "Title exit panel must start inactive.");
        }

        private static void VerifyAudioSettings(Scene scene)
        {
            TitleAudioSettingsPanel panel = FindInScene<TitleAudioSettingsPanel>(scene);
            AssertReferences(panel, "audioMixer", "masterSlider", "musicSlider", "sfxSlider",
                "ambienceSlider");
            SerializedObject panelData = new SerializedObject(panel);
            AudioMixer mixer = panelData.FindProperty("audioMixer").objectReferenceValue as AudioMixer;
            Assert.NotNull(mixer, "Title audio mixer is missing.");
            AssertExposedParameters(mixer, "MasterVolume", "MusicVolume", "SfxVolume", "AmbienceVolume");
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T component = root.GetComponentInChildren<T>(true);
                if (component != null) return component;
            }

            return null;
        }

        private static void AssertReferences(Component component, params string[] propertyNames)
        {
            Assert.NotNull(component, "TitleScene contract owner is missing.");
            SerializedObject data = new SerializedObject(component);
            foreach (string propertyName in propertyNames)
            {
                SerializedProperty property = data.FindProperty(propertyName);
                Assert.NotNull(property, $"Serialized property '{propertyName}' is missing on {component.name}.");
                Assert.NotNull(property.objectReferenceValue,
                    $"{component.name}.{propertyName} must be assigned in TitleScene.");
            }
        }

        private static void AssertExposedParameters(AudioMixer mixer, params string[] expectedNames)
        {
            SerializedProperty parameters = new SerializedObject(mixer).FindProperty("m_ExposedParameters");
            Assert.NotNull(parameters, "AudioMixer exposed parameter data is missing.");
            var names = new HashSet<string>();
            for (int i = 0; i < parameters.arraySize; i++)
                names.Add(parameters.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue);
            foreach (string expectedName in expectedNames)
                Assert.That(names, Does.Contain(expectedName), $"AudioMixer must expose '{expectedName}'.");
        }
    }
}
