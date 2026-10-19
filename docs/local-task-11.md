# LOCAL TASK 11 — Speech bubbles instead of the bottom panel (+ Sevim / AFAD bugs)

Read `CLAUDE.md` and the newest `HANDOFF.md` entry first. Unity is open with MCP. Set Interaction Mode = No Throttling for Play Mode and restore it at the end.
This REPLACES the earlier "modern bottom dock" prompt. If part of that was already built, reuse what fits (cost chips, keyboard shortcuts) and remove the dock.

## Decisions (agreed with Bora)
- **Problem today:** the ChoicePanel floats in the middle of the scene and overlaps the subtitles. `SubtitleView` and the sliding `ChoicePanel` (peek/full) are two systems fighting each other.
- **New model:** speech bubbles attached to whoever is speaking, plus a compact choice list next to the bubble.
- Moves that a door or NPC click can do are **world-only**: never listed (RoomNavigator already hides them; keep that).
- The UI is shown **only when needed**: dialogue lines, placement instructions, special decisions (valve, QTE start, the robber, the street hint). While exploring, nothing is on screen except the HUD.

## 1. Bugs first
1. **Sevim Teyze can't be talked to and never gives her free items.**
   - Leaving Flat 11 (hall → corridor door) with `SevimDone == false` must enter `f4.sevim`. Suspect: RoomLinkRules picks `exit_again` or the wrong move choice, or the dialogue handler skips plain DialogueNodes after the RobberNode change.
   - `thanks` must give the whistle (key pocket) and the Flat 12 info flag; `Kısa kes` gives neither.
   - Make her clickable as well: if `SevimDone` is false, the click starts the dialogue; afterwards, one short line `Hadi evladım, çabuk olun!` (new CSV key).
   - Add a check or test: hall → corridor with `SevimDone=false` lands on `f4.sevim`, and `thanks` grants the whistle.
   - Re-check Hasan's gift (food + water), Cemil's trade and the robber.
2. **The AFAD popup** must appear only at the start of the prep, hide after about 4 s or on the first click, never come back after the quake, hide on any rewind, and never block clicks while hidden. It was visible on Floor 3.

## 2. `SpeechBubble` (world-anchored UI)
- **One bubble prefab, built by code in TestSceneUi.** It sits on a screen-space overlay canvas and follows a world anchor: the speaker's head = sprite bounds top + a small offset, recomputed every LateUpdate, so it follows the camera and the speaker moving.
- **Look:**
  - rounded rect (radius 12), background `#F7F3EA` (warm paper), 2 px dark outline `#1B1D22`, a soft shadow;
  - a small triangular tail pointing at the speaker's head (it flips side if the bubble is clamped to the other side);
  - the speaker name as a small tab on the bubble's top-left edge (Inter Bold 15, uppercase, in the speaker's accent colour: Sevim `#B0479B`, Hasan `#3E8E41`, Cemil `#B07A12`, the robber `#B23A2E`, the wife `#2F6DB5`, Mehmet `#333`);
  - body text Inter SemiBold 20 px, `#1B1D22`, max width 380 px, auto height, max 4 lines;
  - this matches the cartoon art (thick outlines) better than a dark glass panel.
- **Behaviour:**
  - the line typewrites with the voice blips;
  - a small `▼` bobs at the bottom-right when the line is complete;
  - click anywhere / Space / Enter: first completes the line, then advances.
- **Time cost:** a small LED chip `−4 sn` hangs off the bubble's bottom-right corner (same green/amber/red logic as the building timer), so the player sees what listening costs.
- **Placement / clamping:**
  - the bubble prefers above-right of the head;
  - it is clamped inside the safe area: not over the top HUD (timer / panic), not over the bag panel on the right, not off-screen;
  - if the speaker is off-screen, the bubble docks to the nearest screen edge, with the tail pointing toward them.
- One bubble at a time (lines are sequential). Pop-in: scale 0.9 → 1 with a fade over 0.15 s. Pop-out on advance, room transition, rewind and pause.
- **Narration with no speaker** (node bodies, placement instructions such as `Aileni güvenli yere yerleştir`, the street hint `Açık alana çök!`) uses a **narration card** instead: top-centre under the timer, dark (`#0E1116` at 90%), Inter SemiBold 20, white, max width 720. It auto-hides after its text is read or the node changes. It is never in the middle of the scene.
- **Player thoughts / decisions** with no NPC (the valve, the breaker, "Not bırak", the robber's choices) put the bubble on **Mehmet**, with a thought-bubble style (dotted tail, slightly grey background).

## 3. Choice list
- Shown only when the current node has choices that aren't world-handled (doors and hotspots are filtered out as now).
- Attached under the active bubble (or under Mehmet's thought bubble), the same width as the bubble or 360 px minimum, same safe-area clamping.
- **Each row:**
  - a number badge 1–4 (keyboard 1–4 also works);
  - the label in Inter SemiBold 18;
  - a right-aligned cost chip (`−3 sn`, `+ağır` for heavy, `🔒` when locked);
  - background white at 90%, 1.5 px outline;
  - hover: an accent fill at 15% plus a 3 px left bar in the speaker colour;
  - pressed: a 0.97 scale.
- `ShowLocked` rows are dimmed, with the reason under the label in 14 px. `ShowLockedFatal` rows look normal (no spoilers). Trap choices look normal.
- **During-dialogue choices** (`Kısa kes`) appear as a single small pill under the bubble while the lines play, not as a full row.
- Choices appear after the last line finishes typing, with a 0.1 s stagger per row.
- Panic input delay still applies to clicks, keeping the instant ClickFeedback ring.

## 4. Remove the old stuff
- Remove `SubtitleView` and the sliding ChoicePanel (peek/full, sink while running) from the scene builder. Keep the classes only if something else still uses them.
- `NodePlayer` / `DialogueController` / `PlacementController` / `RobberController` / `QTEController` call the new presenter (`DialoguePresenter`: ShowLine / ShowNarration / ShowThought / ShowChoices / Clear) behind one interface, so the controllers don't know about the UI.
- The dialogue line costs, during-dialogue choices and the voice blips work exactly as before.

## 5. Verify
- At 1920×1080 and 1280×720, play these and screenshot each:
  - Sevim (bubble on her, choices under it, `Kısa kes` pill, `thanks` → whistle);
  - Hasan;
  - Cemil's trade;
  - a placement instruction (the narration card);
  - the gas valve decision (Mehmet's thought bubble + choices);
  - the robber's 3 choices;
  - the street hint.
- Check that a bubble never covers the timer, panic meter, bag or another bubble; that the speaker walking or the camera moving keeps the bubble attached; and that rewind, pause and transitions clear it.
- 0 compile errors, EditMode green, Validate Content and Text > Validate clean.
- Write the HANDOFF entry, then commit and push.
