# PROJECT: ELASTIC ARENA — GAME DESIGN DOCUMENT

**Version:** 1.0 · **Status:** authoritative design baseline · **Date:** 2026-10-18

> Working title only. This document is the source of truth for *what* to build. `IMPLEMENTATION_PLAN.md` governs *how* to build it. Do not silently alter locked decisions. Numerical values marked **TUNE** are starting hypotheses, not promises.

## 1. Vision and pillars
A landscape, fixed-camera, side-view, two-player 2D pixel-art arcade duel. Ninjas and other cosmetic fighters move explosively, guard or perfect-parry ricocheting projectiles, use an elastic central membrane as a springboard, and physically shove opponents into hazards. Short, readable, spectacular best-of-three matches. Inspiration: *A Slight Chance of Sawblades* (survival), Sekiro (posture), Celeste (precise movement), Mortal Kombat (finishers/Friendships), Clash Royale (trophy-road presentation). These are inspirations, **not** licenses to copy assets, names, music, UI, or code.

**Pillars:** (1) skill wins, never purchased power; (2) readable deterministic-feeling trajectories; (3) immediate, expressive movement; (4) clear telegraphs and fair deaths; (5) deep mastery from simple controls; (6) cosmetic variety without competitive variance.

## 2. Game loop and rules — LOCKED
- Two fighters spawn on opposite halves of a symmetrical arena; full-height center membrane prevents crossing but permits stretching toward the other side. Camera fixed; landscape mobile-first.
- A match is first to **two round wins**. No conventional health. An **unguarded projectile impact kills instantly**. A blocked impact increases posture; a perfect parry safely reflects it. The user-approved floor-fire trial also eliminates on active contact after two warning flashes (see final trial section).
- If both fighters are eliminated in the **same simulation tick**, declare **DOUBLE K.O. / DRAW**, award neither a round point, reset and replay. Resolve all collisions for both fighters before declaring outcome; never let iteration order pick the winner. Elimination in different ticks follows the simulation ordering; the survivor of the first lethal tick wins.
- Each new round resets positions, velocities, posture, dash charges/fuel, projectile count/power, timers, and divider deformation. Draws do not individually grant match rewards. Winning a best-of-three match triggers a short, skippable cosmetic victory sequence.
- No conventional match timer in initial design; once projectile cap is reached, continue until a round resolves. A **TUNE** anti-stall fallback may be introduced only with explicit approval and fairness testing.

## 3. Movement — LOCKED mechanics, TUNE numbers
- Continuous horizontal run and air steering. One movement action: neutral stick boosts straight up; aiming boosts in that direction immediately, on ground or in air. No separate jump button. The fuel trial uses hold-to-thrust with continuous steering; release stops thrust and leaves controllable coast/gravity. Neutral aim requests upward thrust. Legacy upward boost consumes one normal charge in comparison mode. Parry remains available throughout boost/loading/recoil. Legacy jump commands remain internal for prototype regression/replay compatibility, not in the playable controls.
- **Fuel boost (selected by user after playtest):** current playable tank is1.8 s (2× the earlier0.9 s). Hold boost to burn fuel, steer continuously, release to coast. Regenerate at0.3 fuel-seconds/s after0.35 s without thrust; ignition costs0.04 s. Enemy perfect parry restores0.6 s (one-third tank), capped; owned redirects give no fuel. Invulnerability lasts0.08 s only on an eligible ignition, with0.35 s rearm; holding fuel never extends it. Exhaustion or rope recoil requires release before reignition. Powered acceleration360 and all fuel values are **TUNE**. Rope loading consumes fuel and ends powered thrust on release/exhaustion. One body-attack hit per ignition, not per steering update.
- **Legacy comparison mode:** three dash charges, refilling sequentially at approximately **1.0 s per charge** (**TUNE**). Perfect parry instantly restores **one** dash charge, capped at three. No automatic extra restoration at critical projectile level unless separately approved.
- Directional rocket boost (dash), using continuous normalized movement-stick aim (no eight-way snapping), legacy ~0.30 s powered duration (fuel mode lasts while supplied), upward velocity capped at 40 units/s while preserving launch aim (horizontal/downward cap80), and ~0.08 s invulnerability (**TUNE**). Invulnerability rules must be specified in the prototype and tested against projectile and body collisions. Avoid unintended invincible dash chains.
- Jump buffering, coyote time, acceleration, friction, air steering, dash cancel windows, and jump arc are **TUNE**. Ground movement should be crisp, never slippery. Preserve controllability in rebounds/knockback; no long helpless stun.
- Design target: responsive to mobile latency and touch imprecision, without auto-aiming parries.

