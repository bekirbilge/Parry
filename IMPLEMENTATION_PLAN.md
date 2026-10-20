# PROJECT: ELASTIC ARENA — AUTONOMOUS CODEX IMPLEMENTATION PLAN

**Version:** 1.0 · **Target:** Official OpenAI Codex extension for VS Code on macOS · **Orchestrator:** SOL Medium (if offered in installed Codex model selector) · **Companion:** `GAME_DESIGN_DOCUMENT.md`

> **Operating contract:** User wants to say **“Execute the implementation plan”** once. Codex should proceed autonomously through feasible local work, spawning native subagents when useful, validating each milestone, resuming from durable state, and asking the user only for unavoidable human decisions/credentials/payments/playtesting/approvals. Do not promise indefinite unattended execution: tool, session, rate, context, platform and permission limits may interrupt. On interruption, leave an exact resumable checkpoint.

## 0. FIRST RUN: preflight, no assumptions
1. Read this file and `GAME_DESIGN_DOCUMENT.md`; summarize constraints in `docs/DECISIONS.md` without pasting entire documents into the chat.
2. Inspect OS, Unity installation/version/licensing, Xcode, dotnet, Git, available disk, project tree, installed Codex version, available native subagent tools, models, reasoning settings, permission/sandbox configuration, Unity CLI, image-generation tools/API/credentials and network access. **Do not invent capabilities.**
3. Use **Unity 6 LTS** compatible with the local environment (pin exact installed editor version). If absent and unattended installation is not authorized/possible, prepare repo/config/docs and present a single clear dependency gate. Never bypass software licenses, paywalls or OS security controls.
4. Determine whether this Codex build supports `.codex/agents/*.toml`, `model_reasoning_effort`, agent overrides and parallel agent spawning. If yes, configure as below; if not, use equivalent prompt-based delegation where supported; if delegation unavailable, implement sequentially while retaining task boundaries.
5. Initialize or inspect Git repository; do not overwrite user files, secrets or unrelated work. Create `.gitignore`, `.editorconfig`, `AGENTS.md`, `docs/`, `tools/`, `Tests/`, `Assets/` as appropriate to Unity project layout. Avoid committing generated `Library/`, caches, build outputs, credentials or large source images unnecessarily.
6. Create `docs/STATUS.md`, `docs/TASK_BOARD.md`, `docs/DECISIONS.md`, `docs/ASSUMPTIONS.md`, `docs/TEST_REPORT.md`, `docs/ASSET_MANIFEST.md`, `docs/PLAYTEST.md` and `docs/CHANGELOG.md`. All are short, append or update atomically.
7. Establish a **single canonical execution command** in `AGENTS.md`: “Execute `IMPLEMENTATION_PLAN.md` from the first incomplete verified milestone. Read `docs/STATUS.md` and `docs/TASK_BOARD.md` first.”

## 1. Codex-native delegation design
### Orchestrator duties
- **SOL Medium** coordinates milestones, resolves dependencies, assigns non-overlapping file ownership, checks evidence, updates durable status, and gates progress. It should avoid writing implementation code except tiny integration fixes.
- Prefer **SOL High** for high-risk architecture, simulation determinism, multiplayer prediction/reconciliation, security, and hard race-condition debugging.
- Prefer **SOL Medium** for feature development, integration and moderately complex tests.
- Prefer a supported **lighter/faster model at low effort** for inventory, documentation, straightforward test scaffolds, asset metadata and mechanical changes.
- **Never assume an exact model identifier.** Discover model IDs in the installed Codex extension. Use available SOL / faster alternatives, fall back to parent's model when overrides are unsupported. Reasoning labels and availability may vary. If effort override silently fails, record it and continue rather than looping.
- Default concurrency **2–3** child agents; use one writer per file/module. Parallelize independent work only. Do not spawn agents for trivial edits when delegation overhead exceeds benefit.
- Subagents must not recursively delegate unless explicitly authorized by orchestrator; cap fan-out. Do not open unbounded parallel research threads.

