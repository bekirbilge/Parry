# Test status

## Current verified build — 2026-10-22

- Unity validation, **88/88 EditMode tests**, macOS build and independent review passed for the downward-control fixes.
- Desktop/phone native smoke: 900 ticks and at least three live simulation callbacks each. Phone checksum `03EF15438A58E0D9`.
- iOS export, signed build, signature verification, wireless installation and launch passed. Reconnecting Wi-Fi resolved earlier device-service timeouts.
- Actual iPhone readback: `edge-aware-directional-swipe-follow-stick`, boost tank 1.8, projectile speeds 36 / 72 / 84 / 96, arena 129.5681 × 56.25.
- Latest input coverage includes short downward edge flicks, jitter/slow rejection, quiet rearm, following joystick center and downward airborne movement. Directional parry/contact, steering takeover, release/cancel/menu and ownership regressions remain passing.
- Last full core suite passed 240 fuel rounds / 683,400 ticks, 240 legacy rounds / 223,598 ticks and 96 wide rounds / 450,686 ticks. Core logic was unchanged by the latest input fixes; that suite was not rerun for documentation updates.

Physical phone feel and visual approval remain pending. The records below describe historical iterations; earlier locked/timeout checkpoints are resolved by the current build.

## Validation history

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

Gate A feedback iteration 1 verified on 2026-10-19:

- Full core suite PASS: 240 deterministic bot rounds / 215,564 ticks, 60,000 fuzz ticks and 18,000 replay comparisons under faster movement defaults. New regressions cover immediate first-step movement/jump, one round-start/warning event, full opening warning, no fighter reset at spawn, immediate round reset and preserved standard countdown.
- Python log tests: 5/5 PASS. Unity validation/build PASS, 9/9 EditMode tests; added controller default, applied mode toggle and capture coverage.
- Native graphical smoke PASS: 900 authoritative steps, 3 live driver callbacks advancing simulation; checksum `9F52A88A715D8E25`. Captured tuning confirms run 24 / jump 36 / dash 80 / immediate test start true. Screenshot inspected.
- Independent narrow review PASS; subjective feel remains at Gate A.

Gate A elastic rope iteration verified on 2026-10-19:

- Final core suite PASS: 240 deterministic bot rounds / 207,610 ticks, 60,000 fuzz ticks, 18,000 replay comparisons. Focused tests cover pinned anchors, independent contact heights, spring return, no instant bend relocation, circle/segment clearance, symmetric vertical/diagonal swept contact, rebound, deep node-array clone/hash and feasible tuning bounds.
- Review caught a legal-endpoint/illegal-midpath case around a local bend. Fixed with exact motion-segment/rope-segment clearance and outward endpoint search; swept-only contacts trigger recoil. Invalid stretch values that leave no room for a fighter are rejected.
- Final Unity validation/build PASS: 10/10 EditMode tests, including isolated preview geometry and unchanged live simulation. Native smoke PASS: 900 steps / 3 live driver ticks, checksum `4483D9EC75658E22`; isolated rope demo bend 3.523 units. Both PNGs validated and bent-rope screenshot inspected after joining rasterized segments.
- Independent final source review PASS, with all pending automated checks subsequently passing. `git diff --check` and runtime shell syntax pass. Subjective rope feel remains at Gate A.

Gate A three-dash iteration verified on 2026-10-19:

- Full core suite PASS: 240 deterministic bot rounds / 205,116 ticks, 60,000 fuzz ticks and 18,000 replay comparisons. Checks include three dash consumptions, fourth denied without recharge, sequential refill through all three slots, parry restores one without posture healing and caps at three, round reset restores full charges.
- Unity scene validation/build PASS; 10/10 EditMode tests, including three starting/reset charges and two remaining after a dash through the controller bridge. Native smoke PASS (900 steps / 3 live driver ticks), checksum `A8604A136299A3BA`; screenshot confirms three HUD indicators per fighter.
- Independent final review and `git diff --check` PASS. Gameplay approval remains outstanding.

Controller layout iteration verified on 2026-10-19:

