# FX Assets

V13G.10 starts with runtime-generated pixel FX in `scripts/PixelFxPlayer.cs`.

Future hand-authored sprite sheets should live here and keep the same catalog IDs:

- `fx.weapon_slash`
- `fx.hit_spark`
- `fx.heavy_impact`
- `fx.inner_guard`
- `fx.shaolin_gold_body`
- `fx.taiji_spiral`
- `fx.six_meridian_sword`
- `fx.ancient_tomb_moon`
- `fx.romance_assist`

Rules:

- Use transparent PNG or imported SpriteFrames resources.
- Keep hard pixel edges and low color counts.
- Do not bake soft glow or UI text into FX frames.
- Keep actor bodies, weapons, and FX as separate layers.
