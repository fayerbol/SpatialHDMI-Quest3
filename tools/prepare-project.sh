#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
if [ -e Base ]; then echo 'Base already exists. Refusing to overwrite.' >&2; exit 1; fi
for tool in git git-lfs python3; do command -v "$tool" >/dev/null || { echo "Missing tool: $tool" >&2; exit 1; }; done
python3 tools/validate.py
git clone --branch v1.0.0-pre.1 --depth 1 https://github.com/Unity-Technologies/mr-example-meta-openxr.git Base
git -C Base lfs pull
revision="$(git -C Base rev-parse HEAD)"
case "$revision" in 1130bc5*) ;; *) echo "Unexpected upstream revision: $revision" >&2; exit 1;; esac
cp -R Overlay/Assets/. Base/Assets/
for path in Base/ProjectSettings/ProjectVersion.txt Base/Packages/manifest.json Base/Assets/SpatialHDMI/StreamingAssets/demo.mp4; do
  test -s "$path" || { echo "Missing or empty: $path" >&2; exit 1; }
done
printf '\nUpstream revision: %s\nUnity editor:\n' "$revision"
cat Base/ProjectSettings/ProjectVersion.txt
python3 - <<'PY'
import json
with open('Base/Packages/manifest.json',encoding='utf-8') as f: deps=json.load(f).get('dependencies',{})
required=['com.unity.xr.meta-openxr','com.unity.xr.openxr','com.unity.xr.arfoundation','com.unity.xr.interaction.toolkit']
for k in required:
    if k not in deps: raise SystemExit('Missing direct dependency: '+k)
    print(k+': '+deps[k])
print('Meta XR Core SDK:',deps.get('com.meta.xr.sdk.core','not a direct dependency of this base'))
PY
printf '\nAssembled project: Base/\n'
