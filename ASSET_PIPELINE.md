# Asset pipeline proposal

## Status and scope

Core gameplay is approved to enter P3 visual direction. This document proposes visual and import conventions; it does not approve a visual style, production art, a provider, or commercial-use terms. User selected directionC (darker high-contrast pixel art) from the A/B/C board on2026-10-20. The first static character/arena playable proof is in progress; reference approval is pending. Prepare one character and one Rookie Dojo arena as the vertical-slice proof, then request explicit Gate B approval before bulk production. One built-in image-generation comparison board is saved at `art/concepts/rookie-dojo-directions-v01.png`, with its exact prompt, provenance, dimensions, checksum and pending approval in the adjacent JSON. It is a visual direction preview, not a sprite sheet or gameplay-scale reference. Individual-service terms were reviewed at https://openai.com/policies/row-terms-of-use/ on2026-10-20; the applicable account agreement and production asset review remain separate. No blanket commercial clearance is claimed.

## Proposed art bible

- Aim for expressive pixel art that reads at phone scale: clear separated silhouettes, restrained outlines, strong value contrast between fighters, projectiles, divider, and background, limited competing detail, and effects that do not hide action. Use persistent blue/red team cues plus distinct shapes, patterns, or markings so team identity never depends on color alone. These are readability goals; palette, outline width, lighting, and style remain unapproved until the reference board is reviewed.
- Code reference arena: 100 × 56.25 gameplay units. Candidate reference canvas: 1600 × 900 px (16 px per unit), matching that 16:9 ratio. This is a working proposal, not a locked export size or runtime resolution. Validate phone-scale captures before approval.
- Candidate character cell: 128 × 128 px for the larger-character trial with a consistent center pivot and grounded feet; candidate animation frames use a fixed grid with transparent padding. At16 px/unit, the playable2.5-unit fighter radius gives a5-unit/80 px body diameter (legacy Core default1.5 gives3 units/48 px). Final frame dimensions remain proposals. The art may extend beyond it, but the collision silhouette and competitive hitboxes stay in `Game.Core`; sprite bounds never define or animate collision.
- Keep the projectile's visible solid core at the physical 0.9-unit radius (1.8 units / 28.8 px at the proposed density). Glow, trail, telegraph, and power accents are separate and must not imply a different collision radius.
- Draw the divider as the full 21-node, full-height deflected geometry used by simulation, including floor/ceiling endpoints. Preserve its authoritative shape and hitbox in every arena theme; decoration may frame it but may not obscure it or change collision.
- Character state coverage: idle, run, boost ignition, held-fuel boost loop, boost release/recovery, ascent, fall, guard, parry, recoil, posture break, victory, and defeat. Preserve the same pivot, foot baseline, and frame cell across states. Animation communicates state only; it never drives hitboxes, movement, or timing. Finisher/Friendship content stays out of this proof.

These proposed pixel dimensions follow `ArenaTuning` defaults: 100 × 56.25 units, fighter radius 1.5, projectile radius 0.9; `DividerSystem` uses 21 rope points. They map pixels to world units for review and do not change gameplay tuning.

## Files, naming, and Unity import proposal

- Use lossless RGBA PNG for authored pixel sprites; point filtering, no mipmaps, no lossy compression, consistent sRGB handling, and transparent padding. Keep the source sheets and metadata alongside imported assets. Atlas packing must preserve pixel alignment and transparent borders.
- Name files predictably: `char_<name>_<state>_vNN.png`, `arena_<name>_<layer>_vNN.png`, `projectile_<theme>_core_vNN.png`, with matching metadata/manifest entries. State frame order is left-to-right, top-to-bottom, fixed cell size; record frame count, frame rate, pivot/anchor, and intended loop behavior explicitly. Avoid ambiguous names such as `final`.
- Keep center pivot and foot baseline consistent across character frames. Record pixels-per-unit and pixel reference canvas in metadata; proposed starting PPU is 16, pending phone capture review. Arena layers should separate background, midground, playable boundary, divider, foreground, and portal so gameplay edges remain legible.

