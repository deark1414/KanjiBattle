# KanjiBattle Animated VFX Asset Brief

## Purpose

Create animated transparent sprite assets that make the simple kanji characters feel powerful. The animation must remain readable on both smartphone and desktop screens and must not create visible atlas seams.

## Delivery Format

- PNG sprite sheet with RGBA transparency.
- One effect sequence per sheet, or clearly separated rows per effect.
- Recommended cell size: 256x256 for normal skills, 384x384 for boss-scale skills.
- Recommended frame count: 6 or 8 frames per sequence.
- Recommended frame rate: 12 or 15 fps.
- Include at least 24px transparent padding on every side of every frame.
- Do not allow glow, smoke, sparks, flames, or motion trails to touch a frame edge.
- Keep every frame the same pixel dimensions and center of action.
- Do not use a white, black, board, or colored matte behind the effect.
- Avoid baked-in text, kanji, damage numbers, UI, or background scenery.

## Animation Rules

Each sequence should contain a clear four-part arc:

1. Anticipation: a short charge or wind-up.
2. Action: the projectile, slash, flame, or breath moves with a strong direction.
3. Impact: a brighter, larger, more readable hit moment.
4. Dissipation: particles and glow disappear cleanly.

Use alpha fade and changing scale rather than only swapping colors. The first and last frames should have low alpha so the effect can be looped or interrupted safely.

## Required Sequences

### Projectile Skills

- Arrow: draw, release, three flight frames, impact, fade.
- Spear: thrust or straight throw, with a strong horizontal axis.
- Gun: muzzle flash, straight projectile, hit spark. Keep the travel direction code-controlled.
- Stone: lift, arc, impact dust, falling debris.

### Melee Skills

- Sword: wind-up, diagonal slash, bright impact, short residual arc.
- Hammer: overhead swing, contact flash, stun ring, fade.
- Claw: two clearly separated diagonal slashes, then a small impact burst.

### Elemental Skills

- Fireball: falling fireball, contact explosion, ember fade.
- Water heal: expanding blue ring, droplets, soft upward particles.
- Soil trap: ground crack or mound, trap activation, dust settling. Do not imply that every highlighted cell is damaged.
- Wood push: roots or timber thrusting in one direction, impact dust, retreat.

### Defense and Counter Skills

- Shield: shield flash, incoming impact deflection, small reflected spark.
- Armor: brief metallic shell or plate shimmer, then settle.
- Wall: rising wall fragment, block flash, small counter shockwave.

### Animal and Boss Skills

- Horse charge: dust trail and forward rush.
- Bird retreat: wing streak and backward motion.
- Dragon breath: mouth glow, expanding horizontal flame stream, hot impact, smoke fade.
- Dragon roar: expanding shockwave rings and a short screen-facing burst.

## Visual Style

- Dark transparent background with luminous colored effects.
- Strong silhouettes and high contrast at small display sizes.
- Keep the kanji character visible beneath the effect whenever possible.
- Use restrained color families: fire orange/red, water cyan/blue, soil green/brown, defense blue/white, boss red/purple.
- Reserve the largest bloom and particle count for impact frames and boss skills.

## Number Aura Sequences

Create a separate 3x3 sheet for persistent number auras:

- Rows: lower green, middle yellow, upper red.
- Columns: small, medium, maximum intensity.
- Each cell must be a complete ring and particle field with safe transparent margins.
- The aura should loop smoothly without changing the character position.
- Intensity is represented by particle count, glow radius, and ring brightness, not only by scale.

## Unity Import Requirements

- Texture type: Sprite (2D and UI).
- Sprite mode: Multiple.
- Fixed grid slicing; never automatic slicing.
- Pivot: Center.
- Filter: Bilinear.
- Mipmaps: Off.
- Keep the texture readable only if runtime frame extraction requires it; otherwise leave Read/Write disabled.
- Verify the first, middle, impact, and final frame in Unity and WebGL.

## Acceptance Checklist

- No visible neighboring-cell pixels at 100% scale.
- No glow or particle clipping at any frame edge.
- The effect origin stays stable across all frames.
- The action direction matches the skill's actual target direction.
- The effect remains recognizable on a narrow smartphone viewport.
- The character and kanji remain identifiable during the effect.
- The animation can be slowed for skill-cinematic playback without looking like a frozen image.
- License/provenance information is delivered with the asset set.

## Recommended Initial Batch

Start with four polished sequences before producing the full set:

1. Arrow flight and impact.
2. Fireball fall and explosion.
3. Sword slash.
4. Dragon breath.

Review these in the VFX gallery and in an actual WebGL battle before generating the remaining skills. This keeps the frame padding, scale, timing, and visual intensity consistent across the full asset set.
