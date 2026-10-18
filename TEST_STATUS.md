# Test status

P0 validation (2026-10-18):

- `bash tools/test-core.sh`: PASS, offline source-linked C# 9 smoke suite; config validation, invalid elapsed time, fixed tick numbering, fractional accumulation, deferred catch-up without lost ticks, bounded interpolation, GDD tuning baselines/posture rounding, and invalid tuning relationships.
- Separate `Game.Core.csproj` Release build targeting `netstandard2.1`: PASS, zero warnings/errors. Reference package restored from NuGet into `/private/tmp/parry-nuget-verify`; the .NET SDK lacks this targeting pack out of the box.
- Shell syntax and assembly-definition JSON: PASS.
- Independent reviews: caught stale checkpoint text, interpolation backlog semantics, missing Unity scaffold, and an unsupported tuning restriction; fixed. Final P0 audit found no material scaffold blocker. Unity stdout is captured and logs sanitized after the CLI echoed a transient session credential; sanitizer synthetic checks pass.
- `bash tools/test-unity.sh`: PASS, exit 0 using Unity 6000.3.26f1. Editor import and bootstrap-scene validation succeeded; macOS player build succeeded at `artifacts/build/ElasticArena.app` (102 MiB). Source compiled without C# warnings/errors in editor/build logs.

An initial clock boundary test failed because repeated subtraction emitted 59 ticks for one second at 60 Hz. A small rounding tolerance fixed the boundary; the final smoke suite passes. No gameplay mechanics, fuzz/replay, device performance, or human playtests have passed yet. The bootstrap build contains only the clock scaffold and is not a playable game.