- Unity validation/build PASS, 14/14 EditMode tests. Virtual gamepad states verify south/Cross dash without jump and buffered short taps; west/Square press, hold, release and repress for defense; north/Triangle variable jump hold plus normalized stick; gamepad excluded from P2. Devices are removed after each test. Both-backend setting and assembly/package integration verified.
- Native graphical smoke PASS: 900 steps, 3 live driver ticks, checksum `A8604A136299A3BA`. Authoritative gameplay is unchanged; core suite was not rerun for this input-adapter change.
- First compile exposed ambiguous TouchPhase from the two input APIs; explicit aliases fixed it before the passing rerun. Independent review and `git diff --check` PASS. Real controller hardware/button feel and multi-controller behavior still need a playtest.

Rocket boost and parry refinement verified on 2026-10-20:

- Final core suite PASS: 240 deterministic bot rounds / 222,700 ticks, 60,000 fuzzy-input ticks, 18,000 replay comparisons. New coverage includes mirrored ground/air forward loading, delayed one-time recoil, circle/swept hinge clearance, inward restart denial/outward escape, cancellation of separated load, neutral-up boost, bot action parity and parry during load/recoil after iframes. Near-rope measured peak x55.90 grounded / x55.21 airborne (mirrored x44.10 / x44.79), contact tick1 and rebound tick13.
- Projectile ownership regressions PASS: own shots pass through without damage/posture/free charges, opponent parry transfers immunity, previous owner becomes vulnerable, unclaimed shots kill, wall/guard/decay preserve ownership, snapshot/hash/restore retain it. Decay tests hold each level for 240 ticks and cap at level3; level1 speed supports >75% horizontal arena travel even diagonally.
- Final Unity validation and macOS build PASS; 18/18 EditMode tests. Virtual gamepad tests include neutral upward boost, Cross+Square simultaneous input, actual projectile parry via the controller after boost iframes expire, North unassigned, two-action touch controls, applied tuning capture and deep rope preview. Fixed an exhaust-only Vector2 type typo before the passing rerun.
- Native graphical smoke PASS: 900 steps / 3 live driver callbacks, checksum `54D81295953BC344`. Separate grounded powered preview shows +5.6126 intrusion, 7.3044 bend, then recoil18.75. Images inspected; final runtime script verifies forward intrusion and rebound/recoil as well as PNG validity.
- Independent final source review PASS after fixing hidden bot jump, local stiffness scope and exact contact at load release. `git diff --check` and shell syntax PASS. Physical hardware and human gameplay approval remain outstanding.

Free-angle boost and faster parries verified on 2026-10-20:

- Full core suite PASS: 240 deterministic bot rounds / 208,240 ticks, 60,000 fuzz ticks and 18,000 replay comparisons. Boost tests sweep non-45-degree angles, signs/mirrors and weak input, preserving uniform speed, neutral upward launch, fixed burst direction and charge rules.
- Owned redirects PASS: deliberate active-parry contact reflects an owned shot without power/charge/decay refresh; idle/held guard remains pass-through; owner/speed persist; overlap does not repeatedly redirect. Enemy fast-shot sweeps against moving boost paths and simultaneous wall/owned-redirect regressions PASS. Charged speeds double to 72/84/96, with four-second decay unchanged.
- Unity scene validation/build PASS, 19/19 EditMode tests including virtual stick aim slope1/3 and actual Square owned redirect through GameController with unchanged charge/power and natural timer decrement. Native smoke PASS: 900 steps / 3 live driver callbacks, checksum `69632E7ED4FF11A4`; rope intrusion5.61, bend7.30 and recoil18.75 retained.
- Independent final review and `git diff --check` PASS. No missing enemy-shot collision was reproduced; reported safe pass-through now has an explicit timed redirect. Physical hardware and subjective movement fluidity still require human feedback.

Parry-area and upward-height iteration verified on 2026-10-20:

