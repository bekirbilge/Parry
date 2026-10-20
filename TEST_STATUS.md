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
