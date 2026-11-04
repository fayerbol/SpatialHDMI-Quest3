using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Hands.OpenXR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Transformers;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Meta;
using Unity.XR.CoreUtils;
using SpatialHDMI;

public static class SpatialBuild {
    public const string ScenePath="Assets/SpatialHDMI/Scenes/SpatialHDMI_Main.unity";
    const string RigPath="Assets/Samples/XR Interaction Toolkit/2.5.1/Hands Interaction Demo/Prefabs/XR Interaction Hands Setup.prefab";
    const string MaterialPath="Assets/SpatialHDMI/Resources/SpatialVideo.mat";
    static void Require(bool condition,string message) { if(!condition)throw new Exception("Spatial prebuild validation: "+message); }
    [MenuItem("Spatial HDMI/Validate scene and configuration")]
    public static void Validate() { PrepareScene();Debug.Log("SPATIAL_VALIDATION_PASS: C# loaded, scene, XR, shaders and UI actions checked. Device behavior untested."); }
    [MenuItem("Spatial HDMI/Build Android")]
    public static void Build() {
        PrepareScene();Directory.CreateDirectory("build/Android");
        string output=Path.GetFullPath("build/Android/SpatialHDMI.apk");if(File.Exists(output))File.Delete(output);
        var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes=new[]{ScenePath},locationPathName=output,target=BuildTarget.Android,options=BuildOptions.None });
        Require(result.summary.result==BuildResult.Succeeded && File.Exists(output) && new FileInfo(output).Length>0,"Android APK generation failed: "+result.summary.result);
        Debug.Log("APK_CREATED: "+output);
    }
    // Keep this public entry point for the existing UBA pre-export method.
    public static void PrepareScene() {
        Require(EditorUserBuildSettings.activeBuildTarget==BuildTarget.Android,"Start Unity with -buildTarget Android");
        Require(Application.unityVersion=="2022.3.8f1","Expected pinned Unity 2022.3.8f1; found "+Application.unityVersion);
        foreach(var file in new[]{"Assets/StreamingAssets/demo.mp4","Assets/SpatialHDMI/Resources/Screen.shader","Assets/SpatialHDMI/Resources/SpatialTint.shader",RigPath,"Packages/manifest.json"})
            Require(File.Exists(file),"Missing required file: "+file);
        Require(new FileInfo("Assets/StreamingAssets/demo.mp4").Length>1000000,"Demo is missing or a Git LFS pointer");
        var xr=XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Android);
        Require(xr!=null && xr.Manager!=null && xr.Manager.activeLoaders.Any(l=>l && l.GetType().FullName.Contains("OpenXRLoader")),"Android OpenXR loader missing");
        xr.InitManagerOnStart=true;
        EditorUtility.SetDirty(xr);EditorUtility.SetDirty(xr.Manager);
        var settings=OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
        Require(settings!=null,"Android OpenXR settings missing");
        Require(settings.GetFeature<HandTracking>()!=null && settings.GetFeature<HandTracking>().enabled,"Hand Tracking Subsystem feature disabled");
        Require(settings.GetFeature<MetaHandTrackingAim>()!=null && settings.GetFeature<MetaHandTrackingAim>().enabled,"Meta Hand Tracking Aim feature disabled");
        Require(settings.GetFeature<ARCameraFeature>()!=null && settings.GetFeature<ARCameraFeature>().enabled,"Meta passthrough camera feature disabled");
        Require(settings.GetFeature<ARSessionFeature>()!=null && settings.GetFeature<ARSessionFeature>().enabled,"Meta AR session feature disabled");
        Require(settings.GetFeatures().Any(f=>f && f.enabled && f.GetType().Name=="OculusTouchControllerProfile"),"Touch controller profile disabled");
        Require(GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset,"Expected Universal Render Pipeline");
        PlayerSettings.companyName="Spatial HDMI";PlayerSettings.productName="Spatial HDMI";PlayerSettings.bundleVersion="0.2.0";
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android,"com.spatialhdmi.demo");
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android,ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel29;
        PlayerSettings.Android.bundleVersionCode=8;EditorUserBuildSettings.buildAppBundle=false;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{GraphicsDeviceType.OpenGLES3});
        MakeScene();EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
        ValidateGeneratedScene();
        AssetDatabase.SaveAssets();AssetDatabase.Refresh();
    }
    static void MakeScene() {
        var shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/SpatialHDMI/Resources/Screen.shader");
        var tint=AssetDatabase.LoadAssetAtPath<Shader>("Assets/SpatialHDMI/Resources/SpatialTint.shader");
        Require(shader && tint,"Spatial shaders failed to import");
        Require(!ShaderUtil.ShaderHasError(shader) && !ShaderUtil.ShaderHasError(tint),"Shader compiler reports an error");
        Directory.CreateDirectory("Assets/SpatialHDMI/Scenes");
        var material=AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if(!material){material=new Material(shader);AssetDatabase.CreateAsset(material,MaterialPath);}else{material.shader=shader;EditorUtility.SetDirty(material);}
        var handleMaterial=new Material(tint);const string handlePath="Assets/SpatialHDMI/Resources/Handle.mat";
        if(AssetDatabase.LoadAssetAtPath<Material>(handlePath)){UnityEngine.Object.DestroyImmediate(handleMaterial);handleMaterial=AssetDatabase.LoadAssetAtPath<Material>(handlePath);handleMaterial.shader=tint;}else AssetDatabase.CreateAsset(handleMaterial,handlePath);
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(RigPath);Require(prefab,"Official hand rig prefab missing");
        var rig=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);
        var origin=rig.GetComponentInChildren<XROrigin>(true);Require(origin && origin.Camera,"Official rig camera missing");
        var camera=origin.Camera;camera.tag="MainCamera";camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.nearClipPlane=.05f;
        camera.allowHDR=false;camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
        if(!camera.GetComponent<ARCameraManager>())camera.gameObject.AddComponent<ARCameraManager>();
        new GameObject("AR Session").AddComponent<ARSession>();
        foreach(var provider in rig.GetComponentsInChildren<LocomotionProvider>(true))provider.enabled=false;
        // The official rig includes input actions, tracking driver, hand visuals, poke/ray/direct interactors and Touch controllers.
        var root=new GameObject("Spatial Screen");root.transform.position=new Vector3(0,1.5f,2.5f);
        root.AddComponent<MeshFilter>();var renderer=root.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
        renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        var screen=root.AddComponent<SpatialScreen>();
        var body=root.AddComponent<Rigidbody>();body.useGravity=false;body.isKinematic=true;
        var grab=root.AddComponent<XRGrabInteractable>();grab.selectMode=InteractableSelectMode.Multiple;grab.movementType=XRBaseInteractable.MovementType.Instantaneous;
        grab.throwOnDetach=false;grab.useDynamicAttach=true;grab.trackScale=true;grab.addDefaultGrabTransformers=false;
        var transformer=root.AddComponent<XRGeneralGrabTransformer>();transformer.allowOneHandedScaling=false;transformer.allowTwoHandedScaling=true;
        transformer.minimumScaleRatio=.4f;transformer.maximumScaleRatio=2.5f;transformer.thresholdMoveRatioForScale=.05f;
        transformer.allowTwoHandedRotation=XRGeneralGrabTransformer.TwoHandedRotationMode.TwoHandedAverage;
        grab.startingSingleGrabTransformers=new List<XRBaseGrabTransformer>{transformer};grab.startingMultipleGrabTransformers=new List<XRBaseGrabTransformer>{transformer};
        grab.interactionManager=rig.GetComponentInChildren<XRInteractionManager>(true);
        var placement=root.AddComponent<ScreenController>();placement.screen=screen;placement.head=camera.transform;placement.grab=grab;
        placement.handles=new Transform[4];placement.handleVisuals=new Renderer[4];grab.colliders.Clear();
        for(int i=0;i<4;i++) {
            var handle=GameObject.CreatePrimitive(PrimitiveType.Sphere);handle.name="Grab corner "+i;handle.transform.SetParent(root.transform,false);handle.transform.localScale=Vector3.one*.09f;
            placement.handles[i]=handle.transform;placement.handleVisuals[i]=handle.GetComponent<Renderer>();placement.handleVisuals[i].sharedMaterial=handleMaterial;
            grab.colliders.Add(handle.GetComponent<Collider>());
        }
        placement.LayoutHandles();
        var media=new GameObject("Spatial Video Sources");media.AddComponent<VideoPlayer>().playOnAwake=false;media.AddComponent<AudioSource>().playOnAwake=false;
        var video=media.AddComponent<DemoVideo>();video.screenRenderer=renderer;placement.demo=video;
        var sources=media.AddComponent<VideoSources>();sources.video=video;
        var effects=new GameObject("Environment effects").AddComponent<EnvironmentEffects>();effects.screen=screen;effects.video=video;effects.xrCamera=camera;
        var store=new GameObject("Settings").AddComponent<SettingsStore>();store.placement=placement;store.video=video;store.environment=effects;
        var ui=new GameObject("Spatial UI").AddComponent<SpatialUI>();ui.placement=placement;ui.video=video;ui.sources=sources;ui.environment=effects;ui.settings=store;ui.xrCamera=camera;placement.ui=ui;
        var guard=rig.AddComponent<HandInteractionGuard>();guard.placement=placement;guard.ui=ui;ui.hands=guard;
        guard.leftRoot=Find(rig.transform,"Left Hand");guard.rightRoot=Find(rig.transform,"Right Hand");
        Require(guard.leftRoot && guard.rightRoot,"Official left/right hand transforms missing");
        guard.leftInteractors=guard.leftRoot.GetComponentsInChildren<XRBaseInteractor>(true);guard.rightInteractors=guard.rightRoot.GetComponentsInChildren<XRBaseInteractor>(true);
        var diagnostics=media.AddComponent<RuntimeDiagnostics>();diagnostics.video=video;diagnostics.hands=guard;diagnostics.environment=effects;ui.diagnostics=diagnostics;
        Require(EditorSceneManager.SaveScene(scene,ScenePath),"Could not save generated scene");
    }
    static Transform Find(Transform root,string name) { return root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name==name); }
    static T One<T>() where T:UnityEngine.Object {
        var objects=UnityEngine.Object.FindObjectsOfType<T>(true);Require(objects.Length==1,"Expected exactly one "+typeof(T).Name+", found "+objects.Length);return objects[0];
    }
    public static void ValidateGeneratedScene() {
        var placement=One<ScreenController>();var ui=One<SpatialUI>();var video=One<DemoVideo>();
        Require(One<XROrigin>().Camera && One<ARSession>() && One<ARCameraManager>(),"XR / passthrough scene components missing");
        Require(One<EventSystem>() && One<XRUIInputModule>() && One<XRInteractionManager>(),"Interaction routing missing");
        Require(UnityEngine.Object.FindObjectsOfType<InputActionManager>(true).Any(m=>m.actionAssets!=null && m.actionAssets.Count>0),"Input actions are not enabled by a manager");
        Require(One<XROrigin>().Camera.GetComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>(),"Camera tracked pose driver missing");
        var guard=One<HandInteractionGuard>();
        foreach(var root in new[]{guard.leftRoot,guard.rightRoot}) {
            Require(root.GetComponentsInChildren<XRRayInteractor>(true).Length>0,"Hand ray missing");
            Require(root.GetComponentsInChildren<XRPokeInteractor>(true).Length>0,"Hand poke missing");
            Require(root.GetComponentsInChildren<XRDirectInteractor>(true).Length>0,"Hand direct grab missing");
        }
        Require(placement.grab.colliders.Count==4 && placement.grab.selectMode==InteractableSelectMode.Multiple && !placement.grab.throwOnDetach,"Screen manipulation incomplete");
        var transform=placement.grab.GetComponent<XRGeneralGrabTransformer>();
        Require(transform && transform.allowTwoHandedScaling && placement.grab.startingMultipleGrabTransformers.Contains(transform),"Two-hand transformer not serialized");
        Require(video.screenRenderer && ui.video==video && ui.placement==placement && ui.environment && ui.sources && ui.settings && ui.xrCamera,"Application reference missing");
        // Build the same UI used at runtime, invoke actions, and verify effects instead of matching source text.
        ui.BuildUI();
        try {
            Require(ui.Controls.Count>=35,"Incomplete control panel");
            foreach(var selectable in ui.Controls.Values)Require(selectable!=null,"Missing UI control");
            var oldScale=placement.screen.transform.localScale;float oldCurve=placement.screen.curvature;
            ((Slider)ui.Controls["Width"]).value=3;Require(Mathf.Abs(placement.screen.Width-3)<.001f,"Width slider is not connected");
            ((Slider)ui.Controls["Curvature"]).value=.6f;Require(Mathf.Abs(placement.screen.curvature-.6f)<.001f,"Curvature slider is not connected");
            ((Slider)ui.Controls["Glow"]).value=.5f;Require(Mathf.Abs(ui.environment.intensity-.5f)<.001f,"Glow slider is not connected");
            ((Slider)ui.Controls["Dim"]).value=.2f;Require(Mathf.Abs(ui.environment.dimming-.2f)<.001f,"Dimming slider is not connected");
            ((Button)ui.Controls["Lock"]).onClick.Invoke();Require(placement.Locked && !placement.grab.enabled,"Lock button is not connected");
            ((Button)ui.Controls["Lock"]).onClick.Invoke();Require(!placement.Locked && placement.grab.enabled,"Unlock failed");
            var position=placement.screen.transform.position;((Button)ui.Controls["MoveUp"]).onClick.Invoke();Require(Vector3.Distance(position,placement.screen.transform.position)>.09f,"Position button is not connected");
            placement.screen.transform.position=position;placement.screen.transform.localScale=oldScale;placement.SetCurvature(oldCurve);
            var mesh=placement.screen.GetComponent<MeshFilter>().sharedMesh;Require(mesh && mesh.vertexCount==(placement.screen.segments+1)*2,"Screen mesh not generated");
            Require(placement.screen.GetComponentInChildren<TrackedDeviceGraphicRaycaster>(true),"Spatial UI raycaster missing");
            placement.SetCurvature(0);var a=placement.screen.Point(0,-.5f);var b=placement.screen.Point(1,.5f);
            Require(Mathf.Abs((b.x-a.x)/(b.y-a.y)-16f/9f)<.0001f,"Screen aspect ratio changed");
            placement.SetCurvature(1);Require(placement.screen.Point(0,0).z<-.1f,"Curved surface is flat");
            placement.SetCurvature(oldCurve);
        } finally { ui.DestroyGeneratedUI(); }
        // Missing scripts in nested official prefab instances cause a hard failure.
        foreach(var root in SceneManager.GetActiveScene().GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true))
            Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0,"Missing script on "+t.name);
        Debug.Log("Spatial scene invariants and UI action tests passed");
    }
}
