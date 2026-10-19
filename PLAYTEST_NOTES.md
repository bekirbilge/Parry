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

Neutral-stick boost goes straight up; aiming boosts immediately in that direction. There is no separate jump button; Triangle is unassigned. Xbox equivalents: A boost, X defense. Three charges, sequential one-second recharge; perfect parry restores one up to three. The debug bot uses these same movement actions.

Touch: MOVE joystick plus BOOST and DEF. F4 shows mouse-testable touch controls on desktop. Physical gamepad and mobile behavior still need testing; virtual-device integration is covered automatically.

Debug: B bot/local, R restart, P pause, N step paused, F1 tuning, F2 quarter speed, F3 collisions/FPS, F12 capture. macOS may require Fn for function keys. Tuning changes apply on Apply / reset match. Immediate test start is enabled; movement works while the first ball's three-second warning runs. F1 can restore normal countdown.

F12 saves a PNG and applied `tuning.json` in the chosen output folder; the tuning panel shows the path. Captures include boost/load timings, invulnerability, projectile decay/speeds and parry window.

## Latest changes to evaluate

- Boost is a 0.30 s powered directional burst, with only 0.08 s invulnerability. A rope contact loads for ~0.20 s before recoil. Fixed anchors and movable floor/ceiling hinges allow real forward stretch without crossing the rope; maximum bend remains eight units.
- Parry window is 0.16 s and works during boost, rope loading and recoil. Hold defense for guard; a fresh press is needed for another parry. Parry never heals posture.
- Charged projectile speeds are 36 / 42 / 48 units/s versus normal 24. Each power level lasts four seconds before losing one level; a perfect parry resets that timer. Critical can therefore take twelve seconds to fully return to normal without another parry.
- Only the last perfect parrier is immune to that projectile until the opponent perfect-parries it. An owned shot passes through its owner, with no posture damage or repeated parry benefits. Walls, ordinary guard and decay preserve ownership. The outer ring shows which player is immune; the core color shows power. Unclaimed shots threaten both players.

## Five-minute checklist

1. Boost upward from neutral and directionally while aiming. Spend three charges, then check sequential recharge; try movement without a jump button.
2. Press Cross + Square together, then tap Square midway through a boost. Compare parry timing while still, loading the rope, and recoiling.
3. Boost into the rope from close range on ground and in air. Expect forward penetration into the opponent's resting half before one rebound. Check diagonal vertical momentum and escape with an outward boost.
4. Parry a projectile, let it bounce back through you, and verify the owner ring. Have the opponent parry it back: it should now threaten you. Compare ordinary guard with parry ownership transfer.
5. Judge charged speed, four-second decay, posture pressure, fairness of grazing hits, simultaneous K.O., first-to-two flow, and equal opposing boosts in local mode.

Report requested changes for boost control, rope penetration/recoil, parry ease, charged speed and ownership immunity. Include any unfair-death example or captured tuning. Explicitly say when the core combat is fun enough to proceed.

## Feedback history

2026-10-19: user requested faster movement/higher jumps, immediate starts, elastic rope, three charges, and Cross boost/Square parry. Run is 24 units/s; later one-button boost supersedes the separate jump control.

2026-10-20: user requested rocket-like boost and deep rope loading, slower projectile decay, stronger charged speed, owner immunity, easier boost parry, and eliminating the jump button. User selected neutral-stick upward boost without tap/hold delay and last-parrier immunity until opponent parry. Implemented as the tunable trial above; subjective approval remains outstanding.
