# Project status

Date: 2026-10-20 (Europe/Istanbul)
Milestone: larger-character feedback + early phone-playtest preparation; phone OS/model pending
Branch: mobile/phone-playtest-readability (pulled origin/main before work; prior tested work retained; no issue supplied)
Remote: https://github.com/bekirbilge/Parry.git

Unity6000.3.26f1 LTS, macOS arm64, .NET10.0.101 and active Unity Personal license. Git/Unity/native execution require sandbox escalation.

## Approved gameplay baseline

P0 tooling, P1 fixed-step combat/replay and P2 playable graybox are complete. User passed Gate A on2026-10-20: “ok this is fine now, i think the game is at a good place. whats next??”

Keep fuel movement/current tuning: tank0.9s, refill0.3/s after0.35s pause, enemy parry refunds one-third; upward cap40, horizontal80. Playable body radius2.5 (five-unit diameter,67% larger), parry radius4 plus projectile radius0.9, window0.16s; speed levels24/72/84/96 with4s power thresholds and12units/s² slowdown. Parry safety ends on separation, not permanent ownership. Snapshotversion7. F1 retains three charges for comparison; actual rope/collision geometry remains authoritative. See GDD for complete rules.

Last full core suite:240 fuel rounds/683,400 ticks and240 legacy rounds/223,598 ticks plus replay/fuzz. No Core runtime files changed; new larger-config tests cover the applied2.5-radius bodies.

## Current visual proof

User selected directionC: darker high-contrast pixel art. One static ninja and one empty Rookie Dojo background are imported with point filtering, no mipmaps, uncompressed textures and preserved source dimensions. The projectile/rope/HUD/effects remain procedural. Static art reads simulation snapshots only. F6 toggles art/graybox without reset; missing resources fall back to shapes.

Sources: Assets/Game/Resources/Art/. Exact prompts, hashes, measured character bounds, source floor line and pending approval: art/reference/manifest.json. Layout JSON aligns visible character height/feet and crops the background's decorative lower strip without changing gameplay bounds. Style comparison and provenance: art/concepts/.

Verified: real manifest validator;12/12 Python tests; Unity scene validation, final30/30 EditMode tests and macOS build; native900-step/3-driver-callback smoke at desktop and1280×720. All art/layout loaded. Both checksumsA8EAD925C985E90B match each other; the expected change from the earlier baseline reflects the larger physical body tuning. Rope intrusion4.4846,bend7.3044,recoil18.75 retains prior acceptance limits. Independent source review and diff checks passed. Phone-sized screenshot inspected; this is not physical mobile validation.

Build: artifacts/build/ElasticArena.app
Preview: artifacts/runtime-phone/smoke.png
Reports: artifacts/runtime/smoke.json and artifacts/runtime-phone/smoke.json; artifacts/test-results/editmode.xml
Run/controls/checklist: PLAYTEST_NOTES.md
Validation: python3 tools/assets/validate.py; python3 -m unittest discover -s tests -p 'test_*.py'; bash tools/test-unity.sh; bash tools/test-runtime.sh

## Human gate and next work

Request approval of the ninja and arena appearance/readability in the playable sample. No animation sets or bulk assets approved yet. Current pose is static; pixel-grid/export conventions are proposals. Source/provider provenance is recorded; no blanket commercial clearance or paid service purchase asserted. Real controller/mobile performance and physical-device validation remain unverified.

User explicitly pulled physical phone testing forward before animation. Finish phone-build setup after OS/model selection, collect size/readability feedback and reference approval, then finish the one-character animation/import proof and planP4 effects/audio/HUD polish. PlanP5 networking architecture follows the polished slice; economy and release remain later phases. Preserve current gameplay while working on presentation. A bare continue does not approve reference visuals or bulk generation.

## Phone build preparation

Larger characters are implemented: radius2.5 shared by both players, art/physical body matched; F5 slider1.5–3.5 and applied capture available. Grounding tests cover old/new sizes. New iOS unsigned Xcode-export and Android debug-APK helpers compile; landscape, IL2CPP/ARM64 and safe settings restoration tests pass. Raw PlayerSettings restoration preserves unset/default signing/maps exactly, and ProjectSettings.asset remains unchanged after tests.

Actual mobile export/device run NOT performed. Only Unity macOS playback engine installed. Xcode27.1/iOS27.1 SDK is available, but0 signing identities/0 connected physical devices. Android SDK/adb absent. CLI module listing verified with elevated access: iOS support449MB; Android1.97GB plus JDK113MB. No module installed before phone selection.

Required next input: iPhone or Android and model (async question pending). Then install only that Unity6000.3.26f1 module and run `bash tools/test-mobile.sh ios|android`. Resolve actual build/device/signing needs after producing the reviewable export; no App Store, TestFlight or production deployment authorized. Visual reference Gate B remains pending.
