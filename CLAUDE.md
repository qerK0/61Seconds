# 61 Seconds — Claude Code Project Guide

48-hour game jam project (Civil Game Jam, 16–18 Oct 2026). Unity 6000.6 (see `ProjectSettings/ProjectVersion.txt`), URP, **2.5D isometric**. Team: programmer (Bora), artist, sound designer, lore/narrative writer.
Speed beats polish. Small, working, testable steps. Never leave the project in a non-compiling state.

## The Game
Linear earthquake-escape game. The player, their **wife** and their **child** live in Flat 11 on the 4th floor and must get out of the building alive: **Floor 4 → Floor 3 → Floor 2 → exit**. There is ONE earthquake for the whole game, plus aftershocks.

**The building timer (where the tension comes from).** A `BuildingTimer` (default 300s, on the `LevelConfig` asset) starts with the game. At 0 the building collapses: death and rewind. It pulses the UI at 60s and sounds an alarm with ticks in the last 30s. Every action subtracts its time cost (moving between rooms, opening drawers, closing valves, talking, crafting); injured family members make moves slower until treated with the first aid kit.

**Flow.**
- Floor 4: an AFAD early warning opens a 60s **prep** in our own flat (salon / kitchen / hall; loot costs prep seconds, the valve needs the wrench, valuables are traps; the building timer is not running yet). At prep zero the quake hits and the building timer starts: opening quake placement → Flat 11 after the quake (gas valve, breaker, bedroom with bedsheets) → Sevim in the corridor (dialogue; each line costs time, "Kısa kes" skips her info and whistle) → the corridor (a kind neighbour gives food + water) and Flat 12 (borrow cloth, rope, tape, flashlight, lighter, first aid; "Not bırak" leaves a note).
- Floor 3: aftershock placement on arrival; a needs-aware trader in the corridor; Flat 9 fuse box wiring as a WASD QTE (needs insulated gloves or the crafted grip + tape, else electrocution) which unlocks the cable descent (needs the child harness), plus a balcony where the bedsheet rope climbs down to Flat 7; Flat 10 (dark, gas leak, lighter = explosion, flashlight is safe, collapsed floor = alternative way down).
- Floor 2: strong aftershock placement in Flat 7 (gas leak); a frightened dog blocks the corridor (feed it, use the whistle, or slip past with a WASD QTE whose failures cost building time and re-roll); stairs → street → epilogue (family state, borrow line, loop count).

