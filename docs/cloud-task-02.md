# CLOUD TASK 02 — Run instead of teleport, loot containers, debris + crack progression, new prop art, robber + street finale

Read `CLAUDE.md` and the newest `HANDOFF.md` entry first. CLOUD rules apply:
- no hand-edited .unity/.prefab/.asset/.meta files; everything scene-side goes through generators / MenuItems;
- final Turkish text in `strings_tr.csv`, never TODO;
- small single-purpose scripts, `SixtyOne.*` namespaces, pure logic + EditMode tests;
- the project must compile; say in the handoff what you could not verify.

Work in the order below and commit after each part, so local can pick up a partial result if time runs out. Part A is the most important; Part F is the biggest.

---

## A. Characters run instead of teleporting
Today:
- placement repositions a family member instantly on an anchor;
- on a room change the wife and child snap to the room's spawns.

Change it:
- **Placement:** after the player taps a member and then an anchor, the member RUNS there through `CharacterMotor` / `CharacterAnimator` (run frames, flip by direction), using the same speed as `FamilyFollower`, scaled by `Room.Scale`.
  - The anchor is reserved at once (capacity and adjacency rules are unchanged), so the player can queue the next member while the first is still running.
  - If the placement timer ends before a member arrives, that member resolves at the anchor anyway: snap the last bit and play the quake. Never resolve against the old spot.
  - The player character, placed last, runs too.
- **Room change:** the family no longer pops in.
  - After the transition completes, the wife and child appear at the arrival door and run to their follow offsets behind the player.
  - On a floor change through the stairs, rope or cable, they come in from the arrival point the same way.
- **Rewinds stay instant** (snap). Reduce Effects keeps running; only the camera/visual effects are reduced.
- Walking still costs only real time. The building timer is not charged for running.
- Tests: the anchor is reserved immediately; a member that has not arrived when the timer ends resolves at the target anchor; a rewind snaps.

## B. Grid pathfinding (needed by C and F)
Movement is currently a straight line clamped to the room's `FloorArea`. Add a small grid pathfinder:
- `Placement/GridNav.cs` (pure): an A* over the room's floor cells (Width × Height + ExtraCells), 8-neighbour with no corner cutting, and a blocked-cell set.
- `PlayerMover`, `FamilyFollower` and the placement runs follow the path through cell centres, smoothed: cut a corner when the straight segment is clear.
- A click on a blocked cell goes to the nearest reachable free cell.
- Doors, NPCs and pickups use their approach cell. If that cell is blocked, use the nearest free neighbour.
- Tests: a path around a wall of blocked cells; an unreachable target goes to the nearest free cell; no diagonal squeezes between two blocked corners.

## C. Debris and cracks that grow with every quake
Add a quake counter `GameState.QuakeCount`, restored by snapshots: opening quake = 1, F3 aftershock = 2, F2 strong aftershock = 3, street aftershock = 4.

**Debris:**
- `DebrisPlanner` (pure) picks which floor cells of a room hold debris, from:
  - a seed = hash(roomId, QuakeCount, checkpointSeed), so the same checkpoint always gives the same layout and a loop can be learned;
  - a density per room = `LevelConfig.debrisDensityPerQuake[QuakeCount]` (defaults: 0, 0.06, 0.10, 0.15, 0.20 of the free cells), +0.03 on each lower floor.
- **Hard rule:** debris NEVER blocks progress. A cell is rejected if blocking it would break a GridNav path between any two of these: every door approach cell, every room spawn, every anchor, every pickup / container / NPC approach cell, and the rope / cable / collapsed-floor entry.
  - Check after each placement; keep at most N attempts, then stop.
  - Anchors, spawns and approach cells themselves are never debris.
- Debris only grows: the cells from QuakeCount n are kept at n+1, and new ones are added.
- `DebrisPresenter` (runtime) spawns a debris sprite per blocked cell (random choice of `enkaz 1 .png` / `enkaz 2.png`, random flipX, IsoSorter) when a room is entered or right after a quake resolves.
  - New debris after a quake drops in from above with a small dust puff (a coroutine tween and a particle burst), plus `cue_furniture_crash` at low volume.
- **Cracks:** the wall crack variants (the WhatsApp cracked walls) appear with a probability that grows with QuakeCount and with lower floors: about 0% in the pre-quake flat, about 15% after quake 1, 35% after 2, 55% after 3.
  - Use the same seeding, so a wall that cracked stays cracked.
  - Swap the wall sprites on room enter and after each quake. Cracks only ever increase.
- Tests: debris never disconnects the required cells (fuzz 200 seeds on every room layout); density is monotonic; the same seed gives the same layout; cracks are monotonic.

