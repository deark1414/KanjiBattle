# KanjiBattle Animated VFX Asset Brief

## Purpose

Create animated transparent sprite assets that make the simple kanji characters feel powerful. The animation must remain readable on both smartphone and desktop screens and must not create visible atlas seams.

## 電・糸・鈴の単画像草案: 2026-10-25

- 対象は電の連鎖電撃、糸の引き寄せ、鈴の再響と繭。各素材を透過PNG1枚で表し、フレーム列やスプライトシートを新設しない。糸は直線＋巻き付き、繭は巻き付き＋保護状態、鈴は波紋＋音符の2素材を組み合わせる。位置・回転・伸縮・透明度で動かす現行方針を優先する。
- 電は左端から右端へ進む細長い折れ線状の稲妻。黄白色の芯と控えめな黄土色の縁。敵同士を順につなぐ使い方を想定し、放射状の爆発や巨大な発光を付けない。
- 糸は左から右へ張る白～生成りの一本の糸。糸巻き・針・キャラは含めず、細い撚りと小さなたるみで糸と読ませる。画像の左右端が接続点。対象との距離に合わせて長さを変え、引き寄せ後に消す。
- 鈴は波紋と音符を別画像にする。波紋は以前の音符なし原画へ戻し、独立した音符1枚を2～3個複製して、発動ごとに位置・大きさ・傾きを変えながら外側へ散らして消す。中央の漢字に重ならない距離を保つ。乱数はプレビューの装飾だけに使い、スキルの確率や対象選択に影響させない。方向変更や毎フレームの描画では散り方を引き直さず、発動単位で固定する。
- 巻き付きは白～生成りの糸が対象を取り囲む共通素材。糸の命中時は対象と一緒に動き、繭の発動時にも使う。結び目・針・キャラは含めず、中央の文字が見える余白を保つ。
- 繭は「巻き付き → 巻き付き演出が消える → 繭に囲まれた保護状態が残る」の順。巻き付きの画像をそのまま残すのではなく、別の白い蚕の繭状の保護画像へ切り替える。保護画像は外周に厚みと繊維を持たせ、中心には文字を読む透明な空間を残す。地面・草・葉・生き物は描かない。
- 共通: 意図的な128px相当のドット絵、透過、四辺の安全余白、文字なし。原画・確認用画像は`Assets/Art/Drafts/VFX/SupportSkills/`へ分離。元のVFX・モチーフ・コマは変更しない。
- この工程は画像と動作プレビューの制作。ゲームへの接続はレビュー承認後。生成元と全プロンプト、使用する軸・ピボット、透過・寸法の確認記録を保存する。
- 草案6枚・4種類のスキル例を制作。`Docs/Art/skill-vfx-preview.html`で単画像の伸縮・回転・拡大と、PC／スマホの表示を確認。透過・余白・固定寸法・4方向・再生操作、音符の発動ごとのばらつきと漢字への重なり防止、繭の巻き付き消去後に保護だけが残る順序の検査に成功。制作記録は`Docs/Art/SupportSkillVfx.md`と同名JSON。Unityへの接続・戦闘内検証はまだ行っていない。

## 追加6スキルの草案: 2026-10-25

