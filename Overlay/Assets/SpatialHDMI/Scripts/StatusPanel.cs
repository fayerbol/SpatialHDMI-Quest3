using UnityEngine;
using UnityEngine.XR;
namespace SpatialHDMI {
    public sealed class StatusPanel : MonoBehaviour {
        public Transform head; public DemoVideo demo; public SpatialScreen screen;
        TextMesh text; GameObject panel; bool previous; bool visible=true; float next;
        void Start() {
            if (!head || !demo || !screen) { Debug.LogError("Spatial HDMI: HUD references missing"); enabled=false; return; }
            panel=GameObject.CreatePrimitive(PrimitiveType.Quad); panel.name="Spatial HDMI status"; Destroy(panel.GetComponent<Collider>());
            panel.transform.SetParent(head,false); panel.transform.localPosition=new Vector3(0,-.36f,1.2f); panel.transform.localRotation=Quaternion.identity; panel.transform.localScale=new Vector3(.98f,.29f,1);
            var shader=Shader.Find("Unlit/Color"); if(shader) { var mat=new Material(shader); mat.color=new Color(.035f,.045f,.075f,1); panel.GetComponent<Renderer>().material=mat; }
            var label=new GameObject("Controls"); label.transform.SetParent(panel.transform,false); label.transform.localPosition=new Vector3(-.45f,.40f,-.01f); label.transform.localScale=new Vector3(.0012f,.0040f,1);
            text=label.AddComponent<TextMesh>(); text.fontSize=48; text.characterSize=1; text.color=Color.white; text.anchor=TextAnchor.UpperLeft;
        }
        void Update() {
            var left=InputDevices.GetDeviceAtXRNode(XRNode.LeftHand); left.TryGetFeatureValue(CommonUsages.primaryButton,out bool button);
            if (button && !previous) { visible=!visible; if(panel) panel.SetActive(visible); } previous=button;
            if (text && Time.unscaledTime>next) { next=Time.unscaledTime+.25f; text.text="SPATIAL HDMI  /  DEMO\n"+demo.Status+"\nLEFT STICK: move    + GRIP: size / curve\nRIGHT STICK: distance    + GRIP: rotate\nA: pause / play   B: recenter   X: hide"; }
        }
        void OnDestroy() { if(panel) Destroy(panel); }
    }
}