## 4. Mobile input — TUNE layout
- Left thumb: virtual directional joystick (movement and directional influence; neutral boost is straight up).
- Right thumb: boost and defense; no separate jump button. Tap defense = timed parry attempt (prototype window 0.16 s, **TUNE**); hold defense = guard after tap window. Perfect-parry window, hold threshold, priority when pressing multiple controls, and gesture forgiveness are **TUNE**.
- Local phone prototype arena width grows to match the available combat viewport, keeping height56.25 world units and square visual units. Dimensions are fixed for that match and captured with tuning; both landscape orientations are allowed. A future network match must share one authoritative arena configuration rather than choosing independent client bounds.
- Current iPhone 17 Pro Max scheme (`edge-aware-directional-swipe-follow-stick`; installed and launch-verified): visible control strip occupies14% of safe-area height. The left0–50% starts a floating MOVE joystick that follows overflow beyond travel. In50–90%, hold for BOOST and make a quick directional swipe for one parry pulse; in90–100%, tap for parry or hold for guard. There is no BOTH zone. BOOST and PARRY/GUARD accept touches across the bottom25% safe-area height; MOVE starts only in the visible strip. A swipe aims boost/parry until stick movement takes over, and final lift-flick aim survives its buffered parry tick. Normal swipe distance is `clamp(3.5% screen height, 12–96 px)`. Near the bottom edge it adapts to `min(normal distance, max(12 px, 1.5 × touch slop, anchorY / 2))`, with speed threshold scaled proportionally; interior speed threshold is1 screen-height/s. Recognition window0.18 s; quiet rearm0.12 s. Parry reflection keeps the hybrid/±30° rule.
- Support simultaneous directional input + boost/parry. Prevent accidental guard toggling and conflicting gestures. Add accessible control sizing and remapping after first prototype.
- For desktop playtesting, keyboard/gamepad equivalents must expose identical actions.

## 5. Projectiles and ricochet — LOCKED
- Round starts with **one** projectile. Every **15 s** add one, up to **six active** (**TUNE** interval/cap). Spawn gives a **3 s** visible warning, from top-center portal on a predictable, fair, mirrored/alternating trajectory; no unavoidable immediate spawn hit.
- All projectiles remain **permanently until the round ends**. No lifetime or bounce-count despawn. Pass through each other and the center membrane. Bounce predictably off walls, ceiling and floor using deterministic rules; no random speed ramp.
- Arena themes change appearance (sawblade, fireball, meteor, magic orb) but **never** hitbox, speed, bounce, parry, posture, or other competitive properties.
- Parry outgoing angle is a controlled hybrid of incoming velocity, impact geometry and player input. Input steers within about **±30°** of natural reflection (**TUNE**); same steering at all projectile levels. No level-specific aim assistance.
- A perfect parry increments power by one level (max 3), resets decay timer, boosts speed/posture pressure, and restores one dash charge in legacy mode or one-third fuel tank in the trial. With no subsequent perfect parry, lose **one level every four seconds** until zero. Example multipliers (**TUNE**):

| Level | Name | Speed multiplier | Block posture multiplier |
|---|---|---:|---:|
| 0 | Normal | 1.00× | 1.00× |
| 1 | Charged | 2.00× | 1.25× |
| 2 | Overcharged | 2.333…× | 1.50× |
| 3 | Critical | 2.666…× | 2.00× |

- Parry grants **contact-only safety** against that projectile: enough to pass through the immediate overlap without self-damage. Safety ends as soon as projectile and parrier separate beyond the expanded parry contact distance (fighter radius + parry reach + projectile radius; playable default6.9 units with the larger parry trial). Returning shots are dangerous again, even if the opponent never touched them; a new valid parry grants its normal rewards. No lasting ownership immunity. Safety is evaluated during continuous collision resolution and at tick end, so a same-step wall return after separation is dangerous. Opponent parry replaces protection. Within a still-protected contact, deliberate safe redirect preserves power/speed/decay and grants no reward. Guard, wall bounce and decay never refresh safety. Ring indicates only current contact protection, not permanent ownership.
- Playable projectile speeds are36 normal and72/84/96 at power levels1–3. Keep visual states obvious through glow/trail/impact/audio, without obscuring exact hitbox. User-approved readability trial: projectile radius0.9 units (previous0.6, 50% larger diameter), shared by rendering and collision. After each four-second level threshold, speed eases toward the new target at12 units/s² instead of snapping; the72→36 transition takes three additional seconds. From critical level3, power drops to zero at12 seconds and speed reaches36 at15 seconds without another perfect parry. Posture/power still change at each threshold. Enemy perfect parry immediately reapplies its new level speed and restarts decay; guard, wall bounces and owned redirects retain ongoing slowdown. These values are **TUNE** and fixed-step deterministic.
- Perfect parries **never** reduce posture and remain possible even at 99/100 posture. The guard-break state at exactly 100 must be handled explicitly (see §6).

