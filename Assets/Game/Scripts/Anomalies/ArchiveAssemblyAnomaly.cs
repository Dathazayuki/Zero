using UnityEngine;
using Zero.AnomalyPrototype.Player;
using AssemblyPhase = Zero.AnomalyPrototype.Anomalies.ArchiveAssemblyRules.Phase;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class ArchiveAssemblyAnomaly : AnomalyBehaviour
    {
        [SerializeField] private ArchiveAssemblyPresentation body;
        [SerializeField] private Camera observer;
        [SerializeField] private PlayerDeathController death;
        [SerializeField] private AnomalyDeathPresentation presentation;
        [SerializeField, Min(0.1f)] private float scatterSeconds = 1.5f;
        [SerializeField, Min(0.1f)] private float crawlTogetherSeconds = 9f;
        [SerializeField, Min(0.1f)] private float assemblySeconds = 3f;
        private ArchiveAssemblyRules rules;
        private Quaternion cameraStart, cameraLocal;
        private float cameraElapsed;
        private bool ownsCamera;
        public AssemblyPhase State => rules != null ? rules.State : AssemblyPhase.Scattered;
        public bool IsConfigured => body != null && body.IsConfigured && observer != null && death != null && presentation != null;

        public void Configure(ArchiveAssemblyPresentation actor, Camera camera, PlayerDeathController controller, AnomalyDeathPresentation view)
        {
            body = actor;
            observer = camera;
            death = controller;
            presentation = view;
        }

        public override void CaptureNormalState()
        {
            if (rules != null || !IsConfigured) return;
            body.Capture();
            rules = new ArchiveAssemblyRules(scatterSeconds, crawlTogetherSeconds, assemblySeconds);
        }

        protected override void OnActivate()
        {
            CaptureNormalState();
            if (rules == null) return;
            rules.Reset();
            body.Scatter();
        }

        private void Update()
        {
            if (!IsActive || rules == null || !IsConfigured) return;
            if (State == AssemblyPhase.Caught)
            {
                if (death.IsScreenBlack || !death.IsDeathInProgress) ResetAnomaly();
                return;
            }
            if (death.IsDeathInProgress) return;
            Advance();
        }

        private void Advance()
        {
            AssemblyPhase previous = State;
            rules.Tick(Time.deltaTime);
            if (previous != State && !EnterPhase()) return;
            if (State == AssemblyPhase.CrawlingTogether) body.CrawlTogether(rules.Progress);
            else if (State == AssemblyPhase.Assembling) body.Assemble(rules.Progress);
            else if (State == AssemblyPhase.Chasing && body.Pursue(observer.transform)) death.TryBeginDeath(this, presentation);
        }

        private bool EnterPhase()
        {
            bool ready = true;
            if (State == AssemblyPhase.CrawlingTogether) ready = body.BeginCrawlTogether();
            else if (State == AssemblyPhase.Assembling) { body.CrawlTogether(1f); body.BeginAssembly(); }
            else if (State == AssemblyPhase.Chasing) { body.Assemble(1f); ready = body.BeginChase(); }
            if (ready) return true;
            Debug.LogWarning("IR3_ASSEMBLY_NAVIGATION_UNAVAILABLE: bake the room NavMesh", this);
            ResetAnomaly();
            return false;
        }

        public override void OnPlayerDeathAccepted()
        {
            if (rules == null || State == AssemblyPhase.Caught) return;
            rules.Catch();
            body.AttackPlayer(observer.transform);
            cameraStart = observer.transform.rotation;
            cameraLocal = observer.transform.localRotation;
            cameraElapsed = 0f;
            ownsCamera = true;
        }

        private void LateUpdate()
        {
            if (!IsActive || !ownsCamera || observer == null || death.IsScreenBlack) return;
            cameraElapsed += Time.unscaledDeltaTime;
            Vector3 direction = body.FocusPoint - observer.transform.position;
            if (direction.sqrMagnitude < 0.001f) return;
            observer.transform.rotation = Quaternion.Slerp(cameraStart, Quaternion.LookRotation(direction),
                Mathf.SmoothStep(0f, 1f, cameraElapsed / 0.35f));
        }

        protected override void OnResetAnomaly()
        {
            rules?.Reset();
            body?.Restore();
            if (ownsCamera && observer != null && death != null && !death.IsDeathInProgress)
                observer.transform.localRotation = cameraLocal;
            ownsCamera = false;
        }

        private void OnDisable() => ResetAnomaly();
    }
}
