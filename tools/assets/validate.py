#!/usr/bin/env python3
"""Validate the two P3 reference PNGs and their provenance manifest."""

import argparse
import hashlib
import json
import math
from pathlib import Path

from PIL import Image


EXPECTED = {
    "character": ("Assets/Game/Resources/Art/char_ninja_idle_v01.png", "Art/char_ninja_idle_v01"),
    "arena": ("Assets/Game/Resources/Art/arena_rookie_dojo_background_v01.png", "Art/arena_rookie_dojo_background_v01"),
}
STATUSES = {"proof_pending", "approved", "rejected"}
LAYOUT_PATH = "Assets/Game/Resources/Art/reference_art_layout_v01.json"


def finite_fraction(value):
    return type(value) in (int, float) and math.isfinite(value) and 0 <= value <= 1


def validate_manifest(manifest_path: Path, root: Path, require_production: bool = False) -> list[str]:
    errors = []
    root = root.resolve()
    try:
        manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    except (OSError, UnicodeError, json.JSONDecodeError) as exc:
        return [f"Cannot read manifest: {exc}"]
    if not isinstance(manifest, dict):
        return ["Manifest must be an object"]
    if manifest.get("version") != 1:
        errors.append("Manifest version must be 1")
    if manifest.get("usage") != "proof":
        errors.append("Manifest usage must be proof")
    status = manifest.get("approval_status")
    if status not in STATUSES:
        errors.append("Unknown approval_status")
    if require_production and status != "approved":
        errors.append("Production requires approved approval_status")
    if manifest.get("layout_path") != LAYOUT_PATH:
        errors.append("Manifest layout_path does not match reference layout")
        layout = None
    else:
        layout_path = (root / LAYOUT_PATH).resolve()
        if not layout_path.is_relative_to(root):
            errors.append("Layout path escapes project root")
            layout = None
        else:
            try:
                layout = json.loads(layout_path.read_text(encoding="utf-8"))
            except (OSError, UnicodeError, json.JSONDecodeError) as exc:
                errors.append(f"Cannot read layout: {exc}")
                layout = None
        if not isinstance(layout, dict) or any(
            not finite_fraction(layout.get(field))
            for field in ("visibleHeightFraction", "soleInsetFraction", "arenaBottomCropFraction")
        ) or (isinstance(layout, dict) and finite_fraction(layout.get("arenaBottomCropFraction"))
              and layout["arenaBottomCropFraction"] > 0.25):
            errors.append("Layout requires finite fraction values")
            layout = None
    assets = manifest.get("assets")
    if not isinstance(assets, list):
        return errors + ["Manifest assets must be a list"]
    seen = set()
    for index, entry in enumerate(assets):
        label = f"assets[{index}]"
        if not isinstance(entry, dict):
            errors.append(f"{label} must be an object")
            continue
        role = entry.get("role")
        if role not in EXPECTED:
            errors.append(f"{label}: unknown role")
            continue
        if role in seen:
            errors.append(f"{label}: duplicate role {role}")
        seen.add(role)
        expected_path, expected_resource = EXPECTED[role]
        relative = entry.get("path")
        if relative != expected_path or entry.get("resources_path") != expected_resource:
            errors.append(f"{label}: path/resources_path do not match {role} proof")
            continue
        image_path = (root / relative).resolve()
        if not image_path.is_relative_to(root):
            errors.append(f"{label}: path escapes project root")
            continue
        for field in ("prompt", "tool", "created_utc"):
            if not isinstance(entry.get(field), str) or not entry[field].strip():
                errors.append(f"{label}: missing {field}")
        provenance = entry.get("provenance")
        if not isinstance(provenance, dict) or any(
            not isinstance(provenance.get(field), str) or not provenance[field].strip()
            for field in ("terms_url", "reviewed_date", "reference", "human_direction", "production_approval")
        ):
            errors.append(f"{label}: incomplete provenance")
        if require_production and (
            not isinstance(provenance, dict) or provenance.get("production_approval") != "approved"
        ):
            errors.append(f"{label}: production requires approved asset provenance")
        if not isinstance(entry.get("pivot"), list) or entry["pivot"] != [0.5, 0.5]:
            errors.append(f"{label}: pivot must be [0.5, 0.5]")
        width, height = entry.get("width"), entry.get("height")
        if not isinstance(width, int) or not isinstance(height, int) or width <= 0 or height <= 0:
            errors.append(f"{label}: invalid canvas dimensions")
        if not image_path.is_file():
            errors.append(f"{label}: missing PNG {relative}")
            continue
        checksum = hashlib.sha256(image_path.read_bytes()).hexdigest()
        if entry.get("sha256") != checksum:
            errors.append(f"{label}: sha256 mismatch")
        try:
            with Image.open(image_path) as image:
                allowed_modes = ("RGBA",) if role == "character" else ("RGB", "RGBA")
                if image.format != "PNG" or image.mode not in allowed_modes:
                    errors.append(f"{label}: expected {'RGBA' if role == 'character' else 'RGB or RGBA'} PNG")
                if image.size != (width, height):
                    errors.append(f"{label}: canvas dimensions mismatch")
                if max(image.size) > 2048:
                    errors.append(f"{label}: exceeds Unity importer max size 2048")
                if role == "character" and image.mode == "RGBA":
                    minimum, maximum = image.getchannel("A").getextrema()
                    if minimum != 0 or maximum != 255:
                        errors.append(f"{label}: character needs visible and fully transparent pixels")
                    if entry.get("visible_alpha_threshold") != 32:
                        errors.append(f"{label}: visible_alpha_threshold must be 32")
                    alpha = image.getchannel("A")
                    bounds = alpha.point(lambda value: 255 if value > 32 else 0).getbbox()
                    if bounds is None:
                        errors.append(f"{label}: no visible character pixels above threshold")
                    else:
                        if entry.get("visible_bounds") != list(bounds):
                            errors.append(f"{label}: visible_bounds mismatch")
                        if layout is not None:
                            visible_height = (bounds[3] - bounds[1]) / image.height
                            sole_inset = (image.height - bounds[3]) / image.height
                            if abs(layout["visibleHeightFraction"] - visible_height) > 1e-6:
                                errors.append("Layout visibleHeightFraction mismatch")
                            if abs(layout["soleInsetFraction"] - sole_inset) > 1e-6:
                                errors.append("Layout soleInsetFraction mismatch")
                if role == "arena":
                    ground_line = entry.get("ground_line_y_px")
                    if type(ground_line) is not int or not 0 <= ground_line <= image.height:
                        errors.append(f"{label}: invalid ground_line_y_px")
                    elif layout is not None:
                        crop = (image.height - ground_line) / image.height
                        if abs(layout["arenaBottomCropFraction"] - crop) > 1e-6:
                            errors.append("Layout arenaBottomCropFraction mismatch")
                image.load()
        except (OSError, ValueError) as exc:
            errors.append(f"{label}: invalid PNG: {exc}")
    if seen != set(EXPECTED):
        errors.append("Manifest requires one character and one arena")
    return errors


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument("--manifest", type=Path)
    parser.add_argument("--production", action="store_true")
    args = parser.parse_args()
    manifest = args.manifest or args.root / "art/reference/manifest.json"
    errors = validate_manifest(manifest, args.root, args.production)
    for error in errors:
        print(error)
    if errors:
        return 1
    print("Reference art validation passed")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