## 6. Defense, posture and death
- Maximum posture **100** (**TUNE**). Example level-0 block +30, level 1 +38, level 2 +45, level 3 +60 (**TUNE**, choose explicit integer rounding).
- While holding guard **without being hit**, posture recovers **faster** than while unguarded: initial proposal guard idle −15/s, unguarded idle −5/s (**TUNE**). Define hit recovery delay to avoid instant reduction during impact resolution.
- Active perfect-parry/owned-redirect area uses fighter radius plus 3.5 units of reach (playable effective radius6 versus physical radius2.5, **TUNE**). User phone feedback explicitly enlarged the effective parry radius4→6 (+50% radius); window/recovery stay unchanged. The user-requested readability trial enlarges body and art together from radius1.5 to2.5 (diameter3→5, about67% larger). Ordinary guard uses the same physical body; boost invulnerability and timing/recovery windows remain unchanged. The visible parry halo matches the authoritative parry radius.
- Perfect parry causes **zero posture damage** and **zero posture recovery**. Parry available at any posture below guard-break state. When a block fills posture to 100, enter guard break for ~0.6 s (**TUNE**), unable to guard/dash; a projectile hit during break eliminates. Resolve any parry attempt *before* determining whether an ordinary block causes break; do not retroactively invalidate a valid perfect parry.
- Unblocked projectile hit eliminates, except during explicitly defined invulnerability. Body attacks never directly eliminate but can cause posture break and knock a fighter into a projectile.
- Specify exact simultaneous contact order in code: collect contacts → classify parry/guard/invulnerability/body → compute projectile changes and posture → determine eliminations together → apply outcome. Include regression tests for frame-boundary interactions.

## 7. Body attacks and contact — LOCKED
- Walking contact: minor push, **no posture damage**.
- Jump/body collision: moderate knockback and ~**15 posture** (**TUNE**).
- Dash-body collision: strong knockback and ~**25 posture** (**TUNE**), uses the active movement resource (legacy charge or trial fuel); **at most one damaging hit per dash/continuous fuel ignition**. Never multi-hit by remaining overlapped.
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
1. Two equal-stat fighters can run, boost upward or directionally with fuel (or three legacy charges), refill, parry and guard in one fixed camera arena.
2. Projectile trajectories are predictable and reproducible; spawn telegraphs work; up to six persist until round end.
3. Perfect parry steers projectile, charges level, decays correctly, restores one legacy dash or one-third fuel tank and never heals posture.
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

User playtest selection (2026-10-20): keep the latest fuel-based playable version and its current tuning, larger projectiles, gradual speed loss and contact-only parry safety. Fuel is the selected default; three charges remain a debug comparison. This records the movement choice; overall Gate A approval is still separate.

Core approval and visual direction (2026-10-20): user accepted the current game as being in a good place, passing Gate A, and chose C (dark high-contrast pixel art) for the first ninja/Rookie Dojo proof. Keep current fuel and combat tuning. Character/arena reference appearance and animation production approval remain pending a playable visual review.

Phone playtest priority (2026-10-20): user requested trying the game on a phone now, before finishing animation. Prepare landscape touch-enabled development exports ahead of later full mobile hardening. Playable character radius2.5 is adjustable1.5–3.5 in F5; both players share the applied value and art scales to it. Legacy Core default radius1.5 is retained for regression/comparison, not the new playable default. Visual reference approval remains pending.


## Floor-fire gameplay trial — lethal revision approved 2026-10-30

User explicitly replaced the earlier launch/posture proposal with instant elimination. Marked floor patches flash twice during a two-second warning, then ignite. Any fighter whose circular body touches active fire is eliminated, regardless of guard, parry or boost invulnerability. Fire changes no posture and gives no launch impulse. A single victim awards the opponent the round; both eliminated in the same fixed step produce the existing draw/replay with no points.

Trial tuning: first warning after6 seconds of active round time; warning2 seconds, fire1.25 seconds, warning-start interval10 seconds. One patch per half; mirrored centers rotate through50%,12%,88%,32%,68% of each half, covering every legal ground position over a full cycle. Full width is24% of a half; flame height1.5 units. Circle-versus-rectangle contact respects corner/airborne clearance. Both patches affect either player, including rope intrusion. Hazard effects resolve before projectile contacts; dead fighters cannot attack or parry. Shared environmental rules apply independently of cosmetic arena themes.

Clock and hit ledger reset each round, including draws; pause/countdown/results do not advance active hazard time. Snapshot version8 includes the hazard clock and per-fighter burst ledger. Core defaults keep fire disabled; the playable trial enables it. Tuning menu and desktop F7 toggle fire with a match restart for comparison. Timing, dimensions and pattern remain tunable and require physical feedback. No bulk content is approved by this trial.
