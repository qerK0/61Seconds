# 61 Seconds — Claude Code Project Guide

48-hour game jam project (Civil Game Jam, 16–18 Oct 2026). Unity 6000.3, URP, **2.5D isometric**. Team: programmer (Bora), artist, sound designer, lore/narrative writer.
Speed beats polish. Small, working, testable steps. Never leave the project in a non-compiling state.

## The Game
Linear earthquake-escape game. The player, their **wife** and their **child** start on the 6th floor of an apartment building and must get out alive. There is NO dog. AFAD alerts trigger three escalating quakes: 6.0 on floor 6, 7.0 on floor 4 and 8.0 on floor 2. Between quakes the player explores isometric rooms, collects items into a 4-slot bag, deals with odd neighbors, shuts off gas/electricity/water, and descends via stairs or holes in the floor.

Choices work like Henry Stickmin. A wrong choice ("what NOT to do in an earthquake") causes a death. The game then rewinds to the last decision point, not the start, and a visible LOOP counter goes up. Each death shows a one-line real earthquake safety tip ("Deprem Notu"). Tips are collected across loops.

**Tone rule:** absurd or comic deaths are only for the player character and "do-not-do" traps. The wife is never shown dying on screen: cut to black, play a sound and show one line of text. The child never dies: the child gets a `Trapped` state that triggers the rewind.

**Safety content must match AFAD guidance.** Drop-Cover-Hold On is correct. All of the following are TRAPS: "triangle of life", doorways, balconies, windows, elevators, running down the stairs during shaking, lighting a flame, and flipping switches when there is a gas smell.

## Architecture (data-driven: new content = new data, not new code)

Root folder: `Assets/_Game/`
```
Scripts/Core/        GameState, Flags, Inventory, Snapshot, LoopCounter, GameEvents, Tween helper
Scripts/Text/        CSV parser, TextDB, TextKey (lore/dialogue/UI strings)
Scripts/Nodes/       SceneNode, Choice, Condition, Outcome, NodePlayer
Scripts/Placement/   Anchor, Hazard, FamilyMember, PlacementNode, PlacementResolver
Scripts/Iso/         IsoSorter, IsoPicker (pointer → world object), iso helpers
Scripts/Audio/       AudioCue (SO), AudioService
Scripts/UI/          ChoicePanel, TimerBar, BagUI, LoopCounterUI, DeathCard, TipJournal, RewindFX
Scripts/Editor/      Validators, content generators, test-scene builder
Content/Text/        strings_tr.csv  ← the lore writer edits this
Data/                Nodes/ Hazards/ Items/ Tips/ Audio/ (ScriptableObject assets)
Prefabs/  Scenes/  Art/  Audio/
Tests/EditMode/      Pure-logic tests
```
Namespace: `SixtyOne.<Folder>` (e.g. `SixtyOne.Nodes`). Assembly definitions: `SixtyOne.Runtime`, `SixtyOne.Editor`, `SixtyOne.Tests.EditMode`.

### Core concepts
- **GameState** is plain C#, owned by a `GameManager` MonoBehaviour. It holds:
  - `Inventory` (max 4 item ids)
  - `Flags` (string → bool, e.g. `GasOff`, `PowerOff`, `WaterOff`, `WifeInjured`)
  - the family members' states and anchor positions
  - the current node id and the current act (1–3)
  - `LoopCount` and the collected tip ids
- **Snapshot:** a deep copy of GameState taken at the start of every decision or placement node. On death:
  - restore the latest snapshot, but keep `LoopCount` and the collected tips (they persist across loops)
  - increment `LoopCount` and play the rewind FX
  - family members go back to their LAST chosen positions, not the defaults, so the player only has to fix the wrong placement
- **SceneNode** (ScriptableObject) has:
  - an id, title/body text keys and a room ref
  - timer seconds (0 = untimed)
  - a list of `Choice`
  - optional `onEnter` outcomes
  - optional "bomb checks": conditions evaluated on enter that can kill or injure someone
- **Choice** has:
  - a label key and a type (`Normal` or `Trap`)
  - a list of `Condition`s: has item, flag is true/false, member state
  - a fail behaviour for when the conditions are not met: `Hide` or `ShowLocked`
  - a list of `Outcome`s: `GoToNode`, `Death`, `SetFlag`, `AddItem`, `RemoveItem`, `SetMemberState`
  - a death text key, a tip ref and an audio cue
  - A **delayed bomb** is an outcome that sets a flag now, plus a later node whose bomb check reads that flag.
- **PlacementNode** is a SceneNode subtype with a timed phase:
  - the player taps a family member and the valid anchors highlight
  - the player taps an anchor, and the member is repositioned instantly (no walk animation)
  - when the timer ends, the quake plays and each Hazard resolves against the members on its anchor
- **Anchor** is a MonoBehaviour in the room. It has:
  - a tag (`Safe` or `Trap`) and a capacity (a table holds 2)
  - a linked Hazard and an adjacency list
  - a label text key that stays hidden until the player has died to it once (this persists across loops)
- **Hazard** (ScriptableObject) has:
  - which quake(s) trigger it (a bitmask of acts)
  - the outcome per member type: player → Death, wife → Death or Injured, child → Trapped
  - a text key and an audio cue
  - a motion type (`Topple`, `Drop`, `Shatter` or `None`), played as a code tween on the hazard prop
- **FamilyMember rules:**
  - the wife goes wherever she is told
  - the child must be on the same anchor as an adult, or on an adjacent one
  - the player is placed last

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
- Input: the new Input System, which is already installed. UI: uGUI + TextMeshPro, with `InputSystemUIInputModule`.
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