- 次の対象は風・磁石・雨・鍛冶・要塞・砂。既存の電・糸・鈴・繭は維持し、同じ動作プレビューへ追加する。実ゲームへの接続は承認後。
- 風: 左から右へ流れる細い白緑の風筋。追風と向風は同じ画像の向き・位置で区別し、追風では対象の移動に添える。
- 磁石: 参考画像2枚目に合わせ、U字磁石の両極それぞれの先端から小さな半円波紋を出す。両極を対象に向け、中央から大きな波を放つ表現にはしない。波紋の発生後に対象を引き寄せる。既存U字モチーフと半円波紋の単画像を再利用し、発生位置・大きさ・移動距離だけを調整する。旧鉄粉・磁力線案の原画は履歴に残す。
- 雨: 回復範囲に小さな青緑の波紋を残し、既存の`WaterHeal/frame_03`の水滴を複製して下向きに降らせる。雲・十字・文字は描かない。
- 鍛冶: 既存ハンマーで対象を叩き、接触時に短い火花を出す。接触後だけ対象のコマを一時的に赤熱させ、元の色へ戻す。大きな炎は使わず、強化を示す演出にする。元ハンマー・コマ画像は変更せず、表示時の着色で表現する。
- 要塞: 投射物は既存の`Gun/frame_02`を再利用して弧を描かせ、着弾には小さな土煙・破片・控えめな火芯の素材を新作する。斜め上へ飛ぶ光線にはしない。
- 砂: 細かな砂粒が舞い上がる小さな砂煙。新しい分身の位置で拡大・消去し、後に砂のコマが現れる。人型の砂人形や新しいキャラ絵は描かない。
- 新規6画像はいずれも単画像・128px相当の透過ドット絵。元の雨滴・弾の原画は加工せず、確認用の別コピーを同じ出力処理へ通す。素材・参照・生成プロンプト・寸法・軸を記録する。フレーム切替なしで位置・回転・拡大・透過だけを動かす。
- 制作後の計14素材・10動作例をPC／スマホと4方向で検査。透過・安全余白・固定寸法・ドットの整合・再生操作が成功し、再利用元2枚の未変更も確認。雨の波紋は対象の足元に収まるよう縮小。草案のまま保持し、Unity戦闘への接続は未実施。

## 追加草案第3弾: 2026-10-25

- 波・草・炭・灯・影・賽子を追加する。既存10動作例は維持し、実ゲームへの接続は承認後。
- 波は右へ進む低い水の波頭で前方の敵を押す。草は根を抜かず地面から若芽が広がり、同じ草のコマが分裂して現れる。
- 炭は被弾後の赤い熾火と小さな火の粉で強化を表す。灯は既存の蝋燭モチーフと合わせられる暖色の小さな炎の揺らぎを使い、近くの味方を守る瞬間に出す。
- 影は人型でない薄い影煙から対象のコマへ変化する。賽子は既存の賽子モチーフを転がし、確定した目に応じ回復・防御・攻撃の演出へつなぐ。星状の小さな金の輝きは決定時だけ使う。
- 新規6素材は128px論理グリッドの透過単画像。フレーム切替は使わず、位置・拡大・回転・透過で動かす。草案のまま保持し、元モチーフ・コマ・本番VFXは変更しない。
- 草案は合計23素材・16動作例。透過・余白・固定寸法・ドット整合、4方向、PC／スマホ、賽子の6出目と効果分類の検査に成功。追加・変更の25系統中16系統を制作済みで残り9系統。内訳と対象範囲は`Docs/Art/SkillVfxProgress.md`。

## 炭の状態切替・賽子の出目: 2026-10-25

- 炭は消火と燃焼の2状態を切り替える。既存の燃焼モチーフは維持し、その同じ炭の配置・形のまま火と赤熱だけを除いた消火画像を作る。被弾後に燃焼へ切り替え、火の粉は切替時だけ。連番アニメではなく状態ごとの単画像とする。
- 賽子は既存の数字モチーフ1〜6をそのまま使う。装飾専用の固定シードでランダムな切替列を作り、徐々に切替間隔を長くして出目を確定。確定した画像を静止保持し、その周囲に光を重ねる。表示中の画像と見出しの数値・効果を一致させる。一時停止や方向変更では乱数を引き直さない。
- 元のモチーフは上書きせず、草案コピーを制作する。ゲームへの接続は承認後。

## Runtime Presentation Update: 2026-09-20

- In battle, default to one approved opaque representative sprite per effect and animate that single sprite with runtime position, rotation, scale, and fade. This avoids the frame-switching and slice-boundary failures previously observed in WebGL. When an attack defeats a character, keep that character visible until its skill VFX has completed, while removing it from gameplay targeting immediately.
- Arrow and Gun: fly one arrow or one enlarged bullet straight from caster to destination. Stone: fly one intact heavy stone in a clear upward arc from caster to target, then let the target-side impact feedback communicate the landing. Do not use a cracked or shattering frame as the travelling stone.

## Army Practice Target: 2026-09-26

