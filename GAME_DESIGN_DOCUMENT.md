# PROJECT: ELASTIC ARENA — GAME DESIGN DOCUMENT

**Version:** 1.0 · **Status:** authoritative design baseline · **Date:** 2026-10-18

> Working title only. This document is the source of truth for *what* to build. `IMPLEMENTATION_PLAN.md` governs *how* to build it. Do not silently alter locked decisions. Numerical values marked **TUNE** are starting hypotheses, not promises.

## 1. Vision and pillars
A landscape, fixed-camera, side-view, two-player 2D pixel-art arcade duel. Ninjas and other cosmetic fighters move explosively, guard or perfect-parry ricocheting projectiles, use an elastic central membrane as a springboard, and physically shove opponents into hazards. Short, readable, spectacular best-of-three matches. Inspiration: *A Slight Chance of Sawblades* (survival), Sekiro (posture), Celeste (precise movement), Mortal Kombat (finishers/Friendships), Clash Royale (trophy-road presentation). These are inspirations, **not** licenses to copy assets, names, music, UI, or code.

**Pillars:** (1) skill wins, never purchased power; (2) readable deterministic-feeling trajectories; (3) immediate, expressive movement; (4) clear telegraphs and fair deaths; (5) deep mastery from simple controls; (6) cosmetic variety without competitive variance.

## 2. Game loop and rules — LOCKED
- Two fighters spawn on opposite halves of a symmetrical arena; full-height center membrane prevents crossing but permits stretching toward the other side. Camera fixed; landscape mobile-first.
- A match is first to **two round wins**. No conventional health. An **unguarded projectile impact kills instantly**. A blocked impact increases posture; a perfect parry safely reflects it.
- If both fighters are eliminated in the **same simulation tick**, declare **DOUBLE K.O. / DRAW**, award neither a round point, reset and replay. Resolve all collisions for both fighters before declaring outcome; never let iteration order pick the winner. Elimination in different ticks follows the simulation ordering; the survivor of the first lethal tick wins.
- Each new round resets positions, velocities, posture, dash charges, projectile count/power, timers, and divider deformation. Draws do not individually grant match rewards. Winning a best-of-three match triggers a short, skippable cosmetic victory sequence.
- No conventional match timer in initial design; once projectile cap is reached, continue until a round resolves. A **TUNE** anti-stall fallback may be introduced only with explicit approval and fairness testing.

## 3. Movement — LOCKED mechanics, TUNE numbers
- Continuous horizontal run and air steering. One movement action: neutral stick boosts straight up; aiming boosts in that direction immediately, on ground or in air. No separate jump button; upward boost consumes a normal charge. Parry remains available throughout boost/loading/recoil. Legacy jump commands remain internal for prototype regression/replay compatibility, not in the playable controls.
- Three dash charges, refilling sequentially at approximately **1.0 s per charge** (**TUNE**). Perfect parry instantly restores **one** dash charge, capped at three. No automatic extra restoration at critical projectile level unless separately approved.
- Directional rocket boost (dash), initially eight-way snapped from movement stick, ~0.30 s powered duration and ~0.08 s invulnerability (**TUNE**). Invulnerability rules must be specified in the prototype and tested against projectile and body collisions. Avoid unintended invincible dash chains.
- Jump buffering, coyote time, acceleration, friction, air steering, dash cancel windows, and jump arc are **TUNE**. Ground movement should be crisp, never slippery. Preserve controllability in rebounds/knockback; no long helpless stun.
- Design target: responsive to mobile latency and touch imprecision, without auto-aiming parries.

## 4. Mobile input — TUNE layout
- Left thumb: virtual directional joystick (movement and directional influence; neutral boost is straight up).
- Right thumb: boost and defense; no separate jump button. Tap defense = timed parry attempt (prototype window 0.16 s, **TUNE**); hold defense = guard after tap window. Perfect-parry window, hold threshold, priority when pressing multiple controls, and gesture forgiveness are **TUNE**.
- Support simultaneous directional input + boost/parry. Prevent accidental guard toggling and conflicting gestures. Add accessible control sizing and remapping after first prototype.
- For desktop playtesting, keyboard/gamepad equivalents must expose identical actions.

## 5. Projectiles and ricochet — LOCKED
- Round starts with **one** projectile. Every **15 s** add one, up to **six active** (**TUNE** interval/cap). Spawn gives a **3 s** visible warning, from top-center portal on a predictable, fair, mirrored/alternating trajectory; no unavoidable immediate spawn hit.
- All projectiles remain **permanently until the round ends**. No lifetime or bounce-count despawn. Pass through each other and the center membrane. Bounce predictably off walls, ceiling and floor using deterministic rules; no random speed ramp.
- Arena themes change appearance (sawblade, fireball, meteor, magic orb) but **never** hitbox, speed, bounce, parry, posture, or other competitive properties.
- Parry outgoing angle is a controlled hybrid of incoming velocity, impact geometry and player input. Input steers within about **±30°** of natural reflection (**TUNE**); same steering at all projectile levels. No level-specific aim assistance.
- A perfect parry increments power by one level (max 3), resets decay timer, boosts speed/posture pressure, and restores one dash charge. With no subsequent perfect parry, lose **one level every four seconds** until zero. Example multipliers (**TUNE**):

