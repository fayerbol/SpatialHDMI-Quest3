#!/usr/bin/env bash
set -euo pipefail

# Unity Build Automation invokes the pre-build hook from the configured
# project subdirectory ("Base"), which already exists as its workspace.
project_dir="$(pwd -P)"
script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd -P)"
repo_root="$(cd "$script_dir/.." && pwd -P)"
expected_project_dir="$(cd "$repo_root/Base" && pwd -P)"

if [ "$project_dir" != "$expected_project_dir" ]; then
  echo "Expected Unity Build Automation to run this hook from $expected_project_dir; got $project_dir" >&2
  exit 1
fi

for tool in git git-lfs python3; do
  command -v "$tool" >/dev/null || { echo "Missing tool: $tool" >&2; exit 1; }
done

cd "$repo_root"
python3 tools/validate.py

tmp="$(mktemp -d "$repo_root/.uba-upstream.XXXXXX")"
trap 'rm -rf "$tmp"' EXIT

git clone --branch v1.0.0-pre.1 --depth 1 https://github.com/Unity-Technologies/mr-example-meta-openxr.git "$tmp/upstream"
git -C "$tmp/upstream" lfs pull
revision="$(git -C "$tmp/upstream" rev-parse HEAD)"
case "$revision" in
  1130bc5*) ;;
  *) echo "Unexpected upstream revision: $revision" >&2; exit 1 ;;
esac

for path in ProjectSettings/ProjectVersion.txt Packages/manifest.json Assets; do
  test -e "$tmp/upstream/$path" || { echo "Missing upstream path: $path" >&2; exit 1; }
done

mkdir -p "$project_dir/Assets" "$project_dir/Packages" "$project_dir/ProjectSettings"
cp -R "$tmp/upstream/Assets/." "$project_dir/Assets/"
cp -R "$tmp/upstream/Packages/." "$project_dir/Packages/"
cp -R "$tmp/upstream/ProjectSettings/." "$project_dir/ProjectSettings/"
cp -R "$repo_root/Overlay/Assets/." "$project_dir/Assets/"

for path in ProjectSettings/ProjectVersion.txt Packages/manifest.json Assets/SpatialHDMI/StreamingAssets/demo.mp4; do
  test -s "$project_dir/$path" || { echo "Missing or empty after assembly: $path" >&2; exit 1; }
done

printf '\nUpstream revision: %s\nUnity editor:\n' "$revision"
cat "$project_dir/ProjectSettings/ProjectVersion.txt"
python3 - <<'PY'
import json
from pathlib import Path
deps=json.loads(Path("Base/Packages/manifest.json").read_text(encoding="utf-8")).get("dependencies", {})
required=["com.unity.xr.meta-openxr","com.unity.xr.openxr","com.unity.xr.arfoundation","com.unity.xr.interaction.toolkit"]
for key in required:
    if key not in deps:
        raise SystemExit("Missing direct dependency: "+key)
    print(key+": "+deps[key])
print("Meta XR Core SDK:",deps.get("com.meta.xr.sdk.core","not a direct dependency of this base"))
PY

printf '\nPrepared Unity project in existing UBA project directory: %s\n' "$project_dir"
