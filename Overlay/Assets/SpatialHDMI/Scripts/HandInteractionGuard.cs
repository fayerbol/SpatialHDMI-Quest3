using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit;

namespace SpatialHDMI {
    // XRI supplies poke / ray / direct interaction. This layer gates recovery and system gestures.
    [DefaultExecutionOrder(50)]
    public sealed class HandInteractionGuard : MonoBehaviour {
        public Transform leftRoot, rightRoot;
        public XRBaseInteractor[] leftInteractors, rightInteractors;
        public ScreenController placement;
        public SpatialUI ui;
        public string Status { get; private set; } = "Waiting for XR hand subsystem";
        readonly List<XRHandSubsystem> subsystems = new List<XRHandSubsystem>();
        XRHandSubsystem hands;
        bool leftRelease=true, rightRelease=true, previousMenu, focused=true;
        float nextDiscovery;
        void Update() {
            if ((hands==null || !hands.running) && Time.unscaledTime>=nextDiscovery) {
                nextDiscovery=Time.unscaledTime+.5f;SubsystemManager.GetSubsystems(subsystems);
                hands=null;foreach(var candidate in subsystems)if(candidate.running){hands=candidate;break;}
            }
            bool leftTracked=hands!=null && hands.running && hands.leftHand.isTracked && focused;
            bool rightTracked=hands!=null && hands.running && hands.rightHand.isTracked && focused;
            Process(leftTracked,MetaAimHand.left,leftRoot,leftInteractors,ref leftRelease);
            Process(rightTracked,MetaAimHand.right,rightRoot,rightInteractors,ref rightRelease);
            bool menu=Menu(MetaAimHand.left)||Menu(MetaAimHand.right);
            if(menu && !previousMenu && focused && ui) ui.ToggleControls();previousMenu=menu;
            Status="Hands L: "+(leftTracked ? leftRelease ? "release pinch" : "tracked" : "lost")+"  R: "+(rightTracked ? rightRelease ? "release pinch" : "tracked" : "lost");
        }
        static bool Menu(MetaAimHand hand) { return hand!=null && (((MetaAimFlags)hand.aimFlags.ReadValue()) & MetaAimFlags.MenuPressed)!=0; }
        void Process(bool tracked,MetaAimHand aim,Transform root,XRBaseInteractor[] interactors,ref bool release) {
            bool systemGesture=aim!=null && (((MetaAimFlags)aim.aimFlags.ReadValue()) & MetaAimFlags.SystemGesture)!=0;
            if(!tracked || systemGesture) { release=true;CancelIfHeldBy(root); }
            else if(aim!=null && !aim.indexPressed.isPressed) release=false;
            bool enabledNow=tracked && !release && !systemGesture;
            if(interactors!=null)foreach(var interactor in interactors)if(interactor)interactor.enabled=enabledNow;
        }
        void CancelIfHeldBy(Transform root) {
            if(!placement || !placement.grab || !root)return;
            foreach(var interactor in placement.grab.interactorsSelecting) {
                if(interactor.transform.IsChildOf(root)) { placement.CancelGrab();break; }
            }
        }
        void OnApplicationFocus(bool focus) { focused=focus;if(!focus){leftRelease=rightRelease=true;placement.CancelGrab();} }
    }
}
