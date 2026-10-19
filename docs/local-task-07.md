# LOCAL TASK 07 — Character/wall scale, LED building timer, main menu + ESC pause, inventory skin, cinematic subtitles, doors

Read `CLAUDE.md` and the newest `HANDOFF.md` entry first. Unity is open with MCP for Unity.
Small steps; the project must compile after each one. Take a screenshot before and after every visual change.
For Play Mode checks, set Interaction Mode = No Throttling and restore it at the end.
Every new player-facing string goes in `strings_tr.csv` with final Turkish text (never TODO).

## 1. Characters are taller than the walls (do this first)
Right now the player is `CharacterArt.PlayerHeight` = 0.9u, while a wall face rises only `RoomTiles.WallTopAboveEdge` = 160/254 ≈ 0.63u above the floor edge. The family towers over the rooms.

Fix:
- **Stack the walls two rows high.**
  - Add `wallRows` (default 2) to `RoomVisualConfig`. `RoomTiles.Walls` places each segment `wallRows` times, each row offset up by exactly the face height (160 px / PPU), with the same sorting order.
  - Check the seam between rows (no gap, no double outline). If there is a 1 px line, overlap the rows by 1–2 px.
  - Windows go only in the upper row; the lower row stays plain or cracked. Cracked variants may appear in either row.
  - Door / wall gaps stay open for the full height.
- Lower `PlayerHeight` to about 0.8, so the player is roughly 60–65% of the wall height. Wife and child keep their ratios; Sevim keeps hers.
- Update every place that assumes a single wall row:
  - the camera framing bounds (`maxY` in RoomTiles / `RoomCameraFraming`) so the wall tops are not cropped;
  - the tile transition pieces, so both rows rise and drop;
  - any prop placed "on the wall" (windows, door lintel, fuse box).
- Re-run Generate Building Levels → Build Test Scene.
- Screenshot the salon, the F4 corridor and Flat 7 with the family standing next to a back wall.
- If two full rows look too tall, try 1.5 rows (the upper row cropped by a SpriteMask, or a half-height sprite slice) and pick whichever reads best. Explain the choice in the handoff.

**Doors and elevator art** (`Art/Concept/door.png`, `Art/Concept/asansorkapı.png`, both already transparent, 832x1216):
- Import as Sprite, pivot bottom-centre, sized so the door is about 1.15× the player's height.
- Use `door.png` on every `Door` in a wall gap. Flip X for the right-wall gaps; the art faces the left wall.
- Use `asansorkapı.png` on the elevator hotspots (F4 corridor, F2 stairs, F2 corridor).
- Doors sit flush in the gap, sort with the wall, and keep their colliders and approach points.

## 2. Building timer = green LED clock (font `Art/Fonts/TrueType (.ttf)/E1234-Regular.ttf`)
- Make a TMP SDF font asset from E1234-Regular (digits, colon and space are enough; padding high enough for a glow).
- Restyle `BuildingTimerDisplay` as a digital alarm clock: a small dark panel (near-black, slightly green-tinted, rounded corners, thin dark border) at the top centre where the timer is now.
  - Digits `MM:SS` in LED green (`#4CFF6A`), with a soft green glow (TMP glow/underlay on a material instance).
  - Behind the digits, an unlit ghost `88:88` in the same font at about 12% alpha, so it reads as a real 7-segment display.
  - The colon blinks once per second while the timer runs, and stays solid while timers are paused (transitions).
  - At 60s it turns amber (`#FFB020`) and pulses as it does today. In the last 30s it turns red (`#FF3B30`) with a slight flicker, alongside the existing alarm + tick.
  - The `spentLabel` (time cost popup) keeps Inter, coloured to match.
- The prep countdown (`PrepCountdownDisplay`) uses the same LED style in amber, so the two clocks read as related.
- Reduce Effects disables the flicker, but not the colour change.

## 3. Main menu + ESC pause menu
**Main menu:** a new scene `Scenes/MainMenu.unity`, built by a MenuItem (`SixtyOne > Build Main Menu`), not by hand. Put it first in Build Settings and `Test_Building` second.
- Background: dark, with a slow subtle camera drift over a blurred shot of the salon. A static darkened screenshot is fine.
- A big "61 SECONDS" title in EB Garamond SemiBold (`Art/Fonts/EBGaramond/`), with a thin line under it.
- Buttons:
  - `Başla`
  - `Ayarlar`: Master / Müzik / Efekt volume sliders, and the Reduce Effects toggle (`Efektleri Azalt`). Save them in PlayerPrefs, apply them through AudioService / mixer groups, and share them with the pause menu.
  - `Çıkış`
