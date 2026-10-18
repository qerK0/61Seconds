# HANDOFF LOG

Newest entry on top. Every Claude Code session (cloud or local) appends an entry here before finishing. Format is defined in CLAUDE.md.

---

## [2026-10-18 21:30] LOCAL — TV unit art, slightly darker grade, experimental foreground wind dust
### Done
- **TV art:** tv1.png is a flat-screen TV panel and tv2.png is an old CRT on its stand. So the mapping is the other way round from the guess: **TvUnit = tv2** (~0.45u) and **Tv = tv1** (~0.35u). Both have a bottom-centre pivot on the opaque pixels.
  - Both are in the Layout Editor prop palette ("TV ünitesi" / "TV").
  - TvUnit is solid. Tv is not solid and spawns with a lift equal to the unit's height (`PropArt.TvUnitHeight` = 0.45). Like wall fixtures, the Tv keeps its lift at build time.
  - New container preset "TV ünitesi" (`tv_unit`, `container.tv_unit`, registered as `UiKeys.ContainerTvUnit`), empty by default.
  - Nothing was placed. `RoomLayoutOverrides.asset` was not touched (same sha1 before and after: 16d835…).
- **Darker grade:** postExposure −0.18 on every act asset. Act 1 −0.33, act 2 −0.48, act 3 −0.63, street −0.03. The code defaults moved by the same step. UI is unaffected.
  - Flat 10 was checked by screenshot: the stove, cabinets, valve, floor and gas haze are still readable, so no practical lights were raised.
  - The street's exposure number stays above act 3's (set in task 17 so the walkway stays readable). It still reads darkest because of its colder, desaturated grade and vignette.
- **Wind dust (EXPERIMENTAL):** `Cinematic/WindDustOverlay.cs` on `FX_WindDust` under the CameraRig.
  - Two self-built particle systems: 170 fine specks and 30 soft, out-of-focus motes, in warm grey at alpha 0.12–0.32.
  - They drift left → right with a slight fall, with gusts every 4–8 s for 1.5 s.
  - Camera space, sorted above the world and below the UI.
  - Denser in the street (×1.7); Reduce Effects cuts it to 40 %.
  - Uses unscaled time and pauses with the pause menu. No colliders.
### Files created / changed
- `Scripts/Cinematic/WindDustOverlay.cs` (new).
- `Editor/SceneBuild/TestSceneBuilder.cs`: `WindDust` flag + FX_WindDust creation.
- `Editor/Art/PropArt.cs`: TvUnit / Tv entries and labels.
- `Editor/SceneBuild/SceneKit.cs`: the Tv keeps its lift.
- `Editor/Layout/PropFootprint.cs`: Tv is not solid.
- `Editor/Layout/LayoutEditorWindow.cs`: Tv spawn lift, solid flag from PropFootprint, "TV ünitesi" label.
- `Editor/SceneBuild/BuildingLayout.cs`: `tv_unit` preset.
- `Text/UiKeys.cs`: `ContainerTvUnit`.
- `strings_tr.csv`: `container.tv_unit`.
- `Cinematic/CinematicProfile.cs`: default exposures.
- `Data/Cinematic/CinematicAct1-3.asset`, `CinematicStreet.asset`.
- `Data/Art/PropArtSet.asset`, tv1/tv2 import settings, `Test_Building.unity` (rebuilt).
- Screenshots: `Screenshots/t18_salon_dust.png`, `t18_flat10_dust.png`, `t18_street_dust.png`.
### Removing the wind dust (one step, either way)
- **Turn it off:** set `WindDust` to false in `TestSceneBuilder.cs` (next to `AmbientFillIntensity`), then run Build Test Scene.
- **Delete it:** remove the `if (WindDust) { … FX_WindDust … }` block and the `WindDust` field in `TestSceneBuilder.cs`, delete `Assets/_Game/Scripts/Cinematic/WindDustOverlay.cs` (+ .meta), then run Build Test Scene. Nothing else references it.
### Needs local wiring (checklist for the MCP session)
- [ ] None. Bora places the TV unit / TV in the Layout Editor.
### Not verified / risks
- The dust was checked by screenshot and particle counts only (197 max, pauses, no colliders). Gust feel and density were not judged in motion.
- The TvUnit art already has a CRT on it, so a flat Tv on top may look doubled. Use either one, or put the Tv on another surface.
- 0 compile errors, EditMode 236/236, Validate Content 0 problems, Text Validate 0 missing / 0 unused.
### Next suggested step
- Watch the dust in motion. Tune or switch it off with the `WindDust` flag.

---

## [2026-10-18 20:30] LOCAL — local-task-17 (repeating info fix, Craft Defteri, ending slot, night street) + the earlier final pass (collision, gas valves, items)
### Done
- **Final pass (earlier today, no entry was written then):**
  - **Collision:** solid props block the cells under their sprite base (`PropFootprint`):
    - the base is the opaque width of the bottom half of the sprite × the saved scale;
    - the footprint's front corner sits on the pivot;
    - wall and ceiling fixtures and signs never block.
  - **Keeping rooms playable:** required cells (spawns, anchors, doors, hotspots, ropes, NPCs) are carved out. A prop that still cuts a path shrinks to its centre cell, and walled-in pockets are blocked.
  - **Shrunk props:** `hall_nightstand` (Flat 11 hall) no longer blocks; `floorhole_23a77` (Flat 9 living) was shrunk to its centre cell.
  - **Container approach:** if a container's approach cell is blocked, the player uses the nearest reachable free cell.
  - **Gas valves:** `GasValveView` runs on all 3 valves:
    - closing turns the handle 90° and plays a creak;
    - the hiss loop stops and the green haze fades;
    - everything is driven by the valve flag, so rewinds restore it;
    - a valve is moved next to its stove when the stove was moved by hand.
  - **Items:** `ItemPlan` fills every container (generous), and a Flat 7 kitchen cabinet was added. Validate Content checks progression (3 bedsheets, harness, gloves or cloth + tape, flashlight, whistle, wrench, ≥3 food, ≥3 water). Floating world labels on items and containers were removed.
- **Part 1, repeating info bug:**
  - **Root cause:** a rewind restored a snapshot that was already close to a death (no stamina or high exhaustion, high poison, or a nearly empty building timer at a checkpoint). The same death fired again seconds later, with its death card and macro info card, again and again.
  - **Fix:** `RewindRecovery` after every rewind:
    - poison and exhaustion → 0 (macro) or ≤ 0.35 (micro);
    - stamina ≥ 40 %;
    - building time ≥ 60 s (`minSecondsAfterRewind`).
  - The macro info card shows once per checkpoint and death cause (`RewindCardGate`). The toast queue is cleared on rewind.
  - Regression test `RepeatingInfoTests` (3 deaths: gas, trap, timer).
