# Local Task 05 — verify world scale + tile transition, fix framing/timers/shake

Read CLAUDE.md and the newest CLOUD entry in HANDOFF.md (world scale 1.5 + tile rise/drop room transition + animator tests). It never ran in Unity. You are the LOCAL session with Unity MCP.

1. Fix any compile errors (new Unity APIs since the stand-ins: Tilemap per-tile access, sprite proxies, RoomVisualConfig, lossyScale-based helpers). Add asmdef references instead of removing features.
2. Run the handoff's local checklist in order: regenerate content, Build Test Scene, Validate Content, Text > Validate, all 134 EditMode tests.
3. Visual checks with before/after screenshots: character sizes and foot contact, seams in a wood room and a stone room, a small and a big room, lateral transition, descend transition, rewind (instant), Reduce Effects (fade), clicks ignored mid-transition, and every room back exactly in place after 10+ transitions. Watch the profiler for spikes on first visit to a room.

4. FIXES (apply, then re-screenshot):
a) Rooms must look bigger on screen: raise RoomCameraFraming fill so rooms use ~95-100% of the playfield and lower the min ortho size so small rooms are framed closer. Tune by eye so the room clearly dominates the screen without clipping walls under the HUD.
b) Transitions must NOT cost time: pause both the 60s prep countdown and the building timer for the duration of the tile rise/drop animation (resume on OnTransitionComplete). The configured door cost (if any) still applies once. Add an EditMode test that a transition does not reduce either timer beyond the configured cost.
c) Scale CameraShake amplitude by current ortho size relative to a reference size so quakes feel equally strong in every room (respect Reduce Effects).
d) Dropped items: make them rise/drop with the room too if easy; otherwise leave as is and note it.

5. Play the full route once (prep -> quake -> placement -> corridor -> floors 3 and 2 -> final escape QTE -> ending) to confirm nothing regressed. No new console errors; keep ProjectSettings/VersionControlSettings.asset as "Visible Meta Files"; don't touch Assets/_Game/Art/Concept/.

Finish with a LOCAL handoff entry (bugs fixed, tuned framing/shake values, screenshots summary). Commit and push to cloud/foundation.
