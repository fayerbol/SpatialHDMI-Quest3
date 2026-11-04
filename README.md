# Spatial HDMI — Meta Quest 3

Spatial video player for mixed reality, with a bundled MP4, a movable 16:9 flat/curved screen, hand/controller interaction and a spatial control panel.

**Version 0.2.0 is a source repair candidate, not a device-validated release.** The previous build #7 was installed by the owner, who reported a pink screen, failed video playback and no hand interaction. This branch replaces that incomplete implementation. No new APK has been produced for these changes yet.

## What changed

- Corrected the packaged video location to **`Assets/StreamingAssets/demo.mp4`**. A nested `Assets/SpatialHDMI/StreamingAssets` directory is not Unity's application StreamingAssets directory.
- Replaced legacy rendering with explicit URP/stereo shaders; added shader error checks before export.
- Reused the **official Unity XRI Hands Interaction Demo rig** already included in the pinned base: both hands, ray pinch, poke, direct grab, hand visuals and Touch controllers.
- Added four explicit grab handles, one/two-hand movement and rotation, two-hand scaling, locking, tracking-loss release and a release-pinch latch on recovery.
- Added world-space playback, screen, atmosphere and sources panels, plus a small quick-access/recenter panel.
- Added prepare/first-frame timeouts, play/pause, seek, volume, aspect-preserving display, an orientation switch and a local Android document picker.
- Added smoothed virtual ambient glow sampled from the video, compositor-alpha darkening, preferences and a local device diagnostics snapshot.
- Added source/configuration regression checks and Unity editor scene/UI invariants. Those editor checks have **not** been run in this environment.

## Evidence and documentation

| Document | Contents |
|---|---|
| [Diagnosis](docs/DIAGNOSTICO.md) | Findings from the previous source, certainty and remaining pink-screen diagnosis |
| [Research](docs/INVESTIGACION.md) | Accessible references, inspected SDK versions and limits of video inspection |
| [Architecture](docs/ARQUITECTURA.md) | Responsibilities, interaction and rendering choices |
| [Validation and installation](docs/VALIDACION.md) | Actual checks, Unity/UBA procedure and Quest acceptance checklist |
| [USB HDMI plan](docs/HDMI_USB.md) | UVC/UAC, native/JNI boundary, permissions, bandwidth, latency and hardware gates |

## Pinned base

[Unity-Technologies/mr-example-meta-openxr](https://github.com/Unity-Technologies/mr-example-meta-openxr) tag `v1.0.0-pre.1`, commit `1130bc5ff206eeef7c0e7d6c5617afb2b36dab4b`.

Unity **2022.3.8f1**, URP **14.0.8**, OpenXR **1.8.2**, Unity OpenXR Meta **0.2.1**, XR Hands **1.3.0**, XRI **2.5.1**, AR Foundation **5.1.0-pre.10**, Input System **1.7.0**. The Meta XR Core/Interaction SDK packages are **not installed**; the existing compatible Unity integration is used instead of importing a current SDK that targets a different editor.

## Reproduce available checks

```bash
python3 tools/validate.py
python3 -m pip install -r tools/requirements-syntax.txt
python3 tools/check_syntax.py
python3 -m unittest discover -s tests -v
bash tools/prepare-project.sh
python3 tools/check_project.py Base
python3 -m unittest discover -s tests -p test_project_contract.py -v
ffmpeg -v error -xerror -i Overlay/Assets/StreamingAssets/demo.mp4 -f null -
```

Assembly refuses to overwrite an existing `Base/`. UBA uses the separate idempotent `tools/prepare-project-uba.sh` hook. Neither Python validation nor syntax parsing compiles Unity C#, Java or shaders.

With the pinned Unity editor and Android modules:

```bash
/path/to/Unity -batchmode -nographics -quit -projectPath "$PWD/Base" -buildTarget Android -executeMethod SpatialBuild.Validate -logFile "$PWD/unity-validate.log"
/path/to/Unity -batchmode -nographics -quit -projectPath "$PWD/Base" -buildTarget Android -executeMethod SpatialBuild.Build -logFile "$PWD/unity-build.log"
```

Run the second command only after validation succeeds. `SpatialBuild.Build` repeats the editor invariants before building. Output: `Base/build/Android/SpatialHDMI.apk`.

The existing GameCI workflow remains manual and requires its existing supported Unity licensing configuration. No credentials, license files or paid services were added or changed. The existing Unity Build Automation target can reuse its hook and `SpatialBuild.PrepareScene` pre-export method; see the validation document.

## Limitations

- No new Android compilation or real Quest validation for 0.2.0 yet. A successful preflight is not proof of a functional app.
- The exact runtime cause of build #7's pink image still needs its Unity/Android shader logs; source risks were removed, not declared proven on hardware.
- USB HDMI capture, UAC audio, negotiated 1080p60/4K60 and spatial anchors are **not implemented**.
- Ambient light is virtual color glow around the display. It does not relight physical surfaces or reconstruct a room mesh.
- Android document-picker availability, decoder compatibility, hand comfort, passthrough alpha and frame timing must be checked on Quest. No DRM-service compatibility is claimed.
