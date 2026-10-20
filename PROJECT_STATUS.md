# Project status

Date: 2026-10-20 (Europe/Istanbul)
Milestone: HUMAN GATE A — core gameplay playtest/approval
Last verified milestone: P2 playable graybox, with approved feedback iterations
Branch: playtest/parry-area-upward-height (updated local main; no remote or issue supplied)

Unity 6000.3.26f1 LTS is installed and pinned per user instruction. macOS arm64, .NET SDK 10.0.101, Xcode 27.1, active Unity Personal license. No remote to pull from. Git metadata and Unity execution require sandbox escalation.

Completed: P0 tooling/configuration/tests; P1 pure fixed-step combat/snapshots/replay; P2 playable graybox, keyboard/gamepad/touch, bots/local two-player, HUD/tuning/capture and automated macOS build.

Latest approved feedback implemented:
- Three boosts; Cross/Shift/Space activates boost, Square/E parries or guards. Continuous normalized aim replaces eight-way snapping; neutral aim boosts upward with upward speed capped at40 (previous80). Each burst retains its launch direction; no separate playable jump control. Bot movement uses the same charge-consuming action.
- 0.30 s powered boost, 0.08 s invulnerability, ~0.20 s contact loading before recoil. A 21-point anchored rope with floor/ceiling hinges permits actual grounded forward stretch while keeping collision/render geometry aligned. Local boost stiffness does not accelerate remote walking contacts. Outward boosts can escape loading; inward restarts cannot extend it.
- Doubled active parry radius3 versus body1.5, matching the visible halo. The 0.16 s parry window and all guard/recovery timings remain unchanged through boost/loading/recoil. Charged projectile speeds 72/84/96; four seconds per power-level decay.
- Last parrier is immune to that projectile until opponent perfect parry; wall/guard/decay preserve ownership. Owned shots pass through unless a timed parry redirects them; SAFE REDIRECT gives no power/charge/decay bonus. Ring indicates owner.

Verified: full core suite (240 deterministic bot rounds / 212,809 ticks, 60k fuzz and 18k replay), Unity scene validation, 20/20 EditMode tests, macOS build, independent final source review, graphical smoke (900 steps, 3 live fixed-tick callbacks; checksum `E1AF683B2C4111C1`). Native boost demo: 5.61-unit forward intrusion, 7.30-unit rope bend, then 18.75-unit recoil. Screenshot inspected. Earlier Python sanitizer tests remain 5/5 passing.

Build: `artifacts/build/ElasticArena.app`.
Reports: `artifacts/runtime/smoke.json`, `smoke.png`, `smoke-rope.png`; `artifacts/test-results/editmode.xml`.
Run, current controls and checklist: `PLAYTEST_NOTES.md`.
Commands: `bash tools/test-core.sh`; `bash tools/test-unity.sh`; `bash tools/test-runtime.sh`.

Known limits: placeholder visuals/audio, real controller/mobile testing and subjective fairness remain unverified; same-runtime repeatability only. No production art, networking, economy or release work approved. Legacy jump commands remain internal for regression/replay compatibility and are not exposed in playable controls.

Current feedback iteration complete: active parry area doubled, upward neutral rise reduced from50 to18.33 units, timing/recovery unchanged, and close opponent returns clear stale contact masks. Core, Unity, build and native smoke pass. Snapshot version4 includes separate parry overlap state.

Next discussion requested by user: evaluate a fuel meter instead of three discrete boosts. No fuel mechanic approved or implemented; current three-charge rules remain for this build.

Blocker: mandatory human gameplay approval. Next: user tests the combined boost/rope/parry/ownership changes and reports feel or explicit approval. A bare continue does not bypass Gate A. Resume from this file, PLAYTEST_NOTES.md and the planning documents; implement feedback, rerun relevant checks, and enter plan P3 only after explicit gameplay approval.
