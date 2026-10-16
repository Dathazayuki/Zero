using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using Zero.AnomalyPrototype.Flow;

namespace Zero.Editor
{
    public static class GameAudioCatalogTools
    {
        private const string CatalogPath = "Assets/Game/Resources/Audio/GameSoundCatalog.asset";
        private const string MixerPath = "Assets/Game/Audio/MX_ZeroGame.mixer";

        [MenuItem("Tools/Zero/Audio/Create Or Extend Sound Catalog")]
        private static void CreateCatalog()
        {
            AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            if (mixer == null) { Debug.LogError("AUDIO_MIXER_MISSING"); return; }
            AudioMixerGroup[] groups = mixer.FindMatchingGroups("");
            if (!HasGroup(groups, "Master") || !HasGroup(groups, "Music") ||
                !HasGroup(groups, "SFX") || !HasGroup(groups, "Ambience")) return;
            EnsureFolder("Assets/Game", "Resources");
            EnsureFolder("Assets/Game/Resources", "Audio");
            SoundCatalog catalog = AssetDatabase.LoadAssetAtPath<SoundCatalog>(CatalogPath);
            bool isNew = catalog == null;
            if (isNew) catalog = ScriptableObject.CreateInstance<SoundCatalog>();
            var entries = new List<SoundDefinition>(catalog.Sounds);
            AddProjectClips(entries);
            AddDefaults(entries);
            Undo.RecordObject(catalog, "Configure Sound Catalog");
            catalog.Configure(mixer, groups, entries);
            if (isNew) AssetDatabase.CreateAsset(catalog, CatalogPath);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssetIfDirty(catalog);
            Selection.activeObject = catalog;
            Debug.Log($"AUDIO_CATALOG_READY: entries={entries.Count}, path={CatalogPath}", catalog);
        }

        private static bool HasGroup(AudioMixerGroup[] groups, string name)
        {
            if (System.Array.Exists(groups, group => group.name == name)) return true;
            Debug.LogError($"AUDIO_MIXER_GROUP_MISSING: group={name}");
            return false;
        }

        private static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + name)) AssetDatabase.CreateFolder(parent, name);
        }

        private static void AddProjectClips(List<SoundDefinition> entries)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Game" }))
            {
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guid));
                if (entries.Exists(sound => sound.Clip == clip)) continue;
                string id = "clip." + clip.name;
                if (entries.Exists(sound => sound.Id == id)) id += "." + guid.Substring(0, 8);
                entries.Add(new SoundDefinition(id, clip, AudioBus.Sfx));
            }
        }

        private static void AddDefaults(List<SoundDefinition> entries)
        {
            Add(entries, "player.footstep", "SoundWalk.mp3", AudioBus.Sfx);
            Add(entries, "player.breathing", "RunBreath2.wav", AudioBus.Sfx);
            Add(entries, "office.door", "Door_Open.wav", AudioBus.Sfx);
            Add(entries, "office.ambience", null, AudioBus.Ambience);
            Add(entries, "office.typing", null, AudioBus.Sfx);
            Add(entries, "office.confirmation", null, AudioBus.Sfx);
            Add(entries, "office.chair", null, AudioBus.Sfx);
            Add(entries, "office.elevator_error", null, AudioBus.Sfx);
            Add(entries, "office.transition", null, AudioBus.Sfx);
        }

        private static void Add(List<SoundDefinition> entries, string id, string file, AudioBus bus)
        {
            if (entries.Exists(sound => sound.Id == id)) return;
            AudioClip clip = file == null ? null : AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/Audio/" + file);
            entries.Add(new SoundDefinition(id, clip, bus));
        }

        [MenuItem("Tools/Zero/Audio/Audit Loaded Sources")]
        private static void Audit()
        {
            AudioSource[] sources = Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int unrouted = 0;
            int longRange = 0;
            foreach (AudioSource source in sources)
            {
                if (source.GetComponentInParent<AudioPlaybackService>() != null) continue;
                if (source.playOnAwake && source.clip == null)
                    Debug.LogWarning($"AUDIO_PLAY_ON_AWAKE_EMPTY: object={source.name}", source);
                if (source.outputAudioMixerGroup == null)
                {
                    unrouted++;
                    Debug.LogWarning($"AUDIO_UNROUTED: object={source.name}, clip={source.clip?.name ?? "<dynamic>"}, " +
                        $"playOnAwake={source.playOnAwake}", source);
                }
                if (source.spatialBlend > 0.5f && source.maxDistance > 30f)
                {
                    longRange++;
                    Debug.LogWarning($"AUDIO_LONG_RANGE: object={source.name}, maxDistance={source.maxDistance:F1}m", source);
                }
            }
            Debug.Log($"AUDIO_AUDIT: sources={sources.Length}, unrouted={unrouted}, longRange={longRange}");
        }
    }
}
