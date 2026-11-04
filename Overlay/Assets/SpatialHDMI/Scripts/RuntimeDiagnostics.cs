using System;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Management;

namespace SpatialHDMI {
    // Device evidence is recorded locally; this does not transmit telemetry.
    public sealed class RuntimeDiagnostics : MonoBehaviour {
        public DemoVideo video;
        public HandInteractionGuard hands;
        public EnvironmentEffects environment;
        public string Status { get; private set; } = "No device snapshot saved";
        float nextWrite=10;
        [Serializable] class Report {
            public string utc,appVersion,buildGuid,unity,device,graphics,renderPipeline,xrLoader,playback,hands,environment;
            public ulong frames; public double mediaTime; public bool prepared,playing,shaderSupported;
        }
        void Update() { if(Time.unscaledTime<nextWrite)return;nextWrite=Time.unscaledTime+30;Save(); }
        public void Save() {
            if(!video || !hands || !environment)return;
            try {
                var manager=XRGeneralSettings.Instance ? XRGeneralSettings.Instance.Manager : null;
                var r=new Report {
                    utc=DateTime.UtcNow.ToString("o"),appVersion=Application.version,buildGuid=Application.buildGUID,unity=Application.unityVersion,
                    device=SystemInfo.deviceModel,graphics=SystemInfo.graphicsDeviceType.ToString(),
                    renderPipeline=GraphicsSettings.currentRenderPipeline ? GraphicsSettings.currentRenderPipeline.GetType().Name : "Built-in",
                    xrLoader=manager && manager.activeLoader ? manager.activeLoader.name : "none",playback=video.Status,
                    hands=hands.Status,environment=environment.Status,frames=video.FramesReceived,mediaTime=video.TimeSeconds,
                    prepared=video.IsPrepared,playing=video.IsPlaying,
                    shaderSupported=video.screenRenderer && video.screenRenderer.sharedMaterial && video.screenRenderer.sharedMaterial.shader.isSupported
                };
                File.WriteAllText(Path.Combine(Application.persistentDataPath,"device-diagnostics.json"),JsonUtility.ToJson(r,true));
                Status="Device snapshot saved";
            } catch(Exception e) { Status="Diagnostics: "+e.Message; }
        }
        void OnApplicationPause(bool paused) { if(paused)Save(); }
    }
}
