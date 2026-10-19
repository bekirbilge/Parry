# Asset pipeline checkpoint

Production art is deferred until gameplay approval. No asset tooling, provider, or commercial-use terms have been verified or selected.

P2 uses code-generated circles, rectangles, trails and a short synthesized cue. There are no generated production sprites or third-party art assets. Fighters show their full circular collision silhouette; cosmetic bodies and eyes read snapshot position/facing only. Projectile cores show the exact radius, with separate faint glow/trails. The membrane graphic follows the authoritative full-height deflected boundary.

Replacement contract: entity center is the render pivot, floor anchor is center minus fighter radius, and authoritative hitboxes remain in `Game.Core`. All characters and projectile themes must keep those hitboxes and stats. Future sprites use lossless RGBA, point filtering, consistent pivots/pixels per unit and a common palette; final canvas dimensions and pixel density remain pending visual direction approval.

Required future animation states: idle, run, jump ascent, fall, dash, guard, parry, knockback/recoil, posture break, defeat and victory; finisher/Friendship states follow their later content phase. Animation never drives competitive collision.

Before the art gate, only replaceable placeholder anchors and hitbox-independent sprite conventions may be prepared. Final reference dimensions, pixel density, palette, silhouette and reference art require art-direction approval. Track provenance, source, license/usage terms, revisions, and human approval for each production asset when the pipeline is established.
