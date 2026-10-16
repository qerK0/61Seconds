# 61 Seconds — Claude Code Project Guide

48-hour game jam project (Civil Game Jam, 16–18 Oct 2026). Unity 6000.6 (see `ProjectSettings/ProjectVersion.txt`), URP, **2.5D isometric**. Team: programmer (Bora), artist, sound designer, lore/narrative writer.
Speed beats polish. Small, working, testable steps. Never leave the project in a non-compiling state.

## The Game
Linear earthquake-escape game. The player, their **wife** and their **child** live in Flat 11 on the 4th floor and must get out of the building alive: **Floor 4 → Floor 3 → Floor 2 → exit**. There is ONE earthquake for the whole game, plus aftershocks.

**The building timer (where the tension comes from).** A `BuildingTimer` (default 300s, on the `LevelConfig` asset) starts with the game. At 0 the building collapses: death and rewind. It pulses the UI at 60s and sounds an alarm with ticks in the last 30s. Every action subtracts its time cost (moving between rooms, opening drawers, closing valves, talking, crafting); injured family members make moves slower until treated with the first aid kit.

**Flow.**
- Floor 4: the neighbour Sevim visits (dialogue; each line costs time, "Kısa kes" skips her info and whistle), then the opening quake (placement moment), then Flat 11 (gas valve, breaker), the corridor (a kind neighbour gives food + water) and Flat 12 (borrow cloth, rope, tape, flashlight, lighter, first aid; "Not bırak" leaves a note).
- Floor 3: aftershock placement on arrival; a needs-aware trader in the corridor; Flat 9 fuse puzzle (needs insulated gloves or the crafted grip + tape, else electrocution) which unlocks the cable descent (needs the child harness); Flat 10 (dark, gas leak, lighter = explosion, flashlight is safe, collapsed floor = alternative way down).
- Floor 2: strong aftershock placement in Flat 7 (gas leak); a frightened dog blocks the corridor (feed it or use the whistle; walking past = bitten, injured, pushed back); stairs → street → epilogue (family state, borrow line, loop count).

