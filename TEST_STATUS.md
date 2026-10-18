# Test status

P0 and P1 verified on 2026-10-18 with Unity 6000.3.26f1 LTS and .NET 10.0.101.

- `bash tools/test-core.sh`: PASS. C# 9 offline source-linked suite covers configuration/math/snapshots; movement, variable jump, coyote/buffer, dash consumption/recharge; divider spring/bounds/symmetric and diagonal rebound; one body hit per dash, body-only guard break and shove-into-projectile; ricochet/continuous sweeps/persistence/ignored pair and divider collisions; parry steering/power/decay/one-charge restoration/zero healing; guard recovery/break and same-time contact legality; spawn warnings/schedule/cap; draw/reset/first-to-two.
- Replay/fuzz: PASS, 18,000 repeated-input comparisons, 60,000 fuzzy-input ticks, 240 completed deterministic bot rounds in 332,295 ticks. No NaNs, bounds/crossing failures, or replay mismatch found. Same-runtime repeatability only.
- Separate netstandard2.1 core Release build: PASS, zero warnings/errors. SDK reference pack was restored from NuGet into a temporary cache; offline smoke runner has no external package prerequisite.
- `bash tools/test-unity.sh`: PASS after final P1 fixes; editor import, bootstrap scene validation, local macOS player build. Logs under ignored `artifacts/logs/` are captured and sanitized.
- `python3 -m unittest discover -s tests -p 'test_*.py'`: PASS, 5 credential-log redaction regressions.
- Independent P1 audit: PASS after fixes for active dash/iframe surviving guard break and repeated guard/parry benefits from continuous overlapping contact. Regressions include fresh recontact and invulnerability expiry while overlapping.

Earlier boundary failures fixed: 60 Hz clock rounding, symmetric divider recoil, same-time wall/parry escape. Shell syntax/JSON checks pass; no separate static-analysis framework configured.

Not yet verified: playable Unity runtime/input/touch/gamepad, safe areas/aspect ratios, real-device frame pacing/thermals, human feel/readability, cross-platform deterministic equivalence. P2 must address feasible runtime checks before the human gate.