- The roster screen uses a dedicated, static, transparent-background Japanese practice scarecrow asset. It is a small weathered wooden post-and-crossbar wrapped in pale straw, viewed almost straight-on with a slight top-down game-UI angle. It has no person, weapon, text, banner, building, ground plane, backdrop, glow, or floating particles.
- The target must read clearly beside a shogi piece at roughly 72px high. Keep the silhouette compact and vertically weighted, with an uneven straw head and a rough cedar post, so the interaction is legible without a card-shaped background.
- The roster remains a plain piece list. Place practice in its own wide band between the army-status information region and the list, borrowing a small amount of the list's upper space: use only two large adjacent cells, with one randomly selected owned piece on the left and its non-attacking scarecrow on the right. Do not label this field; the large piece, scarecrow, and pair of cells make the purpose clear. Each non-number piece takes a short step toward its scarecrow, displays its reviewed skill over about one second, then returns to its lane; do not start a live `BattleManager` match or introduce its rewards, defeat state, traps, or target selection. Existing reviewed representative battle VFX travel from the displayed piece to its own target. Number-passive pieces always briefly show their own existing aura around the owner, independent of the current party's number-combination condition. Sword practice must retain the normal battle's 120-degree, cubic-accelerated sweep and direct texture path rather than falling back to a generic travelling Slash icon. Award only a tiny throttled shared experience reward after a complete drill; practice must never replace stage progression.
- Recruitment uses the same reviewed representative sprite chosen by the joining character's actual skill. The piece casts once from its roster position toward the lower screen before it becomes an ally: directional sources rotate from their documented local-right axis toward that downward travel. Map Counter to Shield, AreaCounter to Wall, Number1/2/3 to their own low/mid/high aura families, and Dragon to DragonBreath rather than a generic Slash or DragonRoar.
- Fireball and WaterHeal both fall as one sprite directly downward without in-plane rotation. WaterHeal uses only clear vertical water droplets in open air, with no landing ripple, splash, rebound droplets, or stationary blinking overlay. Wood grows outward from the caster at a clearly readable scale. Soil appears briefly at placement, while a muted earth-tone cell tint remains for the lifetime of the gameplay trap. Shield, Armor, and Wall are one sprite each with a brief deployment/pulse/fade.
- Horse uses the reviewed representative `Animated/HorseCharge/frame_02`: a warm beige, directional charge-dust burst that follows the horse briefly and fades after reaching the destination. Keep its local-right facing, current silhouette, transparent padding, and material; do not substitute an alternate static cloud, forked wind, wake, or new interpretation. Bird uses the reviewed representative `Animated/BirdRetreat/frame_02` at both the warp origin and destination. Preserve its current feathers and scattered light dust exactly; the origin rises vertically and fades, while the destination falls vertically from above and fades after landing. Keep its maximum opacity low enough that it reads as a trace rather than a large magic effect. Do not add a bird body, smoke, ground debris beyond the reviewed source, a diagonal trajectory, or rotation. Tiger uses the same claw image twice, with the second stroke rotated across the first so the two swipes clearly form a cross even when the target dies on the first hit.
- Direction correction (2026-10-02): `Animated/HorseCharge/frame_02` is authored facing local right. Runtime rotates it 180 degrees from travel and anchors it behind the moving horse, so the charge dust reads as a rear wake rather than a forward projectile.
- Direction correction (2026-09-21): the dragon-breath source points local right: its narrow left edge is the mouth-side origin and its broad right edge is the forward flame volume. Runtime must only convert the downward-growing board Y axis to the upward-growing UI Y axis before rotating the source; do not apply a 180-degree flip.
- Origin correction (2026-09-21): anchor DragonBreath at the source image's local left-center, aligned with the dragon, and scale only outward along the local right axis. Never scale it from the 3x3 area's center, which makes flame visibly grow backward through the dragon.
- Layer correction (2026-09-21): DragonBreath is a global attack VFX and must render in front of the dragon character. Global attack VFX use a battle overlay with a higher sorting order than character canvases; Shield, Armor, and Wall remain direct character children and therefore beneath global attack effects.
- Number auras use one complete aura sprite rotating around the owner. Horse charge, Bird retreat, Tiger claw, Dragon breath, and Dragon roar also use one complete effect sprite with code-driven travel, drift, expansion, or rotation.
- Sword, Hammer, and Spear retain their reviewed bespoke motion paths, but each path renders one opaque source image: Sword and Hammer rotate around a caster-side pivot, while Spear grows forward from its fixed butt. The sword and hammer must share the grounded Japanese-fantasy material language: use an aged blue-green bronze straight double-edged sword and a plain wooden mallet, not a European longsword or war hammer. Do not use a generic sequence player for them.

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
- Spear: two-cell piercing thrust. Use a mostly top-down view with a short rearward ready pose, then extend the full spear straight along its local forward axis so the shaft and spearhead visibly cover the two cells ahead. Avoid diagonal-only poses; the runtime rotates the straight sequence to the attack direction. The output must be an 8-frame sheet with one fixed, transparent cell per frame: leave generous transparent space to the right of the weapon, pin the butt/end of the shaft to the exact left-center of every frame, and never move that anchor. The local pose points to the right. Frames 1-5 advance only the tip away from the fixed butt; frames 6-8 keep the butt fixed while the spear retracts slightly and fades. Do not add soil, ground bursts, or a target-side hit effect: the skill can pierce multiple enemies. Add two or three short, pale gray air wisps close to and parallel with the shaft during the thrust; they must read as air cut by speed, never as dust rising from the board or a futuristic beam. Keep the wisps more subdued than the spear: their largest state is no larger or denser than the current reference's seventh frame. In every frame, the air effect begins at the fixed butt and ends no farther forward than that frame's spear tip. No glow lines. Do not let the weapon dissolve around its midpoint.
- Gun: prioritize the projectile rather than repeatedly animating the gun. Use one recoil/muzzle-flash frame, then a clearly enlarged round bullet traveling on a perfectly straight local axis, followed by a restrained impact spark/dust frame that can be replayed at each pierced target. The runtime applies the effect to every target on the line; the sprite must not imply a single-target range.
- Stone: a heavy stone that rises and falls almost in place at the target cell. Avoid a strong diagonal flight or sideways travel; use only a small vertical bob and scale change to communicate weight. Make frame 6 the shatter peak: the stone breaks into visible chunks and dust. Remove the intact stone after that so frames 7–8 contain only dispersing fragments, lingering impact dust, and a fading ground mark.

