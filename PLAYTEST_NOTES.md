# Gameplay playtest

Human gameplay approval: GIVEN on2026-10-20. User accepted the current fuel-based version after projectile size/slowdown and contact-only safety feedback. Gate A passed; art direction/reference approval is the next human checkpoint.

## Run

Close any older player instance, then launch:

```sh
open /Users/bekir/Code/Parry/artifacts/build/ElasticArena.app --args --parry-output /Users/bekir/Code/Parry/artifacts/playtest
```

Default: blue P1 versus Easy debug bot. B toggles local two-player. Editor: Unity 6000.3.26f1 LTS, `Assets/Game/Unity/Scenes/Bootstrap.unity`, Play.

## Current controls

| Action | PlayStation P1 | Keyboard P1 | Keyboard P2 |
|---|---|---|---|
| Move / aim boost and parry | Left stick | WASD | Arrows |
| Boost | X/Cross | Shift or Space | I or U |
| Tap parry / hold guard | Square | E | O |

Neutral-stick boost goes straight up; aiming boosts immediately at the exact normalized stick angle, without eight-way snapping. Fuel mode lets you steer continuously while holding boost; release to coast. F1 switches to the previous three-charge mode, where each burst retains its launch direction. There is no separate jump button; Triangle is unassigned. Xbox equivalents: A boost, X defense. The current managed2× trial starts with a 1.8-second boost tank. Thrust burns fuel; regeneration starts after a 0.35-second pause and fills an empty tank in six seconds. Enemy perfect parry restores one-third tank. Exhaustion or rope recoil requires releasing boost before another ignition. The comparison mode has three charges, sequential one-second recharge, and one charge restored per enemy perfect parry. The debug bot uses these same movement actions.

Touch: floating MOVE joystick in the left half of the black control strip, plus BOOST and DEF on the right. F4 shows mouse-testable touch controls on desktop. Physical gamepad and mobile behavior still need testing; virtual-device integration is covered automatically.

Debug: B bot/local, R restart, P pause, N step paused, F1 fuel/charges (restarts match), F5 tuning, F2 quarter speed, F3 collisions/FPS, F6 reference art on/off, F12 capture. macOS may require Fn for function keys. Tuning changes apply on Apply / reset match. Immediate test start is enabled; movement works while the first ball's three-second warning runs. The F5 tuning panel can restore normal countdown.

F12 saves a PNG and applied `tuning.json` in the chosen output folder; the tuning panel shows the path. Captures include boost/load timings, invulnerability, projectile decay/speeds and parry window.

## Latest changes to evaluate

- Fuel boost lasts while held and supplied, with continuous steering. Ignition costs 0.04 seconds of fuel; invulnerability remains 0.08 seconds with a 0.35-second rearm cooldown. The legacy comparison uses a 0.30 s powered directional burst with upward speed capped at40 (horizontal/downward80), with only 0.08 s invulnerability. A rope contact loads for ~0.20 s before recoil. Fixed anchors and movable floor/ceiling hinges allow real forward stretch without crossing the rope; maximum bend remains eight units.
- Playable parry radius is now6 units (fighter body2.5; character diameter5 versus the previous3); its visible halo matches collision. The parry window stays0.16 s, recovery timings stay unchanged, and it works during boost, rope loading and recoil. Hold defense for guard; a fresh press is needed for another parry. Parry never heals posture.
- Charged projectile speeds are 72 / 84 / 96 units/s versus normal 24. Each power level lasts four seconds before losing one level; a perfect parry resets that timer. Speed now eases toward the next level at12 units/s² rather than snapping. In particular,72→24 takes four additional seconds after the level1→0 threshold; critical power reaches normal after12 seconds and normal speed after16 seconds without another parry. Balls are50% larger in diameter (radius0.9), with visuals matching collision. F5 exposes projectile size and slowdown, plus Fighter radius for the shared character/body size.
- Parry now protects only the immediate contact with that projectile, so passing through it is safe. Once you separate beyond the parry contact area, it can hurt you again on return, even without an opponent parry. The brief player-colored ring indicates contact protection. A new return parry gives normal power/resource rewards. Redirecting a still-protected contact gives no additional rewards. Other projectiles remain dangerous.

