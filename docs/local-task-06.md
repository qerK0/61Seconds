# LOCAL TASK 06 — Exhaustion death + darkening, wire the team's audio, place the new art

Read `CLAUDE.md` and the newest `HANDOFF.md` entry first. Unity is open with MCP for Unity. Work in small steps and keep the project compiling after each one.
For Play Mode checks, set Preferences > General > Interaction Mode = No Throttling first, then restore it at the end.

## 1. Exhaustion (hunger/thirst) death with a slow screen fade — like gas poisoning
Gas poisoning already rises, darkens the screen (`PoisonOverlay`) and kills at max. Hunger currently never kills: stamina 0 only locks heavy actions and makes actions cost more.

Add exhaustion that kills just as slowly:
- Add an `Exhaustion` value (0..1) to GameState, restored by snapshots like Poison. Put the pure math in `SurvivalModel` (`StepExhaustion`).
  - It rises only while stamina is 0 (`LevelConfig.exhaustionPerSecond`, default ~0.03, about 35s from 0 to death).
  - It falls quickly once stamina is above 0 (`exhaustionDecayPerSecond`, default ~0.2).
- Eating or drinking (food/water use) restores stamina as today, so exhaustion starts falling at once.
- **Screen:** the darkening is driven by `max(poison, exhaustion)`.
  - Reuse `PoisonOverlay`, but give it a neutral black tint for exhaustion (gas keeps its own look).
  - Add a slow heartbeat-like pulse, and a vignette / CinematicDirector darkening if that is cheap.
  - Reduce Effects keeps the fade but drops the pulse.
- **At 1: death.** RewindTarget = Checkpoint (same as gas).
  - New text key `death.exhaustion`: `Dizlerinin bağı çözüldü. Aç ve susuz, enkazın içinde bir adım daha atamadın.`
  - New tip `tip.supplies`: `Deprem çantanda kişi başı en az 3 günlük su ve bozulmayan yiyecek bulunsun.` (title `tip.supplies.title`: `Su ve yiyecek hazır olsun`). Create the Tip asset through the existing generator or a MenuItem, not by hand.
- **Tune stamina** so exhaustion is a real threat inside the 300s building timer. Right now drain is 0.15/s with max 100, which never reaches 0 in time.
  - Target: a player who never eats or drinks hits 0 stamina at about 150–180s with normal heavy actions; one food or water item buys roughly 60–90s.
  - Report the final numbers in the handoff.
- Make the stamina bar flash or pulse red at 0 so the player knows why the screen is darkening.
- EditMode tests for `StepExhaustion`:
  - rises only at 0 stamina;
  - decays when stamina is above 0;
  - clamps to 0..1;
  - the snapshot restores it.

## 2. Wire the team's audio (files are in `Assets/_Game/Voice/`)
Move the clips to `Assets/_Game/Audio/` (via AssetDatabase so the .meta files move with them). Import settings:
- short SFX: Decompress On Load;
- music, ambience and long loops: Streaming, Vorbis.

Map them like this:

| File | Use |
|---|---|
| `afad.mp3` | `cue_afad_alert` |
| `7.1deprem.wav` | opening quake (F4 placement) |
| `6.2deprem.ogg` | F3 aftershock |
| `6.9deprem.wav` | F2 strong aftershock |
| `hırtladenkgelmedenöncec.mp3` | the pre-quake rumble / low growl in the last seconds before a quake hits (prep end + placement countdown), or `cue_placement_alarm` if simpler |
| `heavyobjectfall.wav` | `cue_furniture_crash` (hazard topple/drop) |
| `eşya alma.wav` | `cue_pickup` |
| `kapı acma kapama.wav` | `cue_door` |
| `3lu grup yürüyüşü.mp3` | `cue_footsteps` (while the player runs; loop or retrigger, stop on arrival) |
| `craft sesi.wav` | new cue `cue_craft` (successful combine, incl. bedsheet rope) |
| `duduk.wav` | new cue `cue_whistle` (whistle use + dog whistle option) |
| `havlama.wav` | new cue `cue_dog_bark` (dog QTE / dog corridor enter) |
| `hrrlayankopke.mp3` | new cue `cue_dog_growl` (dog idle loop in the F2 corridor, quiet) |
| `elektrik kacagı.wav` | new cue `cue_electric` (F9 fuse box: QTE fail + electrocution death) |
| `heartbeat.wav` | new cue `cue_heartbeat` (loop; volume follows panic above the delay threshold, and also exhaustion/poison) |
| `damlayan su.wav` | ambience loop for the damaged building (all post-quake indoor rooms, quiet) |
| `Start_muzıgı_sakin_anlar.mp3` | music: prep phase (calm, before the quake) |
| `ilk deprem sonrası sarkı ve sigorta.mp3` | music: after the first quake (Floor 4 → Floor 3 tension) |
| `dışarı varıncaki ses.mp3` | street / epilogue sound |
| `kadın dialog.mp3` | dialogue voice: female speakers (Sevim Teyze) |
| `erkekkonuşma.mp3` | dialogue voice: male speakers (Hasan Amca, Cemil Bey) |

Build only what is missing, and keep it small:
- **MusicService:** two AudioSources with a crossfade, driven by GameEvents/stage.
  - Prep → calm track. Quake → stop. After the opening quake → tension track. Street / epilogue → outro.
  - A rewind to a checkpoint restarts the right track for that stage.
  - Loop where it makes sense.
