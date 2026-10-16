# HANDOFF LOG

Newest entry on top. Every Claude Code session (cloud or local) appends an entry here before finishing. Format is defined in CLAUDE.md.

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
