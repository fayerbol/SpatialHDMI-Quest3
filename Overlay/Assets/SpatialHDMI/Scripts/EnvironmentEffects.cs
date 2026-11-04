using UnityEngine;
using UnityEngine.Rendering;

namespace SpatialHDMI {
    // Virtual glow, not a change to physical room lighting. No room-mesh permission needed.
    public sealed class EnvironmentEffects : MonoBehaviour {
        public DemoVideo video;
        public SpatialScreen screen;
        public Camera xrCamera;
        [Range(0,1)] public float intensity = .25f;
        [Range(.1f,2)] public float reach = .6f;
        [Range(0,.95f)] public float dimming;
        public string Status { get; private set; } = "Ambient glow ready";
        RenderTexture sample;
        readonly Material[] materials = new Material[4];
        readonly Transform[] halos = new Transform[4];
        readonly Color[] target = { Color.black, Color.black, Color.black, Color.black };
        readonly Color[] smooth = { Color.black, Color.black, Color.black, Color.black };
        float nextSample;
        bool pending, hasSample;
        Texture2D cpuSample;
        void Start() {
            var shader = Resources.Load<Shader>("SpatialTint");
            if (!shader) { Status = "Ambient shader missing"; enabled = false; return; }
            for (int i = 0; i < 4; i++) {
                var halo = GameObject.CreatePrimitive(PrimitiveType.Quad); halo.name = "Video glow " + i;
                Destroy(halo.GetComponent<Collider>()); halo.transform.SetParent(screen.transform, false); halos[i] = halo.transform;
                materials[i] = new Material(shader) { renderQueue = 2900 }; materials[i].SetFloat("_SoftEdge", 1);
                halo.GetComponent<Renderer>().sharedMaterial = materials[i];
            }
            sample = new RenderTexture(16, 8, 0, RenderTextureFormat.ARGB32); sample.Create();
            if (!SystemInfo.supportsAsyncGPUReadback) { cpuSample = new Texture2D(16,8,TextureFormat.RGBA32,false); Status = "Ambient fallback sampler 4 Hz"; }
        }
        void Update() {
            if (xrCamera) { xrCamera.clearFlags = CameraClearFlags.SolidColor; xrCamera.backgroundColor = new Color(0,0,0,Mathf.Clamp(dimming,0,.95f)); }
            float blend = 1 - Mathf.Exp(-Time.unscaledDeltaTime / .25f);
            for (int i = 0; i < 4; i++) {
                if (!halos[i]) continue;
                smooth[i] = Color.Lerp(smooth[i], target[i], blend);
                var c = smooth[i]; c.a = hasSample && video && video.FramesReceived > 0 ? intensity * .7f : 0; materials[i].SetColor("_Color", c);
                float r = Mathf.Clamp(reach,.1f,2);
                halos[i].localPosition = new Vector3(i==0 ? -1 : i==1 ? 1 : 0, i==2 ? .5625f : i==3 ? -.5625f : 0, .04f);
                halos[i].localScale = new Vector3(i<2 ? r : 2+r, i<2 ? 1.125f+r : r, 1);
            }
            if (!sample || !video || !video.videoTexture || video.FramesReceived == 0 || intensity <= .001f || pending || Time.unscaledTime < nextSample) return;
            nextSample = Time.unscaledTime + (cpuSample ? .25f : .1f);
            Graphics.Blit(video.videoTexture, sample);
            if (cpuSample) {
                var previous = RenderTexture.active;
                try { RenderTexture.active = sample; cpuSample.ReadPixels(new Rect(0,0,16,8),0,0,false); Accumulate(cpuSample.GetPixels32()); }
                catch (System.Exception e) { hasSample=false; Status="Ambient fallback: "+e.Message; }
                finally { RenderTexture.active=previous; }
                return;
            }
            pending = true;
            AsyncGPUReadback.Request(sample, 0, TextureFormat.RGBA32, request => {
                if (!this) return;
                pending = false;
                if (request.hasError) { hasSample=false; Status = "Ambient sampling failed"; return; }
                var pixels = request.GetData<Color32>();
                for (int edge=0;edge<4;edge++) {
                    Color sum=Color.black;int count=0;
                    for(int y=0;y<8;y++)for(int x=0;x<16;x++)if(IsEdge(edge,x,y)){sum+=(Color)pixels[y*16+x];count++;}
                    target[edge]=sum/Mathf.Max(1,count);
                }
                hasSample=true;Status = "Ambient sampling 10 Hz / smoothed";
            });
        }
        static bool IsEdge(int edge,int x,int y) { return (edge==0&&x<4)||(edge==1&&x>=12)||(edge==2&&y>=6)||(edge==3&&y<2); }
        void Accumulate(Color32[] pixels) {
            for(int edge=0;edge<4;edge++) {
                Color sum=Color.black;int count=0;
                for(int y=0;y<8;y++)for(int x=0;x<16;x++)if(IsEdge(edge,x,y)){sum+=(Color)pixels[y*16+x];count++;}
                target[edge]=sum/Mathf.Max(1,count);
            }
            hasSample=true;
        }
        void OnDestroy() {
            if (sample) { sample.Release(); Destroy(sample); }
            if (cpuSample) Destroy(cpuSample);
            for (int i=0;i<4;i++) { if (halos[i]) Destroy(halos[i].gameObject); if (materials[i]) Destroy(materials[i]); }
        }
    }
}
