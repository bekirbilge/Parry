import hashlib
import importlib.util
import json
import tempfile
import unittest
from pathlib import Path

from PIL import Image


SPEC = importlib.util.spec_from_file_location(
    "asset_validator", Path(__file__).resolve().parents[1] / "tools/assets/validate.py"
)
validator = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(validator)


def proof(tmp_path):
    assets = []
    for role, (path, resource) in validator.EXPECTED.items():
        png = tmp_path / path
        png.parent.mkdir(parents=True, exist_ok=True)
        image = Image.new("RGBA" if role == "character" else "RGB", (4, 4), (255, 20, 20))
        if role == "character":
            image.putpixel((0, 0), (0, 0, 0, 0))
        image.save(png)
        assets.append({
            "role": role, "path": path, "resources_path": resource,
            "width": 4, "height": 4, "pivot": [0.5, 0.5],
            "sha256": hashlib.sha256(png.read_bytes()).hexdigest(),
            "prompt": "reference proof", "tool": "image generator",
            "created_utc": "2026-10-20T12:00:00Z",
            "provenance": {"terms_url": "https://example.com/terms", "reviewed_date": "2026-10-20",
                           "reference": "art/concepts/reference.png", "human_direction": "C approved",
                           "production_approval": "pending"},
        })
        if role == "character":
            assets[-1]["visible_alpha_threshold"] = 32
            assets[-1]["visible_bounds"] = [0, 0, 4, 4]
        else:
            assets[-1]["ground_line_y_px"] = 4
    layout_path = tmp_path / validator.LAYOUT_PATH
    layout_path.write_text(json.dumps({"visibleHeightFraction": 1.0, "soleInsetFraction": 0.0,
                                       "arenaBottomCropFraction": 0.0}))
    manifest = {"version": 1, "usage": "proof", "approval_status": "proof_pending",
                "layout_path": validator.LAYOUT_PATH, "assets": assets}
    path = tmp_path / "manifest.json"
    path.write_text(json.dumps(manifest))
    return path, manifest


class AssetValidationTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)

    def test_valid_proof_and_production_gate(self):
        path, _ = proof(self.root)
        self.assertEqual(validator.validate_manifest(path, self.root), [])
        self.assertIn("Production requires approved", " ".join(
            validator.validate_manifest(path, self.root, require_production=True)
        ))

    def test_character_requires_real_transparency(self):
        path, manifest = proof(self.root)
        png = self.root / validator.EXPECTED["character"][0]
        Image.new("RGBA", (4, 4), (255, 20, 20, 255)).save(png)
        manifest["assets"][0]["sha256"] = hashlib.sha256(png.read_bytes()).hexdigest()
        path.write_text(json.dumps(manifest))
        self.assertIn("fully transparent", " ".join(validator.validate_manifest(path, self.root)))

    def test_canvas_and_checksum_mismatch(self):
        path, manifest = proof(self.root)
        manifest["assets"][0]["width"] = 5
        manifest["assets"][0]["sha256"] = "0" * 64
        path.write_text(json.dumps(manifest))
        errors = " ".join(validator.validate_manifest(path, self.root))
        self.assertIn("canvas dimensions mismatch", errors)
        self.assertIn("sha256 mismatch", errors)

    def test_path_traversal_and_unknown_status(self):
        path, manifest = proof(self.root)
        manifest["assets"][0]["path"] = "../../outside.png"
        manifest["approval_status"] = "production"
        path.write_text(json.dumps(manifest))
        errors = " ".join(validator.validate_manifest(path, self.root))
        self.assertIn("path/resources_path", errors)
        self.assertIn("Unknown approval_status", errors)

    def test_production_requires_each_asset_approval(self):
        path, manifest = proof(self.root)
        manifest["approval_status"] = "approved"
        path.write_text(json.dumps(manifest))
        errors = " ".join(validator.validate_manifest(path, self.root, require_production=True))
        self.assertIn("production requires approved asset provenance", errors)

    def test_character_bounds_and_layout_match_source(self):
        path, manifest = proof(self.root)
        manifest["assets"][0]["visible_bounds"] = [0, 0, 3, 4]
        path.write_text(json.dumps(manifest))
        self.assertIn("visible_bounds mismatch", " ".join(validator.validate_manifest(path, self.root)))
        manifest["assets"][0]["visible_bounds"] = [0, 0, 4, 4]
        path.write_text(json.dumps(manifest))
        layout_path = self.root / validator.LAYOUT_PATH
        layout_path.write_text(json.dumps({"visibleHeightFraction": 0.75, "soleInsetFraction": 0.25,
                                           "arenaBottomCropFraction": 0.0}))
        errors = " ".join(validator.validate_manifest(path, self.root))
        self.assertIn("Layout visibleHeightFraction mismatch", errors)
        self.assertIn("Layout soleInsetFraction mismatch", errors)

    def test_arena_crop_requires_matching_ground_line(self):
        path, manifest = proof(self.root)
        manifest["assets"][1].pop("ground_line_y_px")
        path.write_text(json.dumps(manifest))
        self.assertIn("invalid ground_line_y_px", " ".join(validator.validate_manifest(path, self.root)))
        manifest["assets"][1]["ground_line_y_px"] = 3
        path.write_text(json.dumps(manifest))
        self.assertIn("arenaBottomCropFraction mismatch", " ".join(validator.validate_manifest(path, self.root)))
        layout_path = self.root / validator.LAYOUT_PATH
        layout_path.write_text(json.dumps({"visibleHeightFraction": 1.0, "soleInsetFraction": 0.0,
                                           "arenaBottomCropFraction": 0.25}))
        self.assertEqual(validator.validate_manifest(path, self.root), [])
