using UnityEngine;

namespace Zero.AnomalyPrototype.Flow
{
    public enum Room2LightPhase { Off, WarningToOn, On, WarningToOff }
    public sealed class Room2LightController : MonoBehaviour
    {
        [SerializeField] private Light[] lights = new Light[5];
        [SerializeField] private AudioSource warningSource;
        [SerializeField] private AudioClip beforeOn;
        [SerializeField] private AudioClip beforeOff;
        [SerializeField, Min(0.01f)] private float offSeconds = 2.5f;
        [SerializeField, Min(0.01f)] private float onSeconds = 1.6f;
        [SerializeField, Min(0.01f)] private float warningSeconds = 0.25f;
        private float[] intensities;
        private float timer;
        private float master = 1f;
        private bool running;
        private int cycle;
        private SourceSoundPlayback warningPlayback;
        public Room2LightPhase Phase { get; private set; }
        public float OnElapsed { get; private set; }
        public bool IsPunishing => cycle > 1 && running &&
            (Phase == Room2LightPhase.On || Phase == Room2LightPhase.WarningToOff);

        private void Awake()
        {
            beforeOn = GameAudioBootstrap.ResolveDefaultClip(beforeOn, "clip.LightOnOff");
            beforeOff = GameAudioBootstrap.ResolveDefaultClip(beforeOff, "clip.LightOnOff");
            Cache();
            Apply(true);
        }
        private void Cache()
        {
            if (intensities != null) return;
            intensities = new float[lights.Length];
            for (int i = 0; i < lights.Length; i++) if (lights[i] != null) intensities[i] = lights[i].intensity;
        }
        public void Begin(bool retry)
        {
            Cache();
            cycle = retry ? Mathf.Max(2, cycle) : 1;
            running = true;
            master = 1f;
            Enter(Room2LightPhase.Off);
        }
        public void StopCycle(bool keepOn = true)
        {
            running = false;
            if (warningSource != null) warningSource.Stop();
            Apply(keepOn);
        }
        public void SetMasterIntensity(float value) { master = Mathf.Clamp01(value); Apply(!running || Phase == Room2LightPhase.On || Phase == Room2LightPhase.WarningToOff); }
        private void Apply(bool on)
        {
            Cache();
            for (int i = 0; i < lights.Length; i++) if (lights[i] != null) lights[i].intensity = on ? intensities[i] * master : 0f;
        }
        private void Enter(Room2LightPhase phase)
        {
            Phase = phase;
            timer = 0f;
            if (phase == Room2LightPhase.Off) OnElapsed = 0f;
            Apply(phase == Room2LightPhase.On || phase == Room2LightPhase.WarningToOff);
            if (warningSource != null && (phase == Room2LightPhase.WarningToOn || phase == Room2LightPhase.WarningToOff))
            {
                AudioClip clip = phase == Room2LightPhase.WarningToOn ? beforeOn : beforeOff != null ? beforeOff : beforeOn;
                warningSource.pitch = phase == Room2LightPhase.WarningToOn ? 1.1f : 0.8f;
                if (clip != null)
                    GameAudioBootstrap.BindSource(ref warningPlayback, warningSource, AudioBus.Sfx, false).Play(
                        null, clip, true, Mathf.Max(0.55f, 1f - Mathf.Max(0, cycle - 2) * 0.1f));
            }
        }
        private void Update()
        {
            if (!running) return;
            timer += Time.deltaTime;
            if (Phase == Room2LightPhase.On || Phase == Room2LightPhase.WarningToOff) OnElapsed += Time.deltaTime;
            float duration = Phase == Room2LightPhase.Off ? offSeconds : Phase == Room2LightPhase.On ? onSeconds : warningSeconds;
            if (timer < duration) return;
            if (Phase == Room2LightPhase.WarningToOff) { cycle++; Enter(Room2LightPhase.Off); }
            else Enter((Room2LightPhase)((int)Phase + 1));
        }
        private void OnDisable() => StopCycle();
    }
}
