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
- Stone: a heavy stone that rises and falls almost in place at the target cell. Avoid a strong diagonal flight or sideways travel; use only a small vertical bob and scale change to communicate weight. Make frame 6 the shatter peak: the stone breaks into visible chunks and dust. Remove the intact stone after that so frames 7–8 contain only dispersing fragments, lingering impact dust, and a fading ground mark.

### Melee Skills

- Sword: use a heavy, straight, double-edged greatsword as the motif for the 剣 character, not a curved single-edged katana. Support two readable variants. For a pivot slash, hold the hilt as the stable pivot and visibly rotate the blade through the attack. For the preferred left-to-right slash, place the user/attacker on the left and the target on the right in top-down view. Start with the straight blade tip pointing upper-right or straight up, then make a large readable clockwise motion through right-up -> right -> right-down -> down while the sword advances toward the target. The attack must visibly cross the space between attacker and target; it must not be a small local trail or a point-forward thrust. In the horizontal version, the blade must be visibly displaced between frames 1, 4, and 6. Keep the blade straight in every frame, especially the fourth frame; do not introduce an artificial bend or banana-shaped blade. Follow with a bright contact spark, a short residual arc, and settling dust.
- The left/right attacker-target relationship is a staging instruction only. Never draw the attacker, target, dummy, board, or any other scene object into the sword sprite sheet.
- Hammer: overhead swing, contact flash, stun ring, fade.
- Claw: four clearly separated claw marks per swipe, with the four lines kept readable and naturally irregular rather than perfectly parallel. Treat the character as right-handed: in the top-down local view, the claw enters from the upper-right and rakes diagonally toward the lower-left. The first contact may show only three short marks, matching the staggered placement of real claws; the fourth mark should join as the swipe develops. Each wound must visibly extend along the diagonal in sequence so the scratches grow longer and deeper across the frames. Make the two central claw wounds visibly thicker and deeper than the outer wounds, with more torn earth and debris around them; the outer wounds should remain narrower and lighter. Vary the lengths, widths, start positions, curvature, and timing of the four cuts; do not reveal the full marks at once. Use two diagonal downward-left swipes, a restrained contact burst, and a short dust fade; do not include a tiger body, paw, enemy, or target.

### Elemental Skills

- Fireball: falling fireball, contact explosion, ember fade.
- Water heal: expanding blue ring, droplets, soft upward particles.
- Soil trap: ground crack or mound, trap activation, dust settling. Do not imply that every highlighted cell is damaged.
- Wood push: a natural ground-born attack led by an irregular mass of thick roots. Keep the trunk or stump short at the origin; roots should emerge from the soil in uneven sizes and organic branching, overlap naturally, and form a heavy root arm without looking like a uniform braided rope or algorithmic bundle. Frame 2 should be the first clear emergence. Frames 3–6 should show the root mass making a broad lateral swing across the target area, with visible bending and changing tip position rather than thrusting straight toward the camera or target. Retract underground afterward. Include soil lift, heavy root flex, small leaves/splinters, and settling dust. Keep the local forward axis code-controlled and do not include a character or target.

### Defense and Counter Skills

