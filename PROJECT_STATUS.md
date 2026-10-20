# Project status

Date: 2026-10-20 (Europe/Istanbul)
Milestone: iPhone playtest installed and running; awaiting human touch/readability feedback
Branch: mobile/iphone-playtest (pulled origin/main before work; prior tested work retained; no issue supplied)
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

User explicitly pulled physical phone testing forward before animation. Install the compiled playtest on the selected iPhone17 Pro Max after pairing/signing, collect size/readability feedback and reference approval, then finish the one-character animation/import proof and planP4 effects/audio/HUD polish. PlanP5 networking architecture follows the polished slice; economy and release remain later phases. Preserve current gameplay while working on presentation. A bare continue does not approve reference visuals or bulk generation.

## Phone build preparation

Larger characters are implemented: radius2.5 shared by both players, art/physical body matched; F5 slider1.5–3.5 and applied capture available. Grounding tests cover old/new sizes. New iOS unsigned Xcode-export and Android debug-APK helpers compile; landscape, IL2CPP/ARM64 and safe settings restoration tests pass. Raw PlayerSettings restoration preserves unset/default signing/maps exactly, and ProjectSettings.asset remains unchanged after tests.

Selected phone: iPhone17 Pro Max. Now connected and paired; device reports iOS26.6.2 and Developer Mode enabled and development services ready. Unity6000.3.26f1 iOS module installed successfully at /Applications/Unity/Hub/Editor/6000.3.26f1/PlaybackEngines/iOSSupport. Actual Unity iOS export and unsigned Xcode27.1/iPhoneOS27.1 native compilation PASS, including the final phone UI changes. Xcode project: artifacts/build/ios/Unity-iPhone.xcodeproj. Unsigned app: artifacts/build/ios-derived/Build/Products/Debug-iphoneos/ElasticArena.app. Bundle com.bekirbilge.parry.playtest, iOS15 minimum, both landscape orientations.

Phone HUD/toolbar text scales with arena height, toolbar hit/render geometry agrees, footer uses touch instructions. MOVE/BOOST/DEF retain the existing multitouch behavior. --parry-touch previews phone UI on desktop. Unity validation/macOS build and34/34 EditMode tests pass. Desktop and1280×590 touch-preview native smokes each pass900 steps/3 callbacks with unchanged checksumA8EAD925C985E90B. Synthetic notched/high-density and small-phone safe-area tests pass; preview screenshot inspected. Independent source review passed. TUNE remains a desktop-oriented debug panel; use desktop tuning for this first phone playtest.

Device-specific build, signing and installation PASS. Verified the development provisioning profile includes the connected phone; codesign deep/strict verification passes. Free development provisioning expires2026-10-27. App installed as ElasticArena (com.bekirbilge.parry.playtest). Initial iOS security launch block resolved by user trusting the developer profile. User confirms game opens; devicectl process listing independently confirms ElasticArena running. Logs: artifacts/logs/xcode-ios-device-build.log, iphone-install.log, iphone-launch.log (initial trust failure), iphone-processes.log. No private keys exported, no credentials requested.

Next human gate: play in landscape; evaluate MOVE+BOOST+DEF simultaneously, fuel release/refill, notch clearance in both orientations, larger-character readability, missed touches and stutter/heat. Physical launch is verified; gameplay quality/performance and visual reference Gate B await user feedback. No additional production art/animation authorized yet. Continue from this installed build after feedback.
