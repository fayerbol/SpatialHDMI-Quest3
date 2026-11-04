using System.Collections;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

namespace SpatialHDMI {
    // Object pose stays in tracking space. No camera-follow component is put on the screen.
    public sealed class ScreenController : MonoBehaviour {
        public SpatialScreen screen;
        public Transform head;
        public XRGrabInteractable grab;
        public Transform[] handles;
        public Renderer[] handleVisuals;
        public bool Locked { get; private set; }
        public string InteractionStatus => Locked ? "Position locked" : grab && grab.isSelected ? "Moving / two hands to scale" : grab && grab.isHovered ? "Pinch a handle to move" : "Grab a cyan corner; two hands resize";
        MaterialPropertyBlock colors;
        bool previousA, previousB, previousX;
        public DemoVideo demo;
        public SpatialUI ui;
        Vector3 lastValidPosition; Quaternion lastValidRotation;
        bool validPose;
        IEnumerator Start() {
            // Wait for tracking instead of placing relative to the prefab camera's edit-time pose.
            float deadline = Time.realtimeSinceStartup + 10;
            while (Time.realtimeSinceStartup < deadline) {
                var device = InputDevices.GetDeviceAtXRNode(XRNode.Head);
                if (device.TryGetFeatureValue(CommonUsages.isTracked, out bool tracked) && tracked) break;
                yield return null;
            }
            Recenter();
        }
        public void Recenter() {
            if (!head || !screen) return;
            CancelGrab();
            // Full head orientation permits a ceiling screen when lying down.
            screen.transform.SetPositionAndRotation(head.position + head.forward * 2.5f, head.rotation);
            RememberPose();
        }
        public void SetWidth(float metres) {
            if (!screen) return;
            CancelGrab(); screen.transform.localScale = Vector3.one * (Mathf.Clamp(metres, .8f, 5f) / SpatialScreen.BaseWidth);
        }
        public void SetDistance(float metres) {
            CancelGrab(); var direction = screen.transform.position - head.position;
            if (direction.sqrMagnitude < .01f) direction = head.forward;
            screen.transform.position = head.position + direction.normalized * Mathf.Clamp(metres, .7f, 8);
        }
        public float Distance => screen && head ? Vector3.Distance(screen.transform.position, head.position) : 2.5f;
        public void SetCurvature(float value) { screen.curvature = Mathf.Clamp01(value); screen.Rebuild(); LayoutHandles(); }
        public void ToggleCurve() { SetCurvature(screen.curvature > .01f ? 0 : .35f); }
        public void Nudge(Vector3 local) { CancelGrab(); screen.transform.position += head.TransformDirection(local); }
        public void Tilt(float degrees) { CancelGrab(); screen.transform.Rotate(Vector3.right, degrees, Space.Self); }
        public void Yaw(float degrees) { CancelGrab(); screen.transform.Rotate(Vector3.up, degrees, Space.Self); }
        public void Roll(float degrees) { CancelGrab(); screen.transform.Rotate(Vector3.forward, degrees, Space.Self); }
        public void ToggleLock() { Locked = !Locked; CancelGrab(); if (grab) grab.enabled = !Locked; }
        public void CancelGrab() {
            if (grab && grab.isSelected && grab.interactionManager)
                grab.interactionManager.CancelInteractableSelection((IXRSelectInteractable)grab);
        }
        public void LayoutHandles() {
            if (handles == null || handles.Length != 4 || !screen) return;
            for (int i = 0; i < 4; i++) handles[i].localPosition = screen.Point(i % 2, i < 2 ? -.5f : .5f) + new Vector3(0,0,-.025f);
        }
        void Update() {
            var right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand); var left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            right.TryGetFeatureValue(CommonUsages.primaryButton, out bool a); right.TryGetFeatureValue(CommonUsages.secondaryButton, out bool b);
            left.TryGetFeatureValue(CommonUsages.primaryButton, out bool x);
            if (a && !previousA && demo) demo.TogglePause();
            if (b && !previousB) Recenter();
            if (x && !previousX && ui) ui.ToggleControls();
            previousA = a; previousB = b; previousX = x;
        }
        void LateUpdate() {
            if (!screen) return;
            var t = screen.transform;
            if (!Finite(t.position) || !Finite(t.localScale) || !Finite(new Vector3(t.rotation.x,t.rotation.y,t.rotation.z)) || float.IsNaN(t.rotation.w) || float.IsInfinity(t.rotation.w)) {
                CancelGrab(); if (validPose) t.SetPositionAndRotation(lastValidPosition, lastValidRotation); else Recenter();
                t.localScale = Vector3.one;
            }
            // Absolute limits remain valid after repeated two-hand grabs.
            float s = Mathf.Clamp(t.localScale.x, .4f, 2.5f); t.localScale = Vector3.one * s;
            RememberPose(); LayoutHandles();
            if (colors == null) colors = new MaterialPropertyBlock();
            colors.SetColor("_Color", Locked ? new Color(.2f,.25f,.3f,1) : grab && grab.isSelected ? new Color(1,.7f,.18f,1) : grab && grab.isHovered ? Color.white : new Color(.15f,.85f,1,1));
            if (handleVisuals != null) foreach (var renderer in handleVisuals) if (renderer) renderer.SetPropertyBlock(colors);
        }
        static bool Finite(Vector3 v) { return !(float.IsNaN(v.x)||float.IsInfinity(v.x)||float.IsNaN(v.y)||float.IsInfinity(v.y)||float.IsNaN(v.z)||float.IsInfinity(v.z)); }
        void RememberPose() { lastValidPosition = screen.transform.position; lastValidRotation = screen.transform.rotation; validPose = true; }
        void OnApplicationFocus(bool focus) { if (!focus) CancelGrab(); }
        void OnDisable() { CancelGrab(); }
    }
}