- Final full core suite PASS: 240 deterministic bot rounds / 212,809 ticks, 60,000 fuzz ticks and 18,000 replay comparisons. Active parry radius doubles from1.5 to3 using extra reach1.5; body/guard/invulnerability/lethal radii stay physical. Timing/recovery defaults are explicitly unchanged.
- New coverage: moving boost grazes, inside/outside parry reach, expired window, parry after invulnerable overlap, later physical guard contact, own redirects, wall pinches, persistent mask clone/hash and close opponent returns. Review found stale mask suppression on close returns; reflection now clears the opposite fighter's old markers. The regression starts outside the physical radius at legal divider positions and uses120Hz; it and the full suite pass. Synthetic opponent-return fixtures clear both markers, matching an actual reflection. Snapshot version4 hashes separate physical/parry masks.
- Neutral upward boost apex measured18.333 units versus50.000 with prior cap80. New cap40 preserves exact launch angle by reducing magnitude only when necessary; horizontal/down80 and duration0.30 unchanged. Angle/stick magnitude, diagonal recoil and divider tests PASS.
- Unity validation and macOS build PASS,20/20 EditMode tests, including virtual Cross+Square catching a fast graze outside body radius while timing stays0.16/0.08. Native smoke PASS:900 steps/3 live driver ticks, checksum `E1AF683B2C4111C1`; forward rope intrusion5.61 and recoil18.75 retained.
- Independent source audit found no remaining blocker after the return-mask correction; parent confirmed the requested expanded-only regression and full suite passed. `git diff --check` PASS. Subjective feel and physical controller testing remain at Gate A.

Fuel trial verified on 2026-10-20:

- Full core suite PASS: 240 fuel bot rounds / 1,560,360 ticks and 240 legacy rounds / 212,809 ticks. New 60,000-tick fuel randomized replay/restore checks plus existing legacy 60k fuzz / 18k replay pass. Coverage includes fixed-step burn/ignition/delayed refill, continuous steering/upward cap, release/coast, exhaustion/recoil latch, independent invulnerability rearm, enemy-only one-third refund and cap, owned redirect without refund, load cancellation, snapshot version5, and one body hit per ignition.
- Independent review found recoil-tick body attack classification and guard-break clearing an existing release requirement. Fixed both and added passing regressions; final source review found no remaining material issue. The body regression uses legal tangent positions but isolates contact classification rather than the complete divider-to-combat path.
- Unity LTS scene validation,23/23 EditMode tests and macOS build PASS after fixes. Virtual Cross hold, steering while held, Square during thrust, release, buffered tap and fuel/three-charge mode reset/capture are covered.
- Final native graphical smoke PASS:900 steps /3 live fixed callbacks; checksum9E2541FD7938FCFA. Rope preview intrusion5.6126,bend7.3044,recoil18.75. Fuel HUD screenshot inspected. Python log tests5/5 and git diff check PASS.
- Subjective fuel feel, physical controller/mobile behavior and gameplay approval remain at Gate A. Previous tested three-charge state remains on origin/main; fuel experiment stays on playtest/fuel-boost.

Projectile readability iteration (2026-10-20):

- Radius0.9 (50% larger diameter) shared by drawing and collision. Fixed-step deceleration12 units/s² starts after each four-second power threshold, persists after level0, and ends at the new target. Snapshot version6 hashes the ramp flag.
- Unity LTS scene validation,23/23 EditMode tests and macOS build PASS. Applied capture confirms radius0.9, slowdown12, level decay4s, fuel mode.
- Native graphical smoke PASS:900 steps/3 live callbacks, checksumB6D8344A194382A5; rope intrusion5.6126,bend7.3044,recoil18.75 unchanged. Larger-ball screenshot inspected. Independent source review found no material issues after two radius-sensitive fixture updates; diff check clean.
- Final full core suite PASS:240 fuel bot rounds/1,305,600 ticks and240 legacy rounds/230,912 ticks, including existing fuel/legacy replay and fuzz. New regressions cover bounded monotonic speed loss, final level0 ramp completion, snapshot/hash replay, immediate enemy parry reset and ongoing ramp through owned redirect/guard/wall. Radius-sensitive fixtures preserve expanded-only parry and body-shove collision ordering. Human gameplay/fuel feedback remains required.

Contact-only parry safety iteration (2026-10-20):

