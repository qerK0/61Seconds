# HANDOFF LOG

Newest entry on top. Every Claude Code session (cloud or local) appends an entry here before finishing. Format is defined in CLAUDE.md.

---

## [2026-10-17 04:27] LOCAL — Real isometric tiles: Tilemap floors, back walls, tile sets, per-room camera framing
### Done
- **Missing files:** `Assets/_Game/Art/Tiles/` does not exist and none of the named files exist anywhere ("Wood Floor 13", "Floor Stone 0", "Flat Stone 0/1/12/13"). That includes the artist pack in `Downloads\Assets`, which uses other names (Floor_Stone_01, Floor_Wood_02, Flat_Wood_01…).
  - The Concept/ files are 768×768 **sprite sheets**, already grid-sliced in Unity as `<sheet>_<n>`: floors 3×6 at 256×128, walls 6×4 at 128×192.
  - So "Wood Floor 13" reads as sprite **13** of the wood sheet, and "Flat Stone 12" as sprite 12 of the wall sheet. The fallback uses exactly those indices.
- **Tile-set mapping:**

  | Set | Floor | Back-left wall "/" | Back-right wall "\" | Window walls |
  |---|---|---|---|---|
  | **HomeTileSet** | `Floor_Wood_01-256x128_13` | `Flat_Stone_01-SW-128x192_0` | `_1` | `Flat_Stone_01_WindowA-SW-128x192_0` / `_1` |
  | **CorridorTileSet** | `Floor_Stone_02-256x128-Photoroom_0` (tint 0.92) | `Flat_Stone_01-SW-128x192_12` | `_13` (tint 0.86) | WindowA `_12` / `_13` |

  - The wall sheet stores each material as a **pair of faces**: even index = "/" face, odd = "\" face. 0/1 and 12/13 are therefore the left/right faces of one material, not variants to alternate; alternating them on one edge zig-zags. Variation along a wall comes from a deterministic 3-step shade instead.
  - `RoomTileSet` still supports your spec: left-wall variants alternate by index, and flipX is used when no right sprites are set.
- **Rooms → sets:**
  - Home: Flat 11 (salon, kitchen, hall), 12, 9, 10, 7, 8.
  - Corridor: F4/F3/F2 corridors and the F2 stairwell.
  - The street uses the corridor floor with no walls.
- **1. Import settings** (`SixtyOne > Art > Apply Tile Import Settings`, re-runnable, also run by Generate Tile Sets and Build Test Scene):
  - Sprite, Bilinear, Uncompressed, no mips; floor pivot centre, wall pivot bottom-centre, set per slice through the 2D Sprite data provider.
  - Existing slices and sprite IDs are kept; unsliced sheets are grid-sliced from the `WxH` in the file name.
  - **PPU = 254 for all tiles, not 256.** Every diamond has an anti-aliased, darker edge pixel, and the transparent pixels are stored as magenta. At 256 that showed thin lines between tiles; 254 gives about 1px of overlap and fixes it through PPU, not scaling. Floor and walls share the PPU, so they still line up.
  - These are sheets, so they import as **Multiple**, not Single.
- **2. `RoomTileSet` ScriptableObject** (`Scripts/World/RoomTileSet.cs`): floor sprite + Tile, left/right wall variants, optional window sprites, floor/wall tint. `SixtyOne > Art > Generate Tile Sets` creates `Data/Art/HomeTileSet.asset`, `CorridorTileSet.asset`, `HomeFloorTile.asset` and `CorridorFloorTile.asset`, and logs every missing name with its fallback.
- **3. Room building** (`RoomTiles.cs`, used by `TestSceneBuilder`; `BuildingLayout` has the data):
  - **Floor:** a `Grid` (Isometric, cell size 1×0.5×1) plus a Tilemap per room covering the existing footprint and extra tiles. Tilemap cell centres match `IsoMath.GridToWorld` exactly (checked: cell (2,1) = (0.5, 1.0)), so every prop, anchor, spawn and hidden hotspot already sits on the new floor. The TilemapRenderer (Chunk) uses order −30000, under everything.
  - **Walls:** one 128×192 segment per tile edge on both back edges, at the edge midpoint minus the art's 32px base inset, order −29500. That's above the floor, below all IsoSorter props, decals and characters. No colliders, so clicks are unaffected.
  - **Door gaps and windows** (codes `L<x>` / `R<y>` in `BuildingLayout`):
    - salon door L3, balcony R0, window R1
    - kitchen window R1
    - hall door R3
    - F4/F2 corridors flat doors L1 + L4
    - F3 corridor doors L1, L3, stair R1, window L5
    - Flat 7 window R1
    - Flat 12 and Flat 8 window R2
    - stairwell stairs-down L1
    - every flat has a doorway (L0 or L1)
  - Re-anchored to segment centres so they meet the gaps/windows: salon doorway anchor + lintel x 4.0 → 3.5; salon window anchor/prop/light y 1.6 → 1.5; Flat 7 window y 1.6 → 1.5; F3 window anchor + prop x 5.3 → 5.5; hall door 3.6 → 3.5. All colliders unchanged.
- **Camera framing** (folded in because rooms were clipped by the HUD; the previous framing task was not in HANDOFF):
  - `CameraRig` → Main Camera. New `RoomCameraFraming` on the rig fits each room's `Room.ViewBounds` into the HUD-free viewport (x 0.02–0.85, y 0.41–0.83). The bounds are floor + walls + all room sprites, computed by the builder.
  - It sets `CinematicDirector.BaseOrthoSize` (new property), so the cinematic zoom, tilt and CameraShake still work on the camera itself.
- **4. Lighting:** the TilemapRenderer and wall sprites use URP `Sprite-Lit-Default`. The real wood floor is much darker than the old placeholders, and after the quake it went fully black. Each room now gets a dim cool `Light_AmbientFill` Point Light2D (intensity 0.6). It is not a practical, so the director never kills it, and the floor pattern stays faintly visible after the quake on all three floors.
- **Layout fix found in the screenshots:** the AFAD alert covered the room and blocked anchor taps for 3.5s of each 9–10s placement. It moved to the bottom strip (where choice buttons go; placement has none). A UI raycast check confirms no member/anchor tap hits UI during the alert.
- Validate Content 0, Text > Validate 0, **EditMode 95/95**, console clean (only the "no audio clip" warnings).

### Screenshots (`Captures/`, git-ignored)
- Before: `t0_before_*` (edit-mode placeholder rooms) and the earlier `b*`/`c*`/`d*` play shots.
- After, all at play-time framing with HUD:
  - F4: `u01_f4_placement`, `u02_f4_salon_postquake`, `u03_f4_kitchen` (window wall), `u04_f4_hall` (door gap), `u05_f4_corridor` (2 door gaps), `u06_f4_flat12`
  - F3: `u07_f3_placement`, `u08_f3_corridor` (window + 3 gaps), `u09_f3_flat9`, `u10_f3_flat10`
  - F2: `u11_f2_placement`, `u12_f2_flat7`, `u13_f2_corridor`, `u14_f2_flat8`, `u15_f2_stairs`, `u16_street`
  - `v01`/`v07`/`v11`: the three placement moments after the AFAD move (room fully visible, alert below the instructions).
  - `u20_zoom_corridor`, `w03_zoom_wood_lit`: close-ups. No gaps; the remaining fine lines are the stone texture's grooves and the wood tiles' own bevel edge.
- No room is clipped by the HUD or dialogue. Rooms are smaller on screen than before (ortho 3.2–5.4) because they must fit into the 42%-high free band.