## Prompt and provenance template

For each approved concept, use a short prompt that requests one subject on a plain transparent/solid background, orthographic side view, specified palette/value contrast, readable phone-scale silhouette, and no text, UI, multiple views, or implied collision markers. For sprite requests, specify exact canvas/cell size and one named state; do not ask for a complete animation sheet until a single-frame test is accepted. Arena prompts must reserve clear playfield and full divider visibility. Keep A/B/C prompts parallel except for the style direction being compared.

Manifest proposal fields: asset ID/path, subject/theme, prompt and negative prompt, provider/tool/model, generation date, source/reference inputs, license and usage terms with verification URL/date, revision, canvas/cell dimensions, PPU, pivot/anchor, frame count/rate/loop, palette, importer settings, reviewer, and explicit human approval state. A manifest entry is not a claim of commercial clearance; verify provider terms before production use.

## First proof and validation gate

After a visual direction is approved, build one character, one Rookie Dojo arena, and one projectile theme. Inspect them in the playable scene at phone aspect ratios and at native pixel scale before proposing roster or arena expansion. Validation should check transparent backgrounds, expected canvas/cell and frame counts, pivot/anchor consistency, duplicate frames, palette limits, manifest completeness, verified usage terms, Unity importer settings, and scene readability. Confirm equal gameplay stats and unchanged fighter/projectile radii and divider geometry across the proof themes. No bulk generation, purchases, or provider-dependent automation is approved by this proposal.

## Comparison board review

A is restrained retro, B expressive modern pixel art, C graphic high contrast. All are provisional. The generated mini-scenes enlarge fighters for illustration; use the applied fighter diameter (currently5 units) in a100-unit arena and validate true phone-scale screenshots before playable approval. Blue/red balls in the concepts are illustrative: actual power colors and temporary contact-protection rings remain driven by simulation. The board is outside Unity Assets and is not imported into the game.

## C reference proof

Sources: `Assets/Game/Resources/Art/char_ninja_idle_v01.png` (1254-square RGBA) and `arena_rookie_dojo_background_v01.png` (1672×941 opaque). Exact prompts, checksums, dimensions and approval state are in `art/reference/manifest.json`; built-in image generation was used with the C board as reference. Original outputs are preserved. The generated source sizes differ from proposed grid sizes: these are static proofs, not final sprite sheets.

The character’s visible alpha>32 bounds are(247,264)-(970,1083). Runtime layout metadata in `reference_art_layout_v01.json` records visible height819/1254 and sole inset171/1254. Presentation scales the visible body to twice the applied fighter radius (currently5-unit diameter) and anchors its soles to the same ground plane. The full image canvas is not a hitbox. Source padding/scale must be standardized before animation sheets. F6 switches between reference art and the graybox without resetting simulation. No production animation or mass generation approved.

Reference proof commands: install the pinned Pillow dependency with `python3 -m pip install -r tools/assets/requirements.txt` in the chosen environment when needed, then run `python3 tools/assets/validate.py` and `python3 -m unittest discover -s tests -p 'test_*.py'`. `--production` requires explicit top-level and per-asset approval. Character alpha>32 bounds and layout fractions are checked against the source PNG, not inferred from filenames.

The arena's lower50/941 source height is a decorative dark strip below the painted floor; a metadata-driven UV crop aligns the painted surface with the existing gameplay ground. Source PNG is unchanged.

First playable proof validation: manifest/Python12 tests, Unity28 tests/build, desktop and1280×720 native900-step smoke passed. Both native checksums match the approved graybox93367EB12312CE78. Independent source review passed; source images preserved. Reference character/arena aesthetics remain pending user review; no mass production or animation set approval inferred.

Mobile readability iteration: user requested larger characters; no PNG/source change was needed. The runtime typed configuration enlarges both body and art to radius2.5. Layout remains driven by measured source fractions and applied physical radius, with grounding tests at both1.5 and2.5. Body/parry collision tuning changes are documented in GDD/DECISIONS, not hidden in cosmetic metadata.