- Full core suite PASS:240 fuel bot rounds/683,400 ticks and240 legacy bot rounds/223,598 ticks, plus fuel/legacy replay and fuzz. Snapshot version7 uses LastParrierId only as active contact protection, cleared after separation.
- Regressions cover immediate overlap without repeated reward, moving boosted pass-through, separation and lethal return, normal return parry reward, unrelated projectile danger, opponent replacement, same-tick safety exit/wall return, exactly two simultaneous overlapping parries, guard/wall/ramp behavior and snapshot/hash state. Existing compound-wall expectation updated to safety expiration.
- Independent review found and resolved SafetyExit falling through to body collision handling and unsafe zero-time repeated parries. A local contact-fraction map permits later opposite returns while retaining same-time repeat suppression. Final source review and diff check PASS.
- Final Unity LTS scene validation,23/23 EditMode tests and macOS build PASS. Native smoke PASS:900 steps/3 live callbacks, checksum93367EB12312CE78, rope intrusion5.6126/bend7.3044/recoil18.75 retained. Screenshot inspected; gameplay approval remains required.

P3 C-style static reference proof verified on2026-10-20:

- Core source unchanged. Art toggle parity and actual PNG-derived grounding/layout tested. Final Unity LTS scene validation,28/28 EditMode tests and macOS build PASS. New tests cover Resources loading, pixel-safe importer settings/source dimensions, true character alpha, visible-height/sole/arena-UV geometry at three viewports and F6 preserving simulation state.
- Actual art manifest validation PASS;12/12 Python tests PASS, including prior log sanitizers and new transparency/dimension/hash/path/approval/alpha-bounds/layout/floor-crop regressions. Pillow12.3.0 pinned in tools/assets/requirements.txt. Production mode rejects pending per-asset approval.
- Final desktop native smoke and1280×720 window profile PASS:900 steps/3 live driver callbacks; all resources/layout loaded; checksum93367EB12312CE78 exactly matches approved graybox. Rope intrusion5.6126,bend7.3044,recoil18.75 retained. Corrected cosmetic background floor crop; final phone-sized screenshot inspected. Physical mobile testing not claimed.
- Independent source/manifest review PASS after approval/layout validator fixes. git diff check PASS. No full core suite rerun for this presentation-only iteration; last verified Core results above remain applicable.
- Human Gate A passed; C direction selected. Gate B character/arena reference aesthetics and subsequent production animations/bulk assets remain pending.

Larger-character and early-mobile preparation verified on2026-10-20:

- Full core suite PASS including new applied-radius2.5 tests: spawn/floor/ceiling, larger physical vs parry contact, contact-safety expiry, mirrored rope loading/no crossing and one dash body hit. Existing240 fuel bot rounds/683,400 ticks and240 legacy rounds/223,598 ticks plus replay/fuzz pass. Core defaults remain1.5 for regressions; actual controller applies2.5.
- Unity scene validation/macOS build PASS. Final30/30 EditMode tests PASS after extending art geometry across1.5/2.5 radii and all three viewports. Mobile helper tests verify landscape/debug settings and failure-path restoration. Typed API restoration initially materialized unset PlayerSettings defaults; fixed with raw EditorJsonUtility snapshot/restore and a raw-state equality regression. Final ProjectSettings.asset has no diff.
- Real art manifest validation and12/12 Python tests PASS. Native desktop and1280×720 smoke PASS,900 steps/3 live callbacks; checksumA8EAD925C985E90B on both. Capture confirms fighter radius2.5. Native rope intrusion4.4846,bend7.3044,recoil18.75 passes unchanged thresholds; enlarged-character screenshot inspected.
- Mobile wrapper shell syntax/usage checks PASS; exporter source compiles and settings tests pass. Actual iOS/Android exports, mobile input-backend compatibility and physical device behavior remain UNTESTED: phone selection pending and required Unity modules absent. No mobile install or signing attempted.
- Independent source review passed after raw settings restoration fix. Diff checks clean. Phone OS/model is the next required user input; visual approval and real-device feedback remain pending.


iPhone playtest preparation verified on2026-10-20:

