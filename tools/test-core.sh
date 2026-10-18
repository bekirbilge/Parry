#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cache_root="${TMPDIR:-/tmp}/parry-dotnet-core"
mkdir -p "$cache_root/cli-home" "$cache_root/nuget-packages"

export DOTNET_CLI_HOME="$cache_root/cli-home"
export NUGET_PACKAGES="$cache_root/nuget-packages"
export DOTNET_NOLOGO=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

cat > "$cache_root/NuGet.Config" <<'CONFIG'
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
  </packageSources>
</configuration>
CONFIG

project="$repo_root/tests/Parry.Core.SmokeTests.csproj"
dotnet restore "$project" --configfile "$cache_root/NuGet.Config"
dotnet run --project "$project" --configuration Release --no-restore
