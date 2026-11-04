using System;
using UnityEngine;
namespace SpatialHDMI {
    public sealed class SettingsStore : MonoBehaviour {
        public ScreenController placement;
        public DemoVideo video;
        public EnvironmentEffects environment;
        const string Key = "SpatialHDMI.settings.v2";
        [Serializable] class Data { public float width=2, curve=.15f, volume=.7f, glow=.25f, reach=.6f, dim; public bool flip; }
        void Start() {
            if (!PlayerPrefs.HasKey(Key)) return;
            try {
                var d = JsonUtility.FromJson<Data>(PlayerPrefs.GetString(Key));
                if (d==null || !Valid(d.width)||!Valid(d.curve)||!Valid(d.volume)||!Valid(d.glow)||!Valid(d.reach)||!Valid(d.dim)) throw new FormatException("Invalid saved settings");
                placement.SetWidth(d.width); placement.SetCurvature(d.curve); video.Volume=d.volume; video.SetFlipY(d.flip);
                environment.intensity=Mathf.Clamp01(d.glow); environment.reach=Mathf.Clamp(d.reach,.1f,2); environment.dimming=Mathf.Clamp(d.dim,0,.95f);
            } catch (Exception e) { Debug.LogWarning("Spatial settings reset: "+e.Message); }
            // A raw tracking-origin pose is deliberately not restored as a spatial anchor.
        }
        static bool Valid(float x) { return !float.IsNaN(x) && !float.IsInfinity(x); }
        public void Save() {
            if (!placement || !placement.screen || !video || !environment) return;
            var d=new Data { width=placement.screen.Width,curve=placement.screen.curvature,volume=video.Volume,flip=video.FlipY,glow=environment.intensity,reach=environment.reach,dim=environment.dimming };
            PlayerPrefs.SetString(Key,JsonUtility.ToJson(d)); PlayerPrefs.Save();
        }
        void OnApplicationPause(bool paused) { if (paused) Save(); }
        void OnApplicationQuit() { Save(); }
    }
}
