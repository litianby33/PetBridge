# 默认角色生成提示词

## v1.2.2：行走改为更粗的像素风格

仅重绘左右行走两行，其他动作保留。使用内置图像工具，运行速度改为 4 fps。

```text
Use case: style-transfer.
Asset: replacement TWO walking rows of an existing pixel-art mascot atlas. IMAGE 1 is the CORRECT PIXEL STYLE reference (the six-row sprite sheet, especially its first standing row). IMAGE 2 is the pose/choreography reference but is TOO SMOOTH AND HIGH-DETAIL; do not copy its shading or smooth contours.
Draw the same silver-grey-haired dragon girl in charcoal with muted lavender accents, horns, small knot hair clip, wings, curled tail, purple eyes and natural tiny hands. Exactly FOUR columns x TWO rows = eight full-body sprites. Top row walk screen right, bottom row walk screen left. Preserve clear alternate leg cycle: crossed-stride contact, passing leg raised, opposite uncrossed-stride contact, opposite leg raised. Make column3 limb occlusion genuinely exchange from column1; column4 exchanges anatomical raised leg from column2. Same steady head size, face, hips, ground baseline, no blink jitter.
ABSOLUTELY REQUIRED STYLE: coarse, hand-placed low-resolution pixel game sprites matching image1's idle pixels. Each character should look drawn on an approximately 80x80 logical pixel grid then enlarged with NEAREST NEIGHBOR to about 240x240. Each logical pixel is a plainly visible SQUARE color tile about THREE screen pixels wide. Every outline has stair-step segments and square corners, even around hair, horns, cheeks, clothes and tail. Use discrete opaque flat color clusters, 24-32 colors total, only 2-3 shade levels per material. Small boots and hands simplified into chunky pixel clusters. Keep face readable but DO NOT render glossy detailed anime eyes, hair strands or smooth jewelry. Contours must be blocky pixel steps, NOT continuous thin curved antialiased lines. No painted gradients, no soft semitransparent contours, no smooth anime chibi illustration, no vector linework, no lighting bloom. This request is a TRUE PIXEL SPRITE, not a digital illustration labeled pixel style.
Intended sheet 1024x512, each square cell256x256; character about210px high with generous transparent margin, all extremities safely inside each cell. No text, no grid, no arrows, no motion lines, no ground shadow. Genuinely transparent alpha outside crisp sprite pixels. Same character palette as image1; don't redesign outfit or change anatomy. Exactly one sprite per cell and no neighbor overlap.
```

## 行走专项修正（v1.2.1）

仅重新生成 4×2 行走图，替换原 atlas 的第 1、2 行；其余像素逐字节保持。先后检查了两轮；最终采用下面第二轮结果，保留旧稿以便比较。

### 第一轮

```text
Use case: precise-object-edit.
Asset: replacement walking animation sprite sheet for the referenced pixel desktop dragon girl. Image 1 is the approved corrected identity master; image 2 shows the existing actions with INCORRECT almost-identical walk legs. Keep her approved silver-grey hair, two horns, charcoal outfit with muted purple accents, wings, curled tail, purple eyes, tiny knot hair clip and pixel-art density. Repair ONLY walking choreography; do not redesign the character.
Output exactly FOUR COLUMNS and TWO ROWS = EIGHT full-body poses, regular square cells, transparent background, no text or guides. Intended canvas 1536x768. Row 1 faces three-quarter toward SCREEN RIGHT; row 2 faces three-quarter toward SCREEN LEFT. All characters same size, torso registration and boot ground line, room around horns/wings/tail.
CRITICAL: one whole STRIDE takes four pictures. Columns 1 and 3 must visibly be OPPOSITE contact poses with the identity of the leading leg EXCHANGED. Columns 2 and 4 must be opposite passing poses. Do NOT just repeat one bent-knee pose with hair/eye jitter.
ROW1 screen-right walk:
C1: anatomical NEAR leg stretched FORWARD to the RIGHT, heel touching ground at x=240; FAR leg stretched BACK to the LEFT, toe touching ground at x=148. Clear wide leg stride. Near arm back, far arm forward.
C2: near leg supports directly below hip at x=192; far knee swings forward to RIGHT, boot clearly lifted off ground and passes the standing ankle. Narrow passing silhouette. Torso rises only slightly.
C3: FAR leg now stretched FORWARD to RIGHT at x=240; NEAR leg stretched BACK to LEFT at x=148. Exchange the near/far leg placement and occlusion from C1. Near arm forward, far arm back.
C4: FAR leg supports below hip; NEAR knee swings forward to RIGHT with lifted boot. Opposite passing silhouette from C2.
ROW2 screen-left walk:
C1: NEAR leg stretched FORWARD to LEFT; FAR leg stretched BACK to RIGHT. Near arm back, far arm forward.
C2: near leg supports below hip; far knee swings forward LEFT, boot lifted and crossing past standing ankle.
C3: FAR leg stretched FORWARD LEFT; NEAR leg stretched BACK RIGHT; visibly exchange leg depth/occlusion. Near arm forward, far arm back.
C4: far leg supports below hip; near knee swings LEFT, boot lifted and crossing past standing ankle.
Use the same dark thigh-high boots and trim as the identity. Keep leg anatomy readable at small pixel size: two hips, two knees, two separate boots, a visible gap at contact, overlap only in passing. Far leg slightly darker so viewers can track which leg leads, without introducing new costume asymmetry. The leg silhouettes MUST alternate wide/narrow/wide/narrow. Stable face/expression, minimal hair/tail movement, NO blinking between walk frames. Torso stable, actual limb articulation, no side-to-side whole-character shake. No skating, no jumping in every frame, no duplicate frames, no extra limbs. Genuinely transparent alpha, crisp pixel contours, no haze, shadow, gradient or labels. Exactly 4x2 cells.
```

