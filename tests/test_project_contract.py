import importlib.util
from pathlib import Path
import shutil
import tempfile
import unittest

ROOT=Path(__file__).resolve().parent.parent
PROJECT=ROOT/'Base'
spec=importlib.util.spec_from_file_location('project_validator',ROOT/'tools/check_project.py')
checker=importlib.util.module_from_spec(spec);spec.loader.exec_module(checker)

@unittest.skipUnless((PROJECT/'Packages/manifest.json').exists(),'Run after project assembly')
class AssembledProjectGuards(unittest.TestCase):
 def setUp(self):
  self.temp=tempfile.TemporaryDirectory();self.project=Path(self.temp.name)
  for p in ['ProjectSettings/ProjectVersion.txt','ProjectSettings/GraphicsSettings.asset','Packages/manifest.json',checker.RIG,'Assets/StreamingAssets/demo.mp4','Assets/Editor/SpatialBuild.cs','Assets/XR/Settings/Open XR Package Settings.asset']:
   dest=self.project/p;dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(PROJECT/p,dest)
 def tearDown(self):self.temp.cleanup()
 def test_real_pinned_project_passes(self):self.assertEqual(checker.check(self.project),[])
 def test_disabled_hand_aim_is_rejected(self):
  p=self.project/'Assets/XR/Settings/Open XR Package Settings.asset';s=p.read_text();blocks=s.split('--- !u!')
  p.write_text('--- !u!'.join(b.replace('m_enabled: 1','m_enabled: 0') if 'm_Name: MetaHandTrackingAim Android' in b else b for b in blocks))
  self.assertTrue(any('MetaHandTrackingAim' in e for e in checker.check(self.project)))
 def test_incompatible_xri_package_is_rejected(self):
  p=self.project/'Packages/manifest.json';p.write_text(p.read_text().replace('"2.5.1"','"3.0.0"'))
  self.assertTrue(any('com.unity.xr.interaction.toolkit' in e for e in checker.check(self.project)))
 def test_missing_official_rig_is_rejected(self):
  (self.project/checker.RIG).unlink()
  self.assertTrue(any(checker.RIG in e for e in checker.check(self.project)))
 def test_wrong_pipeline_is_rejected(self):
  p=self.project/'ProjectSettings/GraphicsSettings.asset';s=p.read_text();import re
  p.write_text(re.sub(r'm_CustomRenderPipeline: .+', 'm_CustomRenderPipeline: {fileID: 0}',s))
  self.assertIn('URP not assigned',checker.check(self.project))

if __name__=='__main__':unittest.main()