| Level | Name | Speed multiplier | Block posture multiplier |
|---|---|---:|---:|
| 0 | Normal | 1.00× | 1.00× |
| 1 | Charged | 1.50× | 1.25× |
| 2 | Overcharged | 1.75× | 1.50× |
| 3 | Critical | 2.00× | 2.00× |

- Only the most recent perfect parrier is immune to that projectile until the opponent perfect-parries it back. Wall bounces, ordinary guard and power decay do not transfer or remove immunity. Owned projectiles pass through their owner without damage, posture impact or repeated parry benefits. Unclaimed projectiles remain dangerous to both players. Show ownership with an owner-colored ring while keeping the power color visible.
- Keep visual states obvious through glow/trail/impact/audio, without obscuring exact hitbox. Speed can transition smoothly while level-based posture values switch at decay thresholds; implement deterministically.
- Perfect parries **never** reduce posture and remain possible even at 99/100 posture. The guard-break state at exactly 100 must be handled explicitly (see §6).

## 6. Defense, posture and death
- Maximum posture **100** (**TUNE**). Example level-0 block +30, level 1 +38, level 2 +45, level 3 +60 (**TUNE**, choose explicit integer rounding).
- While holding guard **without being hit**, posture recovers **faster** than while unguarded: initial proposal guard idle −15/s, unguarded idle −5/s (**TUNE**). Define hit recovery delay to avoid instant reduction during impact resolution.
- Perfect parry causes **zero posture damage** and **zero posture recovery**. Parry available at any posture below guard-break state. When a block fills posture to 100, enter guard break for ~0.6 s (**TUNE**), unable to guard/dash; a projectile hit during break eliminates. Resolve any parry attempt *before* determining whether an ordinary block causes break; do not retroactively invalidate a valid perfect parry.
- Unblocked projectile hit eliminates, except during explicitly defined invulnerability. Body attacks never directly eliminate but can cause posture break and knock a fighter into a projectile.
- Specify exact simultaneous contact order in code: collect contacts → classify parry/guard/invulnerability/body → compute projectile changes and posture → determine eliminations together → apply outcome. Include regression tests for frame-boundary interactions.

## 7. Body attacks and contact — LOCKED
- Walking contact: minor push, **no posture damage**.
- Jump/body collision: moderate knockback and ~**15 posture** (**TUNE**).
- Dash-body collision: strong knockback and ~**25 posture** (**TUNE**), consumes normal dash charge; **at most one damaging hit per dash**. Never multi-hit by remaining overlapped.
- Body attacks may break posture, but cannot directly kill. Tactical goal: shove opponent into active projectiles. Preserve fair opportunities to dodge/parry during knockback.
- Whether perfect parry directly counters a body dash is **OPEN FOR PROTOTYPE**; default first implementation: parry applies to projectiles only, with body clash resolved by momentum. Do not silently add a counter mechanic.

## 8. Elastic membrane — LOCKED behavior, TUNE coefficients
- Shared, full-height, elastic divider centered at X=50 on a conceptual 100-unit arena; never permits complete crossing. Projectile paths unaffected by it.
- **Baseline TUNE:** maximum deflection 8 units toward either half; walking press ~3–4; jumping ~5–6; dashing up to 8. Elastic return when pressure ends. Dash rebound travels ~15–20 units back toward own half. No divider-caused posture damage.
- An inward boost first loads and stretches the rope into the opponent’s resting half before recoil (prototype load window ~0.20 s, **TUNE**). Crossing the arena’s resting center line during stretch is allowed; crossing through the actual rope is never allowed. Ground-level rope segments can lie along the floor/ceiling next to fixed anchors so grounded loading is physically possible. Parry stays available through boost, loading and recoil.
- Both players pressing creates opposing forces; simultaneous equal dashes cause strong symmetrical recoil; unequal momentum changes deformation and rebound. Preserve vertical momentum for diagonal rebound. Players retain control, including parry during recoil, subject to tested collision rules.
- The divider is not a generic rigid wall. Model as a **bounded, damped spring with explicit per-player contact constraints**. Prefer stable, auditable custom math over unstable engine spring chains. Separate visual mesh deformation from gameplay collision model.
- Allow offensive body collisions near a stretched membrane without players crossing. Document contact priority and no-teleport invariants.

## 9. Arena geometry and competitive parity
- Ranked arenas use exactly the same symmetric gameplay bounds, projectile collider radii, spawn regions and membrane mechanics. Themes alter only cosmetic layers, sound, lighting, VFX and projectile art.
- All fighters (ninja, knight, samurai, frog, robot, skeleton, etc.) share identical collision boxes, speeds, jump heights, dash/parry windows and all combat stats. Silhouettes must remain readable and fit standardized rig/anchor conventions.
- Prototype theme: **Rookie Dojo**, ninja vs ninja. Subsequent example themes: Clockwork Foundry, Volcanic Temple, Spirit Shrine, Orbital Arena, Grandmaster Citadel. Final names and thresholds are **TUNE**.