### Files created / changed
- Scripts/World/RoomTileSet.cs (new) — tile-set ScriptableObject
- Scripts/Iso/RoomCameraFraming.cs (new) — per-room framing on the camera rig
- Scripts/Placement/Room.cs — `viewBounds` / `ViewBounds`
- Scripts/Cinematic/CinematicDirector.cs — `BaseOrthoSize` property
- Scripts/Editor/Art/TileArt.cs (new) — import settings, name lookup + Concept fallback, Generate Tile Sets
- Scripts/Editor/SceneBuild/RoomTiles.cs (new) — Tilemap floor, wall segments, ambient fill, view bounds
- Scripts/Editor/SceneBuild/BuildingLayout.cs — RoomArt / Walls / Gaps / Windows per room, re-anchored doors and windows
- Scripts/Editor/SceneBuild/TestSceneBuilder.cs — tile rooms, camera rig + framing
- Scripts/Editor/SceneBuild/TestSceneUi.cs — AFAD alert moved to the bottom strip
- Scripts/Editor/SerializedWire.cs — `SetRect`
- Scripts/Editor/SixtyOne.Editor.asmdef — + `Unity.2D.Sprite.Editor` (sprite data provider)
- Data/Art/* (new) — HomeTileSet, CorridorTileSet, HomeFloorTile, CorridorFloorTile
- Art/Concept: the 4 used sheets + `.meta` (PPU/pivot/filter settings). The ComfyUI concept images stay untracked.
- Scenes/Test_Building.unity — rebuilt

### Needs local wiring (checklist for the MCP session)
- [ ] When the artist delivers `Art/Tiles/` with the real names, run `SixtyOne > Art > Generate Tile Sets` and then `Build Test Scene`. Name matches take priority over the Concept fallback automatically.
- [ ] Decide whether wall "variants" should be different materials: e.g. add `Flat Stone 2/3` as a second left/right pair in `TileArt.Generate`.
- [ ] Hand-check the room size on a 16:10 or ultra-wide window: framing adapts to the aspect, but only 16:9 was tested.

### Not verified / risks
- The fallback reading of "Wood Floor 13" etc. as sheet indices is an inference from Unity's slice names. If the artist meant different files, the tile sets will change once `Art/Tiles` exists.
- The ambient fill (0.6) brightens the pre-quake look a little too. Tune `AmbientFillIntensity` in `TestSceneBuilder` if Act 1 looks too flat.
- The bag-full and other HUD toasts still appear mid-screen over the room (transient).

### Next suggested step
- Artist: deliver `Art/Tiles` with the expected names, or confirm the sheet indices. Then add prop sprites (table, wardrobe, doors) in place of the placeholder blocks.

---

## [2026-10-17 03:39] LOCAL — Building design verified in Unity: full route played twice with real timing, 5 bugs fixed, timers tuned
### Done
- **Compile:** the cloud code compiled first time with 0 errors and 0 warnings. Unity generated the `.meta` files. The APIs flagged as first-time all work: `GetComponentInParent<T>(bool)`, `Image.fillMethod`/`fillAmount` (click ring), `ContactFilter2D.noFilter`. No asmdef changes were needed.
- **Checklist:**
  - Generate Building Levels deleted the old Act1/Act2 nodes, items and `tip.leave_quickly`. No missing-script assets were left under `Data/`.
  - Generate Profiles kept the tuned Act 1–3 values.
  - Build Test Scene created `Test_Building.unity` (13 rooms) and deleted `Test_Act1`.
  - **Validate Content: 20 nodes, 0 problems. Text > Validate: 252 referenced, 0 missing/empty/unused. EditMode: 95/95 passed.** All three were re-run after my fixes with the same results.
- **Played the full route**, first step by step with checks, then twice as a timed run in Play mode. Each choice took 3s, each dialogue line 2.5s and each placement tap 1s, and every click went through the real `InputDelayRouter`.
  - Intro lines tick the timer: 4+4+3+3 = 14s, paused while a line is showing. "Kısa kes" skips them, sets `IntroCutShort` and gives no whistle.
  - Opening quake placement: wife + child on the table, player on the inner wall. Everyone survives.
  - Kitchen: poison rose 0.025/s, the overlay darkened and the valve stopped it. The gloves drawer works.
  - Hall breaker, then the exit.
  - Hasan (food + water).
  - Flat 12: borrowed cloth, rope and tape, left the note, and crafted cloth + rope into the harness.
  - F3 aftershock, then the trader. The trader gave the first missing need (tape in run B); with a full bag it shows "Bag full" and the gift is lost.
  - Flat 9: repair, then the fuse panel. A wrong try costs 3s; 1,3,4 ON is accepted. Then the cables (12s, −25 stamina).
  - F2 strong aftershock, then the Flat 7 valve.
  - Dog: the whistle works (kept), food works (consumed), and walking past = injured + pushed to Flat 7. An injured move costs 1.25×.
  - Stairs, then the street epilogue (family line, borrow line by ledger + note, loop count).
- **Extra checks:**
  - Last-30s alarm: fires at exactly 30.0, ticks 30/29/28…, and the timer turns red.
  - Collapse at 0: `death.collapse`, then a macro rewind to `f2.aftershock` with the saved **93.0s**. Flags reset, family snapped to spawns, LoopCount +1.
  - Stamina 0: the heavy cables choice is locked ("exhausted"), and a normal 2s move cost 3s (1.5×).
  - Click delay: 0 at panic 0.5, 0.08s at 0.78, 0.30s at 1.0. Panic only rose above 0.7 right after aftershocks.
  - Hidden-item sparkle: it switched on after 25s in the hall (alpha 0.89). It is very small on screen.
  - Flat 10 lighter: `death.lighter_explosion` + `tip.no_flame`, micro rewind.
  - Electrocution without gloves: a ShowLockedFatal death card naming the missing "Lastik eldiven" + `tip.electric`, micro rewind back to Flat 9 with the gloves.
- **Bugs found and fixed:**
  1. **ClickFeedback swallowed every click after the first** (game-breaking with a real mouse). An instant click ran inside the coroutine before `StartCoroutine` returned, so `pending` stayed set forever. Instant clicks now run synchronously and only delayed clicks use `pending`. Run A: 57 routed clicks, 0 dropped.
  2. **The building timer kept running on the street:** the player could "collapse" after winning. `SurvivalController` now stops ticking, poison and panic on an `EpilogueNode`. Verified: T stayed at 57.1 after 5s on the street.
  3. **Flat 7 gas built up during the 7s placement and the quake:** the player arrived with poison 0.66 and a near-black screen. Gas no longer builds while a `PlacementNode` is active. Arrival poison is now 0.03.
  4. **Used interactables kept their "what you get here" label** (e.g. "Lastik eldiven" on the empty kitchen drawer). `WorldInteractable.Refresh` now hides child TMP labels once used.
  5. **Fuse panel layout:** the toast ("wrong pattern", hints) drew behind the panel's "Şalteri kaldır" button. The toast is now built after the fuse panel (on top). The panel moved to y 0.53–0.81, clear of the toast and of the timer caption.
- **Balance**, from timed play at the pace above (300s timer):
  - **Route A** (whistle intro, gloves, harness, Flat 9 fuse + **cables**): **57.1s left**. That run used the old 8s/7s aftershock timers and failed both placements once; macro rewinds restore the timer, so the deaths cost nothing.
  - **Route B** ("Kısa kes", flashlight, **Flat 10** hole, dog food): **83.0s left**, 0 deaths.
  - Segment cost: **cables ≈ 32.7s** (Flat 9 + repair + fuse + 12s descent, plus about 14s of F4 prep for gloves/tape/harness) vs **Flat 10 ≈ 27.7s** (+1s flashlight pickup).
  - Total action costs on Route A ≈ 114s; the other ≈ 130s is real time (placements, quakes, ~57 decisions).
  - The aftershock placement timers were too tight: 6 taps plus the AFAD popup. **F3 8 → 10s, F2 7 → 9s**, after which Route B placed everything first try.
  - Kept **buildingSeconds = 300**. A good first-timer on the main route lands at about 50–57s at 3s/decision. A slower reader (4s/decision) would lose about 40s more, so watch real playtests.
- **Look:** Acts 2/3 were crushed to near black on real rooms (F2 corridor/Flat 7). Raised post-quake light, and the poison now clears 3× faster so rooms read again soon after the valve.

### Final values
- **LevelConfig:**
  - unchanged: buildingSeconds 300, pulseAt 60, alarmAt 30, realtimeTick on, pauseInDialogue on, injuredMoveExtraPerMember 0.25, staminaMax 100, staminaDrainPerSecond 0.15, staminaZeroTimeMultiplier 1.5, gas kitchen 0.025 / flat10 0.03 / flat7 0.035 per s, wetClothFactor 0.5, panicFromPoison 0.6, aftershockPanic 0.45, panicCalmPerSecond 0.04
  - changed: **poisonDecayPerSecond 0.01 → 0.03** (also the code default)
- **Nodes (generator):** f4.quake 10s, **f3.aftershock 8 → 10s**, **f2.aftershock 7 → 9s**.
- **GameSettings** (unchanged): panicDelayThreshold 0.7, maxInputDelay 0.3, hiddenItemSparkle on, hiddenSparkleAfterSeconds 25, reduceEffects off, reducedShakeScale 0.35.
- **Cinematic:** CinematicAct2 postQuakeGlobalLight 0.34 → **0.38**, postQuakeExposureOffset −0.35 → **−0.30**. CinematicAct3 postQuakeGlobalLight 0.28 → **0.34**, postQuakeExposureOffset −0.40 → **−0.30**. Act 1 unchanged.

### Screenshots (`Captures/`, git-ignored)
- `b01_intro`: HUD reads well (04:56 timer, loop, notes, bag, stamina "Güç"); Sevim's line with "Devam" / "Kısa kes".
- `b02_f4_placement`, `b08_f3_aftershock`: the AFAD popup covers the top half of the room during placement (it fades out). The tap hint and the "-3 sn" spent label show.
- `b03_f4_after_quake`: the salon after the quake, family on their anchors.
- `b04_kitchen_poison`: the poison overlay visibly darkens the room. `c01`/`d01`: kitchen before/after the label fix.
- `b05_hall_sparkle`: the sparkle is a tiny white dot, easy to miss.
- `b06`/`b07`/`c02`: the bag with labels, the crafted "Çocuk taşıma kuşağı", the note toast, the bag-full red flash.
- `b10_electrocution_death`, `b18_collapse_death`: death cards with the missing item / tip / GERİ SAR.
- `b11_fuse_panel` (old layout, toast behind the button) → `c04_fuse` (fixed).
- `b09`/`c03` F3 corridor, `b13`/`c05` Flat 7, `b14`/`b15`/`c06` F2 corridor (before/after the brightness and poison fixes). `d02`/`d03`: Flat 10 dark, then lit by the flashlight.
- `b17_alarm_last30`: red 00:28.
- `b16`/`c07`/`d04`: the street epilogue (warm light, family together, borrow/note/loop lines).

### Files created / changed
- Assets/_Game/Scripts/UI/ClickFeedback.cs — instant clicks run synchronously (fix 1)
- Assets/_Game/Scripts/Building/SurvivalController.cs — no ticking on the epilogue; no gas build-up during placement (fixes 2 and 3)
- Assets/_Game/Scripts/World/WorldInteractable.cs — hide labels on used interactables (fix 4)
- Assets/_Game/Scripts/Editor/SceneBuild/TestSceneUi.cs — toast above the fuse panel, panel moved (fix 5)
- Assets/_Game/Scripts/Building/LevelConfig.cs — poisonDecayPerSecond default 0.03
- Assets/_Game/Scripts/Editor/Content/BuildingLevelsGenerator.Floor3.cs / .Floor2.cs — aftershock timers 10s / 9s
- Assets/_Game/Data/** — generated building content (Floor4/3/2 nodes, items, hazards, recipes, traders, tips, LevelConfig, cue_building_alarm); tuned CinematicAct2/3; GameSettings serialized new fields
- Assets/_Game/Scenes/Test_Building.unity (new), Test_Act1.unity (deleted by the builder)
- Editor-generated `.meta` files for the cloud scripts and tests
- Not committed: `Assets/_Game/Art/Concept/` (left untouched, untracked) and the TMP `LiberationSans SDF - Fallback.asset` atlas churn

### Needs local wiring (checklist for the MCP session)
- [ ] Hand-play once with a real mouse to confirm fix 1 in the UI: choices, anchors and world clicks all respond repeatedly.
- [ ] Assign audio clips: every cue is still silent, including the new `cue_building_alarm`.
- [ ] Make the hidden-item sparkle bigger or brighter once the painted overlays exist.
- [ ] Optional: remove `com.unity.collab-proxy`. It flipped VersionControlSettings to "Unity Version Control" again this session; I reset it each time.

### Not verified / risks
- Balance numbers come from scripted real-time play at a fixed pace, not a human. Watch the 300s total with a slow reader.
- A full bag silently loses the gift from Hasan and the trader (the flags are still set). Consider refusing or deferring the gift when the bag is full.
- The F2 corridor body text still says the dog blocks the way after `DogGone`. The epilogue says "0 kez yaşadın" for a no-death run. Both are writer TODOs.
- Act 2/3 post-quake looks are readable now but still dark with placeholder art. Re-check with real art.
- A test-harness side effect: `Application.runInBackground` persisted `PlayerSettings.runInBackground: 1`, and the Editor rewrote `QualitySettings` per-platform defaults. I reverted both files.

### Next suggested step
- LOCAL or the team: one human playthrough with mouse + sound to confirm feel and the 300s balance, then the writer replaces the `TODO:` texts.
- CLOUD: handle the full-bag gift loss (trader/Hasan) and make the corridor body text conditional on `DogGone`.

---

## [2026-10-16 23:45 UTC] CLOUD — Major design change: one quake, 300s building timer, floors 4→3→2 with checkpoints, survival systems, crafting, epilogue
### Done
- **Building timer replaces the 61s prep.** `BuildingTimer` (pure) + `SurvivalController`: 300s on `Data/LevelConfig.asset`, pulse at 60s, alarm + ticks in the last 30s, collapse at 0 = death with a macro rewind. Every action cost (choices, interactables, dialogue lines, crafting, item use) subtracts from it via `ActionCostRouter`. Moves cost extra per injured member.
- **Checkpoints:** `SceneNode.isCheckpoint` (f4.intro, f3.aftershock, f2.aftershock). `RewindTarget.Checkpoint` = macro (restores timer, stamina, poison, borrow ledger, family positions); `LastNode` = micro. LoopCount, tips, revealed anchors and KnowledgeSet persist.
- **Floor 4:** intro dialogue (Sevim; "Kısa kes" loses the whistle and the Flat 12 info) → opening quake placement → salon/kitchen (valve)/hall (breaker) → corridor (Hasan gives food + water) → Flat 12 (borrow + "Not bırak").
- **Floor 3:** aftershock placement → corridor trader (needs-aware) → Flat 9 fuse puzzle (gloves or insulated grip + tape, else electrocution) → cable descent (heavy; needs the child harness) / Flat 10 (dark, gas, lighter = explosion, flashlight, collapsed floor).
- **Floor 2:** strong aftershock placement in Flat 7 (gas leak) → corridor dog gate (food is consumed, whistle is kept, walking past = bitten + injured + pushed back) → Flat 8 → stairs → street epilogue (family line, borrow line by ledger + LeftNote, loop count).
- **Systems:**
  - panic + input delay (0 below 0.7, at most 0.3s, instant ring feedback, Reduce Effects = 0)
  - stamina (HUD bar; drains over time and per heavy action; food/water restore it; at 0 heavy actions lock and everything costs more time)
  - poison (gas zones per room with a valve flag; darkening overlay + director vignette/saturation; the wet cloth halves it)
  - crafting (3 recipes in `Data/Recipes`; BagUI: tap an item, tap again to use, tap another to combine)
  - DialogueNode (per-line time cost)
  - hidden item hotspots (salon food, hall whistle, Flat 10 tape, Flat 8 water)
- **ContentValidator** now runs node checks again, plus:
  - dialogue lines (keys, cost ≥ 0)
  - recipes
  - traders
  - every gas room has a valve choice
  - unreachable checkpoints
  - placement rooms have safe + trap anchors and an unplaced hazard
  - no firearm ids
- **Text:** `strings_tr.csv` has 252 rows; every new key has a `TODO:` placeholder.
- **Tests:** 95 EditMode tests pass under the mono runner (new: BuildingTimer, Dialogue, Survival, Floor3Rules, Floor2Rules, HiddenItem). The generator was also run under mono against the real CSV:
  - 0 missing or unused keys
  - all 20 nodes reachable
  - validator: 0 problems; the negative checks fire
  - 11 scripted playthroughs pass, including both routes to the epilogue. The cable route used 114s of 300 in action costs (realtime drain not included).
- CLAUDE.md "The Game", folder tree and Core concepts are updated.
- **Why one scene:** one GameManager, GameState and UI, so floor checkpoints and rewinds never load scenes; `RoomSwitcher` shows one room and the family snaps to its spawns.

### Files created / changed
- Runtime:
  - Building/ (LevelConfig, BuildingTimer, SurvivalModel, SurvivalController, GasZone, EpilogueNode/Text/Controller)
  - Dialogue/, Items/ (Recipe, RecipeBook, Crafting, ItemUse, TraderProfile, TraderLogic), Puzzles/
  - World/ (renamed from Prep; WorldInteractable, PickupItem, HiddenItemHotspot)
  - Core routers, Snapshot/GameState/GameSession, Choice/ChoiceView/Condition/Outcome/NodePlayer, PlacementNode/Controller
  - CinematicDirector/Mixer/Profile, IsoPicker, CameraShake
  - UI: BagUI, ChoicePanel, ChoiceButton, BuildingTimerDisplay, StaminaBar, PoisonOverlay, ClickFeedback, FusePuzzlePanel
- Removed: Prep/* (PrepPhaseNode, PrepCountdown, PrepController…), CountdownDisplay, ActionProgressBar, RoomTabs, SampleAct1Generator, and the Prep tests.
- Editor:
  - BuildingLevelsGenerator(.Floor4/.Floor3/.Floor2).cs
  - SceneBuild/BuildingLayout, HiddenItems, SceneKit, TestSceneBuilder, TestSceneUi, TestSceneCinematic
  - ContentValidator
- Tests: BuildingTimerTests, DialogueTests, SurvivalTests, Floor3RulesTests, Floor2RulesTests, HiddenItemTests.
- `Content/Text/strings_tr.csv`, `CLAUDE.md`.

### Needs local wiring (checklist for the MCP session)
1. `git pull`, open Unity and let it compile. Expect 0 errors. New scripts have no .meta files yet (the cloud never writes them), so Unity creates them on import.
2. **SixtyOne > Generate Building Levels.**
   - It deletes the old `Data/Nodes/Act1`, `Data/Nodes/Act2`, the old items (console, crowbar, gold, laptop, wrench) and `tip.leave_quickly`.
   - It creates the `Floor4/3/2` nodes, `Traders/`, `Recipes/` and `LevelConfig.asset`.
   - If any asset with a missing script is left under `Data/`, delete it.
3. **SixtyOne > Cinematic > Generate Profiles.** Floor stages 1–3 map to CinematicAct1–3.
4. **SixtyOne > Build Test Scene.** This builds `Scenes/Test_Building.unity` and deletes `Test_Act1.unity`.
5. With the scene open, run **SixtyOne > Validate Content**. Expect 0 problems; the placement anchor checks only run while the scene is open.
6. Run **SixtyOne > Text > Validate**. Expect no missing or unused keys.
7. Run the Test Runner (EditMode). Expect 95 passing.
8. Play-through in Play mode:
   1. Intro lines tick the timer. "Kısa kes" skips them.
   2. Opening quake placement (10s).
   3. Kitchen: the gas raises poison, the overlay darkens and the valve stops it.
   4. Hall breaker → exit.
   5. Hasan → Flat 12: borrow cloth + rope, leave a note.
   6. Combine cloth + rope in the bag into the harness.
   7. Stairs → F3 aftershock.
   8. Trader → Flat 9: repair needs gloves/grip + tape, then the fuse panel pattern 1,3,4 ON → cables.
   9. F2 strong aftershock → Flat 7 valve → dog (whistle or food) → stairs → out → epilogue text.
   10. Also check: the last-30s alarm, collapse at 0 → floor checkpoint, stamina 0 locking heavy moves, the click ring and delay at high panic, and the hidden-item sparkle after 25s.
9. Tune `LevelConfig` (timer, stamina drain, poison rates, panic) and `GameSettings` (panic threshold, maxInputDelay, sparkle) by feel.

### Not verified / risks
- Compiled only with mcs against API stubs. The editor-side Unity APIs that are new this session:
  - `Component.GetComponentInParent<T>(bool)`
  - `Image.fillMethod`/`fillAmount` for the click ring
  - `ContactFilter2D.noFilter`
- Not seen in Unity at all yet:
  - the UI layout (stamina bar, building timer, fuse panel, poison overlay)
  - camera framing for the new rooms (f2_corridor, flat8, f2_stairs, street)
  - the family snapping between floors
- `AssetDatabase.FindAssets("t:SceneNode")` is relied on for subclasses; the validator also loads each subtype explicitly to be safe.
- Balance is untested. 300s may be generous: the scripted action costs for the full cable route are about 114s, while the realtime tick runs on top.
- The epilogue is text only (no buttons, no restart). The win look comes from the `CinematicDirector` exit node `f2.epilogue`.

### Cut / simplified
- Crafting is "tap item A, then item B" in the bag. There is no drag-and-drop.
- Flat 8 is an empty placeholder with one hidden item (water), as specified.
- Hidden items use placeholder "painted overlay" blocks until the artist's room art exists. Swap `PaintedOverlay` for the painted sprite.
- Nothing from the priority list was cut.

### Next suggested step
- LOCAL: run the checklist, take screenshots of each floor, and tune timer and stamina numbers.
- The writer: replace the `TODO:` texts in `strings_tr.csv`.

---

## [2026-10-17 00:50] LOCAL — Cinematic look verified in Unity, Act 1–3 profiles tuned, world labels made readable
### Done
- **Compile:** Safe Mode is exited. The `Unity.RenderPipelines.Universal.2D.Runtime` reference (needed for Light2D) is in both asmdefs. 0 errors. Fixed the 2 remaining warnings: `ContactFilter2D.noFilter` in IsoPicker, and removed the obsolete `VolumeProfile.isDirty` line in CinematicDirector (not needed for a runtime profile).
- **Risky APIs from the cloud handoff all work for real:**
  - `Light2D.lightType` / `pointLightOuterRadius` setters
  - `GetUniversalAdditionalCameraData()` (Post Processing = on)
  - `VolumeProfile.Add<T>` + `AddObjectToAsset` (`CinematicBaseVolume` has 8 overrides)
  - ParticleSystem setup in edit mode
  - the shader `Universal Render Pipeline/2D/Sprite-Unlit-Default`
  - Dust renders white, **not magenta**, so no manual material was needed.
- **Checklist:** Generate Profiles → Generate Sample Act 1 → Build Test Scene all ran clean. Main Camera post-processing is on. All 6 Light2Ds target sorting layer 0 (Default), which is the project's only layer. Validate Content: 0 problems. Text > Validate: 145 referenced, 0 missing/empty/unused. **EditMode 70/70 passed.**
- **CSV lock fix:** `strings_tr.csv` was open in LibreOffice. `File.ReadAllText` then threw "Sharing violation", which broke both validators and 1 test. Added `TextDB.ReadCsvFile` (FileShare.ReadWrite) and used it in TextDB, TextMenu, TextCsvPostprocessor and CsvParserTests. The writer can now keep the sheet open.
- **World labels:** after tuning, the white TMP labels turned grey on light tiles (ACES + negative EV). New `WorldLabelStyle` gives prep and anchor labels a shared `Data/WorldLabel_SDF.mat`: HDR face 1.7, black outline 0.18 and an underlay. Build Test Scene applies it. They now read crisp white in every phase.
- **Play-tested loops 1–2 + screenshots** (saved in `Captures/`, which is git-ignored):
  - **T=61 prep start** (`01_*`): AFAD popup + 61 countdown readable; room moody but every prop visible. Grey labels were low-contrast, which led to the label fix.
  - **T=10** (`02_T10_letterbox`): ÇÖK-KAPAN-TUTUN alert + red countdown. The letterbox bars are there but hard to see because the area outside the room is already black. Heartbeat vignette pulses on each tick.
  - **Quake** (`03_quake-1`): dust specks across the screen, chromatic fringe on edges, lights flickering, "SARSINTI! (6,0)" readable. Measured peaks: flash +1.6 EV, chromatic 0.77, lens 0.20, roll 0.69°, cam offset 0.153.
  - **Post-quake, no flashlight** (`04_postquake_noflashlight`): before tuning, the back walls were nearly crushed. After tuning, floor/characters/labels read and the walls are dark but separable.
  - **Post-quake, flashlight** (`04b`, `12_tuned_*`): warm pool around the player, and the rest of the room stays readable.
  - **Death** (`03a`, `05a_*`, `05_death_card_unplaced`): blends to sat −100 / contrast 40 / vignette 0.6 (values confirmed on the volume). The card fades in 0.5s later; text, Deprem Notu and GERİ SAR are all clear.
  - **Rewind** (`06_rewind` micro → placement at T=10, LOOP #1, bag kept; `06b_rewind_macro` → 61s, LOOP #2, empty bag): RewindFX scanlines + grain/aberration spike.
  - Loop 2: flashlight + crowbar from the bedroom, placement table/table/inner wall → everyone safe → explore.
- **Reduce Effects ON** (measured during the quake): roll 0°, chromatic 0, lens 0, no flash (exposure stayed at the −0.15 base), cam shake offset 0.044 vs 0.153 (≈0.29×, matches the 0.35 scale). Set back to OFF afterwards.
- **Final tuned values** (Act 1 tuned from the screenshots; Acts 2/3 step darker and harsher from it):

  | field | Act 1 | Act 2 | Act 3 |
  |---|---|---|---|
  | postExposure | −0.15 | −0.30 | −0.45 |
  | contrast | 12 | 18 | 24 |
  | saturation | −20 | −28 | −36 |
  | vignette / smoothness | 0.32 / 0.55 | 0.37 / 0.50 | 0.42 / 0.45 |
  | grainIntensity | 0.20 | 0.28 | 0.36 |
  | globalLightIntensity | 0.80 | 0.70 | 0.60 |
  | prepVignetteEnd / prepSaturationEnd | 0.50 / −45 | 0.55 / −53 | 0.60 / −61 |
  | postQuakeGlobalLight | 0.40 | 0.34 | 0.28 |
  | postQuakeExposureOffset | −0.30 | −0.35 | −0.40 |
  | postQuakeVignette / postQuakeSaturation | 0.48 / −45 | 0.52 / −53 | 0.56 / −61 |
  | rotationalShake° / dutchTilt° | 0.5 / 0 | 0.85 / 0 | 1.2 / 2.5 |
  | chromaticSettle / lensWobble | 0.10 / 0.20 | 0.15 / 0.26 | 0.20 / 0.32 |
  | dustBurstCount | 80 | 140 | 200 |
- `VersionControlSettings` was flipped to "Unity Version Control" twice by the Editor. Reset to **Visible Meta Files** (checked on disk).
### Files created / changed
- Assets/_Game/Scripts/SixtyOne.Runtime.asmdef, Editor/SixtyOne.Editor.asmdef — + `Unity.RenderPipelines.Universal.2D.Runtime` (Light2D)
- Assets/_Game/Scripts/Iso/IsoPicker.cs, Cinematic/CinematicDirector.cs — obsolete-API warnings fixed
- Assets/_Game/Scripts/Text/TextDB.cs (+ReadCsvFile), Editor/Text/TextMenu.cs, Editor/Text/TextCsvPostprocessor.cs, Tests/EditMode/CsvParserTests.cs — share-tolerant CSV reads
- Assets/_Game/Scripts/Editor/SceneBuild/WorldLabelStyle.cs — new; TestSceneBuilder.cs applies it to both label kinds
- Assets/_Game/Data/Cinematic/* — generated + tuned CinematicAct1–3, CinematicBaseVolume, CinematicDust.mat
- Assets/_Game/Data/WorldLabel_SDF.mat — generated label material
- Assets/_Game/Data/GameSettings.asset — new reduceEffects/reducedShakeScale fields serialized (reduceEffects = 0)
- Assets/_Game/Scenes/Test_Act1.unity — rebuilt by Build Test Scene
- Cloud script/test `.meta` files (Editor-generated); .gitignore (`.~lock.*#`, `/Captures/`)
### Needs local wiring (checklist for the MCP session)
- [ ] Hand-feel pass with real mouse + focused Game view: letterbox slide, heartbeat rhythm and the flicker timing on the real frame rate
- [ ] Move overlapping bedroom labels apart ("Komodini aç: el feneri" vs "Yatağın altına bak: levye") in TestSceneBuilder specs
- [ ] Assign audio clips to the cues (still all silent)
- [ ] Optional: remove `com.unity.collab-proxy` from Packages/manifest.json; it keeps flipping the VCS mode (needs sign-off)
### Not verified / risks
- Acts 2 and 3 were tuned by extrapolation only; no Act 2/3 scene exists yet, so they were never seen on screen.
- The letterbox is barely visible because the scene background is already black. Once real room art fills the frame, check whether 2.35:1 still reads.
- **MCP play-testing caveat:** with the Editor unfocused, Play Mode only advances when `Application.runInBackground = true` is set at runtime. Twice during play, a script recompile + domain reload reset the session mid-run (`GameManager.Instance` null). This came from the MCP tooling, not game code. "Recompile After Finished Playing" (EditorPrefs) stopped it; I restored the pref to default afterwards.
- The win look is still untested (no exit node yet).
### Next suggested step
- CLOUD: Act 2 content (floor 4, 7.0 quake, short/zero prep), then a LOCAL pass to eyeball the CinematicAct2 values on a real scene.

---

## [2026-10-16 22:10] CLOUD — Cinematic look: CinematicDirector + per-act CinematicProfiles (dark, dramatic, event-driven)
### Done
- Rebased on top of the 23:05 LOCAL entry (prep wiring, family click fix); no code conflicts. Timestamp is the cloud clock (UTC); local entries use local time, which is why 22:10 sits above 23:05.
- **This was requested as a LOCAL session with MCP Play Mode verification, but it ran in the cloud.** No Unity Editor or Unity MCP server was reachable (nothing on localhost:8080, no MCP tools). Code and generators are done. **No screenshots were taken and no visual tuning was done**; that is the first item in the checklist below.
- **Render setup (from project files):** every quality level uses `Assets/Settings/UniversalRP.asset`. Its only renderer is `Renderer2D.asset` (**2D Renderer**), with post-process data assigned and HDR on. So: Light2D + post-processing. The director forces `renderPostProcessing = true` on the camera at runtime, and the scene builder sets it too.
- **New `Scripts/Cinematic/`:**
  - `CinematicProfile` (SO per act; every value has a [Tooltip]; `ApplyActDefaults(act)` makes each act darker, more desaturated and harsher, and only Act 3 gets the 2.5° dutch tilt).
  - `CinematicMixer` (pure layered blend: prep / heartbeat / post-quake / death / rewind / win / flash / quake aberration).
  - `CinematicDirector` (deep-copies the volume profile at runtime, never edits the asset; unscaled time only; never touches `Time.timeScale`; reacts to NodeEntered, PrepTick, QuakeStarted/Ended, Died, Rewound).
  - `LetterboxBars` (2.35:1, first child of the canvas so all UI stays on top).
- **Behaviour:**
  - Base look: ACES, color adjustments (−EV, +contrast, −20 sat, cool filter), split toning (cold shadows / warm highlights), vignette 0.35, subtle grain, low bloom (threshold 1.1).
  - Prep: curve-driven vignette 0.35→0.55, saturation −20→−45, 8% ortho push-in over 61→0.
  - Placement window: letterbox slides in over 0.4s, a vignette heartbeat on every countdown tick, audio low-pass at 1800 Hz.
  - Quake: 0.08s exposure flash, chromatic 0→0.8→0.1, lens wobble, 3–5 light flickers then power off (unless `keepLightsFlag`), dust burst, rotational shake on top of CameraShake, dutch tilt in Act 3.
  - Post-quake: darker (−0.4 EV, global light 0.3), and a soft player light when `flashlight` is in the bag.
  - Death: 0.5s blend to sat −100 / contrast 40 / vignette 0.6, and DeathCard now waits 0.5s (`showDelaySeconds`; the wife's hard cut is unchanged).
  - Rewind: chromatic + grain spike over 0.9s, alongside RewindFX; the base look is restored.
  - Win: warm, low-vignette blend on any `exitNodeIds` node.
- **Accessibility:** `GameSettings.ReduceEffects` turns off the flash, chromatic aberration, lens wobble, tilt and rotational shake, and scales CameraShake by `reducedShakeScale` (0.35).
- **Other:** `AudioService.SetLowPass/ClearLowPass` (AudioLowPassFilter on the listener). The asmdefs now reference `Unity.RenderPipelines.Core.Runtime` + `Unity.RenderPipelines.Universal.Runtime`.
- **Editor:**
  - New menus SixtyOne > Cinematic > Generate Profiles and > Reset Profiles To Act Defaults. They create `Assets/_Game/Data/Cinematic/CinematicAct1-3.asset`, `CinematicBaseVolume.asset` and `CinematicDust.mat`. Existing profiles keep their tuned values.
  - Build Test Scene now adds: a global Volume (priority 10), a Global Light2D, 4 warm practical Light2Ds (living-room ceiling lamp, window, kitchen lamp, bedside lamp), the flashlight Light2D, the QuakeDust particle system, the letterbox and CinematicDirector (all wired).
- **Gameplay timing:** unchanged. No countdown, timer or `Time.timeScale` changes. CameraShake still runs on scaled time as before.
- **Tests:** 70 EditMode tests, 10 new (CinematicMixerTests). All pass under Mono with Unity API stubs (stub math made real); not yet run in Unity.
### Files created / changed
- Assets/_Game/Scripts/Cinematic/{CinematicProfile, CinematicMixer, CinematicDirector, LetterboxBars}.cs — new
- Assets/_Game/Scripts/Core/GameSettings.cs — ReduceEffects + ReducedShakeScale
- Assets/_Game/Scripts/Iso/CameraShake.cs — scales by ReducedShakeScale when ReduceEffects is on
- Assets/_Game/Scripts/Audio/AudioService.cs — SetLowPass / ClearLowPass
- Assets/_Game/Scripts/UI/DeathCard.cs — showDelaySeconds
- Assets/_Game/Scripts/SixtyOne.Runtime.asmdef, Editor/SixtyOne.Editor.asmdef — URP Core/Universal references
- Assets/_Game/Scripts/Editor/Cinematic/CinematicAssetsGenerator.cs, Editor/SceneBuild/TestSceneCinematic.cs — new; TestSceneBuilder.cs, TestSceneUi.cs — cinematic wiring + letterbox
- Assets/_Game/Tests/EditMode/CinematicMixerTests.cs — new
- CLAUDE.md (Cinematic folder + "Cinematic look" rules), HANDOFF.md
### Needs local wiring (checklist for the MCP session)
- [ ] Open Unity, let it compile. Check that the URP asmdef references resolve (`Unity.RenderPipelines.Universal.Runtime`, `Unity.RenderPipelines.Core.Runtime`). Fix any compile errors first.
- [ ] Run menu: SixtyOne > Cinematic > Generate Profiles (creates the `Data/Cinematic/` assets)
- [ ] Run menu: SixtyOne > Generate Sample Act 1
- [ ] Run menu: SixtyOne > Build Test Scene (rebuilds Test_Act1 with the Volume, Light2Ds, dust, letterbox and director)
- [ ] Check on `Main Camera`: Rendering > Post Processing is ticked. Check on each Light2D: Target Sorting Layers includes Default; if not, set "All".
- [ ] Run menu: SixtyOne > Validate Content, then SixtyOne > Text > Validate. Expect 0 problems.
- [ ] Test Runner > EditMode > Run All (70 tests)
- [ ] Play loop 1 + loop 2 (see the 18:17 entry) and take MCP screenshots at: **prep start (T=61)**, **T=10 (letterbox + heartbeat)**, **quake (flash/aberration/flicker/dust)**, **post-quake (dark; flashlight light if carried)**, **death card (B&W blend first)**, **rewind (aberration + grain spike)**.
- [ ] Tune `CinematicAct1.asset` until UI and interactable labels read clearly and blacks aren't crushed. Likely knobs: `postExposure`, `globalLightIntensity`, `postQuakeGlobalLight`, `prepVignetteEnd`, `vignetteSmoothness`. Then press SixtyOne > Cinematic > Reset Profiles To Act Defaults only if you want to start over.
- [ ] Toggle `GameSettings.asset` > Reduce Effects and replay the quake: no flash, no aberration, no tilt, lighter shake.
- [ ] Console: confirm no new errors or warnings (especially Light2D/Volume/ParticleSystem material).
- [ ] Optional: add an exit/win node id to CinematicDirector > Exit Node Ids once one exists (none in the sample yet).
### Not verified / risks
- **Nothing visual has been verified**: no Play Mode run and no screenshots. All default strengths are educated guesses.
- Compiled only against hand-written stubs. The riskiest real-API spots:
  - `Light2D.lightType`/`pointLightOuterRadius` setters
  - `camera.GetUniversalAdditionalCameraData()`
  - `VolumeProfile.Add<T>(true)` + `AssetDatabase.AddObjectToAsset` for the base profile
  - `ParticleSystem` module setup in edit mode
  - Shader name `Universal Render Pipeline/2D/Sprite-Unlit-Default`
- Light2D created via `AddComponent` may not target all sorting layers by default (the project only has "Default", so it should be fine).
- Sprites use the default Lit material, so the darker global light darkens the world. TMP world labels and the overlay UI are unlit and stay readable. Verify interactable labels against the post-quake darkness.
- The dust particle uses a generated material; if it renders magenta, assign a Sprite-Unlit material by hand.
- No "exit building" node exists yet, so the win look is untested in the sample flow.
### Next suggested step
- LOCAL (MCP): run the checklist above, take the six screenshots, tune the three CinematicAct profiles, then commit the tuned assets.

---

## [2026-10-16 23:05] LOCAL — Prep phase wired + loop 1/loop 2 play-test, 2 fixes
### Done
- Compile: 0 errors, 0 warnings (Runtime 149 types, Editor 39, Tests 22). The `PickupItem.cs` move had lost its GUID, so I restored the original GUID (`4c106fd3…`) on `Scripts/Prep/PickupItem.cs.meta`.
- Generate Sample Act 1 + Build Test Scene ran clean.
- Validate Content first reported 13 problems. All of them came from **stale assets left over from the old flow** that the generator doesn't delete: `act1.n01_alert`, `act1.n04_stairs`, `tip.no_stairs_during`, `tip.no_valuables`. After deleting them: **0 problems**. Text > Validate: 145 referenced, 0 missing/empty/unused.
- EditMode tests: **60/60 passed** (before and after the fixes).
- **Loop 1 works as specified:** the countdown starts at 61, gold and laptop go into the bag (pickups hide), the window fires at 10 with the ÇÖK-KAPAN-TUTUN alert, and placement works (wife+child on the table, player on the inner wall). Quake: everyone stays healthy → exploration (gas/breaker locked without the wrench) → door → the locked crowbar choice kills with "Keşke yanıma levyeyi alsaydım...", "Eksik: Levye" and the emergency-bag tip → GERİ SAR → 61s, empty bag, LOOP #1, crowbar in the KnowledgeSet, family positions reset.
- **Loop 2 works as specified:** the crowbar pulses, then bedroom wrench (3s) → crowbar (3s) → kitchen gas valve (5s, GasOff) → salon. Micro rewind check: the player on the wardrobe dies ("Dolap sana sarılmaya…") → rewind to placement with the countdown at 10, LOOP #2, bag/GasOff and last positions kept, wardrobe label revealed. Then the inner wall → exploration (the gas/light traps are gone) → door opens with the crowbar → stairs "Dikkatlice aşağı in" → `act2.n01_entry` (Act 2, floor4). No kitchen fire.
- Extra checks: if the window hits while you're in the bedroom, the game switches back to the salon correctly. Leaving a member unplaced triggers the chandelier death. The GameSettings Knowledge Highlight toggle off stops the pulse, and on restarts it.
- **Bug fixed — family blocks prep clicks:** the family follows into each prep room and the player spawns on top of the bedroom crowbar, so `IsoPicker` picked `Player` instead of the crowbar. `FamilyMember`/`Anchor.IsClickable` now also require a listener (`Clicked != null`), so they're clickable only while PlacementController is subscribed. Verified: the crowbar picks during prep, and member/anchor picks still work during placement.
- `ProjectSettings/VersionControlSettings.asset` had been flipped to "Unity Version Control" by the Editor. I set it back to **Visible Meta Files** via `VersionControlSettings.mode`.
### Files created / changed
- Assets/_Game/Scripts/Placement/FamilyMember.cs, Anchor.cs — IsClickable only while listened to (prep click fix)
- Assets/_Game/Scripts/Prep/PickupItem.cs.meta — original GUID restored; Placement/PickupItem.cs.meta removed
- Assets/_Game/Data/** — regenerated; 4 stale assets deleted, new prep/door/stairs nodes, GameSettings, cues, items
- Assets/_Game/Scenes/Test_Act1.unity — rebuilt by Build Test Scene
- *.meta for the new cloud scripts/tests (Editor-generated)
### Needs local wiring (checklist for the MCP session)
- [ ] Hand-feel play-test with a real mouse: prep is fully driven by code via `IsoPicker.Pick` + UI button onClick. Layout, readability and timing still need human eyes.
- [ ] Assign clips to the cues (all 12 are silent; only "no clips" warnings in the console)
- [ ] Optional: remove `com.unity.collab-proxy` from Packages/manifest.json if Unity keeps flipping the VCS mode (not done; manifest changes need sign-off)
### Not verified / risks
- The AFAD popup is shown while the countdown is already running (the "Kapat" button costs real seconds). This may be intended; decide by design.
- The ChoicePanel keeps old buttons for one frame after a choice (deferred Destroy). Harmless in code; watch for double-clicks in real play.
- The generator doesn't remove assets it no longer makes. After renaming or removing nodes/tips, delete the stale `.asset` files or Validate will complain.
- Item icons are placeholders (`ph_dot`) on the death card and in the bag.
- Budget: loop 2's optimal route used about 22s of the 51s prep, so there's lots of slack; consider more costs or a shorter prep after playtests.
### Next suggested step
- CLOUD: Act 2 content (PrepPhaseNode with short/zero prep, 7.0, floor 4) + neighbor NPC nodes; optionally make the generator delete stale node/tip assets under Data/.

---

## [2026-10-16 18:17] CLOUD — Design change: 61-second prep phase + two-layer loop (micro/macro rewind, KnowledgeSet)
### Done
- Same branch `cloud/foundation` (PR #1), one commit per milestone: Core → Nodes → Prep → UI → Content/Editor → Tests → Docs. Supersedes the Act 1 flow from the 17:40 entry; everything else from that entry still applies.
- **New Act 1 flow:** AFAD popup "Deprem 61 saniye sonra" → 51s prep in 3 rooms (salon/mutfak/yatak odası, room tabs on the left, each walk 1s) → at 10s "ÇÖK-KAPAN-TUTUN" → existing placement → quake → exploration → **jammed door** (ShowLockedFatal without the crowbar = macro rewind) → stairs → Act 2 entry (the gas bomb also counts GasOff set during prep).
- **Core:** `RewindTarget` (LastNode/PrepStart), `KnowledgeSet` (persists across all rewinds), PrepStart snapshot (`GameSession.TakePrepStartSnapshot`; a macro restore also resets member anchors), `DeathInfo.MissingItemId`, `InputGate` (blocks world clicks and choices during timed actions), `GameSettings` SO (knowledge highlight toggle), new prep events in GameEvents.
- **Nodes:** `FailBehaviour.ShowLockedFatal` (`Choice.FatalWithout(item, reason, missingText)`, shown locked but clickable), `DeathData.rewindTarget`, `SceneNode.IsPrepStart`. NodePlayer takes the PrepStart snapshot, handles fatal-locked clicks, presents choices on handler nodes too, and starts the timer before handlers (avoids a re-entrancy bug).
- **Prep (new `Scripts/Prep/`):** pure `PrepCountdown` (window / last-10s ticks / zero events) and `PrepAction`; `PrepPhaseNode` (duration, window, rooms, linked PlacementNode, alert keys, tick/alarm cues; duration ≤ window = straight to placement); `PrepController` (INodeHandler that runs the countdown, timed actions and room travel, hands off at the window, keeps ticking through placement, and resumes at the window on a micro rewind into placement); `PrepInteractable` (cost, required item, grants item, outcomes, one-shot via `Used_<id>` flag, knowledge pulse highlight); `PickupItem` is now its 1s preset. Trap items no longer kill; they waste a slot and time.
- **UI:** CountdownDisplay (big number, red in the window, punch on ticks), ActionProgressBar, RoomTabs, keyed alerts in AfadAlertPopup, a DeathCard "Eksik: <item>" block with icon, bag-full slot flash, fatal-locked buttons, longer RewindFX on macro rewinds.
- **Editor:** the generator builds a PrepPhaseNode plus the door and stairs nodes, 3 new cues and a GameSettings asset. Build Test Scene adds the kitchen and bedroom rooms, 12 interactables (whistle, gold, laptop, console and breaker 4s in the salon; gas valve 5s needing the wrench, water, first aid cabinet 3s and gloves in the kitchen; wrench drawer 3s, flashlight 3s and crowbar 3s in the bedroom), PrepController and the prep UI. Validate Content now checks ShowLockedFatal (item + text + tip + HasItem), PrepPhaseNode → PlacementNode (timer = window), and prep interactable wiring.
- **CSV:** 145 rows. 34 new keys (prep labels, rooms, alerts, door node, new items, `death.missing.crowbar`, `tip.emergency_bag`); the old Act 1 alert-node, freeze, gold and laptop keys were removed, and the stairs keys were renamed `act1.n04.*` → `act1.n05.*`. Every key in use is present and none are unused.
- **Tests:** 60 total, 16 new (PrepCountdown thresholds, action time costs, macro vs micro targets, PrepStart restore, KnowledgeSet persistence, fatal-locked choice view/death).
- **CLAUDE.md:** "The Game" and Core concepts describe the prep phase and the two-layer loop; Unity version fixed to 6000.6; asmdef assembly names documented (`UnityEngine.UI`).
### Files created / changed
- Assets/_Game/Scripts/Core — RewindTarget, KnowledgeSet, InputGate, GameSettings (new); GameState, Snapshot, GameSession, GameManager, DeathInfo, GameEvents (changed)
- Assets/_Game/Scripts/Nodes — Choice, ChoiceView, DeathData, SceneNode, NodePlayer, Item (changed)
- Assets/_Game/Scripts/Prep/* — new folder (6 files); Placement/PickupItem.cs moved here as a PrepInteractable preset
- Assets/_Game/Scripts/Placement/RoomSwitcher.cs (FindRoom), Iso/IsoPicker.cs (InputGate), Text/UiKeys.cs (3 keys)
- Assets/_Game/Scripts/UI — CountdownDisplay, ActionProgressBar, RoomTabs (new); ChoiceButton, ChoicePanel, AfadAlertPopup, DeathCard, BagUI, RewindFX (changed)
- Assets/_Game/Scripts/Editor — SampleAct1Generator, TestSceneBuilder, TestSceneUi, ContentValidator
- Assets/_Game/Tests/EditMode — PrepCountdownTests, PrepActionTests, RewindTargetTests (new)
- Assets/_Game/Content/Text/strings_tr.csv, CLAUDE.md, HANDOFF.md
### Needs local wiring (checklist for the MCP session)
- [ ] Open Unity, let it import and compile. Fix any compile errors first (see risks).
- [ ] Window > TextMeshPro > Import TMP Essential Resources (skip if already done)
- [ ] Run menu: SixtyOne > Generate Sample Act 1 → `Assets/_Game/Data/` gets GameSettings.asset, a PrepPhaseNode `act1.n01_prep`, the door node `act1.n04_door`, the stairs node `act1.n05_stairs`, and 12 cues
- [ ] Run menu: SixtyOne > Build Test Scene → rebuilds `Assets/_Game/Scenes/Test_Act1.unity` with 5 rooms, 12 prep interactables and the prep UI (overwrites any earlier build)
- [ ] With Test_Act1 open, run menu: SixtyOne > Validate Content. It should report 0 problems.
- [ ] Run menu: SixtyOne > Text > Validate. It should report 0 missing.
- [ ] Window > General > Test Runner > EditMode > Run All (60 tests)
- [ ] Play loop 1: the AFAD popup shows 61 and the countdown runs. Grab gold/laptop (wasted slots) and skip the crowbar. At 10s, place wife + child on the table and the player on the inner wall → quake → exploration → leave → at the door click the locked crowbar choice → "Keşke yanıma levyeyi alsaydım..." + crowbar icon → rewind to 61s with an empty bag and LOOP 1.
- [ ] Play loop 2: the crowbar interactable in the bedroom pulses. Take the wrench (bedroom 3s) → gas valve (kitchen 5s) → crowbar → place → quake → door opens → stairs → Act 2 placeholder (no kitchen fire). Also check a micro rewind (wrong anchor): it should rewind to the placement with the countdown at 10.
- [ ] Toggle `Assets/_Game/Data/GameSettings.asset` > Knowledge Highlight off and confirm the pulse is gone
- [ ] Optional: tune costs/positions in `TestSceneBuilder.PrepSpecs()`; assign clips to `cue_countdown_tick`, `cue_placement_alarm` and the other cues
### Not verified / risks
- Still not compiled in Unity. All three assemblies compile with `mcs` against hand-written Unity API stubs, and all 60 EditMode tests pass under Mono with a small NUnit-compatible runner. In that run Unity objects are stubs; only the game logic is the real code.
- Risky Unity API spots (unchanged): TMP members, `InputSystemUIInputModule.AssignDefaultActions`, `AdvancedDropdown`, `Physics2D.OverlapPoint(point, filter, list)`. New risk: `PrepInteractable.Configure` is called on components in edit mode, which relies on the builder saving the scene right after.
- Countdown display vs placement timer: both start the same frame at the window, so they should agree to within one frame.
- During prep the family follows the player between rooms (members snap to each room's spawns). That's harmless for now, but the artist/designer may want them to stay in the salon.
- Prep interactables are only clickable during prep, not in post-quake exploration (by design: what you didn't pack is gone).
- Not play-tested: layout, timings (51s budget vs ~25s of useful actions) and UI positions are guesses.
### Next suggested step
- LOCAL: run the checklist and tune the prep budget and costs by play-testing. Then CLOUD: Act 2 content using PrepPhaseNode with a short or zero prep (7.0, floor 4), plus neighbor NPC nodes.

---

## [2026-10-16 22:10] LOCAL — Wired cloud foundation: compile check, TMP, Act 1 data, test scene, validators, tests
### Done
- Unity 6000.6.6f1 compiled all three assemblies (Runtime 131 types, Editor 37, Tests 16) with **0 errors and 0 warnings** from `_Game`. None of the API risks in the cloud entry came up (TMP, AssignDefaultActions, AdvancedDropdown, OverlapPoint).
- Imported TMP Essential Resources (`Assets/TextMesh Pro/`).
- Ran SixtyOne > Generate Sample Act 1 → `Assets/_Game/Data/**` created.
- Ran SixtyOne > Build Test Scene → `Assets/_Game/Scenes/Test_Act1.unity` + `Assets/_Game/Art/Placeholder/*.png`.
- **Bug fixed in Build Test Scene:** every ScriptableObject reference in the built scene was null (anchor hazards, NodePlayer content, TextDBLoader settings, pickup items). Cause: `EditorSceneManager.NewScene(..., Single)` unloads unused assets, and the generated SOs were only held by a local variable. Now the scene is created first and the data is generated after.
- Validate Content: **0 problems** (it found 8 before the fix). Text > Validate: 116 referenced, 0 missing, 0 empty, 0 unused.
- EditMode tests: **44/44 passed**.
- Turkish glyphs: static `LiberationSans SDF` lacks ğĞşŞıİ, but its existing dynamic fallback `LiberationSans SDF - Fallback Dynamic` (TTF source) adds all of them, so no new font was needed.
- Play Mode smoke test: Test_Act1 boots into the AFAD node and the choices, LOOP #0, bag and journal render Turkish text. The only console output was the expected "cue has no clips" warning.
### Files created / changed
- Assets/_Game/Scripts/Editor/SceneBuild/TestSceneBuilder.cs — NewScene moved before data generation (fix for null refs)
- Assets/TextMesh Pro/** — TMP Essential Resources
- Assets/_Game/Data/**, Assets/_Game/Scenes/Test_Act1.unity, Assets/_Game/Art/Placeholder/** — generated by menu items
- *.meta for all cloud files (generated by the Editor)
- ProjectSettings/VersionControlSettings.asset, SceneTemplateSettings.json — touched by the Editor, not by hand
- Nothing committed yet (branch `cloud/foundation`).
### Needs local wiring (checklist for the MCP session)
- [ ] Commit the generated assets + .meta files on `cloud/foundation` and merge PR #1
- [ ] Manual play-through of Test_Act1 (placement → quake → wrench/gas → stairs; trap deaths, LOOP counter, Deprem Notu, gas-fire rewind). This needs real clicks, so it wasn't driven here.
- [ ] Optional: add Test_Act1 to Build Settings; the sound designer assigns clips on `Assets/_Game/Data/Audio/cue_*.asset`
### Not verified / risks
- Only boot was smoke-tested, not the full Act 1 flow. Layout, timers and shake are still untuned.
- If "Clear Dynamic Data on Build" is turned off on the fallback font, its atlas can grow in git. Low risk.
### Next suggested step
- LOCAL: manual Act 1 play-through and tune layout and timers. Then CLOUD: Act 2 (floor 4, 7.0) generator plus odd-neighbor NPC nodes.

---

## [2026-10-16 17:40] CLOUD — Runtime foundation (cloud-task-01): Core, Text, Nodes, Placement, Iso, Audio, UI, Editor tools, Act 1 sample, tests
### Done
- Branch `cloud/foundation`, one commit per milestone (1–9). 97 C# files under `Assets/_Game/`.
- **Assemblies:** `SixtyOne.Runtime` (refs Unity.InputSystem, Unity.TextMeshPro, UnityEngine.UI; the uGUI assembly is really named `UnityEngine.UI`, not "Unity.ugui"), `SixtyOne.Editor` (Editor only), `SixtyOne.Tests.EditMode` (NUnit, UNITY_INCLUDE_TESTS).
- **Core:** GameState, Flags, Inventory (4), FamilyMemberState, LoopCounter, Snapshot (restore keeps LoopCount, tips, revealed anchors and member anchor positions), GameSession (pure death → rewind logic plus snapshots per node), GameManager, GameEvents, StateOps, CountdownTimer, coroutine Tween + Easing.
- **Text:** RFC 4180 CsvParser, TextDB (`[[key]]`, magenta in UI), TextDBSettings/Loader, TextKey + searchable dropdown drawer with Turkish preview, LocalizedText, CSV hot reload (AssetPostprocessor), menus Text > Validate / Export Missing Keys / Reload CSV. `strings_tr.csv` has 116 `TODO:` rows covering every key in use.
- **Nodes:** Condition / Outcome / Choice (plus `showIf` visibility conditions, a locked reason and timeout-only choices), DeathData, BombCheck (delayed bombs can rewind to the node that armed them), SceneNode, Item (can be a trap item), Tip, GameContent, IChoicePresenter, NodePlayer.
- **Placement:** Anchor, Hazard, HazardProp (topple/drop/shatter tweens), FamilyMember, Room, RoomSwitcher, PlacementNode, PlacementController, pure PlacementRules + PlacementResolver, PickupItem.
- **Iso:** IsoSorter, IsoPicker (Input System pointer, `Physics2D.OverlapPoint`, highest order wins, UI blocks), IsoMath, CameraShake (0.12 / 0.22 / 0.38 per act).
- **Audio:** AudioCue, AudioService (pooled, music layer, warns once), AudioEventMap + AudioEventHooks.
- **UI:** ChoicePanel/ChoiceButton, TimerBar, BagUI, LoopCounterUI, DeathCard (the wife's death is a hard cut to black), TipJournal, AfadAlertPopup, MessageToast, RewindFX (scanline texture generated in code + reverse shake).
- **Editor:** SixtyOne > Validate Content, Generate Sample Act 1, Build Test Scene (placeholder PNG sprites, full canvas, wiring through `SerializedObject`).
- **Tests:** 44 EditMode tests covering the CSV parser and TextDB, conditions and choice views, outcomes, snapshots/session, placement rules and resolver, and the delayed gas bomb.
### Files created / changed
- Assets/_Game/Scripts/** — all runtime and editor code above (namespaces `SixtyOne.<Folder>`; editor code is in `SixtyOne.Editor`)
- Assets/_Game/Tests/EditMode/** — tests + asmdef
- Assets/_Game/Content/Text/strings_tr.csv — placeholder lore/UI text (`TODO:` prefix)
- HANDOFF.md — this entry
- No .unity/.prefab/.asset/.meta files and no ProjectSettings/manifest changes.
### Needs local wiring (checklist for the MCP session)
- [ ] Open Unity and let it import and compile. Fix any compile errors first (see risks).
- [ ] Window > TextMeshPro > Import TMP Essential Resources (Build Test Scene refuses to run without them)
- [ ] Run menu: SixtyOne > Generate Sample Act 1 → creates `Assets/_Game/Data/{TextDBSettings, GameContent}.asset`, plus Nodes/, Hazards/, Items/, Tips/ and Audio/ (10 cues + AudioEventMap)
- [ ] Run menu: SixtyOne > Build Test Scene → creates `Assets/_Game/Scenes/Test_Act1.unity` and `Assets/_Game/Art/Placeholder/*.png` (it also re-runs the generator)
- [ ] With Test_Act1 open, run menu: SixtyOne > Validate Content. It should report 0 problems.
- [ ] Run menu: SixtyOne > Text > Validate. Expect 0 missing; a few "unused" keys are fine (wife injury lines that only show when a hazard is fatal)
- [ ] Window > General > Test Runner > EditMode > Run All (44 tests)
- [ ] Play Test_Act1 end to end: AFAD popup → choose "Ailemi güvenli yerlere yerleştir" → place wife then child on the table, then the player on the inner wall → quake → explore: pick up the wrench, close the gas, leave → stairs → down → Act 2 placeholder. Also try a trap (wardrobe/window), check the LOOP counter, Deprem Notu and journal, and leave the gas open to trigger the kitchen-fire rewind to the exploration node.
- [ ] Optional: add Test_Act1 to Build Settings; assign AudioClips on `Assets/_Game/Data/Audio/cue_*.asset` (sound designer)
### Not verified / risks
- Pushed to `origin/cloud/foundation`; PR: https://github.com/qerK0/61Seconds/pull/1
- Not compiled in Unity (there's no editor in cloud). I installed `mcs` and compiled all three assemblies against hand-written Unity API stubs, and they compile cleanly. That catches namespace, type and signature errors in our code, but not wrong assumptions about the Unity API itself. The most likely spots are TMP members (`TextAlignmentOptions`, `TextMeshPro.sortingOrder`), `InputSystemUIInputModule.AssignDefaultActions`, `AdvancedDropdown`, and `Physics2D.OverlapPoint(point, ContactFilter2D, List)`.
- The project is on Unity **6000.6.6f1** (ProjectVersion.txt), not 6000.3 as CLAUDE.md says. I avoided APIs known to be deprecated, but newer obsolete warnings are possible.
- TMP's default LiberationSans may lack Turkish glyphs (ğ, ş, ı, İ) unless the dynamic fallback font is present. If you see boxes, add a dynamic font asset with Turkish coverage as a fallback.
- Not play-tested: layout numbers (camera size 3, UI anchors, iso positions) are best guesses.
- The CSV only hot-reloads during Play Mode when Asset Pipeline auto-refresh is enabled in Play Mode (Preferences).
- Design choice: placement anchors persist across loops (as specified), so after a rewind the player only fixes the wrong placement. "Player placed last" means the player can be selected only after the wife and child have an anchor.
### Next suggested step
- LOCAL: run the checklist, fix compile errors, play-test Act 1 and tune layout, timers and shake. Then CLOUD: Act 2 (floor 4, 7.0) content generator on the same pattern, plus odd-neighbor NPC nodes.

---

## [2026-10-16 19:50] SETUP — Design revision: 2.5D isometric, dog removed, text/audio pipelines
### Done
- CLAUDE.md revised: isometric rules, family = player + wife + child (no dog), CSV lore pipeline, AudioCue pipeline, art budget limits removed
### Files created / changed
- CLAUDE.md — design revision
### Needs local wiring (checklist for the MCP session)
- [ ] None yet
### Not verified / risks
- None
### Next suggested step
- CLOUD: full runtime foundation (see cloud prompt): Core, Text, Nodes, Placement, Iso, Audio, UI, Editor tools, tests, sample Act 1 + test scene builder.

---

## [2026-10-16 19:45] SETUP — Project guide and MCP config added
### Done
- Added CLAUDE.md (game summary, architecture, cloud/local work split, rules)
- Added HANDOFF.md (this log)
- Added .mcp.json pointing Claude Code at the MCP for Unity HTTP server
- Added com.coplaydev.unity-mcp (git, #main) to Packages/manifest.json (backup: manifest.json.bak)
### Files created / changed
- CLAUDE.md, HANDOFF.md, .mcp.json — project root
- Packages/manifest.json — MCP for Unity package added
### Needs local wiring (checklist for the MCP session)
- [ ] Open/focus Unity so it resolves the new MCP for Unity package (already in manifest.json)
- [ ] Open Window > MCP for Unity, start the server, confirm it is connected
- [ ] .mcp.json could not be written remotely. In the project root run: `claude mcp add --transport http --scope project unityMCP http://localhost:8080/mcp`
- [ ] Then run `claude`, then `/mcp` and check that unityMCP is listed and connected
### Not verified / risks
- MCP server port assumed 8080 (package default). If the MCP window shows another port, update .mcp.json.
### Next suggested step
- Superseded by the 19:50 entry.
