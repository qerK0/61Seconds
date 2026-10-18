# LOCAL TASK 14 — New props/items, cut items, stairwell doors, Layout Editor v2, the "varnish" pass

Read `CLAUDE.md` and the newest `HANDOFF.md` entry first. Unity is open with MCP. Small steps; compile after each. Commit after each numbered part.

## 1. New art (all in `Assets/_Game/Art/items/`)
**Props / furniture** (into PropArtSet + the Layout Editor palette; bottom-centre pivot, PPU by target height like PropArt does):

| File | Prop | Target height |
|---|---|---|
| `dolapv2.png` | `WardrobeV2`, a wardrobe variant. Usable as a container preset (`container.wardrobe`) | ~1.1u |
| `mutfak dolabi.png` | `KitchenCabinet`. Container preset `container.kitchen_cabinet` = `Mutfak dolabı` | ~0.6u |
| `alet canta.png` | `Toolbox`. Container preset `container.toolbox`; it replaces the coloured toolbox in the Flat 11 hall | ~0.25u |
| `koltuk.png` | `Armchair` / `Sofa`. Replaces the coloured sofas (Flat 12, 9, 8 living rooms) | ~0.5u |
| `yatak.png` | `Bed`. Replaces the coloured beds (Flat 11 / 12 / 8 bedrooms) | ~0.45u |
| `zemin delik.png` | `FloorHole`, a floor decal (sorts on the floor layer, under characters). Use it for the Flat 10 collapsed floor (it still needs the rope, per the existing rule) | ~1.4u wide |

**Item icons** (via the generated UI icon pipeline from task 13, plus world pickup sprites):

| File | Item |
|---|---|
| `bez kuru.png` | `cloth` |
| `bez islak.png` | `wet_cloth`, its world/pickup sprite and the crafting result flash |
| `bez maske.png` | the **wet_cloth bag/UI icon**. Bora: "the wet cloth, worn". While the player has `wet_cloth` and is in a gas room, show a small "maske takılı" indicator using this icon next to the poison overlay. No new item id; no new mechanic. |

## 2. Cut items and objects (remove cleanly)
- **Remove the items** `gold`, `console` (PS) and `laptop`: their item assets, every pickup / container content that referenced them, the prep-trap logic tied to them, tips that only exist for them, their CSV keys (only if unused), validators and tests. Prep tension now comes only from time and slots.
- **Remove the medicine cabinet object** (`container.medicine` / `salon_cabinet` "ecza dolabı" and its preset). The **`first_aid` item STAYS**: move the salon's first_aid into another existing container in Flat 11 (the kitchen cabinet, `mutfak dolabi`), and keep Flat 12's first_aid in its cabinet. The injury + first-aid mechanic is unchanged.
- Run Validate Content: no dangling ids. Every progression-critical item (bedsheet ×3 path, rope, gloves or grip + tape, flashlight, child_harness, whistle, food/water sources) must still be obtainable. Add/extend a validator rule that checks this list exists somewhere reachable.

## 3. No stairs art → stairwell doors
There will be no stairs sprite. Replace every `StairsDown` prop (the F4 corridor, the F2 corridor, the F2 stairs room) with a **stairwell door**:
- the same `kapi.png` door art in a wall gap;
- a small sign above it: a dark plate with white text `MERDİVEN ↓` (TMP world text, CSV key `ui.sign.stairs`);
- clicking it plays the existing stairs move choice (same costs, conditions and nodes).

Remove the stairs edge prop art references. The F3 broken stair edge hazard stays as a hazard anchor, with debris art instead of a stair sprite.

## 4. Layout Editor v2
Extend `RoomLayoutOverrides` + `LayoutEditorWindow`:
- **Store a full transform per placed object:** position (gx, gy + lift), **scale** (uniform; default 1; a slider 0.5–2 in the window plus the Scene scale tool), flipX and sorting nudge (±). The builder applies scale on rebuild, and nav blocking uses the scaled footprint.
- **Doors and stairwell doors become editable layout objects:** move along their wall (snapped to wall-gap edge codes), flip, and change the art (door / elevator / stairwell door). Their link target, choice and costs stay code-defined and show read-only. Moving a door moves its approach point and arrival point with it. The validator re-checks reachability after door moves.
- **The new props from part 1** show in the palette, and the **container presets** include `Mutfak dolabı`, `Alet çantası` and `Gardırop v2`. Remove `Ecza dolabı`.
- **"Reset scale"** and **"Match prop height"** buttons: the latter sets the scale so the prop reaches its target height from the PropArt table.
- **Backward compatible:** existing overrides without scale load as scale 1.
- Update `docs/layout-editor.md` (Turkish, short) with scale + doors.

## 5. The "varnish" pass (a unifying look; keep it subtle)
CinematicDirector already drives ACES tonemapping, colour adjustments, vignette and bloom per act. Add on top:
- a **URP Color Lookup** override with `Art/LUT/LUT_61s_Dust_32.png` (a 32-size strip: lifted blacks, slight desaturation, teal shadows, warm dusty highlights), contribution about 0.6 as the base for every act;
  - import it as Default with sRGB, no mipmaps, no compression, Clamp, Bilinear, and follow the URP Color Lookup requirements;
  - `LUT_Neutral_32.png` is the identity LUT for checking: with it at contribution 1 the image must look unchanged. If it doesn't, fix the import settings, not the LUT;
- **film grain** (URP Film Grain, Thin1, intensity about 0.12, response 0.8) to tie together the pixel-art bag, the cartoon characters and the AI-painted props;
- a **soft contact shadow** under every prop and character that lacks one (the existing ph_shadow, alpha 0.35, scaled to the footprint). This grounds mismatched art more than any filter;
- the vignette is already there; just make sure the base intensity is ≤ 0.3 in act 1 and the acts keep getting darker as before.

Rules:
- The UI must NOT be graded: keep the HUD on a Screen Space Overlay canvas. The LED timer green, the warning reds and the item icons stay true.
- Reduce Effects disables film grain; the LUT stays.

**Verify:** before/after screenshots of the salon, the F3 corridor, Flat 10 (dark) and the street at 1080p. Note them in the handoff, and say whether the LUT contribution needs tuning.

## 6. Verify everything
- Rebuild the scene with the overrides; play F4 → street once: the new props are in place, the containers work (mutfak dolabı has first_aid), the stairwell doors work on every floor, no gold/laptop/console anywhere, the wet cloth shows the mask icon in gas.
- Layout Editor: scale a wardrobe, move a door along its wall, save, rebuild — both persist.
- 0 compile errors, EditMode green, Validate Content and Text > Validate clean.
- Write the HANDOFF entry, then commit and push.