## D. Loot containers (click, wait, pick what goes in the bag)
Today items are `PickupItem`s with a time cost. Add containers for furniture you search (wardrobe, cabinet, bookshelf, drawers, medicine cabinet, toolbox, nightstand):
- **`LootContainer` (WorldInteractable / IClickable):**
  - an id, a sprite;
  - `searchSeconds` (real time spent searching, default 1.5s);
  - `buildingCost` (building-timer cost, default 3s, paid once on the first search);
  - a contents list (item ids, which may include traps like gold);
  - `borrowed` (true in neighbours' flats → BorrowLedger);
  - a hidden flag for hidden extra items.
- **Flow:**
  1. Click: the player runs to the approach cell.
  2. A radial progress ring fills over `searchSeconds`, with a rummaging sound (`cue_search`; generate one with `tools/gen_sfx.py`: cloth and wood rustle, about 1.2s). Input is locked except Esc. Clicking elsewhere cancels and refunds nothing.
  3. A **LootWindow** opens next to the bag, showing the container's contents as slots (the same slot sprite as the bag) with the container's name as the title.
     - Click an item to move it to the bag (if the bag is full, it flashes plus `cue_denied`).
     - Click a bag item to put it back.
     - `Al hepsini` takes all that fit. `Kapat` closes the window.
  4. Items left behind stay in the container. Container contents live in GameState (`ContainerLedger`, restored by snapshots like `RoomDropLedger`).
  5. A second visit opens the window right away, with no search time and no cost.
- Key items (flashlight, whistle) go straight to their backpack pockets.
- **Convert** the existing closed-surface pickups (`surface: false` in `BuildingLayout`) into containers with the same contents, ids and costs (e.g. `salon_cabinet`, `kitchen_drawer`, `hall_toolbox`, `hall_jewelry_box`, `hall_nightstand`, `bedroom_wardrobe_sheet`, `flat12_cabinet`, `flat12_closet_flashlight`, `flat12_wardrobe_sheet`). Group items that share one piece of furniture into one container. Open-surface pickups stay as they are.
- In neighbours' flats the window title says `Ödünç al`, and every item taken is recorded as borrowed. Never use "yağma", "loot" or "çalmak" in player text.
- **New Turkish strings:** `ui.loot.searching` = `Aranıyor…`, `ui.loot.take_all` = `Hepsini al`, `ui.loot.close` = `Kapat`, `ui.loot.borrow_title` = `Ödünç al`, `ui.loot.empty` = `Boş`, plus a container name per container (`container.wardrobe` = `Gardırop`, `container.cabinet` = `Dolap`, `container.bookshelf` = `Kitaplık`, `container.drawer` = `Çekmece`, `container.medicine` = `Ecza dolabı`, `container.toolbox` = `Alet kutusu`, `container.nightstand` = `Komodin`, `container.jewelry` = `Mücevher kutusu`).
- Tests: move item ↔ bag; bag full; key items go to pockets; contents persist across a rewind to the snapshot; the second visit has no cost.

## E. New prop art (generators only; local imports and tunes)
All files are in `Assets/_Game/Art/Concept/`. Write an `Editor/Art/PropArt.cs` import step:
- sprite, bottom-centre pivot;
- PPU chosen so each prop lands at a target height in world units: a table about 0.45u, a wardrobe about 1.1u, a bookshelf about 1.1u, a stove about 0.5u, a chair about 0.55u, the valve about 0.25u, the fuse box about 0.25u, the wall lamp about 0.2u, debris about 0.35u wide;
- a `PropArtSet` ScriptableObject mapping prop names → sprites.

`BuildingLayout` props use the sprite when one is mapped, and the coloured block otherwise. Mapping:

| File | Use |
|---|---|
| `masA.png` | `Table` (salon) and `Desk` (Flat 7) |
| `masaa.png` | new `Chair` props: 2 at the salon table, 1 at the F7 desk |
| `dolap.png` | `Wardrobe`: salon (the topple hazard keeps working on the sprite) and the bedrooms |
| `ComfyUI_00021_.png` | a second wardrobe/cabinet style, for neighbours' flats (Flat 12, 9, 8) |
| `bokkshelf.png` | `Bookshelf`: the F7 topple hazard, plus one per living room as a container |
| `ocak.png` | `Stove` (Flat 11, 10 and 7 kitchens) |
| `vana.png` | a NEW visible gas-valve prop next to each stove. Clicking it plays the valve choice (`ChoiceHotspot`) instead of a panel button |
| `sigorta.png` | `FuseBox` (Flat 11 hall, Flat 9 hall) |
| `lamba.png` | wall lamps on back walls (a Light2D practical sits on each) |
| `frame0000–0005.png` | the chandelier (`CeilingLamp` and the salon's `open_room` hazard). Frames 0–4 = a swing loop played during a quake; frame 5 = fallen on the floor (the drop hazard ends on frame 5) |
| `enkaz 1 .png`, `enkaz 2.png` | debris (Part C) |
| `kapi.png` | the NEW door art: replace `door.png` on all `Door`s (same flush-in-gap rules; flipX for right walls) |

- `ComfyUI_00045_.png` and `ComfyUI_00053_.png` are flat textures, not iso tiles. Do not use them in the world (local may use them for the main-menu background).
- Containers in Part D use these sprites (wardrobe, bookshelf, the cabinet = ComfyUI wardrobe).

## F. The robber, then the street finale (6 × 20, heading LEFT)
**The robber (confirmed by Bora): a scripted, unavoidable encounter on the F2 stairs, right before the street.**
An injured, panicked man (`npc.robber.name` = `Telaşlı Adam`) blocks the way down.
- **No matter what the player chooses, he takes our food and we cannot escape it.** The choices only change the dialogue flavour:
  - `Yalvar`
  - `Kısa kes`
  - `Ona bağır`
  Each choice gets its own reply line, and every branch ends the same way.
- **What he takes:**
  - ALL food items from the bag;
  - if there is no food, one water;
  - if there is neither, he finds nothing, says a bitter line, and leaves.
  - Key items are never taken.
- Show what was taken: a `MessageToast` per item, plus a short beat where his sprite runs off down the stairs.
- Then the player automatically continues to the street. No QTE, no death, no fight, no weapons.
- Stamina consequence: the street starts with low supplies on purpose. This makes the exhaustion system matter in the finale.
- Write his 4–5 lines and the 3 replies in the tone of the other NPCs: desperate, not evil. One line should show he has a child waiting too, so it lands emotionally rather than as a villain.
- **Code:**
  - `RobberNode` = DialogueNode + a `TakeItems` outcome, `TakeItems(filter = Food, fallback = Water x1, excludeKeyItems = true)` (pure, tested);
  - the BorrowLedger is untouched;
  - add a flag `RobbedAtExit` so the epilogue can mention it.
- Tests: food is all removed; the water fallback; the empty-bag line; key items are kept; every branch leads to the street node.

**The street:**
- One long room `street`: 20 cells long × 6 cells deep in grid space, with the exit on the FAR LEFT.
- Floor tiles: the Rocky sheet `Art/Concept/Floor_Rocky_01-256x128.png`, sliced like the other floor sheets (names `Floor Rocky N`). Use `Floor Rocky 17` for most cells (about 85%) and `Floor Rocky 3` as random patches (about 15%), seeded.
- No walls: a low kerb line and building façades as tinted silhouettes along the back edge (blocks are fine; local adds art later).
- **Camera:** in the street, `RoomCameraFraming` switches to follow mode. Ortho size is fixed to show about 7 cells of length; the camera follows the player along the street's long axis with smooth damping and is clamped to the street bounds.
- **The flow:**
  1. Enter from the building door at the right end.
  2. Run left past cars, debris and a fallen pole to the `Acil Toplanma Alanı` sign at the left end. That sign is the exit node → epilogue.
  3. Debris density uses QuakeCount 4, and the same never-block rule: the path to the sign must always exist.
- **This is where the quake is felt most (`StreetQuakeDirector`):** about 6s after entering, the 4th quake (the strongest) hits:
  - maximum camera shake (still scaled by Reduce Effects);
  - the screen tilts slightly;
  - `cue_quake_f4`, louder;
  - car alarms (generate `sfx_car_alarm_loop` with `gen_sfx.py`);
  - glass rain from the façades (`cue_glass_shatter` + particles);
  - 3–4 **façade drops**: a red warning shadow grows on 2×2 cells next to the buildings for 1.2s, then debris crashes there. A member under it → death (player) / Trapped (child) / wife cut-to-black, as with any hazard. New hazard `hz.street_facade`, tip `tip.street`: `Sarsıntı dışarıda yakalarsa binalardan, duvarlardan, direk ve tellerden uzak, açık alanda çök-kapan-tutun.` (title `tip.street.title`: `Dışarıda binalardan uzaklaş`);
  - a **sparking downed power line** across the middle of the street (a blocked line of cells with one gap). Clicking it or walking into the spark cells → electrocution death with `tip.electric`;
  - the family follows. While shaking, the input delay from panic applies.
- **During the shaking**, a placement-lite moment: an on-screen hint `Açık alana çök!`. If the player is standing in the open middle of the street (not within 1 cell of the façades or the power line) when the quake peaks, they survive. AFAD-correct: Drop-Cover-Hold in the open, away from buildings.
- The building timer keeps running. When it hits 0 in the street, the building collapses behind you (huge dust cloud, shake): death only if the player is still within 5 cells of the building door; otherwise it's just a cinematic beat. Make the timer text say this in the epilogue.
- The checkpoint at the street entry = macro rewinds land there.
- Tests: the street path from the door to the sign always exists for 200 seeds; the façade-drop target cells never include the only path cell.

---

## Handoff
Write the HANDOFF entry with a precise **"Needs local wiring"** checklist, at least:
- run the PropArt import;
- Generate Building Levels → Build Test Scene;
- the target heights to tune by eye;
- a test of every new interaction;
- the street camera follow;
- the street quake feel;
- the debris layouts on F3/F2;
- running placement under the timer.
