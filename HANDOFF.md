# HANDOFF LOG

Newest entry on top. Every Claude Code session (cloud or local) appends an entry here before finishing. Format is defined in CLAUDE.md.

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
