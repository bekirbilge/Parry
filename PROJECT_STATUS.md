# Project status

Updated: 2026-10-22 (Europe/Istanbul)
Milestone: downward touch fixes installed; phone playtest pending
Branch: fix/downward-boost-steering
Remote: https://github.com/bekirbilge/Parry.git

## Current build

Unity 6000.3.26f1 LTS; macOS arm64; .NET 10.0.101; Xcode 27.1/iPhoneOS 27.1 SDK. Development app `com.bekirbilge.parry.playtest` (ElasticArena) is installed and launched on the paired iPhone 17 Pro Max, iOS 26.6.2. Developer Mode and developer-profile trust are enabled. Personal Team provisioning expires 2026-10-27.

Wireless installation initially timed out; reconnecting iPhone Wi-Fi resolved it. Latest installation, launch and app-only tuning readback succeeded. No current installation blocker. Reconnect Wi-Fi and keep the phone unlocked if device services stall again.

## Playable configuration

- Continuous boost: 1.8-second tank, 0.04 ignition cost, 0.3/s regeneration after 0.35 seconds, perfect-parry refund one-third (0.6 seconds). Exhaustion or rope recoil requires release before reignition. Legacy three-charge comparison remains available with F1.
- Boost speed: horizontal/downward 80, upward cap 40, steering acceleration 360. Neutral boost rises. Reversing full upward velocity takes about 0.12 seconds to stop before descending; acceleration is unchanged.
- Projectile speeds: normal/spawn 36, powered 72 / 84 / 96. One power level decays every four seconds; speed eases at 12 units/s². Radius 0.9; projectiles persist until round end.
- Fighter body radius 2.5, parry radius 6 (extra reach 3.5), projectile contact radius 6.9. Parry window 0.16 seconds; no posture healing. Parry protection ends on projectile separation.
- Best of three; double KO draws award no point. Elastic rope stretches into the opponent's side before recoil. Core fixed-step state remains snapshot version 7.

## Current phone controls

Left 0–50% starts a floating MOVE joystick; its center follows drags beyond one travel radius. Middle-right 50–90% holds BOOST; a quick swipe supplies boost/parry aim and one timed parry pulse. Rightmost 90–100% taps for parry or holds guard. BOTH is removed. A stationary left thumb preserves swipe aim; deliberate cumulative stick movement takes steering back. A final lift-flick preserves aim for its buffered defense tick.

Visible strip: bottom 14% of safe-area height. Right actions also accept invisible touches over the bottom 25%; MOVE starts only in the visible strip. Fingers retain their initial roles until release/cancel. Normal swipe threshold is 3.5% of screen height, clamped to 12–96 pixels, minimum speed one screen-height/second, recognition window 0.18 seconds and quiet rearm 0.12 seconds. Downward-dominant swipes near the physical bottom shorten that distance to `min(normal, max(12px, 1.5 × slop, anchorY / 2))`, with proportional speed threshold. This retains jitter and slow-drag rejection.

Combat fills the upper 86% with uniform proportions. Phone arena width fits the viewport at fixed height 56.25, minimum width 100; verified phone dimensions are 129.5681 × 56.25. Desktop width stays 100. Match dimensions remain fixed through landscape flips; geometry changes clear held input. Both landscape orientations are enabled. Top-right PAUSE opens Resume, Mode, Tuning and Restart; tuning supports touch scrolling. Match-complete tap-to-restart is implemented.

## Verified checkpoint

- Unity validation, 88/88 EditMode tests, macOS build and independent source/test review passed. Tests include edge down-swipes, long joystick drags, airborne downward boost, directional swipe parries during boost, ownership, takeover, cancel/menu clearing and buffered release flicks.
- Native desktop and phone-profile smoke runs passed 900 simulation ticks and at least three live driver callbacks. Latest phone-profile checksum: `03EF15438A58E0D9`.
- iOS export, signed Xcode build, signature verification, wireless install and launch passed. Device readback confirms `touchScheme=edge-aware-directional-swipe-follow-stick`, tank 1.8 and projectile speeds 36 / 72 / 84 / 96.
- Last full core suite passed 240 fuel rounds / 683,400 ticks, 240 legacy rounds / 223,598 ticks and 96 wide-arena rounds / 450,686 ticks, plus replay/bounds checks. Latest fixes change presentation input only; core algorithms/defaults are unchanged. Core baseline tuning intentionally differs from applied playable tuning.

Artifacts: `artifacts/build/ElasticArena.app`, `artifacts/build/ios/Unity-iPhone.xcodeproj`, `artifacts/build/ios-derived/Build/Products/Debug-iphoneos/ElasticArena.app`, `artifacts/test-results/editmode.xml`, `artifacts/runtime-touch/smoke.png`, `artifacts/device/tuning.json`. Logs are under `artifacts/logs/`; build artifacts are local, not committed. See TEST_STATUS.md for test history and PLAYTEST_NOTES.md for controls/run instructions.

## Next checkpoint

Ask for physical feedback on downward edge swipes, long-drag reversal, stick/swipe takeover, accidental parries and the narrow guard strip. Also check both landscape rotations, full-width wall bounces, readability and boost management. Physical feel is pending; automated checks do not approve it.

P0/P1/P2 are complete and gameplay Gate A passed on 2026-10-20. Direction C (dark, high-contrast pixel art) is selected; the single static ninja/Rookie Dojo references still await visual Gate B. Resume P3 art/animation work after current phone feedback and reference approval. No bulk assets, networking, economy or release work is approved by this checkpoint. Preserve the existing core rules and explicit user tuning.
