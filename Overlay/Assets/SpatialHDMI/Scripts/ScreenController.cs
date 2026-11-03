using UnityEngine;
using UnityEngine.XR;

namespace SpatialHDMI {
    public sealed class ScreenController : MonoBehaviour {
        public SpatialScreen screen;
        public Transform head;
        public DemoVideo demo;
        public float moveSpeed=1f;
        bool prevA, prevB, dirty;
        float lastChange;
        const string Key="SpatialHDMI.pose.v1";
        [System.Serializable] class Data { public Vector3 position; public Quaternion rotation; public float width,curve; }
        void Start() {
            if (!screen || !head) { enabled=false; return; }
            if (PlayerPrefs.HasKey(Key)) {
                try { var d=JsonUtility.FromJson<Data>(PlayerPrefs.GetString(Key)); screen.transform.SetPositionAndRotation(d.position,d.rotation); screen.width=Mathf.Clamp(d.width,.5f,5f); screen.curvature=Mathf.Clamp01(d.curve); screen.Rebuild(); }
                catch (System.Exception e) { Debug.LogWarning(e.Message); Recenter(); }
            } else Recenter();
        }
        public void Recenter() {
            Vector3 f=Vector3.ProjectOnPlane(head.forward,Vector3.up).normalized; if (f.sqrMagnitude<.01f) f=Vector3.forward;
            screen.transform.position=head.position+f*2.5f; screen.transform.rotation=Quaternion.LookRotation(f,Vector3.up); Changed();
        }
        void Update() {
            var left=InputDevices.GetDeviceAtXRNode(XRNode.LeftHand); var right=InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            left.TryGetFeatureValue(CommonUsages.primary2DAxis,out Vector2 l); right.TryGetFeatureValue(CommonUsages.primary2DAxis,out Vector2 r);
            left.TryGetFeatureValue(CommonUsages.gripButton,out bool lg); right.TryGetFeatureValue(CommonUsages.gripButton,out bool rg);
            right.TryGetFeatureValue(CommonUsages.primaryButton,out bool a); right.TryGetFeatureValue(CommonUsages.secondaryButton,out bool b);
            if (a && !prevA && demo) demo.TogglePause(); if (b && !prevB) Recenter(); prevA=a; prevB=b;
            float dt=Time.deltaTime; bool change=false;
            if (l.sqrMagnitude>.04f) { if (lg) { screen.width=Mathf.Clamp(screen.width+l.y*dt, .5f,5f); screen.curvature=Mathf.Clamp01(screen.curvature+l.x*dt*.4f); screen.Rebuild(); } else { screen.transform.position+=(head.right*l.x+Vector3.up*l.y)*moveSpeed*dt; } change=true; }
            if (r.sqrMagnitude>.04f) { if (rg) screen.transform.Rotate(Vector3.up,r.x*50f*dt,Space.World); else screen.transform.position+=Vector3.ProjectOnPlane(head.forward,Vector3.up).normalized*r.y*moveSpeed*dt; change=true; }
            if (change) Changed(); if (dirty && Time.unscaledTime-lastChange>1f) Save();
        }
        void Changed() { dirty=true; lastChange=Time.unscaledTime; }
        void Save() { if (!screen) return; var d=new Data {position=screen.transform.position,rotation=screen.transform.rotation,width=screen.width,curve=screen.curvature}; PlayerPrefs.SetString(Key,JsonUtility.ToJson(d)); PlayerPrefs.Save(); dirty=false; }
        void OnApplicationPause(bool pause) { if (pause && dirty) Save(); }
        void OnApplicationQuit() { if (dirty) Save(); }
    }
}
