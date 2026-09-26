# Hearthhold UI / UX Design Contract

This file is the source of truth for Hearthhold interface work. Read it before changing layouts, interaction patterns, typography, colors, selection feedback, or presentation assets. If implementation and this contract disagree, update the contract deliberately in the same change instead of introducing an exception silently.

## Product character

Hearthhold is a polished, readable, stylized 3D strategy game for Windows PC. Its personality is warm frontier fantasy: dark timber, pale masonry, restrained brass, ember gold, and mint magical accents. Shapes should be chunky and readable from an isometric camera, but never look like flat stickers or unfinished primitives.

The player-facing Chinese game name is **篝火堡垒**. Use it consistently in the HUD, help, notices, release copy, and screenshots. Hearthhold remains the English project name.

References may inform hierarchy and usability, but all names, icons, models, textures, silhouettes, and copy must remain original or come from compatible licensed sources recorded in `THIRD_PARTY_ASSETS.md`.

## 3D art prototype gate

- A model is not approved from a single isometric screenshot. The same shipped mesh must read from the default camera and from the opposite quarter turn; no camera-facing character or building planes may serve as final art.
- Structure, costume, pet anatomy, and spell volume use actual depth-bearing geometry. Painted textures may add surface detail, but never supply the entire silhouette or an attack pose.
- The first vertical slice compares one keep, one vanguard, one pet, and one healing spell in-game before expanding to the roster. Keep their ground contacts inside logical footprints; animation must move meaningful body parts rather than only scaling a flat image.
- For each new third-party source, record author, URL, license, and whether source assets may be committed to the public repository. Unity Asset Store packages under its standard EULA are not treated as CC0 simply because the store price is free.

## Information hierarchy

1. The world and the current player action remain the visual focus.
2. Persistent resources and progression sit at the top edge.
3. Context for the selected object appears in one right-side inspector.
4. Frequent actions live in the bottom dock: Build, Train, Army Guide, Heroes/Pets, and Expedition.
5. Infrequent controls and full shortcut lists live in Help, not in the permanent HUD.
6. Catalogs, research, progression, and management screens use one centered modal and dim the world.

Do not show a long permanent row of buildings or units when a catalog can disclose them on demand.

## Catalog and research disclosure

- The first layer of a troop, spell, or building catalog is a scannable grid: original icon, name, level/unlock state, and the one piece of timing information relevant to that action. Do not pack role descriptions, combat stats, costs, and instructions into the same tile.
- Clicking the icon/tile opens a separate detail card. Put the full description, current-to-next values, requirements, resources, and one explicit action there. The detail card has a clear return path to the grid.
- The laboratory currently completes research immediately per `docs/PROGRESSION-AND-ECONOMY-CONTRACT.md`; show “即时” instead of inventing a countdown. If research time is introduced later, update the rule, save migration, tests, and this contract together.
- UI icons are original symbols or live 3D previews. A catalog symbol may be 2D, but a world character or building cannot be a camera-facing sprite posing as a model.

In the campaign dossier, clicking an unlocked mission selects and highlights it without closing the modal. Its description and the explicit expedition action stay visible in a bottom decision area. Achievement rewards update in place; when storage capacity blocks a claim, the card says so instead of presenting a silent button.

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
- Modals must fit within the 1280 × 720 minimum viewport. Widen long-form detail cards before shrinking text; shorten or reorganize copy if a compact-height variant is needed. No close or confirm control may fall below the screen edge.
- Keep one clear primary action per panel. Destructive actions are separated and require confirmation.

## Hero hall pattern

Hero and pet management uses a three-column "frontier dossier" layout: animated roster cards on the left, one large real-time 3D stage in the center, and decision-ready stats on the right. Cards are clickable and always show identity, role, level, unlock state, and bond state. The detail column shows current-to-next-level values before the upgrade action. A pet's bond action and upgrade action are separate because they change different systems.

Use live RenderTexture previews instead of pre-rendered GIF files: they preserve the current level ornaments, model, and action animation without duplicating art assets. Future heroes and pets extend the roster list rather than creating a new modal.

Building, troop, hero, and pet detail previews keep their camera still until the player holds the left mouse button and drags inside the preview. Horizontal drag orbits around the model; vertical drag tilts within a safe range. Release stops movement immediately. Idle model actions may continue, but neither the preview camera nor the model may auto-spin. The preview must consume this drag so it never moves the world or closes the modal.

Identity changes must alter 3D silhouette, equipment, motion, or the causal VFX shape—not only hue. Read the matching dossier in `docs/entities/` before modifying an entity, and distinguish what is currently implemented from planned art. The shared rendering helpers can be reused; each entity still needs its own authored shape and behavior entry point.

## Color and state

- Deep green-black panels: world-compatible neutral surface.
- Ember gold: selection and primary emphasis.
- Mint: valid placement, completion, healing, and positive state.
- Coral red: invalid placement, danger, and destructive confirmation.
- Disabled controls must remain legible and explain their prerequisite in nearby text.
- An unavailable upgrade states its actual prerequisite (such as the required 议事堡 level) in the inspector; do not show a price on a button that cannot upgrade.
- Small directional controls use drawn shapes rather than font glyphs, so their arrows stay visible with Chinese font fallbacks.

Color is never the only state signal: combine it with text, iconography, outline shape, or motion.

## World interaction

