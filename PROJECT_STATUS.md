# Project status

Date: 2026-10-22 (Europe/Istanbul)
Milestone: downward touch reliability fixes
Branch: fix/downward-boost-steering (origin/main refreshed; prior tested work retained; no issue supplied)
Remote: https://github.com/bekirbilge/Parry.git

## Current playable configuration

Unity6000.3.26f1 LTS, macOS arm64, .NET10.0.101; Unity Personal active. iOS support installed. Xcode27.1/iPhoneOS27.1 SDK available. iPhone17 Pro Max paired, iOS26.6.2, Developer Mode enabled, local development identity/profile trusted. App bundle com.bekirbilge.parry.playtest, minimum iOS15, landscape orientations. Personal Team provisioning expires2026-10-27.

P0/P1/P2 complete; user passed gameplay Gate A. Fuel-based boost remains selected: tank1.8s (2× prior0.9), regeneration0.3/s after0.35s delay, perfect-parry refund one-third (0.6s). Display resource name is now BOOST; internal fuel/config/snapshot names preserved. F1 retains three-charge comparison. Fighter radius2.5; latest explicit feedback increases playable parry radius4→6 (extra reach3.5), projectile radius0.9 =>contact6.9. Window0.16s unchanged. Safety expires on projectile separation. Projectile speeds36/72/84/96, four-second thresholds, gradual12units/s² slowdown; snapshotversion7. GDD governs remaining rules.

## Phone feedback implementation

Upper86% of safe-area height is the combat viewport; lower14% is the visible control strip. Phone world width now derives from viewport aspect at match reset, with height56.25 and minimum width100. Full authoritative combat fills the phone viewport uniformly; internal side rails/padding are removed. Actual phone capture verifies width129.5681/height56.25. Desktop width stays100. Match dimensions remain fixed through landscape flips; geometry changes clear held input without restarting. Future networking must share one authoritative arena configuration.

Mobile control trial (2026-10-22): the visible14% strip is full width. Left0–50% starts floating MOVE;50–90% holds BOOST, with a quick swipe producing one parry pulse;90–100% taps for parry or holds guard. There is no BOTH action. BOOST and PARRY/GUARD also accept touches in the invisible bottom25% safe-area margin; MOVE starts only in the visible strip. Touch roles stay fixed until release/cancel, and the left stick normally aims. A recognized swipe supplies normalized boost/parry aim until deliberate stick movement, another swipe or boost release. Final lift-flick aim survives one buffered defense tick. Slow cumulative stick movement takes control back; stationary/jittering fingers do not. Swipe tuning is3.5% of screen height (clamped12–96 px), minimum speed1 screen-height/s,0.18 s recognition window, and0.12 s quiet rearm. Tank1.8 s is unchanged. Implemented and validated below.

Boost touch aiming accepts deliberate drags over3 pixels when that is smaller than the walking dead zone. This fixes short downward drags falling back to neutral/up; walking and raw gamepad/keyboard dead zones stay unchanged. Downward boost remains blocked by the floor when grounded. User subsequently reported both joystick and swipe downward-control trouble; approved fixes make the mobile center follow long drags and shorten downward swipe distance near the physical edge. Mobile runtime now uses AutoRotation with both landscape flags enabled and portrait disabled. Physical left/right flip feedback remains pending.

Only top-right PAUSE during play. Modal RESUME/MODE/TUNING/RESTART clears held input and blocks gameplay. Restart resumes tick0. Match-complete TAP TO RESTART now has a shared visible hit target. Phone tuning uses large text/sliders/state buttons, touch scroll and fixed BACK/APPLY controls; slider/button pointer ownership suppresses manual scroll. Tuning spans the control-strip area while paused. --parry-tuning-preview supports native screenshot checks.

## Validation and artifacts

Core suite PASS including expanded parry range, exact refund/no posture healing, separation safety expiry and lethal return.240 fuel rounds/683,400 ticks and240 legacy rounds/223,598 ticks plus replay/fuzz PASS. Unity scene validation/macOS build and76/76 EditMode tests PASS. Additional wide-arena suite now exercises1.8-second capacity: PASS96 rounds/450,686 ticks across widths125/150/180, replay/resume, bounds and bot targeting. New trial tests prove powered flight past the old budget, exhaustion/release latch, unchanged short invulnerability, delayed half refill in3s and full refill in6s, exact capped0.6 parry refunds, both-player reset and captured1.8 capacity. Legacy core defaults remain0.9 for baseline tests; applied playable configuration overrides only tank capacity.

Previous swipe-trial desktop native900-step smoke checksumB14368ED060D390C; previous wide touch/tuning previews both PASS900 steps/3 callbacks, checksum8547B1A86A2798FF at width141.8999/height56.25. Native captures verify tank1.8 and refill0.3. Independent source/test review passed. Full-width arena, speed, parry timing and invulnerability remain unchanged. New swipe controls passed input and full-simulation projectile-contact regressions, including after boost invulnerability expires. Independent review found no remaining material issue. Native touch/tuning captures identify touchScheme=boost-swipe-right-guard.

