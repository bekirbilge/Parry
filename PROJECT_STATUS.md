# Project status

Date: 2026-10-19 (Europe/Istanbul)
Milestone: HUMAN GATE A — core gameplay playtest/approval
Last verified milestone: P2 playable graybox
Branch: playtest/movement-iteration (local main updated to verified P2; no remote or issue supplied)

Unity 6000.3.26f1 LTS is installed and pinned (user explicitly confirmed LTS). Environment: macOS arm64, .NET SDK 10.0.101, Xcode 27.1, active Unity Personal license. No remote exists to pull from; original planning documents are preserved on main. Git and Unity execution need sandbox escalation.

Completed:
- P0: repository/checkpoints, assembly boundaries, configuration, offline tests and reproducible Unity build.
- P1: movement/jump/dash; spring divider/body combat; continuous projectile/defense solver; spawn/decay/posture; draw/reset/best-of-three; snapshots and same-runtime replay/fuzz.
- P2: playable graybox/HUD/telegraphs, keyboard/gamepad/multitouch adapters, bot/local two-player modes, adjustable bot difficulty, tuning/capture, pause/slow/step, scene and build automation.

Verified: full core suite (240 bot rounds in 332,295 ticks, 60k fuzz and 18k replay), 5 Python tests, Unity scene validation, 8/8 EditMode tests, macOS player build with zero C# diagnostics, graphical smoke 900 authoritative steps including 3 through the live fixed-tick callback. Independent P2 review passed after difficulty and hitbox-visibility fixes. Final screenshot inspected.

Build: `artifacts/build/ElasticArena.app`.
Preview/report: `artifacts/runtime/smoke.png`, `artifacts/runtime/smoke.json`.
Test report: `artifacts/test-results/editmode.xml`.
Run/control/checklist guide: `PLAYTEST_NOTES.md`.

Commands: `bash tools/test-core.sh`; `python3 -m unittest discover -s tests -p 'test_*.py'`; `bash tools/test-unity.sh`; `bash tools/test-runtime.sh`.

Known limitations: placeholder IMGUI graphics and synthesized audio; real mobile touch/gamepad mappings, device performance/thermals and subjective fairness/readability remain unverified. No cross-platform bitwise determinism claim. No production art, online networking or economy implemented.

Gate A feedback iteration 1: faster run/jump/dash and immediate testing start implemented and rebuilt. Validation passed: full core suite (240 bot rounds / 215,564 ticks, fuzz/replay), 5 Python tests, 9/9 Unity EditMode tests, scene validation, macOS build, and graphical smoke (900 steps, 3 live driver ticks; checksum `9F52A88A715D8E25`). Independent change review passed and screenshot inspected. First projectile warning remains three seconds; normal countdown is selectable in TUNE. User feedback is not production approval.

Blocker: mandatory human gameplay approval; requested feel changes are ready for another playtest.
Next action: user plays for five minutes and reports movement, dash, parry, guard, membrane, fairness and readability; explicitly approve core combat or request changes. Capture edited values if tuning.
Resume: read this file and PLAYTEST_NOTES.md. Implement requested feel fixes and repeat checks as necessary; begin plan P3 only after explicit gameplay approval. A bare continue does not imply approval of subjective gameplay. No art/network/release approvals granted.
