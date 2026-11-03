using UnityEngine;
using UnityEngine.XR;
namespace SpatialHDMI {
    // XR input pose is local to the tracking origin. No device means keep the last pose.
    public sealed class HeadTracking : MonoBehaviour {
        void Update() {
            var h = InputDevices.GetDeviceAtXRNode(XRNode.CenterEye);
            if (!h.isValid) h = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            if (h.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 p)) transform.localPosition = p;
            if (h.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion q)) transform.localRotation = q;
        }
    }
}