### Native configuration (create only after checking local support)
Project-scoped `.codex/config.toml` (merge, don't overwrite):

```toml
[agents]
enabled = true
max_concurrent_threads_per_session = 3
```

Create `.codex/agents/` role files **only if the installed Codex version recognizes them**. Example `.codex/agents/implementer.toml`:

```toml
name = "implementer"
description = "Implement one scoped Unity/C# feature with tests, no unrelated edits."
model_reasoning_effort = "medium"
developer_instructions = "Own only assigned files. Read task packet and relevant interfaces. Implement smallest correct change with tests. Return terse handoff: changed paths, tests run, failures, risks, next step. Never edit design-locked rules."
```

Analogous roles: `architect` (high, design only), `reviewer` (high, read-only where supported), `tester` (medium/low), `asset_integrator` (low/medium), `researcher` (low, read-heavy). Add `model = "<verified-available-model-id>"` only after verifying valid IDs; never copy a placeholder into a live config. Keep role files short. Test one spawn before relying on custom agents.

### Task packet contract — ALWAYS use for delegations
```text
TASK ID: P<n>-T<n>
ROLE / MODEL / EFFORT: [verified available role; intended reasoning]
OBJECTIVE: [single measurable result]
DESIGN RULES: [only relevant excerpts/section references from GAME_DESIGN_DOCUMENT.md]
INPUTS: [exact file paths, interfaces, relevant test names]
OWNED FILES: [exclusive write scope]
OUT OF SCOPE: [explicit exclusions]
ACCEPTANCE: [commands/tests and expected behavior]
BUDGET: [time/complexity; stop after 2 failed approaches and report]
OUTPUT: [<= 250 words: paths changed, tests, result, blockers, assumptions]
```
Never forward full agent transcripts to the parent. Prefer committed file changes, diff summaries, small test logs and a concise result. Agent must report actual tool output, not unverified success.

### Context and token budget discipline
- **Disk is memory:** use task board, decision log and status; restart from them instead of long chat history.
- Orchestrator reads a compact `docs/STATUS.md` (target <100 lines), the active milestone, and changed interfaces only. Subagents read only task packets + relevant files. Do not load entire repo or logs without need.
- Log full diagnostics to `artifacts/logs/` (gitignored), return only last 30–80 relevant lines and a link/path.
- Batch related reads, prefer code search and diffs; no repeated broad web research once decision is recorded. Use minimal handoffs.
- Run one architectural review at phase boundaries, not on every trivial file. Escalate reasoning only after concrete failure evidence.
- If agent context grows large, write checkpoint, close completed subagents, start a fresh task with only status + next packet. Never discard unresolved blockers.
- Track rough agent calls, retry count, tests and wall-clock time per milestone; optimize total successful delivery, not minimum tokens per call.

## 2. Architecture: build the simulation before content
**Recommendation:** Unity 6, C#, mobile-first 2D rendering, fixed-step pure-C# gameplay domain isolated from Unity `MonoBehaviour`. Unity views interpolate simulation state; no Unity `Rigidbody2D` used as authoritative combat truth. A headless test runner can advance N ticks without rendering.

Suggested modules:
```
Game.Core/           // pure C# tick, state, math, collision, events
Game.Core.Tests/     // deterministic unit/property/replay tests
Game.Unity/          // Unity adapters: input, render, audio, UI, scene
Game.Bot/            // deterministic bot controller and scenarios
Game.Net/            // transport abstractions, snapshots, reconciliation
Game.Backend/        // auth, matches, inventory, server authority (later)
Game.Content/        // cosmetics, manifest, unlock catalogs
Tools/               // asset validators, imports, CI scripts
Docs/                // ADRs, tuning tables, protocols, playtest results
```
Adapt paths to actual Unity assembly definition layout. Avoid over-engineering empty modules. **No networking vendor lock-in until measured prototype needs are known.**

### Simulation requirements
- Tick-based update (start with **60 Hz TUNE**), explicit simulation state, fixed update order, no dependence on Unity frame time or floating platform behavior for fairness claims.
- Use robust sweep/continuous collision detection for fast projectiles; avoid tunneling at level 3. Standardized contact event list sorted by deterministic keys (tick, contact fraction, stable entity ID, priority). Resolve both fighters' lethal contacts in same tick.
- Separate movement state, dash charges/recharge, defense state, posture, projectile level/decay, divider spring, body attack hit registration, match state, and spawn scheduler.
- Record inputs and state snapshots with versioned schema and hash/checksum for replay comparison. Seed randomness; ideally avoid randomness in competitive trajectories.
- All combat numbers in one version-controlled tuning asset with test baselines; no magic numbers scattered across scripts.
- Server-authoritative model later: client input commands with tick numbers, prediction/reconciliation, remote interpolation, validation, anti-cheat, disconnect/reconnect and clock drift handling. Prototype offline first.

### Collision priority specification to implement and test
1. Gather all collisions over simulation step using stable ordering and contact time.
2. Resolve legal defense state at contact instant: valid perfect parry > valid guard > explicit invulnerability > unguarded hit (the relative priority of invulnerability vs parry is a **TUNE** choice, document and test).
3. Apply parry steering, projectile level change and dash restoration, or block posture, or lethal projectile contacts.
4. Resolve body impacts with per-dash once-only damage and divider constraints; allow combined knockback/hazard interaction according to contact times.
5. Evaluate guard break and both players' elimination flags **after** all relevant same-tick contacts.
6. Emit one draw/win/continue event; never select a winner by iteration order.

## 3. Autonomous milestone plan
Each milestone is **definition-of-done gated**: compile succeeds, relevant tests pass, no critical regressions, status/checkpoint updated, review performed proportional to risk. Do not skip failed gates. Commit at stable milestones if Git is available and repository permissions allow. If a required human gate blocks, stop cleanly; do not pretend to have approval.

### P0 — Repository and tooling
**Tasks:** detect toolchain; pin Unity/editor/package versions; scaffold Unity project; set Git hygiene; assembly definitions; headless test scripts; CI workflow if Git remote exists or local CI equivalent; Codex instructions/roles; initial design decisions and tuning table.
**Exit:** clean editor import, one headless test runs, a reproducible local build/test command, role delegation smoke test (if supported), `docs/STATUS.md` populated. If Unity absent, provide actionable dependency instructions and stop at explicit dependency gate.

### P1 — Pure combat simulation
**Tasks:** vector math and arena; fixed-tick state machine; movement/jump/coyote/air dash/charge regeneration; projectile spawn schedule and telegraph events; permanent ricochets and swept collisions; defense/parry/guard; level 0–3 and one-second decay; posture recovery/break; body hits; bounded membrane spring; death/draw/best-of-three.
**Tests:** projectile bounce invariants; never crossing divider; no tunneling; exact 15-second schedule/6 cap; permanent projectile count; power decay; +1 dash perfect parry; perfect parry does not heal posture; guard recovers faster; dash only hits once; same-tick draw independent of entity order; replay hash identical across repeated runs on same runtime; zero NaNs/out-of-bounds after long fuzz runs.
**Exit:** hundreds of headless bot-vs-bot simulation rounds without crashes; every locked mechanic represented by automated tests. No art required.

### P2 — Playable graybox vertical slice
**Tasks:** fixed landscape arena; input bindings for keyboard/gamepad/touch; visible fighter state; collision debug overlay; projectile telegraphs, trails and power states; posture HUD; membrane visualization; basic audio/hitstop/afterimages; simple deterministic bot with adjustable difficulty; pause/restart; screenshot/video capture script where supported.
**Tests:** Unity PlayMode/EditMode, smoke scene load, mobile aspect ratios/safe areas, input simultaneous press behavior, 60 FPS target on representative hardware (actual device needed for definitive proof).
**HUMAN GATE A — gameplay:** provide install/run instructions, five-minute test checklist and capture form. Ask user to approve movement, dash, parry, guard, divider, fairness and readability. Stop if approval unavailable. Record feedback; iterate until explicitly accepted.

### P3 — Art-direction and asset pipeline
**Tasks:** create art bible (reference canvas, pixels/unit, palette, outline, silhouette, animation frame conventions, anchors); produce 2–3 character/arena concept candidates with an available AI image tool or prepare prompts when tool unavailable; choose one coherent style; build one production-quality ninja + Rookie Dojo + projectile theme; standardize sprite sheets, atlas packing, metadata, importer settings and validation.
**Art tool decision:** investigate current commercial rights, export formats, pricing and automation capabilities of available providers (e.g. PixelLab) **at execution time**. Never assume a paid API is connected or available. Use procedural VFX and manual placeholder sprites as fallback. No purchases without user approval.
**Automation:** `tools/assets/validate` checks transparent backgrounds, sprite sizes, frame counts, anchor consistency, duplicate frames, palette, manifest and licenses; `tools/assets/import` maps files to Unity importer metadata; test in scene. Never treat an AI image as a correctly aligned animation sheet without verification.
**HUMAN GATE B — art:** show reference images and playable preview, request explicit style approval before bulk generating roster/arenas. Do not spend on bulk credits beforehand.

### P4 — Production feel and content foundations
**Tasks:** refine VFX/audio/readability; animation state machine; accessible flash settings; quick rematch and victory sequences; configurable cosmetics and arena visual swap with gameplay-state equality tests; event-driven UI; onboarding/tutorial; input accessibility; performance profiling.
**Exit:** one polished vertical slice, stable performance on tested devices, regression tests for identical stats/hitboxes across skins and themes. May require a second brief human feel gate for significant control changes.

### P5 — Multiplayer prototype (major technical risk)
**Tasks:** architecture spike and ADR comparing authoritative dedicated-server simulation, transport, region hosting, Unity networking ecosystem and operating costs. Implement two-client match using same core simulation; tick-numbered inputs, snapshots, reconciliation/prediction, state checksums, reconnect/timeouts, anti-cheat validation. Build local latency/jitter/loss harness before paying for cloud hosting.
**Tests:** 0/50/100/200 ms latency and jitter/loss scenarios; simultaneous K.O.; level-3 ricochets; near-frame parry; divider dash clash; no duplicate rewards; exploit inputs rejected; bandwidth/CPU budgets. Test real geographically separated players before declaring online ready.
**Exit:** repeatable fair match sessions with documented latency limits. Human network playtest needed. Never claim server authority when using peer-hosted outcomes.

### P6 — Accounts, economy and progression
**Tasks:** secure identities, authoritative trophies/coins, inventory/entitlements, trophy road permanent milestone unlocks, catalog and cosmetic store, achievement validation, basic analytics with privacy minimization, backend migrations and observability. No paid products until authorized and platform compliant.
**Tests:** win/loss rewards, no reward on draw-round alone, permanent unlock despite trophy loss, double-spend protection, retries/idempotency, no stat advantage from cosmetics, data deletion/privacy flows.
**Exit:** persistent progression in staging and a full match-to-reward-to-cosmetic loop.

### P7 — Content, polish and release candidate
**Tasks:** additional arenas/characters with identical mechanics, finisher/Friendship framework and a small initial set, sound/music rights validation, tutorials, localization-ready strings, settings, mobile optimization, accessibility, crash reports, privacy documentation, store assets and internal distribution.
**Tests:** regression, soak, device matrix, iOS signing/build, Android portability check, backend load/security review, gameplay balance, age/privacy/IAP policy checks as applicable.
**HUMAN GATE C — release:** user reviews real-device builds, content, privacy, store metadata and spending/production launch. **Never** submit to app stores, deploy paid production infrastructure or publish publicly without explicit approval.

## 4. QA strategy and automatic recovery
- Run smallest relevant unit tests after each edit; full suite at milestone gate. Use test-first for tricky collision/state invariants.
- Categorize failures: code defect, flaky test, missing dependency, permissions, network, service/credential, insufficient hardware, design ambiguity, tool limitation.
- Retry a failing approach **at most twice** without new evidence; then delegate one high-reasoning diagnostic review. Never enter infinite fix loops. Record failing command and excerpt.
- For external unavailable services, implement mock interfaces, validate local functionality, mark `BLOCKED_EXTERNAL`, and continue only with independent tasks. Do not fake real service integration or credentials.
- At each verified milestone: update `docs/STATUS.md` (phase, last successful tests, open blockers, next task), `docs/TASK_BOARD.md`, `docs/DECISIONS.md`, and commit summary. Keep progress durable across new Codex sessions.
- If token/session/time limits approach, stop gracefully with **RESUME HERE** and one exact next command. Never claim background execution continues after session stops.

## 5. Security, permissions and spending policy
- Work only in the opened project workspace unless explicitly authorized. Do not delete unrelated files or reset Git history. Request permission for irreversible/destructive commands.
- Do not purchase API credits, sign up for external services, enable billing, expose ports, deploy publicly, publish assets, send emails, submit app store builds or store credentials in repo without user approval.
- Secrets through environment/secret manager only; `.env.example` documents required keys, real `.env` ignored. Avoid transmitting user project files to unapproved third-party art services.
- Asset rights, generated content commercial terms, app store compliance and privacy rules require documented verification, not assumptions.
- Respect Codex sandbox and approval requests. If user isn't present to approve, pause safely with a clear gate.

## 6. Human checkpoints — only when necessary
1. **Toolchain/credential gate:** missing Unity/Xcode/signing, account/API access, purchases, provider terms or permissions that cannot be resolved locally.
2. **Gameplay feel gate:** after graybox P2. Ask for focused feedback with explicit pass/fail per movement, parry, divider, combat readability.
3. **Visual direction gate:** before expensive/bulk art production P3.
4. **Online fairness gate:** real-player latency and control fairness at P5.
5. **Release gate:** before public deployment/store submission or recurring costs.
Do not interrupt for routine parameter tuning, lint errors, code organization, ordinary bugs or internal agent choices. Bundle questions rather than asking piecemeal. Playtesting and art approval are explicitly the user's responsibilities.

## 7. Templates for project files
### `AGENTS.md` (Codex project instructions)
```md
# Codex Project Rules
Read `GAME_DESIGN_DOCUMENT.md`, `IMPLEMENTATION_PLAN.md`, and `docs/STATUS.md` before starting work.
When user says "execute", resume the first incomplete milestone in `docs/TASK_BOARD.md`.
Use native Codex subagents for independent bounded tasks when available; prefer 2–3 max concurrently and non-overlapping file ownership. Choose models/effort by complexity after checking actual availability.
Never silently change locked game rules. Follow acceptance tests, evidence-based checkpoints and permission rules in `IMPLEMENTATION_PLAN.md`.
Write concise handoffs; preserve durable status. Stop only for necessary human gates or genuine external blockers.
```

### `docs/STATUS.md`
```md
# Execution status
Phase: P0
Last verified milestone: none
Last successful test command: not run
Current task: P0 preflight
Next task: detect Unity, Codex agents, Git and toolchain
Blockers: none known
Human approval needed: no
Resume instruction: read both root MD files and this status; run first unchecked task.
```

### `docs/TASK_BOARD.md`
```md
# Task board
- [x] P0 toolchain/repo/CI
- [x] P1 headless combat simulation
- [x] P2 playable graybox — HUMAN GAMEPLAY GATE passed2026-10-20
- [ ] P3 AI art pipeline — HUMAN ART GATE
- [ ] P4 production-feel vertical slice
- [ ] P5 online PvP — HUMAN NETWORK TEST
- [ ] P6 economy/progression
- [ ] P7 launch content — HUMAN RELEASE GATE
```

## 8. One-paste execution prompt for VS Code Codex
Select **SOL Medium** if available; open the project folder with both Markdown documents at its root. Paste:

> **EXECUTE THE PROJECT.** Read `GAME_DESIGN_DOCUMENT.md` and `IMPLEMENTATION_PLAN.md` as authoritative instructions. You are the Codex orchestrator. Start at P0 or resume the first unverified milestone from `docs/STATUS.md`. Discover your actual subagent/model/effort capabilities and configure project-scoped Codex agents when supported. Delegate bounded non-overlapping tasks to appropriate models; prefer medium effort for orchestration, high for complex architecture/review and low/medium for routine implementation, tests and assets. Minimize context bloat through task packets and durable files. Work autonomously, implement, test, fix, review, checkpoint and proceed through every feasible milestone. Do not stop merely to ask what to do next. Stop only at mandatory human playtesting/art approval, missing credentials/permissions, paid services or release authorization, or genuine blocking limitations. Never fake tests or approvals. On any stop, write exact reproducible status and next action in `docs/STATUS.md`, then tell me precisely what is needed.

## 9. Definition of project completion
A validated mobile game with fun, approved combat; consistent licensed/provenance-tracked art; reliable two-player online PvP; fair authoritative outcomes; trophy/coin progression; cosmetic-only monetization design; testable release builds; documentation and deployment/runbooks. This is a multi-phase project, not a single uninterrupted Codex turn. **“Execute” means autonomously advance as far as the actual environment and human gates permit, and always leave it resumable.**
