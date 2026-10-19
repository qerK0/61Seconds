# LOCAL TASK 15 — Flashy main menu ("İSTANBUL AFET BÖLGESİ") + end screen ("SİMÜLASYON SONA ERDİ")

Read `CLAUDE.md` and the newest `HANDOFF.md` entry first. Unity is open with MCP.
**Scope:** only the main menu scene and the end screen. Don't touch gameplay.

The mockups are `docs/ref/ref_menu_mockup.jpg` and `docs/ref/ref_end_mockup.jpg`. Look at both. Match their composition and mood, but build them from real UI elements. The mockups' baked-in text, buttons and gibberish "stats" lines must NOT appear in the game.

**Assets prepared for you:**
- `Art/Menu/menu_bg.png`: the Istanbul map, 1920×1080, with the left UI column already blurred and darkened.
- `Art/Menu/end_bg.png`: the ruins, 1920×1080, with the lower area dark for text.
- Fonts (make TMP SDF assets with full Turkish coverage: çğıöşüÇĞİÖŞÜ, plus `'` and `→`):
  - `Art/Fonts/Oswald/Oswald-Bold.ttf` and `Oswald-Medium.ttf`: titles and menu buttons;
  - `Art/Fonts/IBMPlexMono/IBMPlexMono-Regular.ttf` and `-Bold.ttf`: end-screen body, HUD-like readouts.
- All text goes in strings_tr.csv (keys `ui.menu.*`, `ui.end.*`), in final Turkish.

## 1. Main menu (`Scenes/MainMenu.unity`, rebuilt by the existing Build Main Menu MenuItem)
**Layout at 1920×1080** (anchor-scaled, check at 1280×720 too):
- **Background:** menu_bg with a slow Ken Burns effect (scale 1.0 → 1.06 and a small pan over about 40s, ping-pong; unscaled time).
- **Title** at left, upper third:
  - `İSTANBUL`: Oswald Bold, about 150 px, off-white `#E9E6E1`, with a dark red inner shadow / outline `#5A0A0A` and a rough, distressed look. A noise/scratch mask via the material is fine; a subtle texture is enough.
  - `AFET BÖLGESİ` under it: Oswald Medium, about 64 px, letter-spacing 0.08em.
  - About every 5–8 s, a short **glitch** (0.15 s): RGB split, a horizontal slice offset and a flicker. Reduce Effects turns it off.
