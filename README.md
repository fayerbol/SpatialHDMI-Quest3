# Spatial HDMI — Meta Quest 3 (experimental)

An experimental Unity XR Android application for Meta Quest 3. The current prototype renders a curved, movable virtual screen and plays a bundled demo video. **It does not yet capture an HDMI/UVC input**, and an APK has **not yet been successfully built or tested on Quest 3**.

## Source layout

- `Overlay/Assets/SpatialHDMI/Scripts/`: procedural screen, controller input, demo video, status HUD and head tracking.
- `Overlay/Assets/SpatialHDMI/Resources/Screen.shader`: stereo-aware video screen shader.
- `Overlay/Assets/SpatialHDMI/StreamingAssets/demo.mp4`: packaged test video.
- `Overlay/Assets/Editor/SpatialBuild.cs`: editor scene generator and Android build entry point (`SpatialBuild.Build`).
- `tools/validate.py`: static source checks (not a C# compile).
- `tools/prepare-project.sh`: assembles the overlay over a pinned official Unity XR sample.
- `.github/workflows/preflight.yml`: source/video/upstream preparation checks, without a Unity license.
- `.github/workflows/android.yml`: manually triggered Android APK build, currently blocked on CI licensing.

## Current functionality (unverified on device)

- Curved 16:9 screen, adjustable width, curvature, position and yaw.
- Saved screen placement via Unity PlayerPrefs.
- Bundled MP4 demo playback through Unity VideoPlayer.
- Controller mappings: left stick moves screen vertically/laterally; left grip + stick changes size/curve; right stick moves screen depth; right grip + stick rotates; A toggles playback; B recenters; X toggles the HUD.

## License-free checks

GitHub Actions → **Spatial HDMI preflight (no Unity license)** runs on pushes to `main` and can be started manually. It validates required files, checks the video with ffprobe, assembles the pinned Unity base and verifies generated paths. Passing this workflow does **not** prove Unity C# compilation or XR functionality.

For local static checks: `python3 tools/validate.py`. For assembly on a system with git-lfs: `bash tools/prepare-project.sh`. The assembly script refuses to overwrite an existing `Base/` directory.

## Android build

The **Spatial HDMI Android** workflow is manual only. It uses GameCI Unity Builder v4 and expects three repository Actions secrets: `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD`. Do **not** paste credentials or license content into issues, commits, chat, or workflow logs. The existing Unity Personal activation in Unity Hub on a separate Windows machine does not by itself provide a CI license file. Only use Unity's officially supported licensing procedures; do not fabricate or repurpose a license file.

Once a supported CI license is available, configure the secrets under **Settings → Secrets and variables → Actions**, run **Spatial HDMI Android** from Actions, and download the `SpatialHDMI-APK` artifact on success. Until then, the workflow deliberately fails early with a licensing message. No paid service is required by this repository, but GitHub-hosted runner quotas and Unity licensing terms still apply.

## Known limitations

1. **No confirmed APK or Quest test yet.** Android compilation and passthrough behavior remain unverified.
2. The project currently plays a demo file; USB capture/UVC, USB-C PD passthrough, and HDMI Link integration are **not implemented**.
3. Passthrough/OpenXR feature configuration is inherited from the upstream project and must be confirmed in an actual Unity build and on the device.
4. Real-time room mesh/geometry and ambient lighting are **not implemented**.
5. Input mappings depend on the XR controller runtime and need hardware validation.

## Upstream

`tools/prepare-project.sh` uses [Unity-Technologies/mr-example-meta-openxr](https://github.com/Unity-Technologies/mr-example-meta-openxr) at tag `v1.0.0-pre.1`, with a revision guard. See the script for exact pin and package checks.
