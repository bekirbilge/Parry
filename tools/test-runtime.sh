#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
project_dir="$(cd -- "$script_dir/.." && pwd)"
app="$project_dir/artifacts/build/ElasticArena.app"
output_dir="$project_dir/artifacts/runtime"
logs_dir="$project_dir/artifacts/logs"
mkdir -p "$output_dir" "$logs_dir"

if [[ ! -d "$app" ]]; then
  printf 'Built macOS player is missing: %s\n' "$app" >&2
  exit 1
fi

if python3 - "$app" "$output_dir" "$logs_dir" <<'PY'
import json
import pathlib
import plistlib
import struct
import subprocess
import sys

app, output_dir, logs_dir = map(pathlib.Path, sys.argv[1:])
with (app / "Contents" / "Info.plist").open("rb") as plist_file:
    executable = plistlib.load(plist_file).get("CFBundleExecutable")
if not executable or pathlib.Path(executable).name != executable:
    raise SystemExit("Player bundle executable name is invalid")
binary = app / "Contents" / "MacOS" / executable
if not binary.is_file():
    raise SystemExit("Player bundle executable is missing")
report = output_dir / "smoke.json"
screenshot = output_dir / "smoke.png"
rope_screenshot = output_dir / "smoke-rope.png"
report.unlink(missing_ok=True)
screenshot.unlink(missing_ok=True)
rope_screenshot.unlink(missing_ok=True)
with (logs_dir / "runtime.stdout.log").open("wb") as stdout, (logs_dir / "runtime.stderr.log").open("wb") as stderr:
    try:
        result = subprocess.run(
            [str(binary), "--parry-smoke", "--parry-output", str(output_dir)],
            stdout=stdout, stderr=stderr, timeout=120, check=False,
        )
    except subprocess.TimeoutExpired:
        raise SystemExit("Player smoke timed out after 120 seconds")
if result.returncode:
    raise SystemExit(f"Player smoke exited with status {result.returncode}")
if not report.is_file():
    raise SystemExit("Player smoke report is missing")
data = json.loads(report.read_text(encoding="utf-8"))
if (data.get("completed") is not True or data.get("ticks") != 900 or
        data.get("initialTick") != 0 or data.get("finalTick") != 900 or
        data.get("finalTick") - data.get("initialTick") != 900 or
        data.get("driverTicks", 0) < 3 or data.get("driverSimTicks", 0) < 3 or data.get("error")):
    raise SystemExit("Player smoke report failed simulation checks")
if not 0.75 <= data.get("ropeBend", 0) <= 8.000001:
    raise SystemExit("Player rope demo did not produce bounded visible deformation")
if not 4.0 <= data.get("ropeIntrusion", 0) <= 8.000001:
    raise SystemExit("Player boost demo did not push the fighter into the opponent's resting half")
if data.get("ropeReboundSeen") is not True or not 15 <= data.get("ropeRecoilUnits", 0) <= 20:
    raise SystemExit("Player boost demo did not recoil after loading the rope")
for field, path in (("screenshot", screenshot), ("ropeScreenshot", rope_screenshot)):
    if pathlib.Path(data.get(field, "")).resolve() != path.resolve():
        raise SystemExit(f"Player smoke {field} path is unexpected")
    if not path.is_file() or path.stat().st_size < 1000:
        raise SystemExit(f"Player smoke {field} is missing or invalid")
    with path.open("rb") as image_file:
        header = image_file.read(24)
    if header[:8] != b"\x89PNG\r\n\x1a\n" or header[12:16] != b"IHDR":
        raise SystemExit(f"Player smoke {field} is not a PNG")
    image_width, image_height = struct.unpack(">II", header[16:24])
    if image_width < 640 or image_height < 360:
        raise SystemExit(f"Player smoke {field} dimensions are too small")

PY
then
  status=0
else
  status=$?
fi

if ! python3 "$script_dir/sanitize-unity-log.py" "$logs_dir" >/dev/null 2>&1; then
  printf 'Runtime log sanitization failed.\n' >&2
  exit 1
fi
if (( status == 0 )); then
  printf 'Player smoke passed. Report: %s/smoke.json Screenshot: %s/smoke.png Logs: %s/runtime.stdout.log %s/runtime.stderr.log\n' "$output_dir" "$output_dir" "$logs_dir" "$logs_dir"
else
  printf 'Player smoke failed (exit %s). Logs: %s/runtime.stdout.log %s/runtime.stderr.log\n' "$status" "$logs_dir" "$logs_dir" >&2
  exit "$status"
fi
