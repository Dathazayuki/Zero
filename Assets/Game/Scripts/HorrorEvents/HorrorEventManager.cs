using System.Collections.Generic;
using UnityEngine;

namespace Zero.AnomalyPrototype.HorrorEvents
{
    public sealed class HorrorEventManager : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float eventChance = 0.55f;

        private readonly List<HorrorEventBehaviour> events = new List<HorrorEventBehaviour>();
        private readonly List<HorrorEventBehaviour> eligible = new List<HorrorEventBehaviour>();
        private HorrorEventBehaviour previous;

        public HorrorEventBehaviour Current { get; private set; }

        public void Initialize(IReadOnlyList<HorrorEventBehaviour> registeredEvents, float chance = 0.55f)
        {
            ResetCurrent();
            events.Clear();
            if (registeredEvents != null)
            {
                for (int i = 0; i < registeredEvents.Count; i++)
                {
                    HorrorEventBehaviour horrorEvent = registeredEvents[i];
                    if (horrorEvent != null && !events.Contains(horrorEvent))
                    {
                        events.Add(horrorEvent);
                    }
                }
            }

            eventChance = Mathf.Clamp01(chance);
            previous = null;
        }

        public HorrorEventBehaviour SelectAndArm()
        {
            ResetCurrent();
            if (events.Count == 0 || Random.value > eventChance)
            {
                return null;
            }

            eligible.Clear();
            for (int i = 0; i < events.Count; i++)
            {
                if (events[i] != null && events[i] != previous)
                {
                    eligible.Add(events[i]);
                }
            }

            if (eligible.Count == 0)
            {
                eligible.AddRange(events);
            }

            Current = WeightedPick(eligible);
            if (Current != null)
            {
                Current.Arm();
                previous = Current;
            }

            return Current;
        }

        public void TryPlayCurrent()
        {
            if (Current != null)
            {
                Current.Play();
            }
        }

        public void ResetCurrent()
        {
            if (Current != null)
            {
                Current.ResetEvent();
                Current = null;
            }
        }

        private static HorrorEventBehaviour WeightedPick(List<HorrorEventBehaviour> pool)
        {
            float totalWeight = 0f;
            for (int i = 0; i < pool.Count; i++)
            {
                totalWeight += Mathf.Max(0.01f, pool[i].SelectionWeight);
            }

            if (totalWeight <= 0f)
            {
                return null;
            }

            float roll = Random.value * totalWeight;
            for (int i = 0; i < pool.Count; i++)
            {
                roll -= Mathf.Max(0.01f, pool[i].SelectionWeight);
                if (roll <= 0f)
                {
                    return pool[i];
                }
            }

            return pool[pool.Count - 1];
        }
    }
}
