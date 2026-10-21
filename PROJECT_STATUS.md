# Project status

Date: 2026-10-21 (Europe/Istanbul)
Milestone: managed double-capacity boost trial
Branch: playtest/double-boost-capacity (origin/main refreshed; prior tested work retained; no issue supplied)
Remote: https://github.com/bekirbilge/Parry.git

## Current playable configuration

Unity6000.3.26f1 LTS, macOS arm64, .NET10.0.101; Unity Personal active. iOS support installed. Xcode27.1/iPhoneOS27.1 SDK available. iPhone17 Pro Max paired, iOS26.6.2, Developer Mode enabled, local development identity/profile trusted. App bundle com.bekirbilge.parry.playtest, minimum iOS15, landscape orientations. Personal Team provisioning expires2026-10-27.

P0/P1/P2 complete; user passed gameplay Gate A. Fuel-based boost remains selected: tank1.8s (2× prior0.9), regeneration0.3/s after0.35s delay, perfect-parry refund one-third (0.6s). Display resource name is now BOOST; internal fuel/config/snapshot names preserved. F1 retains three-charge comparison. Fighter radius2.5; latest explicit feedback increases playable parry radius4→6 (extra reach3.5), projectile radius0.9 =>contact6.9. Window0.16s unchanged. Safety expires on projectile separation. Projectile speeds24/72/84/96, four-second thresholds, gradual12units/s² slowdown; snapshotversion7. GDD governs remaining rules.

## Phone feedback implementation

Upper86% of safe-area height is the combat viewport; lower14% is the visible control strip. Phone world width now derives from viewport aspect at match reset, with height56.25 and minimum width100. Full authoritative combat fills the phone viewport uniformly; internal side rails/padding are removed. Actual phone capture verifies width129.5681/height56.25. Desktop width stays100. Match dimensions remain fixed through landscape flips; geometry changes clear held input without restarting. Future networking must share one authoritative arena configuration.

Mobile controls are full-width zones: left0–50% floating MOVE, nominal50–75% BOOST and75–100% PARRY, overridden by shared70–80% BOTH. BOOST/PARRY/BOTH accept invisible touches across the bottom25% of the safe area; MOVE begins only in the visible14% strip. Initial roles lock until release/cancel. Aggregate finger ownership preserves held actions until all sources release and prevents duplicate press rewards.

Boost touch aiming accepts deliberate drags over3 pixels when that is smaller than the walking dead zone. This fixes short downward drags falling back to neutral/up; walking and raw gamepad/keyboard dead zones stay unchanged. Downward boost remains blocked by the floor when grounded. User now reports it working and asks to skip further investigation. Mobile runtime now uses AutoRotation with both landscape flags enabled and portrait disabled. Physical left/right flip feedback remains pending.

Only top-right PAUSE during play. Modal RESUME/MODE/TUNING/RESTART clears held input and blocks gameplay. Restart resumes tick0. Match-complete TAP TO RESTART now has a shared visible hit target. Phone tuning uses large text/sliders/state buttons, touch scroll and fixed BACK/APPLY controls; slider/button pointer ownership suppresses manual scroll. Tuning spans the control-strip area while paused. --parry-tuning-preview supports native screenshot checks.

## Validation and artifacts

Core suite PASS including expanded parry range, exact refund/no posture healing, separation safety expiry and lethal return.240 fuel rounds/683,400 ticks and240 legacy rounds/223,598 ticks plus replay/fuzz PASS. Unity scene validation/macOS build and63/63 EditMode tests PASS. Additional wide-arena suite now exercises1.8-second capacity: PASS96 rounds/450,686 ticks across widths125/150/180, replay/resume, bounds and bot targeting. New trial tests prove powered flight past the old budget, exhaustion/release latch, unchanged short invulnerability, delayed half refill in3s and full refill in6s, exact capped0.6 parry refunds, both-player reset and captured1.8 capacity. Legacy core defaults remain0.9 for baseline tests; applied playable configuration overrides only tank capacity.

Desktop native900-step smoke checksumB14368ED060D390C; wide touch/tuning previews both PASS900 steps/3 callbacks, checksum8547B1A86A2798FF at width141.8999/height56.25. Native captures verify tank1.8 and refill0.3. Independent source/test review passed. Full-width arena, input layout, speed, parry timing and invulnerability configuration remain unchanged.

macOS app: artifacts/build/ElasticArena.app
Xcode project: artifacts/build/ios/Unity-iPhone.xcodeproj
Device app: artifacts/build/ios-derived/Build/Products/Debug-iphoneos/ElasticArena.app
Previews: artifacts/runtime-touch/smoke.png, artifacts/runtime-tuning/smoke.png
Test report: artifacts/test-results/editmode.xml. Run/control instructions: PLAYTEST_NOTES.md.

Final2× trial iOS export, signed Xcode build, signature verification and device installation PASS. Launch was blocked only because iOS reports the phone Locked; user must unlock/open ElasticArena. Actual phone tuning readback for the new trial is pending launch (the existing device artifact predates this update). Logs: artifacts/logs/xcode-ios-generic-build.log, iphone-install.log, iphone-launch.log, iphone-tuning-copy.log.


## Next human gate

Test full-width wall bounces, both landscape flips during a match, invisible action margins and combo gestures. Downward boost is now reported working; no further investigation requested. User chose2× boost capacity rather than unlimited. Judge whether longer flight still requires management: empty-to-full takes6s plus0.35s delay, and each enemy perfect parry restores0.6s. Keep this finite trial while awaiting feedback. Actual phone feel/readability/thermals await feedback. User chose dark high-contrast art directionC; current single static ninja and Rookie Dojo references await visual Gate B. No animation set or bulk art approved. Preserve gameplay except explicit tuning feedback; defer networking/economy/release. No App Store/TestFlight authorization or paid service needed here.
