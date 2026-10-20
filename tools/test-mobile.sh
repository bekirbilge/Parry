#!/usr/bin/env bash
set -euo pipefail

if (( $# != 1 )) || { [[ "$1" != ios ]] && [[ "$1" != android ]]; }; then
  printf 'Usage: bash tools/test-mobile.sh ios|android\n' >&2
  exit 2
fi

script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
project_dir="$(cd -- "$script_dir/.." && pwd)"
logs_dir="$project_dir/artifacts/logs"
mkdir -p "$logs_dir"
sanitizer="$script_dir/sanitize-unity-log.py"
platform="$1"

if [[ "$platform" == ios ]]; then
  target="iOS"
  method="Game.Editor.MobileBuildPipeline.BuildIOS"
  output="$project_dir/artifacts/build/ios"
else
  target="Android"
  method="Game.Editor.MobileBuildPipeline.BuildAndroid"
  output="$project_dir/artifacts/build/Parry-playtest.apk"
fi

editor_log="$logs_dir/unity-mobile-$platform.log"
cli_log="$logs_dir/unity-mobile-$platform.cli.log"

if unity build "$project_dir" \
  --editor-version 6000.3.26f1 \
  --target "$target" \
  --execute-method "$method" \
  --log-file "$editor_log" \
  --timeout 1800 \
  --allow-dirty-build \
  --no-tail \
  >"$cli_log" 2>&1; then
  build_status=0
else
  build_status=$?
fi

if ! python3 "$sanitizer" "$logs_dir" >/dev/null 2>&1; then
  printf 'Unity log sanitization failed; inspect the sanitizer before sharing logs.\n' >&2
  exit 1
fi
if (( build_status != 0 )); then
  printf '%s playtest export failed (exit %s). Logs: %s %s\n' "$target" "$build_status" "$editor_log" "$cli_log" >&2
  exit "$build_status"
fi

if [[ "$platform" == ios ]]; then
  if [[ ! -d "$output" ]] || ! compgen -G "$output/*.xcodeproj" >/dev/null; then
    printf 'iOS export reported success but no Xcode project exists in %s\n' "$output" >&2
    exit 1
  fi
else
  if [[ ! -s "$output" ]]; then
    printf 'Android export reported success but no nonempty APK exists at %s\n' "$output" >&2
    exit 1
  fi
fi
printf '%s playtest export passed: %s\nLogs: %s %s\n' "$target" "$output" "$editor_log" "$cli_log"