- **Ambience:** a per-room or per-stage loop (dripping water after the quake, street outside).
- **Dialogue voice:** while `TypewriterText` types a line, play short random slices of the speaker's voice clip.
  - Use a random start offset, about 0.08–0.15s per blip, every 2–3 characters, skipping spaces and punctuation.
  - Pitch per speaker: Sevim higher (~1.15), Hasan lower (~0.85), Cemil ~1.0.
  - Put the speaker → (clip, pitch) mapping on a small ScriptableObject.
  - If the clips are long recorded speech, slicing random 0.1s windows is the intended "animalese" effect. Stop the voice when the line finishes or is skipped.
- Add volumes to the cues so the mix is sane: music under SFX, ambience lowest, dialogue blips clearly audible.

### 2b. Procedurally generated SFX (`Assets/_Game/Voice/Generated/`)
These fill every cue the sound designer has not delivered yet. They were synthesized by `tools/gen_sfx.py` (Python + numpy/scipy, deterministic seed). Move them to `Assets/_Game/Audio/Generated/` together with the rest and wire them:

| File | Use |
|---|---|
| `sfx_death_sting.wav` | `cue_death_sting` (dramatic low hit + a slightly absurd slide-whistle drop) |
| `sfx_rewind.wav` | `cue_rewind` (VHS tape rewind; play it with the RewindFX, longer/louder for macro rewinds) |
| `sfx_glass_shatter.wav` | `cue_glass_shatter` (window hazards) |
| `sfx_choice_click.wav` | `cue_choice_click` |
| `sfx_denied.wav` | `cue_denied` |
| `sfx_qte_hit.wav` | `cue_qte_hit` |
| `sfx_qte_miss.wav` | `cue_qte_miss` |
| `sfx_qte_success.wav` | `cue_qte_success` |
| `sfx_building_alarm_loop.wav` | `cue_building_alarm` (loop, last 30s) |
| `sfx_countdown_tick.wav` | `cue_countdown_tick` (once per second in the last 30s and during the prep countdown's last 10s) |
| `sfx_descend.wav` | `cue_descend` (rope / cable descent) |
| `sfx_fire_explosion.wav` | `cue_fire` (lighter in gas = explosion, kitchen fire death) |

Use pitch variation (±5%) on the clicks, ticks and QTE blips. Keep these at sensible volumes: the click and tick are quiet, the death sting and explosion are loud.
If the sound designer later delivers a file for the same cue, just swap the clip on the cue asset.

**If you need any other small SFX** while wiring (for example eat/drink, valve turn, bag open, cloth tear, cough), generate it the same way:
- add a function to `tools/gen_sfx.py` and run it with the local Python (`pip install numpy scipy` if needed);
- write the WAV into `Assets/_Game/Audio/Generated/` (44.1 kHz, 16-bit mono, peak about -1 dBFS);
- wire it to a cue and list it in the handoff.

Do not spend more than a few minutes per sound.

## 3. Place the new art
- **Item icons** (`Assets/_Game/Art/items/`): import as Sprite (2D, single, alpha is transparency), then assign the icon on each item asset and use it for the world pickup sprite too:

  | Icon file | Item |
  |---|---|
  | bread | food |
  | water bottle | water |
  | flashlight | flashlight |
  | gloves | gloves |
  | sheet | bedsheet |
  | bant | tape |
  | golds | gold |
  | lighter | lighter |
  | medkit | first_aid |
  | rope | rope |
  | sheet rope | bedsheet_rope |
  | whsitle | whistle |
  | wrench | wrench |

  - `crowbar` and `mask` are unused; leave them.
  - Items with no icon yet keep the placeholder: cloth, wet_cloth, child_harness, insulated_grip, laptop, console.
  - Make sure BagUI, KeyItemCornerHUD and world pickups show them at a readable size.
- **Sevim Teyze idle** (`Assets/_Game/Art/char/SEVIM_IDLE/frame0000–0008.png`, 9 frames):
  - Import through the same pipeline as the family (extend `Import Character Frames` or add a sibling menu).
  - Give the Sevim hotspot in the F4 corridor a looping idle animation (~8–10 fps), feet on the floor, scaled to match the family.
  - Code must not depend on the frame count.
- **Walls/floor** (`Art/Concept`):
  - `pencereler *.png` are stone wall segments with windows. Use them where the layout has `windows:` on stone (apartment) walls.
  - The two `WhatsApp Image ... aa/aan.png` files are cracked stone walls. Use them as random variants on post-quake corridor walls (F3/F2).
  - `parke.png` is a parquet floor. Compare it with Wood Floor 13 and use it in our flat if it tiles cleanly, otherwise leave it.
  - Check pivots and seams, and screenshot before/after.

## 4. Verify
- Compile with 0 errors. Run EditMode, expecting all of them green (138 + new). Run Validate Content and Text > Validate with 0 problems.
- Play: prep (music, AFAD, footsteps, door, pickup) → quake (7.1 rumble, crash) → Sevim dialogue (female blips, idle animation) → Hasan (male blips) → the F3 aftershock (6.2) → the dog (growl, bark, whistle).
- Let stamina hit 0 once: the bar pulses, the screen slowly darkens with the heartbeat, then death → exhaustion card + tip → checkpoint rewind.
- Screenshot the bag with real icons, Sevim, a window wall and a cracked corridor wall.
- Restore the Interaction Mode preference. Write the HANDOFF entry, then commit and push to `cloud/foundation`.
