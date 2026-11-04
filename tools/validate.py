#!/usr/bin/env python3
"""Offline source contract checks. Unity editor invariants live in SpatialBuild.Validate."""
from pathlib import Path
import hashlib
import re
import sys
import xml.etree.ElementTree as ET

ROOT=Path(__file__).resolve().parent.parent
REQUIRED=[
 'Overlay/Assets/StreamingAssets/demo.mp4',
 'Overlay/Assets/SpatialHDMI/Resources/Screen.shader',
 'Overlay/Assets/SpatialHDMI/Resources/SpatialTint.shader',
 'Overlay/Assets/Editor/SpatialBuild.cs',
 'Overlay/Assets/Editor/SpatialBuildCompatibility.json',
 *['Overlay/Assets/SpatialHDMI/Scripts/'+name+'.cs' for name in ['DemoVideo','SpatialScreen','ScreenController','SpatialUI','HandInteractionGuard','EnvironmentEffects','VideoSources','SettingsStore','SliderGesture','RuntimeDiagnostics']],
 'Overlay/Assets/Plugins/Android/SpatialMedia.androidlib/AndroidManifest.xml',
 'Overlay/Assets/Plugins/Android/SpatialMedia.androidlib/src/com/spatialhdmi/bridge/MediaPickerActivity.java',
 'tools/check_project.py','tools/configure_editor.py','tools/prepare-project.sh','tools/prepare-project-uba.sh',
 '.github/workflows/preflight.yml','.github/workflows/android.yml',
]

def validate(root=ROOT):
 root=Path(root);errors=[]
 for name in REQUIRED:
  p=root/name
  if not p.is_file() or p.stat().st_size==0:errors.append('Missing/empty: '+name)
 if errors:return errors
 for p in (root/'Overlay/Assets').rglob('StreamingAssets'):
  if p.relative_to(root/'Overlay/Assets').as_posix()!='StreamingAssets':errors.append('StreamingAssets must be directly under Assets: '+str(p))
 video=root/'Overlay/Assets/StreamingAssets/demo.mp4'
 if hashlib.sha256(video.read_bytes()).hexdigest()!='95264838c2d93bb1230f80f9ffff1cf93bb7b55b71e9f83fda514387171c2089':errors.append('Demo video hash mismatch')
 for p in (root/'Overlay/Assets/SpatialHDMI/Resources').glob('*.shader'):
  s=p.read_text()
  if not re.search(r'"RenderPipeline"\s*=\s*"UniversalPipeline"',s):errors.append('URP shader tag missing: '+p.name)
  if 'UnityCG.cginc' in s or 'CGPROGRAM' in s:errors.append('Legacy shader path: '+p.name)
  for macro in ['UNITY_VERTEX_OUTPUT_STEREO','UNITY_SETUP_INSTANCE_ID','UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO','UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX']:
   if macro not in s:errors.append('Stereo shader contract missing: '+p.name+' '+macro)
 manifest=root/'Overlay/Assets/Plugins/Android/SpatialMedia.androidlib/AndroidManifest.xml'
 try:
  doc=ET.parse(manifest); ns='{http://schemas.android.com/apk/res/android}'
  activity=doc.find('./application/activity')
  if activity is None or activity.get(ns+'exported')!='false':errors.append('File picker activity must not be exported')
  if doc.findall('uses-permission'):errors.append('Demo/local picker must not request broad device permissions')
 except ET.ParseError as error:errors.append('Invalid Android library manifest: '+str(error))
 for old in ['AutoSetup','HeadTracking','StatusPanel','RoomLight']:
  if (root/('Overlay/Assets/SpatialHDMI/Scripts/'+old+'.cs')).exists():errors.append('Obsolete or conflicting component: '+old)
 return errors

if __name__=='__main__':
 errors=validate()
 if errors:raise SystemExit('\n'.join(errors))
 print('PASS: source/media/shader/Android contracts (not C# compilation or a Quest test)')