**Survival systems.** Stamina (food/water restore it; it drains over time and per heavy action; at 0 heavy actions are locked and everything costs more time), poison (gas rooms with an open valve; darkening + vignette; max = death with a gas tip; the wet cloth halves it), panic (rises as the timer falls, when poisoned and on aftershocks; above `panicDelayThreshold` clicks get an input delay up to `maxInputDelay`, with instant visual feedback; Reduce Effects forces 0), crafting (two bag items → one, order-independent; the bag stays 4 slots), hidden items (invisible hotspots in the room art; optional sparkle in later loops or after N seconds).

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
Scripts/Placement/   Anchor, Hazard, FamilyMember, PlacementNode, PlacementResolver, Room/RoomSwitcher
Scripts/Building/    LevelConfig, BuildingTimer + SurvivalModel (pure), SurvivalController, GasZone, Epilogue*
Scripts/Dialogue/    DialogueLine, DialogueNode, DialogueController
Scripts/Items/       Recipe, RecipeBook, Crafting, ItemUse, TraderProfile, TraderLogic
Scripts/Puzzles/     FusePuzzle (pure), FusePuzzleNode, FusePuzzleController
Scripts/World/       WorldInteractable, PickupItem, HiddenItemHotspot
Scripts/Iso/         IsoSorter, IsoPicker (pointer → world object), iso helpers
Scripts/Cinematic/   CinematicProfile, CinematicMixer (pure), CinematicDirector, LetterboxBars
Scripts/Audio/       AudioCue (SO), AudioService
Scripts/UI/          ChoicePanel, BagUI, BuildingTimerDisplay, StaminaBar, PoisonOverlay, ClickFeedback, FusePuzzlePanel, LoopCounterUI, DeathCard, TipJournal, RewindFX
Scripts/Editor/      Validators, BuildingLevelsGenerator (one partial per floor), test-scene builder (BuildingLayout tables)
Content/Text/        strings_tr.csv  ← the lore writer edits this
Data/                Nodes/Floor4..2 Hazards/ Items/ Tips/ Traders/ Recipes/ Audio/ LevelConfig (ScriptableObject assets)
Prefabs/  Scenes/  Art/  Audio/
Tests/EditMode/      Pure-logic tests
```
Namespace: `SixtyOne.<Folder>` (e.g. `SixtyOne.Nodes`). Assembly definitions: `SixtyOne.Runtime`, `SixtyOne.Editor`, `SixtyOne.Tests.EditMode`.

### Core concepts
- **GameState** is plain C#, owned by a `GameManager` MonoBehaviour. It holds:
  - `Inventory` (max 4 item ids), `Flags` (string → bool, e.g. `GasOff`, `PowerCut_F3`, `LeftNote`, `DogGone`)
  - the family members' states and anchor positions, the current node id and floor stage
  - `BuildingTime`, `Stamina`, `Poison` and the `BorrowLedger` (all restored by snapshots)
  - `LoopCount`, the collected tip ids, revealed anchor labels and the `KnowledgeSet` (these persist across every rewind)
- **Snapshot:** a deep copy of GameState taken at the start of every node. Nodes marked `isCheckpoint` (the start of each floor) also become the **floor checkpoint**. On death, `DeathInfo.RewindTarget` picks the snapshot:
  - `LastNode` (micro): restore the latest snapshot. Family members stay at their LAST chosen positions.
  - `Checkpoint` (macro): restore the floor checkpoint, including the building timer, and reset family positions.
  - Either way: keep the persistent data above, increment `LoopCount`, play the rewind FX (longer for macro rewinds).
- **SceneNode** (ScriptableObject) has an id, title/body text keys, a room id, a timer (0 = untimed), `Choice`s, optional `onEnter` outcomes and "bomb checks" (conditions evaluated on enter that can kill). Subtypes are driven by `INodeHandler`s: `PlacementNode` (PlacementController), `DialogueNode` (DialogueController), `FusePuzzleNode` (FusePuzzleController), `EpilogueNode` (EpilogueController). With no handler the room is explorable (`ExplorationGate`).
- **Choice** has a label key, a type (`Normal`/`Trap`), `Condition`s (has item, has any item, flag, member state), a fail behaviour (`Hide`, `ShowLocked` with a reason, `ShowLockedFatal` = clickable death naming the first missing item), `Outcome`s (`GoToNode`, `Death`, `SetFlag`, `AddItem`, `RemoveItem`, `SetMemberState`, `TraderGive`, `ShowMessage`), and costs: time seconds, stamina, `heavy` (locked at 0 stamina), `isMove` (slower with injured members). A **delayed bomb** sets a flag now; a later node's bomb check reads it.
- **Action costs** go through `ActionCostRouter` to the `SurvivalController` (building timer + stamina). Clicks go through `InputDelayRouter` (panic delay); `ClickFeedback` shows the instant ring.
- **DialogueNode** has lines (speaker key, text key, time cost per line) and choices that show during the lines (e.g. "hurry") or at the end.
- **WorldInteractable** (IClickable): time/stamina cost, optional required item, granted item, `borrowed` (recorded in the BorrowLedger), extra outcomes, one-shot as `Used_<id>` flags. `PickupItem` = open-surface preset; `HiddenItemHotspot` = invisible collider + painted overlay + sparkle.
- **Recipe** (inputA, inputB, output, time, stamina) in a `RecipeBook`; BagUI: tap an item, tap it again to use it (food/water/first aid), tap another to combine.
- **TraderProfile**: ordered needs + fallback food/water; the trader gives the first need the player lacks.
- **LevelConfig**: building timer thresholds and texts, stamina, poison (`GasZone` per room with its valve flag), panic and input delay curves.
- **PlacementNode** is a SceneNode subtype with a timed phase (opening quake and each aftershock; magnitude + shake per node):
  - the player taps a family member and the valid anchors highlight
  - the player taps an anchor, and the member is repositioned instantly (no walk animation)
  - when the timer ends, the quake plays and each Hazard resolves against the members on its anchor
- **Anchor** is a MonoBehaviour in the room: a tag (`Safe` or `Trap`), a capacity (a table holds 2), a linked Hazard, an adjacency list and a label key hidden until the player has died to it once.
- **Hazard** (ScriptableObject): outcome per member type (player → Death, wife → Death or Injured, child → Trapped), text keys, a tip, and a motion type (`Topple`, `Drop`, `Shatter` or `None`) played as a code tween on the hazard prop.
- **FamilyMember rules:** the wife goes wherever she is told; the child must be on the same anchor as an adult, or on an adjacent one; the player is placed last. Between rooms and floors the family follows the player (snaps to the room's spawns).
- **One scene** (`Test_Building.unity`) holds every room of every floor; `RoomSwitcher` shows one room at a time. One GameManager, GameState and UI means checkpoints and rewinds never load scenes.

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
- Write or extend `strings_tr.csv` with placeholder text for any new keys, prefixing the placeholder text with `TODO:`.

Cloud sessions MUST NOT:
- Hand-edit `.unity`, `.prefab` or `.asset` files, or hand-write `.meta` files. Let the Editor generate GUIDs. If assets or scenes are needed, write a `[MenuItem]` that builds them and list it in the handoff.
- Change `ProjectSettings/` or `Packages/manifest.json` unless the task explicitly says to.

### LOCAL sessions (Claude Code + MCP for Unity, Editor open)
- Read the newest `HANDOFF.md` entry first and work through its "Needs local wiring" checklist.
- Run the menu items the cloud session wrote, fix compile errors, build and arrange scenes, assign references, drop in art and audio, run Play Mode and read the console.

## Coding rules
- Keep scripts small and single-purpose. Use `[SerializeField] private` with `[Tooltip]`, and give every data type a short XML summary.
- No third-party packages. No DOTween: use the coroutine tween helper in `Core/`.
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