macOS app: artifacts/build/ElasticArena.app
Xcode project: artifacts/build/ios/Unity-iPhone.xcodeproj
Device app: artifacts/build/ios-derived/Build/Products/Debug-iphoneos/ElasticArena.app
Previews: artifacts/runtime-touch/smoke.png, artifacts/runtime-tuning/smoke.png
Test report: artifacts/test-results/editmode.xml. Run/control instructions: PLAYTEST_NOTES.md.

Previous swipe trial iOS export, signed Xcode build, signature verification, device installation and launch PASS. Actual phone tuning readback confirms touchScheme=boost-swipe-right-guard, tank1.8 and arena129.5681×56.25. Logs: artifacts/logs/xcode-ios-generic-build.log, iphone-install.log, iphone-launch.log, iphone-tuning-copy.log.


## Next human gate

Test faster opening shots and36-speed decay floor, full-width wall bounces, both landscape flips during a match, invisible action margins, directional swipe parries during boost, stick takeover, lift-flicks, accidental swipe frequency and the far-right guard strip. Downward boost is now reported working; no further investigation requested. User chose2× boost capacity rather than unlimited. Judge whether longer flight still requires management: empty-to-full takes6s plus0.35s delay, and each enemy perfect parry restores0.6s. Keep this finite trial while awaiting feedback. Actual phone feel/readability/thermals await feedback. User chose dark high-contrast art directionC; current single static ninja and Rookie Dojo references await visual Gate B. No animation set or bulk art approved. Preserve gameplay except explicit tuning feedback; defer networking/economy/release. No App Store/TestFlight authorization or paid service needed here.

2026-10-22 speed trial: applied normal/spawn speed24→36; powered72/84/96 unchanged through adjusted multipliers. Window, boost, controls,4-second decay thresholds and12units/s² slowdown unchanged. Unity validation/macOS build and76/76 tests PASS, including effective speed captures. Native desktop smoke PASS900 steps/3 callbacks. Independent review PASS. Core defaults unchanged; full core suite from the preceding swipe milestone remains applicable.

Latest speed-trial phone preview PASS900 ticks/3 callbacks, checksum03EF15438A58E0D9, capture speeds36/72/84/96. iOS export, signed Xcode build, signature verification and installation PASS. Launch blocked by iOS Locked; unlock and open ElasticArena. Fresh on-device speed readback awaits launch; prior readback belongs to the swipe build.

Directional swipe trial (2026-10-22): Unity validation/macOS build and85/85 EditMode tests PASS. Tests cover opposite stick/swipe directions, normalized cardinal/diagonal/down aim, persistent hold, cumulative stick takeover including before a fixed tick, raw aim takeover, rearmed redirect, final lift-flick, release/cancel/menu clearing and actual projectile parry with directional outgoing velocity during boost. Independent review PASS after correcting buffered-aim takeover. Native desktop and phone-profile smokes PASS900 steps/3 callbacks; phone checksum03EF15438A58E0D9, capture touchScheme=directional-boost-swipe-right-guard, speeds36/72/84/96. Updated label screenshot inspected. Core simulation unchanged; previous core suite remains applicable. Physical direction/gesture feel awaits playtest.

Latest directional build: iOS export, signed Xcode build, signature verification, install and launch PASS. App-only tuning readback on iPhone17 Pro Max confirms touchScheme=directional-boost-swipe-right-guard, tank1.8 and projectile36/72/84/96. This supersedes the previous speed-only build's locked-launch checkpoint.

Downward-control fix (2026-10-22): mobile touch/mouse joystick center follows overflow beyond one travel radius; desktop remains anchored. Downward-dominant swipes near the physical bottom use min(normalDistance,max(12px,1.5×slop,anchorY/2)), with proportional minimum speed. Interior thresholds and quiet rearm unchanged. Unity validation/macOS build and88/88 EditMode tests PASS. Regressions cover30px near-bottom swipe with opposing stick, rearm/no duplicate pulse, jitter/slow drag rejection, unchanged interior threshold, ownership and long-drag downward airborne movement. Independent review PASS. Native desktop smoke PASS900 ticks/3 callbacks. Core simulation and approved speed/boost tuning unchanged; prior core results remain applicable.

Latest downward-fix phone-profile native smoke PASS900 ticks/3 callbacks, checksum03EF15438A58E0D9; capture identifies edge-aware-directional-swipe-follow-stick with tank1.8 and speeds36/72/84/96. Unity iOS export, signed Xcode build and signature verification PASS. Two installation attempts timed out after60s each, and a15s device-info query also timed out although paired-device discovery lists available. User confirms unlocked and connected wirelessly; no USB assumed. Wireless transport needs reconnection before installation can finish. Previous launched-app/readback results belong to the preceding directional build.
