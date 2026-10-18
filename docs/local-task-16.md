# LOCAL TASK 16 — 8-minute timer, intro briefing, floor-transition info cards, the robber rework + his animation

Read `CLAUDE.md` and the newest `HANDOFF.md` entry first. Unity is open with MCP.
**Scope:** only these parts. Commit after each part.
All Turkish text below is FINAL: put it in strings_tr.csv exactly as written (keys given).

## 1. The game lasts 8 minutes
- `LevelConfig.buildingSeconds` 300 → **480**. Keep the pulse at 60 s and the alarm + ticks at 30 s. The prep stays 60 s and separate.
- Retune stamina so exhaustion is still a real threat in the longer run: a player who never eats/drinks hits 0 stamina at about 260–300 s; one food or water buys about 90 s. Report the numbers in the handoff.
- Update every text that says "5 dakika" / 300, if any.
- Update tests that assume 300.

## 2. Intro briefing (once per new run, before the AFAD alert)
A full-screen card in the same style as the end screen (dark, IBM Plex Mono body, Oswald title, film grain). Timers do not run yet.
- `ui.intro.title` = `BİLGİLENDİRME`
- `ui.intro.body` =
  `Bu oyun bir deprem simülasyonudur. 8 dakikan var: eşini ve çocuğunu 4. kattan sokağa, acil toplanma alanına ulaştır.`
  `AFAD uyarısı geldiğinde 60 saniyen olacak: işine yarayacak olanı al, gerisini bırak.`
  `Her yanlış karar seni geri sarar. Her ölüm sana gerçek bir Deprem Notu bırakır.`
- `ui.intro.controls` =
  `Tıkla: yürü / etkileş · 1–4: seçim · W A S D: hızlı tepki · ESC: duraklat`
- `ui.intro.source` = `Oyundaki deprem bilgileri AFAD önerilerine dayanır.`
- Button: `ui.intro.start` = `ANLADIM`. Enter/Space/click work, but only after 1 s, so a stray click from the menu can't skip it.
- "Yeniden Dene" from the end screen shows it again. Macro/micro rewinds do NOT.

## 3. Floor-transition info cards ("loading screen")
Shown when the family goes **F4 → F3, F3 → F2 and F2 → street**, and on every **macro rewind** (a checkpoint restart).
- **Card layout:**
  - full-screen dark;
  - top-left: the destination label (`3. KAT`, `2. KAT`, `SOKAK`; keys `ui.floor.3`, `ui.floor.2`, `ui.floor.street`);
  - a small `DEPREM BİLGİSİ  n/8`;
  - the card title (Oswald) and body (IBM Plex Mono, max width 900);
  - a dim line `Oyunda: …` that links the fact to what the player just did / will do;
  - a thin progress bar filling over 6 s, then auto-continue. Click / Space / Enter continue early, but only after 1.5 s.
- All timers are paused while the card is up (TimerDelta = 0). Unscaled time. Fade in/out 0.3 s.
- **Which card:** each transition has a preferred card (below). If the player has already seen it, show the next unseen card from the pool; if all 8 are seen, cycle.
  - Seen cards persist in PlayerPrefs and are added to the Galeri as a second tab, `Bilgi Kartları`.
- Preferred cards: F4→F3 = card 3; F3→F2 = card 4; F2→street = card 8; a macro rewind = the card linked to the death cause if one matches (gas → 4, electrocution → 5, a placement trap → 2, collapse/timer → 6), else the next unseen.

**The 8 cards** (AFAD-consistent; keys `info.N.title`, `info.N.body`, `info.N.game`):
1. **Çök – Kapan – Tutun**
   `Sarsıntı başladığında yere çök, sağlam bir masanın altına ya da yanına kapan, bir kolunla başını ve enseni koru, diğer elinle tutun. Sarsıntı bitene kadar yerinden ayrılma.`
   Oyunda: `Deprem anında ailen otomatik olarak çöküp kapanıyor; onları sağlam masanın yanına yerleştir.`
2. **"Hayat üçgeni" bir efsane**
   `"Hayat üçgeni" AFAD tarafından önerilmez. Kapı eşiği, pencere önü, balkon ve devrilebilecek dolap ya da kitaplıkların yanı güvenli yer değildir.`
   Oyunda: `Kapı, pencere, balkon ve dolap önü yerleştirmede tuzaktır.`
3. **Merdiven ve asansör**
   `Sarsıntı sırasında merdivenlere koşma, asansöre asla binme. Sarsıntı bittikten sonra binayı merdivenden, sakin ve hızlı bir şekilde terk et.`
   Oyunda: `Asansör her katta bir tuzak; aşağı inmek için merdiveni kullan.`