- Use `AudioCue` click sounds, and play the calm prep music track in the menu.

**ESC pause** (in `Test_Building`; Esc toggles it, using the Input System):
- Show a dim full-screen overlay and a centred panel: `Devam Et`, `Ayarlar` (the same settings panel), `Ana Menü` and `Oyundan Çık`. Ana Menü and Oyundan Çık each need a confirmation: `Emin misin? İlerleme kaybolacak.` with `Evet` / `Hayır`.
- While paused:
  - `Time.timeScale = 0`. This is the ONE allowed place to touch timeScale; update the CLAUDE.md coding rule to say so.
  - `AudioListener.pause = true`; menu click sounds use `ignoreListenerPause`.
  - World input is blocked.
  - Make sure the building timer, the prep countdown, stamina, poison, exhaustion, QTE timers and the dialogue line costs all stop. They use `Time.deltaTime` via `TimerDelta`; verify each one.
- Esc while a QTE is running pauses it too; resuming continues the same QTE.
- Every new text gets a key in the CSV (`ui.menu.*`, `ui.pause.*`, `ui.settings.*`).

## 4. Inventory UI skin (sheet `Art/uı/examples.png`, 480x320, pixel art)
- Import with Point filter, no compression, no mips, and slice it.
- Use the **red backpack** (top row, second from the left) as the bag frame, with its blue rounded cell as the slot sprite. Set 9-slice borders on the frame and use integer scaling (×3 or ×4) so the pixels stay crisp.
- BagUI shows 6 slots (`LevelConfig.MaxInventorySlots`) in a 3×2 grid inside the backpack frame.
  - The item icon sits centred in the slot at about 80% of the cell.
  - The selected slot gets a highlight; the combine target a second highlight.
  - Drop / use stay as they are.
- `KeyItemCornerHUD` uses the small green-framed panel (top row, fourth from the left) as its frame.
- Do not change the bag logic; this is a reskin only. Note in the handoff if the pixel art clashes with the cartoon art. A softer option is to scale the frame up and keep the icons smooth.

## 5. Cinematic subtitles for dialogue (reference: a dark bottom band, a name above the text, a ▼ continue mark)
Create a `SubtitleView` (UI) and use it for every `DialogueNode` line (Sevim, Hasan, Cemil and the wife's lines), and also for narration/body text with no speaker.
- **Layout:**
  - A full-width bottom band, about 22% of the screen height, with no box or border.
  - A vertical gradient from transparent at the top to black at 85% at the bottom.
  - The text column starts at about 24% of the screen width from the left, with a max width of about 55%, left-aligned.
- **Speaker name:** EB Garamond SemiBold, about 34 px at 1080p, warm white `#F2EDE4`.
- **Line text:** EB Garamond Regular, about 26 px, `#E8E4DC`, line spacing about 1.15, a subtle 1 px dark shadow, at most 3 lines.
- Make TMP SDF assets for both EB Garamond weights through the existing `SixtyOne > Text > Generate Fonts + Theme` menu. They must include all Turkish characters (çğıöşüÇĞİÖŞÜ) and the usual punctuation. Add them to `UITheme`.
- **Behaviour:**
  - The text typewrites (reuse `TypewriterText`) and the voice blips keep working.
  - When the line is complete, show a small `▼` after the last word that bobs gently, about 4 px at 1.2 Hz.
  - A click, Space or Enter first completes the line, then advances to the next one.
  - The line time costs stay exactly as they are; the subtitle is visual only.
  - Choices (`Kısa kes` etc.) appear above the band in the existing ChoicePanel. The ChoicePanel no longer shows dialogue text itself.
  - Fade the band in and out (0.2s). Hide it during room transitions.
- Reduce Effects removes the bob.

## 6. Verify
- 0 compile errors; EditMode all green; Validate Content and Text > Validate show 0 problems.
- Play: main menu → Başla → prep (amber LED) → quake → building timer (green LED, then amber at 60s, red at 30s) → Sevim dialogue in subtitles with ▼ and blips → Esc pause (everything frozen, audio paused) → resume → bag with the backpack skin → doors and elevator art in place → Esc → Ana Menü → confirm.
- Screenshots: the walls with the family before and after, the LED timer in all three colours, the main menu, the pause menu, the bag, a subtitle line.
- Restore Interaction Mode. Write the HANDOFF entry, then commit and push to `cloud/foundation`.