- Installed pinned Unity6000.3.26f1 iOS support; actual final iOS export PASS via `bash tools/test-mobile.sh ios`.
- Unsigned native device-SDK compilation PASS with Xcode27.1/iPhoneOS27.1: Unity-iPhone scheme, Debug, generic iOS destination, CODE_SIGNING_ALLOWED=NO. Final log artifacts/logs/xcode-ios-build.log reports BUILD SUCCEEDED. App metadata confirms bundle com.bekirbilge.parry.playtest, minimum iOS15, landscape left/right.
- Unity bootstrap validation/macOS build PASS;34/34 EditMode tests PASS. New tests cover synthetic notched2868×1320 and640×360 safe areas, toolbar/control separation, phone input routing and default desktop layout.
- Desktop and1280×590 --parry-touch native smokes PASS:900 steps/3 callbacks, both checksumA8EAD925C985E90B. Touch preview screenshot artifacts/runtime-touch/smoke.png visually inspected. No Core changes; prior full core/Python results remain applicable.
- Independent presentation review PASS after mobile layout was threaded through InputAdapter. TUNE debug panel remains desktop-sized, documented. Initial platform switch generated empty icon/default import metadata; discarded that generated churn after export, retaining source settings.
- Physical iPhone17 Pro Max signing/install/touch/frame pacing/thermal tests NOT RUN. User has not connected/trusted phone. Zero local signing identities; device-service discovery inconclusive. Xcode warns of stale simulator services; this did not prevent successful physical-device SDK compilation. Stop for local account/device setup, not production deployment.

Connected-device follow-up (2026-10-20): iPhone17 Pro Max paired, reporting iOS26.6.2. User enabled Developer Mode; device reports enabled and development services available. Actual device-destination automatic-signing Xcode build PASS, codesign deep/strict verification PASS, provisioning profile includes this phone (expires2026-10-27). devicectl installation PASS. Initial launch blocked by iOS Security with signature/entitlement/profile-trust alternatives; requested manual developer trust because local signature/profile checks pass. User trusted the developer profile and confirms game opens. Independent devicectl process listing confirms ElasticArena running. Installation/launch verified; subjective touch/readability, sustained frame pacing and thermal validation remain pending. No source changes or gameplay tests required in this follow-up.

Phone screen/menu iteration verified on2026-10-20:

- Final35/35 EditMode tests PASS, Unity validation/macOS build PASS. Tests cover mobile full safe-area/no footer, smaller controls, pause menu bounds, clearing touch input while menu is open, hidden tuning-layer input suppression and actual Pause/Restart touch path resetting tick0/resuming.
- Desktop and1280×590 full-screen touch smokes PASS900 steps/3 callbacks; unchanged checksumA8EAD925C985E90B. Final screenshot inspected after fixing line-matrix composition, so rope segments and nodes remain aligned under phone aspect scaling.
- Actual iOS export, device-specific signed Xcode build, codesign verification, device installation and launch PASS. No Core runtime changes; prior core/Python results remain applicable. User device feel/readability/thermal feedback pending.
- Independent review passed after tuning overlay guards. One interrupted/overlapping validation attempt left a stale licensing connection; stopped only the owned batch editor/helper and reran serial validation successfully. No user player process was terminated for validation. Generated platform-default metadata discarded after export.

Phone control-strip/tuning/parry trial verified on2026-10-20:

- Full core suite PASS,240 fuel rounds/683,400 ticks and240 legacy rounds/223,598 ticks, replay/fuzz. Added trial coverage for radius6 contact beyond prior reach, unchanged one-third boost refund/no posture healing, contact-only safety expiry and lethal return. Core runtime defaults preserved.
- Unity validation/macOS build and40/40 EditMode tests PASS. Added control-strip safe-area separation, first-touch origin/finger ownership, drag-out/release/cancel/menu clearing, simultaneous actions, actual match-complete restart target, applied radius6/window0.16 and high-density tuning geometry/readability.
- Desktop, strip and tuning native smokes PASS900 steps/3 callbacks; same checksumA8EAD925C985E90B. Captured extra reach3.5/body2.5/window0.16. Final strip and large tuning screenshots inspected, including explicit movement-state buttons and X close label.
- Independent source review passed after preserving true joystick origin by clipping and suppressing manual scroll when IMGUI owns the pointer. Actual phone gesture routing/slider scrolling remains a human validation item; synthetic touches cannot prove it.
- Final iOS export/device-specific Xcode build/codesign verification/install/launch PASS on paired iPhone17 Pro Max. Updated trial is running; subjective approval and sustained performance feedback remain pending.

