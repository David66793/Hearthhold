# Hearthhold UI / UX Design Contract

This file is the source of truth for Hearthhold interface work. Read it before changing layouts, interaction patterns, typography, colors, selection feedback, or presentation assets. If implementation and this contract disagree, update the contract deliberately in the same change instead of introducing an exception silently.

## Product character

Hearthhold is a polished, readable, stylized 3D strategy game for Windows PC. Its personality is warm frontier fantasy: dark timber, pale masonry, restrained brass, ember gold, and mint magical accents. Shapes should be chunky and readable from an isometric camera, but never look like flat stickers or unfinished primitives.

References may inform hierarchy and usability, but all names, icons, models, textures, silhouettes, and copy must remain original or come from compatible licensed sources recorded in `THIRD_PARTY_NOTICES.md`.

## Information hierarchy

1. The world and the current player action remain the visual focus.
2. Persistent resources and progression sit at the top edge.
3. Context for the selected object appears in one right-side inspector.
4. Frequent actions live in the bottom dock: Build, Train, Army Guide, Heroes/Pets, and Expedition.
5. Infrequent controls and full shortcut lists live in Help, not in the permanent HUD.
6. Catalogs, research, progression, and management screens use one centered modal and dim the world.

Do not show a long permanent row of buildings or units when a catalog can disclose them on demand.

## Typography

The UI font is Microsoft YaHei UI with Microsoft YaHei and Arial as fallbacks.

| Role | Target size | Rules |
|---|---:|---|
| Modal title | 27 px | Bold; one line; at least 36 px line box |
| Section/card title | 18 px | Bold; one line; at least 25 px line box |
| Body/action label | 18 px | Prefer one line; wrap only in wide panels |
| Metadata/helper text | 15 px | Minimum size anywhere in the shipping UI |

Text must never collide with a button, resource state, or another label. Chinese glyphs need at least font size + 7 px of vertical room. Descriptions get two lines where needed; shorten copy before shrinking below 15 px. Verify locked, capped, expensive, and long-name states.

## Layout and spacing

- Design baseline: 1440 × 900; supported minimum: 1280 × 720.
- Outer modal padding: 25 px. Standard card inset: 12–16 px.
- Standard gap: 8 px; group gap: 15–24 px.
- Primary buttons are at least 42 px high. Compact row actions are at least 32 px high.
- A table/card row must reserve independent regions for title, status, description, and action. Descriptions may flow underneath the title/status row but never underneath the action button.
- Keep one clear primary action per panel. Destructive actions are separated and require confirmation.

## Hero hall pattern

Hero and pet management uses a three-column "frontier dossier" layout: animated roster cards on the left, one large real-time 3D stage in the center, and decision-ready stats on the right. Cards are clickable and always show identity, role, level, unlock state, and bond state. The detail column shows current-to-next-level values before the upgrade action. A pet's bond action and upgrade action are separate because they change different systems.

Use live RenderTexture previews instead of pre-rendered GIF files: they preserve the current level ornaments, model, and action animation without duplicating art assets. Future heroes and pets extend the roster list rather than creating a new modal.

## Color and state

- Deep green-black panels: world-compatible neutral surface.
- Ember gold: selection and primary emphasis.
- Mint: valid placement, completion, healing, and positive state.
- Coral red: invalid placement, danger, and destructive confirmation.
- Disabled controls must remain legible and explain their prerequisite in nearby text.

Color is never the only state signal: combine it with text, iconography, outline shape, or motion.

## World interaction

- Every building is anchored to the south-west corner of its integer footprint; its visual center is `(x + size/2, z + size/2)`.
- Placement previews, colliders, shadows, selection marks, and saved coordinates must use the same footprint.
- A single wall occupies exactly one cell. Its selection feedback is a mint square cell outline, not a circular aura, so placement coordinates are unambiguous and distinguishable from the wall's gold trim.
- “Select row” chooses only straight, contiguous, axis-aligned wall segments. It stops at gaps and corners. Batch upgrade and batch move are atomic; batch demolition is intentionally unavailable to prevent accidental loss.
- Wall-row movement previews every destination cell together. The complete 1×1 outline renders above world depth so a tall wall cannot visually hide the rear edges and create a false offset.
- Move, build, cancel, upgrade, and select actions must immediately update both world feedback and the inspector.

## Motion and feedback

- Hover/selection pulse is subtle (roughly 2–3%), not distracting.
- Combat attacks require anticipation, impact, and recovery phases plus readable hit feedback.
- Placement validity updates continuously under the pointer.
- Every rejected action explains why in the notice area; successful batch actions state the affected count.

## Modal catalog pattern

The Build Catalog is a two-column table on desktop. Each row contains:

1. Building name (18 px bold)
2. Built/limit or unlock prerequisite
3. A concise two-line description
4. One right-aligned placement action

No element may overlap at 1280 × 720 or 1440 × 900. Opening the catalog pauses map interaction; Escape and the visible Close button return to the world.

## Verification gate

For any UI change:

1. Run core tests.
2. Build the Unity Windows player.
3. Run Home smoke and the affected modal smoke (Build Catalog for catalog/typography work).
4. Inspect screenshots at 1440 × 900 for clipping, overlap, contrast, selection alignment, and stale state.
5. If selection or placement changed, verify all four camera rotations and edge cells.

No UI task is complete solely because it compiles.
