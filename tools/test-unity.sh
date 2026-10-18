#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
project_dir="$(cd -- "$script_dir/.." && pwd)"
editor_version="6000.3.26f1"
logs_dir="$project_dir/artifacts/logs"
mkdir -p "$logs_dir"
sanitizer="$script_dir/sanitize-unity-log.py"

sanitize_logs() {
  if ! python3 "$sanitizer" "$logs_dir" >/dev/null 2>&1; then
    printf 'Unity log sanitization failed; inspect the sanitizer before sharing logs.\n' >&2
    exit 1
  fi
}

report_stage() {
  local stage="$1"
  local status="$2"
  local editor_log="$3"
  local cli_log="$4"
  sanitize_logs
  if (( status == 0 )); then
    printf '%s passed. Logs: %s %s\n' "$stage" "$editor_log" "$cli_log"
  else
    printf '%s failed (exit %s). Logs: %s %s\n' "$stage" "$status" "$editor_log" "$cli_log" >&2
    exit "$status"
  fi
}

sanitize_logs

if unity run "$project_dir" --editor-version "$editor_version" --timeout 600 -- \
  -nographics -logFile "$logs_dir/unity-validation.log" \
  -executeMethod Game.Editor.BootstrapPipeline.Validate \
  >"$logs_dir/unity-validation.cli.log" 2>&1; then
  validation_status=0
else
  validation_status=$?
fi
report_stage "Unity validation" "$validation_status" "$logs_dir/unity-validation.log" "$logs_dir/unity-validation.cli.log"

if unity build "$project_dir" \
  --editor-version "$editor_version" \
  --target StandaloneOSX \
  --execute-method Game.Editor.BootstrapPipeline.BuildMacOS \
  --output-path "$project_dir/artifacts/build/ElasticArena.app" \
  --log-file "$logs_dir/unity-build.log" \
  --timeout 600 \
  --allow-dirty-build \
  >"$logs_dir/unity-build.cli.log" 2>&1; then
  build_status=0
else
  build_status=$?
fi
report_stage "Unity macOS build" "$build_status" "$logs_dir/unity-build.log" "$logs_dir/unity-build.cli.log"