Phone zone/aspect iteration verified on2026-10-21:

- Unity validation/macOS build and59/59 EditMode tests PASS. New tests cover exact50/70/75/80/100% boundaries, invisible upper action-region taps/rejections, combo press/hold/release across multiple fingers, quick taps/cancel/menu clearing, uniform world/character/projectile scales, and scenery UV aspect/bottom alignment. Fixed subpixel rounding at the inclusive80% combo edge.
- Desktop/touch/tuning native smokes PASS900 steps/3 callbacks, checksumA8EAD925C985E90B for all. Final mobile and tuning screenshots inspected; fighters/projectiles retain proportions and the strip is shorter. Core gameplay is unchanged; prior full core suite remains applicable.
- Narrow independent static review PASS for hit-region math, shared-finger ownership/press edges and uniform rendering. Root integration review restored unchanged desktop scenery rendering and persistent black strip clearing. Larger review attempts hit a model usage limit; the bounded review completed with an available smaller model. Actual phone zone/gesture feel still requires user feedback.
- Final Unity iOS export and generic automatic-signing iPhoneOS build PASS, codesign verification and paired-device provisioning membership PASS. Direct Xcode destination was temporarily missing although devicectl reports the phone ready; installation/launch proceeds through devicectl. Installation and launch PASS. The initial Locked response was resolved by user unlocking the phone; devicectl successfully launched the updated app.

Wider phone arena/rotation/downward aiming verified on2026-10-21:

- Full core suite PASS, including new downward air ignition, up→down steering and floor clamp/active-state regressions. Existing240 fuel rounds/683,400 ticks and240 legacy rounds/223,598 ticks PASS. New wide suite PASS32 rounds each: width125/179,545 ticks;150/177,580;180/225,843, with repeated-input checksums, snapshot resume, actual spawn/divider/wall bounds and bot targeting. Longer width180 rounds required raising the stress-run tick budget; no invariant failed.
- Unity validation/macOS build and62/62 EditMode tests PASS. Tests cover actual mobile width/uniform world-edge projection/reset retention, captured dimensions, geometry-change input clearing and safe-area downward joystick+boost integration. A short4-pixel safe-area drag stays neutral for walking but aims down during boost. Explicit boost fuel initialization is required for movement integration.
- Desktop native900-step/3-callback smoke PASS checksumA8EAD925C985E90B. Wide phone/tuning native profiles PASS900 steps/3 callbacks, both checksum8A394471B952BE6D at applied141.8999×56.25. Different dimensions legitimately produce different state checksums. Mobile screenshot inspected, no internal side borders/distortion.
- Independent source review PASS for width flow through core/bot/rendering, landscape flags and touch dead-zone fix. Export/compile/signature/install/launch PASS. Read back this app's own phone tuning file:129.5681×56.25, body2.5, parry reach3.5, fuel mode retained. Physical landscape-flip feel and downward aiming feedback remain pending; no unlimited boost implementation.

Managed2× boost trial verified on2026-10-21:

- Full core suite PASS. New trial runs actual movement/combat phases at configured60Hz: one-second powered hold past old budget, exact burn/ignition, eventual exhaustion/release latch, expired short invulnerability, delayed0.9 refill over3s and1.8 over6s, held input cannot reignite, fresh release/press works. Existing240 fuel/240 legacy bot rounds remain PASS.
- Wide stress trials now use1.8 capacity:125-width32 rounds/178,331 ticks;150/32/117,639;180/32/154,716; replay/bounds checks PASS.96 rounds/450,686 ticks total.
- Unity validation/macOS build and63/63 EditMode tests PASS. Applied tank1.8, rates/delay/ignition/invulnerability/parry timing unchanged, both reset tanks1.8, capture1.8, exact capped0.6 refunds. Refund tests isolate passive regeneration from the reward.
- Desktop native900-step smoke PASS checksumB14368ED060D390C; touch/tuning profiles each PASS900 steps/3 callbacks at width141.8999, checksum8547B1A86A2798FF. Captures show1.8 capacity and0.3/s refill. Independent final code/test review passed after fixtures were aligned with actual fixed-step phase/timer ordering.
- Actual iOS export/signed device build/codesign verification/install PASS. Launch blocked by iOS Locked; unlock/open and fresh phone tuning readback pending. Physical resource-management feel still needs user playtest.