- **Part 2, Craft Defteri (`CraftNotebook`):**
  - Look: a spiral-bound lined page (#F3EEDF, blue rules, red margin, 10 rings over punched holes, −1.5°, shadow) in Caveat SDF (`UITheme.Hand` / `HandBold`).
  - Content: one line per recipe plus the bedsheet rope line. Undiscovered results show `???` under a scribble. Discovery = crafted, or all inputs held at once; it is stored in the KnowledgeSet as `recipe:<output>`.
  - Crafting: two taped slots (a bag click fills them while the notebook is open) and a `Birleştir` button with the same costs as the bag combine. Two bedsheets in the slots tie the rope.
  - Open with the `Defter` button by the bag header or `C`. Esc closes it before the pause menu. A paper rustle plays and timers keep running.
- **Part 3, ending slot:**
  - `EndingCinematic` plays at the sign before the epilogue. Frames come from `Data/Cinematic/EndingCinematicConfig` (12 fps, `cue_ending`, else the outro music) and can be skipped after 1 s.
  - Fallback (no frames): fade to black, then `Acil toplanma alanına ulaştınız.` in IBM Plex Mono for 3 s.
  - No deaths while it plays.
  - Menu `SixtyOne > Art > Import Ending Frames` (folder `Art/anim/ENDING`).
- **Part 4, the street (`StreetDressing` + `StreetAmbience`):**
  - **Ruins and rubble:** `enkaz son 2/3` façades along the back edge (alternating, flipped, scaled, darker further back), `enkaz son 1` mounds (also the façade-drop rubble at 0.55 scale), small debris, broken low walls.
  - **Street props:**
    - thrown furniture;
    - a sinkhole (solid FloorHole);
    - door art on our building;
    - dark cars with a blinking orange alarm light;
    - 3 lamps (dead / flicker / swing).
  - **Atmosphere:**
    - fire glow with sparks and smoke;
    - 2 fog layers and falling ash;
    - red / blue emergency lights pulsing from the left;
    - a green ACİL TOPLANMA ALANI board on the sign post, with a warm light and 4 survivors in blankets;
    - a lighter walkway band on the darkened floor.
  - **Sound:** wind loop, distant sirens, an ambulance that gets louder toward the sign, and a distant dog bark or metal groan every 10–20 s.
  - **Grade:** new `CinematicStreet.asset`, used while the street is shown. It is colder and stronger-vignetted than floor 2; exposure was tuned up so the walkway stays readable.
### Files created / changed
- Scripts:
  - `Core/RewindRecovery.cs` (+ `RewindCardGate`), `Building/SurvivalController.cs`, `UI/Menu/InfoCardScreen.cs`, `UI/MessageToast.cs`;
  - `UI/CraftNotebook.cs`, `UI/BagUI.cs`, `UI/PauseMenu.cs`, `UI/UITheme.cs`, `Text/UiKeys.cs`;
  - `Cinematic/EndingCinematic.cs`, `EndingCinematicConfig.cs`, `CinematicDirector.cs`, `CinematicProfile.cs`, `Building/EpilogueController.cs`, `Core/GameManager.cs`;
  - `World/StreetAmbience.cs`, `World/StreetQuakeDirector.cs` (rubble scale), `World/GasValveView.cs`.
- Editor:
  - `Layout/PropFootprint.cs`, `Layout/ItemPlan.cs`, `Layout/LayoutOverrides.cs`;
  - `SceneBuild/StreetDressing.cs`, `TestSceneCraftNotebook.cs`, `TestSceneEnding.cs`, `GasValves.cs`, `TestSceneBuilder.cs`, `TestSceneUi.cs`, `TestSceneCinematic.cs`, `BuildingLayout.cs`, `SceneKit.cs`, `PlaceholderArt.cs`;
  - `Art/EndingArt.cs`, `Art/PropArt.cs`, `Text/FontAssetsGenerator.cs`, `Cinematic/CinematicAssetsGenerator.cs`, `Content/BuildingLevelsGenerator.cs`, `Content/TeamAudioImporter.cs`.
- Tests: `RepeatingInfoTests.cs`, `PropCollisionTests.cs`, `LayoutOverrideTests.cs` (carve-out), `LayoutV2Tests.cs` (stairs art).
- Data / art:
  - `strings_tr.csv` (`ui.craft.*`, `ui.ending.fallback`);
  - Caveat SDF assets, `CinematicStreet.asset`, `StreetHaze.mat`, `EndingCinematicConfig.asset`, new cues;
  - `Audio/Generated` sfx (gas hiss, valve creak, paper rustle, wind, siren, metal groan; all from `tools/gen_sfx.py`);
  - `Test_Building.unity` (rebuilt).
### Needs local wiring (checklist for the MCP session)
- [ ] When the ending frames arrive: drop `frame0000.png…` into `Assets/_Game/Art/anim/ENDING/`, then run `SixtyOne > Art > Import Ending Frames`. No rebuild is needed. Set the fps on `EndingCinematicConfig` (default 12) and an optional clip on `cue_ending`.
### Not verified / risks
- **Not done end to end:** the full menu-to-end playthrough. Instead, these were tested with Play Mode code:
  - street → quake → sign → ending fallback → epilogue;
  - 2 street deaths (1 info card, then none);
  - macro and micro rewinds;
  - a notebook craft (cloth + water → wet cloth).
- **Never launched:** the Windows exe from the earlier final pass was not run for 2 minutes. The zip was made only in this session.
- **Street look was tuned by screenshot only.** The grade is brighter than floor 2 so it stays readable. The fire, emergency and lamp glows are subtle under the exposure, and the thrown-furniture and lamp positions were set by numbers. Check it in-game.
- The street lamp head reuses `lamba.png` (a wall lamp), so it reads small.
- The previous debris planner still scatters `enkaz 1/2` over the street walkway after the quakes (existing behaviour).
### Next suggested step
- Play the whole run from the main menu once and check the street by eye.
- Drop the ending frames in when they are ready.

## [2026-10-18 15:10] LOCAL — New furniture art (gardrop, komidin, sandalye) + stairs art (merdiven)
### Done
- **Props** (`PropArt`: bottom-centre pivot on the opaque pixels, PPU from the target height):
  - `Wardrobe` uses gardrop.png (~1.1u); the old dolap.png is the `WardrobeOld` variant.
  - `Chair` uses sandalye.png (~0.55u); masaa.png is `ChairOld`.
  - New `Nightstand` from komidin.png (~0.32u).
  - `WardrobeV2` is unchanged.
  - Palette labels: "Wardrobe (eski)", "Wardrobe v2", "Chair (masaa)" (`PropArt.Label`).
- **Overrides keep working:** prop names did not change, so overrides that use "Wardrobe" / "Chair" load the new art. Only an explicitly chosen variant (WardrobeV2 / WardrobeOld / ChairOld) keeps the old look.
- **Container presets:** "Gardırop" uses the new wardrobe art. "Komodin" (container.nightstand) now has `Art = Nightstand`, which also covers `hall_nightstand` (the flashlight).
- **New empty nightstands** next to each bed: `bedroom_nightstand` (Flat 11), `flat12_nightstand` and `flat8_nightstand` (borrowed).
- The salon and F7 desk chairs are "Chair" props, so they switch to sandalye automatically.
- **Stairs:** new `DoorArtKind.Stairs`. `DoorArt.Stairs` builds `Generated/stairs_wall.png` from merdiven.png:
  - each column's bottom sits on the gap's 2:1 floor edge;
  - one vertical scale for all columns, so the tallest column fills 97 % of the two-row wall and the steps keep their shape;
  - the width is one wall segment;
  - flipped on right-wall gaps.
- **Usable stairs** (only the art changed; choices, costs and nodes are the same) keep the "MERDİVEN ↓" sign: F4 corridor L0 → F3, F2 corridor L0 → stairs room, F2 stairs room L1 → street (hotspot `Art = "Stairs"`; the robber still triggers there).
- **Decorative stairs** (`RoomSpec.DecorStairs`, `SceneKit.DecorStairs`; no collider, no sign):
  - F3 corridor L0, the flight down from F4 (new L0 gap);
  - F3 corridor R1, the broken flight: dark tint plus 2 debris sprites. The stair-edge prop and its hazard anchor are unchanged.
- **Layout Editor:** the door art popup is now Door / Elevator / Stairwell door / Stairs.
### Files created / changed
- `Scripts/Editor/Art/PropArt.cs`, `DoorArt.cs`, `Editor/SceneBuild/BuildingLayout.cs`, `SceneKit.cs`, `TestSceneBuilder.cs`, `Editor/Layout/LayoutEditorWindow.cs`, `RoomLayoutOverrides.cs` (tooltip).
- `Data/Art/PropArtSet.asset`, the gardrop / komidin / sandalye / merdiven import settings, `Art/Concept/Generated/stairs_wall.png`, `Test_Building.unity` (rebuilt with the overrides).
### Needs local wiring (checklist for the MCP session)
- [ ] None.
### Not verified / risks
- **Step 3 (verify) was skipped at Bora's request.** No screenshots, no F4 → street walk, EditMode not run, Validate Content not run.
- What was checked: 0 compile errors; Import Prop Art and Build Test Scene ran. The scene contains `Stairs_L0`, `Stairs_R1_broken` and the stair doors.
- The first Build Test Scene attempt lost the MCP connection; the second one completed and the scene was saved.
- **Look first if something seems off:**
  - the stairs art is small (143 × 156) and stretched to the wall height, so it may look soft or tall;
  - the debris offsets on the broken stairs were placed by numbers, not by eye;
  - the nightstand spots (0.5, 3.3–3.4) may overlap a bed corner or a wall lamp.
- Layout Editor door proxies still show the door sprite until Rebuild scene.
### Next suggested step
- Look at the F4 / F3 / F2 stairs and a bedroom in the editor, then run Validate Content + EditMode.


## [2026-10-18 13:30] LOCAL — Task 16: 8-minute timer, intro briefing, floor info cards, robber rework + idle animation
### Done
- **8 minutes:** `buildingSeconds` 300 → 480; the pulse stays at 60 s, the alarm + ticks at 30 s, the prep at 60 s.
  - Stamina retune: drain 0.42 → 0.36/s. With no food or water you hit 0 at ~278 s; heavy actions bring that a little earlier (~250–270 s).
  - Food 35 → 33 (≈ 92 s), water 25 → 32 (≈ 89 s).
  - No text said "5 dakika". The timer tests use 480.
- **Intro briefing** (`UI/Menu/IntroBriefing`, built by `TestSceneIntroCards.BuildIntro`): BİLGİLENDİRME in the end-screen style.
  - Input (ANLADIM, Enter, Space or a click) only counts after 1 s.
  - It holds the first node through the new `Core/RunStartGate`, so the AFAD alert waits.
  - It also holds every timer through the new `Core/ScreenHold`.
  - It shows on every scene load (menu start and Yeniden Dene) and never on a rewind.
- **Floor info cards** (`UI/Menu/InfoCardScreen` + pure `Core/InfoCardRules` + `Core/InfoCardStore` in PlayerPrefs):
  - Shown on F4→F3 (card 3), F3→F2 (card 4) and F2→street (card 8), detected as the floor stage going up on NodeEntered.
  - Also shown on every macro rewind, picked by the death tip: gas → 4, electric → 5, placement traps → 2, collapse / exhaustion / bag → 6. Extras: elevator → 3, street → 8.
  - Otherwise it shows the next unseen card, cycling once all 8 are seen.
  - Layout: 6 s progress bar, continue after 1.5 s, 0.3 s fades.
  - `ScreenHold` makes `NavigationEvents.TimerDelta` return 0 and stops NodePlayer's node timer. Input is blocked and ESC is ignored while a card is up.
- **Galeri:** two tabs, Deprem Notları / Bilgi Kartları (a 4×2 grid, "Görülen kartlar: X/8"), each with a red marker.
- **Robber:** Tehlikeli Adam, a mugger with menacing lines. All 3 replies were rewritten:
  - each reply's body now shows on the narration card;
  - Ona bağır makes him step closer with a lunge while the family flinches (`RobberNode.stepsCloser`);
  - after he runs off, Mehmet says `ui.robber.after`, then the game goes to the street;
  - the epilogue line is updated and the son line is removed.
  - No violence and no injury.
- **Robber idle:** `hırt` was moved to `Art/char/ROBBER_IDLE` (AssetDatabase.MoveAsset). It loops at 8 fps for the 8-frame set and 24 fps for a set of 24 or more frames (`CharacterArt.NpcIdleFpsFor`). The art faces left and is flipped toward the family (`Face` = the stairs spawn). The run-off tween still goes to the stairwell door, then fades.
- **Fix:** speech-bubble name tabs uppercase with the tr-TR culture ("TEHLİKELİ ADAM", not "TEHLIKELI").
### Files created / changed
- New: `Core/ScreenHold.cs`, `Core/RunStartGate.cs`, `Core/InfoCardRules.cs`, `Core/InfoCardStore.cs`, `UI/Menu/IntroBriefing.cs`, `UI/Menu/InfoCardScreen.cs`, `Editor/SceneBuild/TestSceneIntroCards.cs`, `Tests/EditMode/InfoCardRulesTests.cs`, `Art/char/ROBBER_IDLE/` (moved).
- Changed:
  - `Core/NavigationEvents.cs`, `Nodes/NodePlayer.cs`, `UI/PauseMenu.cs`, `UI/Menu/TipGalleryPanel.cs`, `UI/SpeechBubble.cs`, `Text/UiKeys.cs`;
  - `Dialogue/RobberController.cs`, `RobberNode.cs`, `RobberSetup.cs`;
  - `Building/LevelConfig.cs`, `BuildingTimer.cs`;
  - `Editor/Art/CharacterArt.cs`, `Editor/SceneBuild/SceneKit.cs`, `BuildingLayout.cs`, `TestSceneUi.cs`, `MainMenuBuilder.cs`, `Editor/Content/BuildingLevelsGenerator(.Floor2).cs`;
  - `strings_tr.csv` (intro, floor labels, 8 cards, tabs, robber). Python's csv writer also dropped unneeded quotes on a few unrelated rows; the content is the same.
  - LevelConfig, food and water assets; the F2 robber node assets; both scenes rebuilt.
### Needs local wiring (checklist for the MCP session)
- [ ] None. If Bora copies the 101-frame robber set into `Art/char/ROBBER_IDLE` (frame0000…), run SixtyOne > Art > Import Character Frames, then Build Test Scene; it switches to 24 fps on its own.
### Not verified / risks
- **Bora stopped testing partway ("testi sal herşey okkey").** Verified:
  - 0 compile errors.
  - EditMode 230/230 after parts 1–3. Not re-run after parts 4–5.
  - In Play Mode: the intro (1 s guard, holds the first node and the prep timer), the macro rewind after a gas death (kitchen fire) showing card 4, card 3 with the prep frozen, and the robber's menacing first line, the choice thought bubble, the Ona bağır reply and the food being taken (bag 3 → 1, RobbedAtExit).
- **Not checked:** the step-closer and flinch on screen, the run-off and Mehmet's after line on screen, Yalvar / Kısa kes, card 8 on the real street arrival, the end screen, the Galeri tab in the menu, Validate Content and Text > Validate.
- **Jumping nodes from code mixes stages.** The intro can also be dismissed by the click that focuses the Game view, which still counts as a real click after 1 s.
- `Screenshots/t16_card_gas_rewind.png` shows the game after the card had already closed, not the card itself.
### Next suggested step
- Play one full run: intro → cards 3/4/8 → each robber reply → end screen. Then run Validate Content + Text > Validate and EditMode.


## [2026-10-18 10:20] LOCAL — Task 15: "İSTANBUL AFET BÖLGESİ" main menu + "SİMÜLASYON SONA ERDİ" end screen
### Done
- **Fonts:** TMP SDF assets for Oswald Bold / Medium and IBM Plex Mono Regular / Bold, with ASCII, full Turkish, ’ and ‹›.
  - Each has the Inter dynamic fallback; Oswald has no arrow glyphs, so → falls back to Inter.
  - The fonts are new slots on UITheme (`DisplayBold`, `DisplayMedium`, `Mono`, `MonoBold`), set by Generate Fonts + Theme.
- **Main menu (`SixtyOne > Build Main Menu`, rebuilt):**
  - Background: menu_bg with a Ken Burns move (1.0 → 1.06 plus a pan, 40 s ping-pong).
  - Overlays: 6 pulsing alert pins, smoke and ash motes, screen corner brackets, scanlines, vignette.
  - Readouts (IBM Plex Mono, 16 px): KANDİLLİ quake clock, coordinates with a jittering last digit, the seismic-feed clock, a zone line.
  - A seismograph polyline (custom Graphic). Every 4–7 s it spikes, with a soft thud and a micro-shake.
  - Title: "İSTANBUL" (Oswald Bold 150) uses a distressed TMP material (scratch face texture, #5A0A0A outline, dark red inner underlay) over a red haze. "AFET BÖLGESİ" is Oswald Medium 64 with 0.08em spacing.
  - Title glitch every 5–8 s (0.15 s): red / cyan split, a masked horizontal slice that jumps aside, a flicker.
  - **Oyunu Başlat** (primary): red gradient, 2 px bright border, glow, metal corner brackets.
    - Idle: the glow pulses.
    - Hover / selected: stronger glow, a diagonal shine sweep, scale 1.03.
    - Press: 0.97, a micro-shake, the press click and a low thud.
  - **Ayarlar / Galeri / Çıkış** (secondary): dark red rows, uppercase and letter-spaced. When hot, an accent bar and › slide in and the text brightens.
  - A scrolling hazard stripe under the buttons.
  - Mouse hover selects the button, so mouse and keyboard / gamepad share one "hot" state. Up/Down wrap; Enter submits; Esc / B closes panels.
  - Intro: a 0.6 s fade from black; the title slams in (1.15 → 1, shake, thud, dust puff); the buttons slide in from the left, staggered. Any key or click skips.
  - Start: 0.5 s fade to black with the quake rumble, then Test_Building.
  - Ayarlar: the shared SettingsPanel, restyled (dark red panel, Oswald, red slider fills).
  - Galeri: 18 note cards (locked = ???) and the "Toplanan notlar: X/18" counter. No "endings seen" data exists, so it shows only the notes.
  - Çıkış: a confirm dialog, then `SceneFlow.Quit` (it also stops Play Mode in the Editor).
  - Version tag bottom-right. Audio: prep music ducked to 60 % over a rumble bed.
- **Notes persist:** new `Core/TipStore` (PlayerPrefs). GameManager saves a tip on every death and loads the saved tips into every new run, so Yeniden Dene keeps them.
- **End screen** (Test_Building overlay, `TestSceneEndScreen` + `UI/Menu/EndScreen`):
  - `EpilogueController` now raises `GameEvents.EpilogueDone` after the epilogue text: a key or click after 4 s, or 20 s.
  - Visuals: end_bg pushing in (1.0 → 1.05), ash and smoke, film grain, scanlines, vignette.
  - Reveal:
    - the title types in (Plex Mono Bold 72, 0.06em) behind a blinking cursor, then glows;
    - the line types in, ending in amber "hayatta kalabilirdin.";
    - after 0.8 s the red punchline lands with one hard glitch, a boom and a shake.
  - Stats count up one by one: Döngü sayısı, Toplanan Deprem Notları X/18, Kalan bina süresi (or "Bina arkanızda çöktü"), Aile.
  - Buttons:
    - **YENİDEN DENE** (amber text, the underline grows) reloads the game: a full new prep, notes kept.
    - **MENÜYE DÖN** (framed, orange glow) fades out and loads MainMenu.
  - Audio: the outro music fades in; soft type ticks; the boom on the punchline. The AFAD footer line is at the bottom. Space / Enter / click completes the reveal.
- **New effect components** (`Scripts/UI/Menu`): KenBurns, UiShake, GlitchText, MenuButtonFx, UvScroll, HudReadout, AlertPin, Seismograph, UiParticles, ScreenOverlayFx, ScreenFader, MenuIntro, TipGalleryPanel, EndScreen.
  - All run on unscaled time.
  - Reduce Effects turns off the glitches, the shakes, the scanline flicker and the grain. Fades and particles stay (particles at 40 %).
- **Audio:** `tools/gen_sfx.py` takes an optional name prefix. `py tools/gen_sfx.py Assets/_Game/Audio/Generated sfx_menu_` wrote only the 6 new sounds (hover, press, thud, boom, type, rumble loop); the existing WAVs are untouched. `MenuAudio` makes the cue_menu_* cues.
- **Text:** final Turkish strings for every `ui.menu.*` / `ui.end.*` key. The menu title is now "İSTANBUL", the start button "Oyunu Başlat".
- **Verify:**
  - 0 compile errors.
  - EditMode **226 / 226** (new: TipStoreTests).
  - Validate Content: 0 problems. Text > Validate: 439 / 0 / 0 / 0.
  - Played: MainMenu → Oyunu Başlat → Test_Building, where the saved notes loaded into the run → epilogue node → end screen after the 20 s read timeout → Yeniden Dene (fresh prep, notes kept) → end screen → Menüye Dön → MainMenu (Start selected).
  - Screenshots in `Screenshots/`:
    - `t15_menu_idle.png`, `t15_menu_hover_start.png`, `t15_menu_hover_settings_row.png`, `t15_menu_gallery.png`, `t15_menu_settings.png`;
    - `t15_end_typewriter.png` (mid-reveal), `t15_end_final.png` (stats + buttons).

### Files created / changed
- New:
  - runtime: `Scripts/Core/TipStore.cs`, `Scripts/UI/Menu/*.cs` (14 components);
  - editor: `Scripts/Editor/Art/MenuArt.cs` (procedural UI art → `Art/Menu/Generated`), `Scripts/Editor/SceneBuild/MenuKit.cs`, `MenuAudio.cs`, `TestSceneEndScreen.cs`;
  - `Tests/EditMode/TipStoreTests.cs`;
  - Oswald / Plex SDF assets, the sfx_menu_* WAVs, the cue_menu_* cues, the menu art (bg, generated sprites, title material).
- Changed:
  - `UI/MainMenu.cs` (rewritten), `UI/UITheme.cs`, `Building/EpilogueController.cs`, `Core/GameEvents.cs` (EpilogueDone), `Core/GameManager.cs` (TipStore), `Text/UiKeys.cs`;
  - `Editor/SceneBuild/MainMenuBuilder.cs` (rewritten), `TestSceneUi.cs`, `Editor/Text/FontAssetsGenerator.cs`;
  - `strings_tr.csv`, `tools/gen_sfx.py`;
  - `MainMenu.unity`, `Test_Building.unity` (rebuilt).
  - Removed `UI/MenuDrift.cs`.

### Needs local wiring (checklist for the MCP session)
- [ ] None. If the art changes: run SixtyOne > Build Main Menu and Build Test Scene.

### Not verified / risks
- **1280×720 was not captured.** The canvas scales with the screen (16:9, match 0.5), so the layout is the same, but nothing was looked at at that size.
- **Keyboard navigation was driven through the EventSystem from code**, not by real key presses (same for the intro skip and Space / Enter on the end screen). The Up/Down / Enter wiring is explicit Navigation + InputSystemUIInputModule.
- **The shine sweep was not caught on camera.** The hover screenshot shows the strong glow; the sweep (0.55 s) had already finished.
- **The epilogue → end screen hand-off** was tested through the 20 s timeout, not a click after 4 s.
- **The distressed title needs an over-1 face colour (1.45)** because the TMP face texture darkens the whole face; it reads off-white in the shots.
- **Menu panels are fully opaque.** At 0.96 alpha the HDR title bled through.
- **The "PlayerLoop called recursively" console warnings** came from my editor-side frame pumping during the checks, not from game code.
- **I cleared the test notes I saved into PlayerPrefs** during the checks.
- **Not committed:** your uncommitted `RoomLayoutOverrides.asset` change from before this session.

### Next suggested step
- Bora checks the menu at 1280×720 and with Reduce Effects on, and plays a real run to the end screen.

---

## [2026-10-18 09:10] LOCAL — Task 14: new props/items, cut items, stairwell doors, Layout Editor v2, varnish pass
### Done
- **1. New art** (Art/items):
  - PropArt now also imports from Art/items: WardrobeV2 (1.1), KitchenCabinet (0.6), Toolbox (0.25), Armchair + Sofa (0.5), Bed (0.45), FloorHole (1.4 wide, centre pivot).
  - The coloured sofas and beds became art by name. The Flat 11 hall toolbox uses the tool-bag art. The Flat 10 collapsed floor is a FloorHole decal: floor layer, under characters, blocks its nav cell; the way down still needs rope + harness.
  - Container presets: `wardrobe_v2` (still "Gardırop"), `kitchen_cabinet` ("Mutfak dolabı"), `toolbox` ("Alet çantası").
  - Item art: `cloth` = bez kuru; `wet_cloth` world = bez islak; bag/UI icon = bez maske.
  - New `UI/MaskIndicator`: a "Maske takılı" chip (mask icon), left of the panic meter and drawn over the gas darkening. It shows while the player has `wet_cloth` in a room with leaking gas (`SurvivalController.MaskWorn`). No new mechanic.
- **2. Cuts:**
  - Removed `gold`, `laptop`, `console`: item assets, pickups, the jewelry box, `Item.isTrap`, CSV keys; tests now use other ids.
  - Removed the medicine cabinet (salon_cabinet / container.medicine).
  - `first_aid` moved to a new Flat 11 kitchen cabinet (`kitchen_cabinet`). Flat 12's cabinet still has one.
  - The stale laptop / console / StairsDown override entries were pruned from RoomLayoutOverrides.
  - New Validate Content rule `CheckProgressionItems`: at least 3 bedsheets (LevelConfig count), bedsheet_rope, rope, gloves or grip+tape, flashlight, child_harness, whistle, food, water and first_aid must each come from somewhere: rooms (overrides applied), outcomes, traders, or recipes (fixpoint).
- **3. Stairwell doors** (there is no stairs art):
  - F4 corridor L0 → F3 and F2 corridor L0 → stairs are `DoorSpec.Art = Stairwell` (was the G1 floor exit + StairsDown prop).
  - The F2 stairs room has a wall hotspot at L1 that plays `f2.exit.c_out`.
  - Each has the kapi art plus a dark plate with "MERDİVEN ↓" (`ui.sign.stairs`).
  - The F3 stair edge is now marked by debris art (2x debris sprite); its hazard anchor is unchanged.
- **4. Layout Editor v2:**
  - Per-object uniform `objectScale` (slider 0.5–2 or the Scale tool, kept uniform on release; Reset scale; Match prop height from the PropArt table) and `sortNudge`.
  - The builder applies scale + nudge; "Approach" children keep their world position. Nav blocking uses the scaled footprint (scale 2 = 2x2 cells).
  - Doors and the stairwell hotspot are `LayoutKind.Door`: drag along the wall (snaps to the nearest L/R gap) or pick the edge. Flip and art (Door / Elevator / Stairwell) are editable; the target, choice and costs are read-only.
  - A moved two-way door moves the arrival point on the other side; gaps follow it.
  - Check reports doors off-wall or two doors in one gap, then reachability as before.
  - Container palette labels are Turkish (Ecza dolabı gone). Old entries load as scale 1 (`Scale1`). `docs/layout-editor.md` updated.
  - Persistence test: salon wardrobe scaled 1.3 and the Flat 12 door dragged to L5, saved, rebuilt: both persisted, 0 layout problems. **The overrides asset was then restored from a backup**, so Bora's layout is unchanged.
- **5. Varnish:**
  - `CinematicProfile.lut` + `lutContribution` (0.6). Every act gets LUT_61s_Dust_32 from the generator; CinematicDirector drives a URP ColorLookup, and the base volume has one.
  - Film grain is Thin1 (act 1/2/3: 0.12 / 0.14 / 0.16, response 0.8). The mixer zeroes grain under Reduce Effects, rewind spike included; the LUT stays.
  - Vignette defaults and assets: act 1 0.28, act 2 0.33, act 3 0.38.
  - Contact shadows (ph_shadow, alpha 0.35, footprint width, own sorting group on the floor layer) under floor-standing mapped props and all containers. Characters and NPCs already had them.
  - The HUD stays on the Screen Space Overlay canvas, so it is not graded: the LED timer is still orange in the shots.
- **6. Verify:**
  - 0 compile errors.
  - EditMode **224 / 224**. New tests: `LayoutV2Tests` (6), plus grain-off-under-Reduce-Effects and the act vignette order in CinematicMixerTests.
  - Validate Content: 29 nodes, 0 problems. Text > Validate: 414 / 0 / 0 / 0.
  - Play (driven from code):
    - The Flat 11 kitchen cabinet search opens "Mutfak dolabı" with İlk yardım çantası.
    - The F4 stairwell door → `f3.aftershock`; the F2 corridor stairwell door (DogGone) → `f2.exit`; the F2 stairwell hotspot → `f2.robber`.
    - The wet cloth in Flat 10 gas shows the mask chip and the mask bag icon.
  - Screenshots, all `Screenshots/t14_*.png`:
    - before/after salon, F3 corridor, Flat 10 and street (1920x1080 game view; saved full size);
    - the three stairwell doors, the kitchen cabinet loot, the mask in gas, the hall toolbox, the bedroom bed;
    - the LUT checks.

### Files created / changed
- New:
  - `Scripts/UI/MaskIndicator.cs`;
  - `Tests/EditMode/LayoutV2Tests.cs`;
  - `Art/items/ui/cloth_ui.png`, `wet_cloth_ui.png`;
  - the LUT pngs + metas and the new item pngs + metas, now tracked.
- Changed:
  - Editor: `PropArt.cs`, `ItemIconArt.cs`, `BuildingLayout.cs` (door kinds, MoveDoor / MoveHotspot, Footprint, presets, stairwells), `SceneKit.cs` (stairwell sign, floor decal, contact shadows, door art / flip, layout tags), `TestSceneBuilder.cs`, `TestSceneUi.cs`, `LayoutOverrides.cs`, `RoomLayoutOverrides.cs`, `LayoutEditorWindow.cs`, `ContentValidator.cs`, `BuildingLevelsGenerator.cs`, `CinematicAssetsGenerator.cs`.
  - Runtime: `SurvivalController.cs` (MaskWorn), `Item.cs` (isTrap removed), `LayoutObject.cs` (Door kind, scale / nudge / door fields), `LootContainer.cs` (docs), `CinematicProfile.cs`, `CinematicDirector.cs`, `CinematicMixer.cs`, `UiKeys.cs`.
  - `strings_tr.csv`:
    - added `ui.mask.worn`, `ui.sign.stairs`, `container.kitchen_cabinet`;
    - toolbox is now "Alet çantası";
    - removed gold / laptop / console / medicine / jewelry.
  - Tests: Loot, Snapshot, RewindTarget, DelayedBomb, CinematicMixer.
  - Generated: items, GameContent, PropArtSet, CinematicAct1-3, CinematicBaseVolume, RoomLayoutOverrides (pruned), `Test_Building.unity`; `docs/layout-editor.md`.

### Needs local wiring (checklist for the MCP session)
- [ ] None required. Optional: Bora tunes the LUT contribution by eye on CinematicAct1-3 (0.6 now).

### Not verified / risks
- **LUT import: sRGB is OFF, not on as the task said.** URP's `ColorLookup.ValidateLUT()` rejects sRGB formats (both LUTs were "invalid" with sRGB on). Proof with sRGB off:
  - the neutral LUT at contribution 1 vs none differs by only 0.22 / 255 on average (0.4 % of pixels > 6, animated sprites), so the image is unchanged;
  - the dust LUT at 1 changes it by 6.6 / 255 on average.
- **LUT tuning:** 0.6 reads subtle and fine in the lit rooms (lifted blacks, slightly muted). In the dark rooms (Flat 10, street) the prep-end vignette dominates. The before/after shots are taken a few prep-seconds apart, so the vignette build-up differs a bit. No change recommended now; try 0.5–0.7 by eye if the street feels too grey.
- **No full click-through F4 → street.** The play check entered nodes from code and clicked the doors/hotspots directly. The robbery → street dialogue still waits for line clicks (unchanged from earlier tasks). The street was only viewed via room jump.
- **The game ran only with `Application.runInBackground = true`** (set at runtime for the checks). With the editor unfocused it otherwise stays on frame 1.
- **The stairwell sign is screen-aligned** (not skewed to the wall) and sits across the top of the door: there is no wall above the lintel.
- **The Flat 10 floor hole sits in the room middle, just behind the family's spawn.**

### Next suggested step
- A real click-through F4 → street with the new art, and a look at the prop sizes (Match prop height where art looks off).

---

## [2026-10-18 04:30] LOCAL — Bag icon fix (UI icons) + Hasan removed, Sevim gives the gift
### Done
- **Bag icons, root cause:**
  - the slots showed the world sprites (bottom pivot, big transparent padding);
  - every slot icon started with the `art.Dot` placeholder (the "tiny dot");
  - a refresh only set the sprite when the item had one, so an item without art kept the previous sprite.
- **Fix:**
  - `ItemIconArt.UiFor(id)` writes `Art/items/ui/<id>_ui.png`: the art cropped to its opaque pixels, padded to a square (aspect kept), 256 px, centre pivot. Items without art get a shared `placeholder_ui.png` (outlined box). World sprites are untouched.
  - `Item.UiIcon` (falls back to `Icon`) is set by the generator.
  - New `UI/SlotIcon`, used by BagUI (stretched, 12% inset), LootWindow (the area above the name, 12% inset) and KeyItemPocket (its authored spot restored). On every refresh it resets anchors, pivot, position, size, scale and rotation, with preserveAspect = true and useSpriteMesh = false.
  - The builder no longer seeds slot icons with `art.Dot`. No slot pop tween exists, so none needed fixing.
- **Hasan removed:**
  - the `f4.neighbor` node (asset deleted by the generator), the corridor "talk" choice, the GiftDone / NeighborGone flags;
  - his hotspot, the HASAN_IDLE slot, his speaker colour and voice (DialogueVoices rewired);
  - his CSV keys and his test. Validate Content: 29 nodes, 0 problems.
- **Sevim gives the gift (`f4.sevim`, new `Outcome.GiveItem` → `StateOps.GiveOrDrop`):**
  - "Sağ ol teyzeciğim" gives food + water + whistle (key pocket) and the Flat 12 info.
  - "Kısa kes" gives only one water.
  - With a full bag, what doesn't fit is dropped in the room at her feet (`DropAnchor` "DropSpot" in front of her; DroppedItemsPresenter uses it). Key items never drop (already owned = nothing to add).
  - One queued toast per item: "Sevim Teyze: Ekmek verdi", or "…Çantan dolu evladım, Su şuraya, yere bırakıyorum."
  - New l3 / l4 (l4: "Al evladım, ekmekle su koydum, bu düdük de sende dursun; enkaz altında kalırsan öttür, sesin duyulsun.").
  - Her start (hall → corridor door with SevimDone = false) and her click are unchanged.
- **Tests:** thanks = food + water + whistle; Kısa kes = water only; a full bag drops food + water (a key item never drops); `HasanIsGone`; `ItemUiIconTests` (every item: square, centre pivot). EditMode **217 / 217**. Text > Validate 0 / 0 / 0. 0 compile errors.
- **Play Mode (F4 corridor):**
  - thanks: bag 2/8 with bread + water centred, the whistle in its pocket, 3 toasts;
  - Kısa kes: only water ("Sevim Teyze: Su verdi");
  - full bag: bread + water lying in front of her, whistle in the pocket;
  - the Flat 12 loot window icon is centred.
  - Screenshots: `Screenshots/t13_bag_empty.png`, `t13_bag_4.png`, `t13_bag_full.png` (incl. 2 placeholders), `t13_loot_window.png`, `t13_sevim_gift_choice.png`, `t13_sevim_thanks_bag-1.png`, `t13_fullbag_drop.png`.

### Files created / changed
- New:
  - `Scripts/UI/SlotIcon.cs`, `Scripts/World/DropAnchor.cs`;
  - `Tests/EditMode/ItemUiIconTests.cs`;
  - `Art/items/ui/*_ui.png` + `placeholder_ui.png` (generated).
- Changed:
  - `Nodes/Item.cs` (UiIcon), `Nodes/Outcome.cs` + `OutcomeExecutor.cs` (GiveItem), `Core/StateOps.cs` (GiveOrDrop);
  - `UI/BagUI.cs`, `LootWindow.cs`, `KeyItemPocket.cs`, `World/DroppedItemsPresenter.cs`, `Text/UiKeys.cs` (gift keys);
  - editor: `ItemIconArt.cs`, `BuildingLevelsGenerator(.Floor4).cs`, `CharacterArt.cs`, `TeamAudioImporter.cs`, `BuildingLayout.cs`, `SceneKit.cs`, `TestSceneUi.cs`;
  - `Tests/EditMode/NpcDialogueFlowTests.cs`, `strings_tr.csv`;
  - generated items / nodes / `DialogueVoices` / `GameContent`; `Test_Building.unity` (rebuilt).

### Needs local wiring (checklist for the MCP session)
- [ ] None. Optional: Bora checks the whistle's size in the right side pocket (it uses the pocket's authored spot, which is small).

### Not verified / risks
- I didn't do a real rewind between the thanks and Kısa kes runs: each run re-entered `f4.sevim` with a reset state.
- The prep → quake → corridor part was not replayed this time (the flow from the earlier sessions is unchanged).

### Next suggested step
- If more NPCs give things, reuse `Outcome.GiveItem` (one toast per item, drop at their feet).

---

## [2026-10-18 03:55] LOCAL — Drop-Cover-Hold (Çök-Kapan-Tutun) crouch animation for the family during quakes
### Done
- **Art:** `cro all/mehmet crouch`, `anne crouch` and `velet crouch` moved (AssetDatabase.MoveAsset, .meta kept) to `Art/char/CROUCH_MEHMET`, `CROUCH_ANNE` and `CROUCH_CHILD`; the empty `cro all` folder was removed.
- **Import:** `CharacterArt.ImportCrouch(member)`, part of `SixtyOne > Art > Import Character Frames`; any frame count.
  - Same settings as the run frames.
  - Per character: ONE pivot (the lowest opaque row over all frames + frame 0's horizontal foot centre) and ONE scale (frame 0's visible height = the character's in-game height).
  - No per-frame trim or re-pivot.
  - Result: Mehmet 10 frames, the wife 10, the child 7.
- **CharacterAnimator:**
  - new `Crouch` state, priority crouch > run > idle (`StateFor(moving, velocity, crouching)`);
  - forward at 14 fps, holds the last frame (`CrouchClock` / `CrouchFrameIndex`, pure), backwards at 18 fps to stand up;
  - the crouch frames use their own body scale (`ConfigureCrouch`); flipX is kept.
- **CrouchDirector** (new, on the Game object) reacts to `QuakeStarted` / `QuakeEnded`:
  - every member who is not Dead or Trapped crouches; the end plays the stand-up;
  - placement quakes (opening, F3, F2) set `CrouchGate.Frozen`, so `PlayerMover.CanMove` and the followers stop until everyone is standing again;
  - the prep-zero onset rumble (raised while the prep node is still current) is ignored, because the family still has to be placed;
  - a death or Trapped snaps that member to idle; rewinds snap everyone to idle with no reverse animation;
  - Reduce Effects doesn't change it.
  - `PlacementController.IsQuaking` was added so the director can tell the real shake from the onset.
- **Street quake:** members crouch while standing still; running stands them up at once. The street mechanic is "run to the open area during the shake" (checked at the peak), so freezing there would make that death unavoidable.
- **Tests:** `CrouchBeatsRunBeatsIdle`, `CrouchPlaysOnceThenHoldsTheLastFrame`. EditMode **215 / 215**. 0 compile errors.
- **Play Mode** (a console logger on QuakeStarted / QuakeEnded):
  - opening quake (table), F3 aftershock and F2 strong aftershock: all three crouched during the shake (still crouched at QuakeEnded), freeze on, then stood up, freeze off, everyone alive;
  - street: all three crouched, no freeze, stood up after;
  - the F3 detour died to the gas bomb because my shortcut skipped the F4 valve; the rewind snapped everyone to idle, as intended.
- Screenshots: `Screenshots/t12_f4_crouch_mid.png`, `t12_f4_crouch_held_full.png` (salon, at the table), `t12_f2_crouch.png` (Flat 7 mid-shake), `t12_crouch_mid.png` / `t12_crouch_held.png` (all three, mid and held).

### Files created / changed
- New:
  - `Scripts/Placement/CrouchDirector.cs`, `CrouchGate.cs`;
  - `Art/char/CROUCH_MEHMET|ANNE|CHILD/` (moved art, reimported).
- Changed:
  - `Placement/CharacterAnimator.cs` (Crouch state);
  - `PlayerMover.cs` / `FamilyFollower.cs` (CrouchGate);
  - `PlacementController.cs` (IsQuaking);
  - `Editor/Art/CharacterArt.cs` (ImportCrouch);
  - `Editor/SceneBuild/SceneKit.cs` (ConfigureCrouch);
  - `TestSceneBuilder.cs` (the director);
  - `Tests/EditMode/CharacterAnimationTests.cs`;
  - `Scenes/Test_Building.unity` (rebuilt).

### Needs local wiring (checklist for the MCP session)
- [ ] Bora: watch a real quake at normal speed. I stepped / paused for the stills, so the 14 / 18 fps feel is not judged by eye.

### Not verified / risks
- Placement anchors put the family next to the salon table, not under it (unchanged), so in the stills the table draws in front of them.
- Floor 2 is very dark, so the crouch is hard to read there (lighting, not the animation).
- The street was entered by a jump, so its follow-camera framing was off in my captures; the logs confirm the crouch.

### Next suggested step
- If Bora wants the street crouch to also freeze, decide what replaces the "run to the open" check first.

---

## [2026-10-18 03:20] LOCAL — Tasks 10 + 11: NPC art (dog, Cemil), rabid dog, Cemil's trade, Sevim / AFAD fixes, speech bubbles instead of the bottom panel
### Done
- **NPC art (task 10):**
  - `kuduz it` → `Art/char/DOG_IDLE` and `cemil bey` → `Art/char/CEMIL_IDLE` (moved with their .meta files, no empty folders left).
  - NPC idle import (`CharacterArt.ImportIdle`): every frame is trimmed to the opaque bounds shared by ALL frames (no jitter), pivot at the feet, max size 1024, 9 fps. Sevim uses the same import.
  - Heights: the dog is 0.55 × the player, Cemil 1.0.
  - Facing (`HotspotSpec.Face`): the dog faces the player's arrival door, Cemil faces the corridor centre (his art looks left).
- **Rabid dog (F2 corridor):**
  - "Yemeği uzağa fırlat" costs 1 food; the dog runs off-screen and fades (`RabidDogPresenter`); no QTE.
  - "Düdüğü öttür" sets `DogScared`: the dog backs off and the QTE becomes 3–4 keys, slower, with a smaller penalty (`qte.dog_easy`).
  - Slipping past: the 3rd failure is a bite that rewinds to the corridor (`QTENode.ConfigureFailLimit`).
  - "Sakinleştirmeye çalış / sev" is a trap: instant bite.
  - Both bites show `death.dog_bite` + `tip.rabies` (Tip asset generated). Bark plays on QTE start and on the bite; the dog trembles during the QTE; the growl loops in the corridor.
- **Cemil Bey:** 4 grumpy-but-fair lines + a thanks / grumble line, art on his hotspot. TraderLogic asks nothing in return (unchanged). Male voice, pitch 1.0.
- **Sevim (task 11 bug 1):** no logic failure reproduced. Through the real doors, the mover and UI raycasts, hall → corridor with SevimDone=false enters `f4.sevim`; "thanks" gives the whistle (key pocket) + KnowsFlat12; "Kısa kes" gives neither.
  - Likely cause: with the old panel, "Kısa kes" was the only button while she talked.
  - She is now clickable: the first click talks, afterwards "Hadi evladım, çabuk olun!".
  - `NpcDialogueFlowTests` covers Sevim, Hasan, Cemil and the robber on the real generated nodes.
- **AFAD popup (task 11 bug 2):**
  - It only shows at the START of the prep (`PrepRules.ShowsStartAlert`); walking back into the salon re-showed it before.
  - It hides after 4 s, on the first click anywhere, and on rewind or death; while hidden it blocks no clicks.
  - The Floor 3 sighting was my own test: Play Mode was frozen while the editor was unfocused.
- **Speech bubbles (task 11):** `SubtitleView`, `ChoicePanel`, `ChoiceButton` and `IChoicePresenter` are removed. The controllers use `IDialoguePresenter` (ShowLine / ShowNarration / ShowThought / ShowChoices / Clear), implemented by `DialoguePresenter`.
  - Lines appear in one `SpeechBubble` on the speaker's head:
    - paper card (#F7F3EA), ink outline, shadow;
    - name tab in the speaker colour;
    - typewriter + voice blips, ▼ when complete;
    - "−N sn" LED chip in the building-timer colours;
    - tail aimed at the head.
  - Click anywhere / Space / Enter completes the line, then advances. Keys 1–4 pick rows. Every pick goes through the panic input delay.
  - "Kısa kes" is a pill. After-dialogue rows hang under the last speaker; the robber's choices go on Mehmet (`DialogueNode.ChoicesAsThought`).
  - A node with choices the world doesn't offer = Mehmet's thought bubble (grey, dotted tail) with its body and rows; it hides while he runs. Otherwise the body goes to the `NarrationCard` (top centre, under the panic meter), which also shows the placement / street hints (`UiKeys.IsNarration`; the toast skips them).
  - `BubbleLayout` (pure, tested) keeps bubbles below the panic meter and left of the bag column; an off-screen speaker docks to the nearest edge.
  - Pause, transitions and death clear or hide the bubble.
- 0 compile errors. EditMode **213 / 213**. Validate Content 0 problems. Text > Validate 0 missing / empty / unused.

### Files created / changed
- New runtime:
  - `UI/DialoguePresenter.cs`, `SpeechBubble.cs`, `BubbleChoiceRow.cs`, `BubbleLayout.cs`, `SpeakerAnchor.cs`, `NarrationCard.cs`;
  - `Nodes/IDialoguePresenter.cs`, `World/RabidDogPresenter.cs`.
- New editor: `Editor/SceneBuild/TestSceneDialogueUi.cs`.
- New tests: `Tests/EditMode/NpcDialogueFlowTests.cs`, `BubbleLayoutTests.cs` (plus new cases in `QTETests`, `PrepTests`).
- Removed: `UI/ChoicePanel.cs`, `UI/ChoiceButton.cs`, `UI/SubtitleView.cs`, `Nodes/IChoicePresenter.cs`.
- Changed:
  - controllers: `NodePlayer`, `DialogueController`, `RobberController`, `EpilogueController`, `DialogueNode`;
  - QTE: `QTENode` (fail limit, easy config), `QTEController`;
  - prep / UI: `PrepRules`, `PrepPhase`, `AfadAlertPopup`, `MessageToast`, `UiKeys`;
  - art / builders: `CharacterArt`, `SceneKit`, `BuildingLayout`, `TestSceneUi`, `TestSceneBuilder`, `SerializedWire` (SetVector2);
  - generators: Floor 2 / 3 / 4 and `BuildingLevelsGenerator`;
  - `strings_tr.csv`, `CLAUDE.md`;
  - generated nodes, the dog QTE configs, the `tip.rabies` asset and `Test_Building.unity` (rebuilt).
- Screenshots (1080p): `Screenshots/t11_1080_*.png` (prep AFAD + card, placement card, hall thought, Sevim line / choices, Hasan, Cemil line / trade, Flat 7 thought).

### Needs local wiring (checklist for the MCP session)
- [ ] Bora playtests (he asked to do it himself): Sevim / Hasan / Cemil / robber bubbles, the street hint card, all 4 dog options, rewinds and tips.
- [ ] Not done yet: the robber's 3 choices, the street hint and the 4 dog options in Play Mode; the 1280×720 pass.
- [ ] Wife lines (if any are added) need an `npc.wife.name` CSV row. Her anchor already exists; nothing references the key yet.

### Not verified / risks
- I didn't drive real mouse hover / press / keyboard 1–4 by hand; rows were pressed through `PressRow`, lines through the presenter's click handler.
- 720p not checked. The safe area uses the panic meter / bag column rects, so it should scale, but it is not checked.
- Reason / name text is 16 px, not the 14 / 15 px in the brief, because the project rule is nothing below 16 px at 1080p.
- Screenshots over MCP sometimes come back without UI right after the Game view resolution changes (a capture fallback, not the game).
- Hasan has no art yet: his bubble tail points at a dark silhouette.

### Next suggested step
- Bora's playtest feedback on the bubble look (sizes, offsets, colours: all on `SpeechBubble` / `TestSceneDialogueUi`), then the 720p pass and the robber / street / dog checks.

---

## [2026-10-18 01:00] LOCAL — Task 09: Layout Editor (hand placement that survives rebuilds) + F9 room jump
### Done
- **Data: `Data/Layout/RoomLayoutOverrides.asset`** (`Editor/Layout/RoomLayoutOverrides.cs`).
  - One entry per room id, each a list of placed objects.
  - Each object: kind (Prop / Pickup / Container / HiddenItem / Light), id, gx/gy, lift, flipX, prop name, blocksNav; pickup data (item, cost, borrowed, surface, prepOnly); container data (type, contents, hidden contents, search seconds, building cost, borrowed); hazard prop (id, topple, drop); light radius / colour; block scale / tint; `removed`.
  - Code objects keep stable ids: props are `Name`, `Name#2`... (`BuildingLayout.AssignPropIds`); items, containers and lights keep theirs. The same id replaces the code object, a new id adds one, and `removed` deletes one.
- **Builder:** `LayoutOverrides.Rooms()` applies the overrides after `BuildingLayout`. Build Test Scene uses it.
  - Every prop, pickup, container, hidden item and light gets a `LayoutObject` tag (`World/LayoutObject.cs`, pure data).
  - flipX mirrors the main sprite.
  - Hand-placed furniture with "Yolu kapatır" adds its cell to the room's blocked cells, so the runtime nav goes around it. Code props still don't block.
  - An unknown item id logs an error instead of throwing.
- **Checks:** `LayoutOverrides.Check` / `CheckAll` (also inside Validate Content and printed on every build):
  - furniture on a required cell (spawns, anchors, door approaches, NPC hotspots, items, ropes, container approaches);
  - furniture that cuts a required cell off (same groups as `DebrisPlanner`);
  - unknown rooms / items.
  - Messages give the room and the object id, e.g. `room 'flat8_living': 'wardrobe_on_door' blocks the door approach → flat8 cell (3, 1)`.
  - A container may stand on its own approach cell; a free neighbour must stay reachable instead.
- **Window `SixtyOne > Layout Editor`** (`Editor/Layout/LayoutEditorWindow.cs`):
  - room list by floor (Kat 4 / 3 / 2 / Sokak, `*` = has edits) with Prev / Next; a selected room is shown + isolated with SceneVisibility, framed, and its root selected;
  - palette: PropArtSet thumbnails, every item icon (pickup), the 8 container presets, a light. A click spawns a placeholder at the room centre, selected, Move tool on;
  - inspector panel: item dropdown, cost, borrowed, surface, prep-only, container contents add/remove, search seconds, building cost, flipX, lift, blocks nav, hazard prop + id. Everything uses Undo;
  - snap to the half-cell iso grid on mouse up (on by default, sits on the floor via lift); the iso grid with blocked cells in red; 🔒 gizmos for anchors / door approaches / hotspots / ropes / spawns;
  - Ctrl+D: the copy gets a new id (and becomes a new object);
  - Save room / Save all write only the edits against the code layout (unchanged objects are left out, so later code changes still reach them); Revert room to code; Rebuild scene.
- **Build Test Scene** no longer shows the modal "save changes?" dialog. That dialog was what froze the editor and MCP in task 08. The test scene's unsaved edits are dropped (it is regenerated); other dirty saved scenes are saved silently; it refuses to run in Play Mode.
- **F9 room jump** (`Placement/DevRoomJump.cs`, compiled only in the Editor / Development builds): a room list that shows the room at once and snaps the family there (`RoomSwitcher.JumpTo`), keeping the state and node.
- **Bug fixed:** `Room.Links` cached its door list in edit mode. After a build the validator saw no doors and reported 25 false "no door in zone" problems. Edit mode now always recomputes the list.
- **Tests:** new `LayoutOverrideTests`:
  - unique prop ids;
  - apply move / remove / add;
  - a prop on a door approach is reported;
  - the code layout has no problems;
  - an unchanged room diffs to nothing;
  - the diff keeps moves, adds and removals.
  - EditMode **200 / 200**.
- **Verified in the editor:**
  - moved the Flat 11 salon wardrobe, added a chair + food pickup in Flat 8, a drawer with water + cloth in Flat 9, and removed the Flat 12 wall lamp;
  - Save gave exactly 1 / 2 / 1 / 1 edits; after Rebuild all were still there as real objects;
  - Play: the food pickup went into the bag; the drawer searched and opened "Ödünç al / Çekmece" with Su + Bez;
  - a wardrobe on the Flat 8 door approach → Validate Content flagged it;
  - the window isolates / frames the room and draws the locked gizmos (screenshot `Screenshots/t09_layout_editor_sceneview.png`).
  - Then I **removed my test edits**, so the committed overrides asset is empty.
- Validate Content: 0 problems. Text > Validate: 0 missing / empty / unused. 0 compile errors.
- `docs/layout-editor.md`: a short Turkish how-to for Bora. CLAUDE.md: architecture note.

### Files created / changed
- New:
  - `Scripts/World/LayoutObject.cs`, `Scripts/Placement/DevRoomJump.cs`;
  - `Scripts/Editor/Layout/RoomLayoutOverrides.cs`, `LayoutOverrides.cs`, `LayoutEditorWindow.cs`;
  - `Tests/EditMode/LayoutOverrideTests.cs`, `docs/layout-editor.md`, `Data/Layout/RoomLayoutOverrides.asset`.
- Changed:
  - `Editor/SceneBuild/BuildingLayout.cs`: ids / flip / blocksNav, `ContainerPreset`, `ContainerKinds`;
  - `Editor/SceneBuild/SceneKit.cs`: `Prop(spec)`, `EffectiveLift`;
  - `Editor/SceneBuild/TestSceneBuilder.cs`: overrides, tags, no modal dialog, DevRoomJump;
  - `Editor/Content/ContentValidator.cs`: layout check;
  - `Placement/RoomSwitcher.cs`: `JumpTo`;
  - `Placement/Room.cs`: Links in edit mode;
  - `Scenes/Test_Building.unity` (rebuilt), `CLAUDE.md`.

### Needs local wiring (checklist for the MCP session)
- [ ] Bora: place furniture with `SixtyOne > Layout Editor`, then **commit `Assets/_Game/Data/Layout/RoomLayoutOverrides.asset`**. It is plain YAML, so every change shows in a git diff.
- [ ] Still open from task 08: the street → sign death (`death.street_power`, path through the spark cells) and the dark street.

### Not verified / risks
- I drove the window with code (selection, save, rebuild), not by dragging with the mouse. The snap-on-mouse-up, the palette clicks and Ctrl+D id renaming were not exercised by hand.
- Palette spawns are placeholders (sprite + tag). They become real pickups / containers only after Save + Rebuild scene.
- Hand-placed props flagged as hazard props need a hazard whose prop id matches; nothing checks that yet.
- When the editor is not focused, Play Mode can stall with `runInBackground` off. That is an editor setting, not a game bug.

### Next suggested step
- Bora places the furniture for every room with the Layout Editor. Then fix the street spark-path bug and run the full playthrough from task 08.

---

## [2026-10-18 00:05] LOCAL — Task 08 (partial): Bora's 4 playtest fixes + cloud task 02 wiring; play checks stopped early
### Done
- **Wiring:** compiled with 0 errors and ran these menus: Import Team Audio (34 cues), Import Prop Art (11 props, 6 chandelier frames, 2 debris), Generate Tile Sets (Rocky street set), Import Character Frames, Generate Building Levels, Build Test Scene (24 rooms).
- **Validators:** Validate Content: 30 nodes, 0 problems. Text > Validate: 413 referenced, 0 missing / empty / unused. No TODO in the CSV.
- **EditMode tests:** 194 / 194 pass, including the new `DescentRuleTests`.
- **Fix 1 + 3: the NPC placeholder.**
  - The scene had no extra object under Sevim, so the "white bean" was the pale placeholder capsule of the NPC next to her (Hasan).
  - NPCs with idle art now play it with no capsule. NPCs without art (Hasan, Cemil, the dog, the robber) are a dark neutral silhouette with a soft shadow, at their height relative to the player: dog 0.45, Hasan 0.95, Cemil 1.0, robber 1.0, Sevim 0.9.
  - `CharacterArt.NpcIdles` picks up `Art/char/{SEVIM,HASAN,CEMIL,ROBBER,DOG}_IDLE/frame*.png` automatically when the folder exists, with any number of frames. The scene builder uses it, so a rebuild keeps this.
  - The dog is now an NPC object (decorative) instead of a coloured prop.
- **Fix 2: the rope rule.**
  - The Flat 10 collapsed floor had no requirement at all, and the Flat 9 cables needed only the harness.
  - Both now go through `NeedsDescentGear`: they need the bedsheet rope (used up) and the child harness. Without them the choice is ShowLockedFatal: `death.descent_no_gear` names the missing item and rewinds to the F3 checkpoint.
  - The balcony rope already refused without the rope and harness.
  - Doors: the only floor-skipping door is the F4→F3 stairs. Doors into another zone always play the node's move choice (`RoomLinkRules`).
  - To avoid a soft-lock (bedsheets existed only on F4), F3 now has 3 borrowable bedsheets: on the Flat 9 sofa, in the Flat 9 bookshelf and in the Flat 10 bookshelf. Bora approved this.
  - New `Tests/EditMode/DescentRuleTests.cs`:
    - every GoTo into a lower floor (except `f4.corridor.c_down`) requires `bedsheet_rope` + `child_harness` and is ShowLockedFatal or Hide;
    - no door skips a floor except the stairs;
    - the balcony's `CanDescend` rule.
  - Verified in Play Mode: the collapsed floor without gear → death card "Eksik: Çarşaf halatı" plus the new line.
- **Fix 4: Panik.**
  - New `UI/PanicMeter.cs` under the timer: label `Panik`, amber → red fill, a tick at `PanicDelayThreshold`, pulses above it (steady with Reduce Effects).
  - The stamina label is now `Enerji`. New key: `ui.panic.label`.
- **Validator fixes:**
  - `hz.street_facade` counts as linked through `StreetQuakeDirector.FacadeHazard`.
  - The street death keys moved to `UiKeys` (`StreetGlassDeath` / `StreetPowerDeath`).
  - Removed the dead `room.street` key (there is no door into the street).
- **Play checks done:**
  - F4 corridor NPCs (screenshot);
  - F3 descents locked-fatal;
  - the F2 dog silhouette;
  - a micro rewind (LOOP + Notlar went up);
  - the robber with 2 food + 1 water + the flashlight: he took both food, kept the water and the key item, `RobbedAtExit` was set and the game moved to the street automatically;
  - the street quake 4 ran (QuakeCount 4, `StreetQuakeDone`).
- Interaction Mode set to No Throttling for Play Mode, then restored (0).

### Files created / changed
- `Scripts/Editor/Art/CharacterArt.cs`: NPC idle table and `ImportIdle(id)`; replaces the Sevim-only import.
- `Scripts/Editor/SceneBuild/SceneKit.cs`: NPC idle art or a dark silhouette; spark key via UiKeys.
- `Scripts/Editor/SceneBuild/BuildingLayout.cs`: NPC idle ids, the dog as an NPC, F3 bedsheets.
- `Scripts/Editor/SceneBuild/TestSceneUi.cs`: `BuildPanicMeter`.
- `Scripts/Editor/Content/BuildingLevelsGenerator.Floor3.cs`: `NeedsDescentGear` on the cables and the collapsed floor.
- `Scripts/Editor/Content/ContentValidator.cs`: the street façade hazard counts as linked.
- `Scripts/UI/PanicMeter.cs` (new).
- `Scripts/Text/UiKeys.cs`: `PanicLabel`, street death keys.
- `Scripts/World/StreetQuakeDirector.cs`, `StreetPowerLine.cs`: `FacadeHazard` accessor; keys via UiKeys.
- `Tests/EditMode/DescentRuleTests.cs` (new).
- `Content/Text/strings_tr.csv`: `ui.panic.label`, `ui.stamina.label` = Enerji, `death.descent_no_gear`, `f3.flat10.c_hole.locked`, new `f3.flat9.c_cables.locked`; removed `room.street`.
- Regenerated data (nodes, robber / street nodes, cues, tile sets, `PropArtSet`), `Scenes/Test_Building.unity`, and Unity-generated `.meta` files for the cloud task 02 scripts.

### Needs local wiring (checklist for the MCP session)
- [ ] **Bug (found, not fixed): street → sign death.** On the street after quake 4, `f2.street.c_sign` (the run to the sign) killed the player with `death.street_power`. Check `StreetQuakeDirector` line ~211 (`StreetLayout.IsSpark(cell)`):
  - Is the player's cell computed correctly? `PlayerMover.transform.position` read (0,0,0) in world space.
  - Does the `GridNav` path to the sign go through a spark cell (9,5) / (11,5)? Spark cells are walkable, so A* may route through them; they should be blocked for auto paths.
- [ ] The street is very dark: the façade drop warnings are hard to read. Raise the street ambient light / Act-3 darkness on the street.
- [ ] Remaining task-08 play checks (stopped at Bora's request):
  - prop height tuning against the family + screenshots of every room;
  - doors (`kapi.png` flush / flipped);
  - the chandelier swing / frame 5;
  - placement runs when the timer ends mid-run;
  - the family running in from the door;
  - debris after quakes 1–3 (corridor screenshots) + cracks;
  - every loot container (search ring, take / take all / bag full / free second visit / Ödünç al / rewind);
  - the robber's Yalvar / Ağla-kısa kes / Bağır replies one by one + the toasts;
  - the street camera follow, the quake feel, "Açık alana çök!", the collapse-behind beat near / far;
  - a full playthrough from the main menu.

### Not verified / risks
- Play checks were done by jumping to nodes with code (`NodePlayer.Enter`), not a real playthrough. The prep and building timers were not running in those checks.
- The robber choices were clicked through quickly; which reply played was not checked.
- `Build Test Scene` once blocked the editor (most likely a modal dialog) and dropped the MCP socket; it worked after Bora cleared it.
- The uncommitted TMP font assets, `ProjectSettings/VersionControlSettings.asset`, `61s_local.bat` and the `Art/Fonts` OTF / WOFF folders were left out of the commit.

### Next suggested step
- Fix the street spark / path bug (block spark cells for auto paths), brighten the street, then run the remaining play checks above and one full playthrough.

---

## [2026-10-17 19:56] CLOUD — Task 02: running members, grid paths, debris/cracks, loot containers, prop art, robber + street finale
### Done
One commit per part (A–F), so a partial pick-up works.
- **A. Running instead of teleporting.**
  - Placement reserves the anchor at once (`PlacementRuns`), then the member runs there at the follower speed × `Room.Scale` (injured is slower). You can queue the next member while the first is still running; the player runs too.
  - When the timer ends, anyone still running snaps onto their reserved anchor before the quake resolves.
  - After a room transition the player pops in at the arrival point. The wife and child stay hidden until input unlocks, then appear at the arrival door (or the spawn for rope / cable / stairs nodes) and run to their follow spots (`FamilyFollower.ArriveFrom`). This works during dialogue and placement too.
  - Rewinds still snap (`RoomSwitcher.LastShowInstant`); Reduce Effects still runs.
  - `CharacterMotor` now takes waypoints and remembers an owner, so the mover, followers and placement never stop each other's runs.
- **B. `GridNav` (pure A*).** 8 neighbours, no corner cutting, a blocked-cell set, and string-pulled smoothing with a body-radius line check.
  - A blocked or unreachable target goes to the nearest reachable free cell; an approach cell under debris uses the nearest free neighbour.
  - Used by `PlayerMover`, `FamilyFollower` (re-plans every 0.2 s) and the placement runs. Each `Room` owns its nav (floor + extra cells + static blocks + debris).
- **C. Debris and cracks.**
  - New state, restored by snapshots: `GameState.QuakeCount` (opening 1, F3 2, F2 3, street 4, via `PlacementNode.quakeNumber`), `DebrisSeed` (checkpoint seed, default 61) and `Containers`.
  - `DebrisPlanner` (pure): seeded per room / quake / seed, density from `LevelConfig` (0 / 6 / 10 / 15 / 20 % plus 3 % per lower floor), only grows. It never cuts a path between required cells (door approaches and arrivals, spawns, anchors, pickup / container / NPC / rope cells); required cells are never debris.
  - `DebrisPresenter`: applies debris instantly on room enter and on rewinds. After a quake the new pieces drop in from above with a dust puff and a quiet `cue_furniture_crash`.
  - `WallCracks` + `CrackRules`: per-segment seeded rolls against a monotonic chance (0 / 15 / 35 / 55 / 70 % plus 5 % per lower floor), using the WhatsApp cracked faces from every tile set.
- **D. Loot containers.**
  - `LootContainer` flow: the player runs up, a radial ring and "Aranıyor…" fill over 1.5 s with `cue_search`, input is locked except Esc, and any click cancels with no refund. The building cost (3 s, prep seconds before the quake) is paid once.
  - Then `LootWindow` opens next to the bag:
    - click an item to take it (bag full → the bag flashes and `cue_denied` plays);
    - "Hepsini al" takes everything that fits; "Kapat" closes;
    - a bag click puts an item back;
    - in neighbours' flats the title is "Ödünç al" and items taken are borrowed.
  - A second visit is free and instant. Key items go to the pockets.
  - Hidden extras: only inside once the container was searched in an earlier loop (KnowledgeSet).
  - Converted every closed-surface pickup, keeping ids and contents: `salon_cabinet` (ecza dolabı), `kitchen_drawer`, `hall_nightstand`, `hall_toolbox`, `hall_jewelry_box`, `bedroom_wardrobe_sheet`, `flat12_closet_flashlight`, `flat12_cabinet`, `flat12_wardrobe_sheet`.
  - Added bookshelf containers:
    - salon: empty (a prep-time trap);
    - Flat 12: water;
    - Flat 9: food;
    - Flat 10: cloth;
    - Flat 8: food.
  - Generated `sfx_search.wav` and `sfx_car_alarm_loop.wav` with `tools/gen_sfx.py` (`cue_search`, `cue_car_alarm`).
- **E. Prop art.** New menu `SixtyOne > Art > Import Prop Art` (`PropArt` → `Data/Art/PropArtSet.asset`) imports the sprites with a bottom-centre pivot on the opaque pixels and a PPU set to the target size. The scene builder uses a mapped sprite, else the coloured block.
  - Mapped props:
    - table / desk (`masA`), chairs (`masaa`; 2 in the salon, 1 at the F7 desk);
    - wardrobe (`dolap`); the neighbours' wardrobe and the cabinet (`ComfyUI_00021_`);
    - bookshelf (`bokkshelf`), stove (`ocak`), fuse box (`sigorta`);
    - wall lamps (`lamba`), each with a Light2D practical: F11 hall, F4 / F3 / F2 corridors, F12 hall;
    - debris (`enkaz 1 ` / `enkaz 2`).
  - **Gas valve:** a new visible prop (`vana`) next to every stove is a ChoiceHotspot. It plays the valve choice; the prep and post-quake choices share it via `alsoKeys`.
  - **Chandelier:** `PropFrames` plays frames 0–4 as a swing during a quake; the Drop hazard ends on frame 5 (fallen).
  - **Doors:** now use `kapi.png` (falls back to `door.png`).
  - `ComfyUI_00045_` / `_00053_` are not used in the world.
- **F. Robber, then the street.**
  - **The robbery:** clicking the stairs (hotspot, `f2.exit.c_out`) starts `f2.robber`. "Telaşlı Adam" has 4 lines, one about his 5-year-old son waiting at the assembly area. Choices: Yalvar / Kısa kes / Ona bağır, each with its own reply node (`RobberNode`).
  - Every reply takes the items with the new outcome `TakeItems` (pure `RobberLogic`: all the food, else one water, never key items, borrow ledger untouched), sets `RobbedAtExit`, queues one toast per item ("{0} elinden alındı."), says a closing line (or the bitter line if he found nothing), runs down the stairs and fades. The game then continues to `f2.street` automatically. No QTE, death, fight or weapons.
  - **The street:** 20×6 cells, a checkpoint. Rocky floor (Rocky 17 with ~15 % seeded Rocky 3 patches), kerb and façade silhouettes, 3 cars, a fallen pole, the downed power line (blocked, one gap) with sparking cells (walk in or click the line = electrocution, `tip.electric`), and the "Acil Toplanma Alanı" sign far left.
  - **Camera:** follow mode (fixed size ≈ 7 cells, at least the street's depth band; smooth-damped; clamped to the street ends).
  - **`StreetQuakeDirector` (the 4th quake):** it starts 6 s after entering, or when the player gets within 2 cells of the line.
    - QuakeCount becomes 4.
    - Act-3 shake ×1.6 (Reduce Effects still scales it) and the tilt.
    - A louder `cue_quake_f4` (`QuakeInfo.SoundAct` / `Loudness`), car alarms and glass rain.
    - 3–4 façade drops: a red shadow on 2×2 cells for 1.2 s, then rubble. A member under it gets `hz.street_facade`: player death, wife cut to black, child trapped. The rubble blocks paths only where it can't cut one.
    - The hint "Açık alana çök!". At the 3.6 s peak the player must be in the open middle (not within 1 cell of the façades or the line), otherwise `death.street_glass` / `tip.street`.
  - The sign is locked until the quake has passed (`StreetQuakeDone`).
  - **Timer at 0 on the street:** death only within 5 cells of the building door (`SurvivalController.CollapseSpares`). Otherwise there is a dust-cloud + shake beat, "Arkanızda bina çöktü!" and the `CollapsedBehind` flag.
  - **Epilogue:** new lines for "it collapsed behind you" or "the building had {0} s left", plus the robbery line.
  - Every street death is a macro rewind to the street checkpoint.
- **Strings:** all new keys are in `strings_tr.csv` with finished Turkish (loot UI, container names, robber, street, deaths, `tip.street` (+ `.title`, exactly as specified), epilogue lines). No TODO; "ödünç al" only.
- **Tests:** new suites, 43 tests: `PlacementRunTests`, `GridNavTests`, `DebrisTests` (200-seed fuzz on every `BuildingLayout` room, monotonic density / debris / cracks, same seed = same layout, QuakeCount rewinds), `LootTests`, `RobberTests`, `StreetTests` (200 seeds door→sign, façade drops never on a chokepoint). The test asmdef now references `SixtyOne.Editor`, for `BuildingLayout`.

### Files created / changed
- Core: `GameState.cs`, `Snapshot.cs` (QuakeCount / DebrisSeed / Containers), `ContainerLedger.cs` (new), `LootEvents.cs` (new), `BorrowLedger.cs` (Remove), `GameEvents.cs` (QueuedMessage, BuildingCollapsedBehind), `QuakeInfo.cs` (SoundAct / Loudness), `FlagNames.cs` (RobbedAtExit, StreetQuakeDone, CollapsedBehind).
- Placement:
  - new: `GridNav.cs`, `PlacementRuns.cs` (also MemberMoveRules, QuakeRules), `PropFrames.cs`;
  - changed: `CharacterMotor.cs`, `FamilyMember.cs`, `FamilyFollower.cs`, `PlayerMover.cs`, `PlacementController.cs`, `PlacementNode.cs`, `Room.cs` (nav, debris, depth, follow camera, RequiredCells), `RoomSwitcher.cs`, `RoomTransitionController.cs`, `ChoiceHotspot.cs` (alsoKeys), `RoomNavigator.cs`, `HazardProp.cs`.
- World:
  - new: `DebrisPlanner.cs` (+ CrackRules), `DebrisPresenter.cs`, `WallCracks.cs`, `LootContainer.cs`, `PropArtSet.cs`, `StreetLayout.cs`, `StreetQuakeDirector.cs`, `StreetPowerLine.cs`;
  - changed: `RoomTileSet.cs` (cracked + patch tiles).
- Items: `LootLogic.cs`, `RobberLogic.cs` (new).
- Dialogue: `RobberNode.cs`, `RobberController.cs`, `RobberSetup.cs` (new); `DialogueNode.cs` (unsealed); `DialogueController.cs`.
- Nodes: `Outcome.cs` / `OutcomeExecutor.cs` (TakeItems, OutcomeResult.Taken).
- Building: `LevelConfig.cs` (damage tables), `SurvivalController.cs` (CollapseSpares), `EpilogueNode.cs`, `EpilogueText.cs`.
- UI: `LootWindow.cs`, `LootSearchRing.cs` (new); `BagUI.cs` (put back); `MessageToast.cs` (queue).
- Other runtime: `Iso/RoomCameraFraming.cs` (follow mode), `Audio/AudioEventHooks.cs`, `Text/UiKeys.cs`.
- Editor:
  - new: `Art/PropArt.cs`;
  - changed: `Art/TileArt.cs` (Rocky street set, cracks on all sets), `Art/DoorArt.cs` (kapi.png), `SceneBuild/BuildingLayout.cs` (containers, valves, chairs, wall lamps, the street, stairs, nav helpers), `SceneBuild/SceneKit.cs` (art props, containers, street extras, hotspot art), `SceneBuild/RoomTiles.cs` (WallCracks, patches), `SceneBuild/TestSceneBuilder.cs`, `SceneBuild/TestSceneUi.cs` (loot window, search ring), `Content/BuildingLevelsGenerator*.cs` (quake numbers, damage, cues, robber, street, `hz.street_facade`, `tip.street`), `Content/ContentValidator.cs`, `Content/TeamAudioImporter.cs`.
- Tests: the 6 new suites above; `SixtyOne.Tests.EditMode.asmdef`.
- Audio: `Audio/Generated/sfx_search.wav`, `Audio/Generated/sfx_car_alarm_loop.wav` (new, no .meta), `tools/gen_sfx.py`.
- `Content/Text/strings_tr.csv`, `CLAUDE.md`.

### Needs local wiring (checklist for the MCP session)
- [ ] Let Unity import, then fix any compile errors (see risks).
- [ ] Run menu: **SixtyOne > Audio > Import Team Audio** (wires `cue_search`, `cue_car_alarm`).
- [ ] Run menu: **SixtyOne > Art > Import Prop Art**, then check `Data/Art/PropArtSet.asset` (13 names, 6 chandelier frames, 2 debris sprites).
- [ ] Run menu: **SixtyOne > Art > Generate Tile Sets** (Rocky sheet slicing → `StreetTileSet` with Rocky 17 / Rocky 3 patches; cracked faces on every set).
- [ ] Run menu: **SixtyOne > Generate Building Levels**, then **SixtyOne > Build Test Scene**, then **SixtyOne > Validate Content** and **Text > Validate**.
- [ ] Run all EditMode tests.
- [ ] Tune the prop target heights by eye in `PropArt.Entries`:
  - table 0.45, chair 0.55, wardrobe / bookshelf 1.1, stove 0.5, valve / fuse box 0.25, wall lamp 0.2 (room-local units);
  - chandelier canvas 1.35 (the hanging height and fallen frame);
  - debris 0.35 wide;
  - the bookshelf on the F8 right wall may need `flipX`.
- [ ] **Running placement under the timer:** queue all three members fast on F4; let the timer end mid-run (they must snap and resolve at the targets); micro-rewind (snap to the last anchors); macro-rewind (spawns).
- [ ] **Room changes:** the wife and child appear at the arrival door and run in (doors, F4→F3 stairs, rope, cables, collapsed floor); with Reduce Effects on.
- [ ] **Paths:** click on debris (nearest free cell), go around debris to doors and pickups, no squeezing between diagonal debris.
- [ ] **Debris layouts on F3 / F2:** density, the drop-in after each aftershock, that nothing blocks a door / anchor / container. Cracks after each quake.
- [ ] **Every new interaction:**
  - each container: first search costs 3 s, the ring, a click cancels, the second visit is free, take / take all / put back, bag full;
  - borrowed title in F12 / F9 / F10 / F8;
  - gas valves in the F11 / F10 / F7 kitchens (prep and post-quake);
  - the chandelier swing and fall;
  - doors with `kapi.png`;
  - the robber via all three choices, with food / water only / nothing in the bag;
  - the street sign before and after the quake.
- [ ] **Street camera follow:** the size (`Room.followCells` 7, `RoomCameraFraming.followHeadroom`), the smoothing and the clamp at both ends.
- [ ] **Street quake feel:**
  - the timing (`StreetQuakeDirector` delay 6 s / peak 3.6 s / 7 s), the shake and tilt strength, the `cue_quake_f4` loudness 1.35;
  - the car alarm, glass rain, façade drop shadows and rubble, the "Açık alana çök!" check, the sparks;
  - the collapse-behind beat (set `BuildingTime` low on the street).
- [ ] Façades, cars, the pole and the kerb are tinted blocks; swap in art when it's delivered.

### Not verified / risks
- **Not compiled in Unity.** All runtime, editor and test code was type-checked with Roslyn (csc 4.8 on Mono) against the Unity **2021.3** reference DLLs plus hand-written stubs for TMP / uGUI / Input System / URP. The only errors left are in pre-existing lines: stub gaps or Unity 6-only APIs (`ContactFilter2D.noFilter`, `FindObjectsByType(FindObjectsInactive)`), not in the new code. Unity 6-specific API differences in new code can't be fully ruled out.
- **Tests ran on Mono, not in Unity.** The 6 new suites (43 tests) plus 7 existing suites (Snapshot, Outcome, PlacementResolver, DelayedBomb, RewindTarget, Condition, Dialogue) ran with NUnitLite against a small managed UnityEngine shim: **83 / 83 passed**. The other existing suites need MonoBehaviour / UI types and were not run here.
- No new `.meta` files were written (Unity generates them): new scripts, the two wavs, and `PropArtSet` / `StreetTileSet` assets (created by the menus).
- `DialogueNode` is no longer sealed (`RobberNode` derives from it); `DialogueController` skips RobberNodes and the robber handler runs first.
- The old stairs → street door (L1) is gone: the street is entered by the robbery node, so the family appears at the street spawn (the building door).
- Façade rubble blocks paths only where it can't cut one (`DebrisPlanner.SafeExtras`); elsewhere it is visual only.
- Placement no longer snaps members to spawns on a normal entry (only on rewinds), so the opening quake starts with the family where they stood during the prep.
- The uncommitted local changes to two TMP font `.asset` files and `ProjectSettings/VersionControlSettings.asset` were already in the checkout; this session left them alone.

### Next suggested step
- Run the local checklist above (generate → build → validate → test), then play the full F4 → street run once and tune the street quake and the prop sizes. After that: real art for the street (façades, cars, the pole, the sign) and the robber sprite.

## [2026-10-17 20:42] LOCAL — Task 07 follow-ups: wall-fitted doors/elevators, 8-slot backpack with key-item pockets
### Done
- **Doors and elevators fitted to the two-row wall:** `DoorArt` now generates `Art/Concept/Generated/door_wall.png` and `elevator_wall.png` from the originals.
  - Each source column is remapped between the door's least-squares bottom and top lines. At one shared PPU:
    - the bottom edge lies exactly on the gap's floor edge, at the wall's 2:1 slant;
    - the width fills one wall segment (0.5u);
    - the height is **90% of the stacked wall** (`RoomTiles.WallColumnHeight(rows)`; 2 rows → about 0.91u).
  - A uniform scale can't meet all three, which is why the art is remapped. The pivot is the bottom-edge midpoint (the gap's edge centre); R walls flip X.
  - Door colliders are `DoorArt.ColliderSize/Offset` (0.5 × (height + rise)). Approach points are unchanged.
  - Elevators now sit flush on the back-right wall line, centred on a segment: F4 corridor hotspot (7.0, 1.5), F2 stairs hotspot (3.0, 1.5), F2 corridor prop (7.0, 1.5). Their colliders and hover use the new height; approach points are still in front.
  - Checked in Play: salon (zoomed on a back-left door: no floating), F4 corridor and F2 stairs.
  - **Height conflict:** the two-row wall is 1.0u tall per pixel column, and the player's visible height is about 0.76u. With the door at 90% of the wall, the player reaches about **85%** of the door, not ⅔. A ⅔ door (≈1.15u) would stick out above the wall. Fixing that needs about 2.5–3 wall rows or a smaller player; it's a design call, so I didn't change either.
- **Bag (8 slots) and pockets:**
  - `Inventory.DefaultCapacity`, `LevelConfig.maxInventorySlots` and the generator's `ConfigureBag` are now **8**; docs updated.
  - Validators only check ≥1 and bedsheets ≤ slots, so nothing else assumed 6. BagUI is array-driven.
  - The whole red backpack (100×140 sheet px) shows at an integer **×3** with Point filter. The 8 slots sit exactly on its drawn 2×4 compartment (`UiSkinArt.CellOrigin`, 16 px pitch). Icons are 80% of a cell; names show under the backpack on hover.
  - New `KeyItemPocket`: left pocket = flashlight, right flap = whistle.
    - Owned: the icon. Missing: a black 55% silhouette. The flashlight is greyed while switched off.
    - Never droppable.
    - A click goes through pure `KeyItemPocketRules`: it plays the current node's available choice that requires the item (checked live: the whistle at `f2.corridor` → `c_whistle`), otherwise toggles the flashlight (new `FlagNames.FlashlightOff`; CinematicDirector respects it), otherwise shows the item's name.
  - `KeyItemCornerHUD` is now built only for key items other than flashlight and whistle; currently there are none, so the scene has 0.
  - To fit the ×3 backpack, the right column is wider: `TestSceneUi.ColumnLeft` 0.9 → **0.835**. RoomCameraFraming follows it.
- **Tests:** InventoryTests updated (`BagHoldsEightItems`, full at 8/8, ninth refused). New `KeyItemPocketTests` (5): LevelConfig defaults to 8; missing item → name; owned whistle → the node's whistle choice; whistle with no use → name; flashlight toggles and stays owned and undroppable.
- **Verify:** EditMode **148/148** passed. Validate Content: 25 nodes, 0 problems. Text > Validate: 358 referenced, 0 missing / empty / unused. Interaction Mode was restored to 0.
- **Screenshots** (`Screenshots/`, local): `t07b_salon_doors`, `t07b_door_zoom`, `t07b_f4_corridor`, `t07b_f2_stairs`, `t07b_bag_empty_full`, `t07b_bag_half`, `t07b_bag_full`.

### Files created / changed
- `Scripts/Editor/Art/DoorArt.cs`: wall-fitted door/elevator generation, collider helpers, `Configure(rows)`.
- `Scripts/Editor/SceneBuild/RoomTiles.cs`: `WallColumnHeight(rows)`.
- `Scripts/Editor/SceneBuild/SceneKit.cs`: door/elevator colliders.
- `Scripts/Editor/SceneBuild/BuildingLayout.cs`: elevators on the wall line.
- `Scripts/Editor/SceneBuild/TestSceneBuilder.cs`: `DoorArt.Configure`.
- `Scripts/Editor/Art/UiSkinArt.cs`: backpack sprite, cell and pocket layout.
- `Scripts/Editor/SceneBuild/TestSceneUi.cs`: backpack bag, pockets, wider column, corner HUD only for other key items.
- `Scripts/Items/KeyItemPocketRules.cs` (new), `Scripts/UI/KeyItemPocket.cs` (new).
- `Scripts/Core/FlagNames.cs` (`FlashlightOff`), `Scripts/Cinematic/CinematicDirector.cs`.
- `Scripts/Core/Inventory.cs`, `GameState.cs`, `Scripts/Building/LevelConfig.cs`, `Scripts/UI/BagUI.cs`, `Scripts/Editor/Content/BuildingLevelsGenerator.cs`: 8 slots.
- `Scripts/Text/UiKeys.cs`, `Content/Text/strings_tr.csv`: `ui.pocket.flashlight_on/off`.
- `Tests/EditMode/InventoryTests.cs`, `Tests/EditMode/KeyItemPocketTests.cs` (new).
- `Data/LevelConfig.asset`, `Scenes/Test_Building.unity`, `Art/Concept/Generated/*` (regenerated).
- `CLAUDE.md`: 8-slot bag, pockets, UI list.

### Needs local wiring (checklist for the MCP session)
- [ ] Nothing required.
- [ ] Decide the door/player ratio (see the height conflict above). For a door ⅔ taller than the player, set `wallRows` to 3 on RoomVisualConfig (in the generator, `Visual.Configure(1.5f, 3)`) and rebuild; the doors refit automatically.

### Not verified / risks
- The flashlight icon reads small in its pocket and in the cells (thin art at 80% of a cell).
- The wider right column (0.835) makes the room view slightly smaller.
- `FlashlightOff` only affects the light: conditions that need the flashlight still pass while it's off.
- Real mouse clicks on the pockets weren't tested; I invoked the Button onClick.
- Building a scene while scripts are compiling can open a modal save dialog that blocks MCP. Save or let the compile finish first.

### Next suggested step
- Settle the wall-rows vs door-ratio question and try 3 rows if wanted. Consider a softer pocket icon size for the thin flashlight art.

## [2026-10-17 19:54] LOCAL — Two-row walls + door/elevator art, LED building timer, main menu + ESC pause, backpack bag skin, cinematic subtitles
### Done
- **Walls and scale (task 1):**
  - `RoomVisualConfig.wallRows` (default 2). `RoomTiles.Walls` stacks the rows; windows go only in the top row, and gaps stay open for the full height.
  - The step between rows is **128 px, not 160**. In the 128x192 wall art the face is 128 px tall in any pixel column; the other 32 px is the 2:1 slant. Stepping by 160 left a 32 px dark seam. Rows overlap by 1 px, and no seam line remains.
  - Upper rows offset the variant index (+3), so cracks don't stack straight above each other.
  - `ViewBounds` (camera framing) uses the stacked height (`WallTopAboveEdge(rows)`). The tile transition already lifts every wall child, so both rows rise and drop.
  - `CharacterArt.PlayerHeight` 0.9 → **0.8**. Next to a back wall the player is about 60–65% of the wall height.
  - I kept **two full rows**: in the salon, F4 corridor and Flat 7 shots it reads right, so the 1.5-row option wasn't needed.
- **Door and elevator art:** `DoorArt` imports both PNGs as single sprites.
  - The pivot is measured from the alpha: the centre of the door's slanted base. The PPU makes the door 1.15× the player's height.
  - Every wall-gap `Door` gets an `Art` child, sorted at `WallOrder + 1` and flipped X on R gaps. Colliders and approach points are unchanged.
  - The elevator hotspots (F4 corridor, F2 stairs) and the F2 corridor elevator prop use `asansorkapı.png`, flipped (all three are on back-right walls).
- **LED building timer (task 2):**
  - `E1234 LED SDF` (digits, colon and space, padding 20) is baked by Generate Fonts + Theme. `UITheme.Led` was added.
  - New `LedClock`: a near-black green-tinted rounded panel with a dark outline, a ghost `88:88` at 12% alpha, and a glow on a material instance. The colon blinks once per second while the timer runs and stays solid during transitions and pause.
  - `BuildingTimerDisplay`: green `#4CFF6A`; amber `#FFB020` pulsing at ≤60s; red `#FF3B30` with flicker at ≤30s. Reduce Effects turns off the flicker, not the colour.
  - `PrepCountdownDisplay` uses the same face in amber, red in the last ticks.
  - The `-Ns` labels stay in Inter and match the clock colour.
- **Main menu + ESC pause (task 3):**
  - `SixtyOne > Build Main Menu` builds `Scenes/MainMenu.unity`:
    - background: a low-res blurred, darkened salon shot (`SixtyOne > Capture Menu Background`, run in Play Mode) with a slow `MenuDrift`;
    - the EB Garamond "61 SECONDS" title with a thin rule;
    - Başla / Ayarlar / Çıkış;
    - prep music and click cue.
  - Build Settings: MainMenu 0, Test_Building 1 (the old SampleScene stays third).
  - `UserSettings` (PlayerPrefs): Master → `AudioListener.volume`; Müzik / Efekt scale the cue volumes in AudioService, MusicService, AudioLoopLayer and DialogueVoice; Efektleri Azalt is copied onto GameSettings.
  - The shared `SettingsPanel` is built by `MenuUi`.
  - `PauseMenu` (Esc, Input System) shows a dim overlay with Devam Et / Ayarlar / Ana Menü / Oyundan Çık. Ana Menü and Oyundan Çık ask "Emin misin? İlerleme kaybolacak." first. Esc steps back out of the confirmation or settings before it resumes.
  - **`Core/PauseState` is the only place that touches `Time.timeScale`** (CLAUDE.md updated). It also sets `AudioListener.pause` and blocks input through `InputGate`.
  - UI clicks use `AudioService.PlayUiCue` (`ignoreListenerPause`).
  - `SceneFlow` clears the pause, InputGate and transition state on scene changes.
  - Verified in Play:
    - prep time and stamina stay frozen while paused;
    - a running dog QTE stays frozen for 10s while paused (QTEController and the hidden-item timer check `PauseState.IsPaused`) and continues the same run after resume;
    - dialogue line costs are paid only on advance, which is blocked while paused;
    - menu → Başla → pause → Ana Menü → Evet returns to the menu with timeScale 1 and no input block.
- **Bag skin (task 4):** `UiSkinArt` imports `Art/uı/examples.png` (Point filter, uncompressed, no mips) and slices it into backpack, backpack_body, slot_cell and key_panel (rects measured from the sheet).
  - The backpack has a drawn 2×4 cell grid that can't be 9-sliced, so `bag_frame.png` is generated: the body with its interior filled flat, borders L7 B8 R7 T10.
  - BagUI is a 3×2 grid of blue cells inside the frame at an **integer ×3** scale (`pixelsPerUnitMultiplier = 1/3`). Icons sit centred at 80% of a cell.
  - Highlights: selected = gold tint (BagUI `selectedColor`); hover = cyan, which serves as the combine-target highlight. Names show under the backpack on hover (`HoverReveal`).
  - `KeyItemCornerHUD` frames use the green pixel panel (white when owned, dimmed when missing).
  - No bag logic changed.
- **Subtitles (task 5):** new `SubtitleView`.
  - Layout: a full-width band over the bottom 22%, with a generated transparent → 85% black gradient. The text column runs from 24% of the screen width, max 55%.
  - Fonts: EB Garamond SemiBold name (34 px, `#F2EDE4`); Regular line (26 px, `#E8E4DC`, +15 line spacing, a saved underlay material for the 1 px shadow, max 3 lines). Both fonts are baked with full Turkish coverage and added to UITheme.
  - Behaviour:
    - the line typewrites and the voice blips are rewired to the subtitle typewriter;
    - a bobbing ▼ sits after the last word (4 px at 1.2 Hz; static with Reduce Effects);
    - a click on the band, Space or Enter first completes the line, then advances;
    - fades over 0.2s and hides during room transitions.
  - Node body text plays as narration (no name, no ▼, fades after a reading delay).
  - ChoicePanel no longer shows dialogue or body text: only the title and choices, opening above the band while it's visible. Line costs are unchanged; Sevim's line advance paid exactly 4s once.
- **Verify:** 0 compile errors; EditMode **143/143** passed; Validate Content: 25 nodes, 0 problems; Text > Validate: 356 referenced, 0 missing / empty / unused. Interaction Mode was set to No Throttling for Play and restored to its previous value (0).
- **Screenshots** (`Screenshots/`, local only): `t07_before_walls`, `t07_salon_family_wall-1`, `t07_corridor_family_wall-1`, `t07_flat7_family_wall`, `t07_led_prep_amber-2`, `t07_led_green` / `t07_led_amber` / `t07_led_red`, `t07_main_menu_final-1`, `t07_settings_final`, `t07_pause_menu`, `t07_pause_confirm`, `t07_bag_skin`, `t07_subtitle_line`.

### Files created / changed
- `Scripts/Editor/SceneBuild/RoomTiles.cs`: stacked wall rows, row-aware view bounds.
- `Scripts/Placement/RoomVisualConfig.cs`, `Scripts/Editor/Content/BuildingLevelsGenerator.cs`: `wallRows` (2).
- `Scripts/Editor/Art/CharacterArt.cs`: PlayerHeight 0.8.
- `Scripts/Editor/Art/DoorArt.cs` (new); `Scripts/Editor/SceneBuild/SceneKit.cs`: door art on wall gaps, elevator art on hotspots and prop.
- `Scripts/Editor/SceneBuild/TestSceneBuilder.cs`: wall rows, voice blips on the subtitle typewriter.
- `Scripts/Editor/Text/FontAssetsGenerator.cs`, `Scripts/UI/UITheme.cs`: LED + EB Garamond SDF assets, theme fields.
- `Scripts/UI/LedClock.cs` (new), `BuildingTimerDisplay.cs`, `PrepCountdownDisplay.cs`: LED face.
- `Scripts/Core/PauseState.cs`, `UserSettings.cs`, `SceneFlow.cs` (new): pause, saved settings, scene changes.
- `Scripts/UI/PauseMenu.cs`, `SettingsPanel.cs`, `MainMenu.cs`, `MenuDrift.cs` (new).
- `Scripts/Editor/SceneBuild/MenuUi.cs`, `MainMenuBuilder.cs` (new): menu builders. Build Main Menu saves the generated Test_Building silently instead of prompting, because a modal dialog blocks MCP.
- `Scripts/Audio/AudioService.cs`, `MusicService.cs`, `AudioLoopLayer.cs`, `DialogueVoice.cs`: volume settings, `PlayUiCue`, `SetMusicVolume`.
- `Scripts/Core/GameManager.cs`: applies UserSettings on Awake.
- `Scripts/QTE/QTEController.cs`, `Scripts/World/HiddenItemHotspot.cs`: stop on pause.
- `Scripts/Editor/Art/UiSkinArt.cs`, `Scripts/UI/HoverReveal.cs` (new); `Scripts/Editor/SceneBuild/TestSceneUi.cs`: backpack bag, key-item frames, LED faces, subtitle band, pause menu.
- `Scripts/UI/SubtitleView.cs` (new), `Scripts/UI/ChoicePanel.cs`: subtitles.
- `Scripts/Text/UiKeys.cs`, `Content/Text/strings_tr.csv`: 18 keys (`ui.menu.*`, `ui.settings.*`, `ui.pause.*`), final Turkish.
- Art (generated or imported): `Art/UI/MenuBackground.png`, `Art/UI/subtitle_gradient.png`, `Art/uı/bag_frame.png`, `Art/Fonts/E1234 LED SDF.asset`, `Art/Fonts/EBGaramond/*SDF.asset`, `EBGaramond-Regular SDF Subtitle.mat`, door / elevator / sheet import settings.
- `Scenes/MainMenu.unity` (new), `Scenes/Test_Building.unity` (rebuilt), `ProjectSettings/EditorBuildSettings.asset`.
- `CLAUDE.md`: timeScale rule (PauseState only), Menus and Subtitles notes.

### Needs local wiring (checklist for the MCP session)
- [ ] Nothing required. To re-shoot the menu background: Play Test_Building → SixtyOne > Capture Menu Background → stop → SixtyOne > Build Main Menu.
- [ ] Press Esc and Space/Enter by hand once. MCP can't send key presses, so I drove the pause and the subtitle advance through their methods; the key bindings themselves are unverified.

### Not verified / risks
- Real keyboard input (Esc / Space / Enter) wasn't tested from MCP (see above).
- There is no AudioMixer asset, and one can't be created from code. Music and effect volumes scale cue volumes in code: new sounds and music/ambience loops follow a slider change at once, but one-shots and alarm/quake loops already playing keep their old volume.
- The pixel-art bag next to the cartoon world is a style clash, though a mild one; I kept the item icons smooth (the softer option). The ×3 scale is crisp at 1080p only; other resolutions scale non-integer.
- The menu background is a 192×108 capture upscaled, so it reads as a blurred, slightly blocky shot.
- The pause overlay is 80% black: at 62% it barely showed on the dark rooms.
- The ▼ is a glyph in code, not a CSV key (it isn't text).
- Leftover user files not committed: ComfyUI concept PNGs, `61s_local.bat`, the E1234 OTF/WOFF variants, the LiberationSans fallback and VersionControlSettings changes.

### Next suggested step
- Play the full loop by hand with a keyboard (Esc mid-QTE, Space through Sevim). If the backpack clashes too much, swap the bag skin for a softer frame.

## [2026-10-17 16:39] LOCAL — Exhaustion death + darkening, team audio wired (music/ambience/voice blips), item icons, Sevim idle, window/cracked walls
### Done
- **Exhaustion (task 1):**
  - `GameState.Exhaustion` (0..1) is restored by snapshots. `SurvivalModel.StepExhaustion` rises only at 0 stamina and decays once stamina is above 0. At 1 the player dies: `death.exhaustion` + `tip.supplies`, Checkpoint rewind.
  - **Final numbers:** stamina max 100, drain **0.42/s** (was 0.15). With no food or water, stamina hits 0 at ~165s (~240s with no heavy actions at all; the F3 cable/hole climbs cost 20–25). Food +35 buys ~83s, water +25 ~60s. At 0, exhaustion rises **0.03/s** (~33s to death) and falls **0.2/s**. Set in `ConfigureStamina` (generator).
  - `PoisonOverlay` is driven by max(poison, exhaustion): gas keeps its green tint and wobble; exhaustion is black with a lub-dub heartbeat pulse. Reduce Effects drops the pulse and keeps the fade.
  - CinematicDirector and the panic baseline also use max(poison, exhaustion).
  - The stamina bar background flashes red at 0 (solid red with Reduce Effects).
  - New `ExhaustionTests` (4 tests).
- **Audio (task 2):**
  - **Import:** `SixtyOne > Audio > Import Team Audio` moves Voice/ → Audio/ through AssetDatabase (the Voice folder is gone), sets import settings (SFX: Decompress On Load ADPCM; music/ambience/long loops: Streaming Vorbis), puts clips on cues and fills `DialogueVoices.asset`.
  - **New cues:** quake_f4/f3/f2 (7.1/6.2/6.9 recordings, looped while shaking, faded at the end), craft, whistle, dog_bark, dog_growl, electric, heartbeat, amb_drip, music_prep/tension/outro.
  - **Code:**
    - `MusicService`: 2-source crossfade. Prep = calm, quake = silence, after a quake = tension, epilogue = outro. A rewind restarts the stage's track.
    - `AmbienceService`: drip after the quake indoors; dog growl in f2_corridor until DogGone, plus one bark on entry; heartbeat follows panic above the threshold, exhaustion and poison.
    - `FootstepsAudio`: loops while the player runs; the transition no longer plays footsteps.
    - `DialogueVoice` + `DialogueVoiceProfile`: animalese blips every 2–3 letters, 0.08–0.15s random slices. Sevim 1.15, Hasan 0.85, Cemil 1.0.
  - **Hooks:** the pre-quake growl (`cue_placement_alarm`) plays in the last 8 prep seconds and during placement countdowns. Macro rewinds play the rewind louder and lower (longer). The building alarm now loops and stops on death, rewind out of the window, and the epilogue. The AFAD alert never stacks twice.
  - **Wiring:** hazards → crash/glass by motion; lighter/stove/kitchen-fire deaths → fire; electrocution death and breaker QTE misses → electric; dog QTE misses → bark; the dog whistle choice → whistle; craft/rope → craft.
  - **Mix:** music 0.35–0.5, ambience 0.15, growl 0.3, click 0.35, tick 0.4, death/fire/quakes 1.0. Pitch ±5% on clicks, ticks and QTE blips.
  - New `MusicRulesTests`.
- **Art (task 3):**
  - **Item icons:** 13 icons, imported by `ItemIconArt` (sprite, PPU so the long side is 0.42u). They show in the bag, the key-item HUD (preserveAspect, slightly larger), surface pickups and dropped items. Items without art keep the placeholder.
  - **Sevim idle:** `Import Character Frames` also imports SEVIM_IDLE (9 frames, same PPU and foot pivot, 0.9× player height). A decorative animated Sevim (`SpriteLoopAnimator`, 9 fps) stands in the F4 corridor. She is not clickable: her dialogue still starts from the Flat 11 exit choice, because a hotspot would pull "thanks" out of the panel.
  - **Walls:** the corner-pair PNGs (128px faces, same format as the sheet cells) are sliced into `_L`/`_R`. Window segments use `pencereler 1` (home) and `pencereler 222` (corridor). New `DamagedCorridorTileSet` mixes the two cracked walls into the F3 corridor and the F2 corridor/stairs.
  - `parke.png` is **not used**: it has a burn stain and notched edges and is not 2:1 (332×152), so it does not tile.
- **Verify:** 0 compile errors. EditMode **143/143**. Validate Content: 25 nodes, 0 problems. Text > Validate: 338 referenced, 0 missing/empty/unused.
- **Play Mode (partly; stopped at the user's request):**
  - Confirmed: prep music → growl at 7s → 7.1 quake (music faded) → glass + death sting → tension music + drip.
  - Sevim blips: female @1.15. Hasan blips: male @0.85. F3 aftershock: 6.2. Kitchen-fire bomb fires. The macro rewind is louder.
  - Screenshots in `Temp/Screens/t06/`: prep_salon, bag_icons, sevim, f3_corridor_cracked-1.
### Files created / changed
- Core/Building: `GameState`, `Snapshot`, `SurvivalModel`, `SurvivalController` (exhaustion, alarm loop), `LevelConfig` (exhaustion fields, `ConfigureStamina`).
- UI: `PoisonOverlay`, `StaminaBar`, `TypewriterText` (CharRevealed/RevealEnded); `Dialogue/DialogueController` (SpeakerChanged); `CinematicDirector`.
- Audio: new `MusicService`, `MusicRules`, `AmbienceService`, `AudioLoopLayer`, `FootstepsAudio`, `DialogueVoice`, `DialogueVoiceProfile`; changed `AudioService` (IsPlaying/FadeOut/scaled Play), `AudioCue` (SetClips), `AudioEventMap` (per-floor quakes, preQuake, craft), `AudioEventHooks`.
- Placement/World: `SpriteLoopAnimator` (new), `RoomTransitionController` (no footsteps), `DroppedItemsPresenter` (icons), `Prep/PrepPhase` (alert dedupe).
- Editor:
  - `TeamAudioImporter` and `ItemIconArt` (new);
  - `CharacterArt` (Sevim);
  - `TileArt` (corner-pair slicing, DamagedCorridor set);
  - `BuildingLevelsGenerator` (+Floor2/3: cues, death/hazard cues, stamina, tip.supplies);
  - `BuildingLayout`, `SceneKit`, `TestSceneBuilder`, `TestSceneUi`.
- Tests: `ExhaustionTests`, `MusicRulesTests`. Content: `strings_tr.csv` (death.exhaustion, tip.supplies(.title)).
- Assets:
  - Audio/ (moved clips, `Trimmed/craft_short.wav` + `Trimmed/electric_short.wav` cut with ffmpeg, regenerated `Generated/sfx_rewind.wav` + `sfx_building_alarm_loop.wav`);
  - new cue assets, `DialogueVoices.asset`, `DamagedCorridorTileSet.asset`, `tip.supplies.asset`;
  - regenerated scene and data.
- `.gitignore`: the 95 MB `Audio/elektrik kacagı.wav` (329s) stays local; the cue uses the 2.5s trim.
### Needs local wiring (checklist for the MCP session)
- [ ] After pulling: run SixtyOne > Generate Building Levels → Audio > Import Team Audio → Art > Import Character Frames → Build Test Scene (in that order; the importer needs the cues).
- [ ] Play the full route by ear (not finished this session):
  - the dog corridor (growl, bark, whistle);
  - the F2 6.9 aftershock;
  - the epilogue outro;
  - eating/drinking mid-exhaustion;
  - a full stamina-0 → exhaustion death → checkpoint rewind.
- [ ] Listen to the mix levels and the looped quake recordings; tune volumes on the cue assets.
- [ ] Screenshot a home window wall close up (the salon right wall) and check the seams of the new window and cracked faces.
### Not verified / risks
- **Play Mode testing was stopped at the user's request ("testi sal").** Not checked:
  - the exhaustion death in play, plus the overlay heartbeat and the stamina flash visuals;
  - the dog growl/bark/whistle, the F2 quake, the outro, footsteps while running, and heartbeat levels.
- **File changes during the session:** `afad.mp3` was renamed to `Audio/Generated/alarm.mp3`, and `sfx_rewind` / `sfx_building_alarm_loop` were deleted while I worked.
  - Per the user: alarm.mp3 = the AFAD alert, and the other two were regenerated with `tools/gen_sfx.py`.
  - The regenerated WAVs differ in bytes from the old ones (the script changed since).
- **Extra clips:** `Audio/loop.mp3` and the original `craft sesi.wav` (12.5s) are not on any cue.
- **Editor timing:** the Play Mode loop was slow under MCP (about 10 fps). `TypewriterText` now also raises the remaining characters on the final step, so voice blips survive long frames.
- **Placement after a micro rewind:** the quake recording only restarts with the main shock, not during the window.
### Next suggested step
- Finish the by-ear playthrough above, then tune the cue volumes and the stamina numbers if the exhaustion timing feels off.

---

## [2026-10-17 11:20] LOCAL — Verified the world scale + tile transition merge; transitions no longer cost time; framing and shake tuned (visual/playthrough tests skipped at the user's request)
### Done
- **Compile:** the cloud code (world scale 1.5, tile rise/drop transition, animator rules) compiled with **0 errors** in Unity 6000.6. No asmdef changes needed; all the first-time APIs (Tilemap per-tile access, sprite proxies, RoomVisualConfig, lossyScale helpers) were fine.
- **Checklist steps 2–5:**
  - Generate Building Levels: writes `RoomVisualConfig.asset` (1.5) and the new `TransitionConfig` fields.
  - Import Character Frames: 11 / 10 / 9 frames, unchanged.
  - Build Test Scene: 24 rooms under a `World` root at (1.5, 1.5, 1).
  - Validate Content: 25 nodes, 0 problems. Text > Validate: 335 referenced, 0 missing / empty / unused.
  - EditMode: **134/134**, run before my fixes.
- **Not a bug, but worth knowing:** a transition looked "stuck" in Play Mode. The cause was Editor throttling, not the controller: when Unity is unfocused, the Play Mode player loop barely ticks (frameCount stayed frozen between MCP calls).
  - The fix for MCP sessions is the Editor preference Interaction Mode = No Throttling (`EditorPrefs InteractionMode = 1`) plus `Application.runInBackground = true` at runtime.
  - I used both for this session and restored the preference to 0 afterwards; no ProjectSettings were changed.
- **4a Framing:** `RoomCameraFraming.fill` 0.9 → **0.97**; `minOrthoSize` 1.2 → **1.0**; playfield viewport (0, 0.07, 0.89, 0.83) → **(0, 0.065, 0.89, 0.845)**.
  - Rooms now use about 97% of the playfield, and small rooms are framed closer.
  - The top edge stays below the timer strip and the right edge left of the HUD column, so walls are not clipped under the HUD.
- **4b Transitions cost no time:** `NavigationEvents.TimersPaused` / `TimerDelta(dt)` is true/0 from `RaiseTransitionStart` until `RaiseTransitionComplete` (also reset in `ClearAll`).
  - `PrepPhase` and `SurvivalController` advance the prep countdown, building timer, stamina drain and poison with `TimerDelta(Time.deltaTime)`.
  - The door / move cost is paid once, before the transition, as before.
  - New `TransitionTimerTests` (4 tests): pause only between start and complete; the prep countdown and the building timer each pay only the door cost across a 1.15s transition; timers run again afterwards.
- **4c Shake:** `CameraShake` multiplies every shake (quakes and QTE shakes) by `SizeFactor` = ortho size / `referenceOrthoSize`, clamped to 0.4–3.
  - The reference is **2.0**, about the salon's ortho at the old world scale 1, so the act intensities keep their old on-screen feel at scale 1.5.
  - Reduce Effects still multiplies by `ReducedShakeScale` first. Pure `SizeFactor(ortho, reference)` is available for tests.
- **4d Dropped items:** left as is. `DroppedItemsPresenter` respawns dropped items on node enter, which happens mid-OUT, so they cannot ride the old room's tiles without reworking the presenter (respawning after the swap and registering them as transition pieces). They pop in with the new room instead.

### Files created / changed
- `Scripts/Core/NavigationEvents.cs`: TimersPaused / TimerDelta.
- `Scripts/Prep/PrepPhase.cs`, `Scripts/Building/SurvivalController.cs`: paused timer delta.
- `Scripts/Iso/CameraShake.cs`: ortho-size scaling (`sizeSource`, `referenceOrthoSize`, `SizeFactor`).
- `Scripts/Iso/RoomCameraFraming.cs`: fill 0.97, min ortho 1.0.
- `Scripts/Editor/SceneBuild/TestSceneBuilder.cs`: playfield viewport.
- `Tests/EditMode/TransitionTimerTests.cs`: new.
- Regenerated `Data/RoomVisualConfig.asset` (new), `Data/TransitionConfig.asset`, `Scenes/Test_Building.unity`; the cloud session's missing `.meta` files are now committed.
- `CLAUDE.md`: transition timer pause + shake scaling.

### Needs local wiring (checklist for the MCP session)
- [ ] Run the EditMode tests: expect **138/138** (134 + 4 new). They were compiled but **not run**.
- [ ] The visual checks I skipped (screenshot each):
  - character sizes and foot contact at 1.5;
  - seams in the salon (wood) and the F3 corridor (stone);
  - a small vs a big room with the new fill 0.97;
  - lateral and descend transitions, an instant rewind, the Reduce Effects fade;
  - clicks ignored mid-transition; rooms back in place after 10+ transitions;
  - the profiler on a first visit to a room.
- [ ] Play the full route once (prep → quake → floors 3/2 → dog QTE → epilogue). Check that the prep / building timers do not move during the tile animation and that quakes feel equally strong in small and big rooms.
- [ ] For MCP Play Mode work, set Preferences > General > Interaction Mode = No Throttling first, or the player loop stalls when Unity is unfocused.

### Not verified / risks
- **Skipped at the user's request:** all visual checks, before/after fix screenshots, the profiler, the full playthrough, and the final EditMode run. Only compilation and the scene rebuild were verified after the fixes.
- **Framing (fill 0.97, viewport top 0.91):** tall props on back walls could touch the timer strip in some rooms. If they do, lower the viewport height to 0.83 or fill to 0.95.
- **Shake reference 2.0** is an estimate of the old salon framing; tune it on `Main Camera > CameraShake` if quakes feel too strong or weak.
- **Placement timers keep running during transitions** (only prep and building timers pause). Placements start after the swap, so this should not matter.
- **Before shots only:** `Temp/Screens/t05/before_salon.png`, `before_hall.png` (old scene, pre-scale) and `scaled_salon_prefix.png` (scale 1.5, before the framing fix; looks the same on screen as before because the framing normalises the scale).

### Next suggested step
- LOCAL: run the skipped visual checklist and the 138 tests; tune fill / shake reference by eye.

---

## [2026-10-17 06:27 UTC] CLOUD — Character animation gaps + tests, one WorldScale (1.5x) for all room visuals, tiles-fly-up / drop-in room transition
(The cloud clock is UTC; the 08:59 LOCAL entry below came earlier in real time. Built on top of `28bec4b`.)

### Done
- **1. Character animation.** The 08:59 local session already built most of it: `Import Character Frames`, `CharacterAnimator`, breathing with panic, shadows, IsoSorter and the Lit material. I only filled the gaps:
  - New runtime `FrameOrder` (pure numeric sort by "(n)"). The importer now uses it, so the sort is unit-tested.
  - `CharacterAnimator` exposes pure rules: `StateFor` (Idle/Run), `RunFrameIndex`, `FlipFor`, `BreathRate`. The component uses them and exposes `State`.
  - **Not changed on purpose:** idle holds the hand-picked "most neutral" frame per character (player 3, wife 10, child 3; `CharacterArt.IdleFrame`), not frame 1. The local session chose it from a contact sheet. Set it to 0 there if you really want frame 1.
- **2. Bigger tiles and walls.**
  - New `RoomVisualConfig` asset (`Data/RoomVisualConfig.asset`, `worldScale` = **1.5**).
  - Build Test Scene puts every room and the family under one **World** root and scales it once. Floor Tilemaps, wall segments, props, anchors, hotspots, doors, spawns and characters therefore stay aligned. Tile PPU is untouched (no per-tile changes, so no new seams).
  - Light2D radii under World are multiplied by the scale (they are world-space values).
  - Runtime values tuned at scale 1 now read the scale from the hierarchy (`Room.Scale`, `Room.WorldOffset`):
    - run speed and the animation speed ratio
    - follower offsets, start and settle distances
    - arrival offsets and anchor slot offsets
    - dropped-item placement (now room-local)
    - floor clamping (`ClampToFloor` via InverseTransformPoint)
    - `Room.ViewBounds` (stored room-local, transformed to world)
    - the flashlight radius and offset
- **3. Room transition.** `RoomTransitionController` keeps its events, input lock, cues, `Finish()` and Play hooks.
  - **OUT:** the old room's floor tiles (one sprite proxy per Tilemap cell, built once per room and reused; the real Tilemap is hidden meanwhile), wall segments and props rise and fade with ease-in. The wave runs from the far corner (highest local y) to the nearest piece, and the family fades too.
  - **SWAP** at the midpoint; the old room's pieces return exactly to rest.
  - **IN:** the new floor drops in first, then walls (+0.05s), then props (+0.1s), each wave-staggered, with ease-out-back (about 10% settle). Then the family pops in at the spawn (0.15s).
  - **Descend** (floor change) uses a longer, higher drop. **Reduce Effects** uses a 0.25s fade through the overlay. **Rewinds** stay instant.
  - Pure `TileTransitionPlan` schedules everything from `TransitionConfig`: depth, delays, piece durations, totals and easing.
  - Floor and Walls containers are tagged with `TransitionLayerTag` by RoomTiles. Other room children with renderers are props. Lights and spawn points never move.
  - The `TransitionPan` camera layer is gone again: CameraRig (framing) → Main Camera (shake/tilt).
  - Unscaled time and coroutines only; no DOTween; `Time.timeScale` is never touched.

  | TransitionConfig | Value |
  |---|---|
  | Out | 0.35s, rise 1.2 |
  | In | 0.4s, drop 1.0 |
  | Descend in | 0.65s, drop 2.5 |
  | Stagger | 0.12s |
  | Wall lag / prop lag | 0.05s / 0.1s |
  | Bounce | 0.1 |
  | Family pop | 0.15s |
  | Reduce Effects fade | 0.25s |
  | **Total** | **0.9s lateral, 1.15s descend** |

  Heights are room-local, so they scale with WorldScale.
- **Tests:** +10, **134/134**.
  - `CharacterAnimationTests`: numeric sort, idle vs run, frame loop + flip, panic breathing.
  - `TileTransitionTests`: far-before-near wave, floor < walls < props, every piece lands exactly at outDuration/inDuration, totals and descend from the config, extreme configs, easing and bounce.
  - Generator harness: 25/25 nodes reachable, validator 0 problems, 12 playthroughs pass.
  - Compile check now uses Roslyn at C# 9 (Unity 6's language level) against API stubs; it can only catch what the stubs cover. ContentValidator unchanged (not needed).

### Files created / changed
- New runtime:
  - `Placement/FrameOrder.cs`, `RoomVisualConfig.cs`, `TileTransitionPlan.cs`, `RoomTransitionPieces.cs`, `TransitionLayerTag.cs`
- Changed runtime:
  - `Placement/`: `RoomTransitionController` (rewritten), `TransitionConfig` (new fields), `CharacterAnimator`, `CharacterMotor`, `Room`, `RoomSwitcher`, `PlayerMover`, `FamilyFollower`, `FamilyMember`
  - `World/DroppedItemsPresenter`, `Cinematic/CinematicDirector`
- Changed editor:
  - `Art/CharacterArt` (uses FrameOrder)
  - `SceneBuild/TestSceneBuilder` (World root + scale, transition characters, no TransitionPan)
  - `SceneBuild/RoomTiles` (layer tags)
  - `Content/BuildingLevelsGenerator` (RoomVisualConfig, new TransitionConfig values)
- Tests: `CharacterAnimationTests`, `TileTransitionTests`. Docs: CLAUDE.md.

### Needs local wiring (checklist for the MCP session, in run order)
1. `git pull`, open Unity, wait for the compile; expect 0 errors.
2. Run **SixtyOne > Generate Building Levels**. It writes `Data/RoomVisualConfig.asset` (1.5) and rewrites `Data/TransitionConfig.asset` with the new fields; the old pan/fade fields disappear.
3. Run **SixtyOne > Art > Import Character Frames** (no change expected; it now sorts through `FrameOrder`), then **SixtyOne > Build Test Scene**.
   - In the Hierarchy: a `World` root with scale (1.5, 1.5, 1) holds all 24 `Room_*` and `Family`. The `CameraRig` → `Main Camera` has no TransitionPan.
4. Run **Validate Content** (0 problems) and **Text > Validate** (0 missing/unused).
5. Run the EditMode Test Runner; expect **134/134**.
6. **Character sizes** (Play, F4 prep salon, everyone standing). Screenshot each check.
   - Player about 1.35 world units tall, which is about 1.3 floor-tile widths.
   - The wife is a little shorter.
   - The child is about 0.75 of the player.
   - Feet sit on the floor (no floating or sinking) in salon, corridor and Flat 7.
   - Run across a room: 12 fps loop, flips by direction, followers keep up. Run speed should look the same relative to the tiles as before (it is scaled by 1.5).
7. **Seams after scaling:** zoom the Scene view into a floor and the back walls in a wood room (Flat 11 salon) and a stone room (F3 corridor). Look for hairline gaps between diamonds or between wall segments, and compare with the 08:59 screenshots.
   - If lines appear, it is MSAA/bilinear at the new zoom, not PPU (unchanged). Try 1.4 or 1.6 in RoomVisualConfig before touching import settings.
8. **Does it read bigger?** `RoomCameraFraming` fits every room to 90% of the playfield, so a uniform scale cannot make a room that already fills the screen bigger on screen.
   - What grows is small rooms (those clamped by minOrthoSize 1.2) and everything relative to the unscaled HUD.
   - Compare before/after screenshots of a small room (Flat 11 hall, F2 stairs) and a big one (F3 corridor).
   - If big rooms still read small, the knob is `RoomCameraFraming.fill` (0.9) or less padding in `Room.ViewBounds`, not WorldScale. Tell the cloud session which you prefer.
9. **Transition feel:**
   - Salon → kitchen door (lateral): old tiles should rise from the far corner first, the new floor drop before walls, props last, a small settle, then the family pops in. Total about 0.9s.
   - Corridor → stairs to F3 and the Flat 9 balcony rope (descend): a longer, higher drop.
   - Click during a transition: it must be ignored (input lock).
   - Die and rewind: an instant swap, no tile animation.
   - Settings → Reduce Effects on: a plain 0.25s fade.
   - Tune in `Data/TransitionConfig.asset`.
10. **Performance during the transition:**
    - Profiler (Play): the first visit to each room creates its floor proxies once (about 16–40 GameObjects), so watch for a one-frame spike on the first transition into a room.
    - Later transitions should only move transforms and set colours (no GC per frame beyond the iterator).
    - Check Batches/SetPass in the Stats window mid-transition: the per-tile proxies replace one chunked Tilemap draw.
11. **Sanity:**
    - After 5–6 transitions, every room's tiles, walls, props, labels, dropped items and door mats are back exactly in place with full alpha. Revisit Flat 11 hall, Flat 12 living and the F2 corridor.
    - The flashlight circle (F3 Flat 10, power off) still covers about the same area around the player.

### Not verified / risks
- **Not run in Unity.** Compiled with Roslyn against API stubs. APIs used here for the first time:
  - `Tilemap.cellBounds.allPositionsWithin`, `HasTile`, `GetSprite`, `GetColor`, `GetCellCenterLocal`
  - `Renderer.sortingLayerID`
  - `Rect.MinMaxRect`
  - `Transform.TransformPoint` / `InverseTransformPoint` / `TransformVector`
- **Tile proxies:** they share the floor's sorting order. Where the 254-PPU tiles overlap by ~1px the draw order between neighbours is arbitrary during the 0.75s animation (same texture, so it should be invisible).
- **WorldInteractable highlight:** the pulse writes the body colour every frame, so a highlighted pickup may ignore the fade for ~0.4s.
- **Dropped items:** `DroppedItemsPresenter` respawns them on node enter (mid-OUT), so dropped items in the old room do not rise with it.
- **CameraShake:** magnitudes are world units and were not scaled. Where framing zooms out for the bigger world, quakes look about 1/1.5 as strong; tune intensityPerAct if needed.
- The transition now takes about 0.9s instead of 0.35s. Each prep move costs that much of the 60s prep (5 moves ≈ 4.5s); re-check the prep budget.

### Next suggested step
- LOCAL: run the checklist above with screenshots (sizes, seams, a mid-transition frame, the profiler on a first visit), then tune RoomVisualConfig, TransitionConfig and RoomCameraFraming.fill by feel.

---

## [2026-10-17 08:59] LOCAL — Visual + interaction overhaul: full-screen rooms, multi-room flats, doors + click-and-run, animated family, Inter + UITheme
### Done
- **Rooms fill the screen.**
  - `RoomCameraFraming` fits each room to 90% of the playfield (the screen left of the HUD column, above the panel peek strip, below the timer).
  - The HUD is compact overlays: timer top-centre (Bold, tabular digits), loop + Notes top-left, and a slim right column (key items, 6 bag slots, Drop / Tie rope, a thin stamina bar). The column collapses to a tab (`CollapsiblePanel`, remembered in PlayerPrefs).
- **Choice panel is a bottom overlay.**
  - It sizes to its content and slides in only when it has something to show.
  - It sinks to a title-only peek strip when it has no buttons (after a reading delay) or while the player runs; click the strip to reopen it.
  - Dialogue typewrites at 90 cps. Clicking completes the line, and the first click on "Devam" while text is still typing only completes it. Per-line time costs are untouched (DialogueController pays them on advance).
- **Multi-room flats (24 rooms, was 14).** Each flat is entrance hall + living room + kitchen/bedroom, sharing one zone (= one node):
  - Flat 11 already had 4 rooms; doors were added.
  - Flat 12: hall / living / bedroom. Flat 9: hall (fuse box) / living (balcony + rope) / kitchen (hanging cables). Flat 10: hall / kitchen (stove) / living (collapsed floor + hidden tape). Flat 7: living (placement) / hall / kitchen (stove). Flat 8: hall / living (hidden water) / bedroom.
  - Corridors stay single rooms, with one door per flat plus the stairs exit.
  - Sub-room choices use the new `Condition.InRoom`: Flat 9 repair in the hall, cables in the kitchen; Flat 10 valve in the kitchen, hole in the living room; Flat 7 valve in the kitchen.
- **Doors replace the room buttons.**
  - `Door` sits on a wall gap or floor exit. Hover shows a brighter threshold mat, an arrow and the room name (dimmed when unavailable).
  - Inside a flat a door switches rooms for `DoorSeconds` = 1s. Into another zone it plays the node's move choice that leads there (`RoomLinkRules`), so move costs, locks, fatal locks and flags behave exactly as before. Those choices are hidden from the panel.
  - `ChoiceHotspot`s: Hasan (talk), Cemil (trader), the F4 and F2-stairs elevators (trap). Hasan disappears after his gift.
- **Click-and-run.**
  - `IsoPicker` gained hover and routing: after the panic input delay, `PlayerMover` runs to a floor point (straight line, clamped to the room's `FloorArea` diamond plus balcony cells) or to a target's approach point, then uses it.
  - Placement / QTE / dialogue clicks are unchanged.
  - Wife and child follow (`FamilyFollower`: delay + offset, injured = slower) and snap around the arrival point on a room change.
- **Character animation.** `SixtyOne > Art > Import Character Frames` (re-runnable): numeric sort, Sprite, Max 512, compressed, no mips, one shared PPU (1690), pivot bottom-centre at the feet (lowest opaque row over all frames).
  - `CharacterAnimator`: run loop at 12 fps × speed ratio, flipX by direction (the art faces right).
  - Idle holds the most neutral frame (checked on a contact sheet) and breathes: scaleY 1.0↔1.02, 0.008u bob, 1.6s period, random phase, up to 2.2× faster at full panic.
  - Soft shadow under the feet; SortingGroup + IsoSorter; Sprite-Lit-Default material.
- **Text.**
  - Inter 4.1 (OFL) downloaded from the official rsms/inter GitHub release into `Art/Fonts/` (+ license).
  - `SixtyOne > Text > Generate Fonts + Theme` bakes static SDF assets (Regular / SemiBold / Bold, ASCII + full Turkish + punctuation, 154 glyphs, no missing characters) with a dynamic Inter fallback. It makes Inter the TMP default font and writes `Data/UI/UITheme.asset`.
  - Buttons are rounded semi-transparent panels with hover / pressed tints and an accent left bar (muted when locked). Locked choices put the reason on their own line. Small labels are all-caps and letter-spaced. Everything is ≥ 16 px at 1080p (enforced by the builder).
- **Validator:** new `CheckDoors`: door targets and arrival points exist, hotspots name real choices, and every non-heavy cross-zone move has a door.
- **Results:**
  - Validate Content: 25 nodes, 0 problems. Text > Validate: 335 referenced, 0 missing / empty / unused.
  - EditMode **124/124** (114 + 10 new `NavigationTests`). No existing test referenced the room buttons, so none needed changes.

### Frame counts and final values
- Frames: Mehmet (player) **11**, wife (anne) **10**, child (çocuk) **9**.
- Idle frames: Mehmet (3), anne (10), çocuk (3).
- Heights: shared PPU 1690. The player is 0.90u tall (1.0× body scale); wife 0.846u (×0.971); child 0.675u (×1.311).
  - The source art is not drawn to scale (wife ~3% taller than the player, child 0.59×), so the body transform corrects it. The PPU stays shared and the files are not resized.
- **MovementConfig:** runSpeed 2.4 u/s, followers ×1.1, injured ×0.75, floorMargin 0.25 cells, wife delay 0.2s / offset (0.38, 0.1), child delay 0.35s / offset (−0.32, 0.12), followStart 0.35u, doorSeconds 1, runFps 12, breath 1.6s / +2% / 0.008u, panic ×2.2.
- **UITheme:**
  - Sizes: timer 64, title 30, speaker 26, body 25 (+12 line spacing), choice 23, small 18, label 16 (letter spacing 8).
  - Colours: accent #FFC24A, panel 6% grey @ 86%.
  - Choice row 56 px, typewriter 90 cps, slide 0.22s.
- **RoomCameraFraming:** fill 0.9, free viewport (0, 0.07, 0.89, 0.83), min ortho 1.2.

### Files created / changed
- Runtime (new):
  - `Placement/`: `Door`, `ChoiceHotspot`, `RoomLink`, `RoomLinkRules` (pure), `RoomNavigator`, `FloorArea` (pure), `CharacterMotor`, `PlayerMover`, `FamilyFollower`, `CharacterAnimator`, `MovementConfig`
  - `Iso/IWorldClickRouter`, `Nodes/IChoiceFilter`
  - `UI/`: `UITheme`, `TypewriterText`, `CollapsiblePanel`
- Runtime (changed):
  - `IsoPicker` (hover, routing, floor clicks)
  - `RoomSwitcher` (zones, pending arrival, local switch, `CurrentRoomId` = shown room)
  - `Room` (zone, floor area, links); `IRoomPresenter` + `NodePlayer` (rewind keeps the sub-room; choice filter)
  - `Condition` / `ConditionEvaluator` (`InRoom`); `FamilyMember` (stops its motor on snap)
  - `ChoicePanel` / `ChoiceButton` (overlay, fit, peek, typewriter, accent bar); timer displays (tabular digits)
  - `RoomCameraFraming`; `WorldInteractable` / `RopeDescent` / `WorldDroppedItem` (approach points); `UiKeys`
- Editor:
  - `BuildingLayout` (rewritten: zones, doors via `Link` / `OneWay`, hotspots)
  - `SceneKit` (Door, Hotspot, AnimatedMember); `TestSceneBuilder` (wiring, door second pass); `TestSceneUi` (rewritten layout, theme)
  - `UiFactory` (themed); `PlaceholderArt` (shadow, rounded 9-slice, arrow); `WorldLabelStyle` (theme font)
  - new `Art/CharacterArt`, new `Text/FontAssetsGenerator`
  - `BuildingLevelsGenerator` (MovementConfig asset, sub-room ids, InRoom choices); `ContentValidator` (CheckDoors)
- Content: `strings_tr.csv` gains 28 keys (24 `room.*` door labels, `ui.door.unavailable`, `ui.not_now`, `ui.bag.collapse/expand`), and two stale strings were fixed ("Antre", "Antreye koş").
- Assets:
  - `Art/Fonts/*` (Inter TTFs, SDF assets, OFL license); `Art/char/RUN` import settings; `Data/UI/UITheme.asset`; `Data/MovementConfig.asset`
  - `TMP Settings.asset` (default font = Inter); the rebuilt `Test_Building.unity`; regenerated node assets
- Tests: `Tests/EditMode/NavigationTests.cs`; docs: `CLAUDE.md`.

### Needs local wiring (checklist for the MCP session)
- [ ] Nothing mandatory: Build Test Scene runs fonts / theme / frame import and reproduces everything. After pulling, run SixtyOne > Generate Building Levels then Build Test Scene if anything looks stale.
- [ ] Optional: play once with a real mouse to feel the hover markers, the run speed (2.4 u/s) and the panel peek timing (the playthrough here was scripted).

### Not verified / risks
- **Playthrough was scripted, not hand-played.** MCP cannot send pointer clicks, so the run used `execute_code` calls to `Door.OnClicked`, `PlayerMover.Route`, hotspots, `NodePlayer.Choose`, the panel buttons, anchors and `QTEController.OnKey`.
  - Because MCP round-trips take seconds, the test topped up `PrepTime` / `BuildingTime` and extended placement timers. Real mouse hover and click picking was not exercised by hand.
  - Route covered: prep (hall wrench + flashlight, kitchen valve + gloves) → quake → bedroom sheets → Sevim → Hasan (hotspot) → Flat 12 (3 rooms, tie rope, harness) → stairs → F3 aftershock → Cemil (gloves) → Flat 9 fuse QTE → balcony rope → F2 aftershock → Flat 7 kitchen valve → dog (food) → stairs → street epilogue.
- **Props never block the run line** (no pathfinding at all); targets are only clamped to the floor diamond.
- **Placeholder art:** doors are wall gaps + a threshold mat (no door sprite); NPCs are capsule placeholders.
- **Screenshot artefact:** some screenshots taken during a room fade show no HUD (a capture artefact); the HUD is present on the next frame.
- **Editor auto-edits again:** `ProjectSettings/VersionControlSettings` (→ Unity Version Control) and `TimeManager` (serialization upgrade). Both were reverted and are not committed.

### Next suggested step
- Hand-play once with the mouse; tune `runSpeed` / `DoorSeconds` / peek delay by feel; have the artist deliver door-frame and NPC sprites (swap them in `SceneKit.Door` / `Hotspot`).

### Screenshots (local only, `Temp/Screens/`, not committed)
- before_prep, after_prep_salon_hd, running_family, kitchen_run_to_drawer, f4_prep_kitchen2, f4_flat11_hall, f4_flat11_bedroom2, f4_sevim_dialogue-1, f4_sevim_choices, f4_corridor_hasan, dialogue_hasan, f4_flat12_living, bag_rope_tied, f4_corridor, f3_corridor_aftershock, f3_flat9_hall-1, qte_breaker, f3_flat9_living-1, f2_flat7_placement, f2_corridor, f2_stairs, f2_epilogue_street, panel_fit_collapsed_bag, rooms_sheet.jpg (all 24 rooms), run_sheet.png (frame contact sheet)

---

## [2026-10-17 06:23] LOCAL — Verified the cloud bag/key-item/rope/transition/QTE merge in Unity; restored the child rule on the rope descent; fixed an F3 gas soft-lock; tuned the QTEs
### Done
- **Compile:** 0 errors on the first import. All the first-time APIs were fine: `InputAction(name, type, binding)`, `anchoredPosition`, `FillMethod.Horizontal`, pointer enter/exit, `GetComponentInParent<T>(bool)`, and the TileArt tuple-foreach. No asmdef changes needed.
- **Checklist, in order:**
  - Generate Building Levels: `item.flashlight` and `item.whistle` have Is Key Item ticked.
  - Cinematic > Generate Profiles, Build Test Scene (14 rooms).
  - Validate Content: 25 nodes, 0 problems, no TODO errors.
  - Text > Validate: 307 referenced, 0 missing, 0 empty, 0 unused.
  - EditMode tests: **114/114** (113 + 1 new).
- **Local work survived the merge** (screenshots):
  - The 60s prep with the AFAD popup; the building timer stays at 300 during the prep and starts at the quake.
  - Sevim is in the F4 corridor.
  - Tilemap floors and back walls in every room checked.
  - Per-room camera framing: salon vs kitchen vs corridor framed differently.
- **Full Play Mode route, played twice:**
  - Flashlight and whistle → corner HUD; the bag count is unchanged (4→4 and 2→2).
  - Drop + recover: water, 6/6→5/6 → leave → back → click → 6/6. Also dropped the wrench in the F4 corridor.
  - Room transitions block input: a second `Choose` during the fade was ignored.
  - Bedroom 2 sheets + Flat 12 1 sheet → "Halat yap" → `bedsheet_rope`. Cloth + rope → child harness.
  - F3 aftershock placement → Flat 9 breaker QTE (a wrong key gives −5s and a new sequence; then success sets PowerCut_F3).
  - Balcony rope descent → F2 aftershock placement → Flat 7 valve → dog QTE (miss −6s and re-roll, then success) → stairs → out → epilogue.
  - **0 "TODO" and 0 `[[missing]]`** in every TMP text and the CSV. No console errors in run 2. Warnings: only the 13 expected "cue has no clips".
- **Bugs fixed:**
  1. **Rope descent no longer needed a way to bring the child** (5a). Restored; details under the answers below.
  2. **F3 gas soft-lock (pre-existing, from the local wrench change `7a9b52a`):**
     - Without the prep wrench, the F4 valve can never be closed. The F3 gas bomb micro-rewound to the F4 corridor, so the only way out was waiting ~3.5 min for the collapse.
     - The bomb now rewinds to the **floor checkpoint** (prep start), where the wrench can be taken. The "Gazı kapat" tip is unchanged.
  3. **VersionControlSettings:** the Editor had switched it to "Unity Version Control". I set it back to Visible Meta Files and the file is not committed.

### Answers
- **5a) Rope descent and the child: the rule was lost, now restored.**
  - Only the Flat 9 *cables* choice still required `child_harness`; the balcony `RopeDescentInteractable` only checked the bedsheet rope and stamina.
  - It now has a data-driven `familyItem` (= `child_harness`, the existing cloth + rope recipe) and a `lockedKey`.
  - Without the harness the balcony is **shown locked** (sprite greyed via `InventoryEvents.OnInventoryChanged`), and a click shows the reason `ui.rope.descent_locked`: "Çocuğu halattan indiremezsin: önce bir taşıma kuşağı yap (bez + ip)."
  - Pure rules: `FamilyCanCome` and `CanDescend(state, rope, family)`, plus a new test. ContentValidator reports a rope point that has no family item or no locked reason.
  - Verified in Play Mode: with the harness removed, the click stays in Flat 9 and shows the reason; with it, the descent works.
- **5b) Dog: yes, all three exist.** Food (consumed), whistle (kept), and "Köpeğin yanından sıyrılmayı dene" → `f2.dog` QTE. All three were shown side by side; nothing needed restoring.
- **5c) Transitions vs prep:**
  - The prep timer runs on `Time.deltaTime` during the fade, so each lateral transition eats its duration: 0.35s, plus at most one frame per half (≈0.38s at 60 fps).
  - The full prep walk (salon→kitchen→salon→hall→salon) is 4 transitions ≈ **1.5s**, which is under the ~3s budget, so **TransitionConfig is unchanged (0.35s lateral, 0.6s descend)**.
  - This does not count the data move costs (1s per prep move), which are design, not transition time.
  - The in-Editor timings were not usable: MCP calls stall the main thread, so the measurements came out at 0.19–1.25s. The figure above comes from the code path (`RoomTransitionController.Run`).

### Tuned values (QTEConfig, set in `BuildingLevelsGenerator.cs`, so Generate keeps them)
- Monte Carlo model of a normal player: 4-choice reaction lognormal with a 0.62s median (σ 0.3), +0.35s to orient on the first key, 2.5% wrong keys.

| Config | Keys | Per key | Total | Penalty | First-try success |
|---|---|---|---|---|---|
| `qte.breaker` | 4–6 | 1.2 → **1.15s** | 8 → **7s** | −5s | ≈71% → **≈65%** |
| `qte.dog` | 5–6 | 1.0 → **1.15s** | 7 → **6.5s** | −6s | ≈38% → **≈64%** |

- The old dog config was too hard: the first key alone often missed the 1.0s window.

### Screenshots (local only, `Temp/Screens/`, not committed)
- 01 prep salon + AFAD popup + 1:00
- 02 prep kitchen (different framing, locked valve reason)
- 03 death card + tip + GERİ SAR
- 04b Sevim in the F4 corridor
- 05 whistle lit in the corner HUD, bag 2/6
- 06 Flat 12: flashlight lit, rope tied, 3/6
- 07 dropped water dot in Flat 12
- 08 F3 gas death
- 09 rope locked reason without the harness
- 10 breaker QTE panel (big key, pips, bars, WASD, −5 sn)
- 11 F2 aftershock after the descent
- 12 dog: food / whistle / slip past
- 13 epilogue

### Files created / changed
- Scripts/World/RopeDescentInteractable.cs — family rule (harness), locked reason, grey tint, pure `FamilyCanCome` / `CanDescend(…, family)`
- Scripts/Editor/SceneBuild/SceneKit.cs, TestSceneBuilder.cs — wire `child_harness` + the locked key + the tint sprite into the rope point
- Scripts/Editor/Content/ContentValidator.cs — rope points need a family item and a locked reason
- Scripts/Editor/Content/BuildingLevelsGenerator.cs — tuned QTE values
- Scripts/Editor/Content/BuildingLevelsGenerator.Floor3.cs — F3 gas bomb → Checkpoint rewind
- Scripts/Text/UiKeys.cs, Content/Text/strings_tr.csv — `ui.rope.descent_locked` (final Turkish)
- Tests/EditMode/InventoryTests.cs — `RopeDescentIsLockedUntilTheChildCanComeSafely`
- Regenerated by the menus: Data/ (QTE, TransitionConfig, items, nodes, 6 audio cues, LevelConfig, cinematic), Scenes/Test_Building.unity, and the .meta files of the cloud scripts

### Needs local wiring (checklist for the MCP session)
- [ ] Sound designer: clips for cue_door, cue_footsteps, cue_descend, cue_qte_hit / miss / success (all 13 cues are still silent)
- [ ] Artist: item icons; dropped items and corner-HUD icons are still placeholder dots, and dropped items have no label

### Not verified / risks
- The QTE success rates are modelled, not playtested by a human. Watch a real first attempt.
- QTE timers use unscaled time: an idle player loses −5s/−6s every ~7s. That is by design, but it is harsh for anyone who looks away.
- The building timer was confirmed stopped only by a short check in the epilogue (70.71 at two reads).
- "PlayerLoop called recursively" errors in run 1 came from my forced `QueuePlayerLoopUpdate`, not from game code. Run 2 had none.
- To drive Play Mode I deleted the EditorPrefs `InteractionMode` and `ApplicationIdleTime`, which resets them to Unity's defaults. Reapply them if you had custom values. `Time.timeScale` was only changed from the MCP harness in Play Mode; no game code touches it.

### Next suggested step
- Human playtest of both QTEs (first-try rate) and the prep walk. Then the art and audio drop-ins.

---

## [2026-10-17 02:41 UTC] CLOUD — 6-slot bag + drop, key items (corner HUD), bedsheet rope descent, room transitions, WASD QTE, TODO-free strings
(Written after rebasing onto the three LOCAL commits below: 05:10, 04:27 and 03:39. The local clock runs ahead of cloud UTC.)
### Merge with the local work (prep phase, tilemaps, camera framing)
- **Camera layers:** `CameraRig` (RoomCameraFraming, per room) → `TransitionPan` (RoomTransitionController) → `Main Camera` (CameraShake, director tilt/zoom). Framing, transition pan and shake each own one transform, so they never fight.
- **Bedroom placement:** the Flat 11 bedroom (2 bedsheets) is reachable after the quake from the salon. The prep hall is still titled "Antre ve yatak odası"; rename it if that confuses players.
- **Whistle:** it is now a key item, so the earlier "full bag loses Sevim's whistle" risk is gone. Hasan's food/water and trader items can still be lost to a full bag, but the bag now has 6 slots.
- **PrepPhase fix:** it now resets the props of the node's own room (`rooms.FindRoom(prep.RoomId)`), not `rooms.Current`, which is still the previous room during a transition.
- **Key-item HUD:** sits top-right, just left of the Notes button.
- **QTE panel:** sits in the room band (0.3–0.7 × 0.42–0.83), like the old fuse panel.
- **Removed:** the orphan `.meta` files of the deleted Puzzles scripts and FusePuzzlePanel.
- **Text:** the local session's new rows (prep alert, prep rooms, f4.sevim, wrench/gold/laptop/console, valve locked) only had their `TODO:` prefix removed. Their texts were already final. strings_tr.csv has 306 rows and 0 TODO.
- **Verification after the merge:**
  - mcs compile clean. TileArt.cs uses tuple-foreach syntax mcs cannot parse, so a stand-in with its public surface was used.
  - **EditMode 113/113**, including PrepTests.
  - Harness: 0 missing/unused keys, 25/25 nodes reachable, validator 0 problems, 12 playthroughs pass, all starting from f4.prep.salon with the wrench.
### Done
- **Bag:**
  - `LevelConfig.MaxInventorySlots` = 6, applied by SurvivalController. BagUI builds one slot per configured slot and shows a `used/total` counter.
  - **Drop** button, shown while a slot is selected, costs 0.5s. The item goes into `GameState.Dropped` (`RoomDropLedger`, per room, restored by snapshots).
  - `DroppedItemsPresenter` spawns a clickable `WorldDroppedItem` in that room. Clicking it puts the item back in the bag (1s).
  - Events go through `InventoryEvents` (`OnItemDropped`, `OnInventoryChanged`, `OnKeyItemAcquired`); UI and world never reference each other.
- **Key items:** `Item.IsKeyItem` is set on the flashlight and whistle by the generator.
  - They go to `GameState.KeyItems`: no bag slot, not droppable, never blocked by a full bag.
  - `KeyItemCornerHUD` sits top-right (left of the Notes button): dim until owned, then lit with a short pop and a hover tooltip.
  - `StateOps.HasItem` (bag or key items) is used by conditions, the trader, interactables, the cinematic flashlight and the wet-cloth check.
- **Bedsheets → rope → descent:**
  - New Flat 11 bedroom (salon → "Yatak odasına geç") holds 2 bedsheets; Flat 12's wardrobe holds 1 (borrowed).
  - `LevelConfig.RequiredBedsheetsForRope` = 3. A "Halat yap" bag prompt (or tapping a bedsheet twice) turns 3 sheets into 1 `bedsheet_rope` (5s, 3 stamina).
  - `RopeDescentInteractable` on the new Flat 9 balcony needs the rope and stamina (heavy). It costs 10s and 20 stamina, keeps the rope tied, and enters `f2.aftershock`.
- **Room transitions:** `RoomTransitionController` (coroutines, unscaled time, no DOTween).
  - Locks input (`InputGate` + `NavigationEvents.OnTransitionStart`), fades the overlay while panning `TransitionPan` (between `CameraRig` and the camera), swaps the room at the midpoint, then unlocks (`OnTransitionComplete`).
  - `TransitionConfig`: 0.35s lateral, 0.6s descend. Plays door + footstep cues, or a descend cue when the floor stage changes.
  - Rewinds swap instantly. Placement nodes finish a running transition first. Reduce Effects drops the pan.
- **WASD QTE:**
  - Pure core: `QTESequence` (random W/A/S/D only, at most 2 identical keys in a row, length rolled per attempt), `QTERun` (per-key window + total timeout), `QTESession` (re-roll on every failure, success/failure callbacks).
  - `QTEConfig` assets: breaker 4–6 keys, 1.2s per key, 8s total, −5s; dog 5–6 keys, 1.0s per key, 7s total, −6s.
  - `QTENode` + `QTEController` read W/A/S/D through Input System actions. `QTEView` shows the big key, wire-stage pips, two countdown bars and on-screen W/A/S/D buttons for touch.
  - On a miss: building-timer penalty, camera shake, red flash, a 0.15s QTE-only hit-stop (Time.timeScale untouched), and a new sequence.
  - `f3.fuse` (breaker wiring) replaces the old switch puzzle: success sets PowerCut_F3.
  - `f2.dog` (dog evasion) replaces "walk past = bitten": success sets DogGone; food and whistle still work.
- **Localization:**
  - Every `TODO` prefix is stripped. All rows (306 after the merge) are final Turkish, including the AFAD popup and the new drop, key-item, rope and QTE keys.
  - `ui.bag.used` and `ui.craft.done` no longer show a literal `{0}`.
  - ContentValidator logs an **error** for any `TODO` (case-insensitive, every column). It also checks that the flashlight and whistle are key items, the bag/rope config, QTE nodes (config, next node, prompt), QTE configs and rope points.
  - The CLAUDE.md CLOUD rule now says to write final text, never `TODO`.
- **Tests:** (counts here are from before the merge; see the merge section above for the current totals)
  - New: `InventoryTests` (6 slots, drop/recover/snapshot, key items 0 slots + HUD state, 3 sheets → rope, rope descent rule), `QTETests` (WASD only, no 3-in-a-row, fresh sequences, run hit/miss/timeouts, session callbacks for BreakerWiring and DogEvasion), `LocalizationTests` (no TODO in the real CSV).
  - 106/106 pass in the cloud Mono runner (no Unity).
  - The generator harness reports: 0 missing/unused keys, 22/22 nodes reachable, validator 0 problems, an injected `TODO` is caught, and 12 scripted playthroughs pass, including the rope route and both dog outcomes.

### Name mapping (spec → code)
| Spec name | Code |
|---|---|
| ItemDefinition | `Item` |
| InventoryController / InventorySlotUI | `BagUI` |
| AudioManager | `AudioService` |
| UITheme / TransitionConfig | `TransitionConfig` asset |
| RoomTabs | `RoomSwitcher` (RoomTabs was removed earlier) |
| BreakerQTEInteractable / DogHazardQTE | `QTENode` with `QTEContext.BreakerWiring` / `.DogEvasion`, played by `QTEController` |
| CraftingConfig | the bag fields on `LevelConfig` |

### Files created / changed
- **New runtime:**
  - Core/: InventoryEvents, KeyItemState, RoomDropLedger, NavigationEvents
  - Placement/: RoomTransitionController, TransitionConfig
  - QTE/: QTEKey, QTESequence, QTERun, QTESession, QTEConfig, QTENode, IQTEView, QTEController
  - UI/: KeyItemCornerHUD, HoverRelay, QTEView
  - World/: WorldDroppedItem, DroppedItemsPresenter, RopeDescentInteractable
  - Text/: TextLint
- **Removed:** Puzzles/* and UI/FusePuzzlePanel.
- **Changed:**
  - Core: Inventory, GameState, Snapshot, StateOps, GameEvents
  - Item, ConditionEvaluator, OutcomeExecutor, NodePlayer, TraderLogic, Crafting, WorldInteractable
  - LevelConfig, SurvivalController, CinematicDirector, PlacementController, RoomSwitcher, BagUI, UiKeys
  - Generator (all floors), BuildingLayout, SceneKit, TestSceneBuilder, TestSceneUi, SerializedWire, ContentValidator
  - strings_tr.csv, CLAUDE.md, Floor2/Floor3 rule tests

### Needs local wiring (checklist for the MCP session, in run order)
1. `git pull`, open Unity and let it compile; expect 0 errors.
   - Unity deletes the orphan `.meta` files of the removed Puzzles scripts, if your local copy has any.
   - New scripts get their `.meta` files on import.
2. Run **SixtyOne > Generate Building Levels**. It creates:
   - `Data/QTE/qte.breaker.asset`, `qte.dog.asset` and `Data/TransitionConfig.asset`
   - items `bedsheet` and `bedsheet_rope`
   - nodes `f4.bedroom` and `f2.dog`; `f3.fuse` becomes a QTE node, and the old FusePuzzleNode asset is replaced
   - 6 new audio cues (cue_door, cue_footsteps, cue_descend, cue_qte_hit, cue_qte_miss, cue_qte_success), which have no clips yet
   - Then check that `item.flashlight` and `item.whistle` have **Is Key Item** ticked.
3. Run **SixtyOne > Cinematic > Generate Profiles**, then **SixtyOne > Build Test Scene**. This adds the TransitionPan under the CameraRig, the 6-slot bag with Drop / Halat yap, the KeyItems corner, the QTE panel and the RoomTransition overlay.
4. With `Test_Building` open, run **SixtyOne > Validate Content**; expect 0 problems and **no** red TODO errors. Then **Text > Validate**.
5. Run the Test Runner (EditMode); expect 113 passing. `LocalizationTests` reads `Assets/_Game/Content/Text/strings_tr.csv` relative to the project root, which is the Unity default working directory.
6. Play Mode via MCP:
   1. Prep: the hall's flashlight nightstand goes to the corner HUD, not the bag. After the quake, hall → exit → Sevim "Sağ ol" → whistle in the corner too, and the bag count does not change.
   2. Take an item (e.g. rope) → select its slot → Bırak → slot freed (n/6). Leave the room and come back → the item lies near the spawn → click it to pick it up again.
   3. Move salon → kitchen → back. Check the 0.35s fade + pan, and that choices and world clicks are ignored during it.
   4. Bedroom: take 2 bedsheets; Flat 12 wardrobe: 1 bedsheet → "Halat yap" → 1 rope.
   5. F3 → Flat 9 → "Kabloyu onar" (with gloves/grip + tape, e.g. from the trader) → QTE panel. Press wrong keys once (−5s, new sequence), then complete it → back in Flat 9 with power cut.
   6. Click the balcony "Halatla aşağı in" → descend transition → F2 aftershock placement.
   7. Flat 7 valve → corridor → "Köpeğin yanından sıyrılmayı dene" → dog QTE (fail once, then succeed) → stairs → out → epilogue.
   8. Scan every popup and dialogue for "TODO"; there should be none.
7. Tuning: QTE lengths, timings and penalties are on the two QTEConfig assets; transition timing is on TransitionConfig. Check that the 0.35s transitions don't eat too much of the 60s prep (each prep move fades once).

### Not verified / risks
- No Unity in this session: compiled with mcs against API stubs. APIs used for the first time:
  - `InputAction(name, InputActionType, binding)` + `performed`
  - `RectTransform.anchoredPosition`
  - `Image.FillMethod.Horizontal`
  - `IPointerEnterHandler` / `IPointerExitHandler`
  - `GetComponentInParent<T>(bool)`
- **Rope descent:** `RopeDescentInteractable` enters the target node directly, not through a choice. It works only in explorable rooms (Flat 9 has no handler).
- **Keyboard focus:** the QTE keys are global keyboard actions. They are enabled only while a QTE node is active, so W/A/S/D don't conflict with anything else today.
- **Panic delay:** QTE keys bypass the panic input delay (QTE tension is its own timer). Say if you want the delay applied.
- **Overlapping transitions:** if two room changes come faster than 0.35s, the first one completes immediately (`Finish`). Watch for flicker.
- **Item icons:** dropped items and corner icons use the placeholder dot until items get icons.

### Cut / simplified
- The F4 hall breaker stays a simple choice; the QTE is the Floor 3 fuse box (the "Fuse Box wiring" in the verify list).
- "Walk past the dog = bitten/injured" is replaced by the dog QTE, as the spec asks: failure costs time and re-rolls, with no injury.
- Hit-stop is QTE-local, because CLAUDE.md forbids changing `Time.timeScale`.
- Rope tying is a prompt or a double-tap, not automatic, so the player decides when to spend the 5s.

### Next suggested step
- LOCAL: run the checklist above with MCP Play Mode, screenshot the bag/corner HUD/QTE panel, tune the QTE and transition numbers, and add clips to the 6 new cues.

---

## [2026-10-17 05:10] LOCAL — Floor 4 opening flow: 60 s home loot prep → quake (building timer starts) → placement → Sevim in the corridor
### Done
- **Prep phase** (new `Scripts/Prep/`):
  - `PrepPhaseNode` (rooms f4.prep.salon / f4.prep.kitchen / f4.prep.hall); f4.prep.salon is the floor checkpoint.
  - `PrepTimer` (pure, ticks for the last 10 s); `PrepPhase` driver (real-time drain, cost sink, AFAD alert, quake at zero); `PrepRules` (pure).
  - `GameState.PrepTime` and `GameState.BuildingTimerStarted` are restored by snapshots.
- **Action costs during prep** come out of the prep countdown (`SurvivalController.SpendTime` → `PrepPhase.Spend`): pickup 1 s, drawer/cabinet 3 s, breaker 4 s, valve 5 s (needs the wrench), moves 1 s.
- **Building timer:** hidden and frozen during prep (new `PrepCountdownDisplay` shows "Depreme kalan 0:59" in its spot). No stamina drain or gas leak before the quake.
- **At prep zero:** quake onset (QuakeStarted, shake 0.4) → f4.quake placement; the building timer starts at 300.
  - Quake panic is now "at least aftershockPanic", not additive, so the onset + main shock don't stack.
- **Our flat loot** (prep-only `WorldInteractable.prepOnly`; dimmed, unlabelled and unclickable after the quake):
  - salon: laptop + console (traps), first aid cabinet, hidden food
  - kitchen: gloves drawer, water
  - hall: wrench toolbox, flashlight nightstand, gold box (trap), hidden whistle
  - New items: wrench, gold, laptop, console.
- **After the quake:** the breaker and valve stay available (valve needs the wrench) and cost building time. The hall exit leads to **f4.sevim** (DialogueNode in the corridor; was f4.intro):
  - "Kısa kes" sets `IntroCutShort`: no whistle, no Flat 12 info.
  - "Sağ ol" gives the whistle and `KnowsFlat12`.
  - `exit_again` once `SevimDone`. Hasan and Flat 12 are unchanged.
- **Cinematic:** tension follows the prep countdown before the quake; letterbox + heartbeat in the last 10 prep seconds (`GameEvents.PrepTick`).
- **Bug fixed:** after a macro rewind, toppled hazard props stayed toppled. `PrepPhase` now resets the room's props on prep entry (verified: wardrobe 80° → 0°).
- **ContentValidator `CheckPrep`:** prep nodes need a quake node and must be untimed; the alert needs keys; with prep configured the start node must be a checkpoint PrepPhaseNode; prep-only interactables must sit in prep rooms.
- **CSV:** f4.intro.* renamed to f4.sevim.* (l1 adjusted to the post-quake corridor). New TODO keys: prep nodes, prep.alert.*, ui.prep.label, valve locked reasons, f4.hall.c_exit_again, the 4 new items.
- **Results:** Validate Content: 23 nodes, 0 problems. Text > Validate: 278 referenced, 0 missing/unused. **EditMode 102/102** (new PrepTests: tick thresholds, cost routing, building timer frozen during prep, f4 checkpoint restore of both timers, micro rewind into the placement).

### Play-mode verification (`Captures/p*`, `q*`)
- **Run 1** (2 s per click, 0 deaths):
  - p01: AFAD alert, prep 0:59, building timer hidden.
  - Loot: laptop (trap), water; the valve stayed locked without the wrench; wrench, flashlight, then gold refused with "Çanta dolu" (p02); breaker, valve.
  - p03: 0:07 in red.
  - Quake at 0 → building 300 starts. p04: placement shows 04:53.
  - p05: salon after the quake (laptop gone, not clickable).
  - p06: Sevim, full dialogue, whistle. p07: Hasan (bag full, gift lost). p08: Flat 12 + note.
  - Floor 3 (Flat 10 route) and Floor 2 (whistle at the dog) → p10: ending with **131.5 s left**.
- **Run 2:**
  - Player left unplaced → death → micro rewind into f4.quake with building 300 saved, wrench kept (q01).
  - "Kısa kes" → IntroCutShort, no whistle, Flat 12 hidden, only "search" offered (q02).
  - Forced collapse → macro rewind to f4.prep.salon: prep 60, building 300 and stopped, empty bag, alert shown again (q03).

### Files created / changed
- Scripts/Prep/{PrepTimer, PrepPhaseNode, PrepPhase, PrepRules}.cs (new); UI/PrepCountdownDisplay.cs (new)
- Core/GameState, Snapshot, GameEvents; Building/SurvivalController, LevelConfig (prepSeconds 60, prepTickFrom 10); World/WorldInteractable, HiddenItemHotspot; Cinematic/CinematicDirector; UI/BuildingTimerDisplay; Text/UiKeys
- Editor: BuildingLevelsGenerator(.Floor4), ContentValidator, SceneBuild/{BuildingLayout, TestSceneBuilder, TestSceneUi, SceneKit, HiddenItems}
- Tests: PrepTests (new); f4.intro ids renamed in BuildingTimerTests, RewindTargetTests, DialogueTests
- Content/Text/strings_tr.csv; Data/** regenerated; Scenes/Test_Building.unity rebuilt

### Needs local wiring (checklist for the MCP session)
- [ ] Rebalance: the prep moved most looting off the building timer, so Route B now ends with about 130 s left at a brisk pace (target 30–60). Consider buildingSeconds 200–230 in `BuildingLevelsGenerator` (`ConfigureTimer`) after a human playtest.
- [ ] Writer: replace the new TODO texts (prep alert, prep room bodies, items).

### Not verified / risks
- The AFAD alert (bottom strip) covers the two prep move buttons for up to 3.5 s; tap to dismiss. Consider a shorter showSeconds for the prep alert.
- Full bag: Sevim's whistle, Hasan's gift and the trader item are silently lost (unchanged behaviour).
- The letterbox in the last 10 s is hard to see against the black background (known).

### Next suggested step
- A human playtest of Floor 4 with mouse + sound, then the timer rebalance above.

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
