# Gameplay playtest

Human gameplay approval: NOT GIVEN. P2 passed technical review, 8/8 Unity tests, macOS build and graphical smoke on 2026-10-19. Stop at Gate A; do not start production work until explicit gameplay approval.

## Run

From Terminal:

```sh
open /Users/bekir/Code/Parry/artifacts/build/ElasticArena.app --args --parry-output /Users/bekir/Code/Parry/artifacts/playtest
```

The default mode is blue P1 versus an Easy debug bot. Press B for local two-player. The bot is a test opponent; please try human PvP if another player is available.

To rebuild/check from the repository: `bash tools/test-core.sh`, `bash tools/test-unity.sh`, then `bash tools/test-runtime.sh`. Unity 6000.3.26f1 LTS is pinned. For Editor play, open the project in that version, open `Assets/Game/Unity/Scenes/Bootstrap.unity`, and press Play.

## Controls

| Action | P1 | P2 (local mode) |
|---|---|---|
| Move / dash direction | WASD | Arrow keys |
| Jump (hold for height) | Space | U |
| Dash | Left Shift | I |
| Defense (tap parry, hold guard) | E | O |

Gamepad 1: left stick, buttons 0 jump / 1 dash / 2 defense. Hardware mappings remain unverified.

Touch: left MOVE joystick; right JUMP / DASH / DEF. Toolbar: pause, bot/local mode, tuning, restart. F4 shows mouse-testable touch widgets on desktop. Physical device touch feel remains unverified.

Debug: B mode, R restart, P pause, N single-step while paused, F1 tuning, F2 quarter speed, F3 colliders/FPS, F12 capture. On macOS, function keys may require Fn. Tuning also exposes Easy / Normal / Hard bot difficulty. Sliders apply when you choose Apply / reset match.

F12 saves a timestamped PNG and `tuning.json`; applied settings, mode, bot difficulty, simulation tick and checksum are captured. With the launch command above, files are in `artifacts/playtest/`; otherwise the tuning panel shows the exact folder.

## Five-minute checklist

1. Compare short/held jumps; steer and dash in the air; spend two charges and observe sequential recharge.
2. Tap defense just before impact, then hold it. Check reflected direction, projectile power colors, one restored dash charge, and posture staying unchanged on parry.
3. Guard through impacts and posture break; compare guarded versus unguarded recovery. A projectile during break should eliminate.
4. Walk/jump/dash into the membrane, including diagonal rebound and opposing dashes in local mode. Fighters should stay on their own side of the visible membrane.
5. Use body attacks to shove an opponent toward projectiles. Body damage alone should not eliminate.
6. Finish a first-to-two match; assess spawn warnings, resets, fairness and visibility of grazing hits. Try touch controls if hardware is available.

Most likely tuning targets: run/jump/dash speed, parry window, projectile speed, spring strength/damping and rebound speed. These are prototype hypotheses, not approved balance values.

## Feedback

Reply with pass or requested changes for movement, dash, parry, guard, membrane, fairness and readability. Include edited values or `tuning.json`, and an example of any unclear/unfair death. Explicitly say whether core combat is fun enough to proceed.

No playtest feedback or visual approval has been received. Production art, networking, economy and release remain gated.