### 第二轮：加强交叉与前后交换

```text
Use case: precise-object-edit. EDIT TARGET: the provided eight-pose walk sprite sheet. Its top bodies are good, but the legs incorrectly repeat the same poses in columns 1 and 3, and again 2 and 4. Fix this error. Preserve all heads, hands, upper torsos, horns, wings, hair ornaments, colors, tail identity, pixel art and the FOUR COLUMN by TWO ROW grid. Transparent background.
Make the four LEG SILHOUETTES per row obviously different:
Column1 CROSS-STRIDE: thighs and shins form a visible X. The leg attached to the screen-LEFT hip crosses diagonally to the screen-RIGHT boot, and the leg attached to the screen-RIGHT hip crosses diagonally to the screen-LEFT boot. Two normal knees, two separate boots. One shin passes behind the other at the middle. Wide ground contact.
Column2 PASSING: one boot planted vertically below the body; the OTHER boot is clearly LIFTED on the SCREEN-RIGHT side, with a bent knee. Narrow stance.
Column3 OPEN-STRIDE: legs form a visible inverted V, NOT X: leg from screen-LEFT hip reaches down/out to screen-LEFT boot, leg from screen-RIGHT hip reaches down/out to screen-RIGHT boot. Wide ground contact. This MUST be visibly different from column1: thighs uncrossed.
Column4 OPPOSITE PASSING: one boot planted vertically below body; the OTHER boot is clearly LIFTED on the SCREEN-LEFT side with bent knee. This MUST be visibly different from column2: lifted boot on the opposite screen side.
Both rows follow X legs -> right boot lifted -> V legs -> left boot lifted. Row1 overall figure faces right, row2 faces left, matching the existing target. The same physical two legs travel through the four poses; change limb occlusion. Make the far boot slightly darker to identify it. This is four keyframes of an alternating walk, not four copies of a running pose. Preserve original outfit/boot trim and correct anatomy, with no third leg or detached foot. Keep hip and torso position constant, common supporting boot baseline, clear margin around each pose. Keep the corrected tiny hands, face and expression identical between columns. No blinking or lateral head jitter. Do not add motion trails or arrows, labels, grid lines or shadows. Exactly eight full-body sprites in 4x2 regular square cells.
```


本角色使用内置图像生成工具制作：先修正已确认母版的双手，再分别生成动作和注视图。以下为实际提示词。生成原图不是严格目标尺寸，打包工具以实际网格分割并统一注册；最终 atlas 为 1024×2560、4 列 10 行。

## 手部修正

```text
Use case: precise-object-edit. Edit ONLY THE TWO HANDS of the provided approved low-resolution pixel-art ChatGPT娘. Preserve character identity, pixel density, exact compact full-body front-facing proportions, grey-black dress, tiny dusty-purple trim, silver-grey hair, horns, wings, curled tail, face, silver-purple eyes, ChatGPT knot hair clip, framing, and all other details. The current hands look like angular blobs sticking out sideways. Redraw both as small anatomically plausible simplified pixel hands in a relaxed neutral standing pose: wrists attach correctly to the sleeve openings, palms angle slightly toward the body, one small clear thumb per hand, the other fingers lightly curled together as one compact readable cluster. Each hand should read like a relaxed natural little hand at tiny sprite scale, not a giant open palm, extra limb, pointing finger or clipped square. Keep wrists and hands distinct from dress and hair; both skin-colored hands clearly visible just beyond the cuffs, same size on both sides. Do not over-detail individual fingers: a few deliberate skin-tone pixels. Maintain true chunky pixel-art outlines and flat color clusters, no smooth anime painting or blur. Center the whole figure with safe transparent margin, horns and boots not cropped. Genuinely transparent backdrop, no glow, no haze, no shadow, no text, no other pose. Single corrected standing master only; all changes restricted to the two hands and their immediate wrist connection.
```

## 六组主要动作

