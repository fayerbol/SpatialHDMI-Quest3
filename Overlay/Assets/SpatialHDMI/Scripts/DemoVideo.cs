using System.IO;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Video;

namespace SpatialHDMI {
    [RequireComponent(typeof(VideoPlayer), typeof(AudioSource))]
    public sealed class DemoVideo : MonoBehaviour {
        public Renderer screenRenderer;
        public RenderTexture videoTexture;
        public string Status { get; private set; } = "Preparing demo";
        VideoPlayer player;
        IEnumerator Start() {
            player = GetComponent<VideoPlayer>();
            if (!screenRenderer) { Status="Screen renderer missing"; yield break; }
            if (!videoTexture) { videoTexture = new RenderTexture(1280,720,0,RenderTextureFormat.ARGB32); videoTexture.Create(); }
            screenRenderer.material.mainTexture = videoTexture;
            player.playOnAwake = false; player.isLooping = true; player.renderMode=VideoRenderMode.RenderTexture;
            player.targetTexture=videoTexture; player.audioOutputMode=VideoAudioOutputMode.AudioSource;
            player.controlledAudioTrackCount=1; player.EnableAudioTrack(0,true); player.SetTargetAudioSource(0,GetComponent<AudioSource>());
            player.errorReceived += OnError;
            // Android StreamingAssets is inside the APK. Copy once to a seekable file for VideoPlayer.
            string source=Path.Combine(Application.streamingAssetsPath,"demo.mp4");
            string dest=Path.Combine(Application.persistentDataPath,"spatial_demo.mp4");
            if (!File.Exists(dest)) {
                using (UnityWebRequest req=UnityWebRequest.Get(source)) {
                    yield return req.SendWebRequest();
                    if (req.result != UnityWebRequest.Result.Success) { Status="Demo copy failed: "+req.error; yield break; }
                    File.WriteAllBytes(dest,req.downloadHandler.data);
                }
            }
            player.source=VideoSource.Url; player.url=dest; player.Prepare();
            float deadline=Time.realtimeSinceStartup+20f;
            while (!player.isPrepared && Time.realtimeSinceStartup < deadline && !Status.StartsWith("Error")) yield return null;
            if (!player.isPrepared) { if (!Status.StartsWith("Error")) Status="Demo preparation timed out"; yield break; }
            player.Play(); Status="Demo playing";
        }
        void OnError(VideoPlayer p,string message) { Status="Error: "+message; }
        public void TogglePause() { if (!player || !player.isPrepared) return; if (player.isPlaying) { player.Pause(); Status="Demo paused"; } else { player.Play(); Status="Demo playing"; } }
        void OnDestroy() { if (player) player.errorReceived -= OnError; }
    }
}