Swipe-parry controls verified on2026-10-22:

- Unity validation,76/76 EditMode tests and macOS compilation PASS. New tests cover boost without automatic defense, fast/slow/jitter gestures, one-pulse behavior, quiet rearm, late hold, Ended/cancel/menu clearing, multi-finger ownership and safe-area boundaries. Full MatchSimulation contact test proves actual PerfectParry after boost invulnerability ends, continued thrust, unchanged ignition sequence and projectile power increase.
- Full core suite PASS:240 fuel rounds/683,400 ticks,240 legacy rounds/223,598 ticks,96 wide-arena rounds/450,686 ticks and replay/bounds checks.
- Desktop, touch and tuning native smokes PASS900 ticks/3 callbacks. Touch/tuning checksum8547B1A86A2798FF; capture retains1.8-second tank and identifies boost-swipe-right-guard. Phone layout screenshot inspected. Independent read-only review passed after closing the full-simulation test gap. Physical gesture feel remains a human playtest.
- iOS export, signed Xcode build, signature verification, install and launch PASS. Readback from this app on iPhone17 Pro Max confirms new touchScheme,1.8-second tank and129.5681×56.25 arena.

1.5× initial projectile speed trial (2026-10-22): Unity validation,76/76 EditMode tests and macOS build PASS. Applied capture assertions verify36/72/84/96; independent review verifies spawn/parry/decay use the adjusted tuning. Native desktop smoke PASS900 ticks/3 callbacks. Core defaults and simulation algorithms unchanged; prior full core results remain applicable.
Phone-profile native smoke PASS900 ticks/3 callbacks, checksum03EF15438A58E0D9 and captured36/72/84/96. iOS export/sign/build/install PASS; launch blocked by phone Locked. New device speed readback pending launch.

Directional boost swipe (2026-10-22): Unity validation/macOS build and85/85 EditMode tests PASS. Normalized directional aim, stationary-stick persistence, cumulative and same-tick stick takeover, raw takeover, new swipe, final Ended aim buffering, cancel/menu, boost source ownership and actual full-simulation parry direction covered. Independent review PASS after fixing pending aim on takeover. Native desktop and phone smokes PASS900 ticks/3 callbacks; phone checksum03EF15438A58E0D9, directional touchScheme and36/72/84/96 captures verified. Core algorithms/defaults unchanged.

Latest directional build: iOS export, signed Xcode build, signature verification, install and launch PASS. App-only tuning readback on iPhone17 Pro Max confirms touchScheme=directional-boost-swipe-right-guard, tank1.8 and projectile36/72/84/96. This supersedes the previous speed-only build's locked-launch checkpoint.

Downward touch correction (2026-10-22): Unity validation,88/88 EditMode tests and macOS build PASS. New regressions exercise mobile follow center after long upward drag, downward airborne movement from the followed center, near-bottom30px down flick overriding an opposing stick, quiet rearm, duplicate suppression, jitter/slow drag rejection and unchanged interior swipe threshold. Existing desktop/input ownership tests retained; mobile origin expectations reflect approved following behavior. Independent review PASS. Desktop900-tick/3-callback native smoke PASS; core algorithms/defaults unchanged. Physical feel remains for user playtest.

Latest downward-fix phone-profile native smoke PASS900 ticks/3 callbacks, checksum03EF15438A58E0D9; capture identifies edge-aware-directional-swipe-follow-stick with tank1.8 and speeds36/72/84/96. Unity iOS export, signed Xcode build and signature verification PASS. Two installation attempts timed out after60s each, and a15s device-info query also timed out although paired-device discovery lists available. User confirms unlocked and connected wirelessly; no USB assumed. Wireless transport needs reconnection before installation can finish. Previous launched-app/readback results belong to the preceding directional build.

Wireless reconnect resolved the installation blocker (2026-10-22). Latest downward-control build installed and launched successfully. Readback from this app on iPhone17 Pro Max verifies edge-aware-directional-swipe-follow-stick, boost tank1.8 and projectile36/72/84/96. Physical downward swipe and long-drag reversal feel now await playtest.
