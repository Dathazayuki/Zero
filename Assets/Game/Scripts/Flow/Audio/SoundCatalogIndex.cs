using System;
using System.Collections.Generic;
using UnityEngine;

namespace Zero.AnomalyPrototype.Flow
{
    public sealed class SoundCatalogIndex
    {
        private readonly Dictionary<string, SoundDefinition> byId = new Dictionary<string, SoundDefinition>(StringComparer.Ordinal);
        private readonly Dictionary<AudioClip, SoundDefinition> byClip = new Dictionary<AudioClip, SoundDefinition>();

        public bool Add(SoundDefinition definition)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.Id) || byId.ContainsKey(definition.Id)) return false;
            byId.Add(definition.Id, definition);
            AudioClip original = definition.LegacyClip != null ? definition.LegacyClip : definition.Clip;
            if (original != null)
            {
                if (!byClip.TryGetValue(original, out SoundDefinition existing)) byClip.Add(original, definition);
                else if (IsGenerated(existing) && !IsGenerated(definition)) byClip[original] = definition;
            }
            return true;
        }

        public SoundDefinition Resolve(string id, AudioClip fallback = null)
        {
            if (!string.IsNullOrEmpty(id)) return byId.TryGetValue(id, out SoundDefinition named) ? named : null;
            return fallback != null && byClip.TryGetValue(fallback, out SoundDefinition mapped) ? mapped : null;
        }

        private static bool IsGenerated(SoundDefinition sound) => sound.Id.StartsWith("clip.", StringComparison.Ordinal);
    }
}
