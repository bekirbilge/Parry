# Gameplay playtest

Human gameplay approval: NOT GIVEN. Stay at Gate A until explicit approval; these are feedback iterations, not permission for production work.

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

Neutral-stick boost goes straight up; aiming boosts immediately at the exact normalized stick angle, without eight-way snapping. Fuel mode lets you steer continuously while holding boost; release to coast. F1 switches to the previous three-charge mode, where each burst retains its launch direction. There is no separate jump button; Triangle is unassigned. Xbox equivalents: A boost, X defense. Fuel mode starts with a 0.9-second tank. Thrust burns fuel; regeneration starts after a 0.35-second pause and fills an empty tank in three seconds. Enemy perfect parry restores one-third tank. Exhaustion or rope recoil requires releasing boost before another ignition. The comparison mode has three charges, sequential one-second recharge, and one charge restored per enemy perfect parry. The debug bot uses these same movement actions.

Touch: MOVE joystick plus BOOST and DEF. F4 shows mouse-testable touch controls on desktop. Physical gamepad and mobile behavior still need testing; virtual-device integration is covered automatically.

Debug: B bot/local, R restart, P pause, N step paused, F1 fuel/charges (restarts match), F5 tuning, F2 quarter speed, F3 collisions/FPS, F12 capture. macOS may require Fn for function keys. Tuning changes apply on Apply / reset match. Immediate test start is enabled; movement works while the first ball's three-second warning runs. The F5 tuning panel can restore normal countdown.

F12 saves a PNG and applied `tuning.json` in the chosen output folder; the tuning panel shows the path. Captures include boost/load timings, invulnerability, projectile decay/speeds and parry window.

## Latest changes to evaluate

- Fuel boost lasts while held and supplied, with continuous steering. Ignition costs 0.04 seconds of fuel; invulnerability remains 0.08 seconds with a 0.35-second rearm cooldown. The legacy comparison uses a 0.30 s powered directional burst with upward speed capped at40 (horizontal/downward80), with only 0.08 s invulnerability. A rope contact loads for ~0.20 s before recoil. Fixed anchors and movable floor/ceiling hinges allow real forward stretch without crossing the rope; maximum bend remains eight units.
- Parry radius is now3 units (fighter body1.5); its visible halo matches collision. The parry window stays0.16 s, recovery timings stay unchanged, and it works during boost, rope loading and recoil. Hold defense for guard; a fresh press is needed for another parry. Parry never heals posture.
- Charged projectile speeds are 72 / 84 / 96 units/s versus normal 24. Each power level lasts four seconds before losing one level; a perfect parry resets that timer. Speed now eases toward the next level at12 units/s² rather than snapping. In particular,72→24 takes four additional seconds after the level1→0 threshold; critical power reaches normal after12 seconds and normal speed after16 seconds without another parry. Balls are50% larger in diameter (radius0.9), with visuals matching collision. F5 exposes size and slowdown.
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
