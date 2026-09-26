# 3D art vertical slice (prototype)

The first comparison scene is in the Windows build, not a sprite mockup. It stages a level-3 Keep, a rigged Vanguard, an articulated Cinder Fox, and a healing field. Run `tools/test-unity-player.ps1 -Case ArtProof` and `-Case ArtProofRotated`; the generated screenshots are `artifacts/screenshots/97-unity-art-proof-front-v151.png` and `98-unity-art-proof-reverse-v151.png`. The second case rotates the real camera, not an image swap. The staging positions vary to keep each specimen visible; the underlying meshes do not.

## Current asset decision

- Keep: existing KayKit CC0 3D FBX, with small in-footprint geometry accents. The accent geometry is procedural and rendered with lit materials.
- Vanguard: existing Quaternius CC0 rigged 3D FBX, with a small set of attached armor parts. The original skeleton and clips remain active.
- Cinder Fox: articulated procedural 3D mesh assembled from lit primitives, with separate head, leg, and tail joints. This is a silhouette and animation prototype, not approved final creature art.
- Heal: ground-radius indicator plus a 3D central core and orbiting motes. The ring is only an area indicator, not a substitute for the spell volume.

Do not purchase or import a whole asset pack on the strength of a store screenshot. The next candidate must first replace **one** specimen and pass the same front/reverse and gameplay tests. Check author, source URL, license, and permission to commit source assets to the public repository in `THIRD_PARTY_ASSETS.md` before import. A free Unity Asset Store price does not imply open-source redistribution rights.

## Approval gate before roster-wide replacement

1. Same actual mesh stays readable at the default and opposite camera angles, with grounded feet and no view-facing cutout.
2. Idle, move, attack, hit, and death move meaningful geometry; the pet's head, limbs, and tail are independently articulated.
3. Existing battle, spell-field, and pet smoke cases pass; collision footprint and world selection remain aligned.
4. Compare specimen screenshots with the current build at game camera scale. A candidate that only looks better in an isolated close-up is not approved.

This slice establishes the 3D pipeline and catches the earlier flat-art regression. It does **not** yet meet a final polish bar: the creature anatomy, terrain materials, and spell choreography still need a dedicated art pass.

## September 2026 art pass: what changed and what did not

- The Cinder Fox now has pointed 3D ears, a leaner muzzle and body, a single tapering vertex-coloured tail mesh, and a more legible four-limb attack lunge. These are authored by project code in `Runtime/ModelViews.cs`; they remain a procedural prototype, not a finished commissioned creature.
- The meadow is now a single, flat, vertex-coloured 3D mesh in `Runtime/MeadowSurface.cs`. Its gentle colour variation does not change building placement or collision. A trial of tiny upright grass blades was rejected after the game-camera screenshot showed dark chevrons; do not restore dense detail without checking it at gameplay scale.
- Heal, fury, and freeze now have distinct 3D cores and smaller sustained boundary opacities. The boundary still communicates gameplay radius; the next VFX pass should improve timing, shape language, and impact feedback rather than enlarge the rings.
- Both art-proof camera directions, the spell-field smoke, and the battle-action smoke passed after the changes. The front and reverse captures remain `97-unity-art-proof-front-v151.png` and `98-unity-art-proof-reverse-v151.png` under `artifacts/screenshots/`.

The visual bar is still open: background composition is sparse, the fox is not production-quality character art, and a static screenshot cannot prove the attack timing. Recheck those in motion before treating this slice as a final art style approval.

## External 3D candidate audit (September 2026)

The user has approved researching external tools and assets. The next trial should remain a **single-asset comparison**, not an unreviewed pack-wide import.

| Candidate | Why it is worth a local trial | Decision for the public repository |
|---|---|---|
| [Quaternius Ultimate Animated Animal Pack](https://quaternius.com/packs/ultimateanimatedanimals.html) | A fox is listed in the author's catalog; the pack advertises 12 animated animals with attack, walk, jump and death takes. This is more promising than adding more primitives to the current Cinder Fox. | **Do not commit a new raw FBX yet.** The pack page still shows CC0, but the author's [QAL page](https://quaternius.com/license.html) was updated on 2026-08-28 and restricts redistribution of standalone assets. Confirm the license included with the exact downloaded archive, or obtain author clarification, before putting a new asset in the public GitHub repository. |
| [Quaternius Stylized Nature MegaKit](https://quaternius.com/packs/stylizednaturemegakit.html) | Contains textured trees, plants, and rocks, plus a Unity URP source version. Test only one tree and one rock at gameplay camera scale before replacing the current perimeter. | Same source-license check. The source/editor bundle is not assumed to be free merely because the standard model subset is free. |
| [Blender](https://www.blender.org/features/) | Free and open-source modeling, rigging and animation pipeline for an original pet or level-specific building ornaments. | Recommended authoring tool, but not installed or silently added to collaborators' machines by this pass. Keep `.blend` sources alongside exported runtime files when created. |
| [Mixamo](https://helpx.adobe.com/creative-cloud/faq/mixamo-faq.html) | Could help with humanoid animation experiments. | **Not the fox solution**: official FAQ says its auto-rigger is biped-only and its service is unavailable to accounts with a China country code. Existing licensed character clips remain preferable. |

Acceptance before replacing the prototype fox: actual animated mesh in Unity, visual comparison at 1440x900 and 1280x720 from two camera quadrants, feet inside logical footprint, attack and death readable at gameplay scale, no material/shader mismatch, and a license record in `THIRD_PARTY_ASSETS.md`. Keep the current prototype as fallback until all gates pass.
