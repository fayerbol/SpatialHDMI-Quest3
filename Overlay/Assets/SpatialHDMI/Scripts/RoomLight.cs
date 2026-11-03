using UnityEngine;
namespace SpatialHDMI {
    // Assign this material to an independently rendered copy of a room mesh; never overwrite the original mesh.
    public sealed class RoomLight : MonoBehaviour {
        public SpatialScreen screen;
        public DemoVideo demo;
        public Material overlay;
        [Range(0,1)] public float intensity=.3f;
        [Range(.1f,5f)] public float reach=2f;
        [Range(0,2)] public float saturation=1f;
        public bool effectEnabled=true;
        void LateUpdate() {
            if (!screen || !demo || !overlay || !demo.videoTexture) return;
            overlay.SetTexture("_MainTex",demo.videoTexture);
            overlay.SetMatrix("_WorldToScreen",screen.transform.worldToLocalMatrix);
            overlay.SetFloat("_Width",screen.width);
            overlay.SetFloat("_Intensity",effectEnabled ? intensity : 0);
            overlay.SetFloat("_Radius",reach);
            overlay.SetFloat("_Saturation",saturation);
        }
    }
}