### Melee Skills

- Sword: use an original Japanese-fantasy bronze tsurugi as the motif for the 剣 character: a straight, double-edged blade with aged blue-green bronze, a small simple guard, a wrapped dark handle, and a few restrained uneven notches or waves along the blade edge. The silhouette should be distinct from a European longsword and must not copy a named or extant sword. For a pivot slash, hold the hilt as the stable pivot and visibly rotate the blade through the attack. For the preferred left-to-right slash, place the user/attacker on the left and the target on the right in top-down view. Start with the straight blade tip pointing upper-right or straight up, then make a large readable clockwise motion through right-up -> right -> right-down -> down while the sword advances toward the target. The attack must visibly cross the space between attacker and target; it must not be a small local trail or a point-forward thrust. In the horizontal version, the blade must be visibly displaced between frames 1, 4, and 6. Keep the blade straight in every frame, especially the fourth frame; do not introduce an artificial bend or banana-shaped blade. Follow with a bright contact spark, a short residual arc, and settling dust.
- The left/right attacker-target relationship is a staging instruction only. Never draw the attacker, target, dummy, board, or any other scene object into the sword sprite sheet.
- Hammer: use a plain Japanese wooden mallet: one solid, slightly tapered hardwood head on a single unadorned wooden handle, with visible grain and no iron bands, rivets, heraldic emblem, or European war-hammer fittings. The runtime presentation must be a clear one-cell smash: pin the butt/end of the handle at the caster-side pivot, raise the hammer head behind and above that pivot, then swing it in a pronounced arc down onto the selected single target cell. Keep this swing within 60 to 90 degrees (currently 78 degrees), and horizontally mirror the left-facing motion so both left and right attacks enter from the board's upper side. Do not translate the hammer head straight down from above the target; that reads as free fall rather than a weapon swing. Do not use the sword's pivot sweep or imply a three-cell area. Use a very short fade after impact; the target shake and stun state communicate contact.
- 2026-09-18 battle simplification: create separate weapon-focused production sheets for Sword and Hammer. Each sheet is exactly 8 isolated frames in a 2x4 grid on genuine RGBA transparency, with generous fixed padding and a stable center. The Sword sheet contains only a straight, double-edged iron greatsword sweeping from upper-left toward lower-right; the Hammer sheet contains only a rugged iron war hammer rising from lower-left toward upper-right. Use a restrained steel glint or faint air wisp only. Do not include dirt, dust, smoke clouds, rocks, ground cracks, impact bursts, characters, boards, or text. These assets are flipped by runtime when the target is left of the attacker.
- Claw: four clearly separated claw marks per swipe, with the four lines kept readable and naturally irregular rather than perfectly parallel. Treat the character as right-handed: in the top-down local view, the claw enters from the upper-right and rakes diagonally toward the lower-left. The first contact may show only three short marks, matching the staggered placement of real claws; the fourth mark should join as the swipe develops. Each wound must visibly extend along the diagonal in sequence so the scratches grow longer and deeper across the frames. Make the two central claw wounds visibly thicker and deeper than the outer wounds, with more torn earth and debris around them; the outer wounds should remain narrower and lighter. Vary the lengths, widths, start positions, curvature, and timing of the four cuts; do not reveal the full marks at once. Use two diagonal downward-left swipes, a restrained contact burst, and a short dust fade; do not include a tiger body, paw, enemy, or target.

