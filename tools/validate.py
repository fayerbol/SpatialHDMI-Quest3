#!/usr/bin/env python3
"""Offline structural checks; does not compile C# or validate Unity XR behavior."""
from pathlib import Path
import re, sys
root=Path(__file__).resolve().parent.parent
files=['Overlay/Assets/SpatialHDMI/Scripts/SpatialScreen.cs','Overlay/Assets/SpatialHDMI/Scripts/ScreenController.cs','Overlay/Assets/SpatialHDMI/Scripts/DemoVideo.cs','Overlay/Assets/SpatialHDMI/Scripts/HeadTracking.cs','Overlay/Assets/SpatialHDMI/Scripts/StatusPanel.cs','Overlay/Assets/SpatialHDMI/Resources/Screen.shader','Overlay/Assets/SpatialHDMI/StreamingAssets/demo.mp4','Overlay/Assets/Editor/SpatialBuild.cs','.github/workflows/android.yml','tools/prepare-project.sh']
for f in files:
    p=root/f
    if not p.is_file() or p.stat().st_size==0: sys.exit('Missing/empty: '+f)
editor=(root/'Overlay/Assets/Editor/SpatialBuild.cs').read_text()
workflow=(root/'.github/workflows/android.yml').read_text()
shader=(root/'Overlay/Assets/SpatialHDMI/Resources/Screen.shader').read_text()
for token in ['SpatialHDMI_Main.unity','AndroidArchitecture.ARM64','ScriptingImplementation.IL2CPP','new EditorBuildSettingsScene(ScenePath,true)','SpatialHDMI.apk','ARCameraManager','XRGeneralSettingsForBuildTarget','screen=screen']:
    if token not in editor: sys.exit('Missing build code marker: '+token)
for token in ['UNITY_VERTEX_OUTPUT_STEREO','UNITY_SETUP_INSTANCE_ID','UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO']:
    if token not in shader: sys.exit('Missing stereo shader marker: '+token)
for token in ['SpatialBuild.Build','test -s Base/build/Android/SpatialHDMI.apk','if: always()','collectUnityLogs: true']:
    if token not in workflow: sys.exit('Missing workflow marker: '+token)
if (root/'Overlay/Assets/SpatialHDMI/Scripts/AutoSetup.cs').exists(): sys.exit('Old runtime autoinstaller conflicts with generated scene')
print('PASS: files and static markers present (not a Unity or device test).')
