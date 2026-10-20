using UnityEngine;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class EndlessPrinterAnomaly : AnomalyBehaviour
    {
        [SerializeField] private MeshFilter paper;
        [SerializeField] private Transform[] route;
        [SerializeField] private GameObject normalPrinter;
        [SerializeField] private GameObject anomalyPrinter;
        [SerializeField, Min(0.01f)] private float paperWidth = 0.21f;
        [SerializeField, Min(0.01f)] private float feedSpeed = 0.24f;
        private const int Segments = 160;
        private Mesh mesh;
        private Mesh originalMesh;
        private Vector3[] vertices;
        private Vector3[] normals;
        private Vector3[] points;
        private float[] distances;
        private float fedLength;
        private bool captured;
        private bool normalPrinterActive;
        public bool IsConfigured => paper != null && route != null && route.Length >= 2;

        public void Configure(MeshFilter surface, Transform[] path)
        {
            paper = surface;
            route = path;
        }

        public void ConfigurePrinter(GameObject normal, GameObject variant)
        {
            normalPrinter = normal;
            anomalyPrinter = variant;
        }

        public override void CaptureNormalState()
        {
            if (captured || !IsConfigured) return;
            for (int i = 0; i < route.Length; i++) if (route[i] == null) return;
            originalMesh = paper.sharedMesh;
            normalPrinterActive = normalPrinter != null && normalPrinter.activeSelf;
            points = new Vector3[route.Length];
            distances = new float[route.Length];
            vertices = new Vector3[(Segments + 1) * 2];
            normals = new Vector3[vertices.Length];
            mesh = new Mesh { name = "IR3_08_RuntimePaper" };
            mesh.MarkDynamic();
            mesh.vertices = vertices;
            mesh.triangles = Indices();
            paper.sharedMesh = mesh;
            captured = true;
            paper.gameObject.SetActive(false);
        }

        private static int[] Indices()
        {
            var indices = new int[Segments * 12];
            for (int i = 0; i < Segments; i++)
            {
                int v = i * 2, t = i * 12;
                indices[t] = v; indices[t + 1] = v + 2; indices[t + 2] = v + 1;
                indices[t + 3] = v + 1; indices[t + 4] = v + 2; indices[t + 5] = v + 3;
                indices[t + 6] = v + 1; indices[t + 7] = v + 2; indices[t + 8] = v;
                indices[t + 9] = v + 3; indices[t + 10] = v + 2; indices[t + 11] = v + 1;
            }
            return indices;
        }

        protected override void OnActivate()
        {
            CaptureNormalState();
            if (!captured) return;
            fedLength = 0f;
            if (normalPrinter != null) normalPrinter.SetActive(false);
            if (anomalyPrinter != null) anomalyPrinter.SetActive(true);
            for (int i = 0; i < route.Length; i++)
            {
                points[i] = paper.transform.InverseTransformPoint(route[i].position);
                distances[i] = i == 0 ? 0f : distances[i - 1] + Vector3.Distance(points[i - 1], points[i]);
            }
            DrawPaper();
            paper.gameObject.SetActive(true);
        }

        private void Update()
        {
            if (!IsActive || !captured || !IsConfigured || fedLength >= distances[distances.Length - 1]) return;
            fedLength = Mathf.Min(fedLength + feedSpeed * Time.deltaTime, distances[distances.Length - 1]);
            DrawPaper();
        }

        private void DrawPaper()
        {
            int span = 1;
            for (int i = 0; i <= Segments; i++)
            {
                float length = Mathf.Min(i * distances[distances.Length - 1] / Segments, fedLength);
                while (span < points.Length - 1 && distances[span] < length) span++;
                float t = Mathf.InverseLerp(distances[span - 1], distances[span], length);
                Vector3 center = Vector3.Lerp(points[span - 1], points[span], t);
                Vector3 side = Vector3.right * (paperWidth * 0.5f);
                vertices[i * 2] = center - side;
                vertices[i * 2 + 1] = center + side;
                Vector3 normal = Vector3.Cross(points[span] - points[span - 1], Vector3.right).normalized;
                normals[i * 2] = normal;
                normals[i * 2 + 1] = normal;
            }
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.RecalculateBounds();
        }

        protected override void OnResetAnomaly()
        {
            fedLength = 0f;
            if (paper != null) paper.gameObject.SetActive(false);
            if (anomalyPrinter != null) anomalyPrinter.SetActive(false);
            if (captured && normalPrinter != null) normalPrinter.SetActive(normalPrinterActive);
        }

        private void OnDisable() => ResetAnomaly();

        private void OnDestroy()
        {
            if (paper != null && paper.sharedMesh == mesh) paper.sharedMesh = originalMesh;
            if (mesh != null) Destroy(mesh);
        }
    }
}
