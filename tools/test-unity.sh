#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
project_dir="$(cd -- "$script_dir/.." && pwd)"
editor_version="6000.3.26f1"
logs_dir="$project_dir/artifacts/logs"
mkdir -p "$logs_dir"

unity run "$project_dir" --editor-version "$editor_version" --timeout 600 -- \
  -nographics -logFile "$logs_dir/unity-validation.log" \
  -executeMethod Game.Editor.BootstrapPipeline.Validate

unity build "$project_dir" \
  --editor-version "$editor_version" \
  --target StandaloneOSX \
  --execute-method Game.Editor.BootstrapPipeline.BuildMacOS \
  --output-path "$project_dir/artifacts/build/ElasticArena.app" \
  --log-file "$logs_dir/unity-build.log" \
  --timeout 600 \
  --allow-dirty-build