- Shield: a large rugged inverted-pentagonal wooden-and-iron tower shield, with the broad edge at the top and a single point at the bottom, inspired by a heavy shield guardian archetype but without depicting a character. Keep the shield facing the viewer and the target directly, with no in-plane diagonal rotation; use an intermediate 30–45 degree elevated camera angle between frontal and top-down views, so both the shield face and its upper/side thickness are visible. When placed between source and target, it should read as a thin but recognizable, weighty frontal barrier while retaining the inverted-pentagonal profile. Do not bake an enemy, projectile, attack direction, or impact object into the asset; those are supplied by gameplay. Animate only shield deployment, a firm guard state, a restrained defensive glint, and return to idle. The local forward face should point toward the target and rotate with the character.
- Armor: a large close-up body-worn defensive overlay focused on a broad chest plate, shoulder guards, and a connected waist guard, not a shield. Show only those upper-body and waist armor parts at a readable scale, with an original design and no borrowed emblem or ornament. Use a clear three-stage appearance: first a faint, low-opacity silhouette of the armor appears, then the same armor becomes solid with about 6 to 7 large, sharp diamond-shaped star glints that feel like transient light rather than fixed decoration. Place only 1 or 2 glints naturally near the chest plate, with the remaining glints loosely scattered around shoulder and waist edges rather than in a symmetrical pattern. Finally the entire set fades from solid to translucent while the same restrained group of glints lingers before disappearing. Keep each glint large and crisp, but avoid filling the whole area so the armor remains the visual focus. Keep the kanji character readable behind or between the plates and avoid a long assembly sequence. Avoid circular radial rings, a central boss emblem, a tower-shield profile, projectiles, enemies, or a fully separate shield.
- Wall: all-direction enclosure effect. From a mostly top-down view, four separate rough stone wall segments should rise from cracks at the north, south, east, and west edges around the origin, leaving the center readable. Animate ground cracks, the four walls rising, a completed four-sided enclosure, then settling dust and fade. Do not include an enemy, projectile, attack object, impact flash, or directional strike; this is a reusable area-defense visual supplied with the runtime's four affected cells.

### Animal and Boss Skills

- Horse charge: use a reusable speed-only effect rather than a horse or hoof illustration. Use a thick, low horizontal stream of dust, sand, small debris, and short ground scuffs along the local forward axis; the runtime can layer the character and other effects over its center as needed. Build the smoke width and density during the charge, then let the stream trail off naturally. Do not include a horse body, hooves, enemy, or impact target; the character supplies the horse identity and the runtime rotates the speed effect.
- Bird retreat: leave a small cluster of loose feathers at the warp origin. Across a fixed 8-frame sheet, let the feathers rise or fall gently while drifting left and right, with slight rotation and staggered spacing; fade them out naturally by the final frames. Do not show a bird body, wings, destination trail, enemy, impact, beam, magic circle, or text. The runtime supplies the teleport movement and rotates the effect as needed.
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

Create nine separate animated sprite assets for NumberPassive aura states: lower weak, lower medium, lower strong; middle weak, middle medium, middle strong; upper weak, upper medium, upper strong. These states are selected by the skill activation condition and are not mapped to the numbers 1 through 9. A review contact sheet may arrange the nine assets together, but runtime assets must remain individually addressable. Each tier shares a material and color family, while the three strength states provide clear visual intensity differences.

- Rows: lower green, middle yellow, upper red.
- Do not create number-specific designs or mappings for 1 through 9. Keep the nine condition-based states explicit: three strengths within each of the lower, middle, and upper tiers. The aura animation should rotate in every state; strength is expressed through controlled differences in alpha, particle emission, glow radius, brush density, and orbit size.
- Replace the current simplified placeholder with a material-based aura: rough ink-brush arcs, small motes, and restrained warm highlights surrounding an empty center where the kanji remains readable. It should feel like a persistent power field, not a clean UI ring or a decorative badge.
- Each state asset should contain a fixed-frame loop of 8 frames arranged as a 2x4 sheet, with a complete aura field in every frame, safe transparent margins, and no character, number, text, or fixed emblem baked in. Keep the nine assets separate with generous spacing; do not use a tightly packed 3x3 production atlas.
- The aura should slowly rotate around the empty center in a seamless loop, with ink-brush arcs and motes drifting around the orbit at slightly different speeds. Keep the motion subtle enough for permanent display while making it feel alive rather than static. Do not pulse, pop, or expand dramatically.
- Make the three tiers clearly distinct in presence as well as color: lower should be a thin, quiet aura with sparse wisps and few motes; middle should have a readable rotating brush ring and moderate particles; upper should be deliberately more dramatic with a larger orbit, thicker swirling ink-smoke, more visible motes, and stronger warm sparks. Keep the upper tier localized around the character and never turn it into a full-screen flare.

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