- Do not show automatic world notices, toast messages, or status banners for routine actions (including layout edits). Reserve the Help screen and explicit detail panels for instructions and prerequisites; maintain visual selection, valid/invalid placement, and button states.
- Mouse interaction is primary; keyboard shortcuts are accelerators, never the only discoverable path. Every frequent action must have a visible clickable control, and map editing must be completable without a keyboard.
- Labels describe the mouse action first. Shortcut hints may follow in a quieter secondary position instead of leading the control name.
- In the ordinary village view, left-press and drag a building to move it. Preserve the grabbed footprint cell under the pointer; releasing on an invalid tile leaves the building in its original position.
- Left-press a wall and sweep across adjacent wall cells to select a straight, contiguous segment of one row. Release to finish selection, then left-press any selected wall and drag to move the entire selection atomically. A perpendicular turn or gap never joins the selection.
- A short click still selects a building for its inspector. World drags must not start through a visible HUD element; right-click cancels the drag and clears its preview immediately.

- Every building is anchored to the south-west corner of its integer footprint; its visual center is `(x + size/2, z + size/2)`.
- Placement previews, colliders, shadows, selection marks, and saved coordinates must use the same footprint.
- A single wall occupies exactly one cell. Its selection feedback is a mint square cell outline, not a circular aura, so placement coordinates are unambiguous and distinguishable from the wall's gold trim.
- “Select row” chooses only straight, contiguous, axis-aligned wall segments. It stops at gaps and corners. Batch upgrade and batch move are atomic; batch demolition is intentionally unavailable to prevent accidental loss.
- Wall-row movement previews every destination cell together. The complete 1×1 outline renders above world depth so a tall wall cannot visually hide the rear edges and create a false offset.
- Move, build, cancel, upgrade, and select actions must immediately update both world feedback and the inspector.
- Map clicks must be rejected whenever the pointer actually hits a visible HUD element. Do not approximate a panel's position with fixed screen coordinates; the HUD scales with resolution.
- Right-click cancellation hides the placement tile, building preview, and wall-row preview in the same frame.

## Formation editor

Village layout editing uses a distinct tactical-workbench mode rather than overloading ordinary building selection.

- The left tool rail exposes Select all, Move selection, Clear to tray, Grid, Finish, and Cancel as visible mouse controls.
- Select all includes every currently placed building. Group movement is atomic: if any footprint would leave the editable map or collide with an unselected building, nothing moves.
- Clear to tray removes buildings only from the in-memory editing layout. It never saves an incomplete village. Finish remains unavailable until every staged building is placed; Cancel restores the exact entry layout.
- The bottom tray supports both click-then-place and drag-to-map placement. Cards show building identity, level, footprint, and staged count without relying on keyboard input.
- The grid is on by default in editing mode and may be toggled with a visible control. Valid placement uses mint, invalid placement uses coral, and selected/group bounds use ember gold.
- Direct manipulation also works inside editing mode: hold and drag any placed building; sweep across contiguous wall cells in one row to select them, then press a selected segment and drag the whole selected row. Releasing over UI or an invalid destination preserves the original positions.
- Editing notices state the next mouse action. Keyboard shortcuts, when present, remain optional and are documented in Help.

## Runtime UI foundation

- Village HUD, village modal screens, and the village Help handbook use Unity uGUI and TextMesh Pro. Battle controls, layout-editor tools, battle Help/result overlays and development diagnostics still use IMGUI and remain explicit migration work; new player-facing screens must use uGUI.
- The visual language is the **forge command table**: iron-green structural surfaces, bronze for primary actions and resources, patina for interaction, parchment for readable text, and ember only for danger or urgent state.
- The map is the visual hero. Permanent HUD occupies the outer edge, uses restrained ornament, and never covers the central planning area without a modal backdrop.
- Components are reusable prefabs or programmatic equivalents with Canvas Scaler support; new screens must not introduce hard-coded IMGUI rectangles.
- Decorative generated art must be original, stored under `Resources/UI`, referenced by a shipped screen, and remain legible at its smallest target size.
- Remaining IMGUI screens use the same forge-command-table skin during migration. Opening a modal must not reveal the old unstyled HUD or default Unity button gradients.
- Chinese UI copy uses one regular-weight sans-serif family. Hierarchy comes from size, color, and spacing; synthetic bold is avoided because it distorts dense Chinese glyphs.

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

## Hero equipment dossier

- The hero hall uses one visual hierarchy: left live roster, center animated 3D figure, right forge dossier. The top-right control switches hero statistics and equipment without opening another modal.
- The right dossier shows two numbered slots first, then the four-item collection, then the selected effect and its action row. Slot and item selection are separate states; equipping never occurs from a mere list click.
- Resource balances and every forge cost are visible before committing. Disabled actions still state their gate (unowned item, hall level, or insufficient materials) in the dossier or catalog.
- The iron, bronze, parchment and mint palette is shared with the command HUD. Chinese interactive text remains at least 15 px at 1280 × 720 and 1440 × 900; compact information must not rely on color alone.
- The hero hall now uses the shared uGUI modal canvas and preserves roster, live preview, dossier, equipment and upgrade interaction order.

## Verification gate

For any UI change:

1. Run core tests.
2. Build the Unity Windows player.
3. Run Home smoke and the affected modal smoke (Build Catalog for catalog/typography work).
4. Inspect screenshots at 1440 × 900 for clipping, overlap, contrast, selection alignment, and stale state.
5. If selection or placement changed, verify all four camera rotations and edge cells.
6. For inspector interaction changes, test the mouse press/release path and verify the resulting game state for Upgrade, Move, Demolish confirmation, and Details. A direct button callback alone does not prove pointer input works.

No UI task is complete solely because it compiles.
