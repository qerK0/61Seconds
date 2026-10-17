# LOCAL TASK 08 — Wire and polish cloud task 02

Read `CLAUDE.md` and the newest `HANDOFF.md` entry (from cloud task 02) first, and work through its "Needs local wiring" checklist. Unity is open with MCP for Unity. Set Interaction Mode = No Throttling for Play Mode and restore it at the end.

Then check each of these, and fix what's broken:
1. **Compile:** 0 errors. Run the EditMode tests; all must pass.
2. **Prop art:**
   - Run the PropArt import. Check every prop against the family (0.8u player) and the two-row walls, and tune the target heights so nothing looks giant or tiny.
   - The table must be low enough that crouching under it reads correctly.
   - Keep feet and bases on the floor; check the sorting against characters walking around each prop.
   - Screenshot every room that changed.
3. **Doors:** `kapi.png` on every door, flush in the gap, flipped on right walls, colliders and approach points OK.
4. **Chandelier:** the swing loop during each quake; the drop ends on frame 5 at the hazard anchor.
5. **Running:**
   - The placement members run to their anchors and resolve correctly even when the timer ends mid-run.
   - On room changes the family runs in from the door.
   - Rewinds snap.
6. **Debris:**
   - Walk F4 → F3 → F2 after each quake. Debris grows, never blocks a door, anchor, NPC, container or rope, and the paths go around it.
   - Cracks increase.
   - Screenshot the same corridor after quakes 1, 2 and 3.
7. **Loot:**
   - Every container: run there, the search ring, the loot window, take one, take all, bag full, the second visit is free.
   - Neighbours' containers say `Ödünç al` and record the borrow.
   - Contents survive a rewind correctly.
8. **Robber + street:**
   - Play the robber with all 3 choices: each one ends with him taking all the food (or 1 water, or nothing), the toasts, him running off, and an automatic move to the street.
   - Street: the Rocky 17/3 floor, the camera follows left, the 4th quake feels like the strongest moment of the game (tune the shake, sound and glass), façade drop warnings are readable, the power line kills, "Açık alana çök!" works, the sign → epilogue.
   - Check the building-timer-at-0 beat both near and far from the door.
9. **Validators:** Validate Content and Text > Validate show 0 problems; no TODO in the CSV.
10. **Full playthrough** from the main menu to the epilogue once, without dev shortcuts. List every bug found and fixed.

Restore Interaction Mode. Write the HANDOFF entry, then commit and push to `cloud/foundation`.
