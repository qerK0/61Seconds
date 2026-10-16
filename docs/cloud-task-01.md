# Cloud Task 01 — Runtime foundation

Read CLAUDE.md and HANDOFF.md first. Follow them strictly, especially the CLOUD rules (no hand-edited .unity/.prefab/.asset/.meta files, no ProjectSettings or manifest changes).

TASK: Build the complete runtime foundation for "61 Seconds" under Assets/_Game/, so that the local MCP session only has to run menu items, assign art/audio and play-test. Work on a new branch `cloud/foundation`. Commit after each milestone below with a clear message, then push and open a PR to main.

## MILESTONE 1 — Assemblies + Core
- asmdefs: SixtyOne.Runtime (Assets/_Game/Scripts, references Unity.InputSystem, Unity.TextMeshPro, Unity.ugui), SixtyOne.Editor (Editor only), SixtyOne.Tests.EditMode (NUnit, test framework).
- GameState, Flags, Inventory (4 slots), FamilyMemberState (Healthy/Injured/Trapped/Dead + anchor id), LoopCounter, Snapshot (deep copy + restore that preserves LoopCount, collected tips and revealed anchor labels), GameEvents (C# events: NodeEntered, ChoiceMade, Died, Rewound, ItemAdded/Removed, FlagChanged, QuakeStarted/Ended).
- GameManager MonoBehaviour that owns state, takes snapshots and runs the death -> rewind flow.
- Tween helper (coroutine based: move, rotate, scale, fade, shake, with easing). No DOTween.

## MILESTONE 2 — Text pipeline
- RFC 4180 CSV parser (quotes, escaped quotes, commas/newlines in fields, UTF-8 BOM tolerant).
- TextDB: loads Assets/_Game/Content/Text/strings_tr.csv (via a TextAsset reference on a TextDBSettings ScriptableObject); Get(key) returns "[[key]]" for missing keys; editor hot-reload on file change.
- TextKey serializable struct + PropertyDrawer with a searchable key dropdown and a preview of the Turkish text.
- LocalizedText component for TMP_Text.
- Create strings_tr.csv with header `key,text,notes` and placeholder rows (text prefixed "TODO:") for every key used by the sample content and UI.
- Editor menus: SixtyOne > Text > Validate (missing + unused keys, console report with clickable asset pings), SixtyOne > Text > Export Missing Keys (append empty rows).

## MILESTONE 3 — Node system
- Condition (HasItem, LacksItem, FlagTrue, FlagFalse, MemberState) with an evaluator.
- Outcome (GoToNode, Death, SetFlag, AddItem, RemoveItem, SetMemberState) with an executor.
- Choice (label key, Normal/Trap, conditions, fail behaviour Hide/ShowLocked, outcomes, death text key, tip ref, audio cue).
- SceneNode ScriptableObject (id, title/body keys, room id, timer seconds, choices, onEnter outcomes, bomb checks = list of {conditions, outcomes}).
- Item and Tip ScriptableObjects (id, name key, description key, icon sprite).
- NodePlayer: enters nodes, takes snapshot, runs bomb checks, presents choices via an interface (IChoicePresenter) so UI is decoupled, handles the timer (timeout = the node's designated timeout choice, or the first Trap if none is set).

## MILESTONE 4 — Placement system
- Anchor MonoBehaviour (id, Safe/Trap, capacity, linked Hazard, adjacency list, label key, ground point, highlight hook).
- Hazard ScriptableObject (act bitmask, outcome per member type, text key, audio cue, motion type Topple/Drop/Shatter/None, target prop id).
- FamilyMember MonoBehaviour (Player/Wife/Child) with rules: child must share or be adjacent to an anchor with an adult; player is placed last; capacity respected.
- PlacementNode (SceneNode subtype) + PlacementController (select member -> highlight valid anchors -> place; timer; on timeout -> quake -> resolve).
- PlacementResolver as pure C# (no MonoBehaviours) so it is unit-testable; returns a list of results; first fatal/trapped result triggers death/rewind, injuries set flags/states and continue.
- Anchor labels revealed after the player dies to that anchor once.

## MILESTONE 5 — Isometric + input
- IsoSorter (sortingOrder from world Y, static/dynamic mode, SortingGroup aware).
- IsoPicker (Input System pointer, mouse + touch, Physics2D.OverlapPoint, highest sortingOrder wins, ignored when pointer is over UI).
- IClickable interface used by FamilyMember, Anchor and PickupItem (pickup adds to bag if a slot is free, else shows a "bag full" message key).
- Camera shake driven by QuakeStarted with intensity per act (6.0 / 7.0 / 8.0).

## MILESTONE 6 — Audio
- AudioCue ScriptableObject (id, clips[], volume, pitch range, optional AudioMixerGroup, loop flag).
- AudioService (pooled AudioSources, Play(cue), PlayById, Stop, music layer); missing clips warn once and stay silent.
- Hook default cues to GameEvents (quake rumble, death sting, rewind, pickup, AFAD alert) via an AudioEventMap ScriptableObject.

## MILESTONE 7 — UI (uGUI + TMP, built from code where possible)
- ChoicePanel (buttons from choices; locked style with lock icon + reason key), TimerBar, BagUI (4 slots), LoopCounterUI ("LOOP #n"), DeathCard (black fade, death text, Deprem Notu, rewind button), TipJournal (collected tips x/total), AFAD alert popup, RewindFX (screen overlay: VHS-like scanlines via a simple full-screen UI shader or animated image + reverse shake).
- All strings via TextDB keys.

## MILESTONE 8 — Editor tooling + sample content
- SixtyOne > Validate Content: every GoToNode has a target, every Trap/Death has a death text and tip, every Hazard is linked to an anchor, no unreachable nodes from the start node, all text keys exist.
- SixtyOne > Generate Sample Act 1: creates ScriptableObject assets for Act 1 (floor 6): AFAD 6.0 alert node, a PlacementNode in the living room (anchors: table [Safe, cap 2], inner wall [Safe, cap 1], wardrobe front [Trap, Topple], window [Trap, Shatter], doorway [Trap], balcony [Trap]), a post-quake exploration node with pickups (wrench, rubber gloves, first aid kit, flashlight, plus trap items like gold/laptop), a gas valve choice requiring the wrench (sets GasOff), a "make tea / light the stove" trap, and an exit to the stairs node. Include one delayed bomb: if GasOff is false when entering Act 2's first node -> kitchen fire death.
- SixtyOne > Build Test Scene: creates Assets/_Game/Scenes/Test_Act1.unity by code with an orthographic camera, GameManager, TextDB, AudioService, EventSystem with InputSystemUIInputModule, the full UI canvas, and placeholder isometric diamond sprites (generated via Texture2D) for floor, anchors, props and the three family members, all wired to the generated Act 1 data. It must be playable end to end with placeholders.

## MILESTONE 9 — Tests (EditMode)
- CSV parser edge cases, Condition evaluation, Outcome execution, Snapshot restore keeps LoopCount/tips, PlacementResolver (child adjacency rule, capacity, trap outcomes per member type, act bitmask), delayed bomb flow.

## CONSTRAINTS
- Unity 6000.3 APIs only, no obsolete calls, no third-party packages.
- Code/comments in English; all player-facing text through TextDB keys (Turkish placeholders allowed in the CSV only).
- Small single-purpose files; XML summaries on public types, [Tooltip] on serialized fields.
- You cannot compile in the cloud: be extra careful with namespaces, asmdef references and using directives; double-check every type you reference exists.

## FINISH
Add the HANDOFF entry exactly as CLAUDE.md requires. The "Needs local wiring" checklist must list, in order, every menu item to run and anything to assign, so the local MCP session can go straight down the list. Print the same entry as your final message.
