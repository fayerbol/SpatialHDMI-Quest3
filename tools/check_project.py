#!/usr/bin/env python3
"""Check the assembled, pinned project without pretending to run Unity."""
from pathlib import Path
import json
import re
import sys

VERSIONS = {
    'com.unity.render-pipelines.universal':'14.0.8',
    'com.unity.xr.openxr':'1.8.2',
    'com.unity.xr.meta-openxr':'0.2.1',
    'com.unity.xr.hands':'1.3.0',
    'com.unity.xr.interaction.toolkit':'2.5.1',
    'com.unity.xr.arfoundation':'5.1.0-pre.10',
    'com.unity.inputsystem':'1.7.0',
}
RIG = 'Assets/Samples/XR Interaction Toolkit/2.5.1/Hands Interaction Demo/Prefabs/XR Interaction Hands Setup.prefab'

def check(project):
    project=Path(project);errors=[]
    for path in ['ProjectSettings/ProjectVersion.txt','Packages/manifest.json',RIG,'Assets/StreamingAssets/demo.mp4','Assets/Editor/SpatialBuild.cs','Assets/XR/Settings/Open XR Package Settings.asset']:
        if not (project/path).is_file():errors.append('Missing assembled project path: '+path)
    if errors:return errors
    if 'm_EditorVersion: 2022.3.8f1\n' not in (project/'ProjectSettings/ProjectVersion.txt').read_text():errors.append('Wrong Unity editor version')
    deps=json.loads((project/'Packages/manifest.json').read_text())['dependencies']
    for name,version in VERSIONS.items():
        if deps.get(name)!=version:errors.append('Incompatible pinned dependency: '+name)
    features=(project/'Assets/XR/Settings/Open XR Package Settings.asset').read_text()
    for name in ['HandTracking','MetaHandTrackingAim','ARCameraFeature','ARSessionFeature','OculusTouchControllerProfile']:
        if not any(re.search(r'  m_Name: '+name+r' Android\s*\n',block) and re.search(r'  m_enabled: 1\s*\n',block) for block in features.split('--- !u!')):
            errors.append('Required Android OpenXR feature disabled or missing: '+name)
    if (project/'Assets/StreamingAssets/demo.mp4').stat().st_size<1000000:errors.append('Packaged demo is empty or an LFS pointer')
    if (project/'Assets/SpatialHDMI/StreamingAssets').exists():errors.append('Invalid nested StreamingAssets remains')
    graphics=(project/'ProjectSettings/GraphicsSettings.asset').read_text()
    if re.search(r'm_CustomRenderPipeline: \{fileID: 0\}',graphics):errors.append('URP not assigned')
    return errors

if __name__=='__main__':
    errors=check(sys.argv[1] if len(sys.argv)>1 else 'Base')
    if errors:raise SystemExit('\n'.join(errors))
    print('PASS: assembled Unity version, package pins, Android features, rig and packaged media (not a Unity compile)')