## Five-minute checklist

1. Hold boost upward from neutral and steer while holding. Release, check delayed refill, then exhaust the tank and release before restarting. Press F1 to compare with three charges; it resets the match.
2. Aim at shallow and steep angles to compare free-angle boosting. Press Cross + Square together, then tap Square midway through a boost. Compare parry timing while still, loading the rope, and recoiling.
3. Boost into the rope from close range on ground and in air. Expect forward penetration into the opponent's resting half before one rebound. Check diagonal vertical momentum and escape with an outward boost.
4. Parry while moving through a ball: the immediate overlap should be safe. Let it separate and return from a wall: it must threaten you again. Parry or guard the return; a fresh return parry should restore fuel/one charge normally. Check that another ball remains dangerous.
5. Judge charged speed, four-second decay, posture pressure, fairness of grazing hits, simultaneous K.O., first-to-two flow, and equal opposing boosts in local mode.

Report requested changes for boost control, rope penetration/recoil, parry ease, charged speed and immediate parry protection. Include any unfair-death example or captured tuning. Explicitly say when the core combat is fun enough to proceed.

## Feedback history

2026-10-19: user requested faster movement/higher jumps, immediate starts, elastic rope, three charges, and Cross boost/Square parry. Run is 24 units/s; later one-button boost supersedes the separate jump control.

2026-10-20: user requested rocket-like boost and deep rope loading, slower projectile decay, stronger charged speed, owner immunity, easier boost parry, and eliminating the jump button. User selected neutral-stick upward boost without tap/hold delay and last-parrier immunity until opponent parry. Original ownership choice was later superseded by contact-only safety; subjective approval remains outstanding.

Latest feedback iteration: user requested twice-faster parry deflections and continuous boost aim. User confirmed speed increases remain a perfect-parry reward, while owned shots can be deliberately redirected without power/charge bonuses. Automated validation and rebuild passed; these changes are ready for another feel test.

Validated area-only trial: compare grazing-shot parries during boost and after invulnerability expires. Boost invulnerability or immediate parry contact protection can allow harmless pass-through. Test the larger parry radius first; timing/recovery was deliberately not increased. Also compare the lower neutral upward boost height.

A close-range opponent return now clears stale contact suppression, so a later parry is not skipped because the projectile is still in the earlier overlap. Core tests,20/20 Unity tests and native smoke pass for this iteration.

Fuel trial authorized after pushing the tested three-charge build to GitHub. Compare sustained steering, fuel pressure, parry refunds and rope recoil against the old mode using F1. Trial values await human feedback; production approval remains outstanding.

Fuel trial validation complete: core replay/stress,23/23 Unity tests, macOS build and native smoke pass. Review fixes preserve the rope-impact body hit and required release through guard break. Please compare fuel steering and refill pressure against F1's three-charge mode, especially while parrying and loading/rebounding from the rope.

Latest request: test larger projectiles and gradual speed loss first, then compare the fuel boost trial with the F1 three-charge mode. Power/posture thresholds stay discrete, so power color can change before speed finishes easing down.

Latest feedback: permanent ownership immunity removed in favor of contact-only protection. Test fresh wall returns, boosted pass-through and repeated parries. User likes the three-charge movement; F1 retains that comparison. Close old game instances with Command-Q before opening a newly rebuilt app.

2026-10-20 selection: user confirmed the latest tested build used fuel and asked to keep it as-is. Fuel movement and current tuning are selected; larger projectiles, gradual slowdown and contact-only safety retained. Three charges remain available through F1 for comparison. Overall Gate A production approval has not been explicitly given.

Gate A approval (2026-10-20): “ok this is fine now, i think the game is at a good place. whats next??” Current fuel, projectile and parry-contact settings form the approved baseline. Proceed to planP3; visual choices are still pending.