### Elemental Skills

- Fireball: falling fireball, contact explosion, ember fade.
- Water heal: one clear vertical water droplet, falling straight down onto the healed target. Do not include a ripple, splash, rebound droplets, expanding ring, upward particles, or a stationary blink.
- Soil trap: ground crack or mound, trap activation, dust settling. Do not imply that every highlighted cell is damaged.
- Wood push: a natural ground-born attack led by an irregular mass of thick roots. Keep the trunk or stump short at the origin; roots should emerge from the soil in uneven sizes and organic branching, overlap naturally, and form a heavy root arm without looking like a uniform braided rope or algorithmic bundle. Frame 2 should be the first clear emergence. Frames 3–6 should show the root mass making a broad lateral swing across the target area, with visible bending and changing tip position rather than thrusting straight toward the camera or target. Retract underground afterward. Include soil lift, heavy root flex, small leaves/splinters, and settling dust. Keep the local forward axis code-controlled and do not include a character or target.

### Defense and Counter Skills

- Shield: a simple upright Japanese hand shield (mochidate): a moderately tall portable wood board that protects the owner's upper body. Give it subtly tapered sides and a gently bowed top edge. Use pale unfinished hinoki- or cedar-like wood with a slim imperfect raised wood perimeter. Paint two bold black-ink horizontal bands across the upper third, plus one restrained original flowing brush motif to break up the lower board; avoid any recognizable family crest. Do not use vertical plank seams, bolts, a handle, hinges, a point-bottom tower-shield outline, a wide panel, metal border, boss, heraldry, or emblem. It should read as a small hand-held shield directly in front of the owner, not a door or architectural panel. Do not bake an enemy, projectile, attack direction, or impact object into the asset; those are supplied by gameplay.
- Armor: a compact Japanese lamellar armor overlay focused on overlapping blackened iron and deep indigo-laced chest plates, large shoulder guards, and a connected kusazuri waist guard. Show only the upper-body and waist armor at a readable scale, with no character, helmet, crest, star effect, borrowed clan mark, or central emblem. The single-sprite runtime presentation has no independent sparkle effect: the armor appears solid, holds briefly, and fades as a whole. Keep the kanji character readable behind or between the plates and avoid a long assembly sequence.
- Wall: one frontal Japanese castle stone-base (ishigaki) segment that appears directly over the owner character, matching the shield and armor ownership/layering rule. Show only the lower sloped stone foundation of a castle: a broad base of large, naturally fitted irregular gray stones tapering slightly toward a shorter top edge. Vary each stone's polygonal silhouette, size, height, and mortar seam so it reads as hand-fitted ishigaki rather than a low pile or repeated brick grid. Do not draw the castle tower, roof, gate, battlements, ground plane, or vegetation. Runtime makes it pop upward from below the owner, settle very briefly, then fade. Do not use an all-direction enclosure, a ring of four segments, a projectile, enemy, impact flash, or directional strike.

### Animal and Boss Skills

