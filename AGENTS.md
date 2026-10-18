# Project rules

Execute `IMPLEMENTATION_PLAN.md` from the first incomplete verified milestone. Read `PROJECT_STATUS.md` first, then the relevant plan section and `GAME_DESIGN_DOCUMENT.md`.

- GDD locked rules govern gameplay. Record conflicts and tuning choices in `DECISIONS.md`.
- Pull main before work when a remote exists; use a dedicated issue branch from updated main. No remote or issue number exists at bootstrap; use `bootstrap/p0`.
- Recheck the current code before implementing. Preserve useful existing work.
- Keep tests in `tests/`; never create `docs/`. Maintain only the explicitly requested root Markdown files.
- Use pure C# fixed-step authoritative logic, separate from Unity presentation. Centralize tunable values.
- Delegate bounded tasks with exclusive file ownership; at most three children. No recursive delegation unless assigned. Return changed paths, actual validation, assumptions, and risks in 250 words or less.
- Use available lighter models for scaffolding, medium for features, high for architecture or difficult failures. Do not create unverified runtime configuration.
- Every milestone requires relevant tests, compilation/build, independent review, and updated status. Never claim unrun checks passed.
- Before pushing, make separate commits per changed file with file-specific messages. Never mention assistant/tooling identity in commit messages. Include an issue number in PR titles when an issue exists; do not invent one.
- Stop at mandatory human gates in the plan. No production art, services, networking, or publication before the relevant approvals.
