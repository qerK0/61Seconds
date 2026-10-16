# HANDOFF LOG

Newest entry on top. Every Claude Code session (cloud or local) appends an entry here before finishing. Format is defined in CLAUDE.md.

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
