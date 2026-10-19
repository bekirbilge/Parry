# Test status

P0 and P1 verified on 2026-10-18 with Unity 6000.3.26f1 LTS and .NET 10.0.101.

- `bash tools/test-core.sh`: PASS. C# 9 offline source-linked suite covers configuration/math/snapshots; movement, variable jump, coyote/buffer, dash consumption/recharge; divider spring/bounds/symmetric and diagonal rebound; one body hit per dash, body-only guard break and shove-into-projectile; ricochet/continuous sweeps/persistence/ignored pair and divider collisions; parry steering/power/decay/one-charge restoration/zero healing; guard recovery/break and same-time contact legality; spawn warnings/schedule/cap; draw/reset/first-to-two.
- Replay/fuzz: PASS, 18,000 repeated-input comparisons, 60,000 fuzzy-input ticks, 240 completed deterministic bot rounds in 332,295 ticks. No NaNs, bounds/crossing failures, or replay mismatch found. Same-runtime repeatability only.
- Separate netstandard2.1 core Release build: PASS, zero warnings/errors. SDK reference pack was restored from NuGet into a temporary cache; offline smoke runner has no external package prerequisite.
- `bash tools/test-unity.sh`: PASS after final P1 fixes; editor import, bootstrap scene validation, local macOS player build. Logs under ignored `artifacts/logs/` are captured and sanitized.
- `python3 -m unittest discover -s tests -p 'test_*.py'`: PASS, 5 credential-log redaction regressions.
- Independent P1 audit: PASS after fixes for active dash/iframe surviving guard break and repeated guard/parry benefits from continuous overlapping contact. Regressions include fresh recontact and invulnerability expiry while overlapping.

Earlier boundary failures fixed: 60 Hz clock rounding, symmetric divider recoil, same-time wall/parry escape. Shell syntax/JSON checks pass; no separate static-analysis framework configured.

P1 did not include Unity runtime or hardware validation. P2 automated results follow; real-device frame pacing/thermals, physical touch/gamepad behavior, subjective feel/readability and cross-platform deterministic equivalence remain unverified.

P2 verified on 2026-10-19:

- Full core suite remains PASS with DebugBot determinism/immutability tests; 240 bot rounds / 332,295 ticks. Python sanitizer suite: 5/5 PASS.
- `bash tools/test-unity.sh`: PASS, Unity LTS 6000.3.26f1 scene validation, 8/8 EditMode tests and macOS build; zero C# diagnostics. Tests cover scene/camera/audio listener, joystick-only axes and Y direction, safe-area/footer layout at 1280x720/16:9/ultrawide, tick adapter, short/held input edges, independent touch fingers, and bot difficulty preserving authoritative state/stats.
- `bash tools/test-runtime.sh`: PASS, native graphical player; initialTick 0, finalTick 900, 900 steps, 3 driver callbacks each advancing simulation. PNG and active tuning capture validated. Final checksum `93821CEC7BFF7288`.
- Independent P2 review: PASS after exposing bot difficulty and showing full collision silhouettes. Final screenshot visually inspected; footer is separate from playfield and grounded fighters fit.
- `git diff --check`, script syntax and package/config JSON checks pass. Unity-generated empty YAML values retain engine formatting; whitespace attributes exempt their trailing spaces.

P2 runtime fixes verified: GUI initialization moved into OnGUI; touch coordinate scope corrected; membrane view matches collision; mobile toolbar and shared touch regions; applied-only captures; footer/guard/hitbox readability; walking contact does not trigger hitstop; camera audio listener present.

Human gameplay approval and physical mobile/gamepad testing remain outstanding. Stop at Gate A.
