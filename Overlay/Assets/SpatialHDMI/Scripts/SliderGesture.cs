using UnityEngine;
using UnityEngine.EventSystems;
namespace SpatialHDMI {
    public sealed class SliderGesture : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IEndDragHandler {
        public bool Held { get; private set; }
        public void OnPointerDown(PointerEventData data) { Held=true; }
        public void OnPointerUp(PointerEventData data) { Held=false; }
        public void OnEndDrag(PointerEventData data) { Held=false; }
        void OnDisable() { Held=false; }
    }
}
