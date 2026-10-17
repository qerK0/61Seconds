# LOCAL TASK 09 — Layout Editor: Bora places props/items by hand in every room, and rebuilds keep them

Read `CLAUDE.md` and the newest `HANDOFF.md` entry first. Unity is open with MCP for Unity.

**Problem:** every room is generated from code (`BuildingLayout` → Build Test Scene), so anything Bora moves by hand in the scene is lost on the next rebuild.

**Goal:** an editor tool where Bora visits each room, drags and adds furniture, items and containers with the normal Scene view tools, and saves. Every rebuild then applies those saved edits.

## 1. Data: `Data/Layout/RoomLayoutOverrides.asset` (ScriptableObject)
- One entry per room id. Each entry holds a list of placed objects:
  - kind (`Prop` / `Pickup` / `Container` / `HiddenItem` / `Light`);
  - a stable id;
  - grid position (gx, gy) and height offset;
  - flipX;
  - prop / sprite name (a `PropArtSet` key);
  - for Pickup: the item id, cost, borrowed and surface;
  - for Container: the container type, contents, searchSeconds, buildingCost and borrowed;
  - topple / drop settings if the prop is a hazard prop;
  - a `removed` flag, so a code-defined object can be deleted.
- Code-defined objects keep their ids. An override with the same id replaces position/flip/sprite; new ids are additions.
- Anchors, doors, hotspots, ropes and spawns stay code-only for now. Changing them can break the AFAD placement logic, so the tool shows them as locked gizmos.

## 2. The builder applies the overrides
- `TestSceneBuilder` / `SceneKit` read the overrides after `BuildingLayout` and apply them per room: move, flip, swap the sprite, remove, add.
- Static props block their nav cells (GridNav) as they do now.
- After applying, run the same checks as `DebrisPlanner`: no required cell blocked, and every door / anchor / NPC / container still reachable. Validate Content reports every violation with the room and object id.

## 3. Editor window `SixtyOne > Layout Editor`
- **Room list** (grouped by floor, plus the street). Clicking a room:
  - isolates it (hides the others with SceneVisibility, not by disabling them);
  - frames the Scene view on it;
  - selects its root.
  `Prev` / `Next` buttons step through rooms.
- **Palette:**
  - a grid of thumbnails from `PropArtSet` (table, chair, wardrobe, bookshelf, stove, valve, fuse box, lamp, chandelier, debris, door, etc.);
  - every item icon (as a Pickup);
  - container presets (wardrobe, cabinet, bookshelf, drawer, medicine cabinet, toolbox, nightstand, jewelry box).
  Click one to spawn it at the room's centre, selected, ready to drag.
- **Inspector for the selected placed object** (a small panel in the window): kind, item id dropdown, cost, borrowed, surface, container contents (item id list + add/remove), search seconds, flipX, `Hazard prop` toggle.
- **Snap:** an option to snap to the half-cell iso grid (default on) and to sit the object on the floor (pivot at the feet). Show the iso grid gizmo with blocked nav cells in red.
- **Buttons:**
  - `Save room`: reads every placed object under the room root (tagged with a `LayoutObject` component carrying kind + id + data) and writes them to the overrides asset;
  - `Save all`;
  - `Revert room to code`: removes that room's overrides;
  - `Rebuild scene` (with the save-scene fix, so no modal dialog).
- Undo works (Undo.RecordObject). Ctrl+D duplicates with a new id.
- Every saved change is a normal asset change that git can diff. Mention this in the handoff so Bora commits `RoomLayoutOverrides.asset`.

## 4. Play Mode helper (for checking in game)
- A dev-only room jump: in the Editor, or in a Development build only, F9 opens a small list of rooms and teleports the player and family there with the current state. Never in release builds.

## 5. Verify
- Move a wardrobe in the Flat 11 salon, add a chair and a food pickup in Flat 8, add a container with 2 items in Flat 9, and remove a code prop in Flat 12. Save, Rebuild scene: every change is still there. Play: the items can be picked up and the container works.
- Try placing a prop on a door's approach cell: Validate Content flags it.
- 0 compile errors; EditMode green; validators clean.
- Write a short `docs/layout-editor.md` how-to for Bora in Turkish, at most 10 lines.
- Write the HANDOFF entry, then commit and push.
