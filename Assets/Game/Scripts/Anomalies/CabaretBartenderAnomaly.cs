using UnityEngine;
using Phase = Zero.AnomalyPrototype.Anomalies.CabaretBartenderRules.Phase;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class CabaretBartenderAnomaly : AnomalyBehaviour
    {
        [SerializeField] private Transform observer;
        [SerializeField] private Transform bottle;
        [SerializeField] private Transform cap;
        [SerializeField] private Transform glass;
        [SerializeField] private Transform liquid;
        [SerializeField] private LineRenderer stream;
        [SerializeField] private Transform servingSurface;
        [SerializeField, Min(.1f)] private float approachDistance = 1.65f;
        [SerializeField] private float[] servingLanes = { -2.2f, -.1f, 2.1f };
        [SerializeField, Min(.1f)] private float fetchSeconds = 2.4f;
        [SerializeField, Min(.1f)] private float uncapSeconds = .9f;
        [SerializeField, Min(.1f)] private float pourSeconds = 3.2f;
        [SerializeField, Min(.1f)] private float serveSeconds = 1.1f;
        [SerializeField, Min(.1f)] private float returnSeconds = 2f;
        [SerializeField] private Vector3 bottleMouth = new Vector3(0f, .435f, 0f);
        [SerializeField] private float liquidBottom = .016f;
        [SerializeField] private float liquidHeight = .064f;
        [SerializeField] private float rimHeight = .12f;
        [SerializeField, Min(.01f)] private float capArcHeight = .18f;
        [SerializeField, Min(.01f)] private float capSideOffset = .22f;
        [SerializeField, Min(.01f)] private float capLift = .16f;
        private Transform[] moved;
        private Vector3[] positions, scales;
        private Quaternion[] rotations;
        private bool normalLiquidActive, normalStreamEnabled;
        private CabaretBartenderRules rules;
        private Vector3 bottleStart, glassStart, laneEntry, preparation, served, readyBottle;
        private Quaternion bottleRotation, pouringRotation;
        private Vector3 liquidInGlass, capInBottle, capHome, capPark;
        private Quaternion liquidRotationInGlass, capClosedRotation;
        private bool HasSnapshot => moved != null && moved.Length == 4 && positions?.Length == 4 &&
            scales?.Length == 4 && rotations?.Length == 4;
        public Phase State => rules == null ? Phase.Waiting : rules.State;
        public bool IsConfigured => observer != null && bottle != null && cap != null && glass != null &&
            liquid != null && stream != null && servingSurface != null && servingLanes != null && servingLanes.Length > 0;

        public override void CaptureNormalState()
        {
            if (HasSnapshot || !IsConfigured) return;
            moved = new[] { bottle, cap, glass, liquid };
            positions = new Vector3[4]; scales = new Vector3[4]; rotations = new Quaternion[4];
            for (int i = 0; i < moved.Length; i++)
            {
                positions[i] = moved[i].localPosition;
                scales[i] = moved[i].localScale;
                rotations[i] = moved[i].localRotation;
            }
            normalLiquidActive = liquid.gameObject.activeSelf;
            normalStreamEnabled = stream.enabled;
            liquidInGlass = glass.InverseTransformPoint(liquid.position);
            liquidRotationInGlass = Quaternion.Inverse(glass.rotation) * liquid.rotation;
            capInBottle = bottle.InverseTransformPoint(cap.position);
        }

        protected override void OnActivate()
        {
            CaptureNormalState();
            if (!IsConfigured || !HasSnapshot) return;
            rules = new CabaretBartenderRules(fetchSeconds, uncapSeconds, pourSeconds, serveSeconds, returnSeconds);
            liquid.gameObject.SetActive(false);
            stream.enabled = false;
        }

        private void Update()
        {
            if (!IsActive || rules == null || !IsConfigured) return;
            Advance(Time.deltaTime);
        }

        public void Advance(float seconds)
        {
            if (!IsActive || rules == null || !IsConfigured) return;
            if (rules.State == Phase.Waiting && !TryBegin()) return;
            rules.Tick(seconds);
            Present();
        }

        private bool TryBegin()
        {
            Vector3 local = servingSurface.InverseTransformPoint(observer.position);
            float lane = servingLanes[0];
            for (int i = 1; i < servingLanes.Length; i++)
                if (Mathf.Abs(servingLanes[i] - local.x) < Mathf.Abs(lane - local.x)) lane = servingLanes[i];
            Vector3 target = servingSurface.TransformPoint(new Vector3(lane, 0f, .22f));
            Vector3 delta = observer.position - target; delta.y = 0f;
            if (local.z < 0f || delta.sqrMagnitude > approachDistance * approachDistance) return false;
            preparation = target;
            served = target + servingSurface.forward * .23f;
            bottleStart = bottle.position; glassStart = glass.position; bottleRotation = bottle.rotation;
            laneEntry = glassStart + servingSurface.forward * Vector3.Dot(target + servingSurface.forward * .15f - glassStart, servingSurface.forward);
            pouringRotation = Quaternion.AngleAxis(112f, servingSurface.right) * bottleRotation;
            Vector3 mouth = preparation + Vector3.up * (rimHeight + .16f);
            readyBottle = mouth - pouringRotation * Vector3.Scale(bottleMouth, bottle.lossyScale);
            capHome = readyBottle + bottleRotation * Vector3.Scale(capInBottle, bottle.lossyScale);
            capPark = capHome - servingSurface.forward * capSideOffset + Vector3.up * capLift;
            capClosedRotation = cap.rotation;
            return rules.TryStart();
        }

        private void Present()
        {
            float t = Mathf.SmoothStep(0f, 1f, rules.Progress);
            stream.enabled = false;
            switch (rules.State)
            {
                case Phase.Fetching: Fetch(t); break;
                case Phase.Uncapping: Uncap(t); break;
                case Phase.Pouring: Pour(t); break;
                case Phase.Serving: Serve(t); break;
                case Phase.Returning: ReturnBottle(t); break;
                case Phase.Finished: Finish(); break;
            }
        }

        private void Fetch(float t)
        {
            Vector3 corner = preparation + servingSurface.forward * .15f;
            if (t < .2f) glass.position = Vector3.Lerp(glassStart, laneEntry, t * 5f);
            else if (t < .8f) glass.position = Vector3.Lerp(laneEntry, corner, (t - .2f) / .6f);
            else glass.position = Vector3.Lerp(corner, preparation, (t - .8f) * 5f);
            bottle.position = Vector3.Lerp(bottleStart, readyBottle, t) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * .65f);
            bottle.rotation = bottleRotation;
        }

        private void Uncap(float t)
        {
            glass.position = preparation;
            bottle.SetPositionAndRotation(readyBottle, bottleRotation);
            cap.position = CapArc(capHome, capPark, t, capArcHeight);
            cap.rotation = capClosedRotation * Quaternion.Euler(0f, 540f * t, 0f);
        }

        private void Pour(float t)
        {
            float tilt = Mathf.Clamp01(t / .22f);
            float fill = Mathf.Clamp01((t - .22f) / .7f);
            glass.position = preparation;
            bottle.SetPositionAndRotation(readyBottle, Quaternion.Slerp(bottleRotation, pouringRotation, tilt));
            cap.SetPositionAndRotation(capPark, capClosedRotation * Quaternion.Euler(0f, 540f, 0f));
            SetFill(fill);
            if (t > .22f && t < .94f) DrawStream(fill);
        }

        private void Serve(float t)
        {
            glass.position = Vector3.Lerp(preparation, served, t);
            bottle.SetPositionAndRotation(readyBottle, Quaternion.Slerp(pouringRotation, bottleRotation, Mathf.Clamp01(t / .35f)));
            float closing = Mathf.Clamp01((t - .35f) / .65f);
            cap.position = CapArc(capPark, capHome, closing, capArcHeight);
            cap.rotation = Quaternion.Slerp(capClosedRotation * Quaternion.Euler(0f, 540f, 0f), capClosedRotation, closing);
            SetFill(1f);
        }

        private void ReturnBottle(float t)
        {
            glass.position = served;
            cap.localPosition = positions[1]; cap.localRotation = rotations[1];
            bottle.SetPositionAndRotation(Vector3.Lerp(readyBottle, bottleStart, t) +
                Vector3.up * (Mathf.Sin(t * Mathf.PI) * .6f), bottleRotation);
            SetFill(1f);
        }

        private void Finish()
        {
            bottle.localPosition = positions[0]; bottle.localRotation = rotations[0];
            cap.localPosition = positions[1]; cap.localRotation = rotations[1];
            glass.position = served;
            SetFill(1f);
        }

        private void SetFill(float amount)
        {
            liquid.gameObject.SetActive(amount > .001f);
            liquid.localScale = Vector3.Scale(scales[3], new Vector3(1f, Mathf.Max(.001f, amount), 1f));
            liquid.SetPositionAndRotation(glass.TransformPoint(liquidInGlass + Vector3.up * (liquidBottom * (1f - amount))),
                glass.rotation * liquidRotationInGlass);
        }

        public static Vector3 CapArc(Vector3 start, Vector3 end, float progress, float height)
        {
            float t = Mathf.Clamp01(progress);
            return Vector3.Lerp(start, end, t) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * height);
        }

        private void DrawStream(float fill)
        {
            stream.enabled = true;
            Vector3 start = bottle.TransformPoint(bottleMouth);
            Vector3 end = glass.position + Vector3.up * (liquidBottom + liquidHeight * fill);
            for (int i = 0; i < stream.positionCount; i++)
            {
                float t = i / (float)(stream.positionCount - 1);
                Vector3 point = Vector3.Lerp(start, end, t);
                point += servingSurface.forward * (Mathf.Sin(t * Mathf.PI) * .012f);
                stream.SetPosition(i, point);
            }
        }

        protected override void OnResetAnomaly()
        {
            rules?.Reset();
            if (!HasSnapshot) return;
            for (int i = 0; i < moved.Length; i++)
            {
                if (moved[i] == null) continue;
                moved[i].localPosition = positions[i]; moved[i].localRotation = rotations[i]; moved[i].localScale = scales[i];
            }
            if (liquid != null) liquid.gameObject.SetActive(normalLiquidActive);
            if (stream != null) stream.enabled = normalStreamEnabled;
        }

        private void OnDisable() => ResetAnomaly();
    }
}
