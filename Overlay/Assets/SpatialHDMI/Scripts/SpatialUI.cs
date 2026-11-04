using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace SpatialHDMI {
    public sealed class SpatialUI : MonoBehaviour {
        public ScreenController placement;
        public DemoVideo video;
        public VideoSources sources;
        public EnvironmentEffects environment;
        public SettingsStore settings;
        public Camera xrCamera;
        public HandInteractionGuard hands;
        public RuntimeDiagnostics diagnostics;
        public readonly Dictionary<string, Selectable> Controls = new Dictionary<string, Selectable>();
        public bool Visible => panel && panel.activeSelf;
        public bool Built => panel;
        GameObject panel, quick;
        readonly List<GameObject> pages = new List<GameObject>();
        RectTransform panelRect;
        Text state, detail, playText, lockText;
        Font font;
        float nextRefresh;
        readonly Color ink = new Color(.035f,.055f,.09f,.98f);
        readonly Color surface = new Color(.08f,.12f,.18f,1);
        readonly Color cyan = new Color(.2f,.84f,1,1);
        void Start() { BuildUI(); }
        public void BuildUI() {
            if (panel) return;
            font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            panel=CanvasObject("Spatial control panel",placement.screen.transform,1280,650);
            panelRect=(RectTransform)panel.transform;
            Background(panel.transform,1280,650);
            Label(panel.transform,"SPATIAL HDMI",-590,274,34,TextAnchor.MiddleLeft,580,48);
            state=Label(panel.transform,"Loading video",590,274,25,TextAnchor.MiddleRight,580,48);
            string[] tabs={"Playback","Screen","Atmosphere","Sources"};
            for(int i=0;i<tabs.Length;i++) { int index=i; Button("Tab"+tabs[i],panel.transform,tabs[i],-474+i*247,210,233,64,()=>ShowPage(index)); }
            Button("Hide",panel.transform,"Hide",525,210,170,64,ToggleControls);
            for(int i=0;i<tabs.Length;i++) { var page=Rect("Page "+tabs[i],panel.transform,0,-10,1180,340).gameObject;pages.Add(page); }
            BuildPlayback(pages[0].transform); BuildScreen(pages[1].transform); BuildAtmosphere(pages[2].transform); BuildSources(pages[3].transform);
            detail=Label(panel.transform,"Pinch a cyan corner to move. Use both hands to resize.",0,-255,21,TextAnchor.MiddleCenter,1180,100);
            Label(panel.transform,"Hands: point + pinch / touch    |    Touch: A play, B recenter, X menu",0,-307,22,TextAnchor.MiddleCenter,1180,36);
            quick=CanvasObject("Spatial quick access",xrCamera.transform,500,90);
            quick.transform.localPosition=new Vector3(0,-.46f,1.15f);quick.transform.localRotation=Quaternion.identity;quick.transform.localScale=Vector3.one*.001f;
            Button("QuickControls",quick.transform,"Controls",-127,0,240,86,ToggleControls);
            Button("QuickRecenter",quick.transform,"Recenter",127,0,240,86,placement.Recenter);
            ShowPage(0); Layout(); Refresh();
        }
        void BuildPlayback(Transform page) {
            var play=Button("PlayPause",page,"Play / Pause",-410,116,340,76,video.TogglePause); playText=play.GetComponentInChildren<Text>();
            Button("Restart",page,"Restart",-45,116,340,76,()=>video.Seek(0));
            Button("Recenter",page,"Recenter",320,116,340,76,placement.Recenter);
            Slider("Progress",page,"Timeline",0,0,0,1,0,video.Seek,1120);
            Slider("Volume",page,"Volume",0,-112,0,1,.7f,v=>video.Volume=v,1120);
        }
        void BuildScreen(Transform page) {
            Slider("Width",page,"Width (m)",-310,121,.8f,5,2,placement.SetWidth,540);
            Slider("Distance",page,"Distance (m)",310,121,.7f,8,2.5f,placement.SetDistance,540);
            Slider("Curvature",page,"Curvature",-310,8,0,1,.15f,placement.SetCurvature,540);
            Button("FlatCurve",page,"Flat / Curved",162,10,270,64,placement.ToggleCurve);
            var locked=Button("Lock",page,"Lock position",452,10,270,64,placement.ToggleLock);lockText=locked.GetComponentInChildren<Text>();
            string[] captions={"Left","Right","Up","Down","Near","Far"};
            Vector3[] moves={Vector3.left,Vector3.right,Vector3.up,Vector3.down,Vector3.back,Vector3.forward};
            for(int i=0;i<6;i++) { Vector3 direction=moves[i]*.1f;Button("Move"+captions[i],page,captions[i],-490+i*196,-85,180,59,()=>placement.Nudge(direction)); }
            Button("TiltDown",page,"Tilt -5°",-490,-158,180,56,()=>placement.Tilt(-5));
            Button("TiltUp",page,"Tilt +5°",-294,-158,180,56,()=>placement.Tilt(5));
            Button("YawLeft",page,"Turn -5°",-98,-158,180,56,()=>placement.Yaw(-5));
            Button("YawRight",page,"Turn +5°",98,-158,180,56,()=>placement.Yaw(5));
            Button("RollLeft",page,"Roll -5°",294,-158,180,56,()=>placement.Roll(-5));
            Button("RollRight",page,"Roll +5°",490,-158,180,56,()=>placement.Roll(5));
        }
        void BuildAtmosphere(Transform page) {
            Slider("Glow",page,"Virtual ambient light",-310,120,0,1,.25f,v=>environment.intensity=v,540);
            Slider("Reach",page,"Glow spread",310,120,.1f,2,.6f,v=>environment.reach=v,540);
            Slider("Dim",page,"Room darkening",-310,0,0,.95f,0,v=>environment.dimming=v,540);
            Button("FlipY",page,"Flip video vertically",310,0,540,68,()=>video.SetFlipY(!video.FlipY));
            Label(page,"Virtual color glow follows the video.\nRoom darkening uses passthrough composition alpha.\nIt does not change physical lights.",0,-125,26,TextAnchor.MiddleCenter,1120,120);
        }
        void BuildSources(Transform page) {
            Button("Demo",page,"Bundled demo",-400,110,360,85,sources.SelectDemo);
            Button("Local",page,"Open local video",0,110,360,85,sources.ChooseLocalFile);
            Button("Usb",page,"Inspect USB",400,110,360,85,sources.InspectUsb);
            Button("Diagnostics",page,"Save device diagnostics",0,-155,520,60,()=>{if(diagnostics)diagnostics.Save();});
            Label(page,"Local video: start with an unprotected H.264 / AAC MP4.\nThe Android picker copies your selected file into app storage.\nUSB HDMI capture is a separate module and is not available in this build.",0,-25,25,TextAnchor.MiddleCenter,1120,160);
        }
        public void ToggleControls() { if (!panel) BuildUI(); panel.SetActive(!panel.activeSelf); if (!panel.activeSelf && settings) settings.Save(); }
        void ShowPage(int index) { for(int i=0;i<pages.Count;i++) pages[i].SetActive(i==index); }
        void Layout() {
            if (!panelRect) return;
            float s=Mathf.Max(.1f,placement.screen.transform.localScale.x);
            panelRect.localScale=Vector3.one*(.00112f/s);
            panelRect.localPosition=new Vector3(0,-.5625f-.39f/s,-.08f);
        }
        void Update() { if (!panel) return; Layout(); if (Time.unscaledTime<nextRefresh) return;nextRefresh=Time.unscaledTime+.2f;Refresh(); }
        void Refresh() {
            if (!state) return;
            state.text=video.State+"  "+Clock(video.TimeSeconds)+" / "+Clock(video.Duration);
            state.color=video.State==DemoVideo.PlaybackState.Error ? new Color(1,.45f,.35f) : cyan;
            playText.text=video.IsPlaying ? "Pause" : "Play";lockText.text=placement.Locked ? "Unlock position" : "Lock position";
            detail.text=video.Status+"\n"+placement.InteractionStatus+"\n"+sources.Status+(hands ? "\n"+hands.Status : "");
            Sync("Progress",video.Progress);Sync("Volume",video.Volume);Sync("Width",placement.screen.Width);Sync("Distance",placement.Distance);Sync("Curvature",placement.screen.curvature);
            Sync("Glow",environment.intensity);Sync("Reach",environment.reach);Sync("Dim",environment.dimming);
        }
        static string Clock(double seconds) { if(double.IsNaN(seconds)||double.IsInfinity(seconds)||seconds<0)return "--:--";int t=(int)seconds;return (t/60).ToString("00")+":"+(t%60).ToString("00"); }
        void Sync(string key,float value) {
            var slider=Controls[key] as Slider;if(!slider)return;
            var gesture=slider.GetComponent<SliderGesture>();if(!gesture.Held)slider.SetValueWithoutNotify(value);
            var valueText=slider.transform.parent.Find(key+"Value").GetComponent<Text>();
            valueText.text=key=="Progress" ? Clock(video.TimeSeconds)+" / "+Clock(video.Duration) : value.ToString("0.00");
        }
        GameObject CanvasObject(string name,Transform parent,int width,int height) {
            var go=Rect(name,parent,0,0,width,height).gameObject;
            var canvas=go.AddComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=xrCamera;
            go.AddComponent<TrackedDeviceGraphicRaycaster>();return go;
        }
        void Background(Transform parent,float width,float height) { var r=Rect("Panel background",parent,0,0,width,height);var image=r.gameObject.AddComponent<Image>();image.color=ink; }
        RectTransform Rect(string name,Transform parent,float x,float y,float width,float height) {
            var go=new GameObject(name,typeof(RectTransform));var r=go.GetComponent<RectTransform>();r.SetParent(parent,false);r.sizeDelta=new Vector2(width,height);r.anchoredPosition=new Vector2(x,y);return r;
        }
        Text Label(Transform parent,string content,float x,float y,int size,TextAnchor anchor,float width,float height) {
            var r=Rect("Text",parent,x,y,width,height);var text=r.gameObject.AddComponent<Text>();text.font=font;text.fontSize=size;text.text=content;text.alignment=anchor;text.color=Color.white;text.raycastTarget=false;
            // Positions represent the center except for explicit left / right aligned headings.
            if(anchor==TextAnchor.MiddleLeft) r.pivot=new Vector2(0,.5f);
            if(anchor==TextAnchor.MiddleRight) r.pivot=new Vector2(1,.5f);
            return text;
        }
        Button Button(string key,Transform parent,string caption,float x,float y,float width,float height,UnityAction action) {
            var r=Rect(key,parent,x,y,width,height);var image=r.gameObject.AddComponent<Image>();image.color=Color.white;
            var button=r.gameObject.AddComponent<Button>();button.targetGraphic=image;
            var colors=button.colors;colors.normalColor=surface;colors.highlightedColor=new Color(.12f,.38f,.48f,1);colors.pressedColor=cyan;colors.selectedColor=surface;button.colors=colors;
            button.navigation=new Navigation { mode=Navigation.Mode.None };button.onClick.AddListener(action);
            Label(r,caption,0,0,26,TextAnchor.MiddleCenter,width-18,height-6);Controls.Add(key,button);return button;
        }
        void Slider(string key,Transform parent,string caption,float x,float y,float min,float max,float initial,UnityAction<float> action,float width) {
            var group=Rect(key+"Group",parent,x,y,width,90);
            Label(group,caption,-width/2,32,25,TextAnchor.MiddleLeft,width*.7f,36);
            var value=Label(group,"",width/2,32,24,TextAnchor.MiddleRight,width*.4f,36);value.name=key+"Value";
            var r=Rect(key,group,0,-12,width,55);var slider=r.gameObject.AddComponent<Slider>();
            var background=Rect("Track",r,0,0,width,14).gameObject.AddComponent<Image>();background.color=surface;
            var track=Rect("Handle area",r,0,0,width-44,55);
            var handle=Rect("Handle",track,0,0,44,55);var image=handle.gameObject.AddComponent<Image>();image.color=cyan;
            // A full-width target also accepts a pinch anywhere on the track.
            var hit=r.gameObject.AddComponent<Image>();hit.color=new Color(0,0,0,.001f);
            slider.handleRect=handle;slider.targetGraphic=image;slider.minValue=min;slider.maxValue=max;slider.SetValueWithoutNotify(initial);
            slider.navigation=new Navigation { mode=Navigation.Mode.None };slider.onValueChanged.AddListener(action);
            r.gameObject.AddComponent<SliderGesture>();Controls.Add(key,slider);
        }
        // The editor validator builds and exercises the exact same UI, then removes its preview.
        public void DestroyGeneratedUI() {
            if (panel) { if(Application.isPlaying)Destroy(panel);else DestroyImmediate(panel); }
            if (quick) { if(Application.isPlaying)Destroy(quick);else DestroyImmediate(quick); }
            pages.Clear();Controls.Clear();panel=null;quick=null;
        }
        void OnDestroy() { if(quick)Destroy(quick); }
    }
}