P3 visual proof: directionC selected. The current sample adds one static ninja pose and the dark Rookie Dojo background. F6 switches back to graybox presentation without resetting simulation. Character foot placement uses measured transparent-padding metadata; gameplay/collision/rope/projectile behavior remains the approved baseline. Review character/background contrast, actual small-screen readability, team identification, grounded feet and boost/parry visibility. This is not an animation set. Reference approval is required before more art or animation production.

Mobile/readability feedback: characters are67% larger in both visual height and body collision, with feet still anchored. F5 Fighter radius ranges1.5–3.5 (default2.5). Check closer body contacts, projectile grazing/parry fairness, and rope stretch at the new size. Desktop and phone-sized-window previews are not physical phone tests.

## iPhone17 Pro Max installation

Unity iOS support is installed. The final landscape playtest exported and compiled successfully with Xcode, unsigned. The phone is now paired with Developer Mode enabled. A device-specific signed build is installed. User trusted the developer profile and confirms the game opens; device process listing confirms it running. Physical gameplay feedback remains pending.

1. Connect the iPhone by USB, unlock it and accept Trust This Computer.
2. Open the project:

```sh
open /Users/bekir/Code/Parry/artifacts/build/ios/Unity-iPhone.xcodeproj
```

3. In Xcode > Settings > Apple Accounts, sign in locally. Select the Unity-iPhone target > Signing & Capabilities, enable Automatically manage signing and choose your Personal Team. Use a unique bundle identifier if Xcode requests one.
4. Select the connected iPhone as the run destination. When prompted, enable Settings > Privacy & Security > Developer Mode on the phone, restart and confirm. If it is not visible yet, pair the phone with Xcode first.
5. Product > Run. If iOS blocks launch as untrusted, open Settings > General > VPN & Device Management > Developer App, select your developer account, and Trust or Allow & Restart as offered. Then open ElasticArena. Approve any device trust/developer prompts. A free Personal Team supports local testing; provisioning expires after seven days. No account password or certificates should be sent in chat.

