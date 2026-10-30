#!/usr/bin/env python3
"""Validate the bounded, transparent single-character animation proof."""

import argparse
import hashlib
import json
import math
from pathlib import Path

from PIL import Image, ImageDraw


MANIFEST_PATH = "art/animation/manifest.json"
TEXTURE_PATH = "Assets/Game/Resources/Art/char_ninja_animation_sample_v01.png"
LAYOUT_PATH = "Assets/Game/Resources/Art/ninja_animation_sample_v01.json"
ROWS = ["idle", "run", "boost", "guard", "parry", "recoil"]
STATUSES = {"proof_pending", "approved", "rejected"}


def _positive_int(value):
    return type(value) is int and value > 0


def _under_root(root, relative, label, errors):
    if not isinstance(relative, str) or not relative.strip():
        errors.append(f"Missing {label} path")
        return None
    path = (root / relative).resolve()
    if not path.is_relative_to(root):
        errors.append(f"{label} path escapes project root")
        return None
    if not path.is_file():
        errors.append(f"Missing {label}: {relative}")
        return None
    return path


def _pixels(image):
    # Pillow 12 has get_flattened_data; older installed versions have getdata.
    getter = getattr(image, "get_flattened_data", None)
    return getter() if getter is not None else image.getdata()


def validate_manifest(manifest_path: Path, root: Path, require_production: bool = False) -> list[str]:
    errors = []
    root = root.resolve()
    try:
        manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    except (OSError, UnicodeError, json.JSONDecodeError) as exc:
        return [f"Cannot read animation manifest: {exc}"]
    if not isinstance(manifest, dict):
        return ["Animation manifest must be an object"]

    if manifest.get("version") != 1:
        errors.append("Manifest version must be 1")
    if manifest.get("usage") != "single-character-animation-proof":
        errors.append("Manifest usage must be single-character-animation-proof")
    status = manifest.get("approval_status")
    if status not in STATUSES:
        errors.append("Unknown approval_status")
    if require_production and status != "approved":
        errors.append("Production requires approved approval_status")
    for field in ("created_date", "tool", "background_edit_prompt"):
        if not isinstance(manifest.get(field), str) or not manifest[field].strip():
            errors.append(f"Missing provenance {field}")
    for field in ("reference", "prompt_path", "source_path"):
        _under_root(root, manifest.get(field), field, errors)
    if manifest.get("texture_path") != TEXTURE_PATH:
        errors.append("Manifest texture_path does not match animation proof")
    if manifest.get("layout_path") != LAYOUT_PATH:
        errors.append("Manifest layout_path does not match animation proof")

    if manifest.get("rows") != ROWS:
        errors.append("Manifest rows must be idle, run, boost, guard, parry, recoil")
    if manifest.get("columns") != 4 or manifest.get("frame_count") != 24:
        errors.append("Manifest requires four columns and 24 frames")
    fps = manifest.get("frames_per_second")
    if not _positive_int(fps) or fps > 60:
        errors.append("Invalid frames_per_second")
    if manifest.get("alpha_threshold") != 32:
        errors.append("alpha_threshold must be 32")
    width, height = manifest.get("width"), manifest.get("height")
    if width != 1024 or height != 1536:
        errors.append("Animation canvas must be 1024 x 1536")
    reference_height = manifest.get("reference_height")
    if type(reference_height) not in (int, float) or not math.isfinite(reference_height) or not 0 < reference_height <= 256:
        errors.append("Invalid reference_height")

    texture_path = _under_root(root, manifest.get("texture_path"), "texture", errors)
    layout_path = _under_root(root, manifest.get("layout_path"), "layout", errors)
    for label, path, checksum_field in (
        ("texture", texture_path, "sha256"), ("layout", layout_path, "layout_sha256")
    ):
        if path is not None and manifest.get(checksum_field) != hashlib.sha256(path.read_bytes()).hexdigest():
            errors.append(f"{label} sha256 mismatch")

    if layout_path is None:
        return errors
    try:
        layout = json.loads(layout_path.read_text(encoding="utf-8"))
    except (OSError, UnicodeError, json.JSONDecodeError) as exc:
        return errors + [f"Cannot read animation layout: {exc}"]
    if not isinstance(layout, dict):
        return errors + ["Animation layout must be an object"]
    if (layout.get("columns"), layout.get("rows"), layout.get("cellWidth"), layout.get("cellHeight")) != (4, 6, 256, 256):
        errors.append("Layout grid must be four columns, six rows, 256 x 256 cells")
    if layout.get("referenceHeight") != reference_height:
        errors.append("Layout referenceHeight mismatch")
    frames = layout.get("frames")
    if not isinstance(frames, list) or len(frames) != 24:
        return errors + ["Layout requires exactly 24 frames"]

    valid_frames = []
    for index, frame in enumerate(frames):
        if not isinstance(frame, dict) or any(type(frame.get(key)) is not int for key in ("left", "top", "right", "bottom")):
            errors.append(f"Frame {index}: invalid rectangle")
            continue
        left, top, right, bottom = (frame[key] for key in ("left", "top", "right", "bottom"))
        if not (0 <= left < right <= 1024 and 0 <= top < bottom <= 1536):
            errors.append(f"Frame {index}: rectangle out of bounds")
            continue
        for other_index, (a, b, c, d) in valid_frames:
            if left < c and a < right and top < d and b < bottom:
                errors.append(f"Frame {index}: overlaps frame {other_index}")
        valid_frames.append((index, (left, top, right, bottom)))
    if texture_path is None or len(valid_frames) != 24:
        return errors

    try:
        with Image.open(texture_path) as image:
            if image.format != "PNG" or image.mode != "RGBA":
                return errors + ["Animation texture must be RGBA PNG"]
            if image.size != (1024, 1536):
                return errors + ["Animation texture dimensions mismatch"]
            image.load()
            alpha = image.getchannel("A")
            minimum, maximum = alpha.getextrema()
            if minimum != 0 or maximum <= 32:
                errors.append("Animation texture requires transparency and visible alpha above 32")

            coverage = Image.new("L", image.size, 0)
            draw = ImageDraw.Draw(coverage)
            hashes = {}
            for index, box in valid_frames:
                left, top, right, bottom = box
                draw.rectangle((left, top, right - 1, bottom - 1), fill=255)
                cropped_alpha = alpha.crop(box)
                visible = cropped_alpha.point(lambda pixel: 255 if pixel > 32 else 0).getbbox()
                if visible is None:
                    errors.append(f"Frame {index}: no visible alpha above 32")
                    continue
                if visible[0] < 1 or visible[1] < 1 or visible[2] >= right - left or visible[3] >= bottom - top:
                    errors.append(f"Frame {index}: transparent padding/gutter missing")
                content_box = (left + visible[0], top + visible[1], left + visible[2], top + visible[3])
                content = image.crop(content_box)
                digest = hashlib.sha256(content.size[0].to_bytes(4, "big") +
                                        content.size[1].to_bytes(4, "big") + content.tobytes()).hexdigest()
                if digest in hashes:
                    errors.append(f"Frame {index}: duplicate of frame {hashes[digest]}")
                hashes[digest] = index
            if any(pixel > 32 and not covered for pixel, covered in zip(_pixels(alpha), _pixels(coverage))):
                errors.append("Visible alpha outside frame rectangles")
    except (OSError, ValueError) as exc:
        errors.append(f"Invalid animation PNG: {exc}")
    return errors


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument("--manifest", type=Path)
    parser.add_argument("--production", action="store_true")
    args = parser.parse_args()
    errors = validate_manifest(args.manifest or args.root / MANIFEST_PATH, args.root, args.production)
    for error in errors:
        print(error)
    if errors:
        return 1
    print("Animation proof validation passed")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