**Survival systems.** Stamina (food/water restore it; it drains over time and per heavy action; at 0 heavy actions are locked and everything costs more time), poison (gas rooms with an open valve; darkening + vignette; max = death with a gas tip; the wet cloth halves it), panic (rises as the timer falls, when poisoned and on aftershocks; above `panicDelayThreshold` clicks get an input delay up to `maxInputDelay`, with instant visual feedback; Reduce Effects forces 0), crafting (two bag items → one, order-independent; 3 bedsheets → 1 bedsheet rope), an 8-slot bag (`LevelConfig.MaxInventorySlots`; the pixel-art backpack's 2x4 compartment) with Drop (the item stays in that room and can be picked up again), key items (flashlight, whistle: no bag slot, not droppable, kept in the backpack's side pockets: left = flashlight, right = whistle; a pocket click plays the node's choice that needs the item, toggles the flashlight (`FlashlightOff` flag) or shows the name), WASD quick-time events (random W/A/S/D sequence per attempt, never 3 identical keys in a row; a miss costs building time and re-rolls), hidden items (invisible hotspots in the room art; optional sparkle in later loops or after N seconds).

Choices work like Henry Stickmin. A wrong choice ("what NOT to do in an earthquake") causes a death, a visible LOOP counter goes up, and each death shows a one-line real earthquake safety tip ("Deprem Notu"). Tips are collected across loops. **The loop has two layers:**
- **Micro rewind:** a wrong placement or choice rewinds to the last decision node.
- **Macro rewind:** a death caused by missing gear or the building collapsing rewinds to the current **floor checkpoint** (the start of the floor, timer included). Locked "missing gear" options stay clickable and kill with the missing item's text.
- These persist across ALL rewinds: `LoopCount`, collected tips, revealed anchor labels, and the **KnowledgeSet** (items the player died for lacking, hidden spots found). The borrow ledger is reset with the checkpoint.

**Tone rule:** absurd or comic deaths are only for the player character and "do-not-do" traps. The wife is never shown dying on screen: cut to black, play a sound and show one line of text. The child never dies: the child gets a `Trapped` state that triggers the rewind.

**Safety content must match AFAD guidance.** Drop-Cover-Hold On is correct. All of the following are TRAPS: "triangle of life", doorways, balconies, windows, elevators, running down the stairs during shaking, lighting a flame, and flipping switches when there is a gas smell. NO firearms anywhere (the validator checks item ids). Taking from neighbours is always "ödünç al" — never "yağma", "loot" or "çalmak".

## Architecture (data-driven: new content = new data, not new code)

Root folder: `Assets/_Game/`
```
Scripts/Core/        GameState, Flags, Inventory, Snapshot, BorrowLedger, KnowledgeSet, GameEvents, routers (ActionCost, InputDelay, ExplorationGate)
Scripts/Text/        CSV parser, TextDB, TextKey (lore/dialogue/UI strings)
Scripts/Nodes/       SceneNode, Choice, Condition, Outcome, NodePlayer, ChoiceView
Scripts/Placement/   Anchor, Hazard, FamilyMember, PlacementNode, PlacementResolver, Room/RoomSwitcher, RoomTransitionController + TransitionConfig + TileTransitionPlan (pure) + RoomTransitionPieces, RoomVisualConfig, Door/ChoiceHotspot (RoomLink) + RoomLinkRules + RoomNavigator, FloorArea, CharacterMotor/PlayerMover/FamilyFollower, CharacterAnimator, MovementConfig
Scripts/Building/    LevelConfig, BuildingTimer + SurvivalModel (pure), SurvivalController, GasZone, Epilogue*
Scripts/Prep/        PrepTimer + PrepRules (pure), PrepPhaseNode, PrepPhase (60s pre-quake loot countdown)
Scripts/Dialogue/    DialogueLine, DialogueNode, DialogueController
Scripts/Items/       Recipe, RecipeBook, Crafting, ItemUse, TraderProfile, TraderLogic
Scripts/QTE/         QTEKey, QTESequence + QTERun + QTESession (pure), QTEConfig, QTENode, QTEController (Input System W/A/S/D)
Scripts/World/       WorldInteractable, PickupItem, HiddenItemHotspot
Scripts/Iso/         IsoSorter, IsoPicker (pointer → world object), iso helpers
Scripts/Cinematic/   CinematicProfile, CinematicMixer (pure), CinematicDirector, LetterboxBars
Scripts/Audio/       AudioCue (SO), AudioService
Scripts/UI/          ChoicePanel, UITheme, TypewriterText, CollapsiblePanel, BagUI, BuildingTimerDisplay, StaminaBar, PoisonOverlay, ClickFeedback, QTEView, KeyItemPocket (KeyItemCornerHUD only for future key items), SubtitleView, PauseMenu, LedClock, LoopCounterUI, DeathCard, TipJournal, RewindFX
Scripts/Editor/      Validators, BuildingLevelsGenerator (one partial per floor), test-scene builder (BuildingLayout tables)
Content/Text/        strings_tr.csv  ← the lore writer edits this
Data/                Nodes/Floor4..2 Hazards/ Items/ Tips/ Traders/ Recipes/ Audio/ LevelConfig (ScriptableObject assets)
Prefabs/  Scenes/  Art/  Audio/
Tests/EditMode/      Pure-logic tests
```
Namespace: `SixtyOne.<Folder>` (e.g. `SixtyOne.Nodes`). Assembly definitions: `SixtyOne.Runtime`, `SixtyOne.Editor`, `SixtyOne.Tests.EditMode`.

### Core concepts
- **GameState** is plain C#, owned by a `GameManager` MonoBehaviour. It holds:
  - `Inventory` (8 item ids by default), `Flags` (string → bool, e.g. `GasOff`, `PowerCut_F3`, `LeftNote`, `DogGone`)
  - the family members' states and anchor positions, the current node id and floor stage
  - `BuildingTime`, `Stamina`, `Poison` and the `BorrowLedger` (all restored by snapshots)
  - `LoopCount`, the collected tip ids, revealed anchor labels and the `KnowledgeSet` (these persist across every rewind)
- **Snapshot:** a deep copy of GameState taken at the start of every node. Nodes marked `isCheckpoint` (the start of each floor) also become the **floor checkpoint**. On death, `DeathInfo.RewindTarget` picks the snapshot:
  - `LastNode` (micro): restore the latest snapshot. Family members stay at their LAST chosen positions.
  - `Checkpoint` (macro): restore the floor checkpoint, including the building timer, and reset family positions.
  - Either way: keep the persistent data above, increment `LoopCount`, play the rewind FX (longer for macro rewinds).
- **SceneNode** (ScriptableObject) has an id, title/body text keys, a room id, a timer (0 = untimed), `Choice`s, optional `onEnter` outcomes and "bomb checks" (conditions evaluated on enter that can kill). Subtypes are driven by `INodeHandler`s: `PlacementNode` (PlacementController), `DialogueNode` (DialogueController), `QTENode` (QTEController), `EpilogueNode` (EpilogueController). With no handler the room is explorable (`ExplorationGate`).
- **Choice** has a label key, a type (`Normal`/`Trap`), `Condition`s (has item, has any item, flag, member state), a fail behaviour (`Hide`, `ShowLocked` with a reason, `ShowLockedFatal` = clickable death naming the first missing item), `Outcome`s (`GoToNode`, `Death`, `SetFlag`, `AddItem`, `RemoveItem`, `SetMemberState`, `TraderGive`, `ShowMessage`), and costs: time seconds, stamina, `heavy` (locked at 0 stamina), `isMove` (slower with injured members). A **delayed bomb** sets a flag now; a later node's bomb check reads it.
- **Action costs** go through `ActionCostRouter` to the `SurvivalController` (building timer + stamina). Clicks go through `InputDelayRouter` (panic delay); `ClickFeedback` shows the instant ring.
- **DialogueNode** has lines (speaker key, text key, time cost per line) and choices that show during the lines (e.g. "hurry") or at the end.
- **WorldInteractable** (IClickable): time/stamina cost, optional required item, granted item, `borrowed` (recorded in the BorrowLedger), extra outcomes, one-shot as `Used_<id>` flags. `PickupItem` = open-surface preset; `HiddenItemHotspot` = invisible collider + painted overlay + sparkle. `RopeDescentInteractable` = needs the bedsheet rope, enters a lower-floor node. `WorldDroppedItem` = an item the player dropped (spawned by `DroppedItemsPresenter` from `GameState.Dropped`).
- **Recipe** (inputA, inputB, output, time, stamina) in a `RecipeBook`; BagUI: tap an item, tap it again to use it (food/water/first aid; a bedsheet ties the rope), tap another to combine, or Drop it into the current room.
- **Bag state:** `Inventory` (slots), `KeyItemState` (key items), `RoomDropLedger` (dropped items per room), all restored by snapshots. Always add items through `StateOps.TryAddItem(state, id, item.IsKeyItem)` and check ownership with `StateOps.HasItem` (bag or key items). UI and world listen to `InventoryEvents`, never to each other.
- **QTENode**: context (BreakerWiring / DogEvasion), `QTEConfig` (length 4–6, time per key, total timeout, building-timer penalty), success/fail outcomes, next node. HitStop freezes only the QTE (QTE timers run on unscaled time and stop while `PauseState.IsPaused`).
- **TraderProfile**: ordered needs + fallback food/water; the trader gives the first need the player lacks.
- **Rooms, zones and doors:** a flat is several rooms (hall + living + kitchen/bedroom) that share one **zone** (`Room.ZoneId` = the entry room = the node's room). Corridors are single rooms. Rooms change only through world `Door`s (there are no room buttons):
  - a door inside the zone switches rooms directly and costs `MovementConfig.DoorSeconds`;
  - a door into another zone plays the current node's move choice that leads there (`RoomLinkRules`), so costs, locks and fatal locks are unchanged;
  - `ChoiceHotspot`s (NPCs, elevators) play a choice by its label key;
  - `RoomNavigator` hides the choices that doors and hotspots offer from the panel.
  - `GameState.CurrentRoomId` is the room actually shown; `Condition.InRoom` puts a choice in a sub-room (Flat 9 fuse box in the hall, Flat 10/7 valves in the kitchen).
  - New door = one `Link(a, "L3", b, "R1")` line in `BuildingLayout` (edge codes: L/R back-wall gaps, F/G front floor exits).
- **Click-and-run:** `IsoPicker` routes clicks (after the panic delay) to `PlayerMover`. A floor click runs the player there (straight line, clamped to the room's `FloorArea`); a click on a door, pickup or NPC runs to its approach point, then uses it. Walking costs only real time. Wife and child (`FamilyFollower`) follow with a delay and offset, and snap to the arrival point on a room change. `CharacterAnimator` loops the run frames (`SixtyOne > Art > Import Character Frames`, sorted numerically) and makes the idle breathing in code (frame order: `FrameOrder`; state rules: `CharacterAnimator.StateFor` / `RunFrameIndex` / `FlipFor`, all unit-tested).
- **Menus:** `Scenes/MainMenu.unity` (built by `SixtyOne > Build Main Menu`, first in Build Settings) → `Test_Building`. Esc opens `PauseMenu` (dim overlay, Devam Et / Ayarlar / Ana Menü / Oyundan Çık with an "Emin misin?" confirm). `UserSettings` (PlayerPrefs: master / music / effects volume, Reduce Effects) is shared by both menus; `SceneFlow` changes scenes.
- **Subtitles:** `SubtitleView` is the cinematic bottom band (EB Garamond name + typewritten line, ▼ continue, click / Space / Enter) for every dialogue line and node body; the ChoicePanel shows only the title and choices above it.
- **UI theme:** `Data/UI/UITheme.asset` (Inter Regular/SemiBold/Bold SDF from `SixtyOne > Text > Generate Fonts + Theme`) styles everything the scene builder makes; nothing is below 16 px at 1080p. The choice panel is a bottom overlay that sizes to its content. It sinks to a peek strip when it has no buttons or while the player runs, and typewrites dialogue (visual only: DialogueController still pays the line costs).
- **LevelConfig**: building timer thresholds and texts, prep seconds, bag slots + bedsheet rope, stamina, poison (`GasZone` per room with its valve flag), panic and input delay curves.
- **PlacementNode** is a SceneNode subtype with a timed phase (opening quake and each aftershock; magnitude + shake per node):
  - the player taps a family member and the valid anchors highlight
  - the player taps an anchor, and the member is repositioned instantly (no walk animation)
  - when the timer ends, the quake plays and each Hazard resolves against the members on its anchor
- **Anchor** is a MonoBehaviour in the room: a tag (`Safe` or `Trap`), a capacity (a table holds 2), a linked Hazard, an adjacency list and a label key hidden until the player has died to it once.
- **Hazard** (ScriptableObject): outcome per member type (player → Death, wife → Death or Injured, child → Trapped), text keys, a tip, and a motion type (`Topple`, `Drop`, `Shatter` or `None`) played as a code tween on the hazard prop.
- **FamilyMember rules:** the wife goes wherever she is told; the child must be on the same anchor as an adult, or on an adjacent one; the player is placed last. Between rooms and floors the family follows the player (snaps to the room's spawns).
- **One scene** (`Test_Building.unity`) holds every room of every floor; `RoomSwitcher` shows one room at a time; a room change plays `RoomTransitionController` (input locked between `NavigationEvents.OnTransitionStart` and `OnTransitionComplete`): the old room's floor tiles (per-tile sprite proxies), walls and props rise and fade in a wave from the far corner, the room swaps at the midpoint, then the new floor, walls and props drop in with a small settle and the family pops in. Descend uses a longer, higher drop; Reduce Effects is a quick fade; rewinds are instant. Transitions never cost time: the prep countdown and building timer advance by `NavigationEvents.TimerDelta` (0 between start and complete); a door's configured cost is still paid once. `CameraShake` scales its magnitude by ortho size / `referenceOrthoSize` (2), so quakes feel the same in every room. All rooms and the family sit under one `World` root scaled by `RoomVisualConfig.WorldScale` (default 1.5): runtime offsets and speeds are tuned at scale 1 and read the scale from the hierarchy (`Room.Scale` / `Room.WorldOffset`). The camera sits under a `CameraRig` moved by `RoomCameraFraming`. One GameManager, GameState and UI means checkpoints and rewinds never load scenes.

### Isometric (2.5D) rules
- Art is pre-rendered isometric sprites (2:1 diamond). The camera is orthographic.
- Depth sorting: an `IsoSorter` component sets `sortingOrder` from world Y, so lower Y is drawn in front. Static props compute it once and moving objects update every LateUpdate. Use `SortingGroup` for multi-sprite objects. Do NOT rely on ProjectSettings sort-axis changes.
- Picking: `IsoPicker` reads the Input System pointer (mouse and touch) and uses `Physics2D.OverlapPoint` against colliders on clickables. When colliders overlap, the highest sortingOrder wins. UI clicks block world clicks.
- Each anchor has a ground point (its pivot at the feet) so characters snap to it correctly.

### Text / lore pipeline (the writer must never open Unity)
- All player-facing text lives in `Assets/_Game/Content/Text/strings_tr.csv` with the columns `key,text,notes`. The file is RFC 4180 CSV: UTF-8, Turkish characters, and quoted fields that may contain commas and newlines.
- Keys are namespaced with dots: `act1.n01.body`, `act1.n01.c1`, `death.wardrobe`, `tip.drop_cover_hold`, `npc.cay_teyze.01`, `ui.loop`.
- Code and assets reference text only by key, using a `TextKey` struct with an Inspector dropdown/search. They never hard-code Turkish strings.
- `TextDB` loads the CSV at startup and can hot-reload it in the Editor when the file changes. A missing key renders as `[[key]]` in magenta, so gaps are visible during play.
- Editor tools:
  - **SixtyOne > Text > Validate** reports keys that assets reference but the CSV lacks, and CSV keys that nothing uses.
  - **SixtyOne > Text > Export Missing Keys** appends empty rows for the writer to fill.
- The writer edits the CSV in Google Sheets or Excel and exports it as CSV. The programmer drops the file into place, and that's the whole process.

### Audio pipeline
- An `AudioCue` ScriptableObject holds an id, clips[], volume, a pitch range and a mixer group. Gameplay code raises cues by reference or id through `AudioService`.
- A missing clip logs a warning once and stays silent. It never throws.
- The sound designer only swaps clips on cue assets.

### Cinematic look
- Renderer: URP **2D Renderer** (`Assets/Settings/Renderer2D.asset`), so lighting uses `Light2D` (a global light plus warm practicals that flicker and die at the quake).
- `CinematicDirector` reacts only to GameEvents, the building timer (tension) and poison. It drives an instantiated copy of the global volume profile (never the asset), uses unscaled time only and never touches `Time.timeScale`.
- All strengths live on `Assets/_Game/Data/Cinematic/CinematicAct1-3.asset`, one per floor stage (Floor 4 = 1, Floor 3 = 2, Floor 2 = 3). Each floor is darker and harsher. `GameSettings.ReduceEffects` disables the flash, chromatic aberration, lens wobble, tilt and rotational shake, and scales down CameraShake.

### Art
- The artist delivers high-quality isometric art. Code must never require specific frame counts.
- Motion (topple, drop, shake, rewind) is done with tweens, particles or shaders unless an Animator is explicitly provided.

## Work split: CLOUD vs LOCAL

### CLOUD sessions (no Unity Editor available)
- Write and refactor C# under `Assets/_Game/Scripts/` and `Assets/_Game/Tests/`.
- Write editor scripts: validators, and `[MenuItem]` generators that create ScriptableObject assets, sample content and test scenes from code.
- Write or extend `strings_tr.csv` with finished Turkish text for any new keys. Never write "TODO" anywhere in the CSV: Validate Content and `LocalizationTests` fail on it (case-insensitive, every column).

Cloud sessions MUST NOT:
- Hand-edit `.unity`, `.prefab` or `.asset` files, or hand-write `.meta` files. Let the Editor generate GUIDs. If assets or scenes are needed, write a `[MenuItem]` that builds them and list it in the handoff.
- Change `ProjectSettings/` or `Packages/manifest.json` unless the task explicitly says to.

### LOCAL sessions (Claude Code + MCP for Unity, Editor open)
- Read the newest `HANDOFF.md` entry first and work through its "Needs local wiring" checklist.
- Run the menu items the cloud session wrote, fix compile errors, build and arrange scenes, assign references, drop in art and audio, run Play Mode and read the console.

## Coding rules
- Keep scripts small and single-purpose. Use `[SerializeField] private` with `[Tooltip]`, and give every data type a short XML summary.
- No third-party packages. No DOTween: use the coroutine tween helper in `Core/`.
- `Time.timeScale` is touched in ONE place only: `Core/PauseState` (the ESC pause sets it to 0 and back). Everything else leaves it alone; unscaled-time systems check `PauseState.IsPaused`.
- Input: the new Input System, which is already installed. UI: uGUI + TextMeshPro, with `InputSystemUIInputModule`. asmdef references use the real assembly names: `Unity.InputSystem`, `Unity.TextMeshPro`, `UnityEngine.UI` (uGUI; there is no "Unity.ugui" assembly).
- Use Unity 6 APIs (`FindFirstObjectByType` etc.) and no obsolete calls.
- Code, comments and identifiers are in English. Player-facing text comes only from TextDB.
- The project must compile after every task. If you can't verify compilation (cloud), say so in the handoff.

## MANDATORY: end every session with a HANDOFF entry
Before finishing ANY session, add a new entry at the TOP of `HANDOFF.md` (under the header) using this template. Then print the same entry as your final message.

```
## [YYYY-MM-DD HH:MM] <CLOUD|LOCAL> — <one-line task summary>
### Done
- ...
### Files created / changed
- path — what and why
### Needs local wiring (checklist for the MCP session)
- [ ] Run menu: SixtyOne > ...
- [ ] Assign X to Y on Z
### Not verified / risks
- e.g. "Not compiled — no editor in cloud"
### Next suggested step
- ...
```
