# Decisions

- **Authority:** `GAME_DESIGN_DOCUMENT.md` governs gameplay. Locked rules cannot change without user approval; tune values remain unimplemented and unapproved until measured and recorded here.
- **Plan path conflict:** `IMPLEMENTATION_PLAN.md` refers to `docs/` checkpoints, but repository rules prohibit `docs/`. Keep requested checkpoints at the repository root; do not create `docs/`.
- **Test path conflict:** the plan names `Tests/` and `Game.Core.Tests/`; repository rules require lowercase `tests/`. Use `tests/`.
- **Phase mapping:** execute plan phases in order: P0 tooling/bootstrap, P1 full pure simulation, P2 playable graybox. The broader user phase list is mapped within these phases; do not bypass their exit criteria or human gates.
- **Architecture:** authoritative gameplay will be pure C# fixed-step logic, separate from Unity presentation. Do not claim cross-platform bitwise determinism without evidence.
- **Unity target:** pin Unity 6000.3.26f1, verified LTS; installation is in progress. No gameplay values have been implemented or approved.
- **Art scope:** no production art before gameplay approval. Preliminary replaceable placeholder anchors and hitbox-independent sprite conventions may be prepared; defer final dimensions and reference-art approval to the art gate.
