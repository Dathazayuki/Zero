using System.Collections.Generic;
using UnityEngine;

namespace Zero.AnomalyPrototype.Utilities
{
    public sealed class NormalStateSnapshot : MonoBehaviour
    {
        private Vector3 localPosition;
        private Quaternion localRotation;
        private Vector3 localScale;
        private bool normalActiveState;
        private bool captured;

        private Renderer[] renderers;
        private bool[] rendererEnabledStates;
        private Material[][] rendererMaterials;
        private Light[] lights;
        private bool[] lightEnabledStates;
        private float[] lightIntensities;
        private Color[] lightColors;
        private AudioSource[] audioSources;
        private Animator[] animators;

        public void Capture()
        {
            localPosition = transform.localPosition;
            localRotation = transform.localRotation;
            localScale = transform.localScale;
            normalActiveState = gameObject.activeSelf;

            renderers = GetComponentsInChildren<Renderer>(true);
            rendererEnabledStates = new bool[renderers.Length];
            rendererMaterials = new Material[renderers.Length][];
            for (int i = 0; i < renderers.Length; i++)
            {
                rendererEnabledStates[i] = renderers[i].enabled;
                rendererMaterials[i] = renderers[i].sharedMaterials;
            }

            lights = GetComponentsInChildren<Light>(true);
            lightEnabledStates = new bool[lights.Length];
            lightIntensities = new float[lights.Length];
            lightColors = new Color[lights.Length];
            for (int i = 0; i < lights.Length; i++)
            {
                lightEnabledStates[i] = lights[i].enabled;
                lightIntensities[i] = lights[i].intensity;
                lightColors[i] = lights[i].color;
            }

            audioSources = GetComponentsInChildren<AudioSource>(true);
            animators = GetComponentsInChildren<Animator>(true);
            captured = true;
        }

        public void Restore()
        {
            if (!captured)
            {
                Capture();
            }

            transform.localPosition = localPosition;
            transform.localRotation = localRotation;
            transform.localScale = localScale;
            gameObject.SetActive(normalActiveState);

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null)
                {
                    continue;
                }

                renderers[i].enabled = rendererEnabledStates[i];
                renderers[i].sharedMaterials = rendererMaterials[i];
            }

            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i] == null)
                {
                    continue;
                }

                lights[i].enabled = lightEnabledStates[i];
                lights[i].intensity = lightIntensities[i];
                lights[i].color = lightColors[i];
            }

            for (int i = 0; i < audioSources.Length; i++)
            {
                if (audioSources[i] != null)
                {
                    audioSources[i].Stop();
                }
            }

            for (int i = 0; i < animators.Length; i++)
            {
                if (animators[i] != null && animators[i].isActiveAndEnabled)
                {
                    animators[i].Rebind();
                    animators[i].Update(0f);
                }
            }
        }
    }

    public sealed class NormalStateRegistry
    {
        private readonly List<NormalStateSnapshot> snapshots = new List<NormalStateSnapshot>();

        public void CaptureFrom(Transform root)
        {
            snapshots.Clear();
            if (root == null)
            {
                return;
            }

            NormalStateSnapshot[] found = root.GetComponentsInChildren<NormalStateSnapshot>(true);
            for (int i = 0; i < found.Length; i++)
            {
                found[i].Capture();
                snapshots.Add(found[i]);
            }
        }

        public void RestoreAll()
        {
            for (int i = 0; i < snapshots.Count; i++)
            {
                if (snapshots[i] != null)
                {
                    snapshots[i].Restore();
                }
            }
        }
    }
}
