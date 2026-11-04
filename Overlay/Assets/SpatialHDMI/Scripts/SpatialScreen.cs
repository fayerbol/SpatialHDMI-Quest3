using UnityEngine;
namespace SpatialHDMI {
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class SpatialScreen : MonoBehaviour {
        public const float BaseWidth = 2f;
        [Range(0, 1)] public float curvature = .15f;
        [Range(8, 128)] public int segments = 64;
        public float Width => BaseWidth * transform.localScale.x;
        public float Height => Width * 9f / 16f;
        Mesh mesh;
        public Vector3 Point(float u, float vertical) {
            float angle = Mathf.Clamp01(curvature) * 1.6f, a = (u - .5f) * angle;
            return new Vector3(angle < .001f ? (u - .5f) * BaseWidth : Mathf.Sin(a) * BaseWidth / angle,
                vertical * BaseWidth * 9f / 16f,
                angle < .001f ? 0 : (Mathf.Cos(a) - 1) * BaseWidth / angle);
        }
        void Awake() { Rebuild(); }
        public void Rebuild() {
            if (!mesh) { mesh = new Mesh { name = "Spatial 16x9 surface" }; mesh.MarkDynamic(); GetComponent<MeshFilter>().sharedMesh = mesh; }
            int n = Mathf.Clamp(segments, 8, 128);
            var vertices = new Vector3[(n + 1) * 2]; var uv = new Vector2[vertices.Length]; var triangles = new int[n * 6];
            for (int i = 0; i <= n; i++) {
                float u = (float)i / n; vertices[i * 2] = Point(u, -.5f); vertices[i * 2 + 1] = Point(u, .5f);
                uv[i * 2] = new Vector2(u, 0); uv[i * 2 + 1] = new Vector2(u, 1);
                if (i == n) continue;
                int j = i * 6, k = i * 2;
                triangles[j] = k; triangles[j+1] = k+1; triangles[j+2] = k+2;
                triangles[j+3] = k+2; triangles[j+4] = k+1; triangles[j+5] = k+3;
            }
            mesh.Clear(); mesh.vertices = vertices; mesh.uv = uv; mesh.triangles = triangles; mesh.RecalculateNormals(); mesh.RecalculateBounds();
        }
        void OnDestroy() { if (mesh) { if (Application.isPlaying) Destroy(mesh); else DestroyImmediate(mesh); } }
    }
}
