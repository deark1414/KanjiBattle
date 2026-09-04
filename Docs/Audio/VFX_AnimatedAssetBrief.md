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

## Art Direction

- Use a mostly top-down view so the effect can be rotated to match the actual attack direction.
- Keep the visual center and the origin point stable across every frame; rotation must happen around the gameplay origin, not around a changing visual center.
- Do not bake screen direction into reusable effects. A projectile or slash should have a clear local forward axis, while the game determines its final rotation.
- For real-world objects, include the object itself in the sprite: a recognizable sword blade, spear, shield, hammer head, arrow, stone, claw, or timber shape is preferable to an abstract streak alone.
- Use lighting, trails, sparks, impact rings, and particles to make the simple kanji character feel powerful, while keeping the character readable underneath the effect.
- Separate the object silhouette from the supporting glow and particles so the object remains legible at small smartphone sizes.
- When an effect is attached to a character, keep the character's kanji as the visual anchor and let the effect expand around it without permanently obscuring it.

### Rotation and Direction

- Author directional assets facing a documented local direction, preferably upward or to the right, and record that direction in the delivery notes.
- Test the same sequence at 0, 90, 180, and 270 degrees. The object, trail, and impact should rotate together without stretching or changing scale.
- Effects that are inherently radial, such as shields, auras, explosions, and roar shockwaves, should remain rotation-neutral.
- Long effects such as dragon breath, spear throws, and gun shots should use a stable local axis and enough transparent margin at both ends for rotation.

### Object Priority

When an effect contains a real object, preserve this order of readability:

1. Object silhouette and direction.
2. Impact or activation point.
3. Supporting glow, trail, sparks, smoke, or dust.
4. Very bright bloom only at the key impact frame.

Do not let bloom or particles erase the object in every frame. The most spectacular frame should be the impact frame, not the entire sequence.

## Animation Rules

Each sequence should contain a clear four-part arc:

1. Anticipation: a short charge or wind-up.
2. Action: the projectile, slash, flame, or breath moves with a strong direction.
3. Impact: a brighter, larger, more readable hit moment.
4. Dissipation: particles and glow disappear cleanly.

Use alpha fade and changing scale rather than only swapping colors. The first and last frames should have low alpha so the effect can be looped or interrupted safely.

## Required Sequences

### Projectile Skills

- Arrow: show only the reusable arrow effect: a short wooden shaft, clearly separated fletching/羽根, and a smaller arrowhead. Do not include a bow, archer, or launch platform. The silhouette must read as an arrow rather than a spear or generic glowing bolt. Use nock/release, three flight frames, impact, and fade; include a restrained dust or air trail behind the fletching.
- Spear: two-cell piercing thrust. Use a mostly top-down view with a short rearward ready pose, then extend the full spear straight along its local forward axis so the shaft and spearhead visibly cover the two cells ahead. Avoid diagonal-only poses; the runtime rotates the straight sequence to the attack direction. Include a clear extend, full reach, restrained impact, and retract sequence.
- Gun: prioritize the projectile rather than repeatedly animating the gun. Use one recoil/muzzle-flash frame, then a clearly enlarged round bullet traveling on a perfectly straight local axis, followed by a restrained impact spark/dust frame that can be replayed at each pierced target. The runtime applies the effect to every target on the line; the sprite must not imply a single-target range.
- Stone: lift, arc, impact dust, falling debris.

### Melee Skills

- Sword: use a heavy, straight, double-edged greatsword as the motif for the 剣 character, not a curved single-edged katana. Support two readable variants. For a pivot slash, hold the hilt as the stable pivot and visibly rotate the blade through the attack. For the preferred left-to-right slash, place the user/attacker on the left and the target on the right in top-down view. Start with the straight blade tip pointing upper-right or straight up, then make a large readable clockwise motion through right-up -> right -> right-down -> down while the sword advances toward the target. The attack must visibly cross the space between attacker and target; it must not be a small local trail or a point-forward thrust. In the horizontal version, the blade must be visibly displaced between frames 1, 4, and 6. Keep the blade straight in every frame, especially the fourth frame; do not introduce an artificial bend or banana-shaped blade. Follow with a bright contact spark, a short residual arc, and settling dust.
- The left/right attacker-target relationship is a staging instruction only. Never draw the attacker, target, dummy, board, or any other scene object into the sword sprite sheet.
- Hammer: overhead swing, contact flash, stun ring, fade.
- Claw: two clearly separated diagonal slashes, then a small impact burst.

### Elemental Skills

- Fireball: falling fireball, contact explosion, ember fade.
- Water heal: expanding blue ring, droplets, soft upward particles.
- Soil trap: ground crack or mound, trap activation, dust settling. Do not imply that every highlighted cell is damaged.
- Wood push: roots or timber thrusting in one direction, impact dust, retreat.

### Defense and Counter Skills

- Shield: a large rugged five-sided wooden-and-iron tower shield, inspired by a heavy shield guardian archetype but without depicting a character. Do not bake an enemy, projectile, attack direction, or impact object into the asset; those are supplied by gameplay. Keep the face readable from above with a central boss and metal rim. Animate only shield deployment, a firm guard state, a restrained defensive glint, and return to idle. The pentagonal silhouette must remain stable while the effect rotates with the character.
- Armor: brief metallic shell or plate shimmer, then settle.
- Wall: rising wall fragment, block flash, small counter shockwave.

### Animal and Boss Skills

- Horse charge: dust trail and forward rush.
- Bird retreat: wing streak and backward motion.
- Dragon breath: effect-only sequence with no dragon head or character artwork. Start from a clear source point on the dragon's side, then expand a grounded cone of flame, smoke, embers, and heat distortion toward the front across the full affected area. The source is supplied by the game character; the sprite must describe the breath volume and its impact, not the dragon.
- Dragon roar: expanding shockwave rings and a short screen-facing burst.

## Visual Style

- Aim for a rugged shogi-board fantasy style: carved wood, forged iron, worn leather, paper, soil, smoke, sparks, embers, and water droplets.
- Use directional light, contact shadows, debris, dust, and practical-looking impact flashes instead of sci-fi beams.
- Strong silhouettes and high contrast at small display sizes.
- Keep the kanji character visible beneath the effect whenever possible.
- Use restrained material-based color families: fire orange/red, water blue/cyan, soil olive/brown, defense iron/blue-white, boss vermilion/deep violet.
- Keep glow localized and warm. Avoid laser lines, holograms, neon outlines, lens-flare-heavy beams, floating magic circles, and clean futuristic energy ribbons.
- Prefer hand-drawn brush edges, chipped metal, rough wood grain, smoke, sparks, and uneven particle sizes over perfectly smooth geometric effects.
- Ground shadows should be soft, short, and attached to the contact area. Do not let a large detached shadow imply that the sword or effect is floating.
- Reserve the largest particle count and brightest flash for impact frames and boss skills; the overall effect should still feel physical and grounded.

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

## Asset Identity and Non-Replacement Rules

- Keep arrow, sword, spear, gun, and other effects as separate named assets. Creating a new sword sequence must never replace or rename the existing arrow asset.
- Use explicit filenames such as `arrow_attack`, `sword_slash`, and `dragon_breath_area` and keep a small manifest with the skill mapping.
- Dragon breath is an area effect and must be authored without the dragon body; the runtime places it at the dragon's origin and rotates it toward the target direction.

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