- **Buttons** (left column, under the title, width about 430):
  - `Oyunu Başlat`: the PRIMARY button. A red panel `#B3121A` → `#6E0A0F` vertical gradient, a 2 px bright red border with a glow, metal corner brackets (9-slice), Oswald Medium 46, a warm off-white label.
    - Idle: a slow pulse of the glow.
    - Hover: the glow is stronger, a white diagonal shine sweeps across, and the panel scales to 1.03.
    - Press: a 0.97 scale, a camera micro-shake, a click sound and a low rumble.
  - `Ayarlar`, `Galeri`, `Çıkış`: SECONDARY rows. A dark translucent red bar (#3A0A0C at 70%), Oswald Medium 34, uppercase, letter-spaced.
    - Hover: a bright left accent bar slides in, the text brightens, and an arrow `›` appears.
  - Under the buttons, a yellow/black **hazard stripe** bar that scrolls slowly.
  - Keyboard/gamepad navigation works (Up/Down/Enter). The selected button gets the hover look.
- **HUD dressing** (sells the "disaster control room" feel; all procedural UI, no new art):
  - thin corner brackets on the screen corners;
  - a tiny monospace readout in the corners that ticks, e.g. `KANDİLLİ // SON SARSINTI M 7.1 · 00:61` and live-looking coordinates `41.0082° K, 28.9784° D` jittering in the last digit;
  - 4–6 **pulsing red alert pins** placed over the map's districts (a red circle + a ring expanding and fading, staggered timing);
  - a **seismograph line** across the bottom-left (a line renderer / UI polyline scrolling left with noise, plus a spike every few seconds that also gives the screen a micro-shake);
  - **smoke/ash particles** drifting slowly across (a UI particle or a world-space particle in front of the background, low alpha);
  - a subtle **scanline overlay** plus a vignette.
- **Audio:**
  - menu music: the calm prep track, ducked, with a low ambient rumble loop under it;
  - button hover tick and click sounds (generate them with tools/gen_sfx.py if missing);
  - the seismograph spike plays a soft thud.
- **Intro:** on scene load, fade from black (0.6 s). The title slams in (scale 1.15 → 1 with a short shake and a dust puff), then the buttons slide in from the left with a stagger. Any key or click skips the intro.
- **Button actions:**
  - `Oyunu Başlat`: a 0.5 s fade to black with a rumble, then load Test_Building.
  - `Ayarlar`: the existing SettingsPanel, restyled to match (dark red panel, Oswald headers).
  - `Galeri`: a panel showing the collected **Deprem Notları** (the tips / TipJournal data persisted in PlayerPrefs), as a grid of cards: locked = `???`, unlocked = title + text, with a counter `Toplanan notlar: X/18`. Also a list of endings seen, if that data exists; otherwise just the notes.
  - `Çıkış`: the confirm dialog, then Application.Quit (and stop Play mode in the Editor).
- **Version tag** bottom-right: `Civil Game Jam 2026 · v1.0`, monospace, small.

## 2. End screen ("SİMÜLASYON SONA ERDİ")
Show it after the epilogue completes (after the existing EpilogueController text), as a full-screen overlay in Test_Building (no scene load).
- **Background:** end_bg with a slow push-in (1.0 → 1.05), drifting ash particles, film grain and a vignette.
- **Text** (centred, max width 900), revealed in sequence:
  1. `SİMÜLASYON SONA ERDİ`: IBM Plex Mono Bold or Oswald Bold, about 72 px, light grey, letter-spacing 0.06em. Type it in with a terminal cursor, then a subtle glow.
  2. `Bu bir simülasyondu. Tekrar oynayabilir, doğru seçimleri bulabilir ve` + `hayatta kalabilirdin.` in amber `#F2A23A`: IBM Plex Mono about 30 px, typewriter.
  3. A pause of 0.8 s, then in red `#E5483E`: `Ancak gerçek İstanbul depreminde 'Restart' (Yeniden Başlat) tuşu olmayacak.`, with a single hard glitch + a low boom when it appears. This is the punchline; give it weight.
- **Stats block** (replaces the mockup's gibberish lines): left-aligned monospace, values count up one by one:
  - `Döngü sayısı` → GameState.LoopCount
  - `Toplanan Deprem Notları` → X/18
  - `Kalan bina süresi` → seconds left (or `Bina arkanızda çöktü` if the CollapsedBehind flag is set)
  - `Aile` → status line (everyone safe / injured / ...), from the existing epilogue data.
- **Buttons:**
  - `YENİDEN DENE`: amber text button (like the mockup), Oswald Medium 30, with an underline that grows on hover. It starts a brand new run (the full prep from the start), keeping the persistent tip collection.
  - `MENÜYE DÖN`: the primary framed button (metal frame plus an orange glow like the mockup). It fades and loads MainMenu.
  - Keyboard navigation works.
- **Audio:** the outro track (cue_music_outro) fades in, the type sounds are soft, and the red line gets the boom.
- An **AFAD link line** at the bottom, small: `Hazırlık için: afad.gov.tr · Deprem çantanı bugün hazırla.` (plain text, no link handling needed).

## 3. General
- Everything is driven by unscaled time and works at 1280×720 and 1920×1080.
- Reduce Effects: no glitch, no shake, no scanline flicker. Fades and particles stay (lower density).
- No text below 16 px at 1080p.
- **Verify:**
  - screenshots of the menu (idle, a hover on Oyunu Başlat, Galeri open, Ayarlar open) and the end screen (mid-typewriter, final state with stats);
  - play menu → game → finish → end screen → Yeniden Dene → menu;
  - 0 compile errors; EditMode green; Text > Validate clean.
- Write the HANDOFF entry, then commit and push.
