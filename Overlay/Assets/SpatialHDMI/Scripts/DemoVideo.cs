using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Video;

namespace SpatialHDMI {
    // VideoPlayer owns decoding; sources only supply a seekable URL or a live texture.
    [RequireComponent(typeof(VideoPlayer), typeof(AudioSource))]
    public sealed class DemoVideo : MonoBehaviour {
        public Renderer screenRenderer;
        public RenderTexture videoTexture { get; private set; }
        public enum PlaybackState { Loading, Playing, Paused, Error }
        public PlaybackState State { get; private set; } = PlaybackState.Loading;
        public string Status { get; private set; } = "Preparing bundled video";
        public bool IsPrepared => player && player.isPrepared;
        public bool IsPlaying => player && player.isPlaying;
        public double Duration => IsPrepared && !double.IsInfinity(player.length) ? player.length : 0;
        public double TimeSeconds => IsPrepared ? player.time : 0;
        public float Progress => Duration > 0 ? Mathf.Clamp01((float)(TimeSeconds / Duration)) : 0;
        public float Volume { get => audioSource ? audioSource.volume : .7f; set { if (audioSource) audioSource.volume = Mathf.Clamp01(value); } }
        public bool FlipY { get; private set; }
        public ulong FramesReceived { get; private set; }
        VideoPlayer player;
        AudioSource audioSource;
        Material material;
        Coroutine opening;
        bool resumeAfterFocus;
        const string DemoFile = "demo-v2.mp4";

        void Awake() {
            player = GetComponent<VideoPlayer>();
            audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false; audioSource.spatialBlend = 0; audioSource.volume = .7f;
            player.playOnAwake = false; player.isLooping = true; player.waitForFirstFrame = true;
            player.renderMode = VideoRenderMode.RenderTexture; player.aspectRatio = VideoAspectRatio.FitInside;
            player.audioOutputMode = VideoAudioOutputMode.AudioSource;
            player.controlledAudioTrackCount = 1;
            player.EnableAudioTrack(0, true); player.SetTargetAudioSource(0, audioSource);
            player.errorReceived += OnError; player.frameReady += OnFrame;
            player.sendFrameReadyEvents = true;
            if (screenRenderer) material = screenRenderer.material;
            AllocateTexture(1280, 720);
        }
        void Start() { opening = StartCoroutine(CopyAndOpenDemo()); }
        public void PlayDemo() { Begin(CopyAndOpenDemo()); }
        public void OpenLocalFile(string path) {
            if (!File.Exists(path)) { Fail("Selected file is unavailable"); return; }
            Begin(Prepare(path));
        }
        void Begin(IEnumerator routine) {
            if (opening != null) StopCoroutine(opening);
            player.Stop(); opening = StartCoroutine(routine);
        }
        IEnumerator CopyAndOpenDemo() {
            State = PlaybackState.Loading; Status = "Loading bundled video";
            string dest = Path.Combine(Application.persistentDataPath, DemoFile);
            // Versioned cache; never reuse the broken prototype's persistent copy.
            if (!File.Exists(dest) || new FileInfo(dest).Length == 0) {
                using (var req = UnityWebRequest.Get(Application.streamingAssetsPath + "/demo.mp4")) {
                    yield return req.SendWebRequest();
                    if (req.result != UnityWebRequest.Result.Success) { Fail("Bundled video: " + req.error); yield break; }
                    try { File.WriteAllBytes(dest + ".tmp", req.downloadHandler.data); File.Move(dest + ".tmp", dest); }
                    catch (Exception e) { Fail("Video cache: " + e.Message); yield break; }
                }
            }
            yield return Prepare(dest);
        }
        IEnumerator Prepare(string path) {
            State = PlaybackState.Loading; Status = "Preparing video"; FramesReceived = 0;
            player.Stop(); player.source = VideoSource.Url; player.url = path; player.Prepare();
            float deadline = Time.realtimeSinceStartup + 25f;
            while (!player.isPrepared && State != PlaybackState.Error && Time.realtimeSinceStartup < deadline) yield return null;
            if (State == PlaybackState.Error) yield break;
            if (!player.isPrepared) { Fail("Decoder preparation timed out. Retry Demo or choose an H.264/AAC MP4."); yield break; }
            float aspect = player.height > 0 ? (float)player.width / player.height : 16f / 9f;
            float largest = Mathf.Max((float)player.width, (float)player.height);
            float scale = largest > 0 ? Mathf.Min(1, 1920f / largest) : 1;
            int w = player.width > 0 ? Mathf.Max(16, Mathf.RoundToInt(player.width * scale)) : 1280;
            int h = player.height > 0 ? Mathf.Max(16, Mathf.RoundToInt(player.height * scale)) : 720;
            AllocateTexture(w, h);
            if (material) material.SetFloat("_VideoAspect", aspect);
            player.Play(); Status = "Waiting for first decoded frame";
            deadline = Time.realtimeSinceStartup + 12f;
            while (FramesReceived == 0 && State != PlaybackState.Error && Time.realtimeSinceStartup < deadline) yield return null;
            if (State == PlaybackState.Error) yield break;
            if (FramesReceived == 0) { player.Pause(); Fail("Decoder produced no frames. Try Demo again."); yield break; }
            State = PlaybackState.Playing; Status = "Playing"; opening = null;
        }
        void AllocateTexture(int width, int height) {
            if (videoTexture) { player.targetTexture = null; videoTexture.Release(); Destroy(videoTexture); }
            videoTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32) { name = "Spatial decoded video", useMipMap = false };
            videoTexture.Create();
            var previous = RenderTexture.active; RenderTexture.active = videoTexture; GL.Clear(true, true, Color.black); RenderTexture.active = previous;
            player.targetTexture = videoTexture;
            if (material) material.SetTexture("_MainTex", videoTexture);
        }
        void OnFrame(VideoPlayer source, long frame) { FramesReceived++; }
        void OnError(VideoPlayer source, string message) { Fail("Decoder: " + message); }
        void Fail(string message) { State = PlaybackState.Error; Status = message; Debug.LogError("SpatialHDMI: " + message); }
        public void TogglePause() {
            if (!IsPrepared || State == PlaybackState.Loading || State == PlaybackState.Error) return;
            if (player.isPlaying) { player.Pause(); State = PlaybackState.Paused; Status = "Paused"; }
            else { player.Play(); State = PlaybackState.Playing; Status = "Playing"; }
        }
        public void Seek(float normalized) { if (IsPrepared && player.canSetTime && Duration > 0) player.time = Duration * Mathf.Clamp01(normalized); }
        public void SetFlipY(bool flip) { FlipY = flip; if (material) material.SetFloat("_FlipY", flip ? 1 : 0); }
        void OnApplicationFocus(bool focused) {
            if (!player) return;
            if (!focused) { resumeAfterFocus = player.isPlaying; if (resumeAfterFocus) { player.Pause(); State = PlaybackState.Paused; Status = "Paused while app is unfocused"; } }
            else if (resumeAfterFocus && IsPrepared && State != PlaybackState.Error) { player.Play(); State = PlaybackState.Playing; Status = "Playing"; resumeAfterFocus = false; }
        }
        void OnDestroy() {
            if (player) { player.errorReceived -= OnError; player.frameReady -= OnFrame; player.Stop(); }
            if (videoTexture) { videoTexture.Release(); Destroy(videoTexture); }
            if (material) Destroy(material);
        }
    }
}
