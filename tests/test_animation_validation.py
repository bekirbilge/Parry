import hashlib
import importlib.util
import json
import shutil
import tempfile
import unittest
from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
SPEC = importlib.util.spec_from_file_location(
    "animation_validator", ROOT / "tools/assets/validate_animation.py"
)
validator = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(validator)


class AnimationValidationTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        self.manifest_path = self.root / validator.MANIFEST_PATH
        self.manifest_path.parent.mkdir(parents=True)
        self.manifest = json.loads((ROOT / validator.MANIFEST_PATH).read_text())
        for field in ("reference", "prompt_path", "source_path", "texture_path", "layout_path"):
            source = ROOT / self.manifest[field]
            target = self.root / self.manifest[field]
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(source, target)
        self.layout_path = self.root / validator.LAYOUT_PATH
        self.layout = json.loads(self.layout_path.read_text())
        self.texture_path = self.root / validator.TEXTURE_PATH
        self._save()

    def _save(self):
        self.layout_path.write_text(json.dumps(self.layout))
        self.manifest["layout_sha256"] = hashlib.sha256(self.layout_path.read_bytes()).hexdigest()
        self.manifest["sha256"] = hashlib.sha256(self.texture_path.read_bytes()).hexdigest()
        self.manifest_path.write_text(json.dumps(self.manifest))

    def _errors(self, production=False):
        return " | ".join(validator.validate_manifest(self.manifest_path, self.root, production))

    def test_actual_proof_passes_and_production_is_gated(self):
        self.assertEqual(validator.validate_manifest(ROOT / validator.MANIFEST_PATH, ROOT), [])
        self.assertEqual(self._errors(), "")
        self.assertIn("Production requires approved", self._errors(production=True))

    def test_detects_texture_and_layout_checksum_mismatch(self):
        self.manifest["sha256"] = "0" * 64
        self.manifest["layout_sha256"] = "0" * 64
        self.manifest_path.write_text(json.dumps(self.manifest))
        errors = self._errors()
        self.assertIn("texture sha256 mismatch", errors)
        self.assertIn("layout sha256 mismatch", errors)

    def test_rejects_out_of_bounds_and_overlapping_frames(self):
        self.layout["frames"][0]["left"] = -1
        self.layout["frames"][1] = dict(self.layout["frames"][2])
        self._save()
        errors = self._errors()
        self.assertIn("rectangle out of bounds", errors)
        self.assertIn("overlaps", errors)

    def test_rejects_empty_frame(self):
        self.layout["frames"][4] = {"left": 0, "top": 0, "right": 2, "bottom": 2}
        self._save()
        errors = self._errors()
        self.assertIn("no visible alpha", errors)

    def test_rejects_unbounded_reference_height_and_missing_provenance(self):
        self.layout["referenceHeight"] = 999
        self.manifest["reference_height"] = 999
        self.manifest.pop("tool")
        self._save()
        errors = self._errors()
        self.assertIn("Invalid reference_height", errors)
        self.assertIn("Missing provenance tool", errors)

    def test_rejects_duplicate_frame_pixels_even_with_valid_hash(self):
        first = self.layout["frames"][0]
        fourth = self.layout["frames"][3]
        with Image.open(self.texture_path) as source:
            image = source.copy()
        image.paste(image.crop((first["left"], first["top"], first["right"], first["bottom"])),
                    (fourth["left"], fourth["top"]))
        image.save(self.texture_path)
        self._save()
        self.assertIn("duplicate of frame 0", self._errors())

    def test_rejects_path_escape_and_missing_padding(self):
        self.manifest["source_path"] = "../../outside.png"
        first = self.layout["frames"][0]
        with Image.open(self.texture_path) as source:
            image = source.copy()
        image.putpixel((first["left"], first["top"]), (255, 255, 255, 255))
        image.save(self.texture_path)
        self._save()
        errors = self._errors()
        self.assertIn("path escapes project root", errors)
        self.assertIn("transparent padding/gutter missing", errors)


if __name__ == "__main__":
    unittest.main()
