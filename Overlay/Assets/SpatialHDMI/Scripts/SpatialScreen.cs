using UnityEngine;

namespace SpatialHDMI {
    // Procedural cylindrical strip. Width and height are in metres.
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class SpatialScreen : MonoBehaviour {
        [Range(0.5f, 5f)] public float width = 2f;
        [Range(0f, 1f)] public float curvature = 0.15f;
        [Range(8, 128)] public int segments = 48;
        Mesh mesh;
        void Awake() { mesh = new Mesh { name = "Spatial HDMI curved screen" }; GetComponent<MeshFilter>().sharedMesh = mesh; Rebuild(); }
        void OnDestroy() { if (mesh != null) Destroy(mesh); }
        public void Rebuild() {
            if (mesh == null) return;
            int n = Mathf.Clamp(segments, 8, 128); float w = Mathf.Clamp(width, .5f, 5f), h = w * 9f / 16f;
            float angle = Mathf.Clamp01(curvature) * 1.6f;
            var v = new Vector3[(n + 1) * 2]; var uv = new Vector2[v.Length]; var triangles = new int[n * 6];
            for (int i = 0; i <= n; i++) {
                float u = (float)i / n, a = (u - .5f) * angle;
                float x = angle < .001f ? (u - .5f) * w : Mathf.Sin(a) * w / angle;
                float z = angle < .001f ? 0 : (Mathf.Cos(a) - 1f) * w / angle;
                v[i*2] = new Vector3(x, -h/2, z); v[i*2+1] = new Vector3(x, h/2, z);
                uv[i*2] = new Vector2(u, 0); uv[i*2+1] = new Vector2(u, 1);
                if (i < n) { int j = i*6, k = i*2; triangles[j]=k; triangles[j+1]=k+1; triangles[j+2]=k+2; triangles[j+3]=k+2; triangles[j+4]=k+1; triangles[j+5]=k+3; }
            }
            mesh.Clear(); mesh.vertices=v; mesh.uv=uv; mesh.triangles=triangles; mesh.RecalculateNormals(); mesh.RecalculateBounds();
        }
    }
}
