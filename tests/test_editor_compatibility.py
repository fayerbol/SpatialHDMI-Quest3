import importlib.util
import json
from pathlib import Path
import tempfile
import unittest

ROOT=Path(__file__).resolve().parent.parent
spec=importlib.util.spec_from_file_location('editor_configuration',ROOT/'tools/configure_editor.py')
editor=importlib.util.module_from_spec(spec);spec.loader.exec_module(editor)
POLICY=json.loads((ROOT/'Overlay'/editor.POLICY_PATH).read_text())

class EditorCompatibilityRegression(unittest.TestCase):
 def setUp(self):
  self.temp=tempfile.TemporaryDirectory();self.project=Path(self.temp.name)
  self.policy=self.project/editor.POLICY_PATH;self.policy.parent.mkdir(parents=True)
  self.policy.write_text(json.dumps(POLICY))
  self.version=self.project/'ProjectSettings/ProjectVersion.txt';self.version.parent.mkdir()
  self.version.write_text('m_EditorVersion: 2022.3.8f1\nm_EditorVersionWithRevision: 2022.3.8f1 (b5eafc012955)\n')
 def tearDown(self):self.temp.cleanup()
 def test_build8_cloud_editor_is_explicitly_supported(self):
  self.assertIn('2022.3.62f3',editor.read_policy(self.project)['supportedEditors'])
 def test_upstream_is_prepared_for_existing_cloud_editor(self):
  self.assertEqual(editor.configure(self.project),'2022.3.62f3')
  self.assertEqual(self.version.read_text(),'m_EditorVersion: 2022.3.62f3\nm_EditorVersionWithRevision: 2022.3.62f3 (96770f904ca7)\n')
 def test_preparation_is_idempotent(self):
  editor.configure(self.project);expected=self.version.read_bytes();editor.configure(self.project)
  self.assertEqual(self.version.read_bytes(),expected)
 def test_other_or_unreviewed_editors_are_not_silently_allowed(self):
  for version in ['2021.3.62f1','2022.2.8f1','2022.3.7f1','2022.3.62f2','2022.3.63f1','6000.0.1f1','2022.3.62b1']:
   with self.subTest(version=version):
    content=f'm_EditorVersion: {version}\n';self.version.write_text(content)
    with self.assertRaises(ValueError):editor.configure(self.project)
    self.assertEqual(self.version.read_text(),content)
 def test_package_manifest_and_lock_are_not_changed(self):
  packages=self.project/'Packages';packages.mkdir()
  for name in ['manifest.json','packages-lock.json']:(packages/name).write_bytes(b'{"dependencies":{"com.unity.xr.openxr":"1.8.2"}}\n')
  before={p.name:p.read_bytes() for p in packages.iterdir()};editor.configure(self.project)
  self.assertEqual(before,{p.name:p.read_bytes() for p in packages.iterdir()})
 def test_missing_policy_does_not_rewrite_metadata(self):
  self.policy.unlink();expected=self.version.read_bytes()
  with self.assertRaises(FileNotFoundError):editor.configure(self.project)
  self.assertEqual(self.version.read_bytes(),expected)
 def test_wrong_changeset_is_rejected(self):
  policy=dict(POLICY,preferredRevision='unknown');self.policy.write_text(json.dumps(policy))
  with self.assertRaises(ValueError):editor.configure(self.project)
 def test_policy_cannot_silently_allow_another_editor_family(self):
  policy=dict(POLICY,supportedEditors=POLICY['supportedEditors']+['6000.0.1f1']);self.policy.write_text(json.dumps(policy))
  with self.assertRaises(ValueError):editor.configure(self.project)
 def test_preferred_editor_must_be_in_allowlist(self):
  policy=dict(POLICY,preferredEditor='2022.3.63f1');self.policy.write_text(json.dumps(policy))
  with self.assertRaises(ValueError):editor.configure(self.project)

if __name__=='__main__':unittest.main()