Apple: [run on a device](https://developer.apple.com/documentation/Xcode/running-your-app-on-simulated-or-physical-devices), [Developer Mode](https://developer.apple.com/documentation/xcode/enabling-developer-mode-on-a-device), [Personal Team limits](https://developer.apple.com/help/account/basics/about-your-developer-account).

Play in landscape. Left MOVE joystick aims movement/boost/parry; hold BOOST for fuel thrust, neutral aim rises; tap PARRY to parry or hold to guard. Boost and defense can be held together with separate fingers. Only PAUSE appears at the top right during play. Tap it for RESUME, MODE (BOT/LOCAL), TUNING and RESTART. Restart closes the menu and resumes a fresh match. Default is P1 versus Easy bot. Combat uses a proportion-preserving view in the upper86% of the safe area; the lower14% is the visible control strip with solid BOOST/PARRY zones. Main HUD text scales for phones. TUNING now has large phone text/sliders, touch scrolling and fixed BACK/APPLY controls. Changes take effect on Apply / reset; Back returns to the pause menu.

First device checklist: try both landscape orientations, check notch/home-indicator clearance, judge the larger characters and projectile visibility, hold MOVE+BOOST+PARRY together, test release/refill, and watch for heat, stutter or missed touches over five minutes. Report touch comfort, readability and unfair misses before more art work. Physical-device behavior is not yet validated.

Re-export after source changes with `bash tools/test-mobile.sh ios`; generated Xcode signing settings may need selecting again. The compiled unsigned app is under artifacts/build/ios-derived/Build/Products/Debug-iphoneos. Desktop touch preview: launch the macOS app with `--parry-touch`, or press F4. Android helper remains available but its module/export/device run is untested.

Phone feedback iteration: full safe-area playfield replaces the letterboxed arena; the fixed world adapts horizontally to the phone aspect without cropping combat. Desktop layout is unchanged. Test both landscape orientations, Pause → Restart, Resume, and separate-finger MOVE+BOOST+PARRY. No other menu buttons should be visible during active play. Tuning must not trigger hidden menu buttons. Updated build has been installed and launched; subjective approval of these changes is pending.

Latest phone trial: place your left thumb anywhere in the left half of the black strip, then drag relative to that initial touch. The joystick follows that origin until you lift; a second finger cannot steal it. The controls stay below combat. Boost is the displayed resource name (internal captures still use fuel identifiers). Parry radius is50% larger (4→6), timing remains0.16s. After the match ends, tap the visible TAP TO RESTART button, or restart from Pause. Check that tuning sliders drag horizontally without scrolling the list, vertical drags between controls scroll, and BACK/APPLY remain accessible. Use `--parry-tuning-preview` with the desktop player for a tuning screenshot. Physical phone feedback on these changes is pending.

2026-10-21 zone trial supersedes the earlier stretched phone presentation and22% strip: combat now fits its true16:9 proportions within the available viewport, scenery fills the surrounding sides, and subtle rails show ricochet bounds. The visible strip is14% of safe height. MOVE starts anywhere in its left half. Right-side touch zones are nominal BOOST50–75% and PARRY75–100%, with BOTH70–80% taking priority. Those three action regions extend invisibly over the bottom25% of safe height, so pressing slightly above a visible zone still works. MOVE does not gain that invisible extension. Fingers retain their initial role while dragging; lift and press again to switch roles. Holding BOTH gives one parry attempt then guard plus normal boost consumption, not repeated perfect parries.

Test taps just above the strip at60%/75%/90% width, and taps above the25% margin which should do nothing. Test separate boost/parry fingers plus BOTH, releasing them in different orders. Compare projectile roundness and character proportions, wall-rail bounces, and whether the thinner strip is still comfortable.

2026-10-21 arena/rotation iteration supersedes the fitted16:9 phone world: the actual local-playtest world now widens to the phone viewport, with height56.25 and unchanged visual proportions. No internal side rails. Desktop stays width100; phone dimensions are fixed through the current match and recaptured on restart. Both landscape orientations are enabled, portrait remains disabled. Flip left/right during play: the match should continue and held input should clear. Actual phone capture verifies width129.5681.

Downward boost: place the left thumb, drag downward and hold BOOST/BOTH while airborne. Small boost-aim drags now register with a smaller dead zone (walking unchanged). Pressing down on the floor cannot move below it. Test steering an ongoing upward boost into a downward dive as well as a fresh downward boost. No unlimited boost change is included; keep the current meter while discussing a separate trial.

User follow-up: downward boost now seems to be working; skip further investigation. Keep the installed fixes and discuss unlimited boost separately.

2026-10-21 managed2× boost trial: user selected double capacity instead of unlimited boost. Tank0.9→1.8 seconds; burn rate,0.04 ignition cost,0.3/s refill and0.35s delay unchanged. A perfect parry still restores one-third, now0.6 seconds. Empty refill takes6 seconds after the delay, so spending the whole tank has a cost. Speed,0.08s invulnerability/0.35s rearm, parry timing and rope-recoil release rules are unchanged. Test longer steering/flight, exhaustion, release before reignition, and parry-assisted refill. Three-charge comparison remains F1; no unlimited mode added.

2026-10-22 swipe-parry control trial: use left0–50% for floating MOVE; hold BOOST in50–90%, and make a quick swipe there for one parry pulse. In90–100%, tap for parry or hold for guard. No BOTH action. The visible strip remains14% of safe height; right-side controls also accept touches in the invisible bottom25% safe-area margin. Touch roles lock until release/cancel; the left stick continuously aims. Swipe tuning is3.5% screen height (clamped12–96 px), minimum speed1 screen-height/s,0.18 s window,0.12 s quiet rearm. The1.8 s boost tank is unchanged. Trial feedback is pending.
