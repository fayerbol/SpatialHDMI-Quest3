using System;
using System.IO;
using UnityEngine;

namespace SpatialHDMI {
    // The live-source contract is independent of VideoPlayer; UVC can supply external textures.
    public interface ILiveVideoSource : IDisposable {
        Texture Frame { get; }
        string Status { get; }
        bool IsStreaming { get; }
        void StartCapture();
        void StopCapture();
    }
    public sealed class VideoSources : MonoBehaviour {
        public DemoVideo video;
        public string Status { get; private set; } = "Bundled demo / local MP4";
        public void ChooseLocalFile() {
#if UNITY_ANDROID && !UNITY_EDITOR
            try {
                using (var unity=new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity=unity.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var intent=new AndroidJavaObject("android.content.Intent")) {
                    intent.Call<AndroidJavaObject>("setClassName",activity, "com.spatialhdmi.bridge.MediaPickerActivity").Dispose();
                    intent.Call<AndroidJavaObject>("putExtra","unityTarget",gameObject.name).Dispose();
                    activity.Call("startActivity",intent);
                    Status="Choose a video in the Android file picker";
                }
            } catch (Exception e) { Status="File picker unavailable: "+e.Message; }
#else
            Status="Android file picker is available in the Quest APK. Editor: use OpenImportedFile(path).";
#endif
        }
        // Called by the native picker after copying a content URI into app-owned storage.
        [UnityEngine.Scripting.Preserve]
        public void OnFileSelected(string result) {
            if (string.IsNullOrEmpty(result)) { Status="Selection cancelled"; return; }
            if (result.StartsWith("ERROR:")) { Status=result; return; }
            OpenImportedFile(result);
        }
        public void OpenImportedFile(string path) {
            if (!File.Exists(path)) { Status="Selected file not found"; return; }
            Status="Local: "+Path.GetFileName(path); video.OpenLocalFile(path);
        }
        public void SelectDemo() { Status="Bundled demo"; video.PlayDemo(); }
        public void InspectUsb() {
#if UNITY_ANDROID && !UNITY_EDITOR
            try {
                using(var unity=new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using(var activity=unity.GetStatic<AndroidJavaObject>("currentActivity"))
                using(var manager=activity.Call<AndroidJavaObject>("getSystemService","usb"))
                using(var devices=manager.Call<AndroidJavaObject>("getDeviceList")) {
                    int count=devices.Call<int>("size");
                    Status="USB devices: "+count+". UVC decoder / UAC audio backend not installed; capture unavailable.";
                }
            } catch(Exception e) { Status="USB inspection: "+e.Message; }
#else
            Status="USB capture needs an Android UVC/UAC backend and a real capture card. Demo remains available.";
#endif
        }
    }
}
