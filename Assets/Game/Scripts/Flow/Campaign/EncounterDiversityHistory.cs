using System;
using System.Collections.Generic;

namespace Zero.AnomalyPrototype.Flow
{
    /// <summary>Persists across attempts; previewing reserve content never records it.</summary>
    public sealed class EncounterDiversityHistory
    {
        private readonly List<string> encounters = new List<string>();
        private readonly HashSet<string> encountered = new HashSet<string>(StringComparer.Ordinal);
        public bool HasEncountered(string id) => encountered.Contains(id ?? string.Empty);

        public bool IsRecent(string id)
        {
            int first = Math.Max(0, encounters.Count - 2);
            for (int i = first; i < encounters.Count; i++)
                if (encounters[i] == id) return true;
            return false;
        }

        public void Record(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return;
            encountered.Add(id);
            encounters.Add(id);
            // Unique coverage + the last two entries are sufficient; avoid unbounded saves.
            if (encounters.Count > 256) Compact();
        }

        public string[] Capture() => encounters.ToArray();

        public void Restore(IEnumerable<string> ids)
        {
            encounters.Clear();
            encountered.Clear();
            if (ids == null) return;
            foreach (string id in ids) Record(id);
        }

        private void Compact()
        {
            string penultimate = encounters[encounters.Count - 2];
            string last = encounters[encounters.Count - 1];
            encounters.Clear();
            encounters.AddRange(encountered);
            encounters.Add(penultimate);
            encounters.Add(last);
        }
    }
}
