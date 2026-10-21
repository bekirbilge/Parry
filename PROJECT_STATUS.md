# Project status

Date: 2026-10-21 (Europe/Istanbul)
Milestone: shorter touch zones, invisible action margin and corrected graphic proportions
Branch: mobile/touch-zones-aspect (origin/main refreshed; prior tested work retained; no issue supplied)
Remote: https://github.com/bekirbilge/Parry.git

## Current playable configuration

Unity6000.3.26f1 LTS, macOS arm64, .NET10.0.101; Unity Personal active. iOS support installed. Xcode27.1/iPhoneOS27.1 SDK available. iPhone17 Pro Max paired, iOS26.6.2, Developer Mode enabled, local development identity/profile trusted. App bundle com.bekirbilge.parry.playtest, minimum iOS15, landscape orientations. Personal Team provisioning expires2026-10-27.

P0/P1/P2 complete; user passed gameplay Gate A. Fuel-based boost remains selected: tank0.9s, regeneration0.3/s after0.35s delay, perfect-parry refund one-third. Display resource name is now BOOST; internal fuel/config/snapshot names preserved. F1 retains three-charge comparison. Fighter radius2.5; latest explicit feedback increases playable parry radius4→6 (extra reach3.5), projectile radius0.9 =>contact6.9. Window0.16s unchanged. Safety expires on projectile separation. Projectile speeds24/72/84/96, four-second thresholds, gradual12units/s² slowdown; snapshotversion7. GDD governs remaining rules.

## Phone feedback implementation

Upper86% of safe-area height is the combat viewport; lower14% is the visible control strip. Full authoritative16:9 world fits uniformly inside the viewport; scenery fills the side space and phone insets with aspect-preserving crop, and subtle rails mark actual wall bounds. No horizontal stretch or cropped combat. Physics and desktop rendering remain unchanged.

Mobile controls are full-width zones: left0–50% floating MOVE, nominal50–75% BOOST and75–100% PARRY, overridden by shared70–80% BOTH. BOOST/PARRY/BOTH accept invisible touches across the bottom25% of the safe area; MOVE begins only in the visible14% strip. Initial roles lock until release/cancel. Aggregate finger ownership preserves held actions until all sources release and prevents duplicate press rewards.

Only top-right PAUSE during play. Modal RESUME/MODE/TUNING/RESTART clears held input and blocks gameplay. Restart resumes tick0. Match-complete TAP TO RESTART now has a shared visible hit target. Phone tuning uses large text/sliders/state buttons, touch scroll and fixed BACK/APPLY controls; slider/button pointer ownership suppresses manual scroll. Tuning spans the control-strip area while paused. --parry-tuning-preview supports native screenshot checks.

## Validation and artifacts

Core suite PASS including expanded parry range, exact refund/no posture healing, separation safety expiry and lethal return.240 fuel rounds/683,400 ticks and240 legacy rounds/223,598 ticks plus replay/fuzz PASS. Unity scene validation/macOS build and59/59 EditMode tests PASS. Desktop, touch-strip and tuning native smokes PASS900 steps/3 callbacks; same checksumA8EAD925C985E90B. Applied capture confirms extraReach3.5/body2.5/window0.16. Final strip/tuning screenshots inspected. Narrow independent static review passed for region boundaries, combo ownership and uniform rendering; root integration review restored unchanged desktop background rendering and persistent strip clearing. Physical zone feel and slider/scroll gestures still need feedback.

macOS app: artifacts/build/ElasticArena.app
Xcode project: artifacts/build/ios/Unity-iPhone.xcodeproj
Device app: artifacts/build/ios-derived/Build/Products/Debug-iphoneos/ElasticArena.app
Previews: artifacts/runtime-touch/smoke.png, artifacts/runtime-tuning/smoke.png
Test report: artifacts/test-results/editmode.xml. Run/control instructions: PLAYTEST_NOTES.md.

Final iOS export and generic signed iPhoneOS Xcode build PASS; signature and provisioning membership verified. Direct device destination was temporarily unavailable to Xcode even though devicectl reports the paired phone ready. Installation and launch PASS via devicectl. User unlocked the phone after the initial Locked response; the updated playtest launched successfully. Logs: artifacts/logs/xcode-ios-generic-build.log, iphone-install.log, iphone-launch.log.

## Next human gate

Test undistorted character/projectile graphics, shorter control strip, right-side presses just above the strip, BOTH70–80% activation, no accidental adjacent action, joystick and tuning gestures. Actual phone feel/readability/thermals await feedback. User chose dark high-contrast art directionC; current single static ninja and Rookie Dojo references await visual Gate B. No animation set or bulk art approved. Preserve gameplay except explicit tuning feedback; defer networking/economy/release. No App Store/TestFlight authorization or paid service needed here.
