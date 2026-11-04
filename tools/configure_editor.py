#!/usr/bin/env python3
"""Align generated project metadata with the existing cloud editor, without editing Cloud."""
from pathlib import Path
import json
import re
import sys

POLICY_PATH = 'Assets/Editor/SpatialBuildCompatibility.json'

def read_policy(project):
    policy=json.loads((Path(project)/POLICY_PATH).read_text())
    supported=policy.get('supportedEditors',[])
    if not supported or not all(re.fullmatch(r'2022\.3\.\d+f\d+',v) for v in supported):
        raise ValueError('Expected an explicit list of reviewed Unity 2022.3 LTS releases')
    if policy.get('sourceEditor') not in supported or policy.get('preferredEditor') not in supported:
        raise ValueError('Source and preferred editors must be supported')
    if not re.fullmatch(r'[0-9a-f]{12}',policy.get('preferredRevision','')):
        raise ValueError('Invalid preferred Unity changeset')
    return policy

def configure(project):
    project=Path(project);policy=read_policy(project)
    path=project/'ProjectSettings/ProjectVersion.txt'
    match=re.search(r'^m_EditorVersion: (\S+)$',path.read_text(),re.MULTILINE)
    if not match or match[1] not in policy['supportedEditors']:
        raise ValueError('Unexpected source editor metadata; refusing to rewrite '+str(path))
    version=policy['preferredEditor'];revision=policy['preferredRevision']
    path.write_text(f'm_EditorVersion: {version}\nm_EditorVersionWithRevision: {version} ({revision})\n')
    return version

if __name__=='__main__':
    print('Prepared project for Unity '+configure(sys.argv[1] if len(sys.argv)>1 else 'Base')+' (existing cloud target unchanged)')