- Horse charge: use a reusable directional forked wind-and-dust effect rather than a horse or hoof illustration. From a directly overhead tactical-board perspective, begin near the local left-center and split into two unequal flattened, curved dust-and-air branches that flow toward the local upper-right and lower-right. Prevent dead straight gaps by adding a faint short recirculating curl through the center and thin drifting grit along long runs. Add only three or four secondary wisps sampled within a constrained band around that forward flow: keep a readable center, minimum spacing between forms, and lower density behind the horse. Place a few small off-axis clumps beyond the main contours, but not enough to make a separate row. Each front tip should shred into unequal particles and ragged wisps, never a round-ended blob. Make each secondary shape differ in curvature and size, but never let the scatter erase the two primary directions or become a row of repeated ornaments. It must read as pressure opening in front of a fast charge, not smoke expelled from its rear. Do not make it a thin straight speed line, a long tail, a projectile, a side-view cloud, a ground plane, or an impact explosion. Do not include a horse body, hooves, enemy, or target; the character supplies the horse identity and the runtime rotates the right-facing asset into the travel direction.
- Bird retreat: use one clean, transparent cluster of loose feathers with no smoke, dust, ground debris, or trail. At the warp origin it rises straight upward and fades. At the destination it begins directly above the cell, falls straight downward, and fades after arriving. Keep the feather image upright in runtime: do not rotate it or send it diagonally. Do not show a bird body, wings, enemy, impact, beam, magic circle, or text.
- Dragon breath: effect-only sequence with no dragon head or character artwork. Start from a clear source point on the dragon's side, then expand a grounded cone of flame, smoke, embers, and heat distortion toward the front across the full affected area. The source is supplied by the game character; the sprite must describe the breath volume and its impact, not the dragon.
- Dragon roar: effect-only radial shockwave for a future or sub-effect of the Dragon skill. From an empty center supplied by the dragon, show a compact pressure burst, then one or two expanding rough dust-and-air shockwave rings with scattered ground grit, followed by a short settling fade. Keep the effect mostly top-down and centered, with a brief screen-facing emphasis supplied by the runtime if needed. Do not include the dragon, head, mouth, breath flame, projectile, enemy, text, neon beam, or magic circle.
- Boss summon: a larger arrival effect for a boss entering the battle. Start with hairline cracks and a compact ground disturbance at the spawn cell, then raise a dense red-black dust and smoke column with a few ember sparks and stone fragments, peak in a broad grounded burst, and settle quickly so the boss remains visible. Use a mostly top-down view and keep the center open enough for the boss character to appear above the effect. Do not include the boss body, dragon head, face, weapon, enemy, text, neon beam, portal, magic circle, or a clean fantasy summoning seal.

## Visual Style

### ドット絵化の優先範囲: 2026-10-02

- 今回の置換対象は、キャラクター駒に添える意味モチーフと技能 VFX のみとする。施設、行軍背景・拠点、案山子、盤面フレームは対象外であり、既存素材を変更しない。
- 駒の漢字、HP、HP バー、所属色を覆わない。モチーフは 256 x 256 px の透明背景 PNG を基本とし、駒の外側へ短く現れる補助表現に留める。漢字札そのものの可読性を落とす置換はしない。
- 鳥の上乗せモチーフは小鳥ではなく、翼を広げて滑空するタカを使う。全身を描き、駒の上側へ短く現れる。頭部と翼端は黒、胴体と翼の内側は白を基調にして、鳥の種類を小さな表示でも判別できるようにする。
- 新規 VFX は意図的に低解像度のドット絵調へ統一する。太い輪郭、少ない色数、強い小サイズのシルエットを用い、写真風の陰影、滑らかな発光、細密な AI 風の質感を避ける。
- 既存の `SkillVfxMotion` と `BattleManager` の発生位置、射程、回転、フェード、表示時間、当たり判定は変更しない。各技能の新素材は既存フォルダ名と原点を維持し、素材だけを差し替える。
- 虎の `TigerTwinClaw` は、透明背景の四本爪痕のみを一撃分として作る。虎の体・脚・顔・文字は描かず、ランタイムが二撃目を回転させて交差させる既存の挙動を維持する。
- ドット絵素材は最終確認まで `Assets/Art/Drafts/` に置き、`Assets/Resources/VFX/` への配置とゲーム画面への接続はレビュー承認後に行う。
- VFX はコンタクトシートや共通スプライトにまとめず、技能フォルダごとに一枚ずつ下書きを用意する。プレビューでは駒モチーフと並べて、透過、中心原点、余白、最小表示時の輪郭を個別に確認する。
- 既存 VFX に対応するドット絵版は、新規の解釈で描き直さない。元の PNG を編集入力にし、対象物、角度、シルエット、キャンバス内の位置、透明余白、右向きなどの向きを固定したうえで、描画だけをドット絵へ変換する。とくに水は現行どおり水玉、盾は木札と墨線の和風意匠を維持する。馬は右端の目立つ欠けだけを一続きの砂埃へ補修してよく、鳥は現行の三枚の淡い羽を維持し、黒い縁取りを足さない。
- 今回の全技能候補は、ランタイムが使う代表 PNG を 4 分の 1 程度へ縮小してから最近傍で元サイズへ戻す。透明度、原点、余白、実装時の向きを保持し、候補同士で別の画像を流用しない。
- 数字の代表エフェクトは、現在の円形オーラを中央の駒を空けた小さな火の玉の周回表現へ置き換える候補を作る。下位・中位・上位はそれぞれ緑・金・赤で区別し、弱・中・強はそれぞれ火の玉一つ・二つ・三つで表す。素材内にラベルや文字は入れない。火の玉は外周に等間隔で置き、既存の共通 VFX モーションに合わせて反時計回りに周回する。火球の頭を進行方向へ向け、尾はその反対側へ円弧状に長く流す。三つ版では120度間隔の三つの長い尾がほぼ一周を描き、中心の駒は空けたまま回転方向を明確にする。

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

