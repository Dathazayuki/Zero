using UnityEngine;
using UnityEngine.AI;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class CabaretRitualEye : MonoBehaviour
    {
        [SerializeField] private Transform eyeball;
        [SerializeField] private Renderer eyelidRenderer;
        [SerializeField] private Renderer eyeballRenderer;
        [SerializeField] private BoxCollider movementArea;
        [SerializeField, Min(0f)] private float fleeSpeed = .65f;
        [SerializeField, Min(0f), Tooltip("0 allows the whole movement area.")] private float roamRadius;
        [SerializeField, Min(.1f)] private float closeSeconds = .65f;
        [SerializeField, Min(.5f)] private float minimumRunDistance = 2.8f;
        [SerializeField] private int randomSeed = 51717;
        private Vector3 home, scale, pupilPosition;
        private Quaternion rotation;
        private float closing;
        private bool captured;
        private System.Random random;
        private NavMeshPath path;
        private Vector3[] corners;
        private int cornerCount, nextCorner;
        private float destinationDelay;
        private MaterialPropertyBlock colour;
        private static readonly int BaseColour = Shader.PropertyToID("_BaseColor");
        public bool IsConfigured => eyeball != null && eyelidRenderer != null && eyeballRenderer != null && movementArea != null;

        public void Capture()
        {
            if (captured || !IsConfigured) return;
            captured = true; home = transform.position; rotation = transform.rotation;
            scale = transform.localScale; pupilPosition = eyeball.localPosition;
            colour = new MaterialPropertyBlock();
            path = new NavMeshPath(); corners = new Vector3[32];
            ResetRoute();
        }

        public void Present(float delta, float time, bool defeated, Vector3 threat, float speedMultiplier = 1f)
        {
            if (!captured) return;
            closing = defeated ? Mathf.Min(1f, closing + delta / closeSeconds) : 0f;
            transform.localScale = new Vector3(scale.x, scale.y * Mathf.Lerp(1f, .015f, closing), scale.z);
            eyeball.localPosition = pupilPosition + new Vector3(Mathf.Sin(time * 2.3f) * .1f,
                Mathf.Sin(time * 3.1f) * .02f, 0f);
            colour.SetColor(BaseColour, Color.Lerp(new Color(1f, .28f, .45f), Color.white, closing) * (1f - closing));
            eyelidRenderer.SetPropertyBlock(colour); eyeballRenderer.SetPropertyBlock(colour);
            if (defeated) { if (closing >= 1f) gameObject.SetActive(false); return; }
            if (float.IsInfinity(threat.x)) return;
            Flee(delta, threat, speedMultiplier);
        }

        private void Flee(float delta, Vector3 threat, float speedMultiplier)
        {
            Vector3 away = transform.position - threat; away.y = 0f;
            if (nextCorner < cornerCount && away.sqrMagnitude < 9f
                && Vector3.Dot(corners[nextCorner] - transform.position, away) < 0f)
            { cornerCount = 0; nextCorner = 0; }
            if (nextCorner >= cornerCount)
            {
                destinationDelay -= delta;
                if (destinationDelay > 0f) return;
                destinationDelay = .6f;
                if (!PickDestination(threat)) return;
            }
            Vector3 candidate = Vector3.MoveTowards(transform.position, corners[nextCorner],
                fleeSpeed * Mathf.Max(1f, speedMultiplier) * delta);
            if (!TryMove(candidate)) { cornerCount = 0; nextCorner = 0; return; }
            if ((transform.position - corners[nextCorner]).sqrMagnitude < .0064f) nextCorner++;
        }

        private bool PickDestination(Vector3 threat)
        {
            if (!NavMesh.SamplePosition(transform.position, out NavMeshHit start, .35f, NavMesh.AllAreas)) return false;
            Bounds bounds = movementArea.bounds;
            for (int attempt = 0; attempt < 8; attempt++)
            {
                Vector3 point = new Vector3(Mathf.Lerp(bounds.min.x, bounds.max.x, (float)random.NextDouble()),
                    home.y, Mathf.Lerp(bounds.min.z, bounds.max.z, (float)random.NextDouble()));
                if ((point - transform.position).sqrMagnitude < minimumRunDistance * minimumRunDistance) continue;
                Vector3 away = transform.position - threat; away.y = 0f;
                if ((point - threat).sqrMagnitude < CabaretRitualRules.MinimumEscapeDistanceSquared(away.sqrMagnitude)) continue;
                if (!NavMesh.SamplePosition(point, out NavMeshHit end, .4f, NavMesh.AllAreas)) continue;
                if (!NavMesh.CalculatePath(start.position, end.position, NavMesh.AllAreas, path)) continue;
                if (path.status != NavMeshPathStatus.PathComplete || !AcceptRoute(bounds, threat)) continue;
                nextCorner = 1;
                return true;
            }
            cornerCount = 0;
            return false;
        }

        private bool AcceptRoute(Bounds bounds, Vector3 threat)
        {
            cornerCount = path.GetCornersNonAlloc(corners);
            if (cornerCount < 2 || cornerCount >= corners.Length) return false;
            for (int i = 0; i < cornerCount; i++)
            {
                if (!bounds.Contains(corners[i]) || Mathf.Abs(corners[i].y - home.y) > .15f) return false;
                if (roamRadius > 0f && (corners[i] - home).sqrMagnitude > roamRadius * roamRadius) return false;
                corners[i].y = home.y;
            }
            Vector3 away = transform.position - threat; away.y = 0f;
            return away.sqrMagnitude >= 9f || Vector3.Dot(corners[1] - transform.position, away) >= 0f;
        }

        private void ResetRoute()
        {
            cornerCount = 0; nextCorner = 0; destinationDelay = 0f;
            random = new System.Random(randomSeed ^ Mathf.RoundToInt(home.x * 131f + home.z * 977f));
        }

        private bool TryMove(Vector3 candidate)
        {
            Vector3 displacement = candidate - home; displacement.y = 0f;
            if (roamRadius > 0f) candidate = home + Vector3.ClampMagnitude(displacement, roamRadius);
            if (!movementArea.bounds.Contains(candidate)) return false;
            if (!NavMesh.SamplePosition(transform.position, out NavMeshHit start, .35f, NavMesh.AllAreas)) return false;
            if (!NavMesh.SamplePosition(candidate, out NavMeshHit end, .35f, NavMesh.AllAreas)) return false;
            if (Mathf.Abs(end.position.y - home.y) > .15f || !movementArea.bounds.Contains(end.position)) return false;
            if (NavMesh.Raycast(start.position, end.position, out NavMeshHit _, NavMesh.AllAreas)) return false;
            candidate = end.position; candidate.y = home.y;
            if ((candidate - transform.position).sqrMagnitude < .000001f) return false;
            transform.position = candidate;
            return true;
        }

        public void Restore()
        {
            if (!captured) return;
            ResetRoute();
            closing = 0f; transform.SetPositionAndRotation(home, rotation); transform.localScale = scale;
            eyeball.localPosition = pupilPosition;
            eyelidRenderer.SetPropertyBlock(null); eyeballRenderer.SetPropertyBlock(null);
        }

        private void OnDisable() => Restore();
    }
}