```text
Use case: stylized-concept. Production animation sprite atlas, ONE PNG asset, of exactly the SAME approved pixel-art dragon-girl desktop mascot shown in the reference. Reference is identity master with corrected natural hands. Preserve silver-grey hair, two horns, grey-black dress with a few muted violet accents, purple eyes, small ChatGPT knot hair clip on the same temple, dragon wings and curled tail, chunky pixel-art palette and proportions. Do NOT redesign or add decorations. Make EXACTLY 24 full-body poses in a perfectly regular grid of FOUR columns and SIX rows. Canvas intended 1536 wide by 2304 tall, every grid cell 384x384. No visible grid, labels or numbers. One pose centered in each cell with plenty of clear transparent padding, no overlap, no cut horns/tail/boots. Camera and body size IDENTICAL in all cells: standing height about 300px, body center x=192 within each cell, boot ground baseline y=350 within each cell except the specified jump lift. Keep hands anatomically sensible as tiny simplified relaxed hands with visible thumbs, no extra fingers. Tiny pixels and discrete color clusters; no soft illustration or glow. Each row is one coherent left-to-right 4-keyframe loop, with real visible action differences:
ROW 1 IDLE: (1) neutral standing open eyes, (2) same stance half blink slight relaxed breath, (3) closed-eye blink no grin and tiny hair/tail sway, (4) reopen neutral. Feet stay exactly on baseline, minimal movement.
ROW 2 WALK RIGHT: body three-quarter toward viewer's right, 4 alternating leg steps in place; opposite arm swing, hair and tail trailing gently; character stays centered and same size, no horizontal translation or back view.
ROW 3 WALK LEFT: body three-quarter toward viewer's left, corresponding alternating 4-step walking cycle, character stays centered; keep the hair emblem belonging to the SAME anatomical side, do not simply mirror every ornament.
ROW 4 WAVE: front view; (1) hand starts rising, (2) one small open hand beside head, (3) hand waves outward slightly with readable thumb, (4) hand returning toward resting position. Other hand relaxed. Stable body and baseline.
ROW 5 JUMP: (1) tiny anticipation crouch boots still at y=350, (2) cheerful midair jump BOTH boots y=322, (3) jump apex BOTH boots y=302, (4) soft landing boots back at y=350. SAME body scale; genuine whole pose rises INSIDE its cell, with headroom for horns. No rockets, dust or props.
ROW 6 THINKING: front view; (1) small hand near chin thoughtful eyes, (2) chin-hand and gaze slightly upper right, (3) small head tilt and half blink, (4) return to thoughtful first pose. Stable baseline, no extra symbols or bubbles. Natural hand anatomy at tiny pixel scale.
Use GENUINELY TRANSPARENT background with crisp opaque sprite pixels, absolutely NO gradient, no aura, no glow, no shadow or decorative marks between poses. Exactly 4x6 = 24 separated complete characters, not 5 columns or missing cells. Keep the reference identity and low-resolution pixel clusters consistent across EVERY pose. No text anywhere.
```

## 十六向注视

```text
Use case: stylized-concept. ONE pixel-art desktop mascot LOOK-DIRECTION sprite sheet, using the provided corrected standing master as exact identity reference. Keep silver-grey hair, dark charcoal dress with tiny muted purple accents, friendly warm skin, purple eyes, horns, wings, curled tail and the little ChatGPT knot hair clip on the SAME anatomical side. Natural tiny relaxed hands outside cuffs. Exactly FOUR columns and FOUR rows = SIXTEEN complete full-body standing poses. Canvas intended 1536x1536 with 384-square cells. One same-size character centered in each cell, boots on common local baseline y=350, head and horns fit inside with clear padding. Identical body stance, scale, framing and outfit throughout. Body stays FRONT-FACING and feet point forward in all frames. Change ONLY eye direction plus a small expressive head tilt, so the companion can look toward a typing cursor. This is NOT a whole-body rotation turntable; do not show a back view or hide the face. Directions are clockwise from straight up, four positions in every row, read left to right: ROW1 UP, UP slightly RIGHT, upper RIGHT diagonal, RIGHT slightly UP. ROW2 RIGHT, RIGHT slightly DOWN, lower RIGHT diagonal, DOWN slightly RIGHT. ROW3 DOWN, DOWN slightly LEFT, lower LEFT diagonal, LEFT slightly DOWN. ROW4 LEFT, LEFT slightly UP, upper LEFT diagonal, UP slightly LEFT. UP means face tilts a little upward and visible pupils sit high in open eyes; DOWN means chin tilts down and pupils sit low with eyelids still open; LEFT/RIGHT mean a slight head turn and pupils clearly shift toward that side while torso stays forward. Faces and both eyes remain readable. All 16 retain the same silhouette, hair ornament, forward torso and ground level. Tiny low-resolution 16-bit game-sprite pixel clusters; dark one-pixel contours, restrained palette, no smooth illustration, no blur or detailed painted gradients. Truly transparent background, no glow, no aura, no ground shadow, no labels, no text, no guide lines. Exactly one character in each of 16 cells, no duplicates pasted in place of different gaze directions.
```