Create nine separate animated sprite assets for NumberPassive aura states: lower weak, lower medium, lower strong; middle weak, middle medium, middle strong; upper weak, upper medium, upper strong. These states are selected at each side's turn start from other living numeric types across all three tiers and are not mapped to the numbers 1 through 9. A review contact sheet may arrange the nine assets together, but runtime assets must remain individually addressable. Each tier shares a material and color family, while the three strength states provide clear visual intensity differences. Runtime uses one reviewed opaque representative frame with code-driven rotation, scale, and fade; it must not resume frame-by-frame switching without WebGL proof.

- Rows: lower green, middle yellow, upper red.
- Do not create number-specific designs or mappings for 1 through 9. Keep the nine condition-based states explicit: three strengths within each of the lower, middle, and upper tiers. The aura animation should rotate in every state; strength is expressed through controlled differences in alpha, particle emission, glow radius, brush density, and orbit size.
- Replace the current simplified placeholder with a material-based aura: rough ink-brush arcs, small motes, and restrained warm highlights surrounding an empty center where the kanji remains readable. It should feel like a persistent power field, not a clean UI ring or a decorative badge.
- Each state asset should contain exactly 8 frames arranged as a 2x4 sheet, including every strong state, with a complete aura field in every frame, safe transparent margins, and no character, number, text, or fixed emblem baked in. Keep the nine assets separate with generous spacing; do not use a tightly packed 3x3 production atlas.
- The aura should slowly rotate around the empty center in a seamless loop: thick/dark brush masses and thin/light gaps must visibly travel around the orbit from frame to frame, rather than the whole ring remaining static. Ink wisps and motes may drift at slightly different speeds. Keep the motion subtle enough for permanent display while making it feel alive rather than static. Do not pulse, pop, or expand dramatically.
- Make the three strength states clearly distinct in form as well as color: weak should use broken thin brush strokes, large transparent gaps, and very few motes; medium should use a readable rotating brush arc with alternating thick and thin sections plus moderate wisps; strong should use several thick rolling ink-smoke masses, smaller gaps, more layered wisps, and stronger warm sparks. The difference must not be only a hue change. Keep the strong state localized around the character and never turn it into a full-screen flare.

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

## Runtime Display Notes: 2026-09-20

- Water uses one restrained opacity cycle between translucent and nearly transparent for roughly one second. Keep it stationary over the healed target; do not turn it into a fast flicker or a long lingering effect.
- The high-tier strong number aura uses the clean seventh source frame and a centered safe crop with a 6% border removed at runtime. This excludes residual pixels at the edge of the generated source while keeping the core ink-and-flame aura intact.
- Keep visual motion in `SkillVfxMotion`: battle and army-practice views share representative-frame selection, direction conversion, three-layer weapon rendering, sword acceleration, hammer gravity swing, and end-only fade. Each screen may choose its own size and duration for legibility, but it must not duplicate or alter the motion equations.