4. **Gaz kokusu varsa**
   `Gaz kokusu alıyorsan çakmak ya da kibrit yakma, elektrik düğmelerine dokunma. Mümkünse vanayı kapat, kapı ve pencereleri açarak ortamı terk et.`
   Oyunda: `Gazlı odada çakmak patlama demek; önce vana, sonra fener.`
5. **Elektrik tesisatı**
   `Sarsıntı bittikten sonra elektrik sigortasını kapat. Kopmuş, sarkan kablolara ve hasarlı tesisata çıplak elle dokunma.`
   Oyunda: `Sigorta kutusuna eldivensiz ya da yalıtımsız dokunmak çarpılmak demek.`
6. **Deprem çantası**
   `Her evde kişi başı en az 72 saat yetecek su ve bozulmayan gıda, el feneri ve yedek pil, düdük, ilk yardım malzemesi, kişisel ilaçlar ve önemli belgelerin kopyalarını içeren bir deprem çantası hazır olsun.`
   Oyunda: `Hazırlıktaki 60 saniye, hazır bir çantan olmadığı için bu kadar zor.`
7. **Enkaz altında kalırsan**
   `Sakin kal; ağzını ve burnunu bir bezle kapatarak tozdan korun. Bağırarak gücünü tüketme; düdük çal ya da borulara, duvara vurarak sesini duyur.`
   Oyunda: `Sevim Teyze'nin verdiği düdük bu yüzden önemli.`
8. **Dışarıdaysan**
   `Binalardan, duvarlardan, elektrik direklerinden ve tellerden uzak, açık bir alana geç. Sarsıntı sonrası hasarlı binalara girme; bölgendeki acil toplanma alanına git. Toplanma alanını e-Devlet üzerinden öğrenebilirsin.`
   Oyunda: `Sokakta cephelerden ve kopuk telden uzak dur, açık alanda çök.`

## 4. The robber rework: a mugger, not a desperate victim (Bora's decision; implement as stated)
He is no longer injured or sympathetic. He is a **threatening mugger who blocks the stairs and forcibly takes our food**. Everything else about the encounter stays: unavoidable, choices only change the flavour, he takes ALL food (else 1 water, else nothing + a line), key items are never taken, then auto-continue to the street.
- Name: `npc.robber.name` = `Tehlikeli Adam`.
- Replace all his lines and the 3 replies with intimidating ones. Rewrite the choices:
  - `Yalvar` → he sneers;
  - `Kısa kes` → he grabs the bag;
  - `Ona bağır` → he steps closer, the knife glints, the family flinches.
  Final Turkish, short, menacing, no slurs, no gore. Example tone: `Çantayı yere bırak. Yavaşça.` / `Kahramanlık yapma, çocuğun izliyor.`
- Remove the "son waiting at the toplanma alanı" line and anything sympathetic.
- **No violence is shown:** he threatens, grabs the food, and runs down the stairs. No injury and no death in this scene.
- After he leaves, one toast/line: `ui.robber.after` = `Canınız yanmadı. Yiyecek bulunur; önemli olan ailen.`
- The epilogue's robbery line is updated to match (`Merdivende bir adam yiyeceklerinizi zorla aldı.`).

## 5. The robber's idle animation (new art)
- The project folder `Assets/_Game/Art/char/hırt` (8 frames) is his idle. A smoother 101-frame version is in the user's Downloads (`Downloads/Assets/tehlikeli adam/tehlikeli adam/`, 1080×1920, the same pose).
  - Use the 8-frame set at ~8 fps by default.
  - If Bora copies the 101-frame folder in, the same importer must handle it at ~24 fps (frame-count independent).
- Move the folder to `Art/char/ROBBER_IDLE` (AssetDatabase.MoveAsset). Height ratio 1.0, feet pivot shared across frames (as with the crouch import). Facing toward the family on the F2 stairs.
- The silhouette placeholder goes away. When he flees, his sprite runs down the stairs: tween it toward the stairwell door, then fade.

## 6. Verify
- New run: menu → intro (the 1 s guard) → AFAD → prep → quake → corridor (Sevim) → F4→F3 card 3 → F3→F2 card 4 → robber (all 3 choices: the menacing lines, food taken, the toast, his idle animation, the flee) → card 8 → street → end screen.
- Die to gas once → the macro rewind shows card 4.
- Galeri shows the `Bilgi Kartları` tab.
- The timer starts at 08:00.
- 0 compile errors; EditMode green; Validate Content and Text > Validate clean (no TODO).
- Write the HANDOFF entry, then commit and push.
