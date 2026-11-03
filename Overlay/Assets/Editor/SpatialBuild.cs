using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Management;
using Unity.XR.CoreUtils;
using SpatialHDMI;

public static class SpatialBuild {
    const string ScenePath="Assets/SpatialHDMI/Scenes/SpatialHDMI_Main.unity";
    const string MaterialPath="Assets/SpatialHDMI/Resources/SpatialVideo.mat";
    [MenuItem("Spatial HDMI/Generate scene and build Android")]
    public static void Build() {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android) throw new Exception("Start Unity with -buildTarget Android");
        foreach(var file in new[]{"Assets/SpatialHDMI/StreamingAssets/demo.mp4","Assets/SpatialHDMI/Resources/Screen.shader","Packages/manifest.json"})
            if (!File.Exists(file)) throw new FileNotFoundException("Missing required file: "+file);
        var xr = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Android);
        if (xr == null || xr.Manager == null || xr.Manager.activeLoaders.Count == 0)
            throw new Exception("No Android XR loader configured in upstream project. Enable OpenXR before building.");
        bool openxr=false;
        foreach(var loader in xr.Manager.activeLoaders) if (loader && loader.GetType().FullName.Contains("OpenXRLoader")) openxr=true;
        if (!openxr) throw new Exception("Android XR loader is not OpenXR");
        // The base supplies the feature settings; package presence cannot prove passthrough is enabled.
        string manifest=File.ReadAllText("Packages/manifest.json");
        if (!manifest.Contains("com.unity.xr.meta-openxr")) throw new Exception("Meta OpenXR package missing from manifest");
        PlayerSettings.companyName="Spatial HDMI"; PlayerSettings.productName="Spatial HDMI";
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android,"com.spatialhdmi.demo");
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android,ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel29;
        EditorUserBuildSettings.buildAppBundle=false;
        MakeScene();
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
        Directory.CreateDirectory("build/Android");
        string output=Path.GetFullPath("build/Android/SpatialHDMI.apk");
        if(File.Exists(output)) File.Delete(output);
        var opts=new BuildPlayerOptions { scenes=new[]{ScenePath}, locationPathName=output, target=BuildTarget.Android, options=BuildOptions.None };
        BuildReport result=BuildPipeline.BuildPlayer(opts);
        if(result.summary.result != BuildResult.Succeeded || !File.Exists(output) || new FileInfo(output).Length == 0)
            throw new Exception("APK generation failed: "+result.summary.result);
        Debug.Log("Created APK: "+output);
    }
    static void MakeScene() {
        var shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/SpatialHDMI/Resources/Screen.shader");
        if (!shader) throw new Exception("Screen shader missing or failed to import");
        Directory.CreateDirectory("Assets/SpatialHDMI/Scenes");
        var material=AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (!material) { material=new Material(shader); AssetDatabase.CreateAsset(material,MaterialPath); }
        else { material.shader=shader; EditorUtility.SetDirty(material); }
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        new GameObject("AR Session").AddComponent<ARSession>();
        var origin=new GameObject("XR Origin"); var xrOrigin=origin.AddComponent<XROrigin>();
        var cameraObject=new GameObject("Main Camera"); cameraObject.tag="MainCamera"; cameraObject.transform.SetParent(origin.transform,false);
        var camera=cameraObject.AddComponent<Camera>(); camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(0,0,0,0); camera.nearClipPlane=.05f;
        cameraObject.AddComponent<AudioListener>(); cameraObject.AddComponent<ARCameraManager>(); cameraObject.AddComponent<HeadTracking>(); xrOrigin.Camera=camera;
        var screenObject=new GameObject("Spatial HDMI Screen"); screenObject.AddComponent<MeshFilter>();
        var renderer=screenObject.AddComponent<MeshRenderer>(); renderer.sharedMaterial=material;
        var screen=screenObject.AddComponent<SpatialScreen>();
        var playerObject=new GameObject("Demo Video"); playerObject.AddComponent<VideoPlayer>(); playerObject.AddComponent<AudioSource>();
        var demo=playerObject.AddComponent<DemoVideo>(); demo.screenRenderer=renderer;
        var controlsObject=new GameObject("Screen Controls"); var controls=controlsObject.AddComponent<ScreenController>(); controls.screen=screen; controls.head=camera.transform; controls.demo=demo;
        var hud=controlsObject.AddComponent<StatusPanel>(); hud.head=camera.transform; hud.screen=screen; hud.demo=demo;
        if (!EditorSceneManager.SaveScene(scene,ScenePath)) throw new Exception("Could not save scene");
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        Debug.Log("Generated and connected "+ScenePath+". XR passthrough feature must also be enabled in project settings.");
    }
}