## 10. Progression, rewards, economy
- Trophy ladder with permanent milestone arena unlocks even if trophies later fall. Illustrative thresholds (**TUNE**): Dojo 0, Foundry 200, Volcano 500, Shrine 1000, Orbital 1600, Citadel 2500.
- Every completed match grants coins including losses; sample **+100 win / +35 loss** (**TUNE**). Trophies and coins are separate systems. No energy gating or daily earn cap. Cosmetic items associated with unlocked arenas become purchasable with earned coins.
- Characters, costumes, projectile skins, trails, emotes, victory poses, finishers and humorous **Friendships** are cosmetic. No paid power, altered visibility/hitboxes, hidden aim bonuses or arena-specific advantage. Skill-only secret cosmetics from verifiable achievements (e.g. bank-shot win, aerial perfect parry, streak).
- Victory animation brief and skippable; competitive rematch flow must be quick. Consider a separate seasonal ranked ladder later, without invalidating permanent trophy-road unlocks.
- Monetization strategy: optional cosmetic purchases only, subject to platform policies and later explicit approval. Do not add ads, gambling, wager mechanics, loot boxes or pay-to-win by default.

## 11. Art and audio bible
- Cohesive expressive **pixel art**, side-view silhouette readability at phone scale. Avoid AI-generated frame inconsistency. Define a reference resolution, pixel density, palette, outline thickness, lighting and effect opacity before batch production.
- Character master sheets: idle, run, jump ascent/fall, dash, parry, guard, guard-break, knockback, defeat, win, finisher/Friendship; standardized anchors, sprite bounding boxes and hitboxes independent of art.
- Separate arena layers: static background, decorative midground, playfield edges, foreground, membrane visual, portal. Preserve identical functional collision geometry across themes.
- Procedural VFX where advantageous: afterimages, motion trails, parry impact flash, particles, hitstop, camera shake; strict readability and reduced-flash accessibility options. Audio conveys parry quality, charge level, posture break and spawn warning.
- Asset provenance: store prompt, tool, generation date, source, license/usage terms, revision, and human approval in a manifest. Never assume a tool's output is commercially cleared without checking terms.

## 12. Multiplayer and fairness requirements
- Server-authoritative simulation or authoritative reconciliation design; clients predict local movement for responsiveness and interpolate remote state. Never trust client-reported kills, posture, coin rewards, trophies or cosmetic entitlements.
- Use fixed-step tick, deterministic event ordering, reproducible seed/initial conditions, stable collision priority and bounded state serialization. Do not claim cross-platform bitwise determinism without tests; use authoritative server outcomes regardless.
- Measure input-to-action delay, parry fairness at latency, jitter, rollback/reconciliation artifacts, server cost and exploit resistance. Record match replays as input/event streams or periodic snapshots for debugging.
- No online mode until local core passes tests and manual feel gate. Prototype AI is a test opponent, not a substitute for human PvP testing.

## 13. Prototype acceptance criteria
1. Two equal-stat fighters can run, boost upward or directionally three times, refill, parry and guard in one fixed camera arena.
2. Projectile trajectories are predictable and reproducible; spawn telegraphs work; up to six persist until round end.
3. Perfect parry steers projectile, charges level, decays correctly, restores one dash and never heals posture.
4. Guard recovers posture faster than unguarded idle; guard break and unguarded instant death work.
5. Divider stretches, rebounds, respects vertical momentum and never permits full crossing.
6. Body jump/dash contact applies knockback/posture once per attack and can force a projectile kill.
7. Best-of-three and same-tick DOUBLE K.O. draws are correct.
8. Game is playable with touch and desktop test inputs; frame pacing and visual readability are acceptable.
9. No gameplay stat differences across skins, arenas or projectile themes.
10. **Human playtest gate:** user explicitly approves feel of movement, parry, membrane, fairness and readability.

## 14. Open decisions — do not block first prototype
- Exact dimensions, pixel reference resolution, frame rate, physics tick, parry active frames, guard hold threshold, projectile spawn angle schedule, collision radii, knockback duration, spring damping and charge interpolation.
- Detailed bot behavior, network transport/vendor, backend hosting, matchmaking region selection, live-ops, retention, prices and store catalog.
- Name/branding, precise animations and visual direction approval, and production art vendor/tool selection.
- Treat these as documented assumptions, instrument them, and request approval only at designated gates or when irreversible/costly.

## 15. Change control
Mark design decisions **LOCKED**, **TUNE**, or **OPEN** in a decision log. Coding agents may change **TUNE** values to meet measured acceptance tests, recording before/after and evidence. Changes to **LOCKED** rules require user approval. Art approval and playtesting remain human responsibilities. No invented approvals.
