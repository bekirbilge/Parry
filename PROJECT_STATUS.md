# Project status

Updated: 2026-10-30 (Europe/Istanbul)
Milestone: animated ninja and floor-fire trial; physical review pending
Branch: playtest/animated-ninja-floor-fire
Remote: https://github.com/bekirbilge/Parry.git

## Current build

Unity 6000.3.26f1 LTS; macOS arm64; .NET 10.0.101; Xcode 27.1/iPhoneOS 27.1 SDK. Development app `com.bekirbilge.parry.playtest` (ElasticArena) is installed and launched on the paired iPhone 17 Pro Max, iOS 26.6.2. Developer Mode and developer-profile trust are enabled. Renewed Personal Team provisioning expires 2026-11-06; user trusted the developer profile and confirmed the animation build opens.

Wireless installation initially timed out; reconnecting iPhone Wi-Fi resolved it. Latest installation, launch and app-only tuning readback succeeded. No current installation blocker. Reconnect Wi-Fi and keep the phone unlocked if device services stall again.

## Playable configuration

- Continuous boost: 1.8-second tank, 0.04 ignition cost, 0.3/s regeneration after 0.35 seconds, perfect-parry refund one-third (0.6 seconds). Exhaustion or rope recoil requires release before reignition. Legacy three-charge comparison remains available with F1.
- Boost speed: horizontal/downward 80, upward cap 40, steering acceleration 360. Neutral boost rises. Reversing full upward velocity takes about 0.12 seconds to stop before descending; acceleration is unchanged.
- Projectile speeds: normal/spawn 36, powered 72 / 84 / 96. One power level decays every four seconds; speed eases at 12 units/s². Radius 0.9; projectiles persist until round end.
- Fighter body radius 2.5, parry radius 6 (extra reach 3.5), projectile contact radius 6.9. Parry window 0.16 seconds; no posture healing. Parry protection ends on projectile separation.
- Best of three; double KO draws award no point. Elastic rope stretches into the opponent's side before recoil. Core fixed-step state remains snapshot version 8.

## Current phone controls

Left 0–50% starts a floating MOVE joystick; its center follows drags beyond one travel radius. Middle-right 50–90% holds BOOST; a quick swipe supplies boost/parry aim and one timed parry pulse. Rightmost 90–100% taps for parry or holds guard. BOTH is removed. A stationary left thumb preserves swipe aim; deliberate cumulative stick movement takes steering back. A final lift-flick preserves aim for its buffered defense tick.

Visible strip: bottom 14% of safe-area height. Right actions also accept invisible touches over the bottom 25%; MOVE starts only in the visible strip. Fingers retain their initial roles until release/cancel. Normal swipe threshold is 3.5% of screen height, clamped to 12–96 pixels, minimum speed one screen-height/second, recognition window 0.18 seconds and quiet rearm 0.12 seconds. Downward-dominant swipes near the physical bottom shorten that distance to `min(normal, max(12px, 1.5 × slop, anchorY / 2))`, with proportional speed threshold. This retains jitter and slow-drag rejection.

Combat fills the upper 86% with uniform proportions. Phone arena width fits the viewport at fixed height 56.25, minimum width 100; verified phone dimensions are 129.5681 × 56.25. Desktop width stays 100. Match dimensions remain fixed through landscape flips; geometry changes clear held input. Both landscape orientations are enabled. Top-right PAUSE opens Resume, Mode, Tuning and Restart; tuning supports touch scrolling. Match-complete tap-to-restart is implemented.

## Verified checkpoint

Unity validation/macOS build and109/109 EditMode tests pass. Reference/animation asset validation and19 Python tests pass. Full Core suite passes, including lethal-fire schedule/contact/round/draw/replay and existing480 bot rounds plus96 wide-arena rounds. Independent review passed after fixing cross-side fire overlap, circular corner clearance and final-active-tick rendering. Snapshot version8 includes hazard clock/hit ledger.

Signed iOS export/build/signature verification and wireless installation pass. The first animation build opened after developer trust renewal and readback confirmed unchanged controls/tuning. The final lethal-fire build is installed but its launch was blocked by the phone lock. Final native checks passed after unlocking the Mac: desktop/phone profiles completed900 ticks/3 callbacks with checksums65F293DF0B895EC3 /3E38AC9276098557. Warning and active-fire captures were inspected. Phone launch/readback remains pending a fresh manual open after auto-lock; the older tuning file is not evidence for the final fire build.

Artifacts: artifacts/build/ElasticArena.app; artifacts/build/ios/Unity-iPhone.xcodeproj; artifacts/build/ios-derived/Build/Products/Debug-iphoneos/ElasticArena.app; artifacts/test-results/editmode.xml; artifacts/animation-preview/ninja-animation-sample.gif; artifacts/device/tuning.json. Local build artifacts are ignored; source/provenance and the sample GIF are tracked under art/animation. Logs live in artifacts/logs.

## Next checkpoint

User accepted the phone controls on2026-10-30 and approved one animated sample. Ask for visual feedback on idle/run/boost/guard/parry/recoil, frame grounding, team readability, parry sparks, trails and the matching swipe/tap boundary. Also check both landscape rotations, full-width wall bounces, readability and boost management. Animation/readability approval remains pending; automated checks do not approve it.

P0/P1/P2 are complete and gameplay Gate A passed on 2026-10-20. Direction C (dark, high-contrast pixel art) is selected; the first animated ninja and existing Rookie Dojo now await visual Gate B. Six pose groups use24 measured frames; ascent/fall and defeat reuse available poses. No bulk assets, networking, economy or release work is approved by this checkpoint. User explicitly approved lethal floor fire (two warning flashes, then elimination) on2026-10-30. The configurable deterministic trial passes automated validation; physical playtest is pending. Preserve the existing core rules and explicit user tuning.


## Current animation / floor-fire trial

One24-frame ninja sheet covers idle/run/boost/guard/parry/recoil, with measured global UV rectangles, common scale and sole grounding. Original source pixels are preserved. Tick-based animation and bounded parry sparks/projectile history leave simulation authoritative. Ascent/fall and defeat temporarily reuse available frames. Generated sample/metadata/provenance are under art/animation and Resources/Art; visual Gate B remains pending.

Swipe and tap parry retain identical radius6 and0.16s. The outer boundary now draws above every pose for clearer visual parity; no proposed radius8 increase was applied. Input regressions compare actual swipe/tap edge contacts.

Floor fire is enabled in the playable trial and disabled in Core defaults. After6s quiet, mirrored patches warn for2s and burn for1.25s; warning starts repeat every10s with rotating positions. Contact instantly eliminates through defense/invulnerability, with no posture or velocity change. Same-step double victims draw/replay with no points. Match reset/draw resets hazard clock and ledger. F7 or Pause → Tuning → Floor fire toggles and restarts for comparison.

Preview: artifacts/animation-preview/ninja-animation-sample.gif. Asset validation: python3 tools/assets/validate_animation.py; production mode rejects pending art approval. The user confirmed the first animation build opens after renewing developer trust. Final combined build passes109 Unity tests,19 asset tests, Core regressions and native captures; installed on iPhone. Manual open and fresh lethal-fire readback remain pending after auto-lock.
