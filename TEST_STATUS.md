# Test status

P0 partial validation (2026-10-18):

- `bash tools/test-core.sh`: PASS, offline source-linked C# 9 smoke suite; config validation, invalid elapsed time, fixed tick numbering, fractional accumulation, deferred catch-up without lost ticks, and bounded interpolation.
- Separate `Game.Core.csproj` Release build targeting `netstandard2.1`: PASS, zero warnings/errors. Reference package restored from NuGet into `/private/tmp/parry-nuget-verify`; the .NET SDK lacks this targeting pack out of the box.
- Shell syntax and assembly-definition JSON: PASS.
- Independent preliminary review: caught stale checkpoint text, interpolation backlog semantics, and missing Unity scaffold. These are addressed. Final editor/build review remains pending.
- `bash tools/test-unity.sh`: NOT RUN, LTS editor still installing. Project version is pinned to 6000.3.26f1; adapter and Editor API bootstrap/build entry points exist.

An initial clock boundary test failed because repeated subtraction emitted 59 ticks for one second at 60 Hz. A small rounding tolerance fixed the boundary; the final smoke suite passes. No gameplay mechanics, Unity import, player build, fuzz/replay, device performance, or human playtests have passed yet.
