using UnityEngine;
using UnityEngine.SceneManagement;

namespace Zero.AnomalyPrototype.Flow
{
    public sealed class GameAudioBootstrap : MonoBehaviour
    {
        private AudioPlaybackService playback;
        private GameSettingsService settings;
        private SoundCatalog catalog;

        // Resolve once during composition; authored clips always take precedence.
        public static AudioClip ResolveDefaultClip(AudioClip authored, string id)
        {
            if (authored != null) return authored;
            SoundCatalog defaults = Resources.Load<SoundCatalog>("Audio/GameSoundCatalog");
            if (defaults == null) return null;
            foreach (SoundDefinition sound in defaults.Sounds)
                if (sound != null && sound.Id == id) return sound.Clip;
            return null;
        }

        // Legacy composition seam: resolves the existing flow only when a source is first bound.
        public static SourceSoundPlayback BindSource(ref SourceSoundPlayback binding, AudioSource source,
            AudioBus bus = AudioBus.Sfx, bool spatial = true)
        {
            AudioPlaybackService service = GameFlowController.Instance != null ? GameFlowController.Instance.Audio : null;
            if (binding != null && binding.Matches(source, service)) return binding;
            binding = new SourceSoundPlayback(source, service, bus, spatial);
            return binding;
        }

        public AudioPlaybackService Initialize(GameSettingsService settingsService)
        {
            settings = settingsService;
            catalog = Resources.Load<SoundCatalog>("Audio/GameSoundCatalog");
            playback = gameObject.AddComponent<AudioPlaybackService>();
            playback.Initialize(catalog);
            if (catalog == null) Debug.LogError("AUDIO_CONFIGURATION_INVALID: missing Audio/GameSoundCatalog", this);
            return playback;
        }

        private void Start()
        {
            if (catalog != null) settings.BindMixer(catalog.Mixer);
            for (int i = 0; i < SceneManager.sceneCount; i++) BindScene(SceneManager.GetSceneAt(i), LoadSceneMode.Additive);
        }

        private void OnEnable() => SceneManager.sceneLoaded += BindScene;
        private void OnDisable() => SceneManager.sceneLoaded -= BindScene;

        private void BindScene(Scene scene, LoadSceneMode mode)
        {
            if (playback == null || !scene.IsValid() || !scene.isLoaded) return;
            foreach (GameObject root in scene.GetRootGameObjects()) BindHierarchy(root);
        }

        public void BindHierarchy(GameObject root)
        {
            if (root == null || playback == null) return;
            foreach (AudioEmitter emitter in root.GetComponentsInChildren<AudioEmitter>(true)) emitter.Bind(playback);
            foreach (AudioSource source in root.GetComponentsInChildren<AudioSource>(true)) RouteLegacySource(source);
        }

        private void RouteLegacySource(AudioSource source)
        {
            if (source.outputAudioMixerGroup != null) return;
            SoundDefinition sound = playback.Resolve(null, source.clip);
            // Generated clip.* entries are inventory, not a trustworthy bus assignment.
            if (sound != null && !sound.Id.StartsWith("clip.", System.StringComparison.Ordinal))
                playback.Route(source, sound.Bus);
        }
    }
}
