# Decisions

- **Authority:** `GAME_DESIGN_DOCUMENT.md` governs gameplay. Locked rules cannot change without user approval; tune values remain unimplemented and unapproved until measured and recorded here.
- **Plan path conflict:** `IMPLEMENTATION_PLAN.md` refers to `docs/` checkpoints, but repository rules prohibit `docs/`. Keep requested checkpoints at the repository root; do not create `docs/`.
- **Test path conflict:** the plan names `Tests/` and `Game.Core.Tests/`; repository rules require lowercase `tests/`. Use `tests/`.
- **Phase mapping:** execute plan phases in order: P0 tooling/bootstrap, P1 full pure simulation, P2 playable graybox. The broader user phase list is mapped within these phases; do not bypass their exit criteria or human gates.
- **Architecture:** authoritative gameplay will be pure C# fixed-step logic, separate from Unity presentation. Do not claim cross-platform bitwise determinism without evidence.
- **Unity target:** pin Unity 6000.3.26f1 (revision de8167e7440c), verified LTS and now installed. The user explicitly confirmed keeping the LTS target during setup. Installed 6000.6.2f1 remains unused for project validation.
- **TUNE baseline:** `CombatTuning.Default` centralizes the GDD numeric starting values. Posture damage rounds to nearest integer, ties away from zero (level 1: 37.5 becomes 38). These are test hypotheses, with no human feel approval.
- **OPEN prototype hypotheses:** 60 Hz; arena 100 x 56.25 units; fighter radius 1.5 and projectile radius 0.6; run speed 12, jump launch 20, projectile speed 24 units/s; parry window 0.10 s; impact recovery delay 0.25 s; spring strength 24 and damping 8. Collider shape and integration coefficients still require P1 implementation/tests. No gameplay rule is implemented by the configuration alone.
- **Clock:** catch-up work is bounded per call but accumulated simulation time is retained. Interpolation exposes only the fractional remainder. Unity's fixed step uses the exact configured tick duration; no authoritative ticks are deliberately skipped.
- **Art scope:** no production art before gameplay approval. Preliminary replaceable placeholder anchors and hitbox-independent sprite conventions may be prepared; defer final dimensions and reference-art approval to the art gate.
