import importlib.util
from pathlib import Path
import shutil
import tempfile
import unittest

ROOT=Path(__file__).resolve().parent.parent
spec=importlib.util.spec_from_file_location('source_validator',ROOT/'tools/validate.py')
validator=importlib.util.module_from_spec(spec);spec.loader.exec_module(validator)

class RegressionGuards(unittest.TestCase):
 def setUp(self):
  self.temp=tempfile.TemporaryDirectory();self.root=Path(self.temp.name)
  for item in ['Overlay','tools','.github']:shutil.copytree(ROOT/item,self.root/item)
 def tearDown(self):self.temp.cleanup()
 def test_current_source_passes(self):self.assertEqual(validator.validate(self.root),[])
 def test_nested_streaming_assets_are_rejected(self):
  (self.root/'Overlay/Assets/SpatialHDMI/StreamingAssets').mkdir()
  self.assertTrue(any('directly under Assets' in e for e in validator.validate(self.root)))
 def test_missing_media_is_rejected(self):
  (self.root/'Overlay/Assets/StreamingAssets/demo.mp4').unlink()
  self.assertTrue(any('demo.mp4' in e for e in validator.validate(self.root)))
 def test_lfs_pointer_or_replaced_media_is_rejected(self):
  (self.root/'Overlay/Assets/StreamingAssets/demo.mp4').write_text('version https://git-lfs.github.com/spec/v1\n')
  self.assertIn('Demo video hash mismatch',validator.validate(self.root))
 def test_builtin_pipeline_shader_is_rejected(self):
  p=self.root/'Overlay/Assets/SpatialHDMI/Resources/Screen.shader';p.write_text(p.read_text().replace('UniversalPipeline','WrongPipeline'))
  self.assertTrue(any('URP shader tag missing' in e for e in validator.validate(self.root)))
 def test_missing_stereo_path_is_rejected(self):
  p=self.root/'Overlay/Assets/SpatialHDMI/Resources/Screen.shader';p.write_text(p.read_text().replace('UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);',''))
  self.assertTrue(any('Stereo shader contract missing' in e for e in validator.validate(self.root)))
 def test_exported_picker_is_rejected(self):
  p=self.root/'Overlay/Assets/Plugins/Android/SpatialMedia.androidlib/AndroidManifest.xml';p.write_text(p.read_text().replace('exported="false"','exported="true"'))
  self.assertTrue(any('must not be exported' in e for e in validator.validate(self.root)))
 def test_old_runtime_installer_is_rejected(self):
  (self.root/'Overlay/Assets/SpatialHDMI/Scripts/AutoSetup.cs').write_text('// stale installer')
  self.assertTrue(any('conflicting component' in e for e in validator.validate(self.root)))

if __name__=='__main__':unittest.main()
