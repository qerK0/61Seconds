using System.Collections.Generic;
using System.Linq;
using SixtyOne.Iso;
using SixtyOne.Nodes;
using SixtyOne.Placement;
using SixtyOne.World;
using UnityEditor;
using UnityEngine;

namespace SixtyOne.Editor
{
    /// <summary>
    /// SixtyOne > Layout Editor: pick a room (it is isolated and framed), drag props / items / containers / lights
    /// with the normal Scene tools (scale: the slider or the Scale tool, uniform), move doors along their wall, add new
    /// ones from the palette, then Save room. Saved edits live in
    /// RoomLayoutOverrides.asset and every Build Test Scene applies them. Anchors, hotspots, ropes and
    /// spawns stay code-only and show as locked gizmos.
    /// </summary>
    public sealed class LayoutEditorWindow : EditorWindow
    {
        private const string SnapPref = "SixtyOne.Layout.Snap";
        private const float SnapStep = 0.5f;

        private List<BuildingLayout.RoomSpec> specs;
        private string roomId;
        private Room room;
        private bool roomWasActive;
        private Vector2 roomScroll;
        private Vector2 paletteScroll;
        private int paletteTab;
        private bool snap = true;
        private PropArtSet props;
        private List<Item> items;
        private readonly Dictionary<string, LayoutObject> idOwners = new Dictionary<string, LayoutObject>();

        [MenuItem("SixtyOne/Layout Editor", priority = 12)]
        public static void Open() => GetWindow<LayoutEditorWindow>("Layout Editor");

        private void OnEnable()
        {
            snap = EditorPrefs.GetBool(SnapPref, true);
            SceneView.duringSceneGui += OnSceneGui;
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
            Selection.selectionChanged += Repaint;
            Reload();
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGui;
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
            Selection.selectionChanged -= Repaint;
            ReleaseRoom();
        }

        private void Reload()
        {
            specs = LayoutOverrides.Rooms();
            props = AssetDatabase.LoadAssetAtPath<PropArtSet>(PropArt.SetPath);
            items = new List<Item>();
            foreach (string id in BuildingLevelsGenerator.ItemIds)
            {
                Item item = AssetDatabase.LoadAssetAtPath<Item>($"{BuildingLevelsGenerator.ItemsDir}/{id}.asset");
                if (item != null) items.Add(item);
            }
        }

        private BuildingLayout.RoomSpec Spec => specs?.Find(s => s.Id == roomId);

        // ---------- GUI ----------

        private void OnGUI()
        {
            if (EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("Leave Play Mode to edit layouts (in game, F9 jumps between rooms).", MessageType.Info);
                return;
            }

            EditorGUILayout.BeginHorizontal();
            DrawRoomList();
            EditorGUILayout.BeginVertical();
            DrawToolbar();
            if (room != null)
            {
                DrawPalette();
                DrawInspector();
            }
            else
            {
                EditorGUILayout.HelpBox("Pick a room on the left. Open Test_Building first (SixtyOne > Build Test Scene).", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawRoomList()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(170f));
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("◀ Prev")) Step(-1);
            if (GUILayout.Button("Next ▶")) Step(1);
            EditorGUILayout.EndHorizontal();
            roomScroll = EditorGUILayout.BeginScrollView(roomScroll);
            string[] floors = { "Kat 4", "Kat 3", "Kat 2", "Sokak" };
            for (int depth = 0; depth < floors.Length; depth++)
            {
                EditorGUILayout.LabelField(floors[depth], EditorStyles.boldLabel);
                foreach (BuildingLayout.RoomSpec spec in specs.Where(s => FloorOf(s) == depth))
                {
                    GUIStyle style = spec.Id == roomId ? EditorStyles.miniButtonMid : EditorStyles.miniButton;
                    string label = (spec.Id == roomId ? "▶ " : "") + spec.Id + (HasOverrides(spec.Id) ? " *" : "");
                    if (GUILayout.Button(label, style)) SelectRoom(spec.Id);
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private static int FloorOf(BuildingLayout.RoomSpec spec) => spec.Id == BuildingLevelsGenerator.Street ? 3 : Mathf.Clamp(spec.Depth, 0, 2);

        private static bool HasOverrides(string id)
        {
            RoomLayoutOverrides asset = LayoutOverrides.Load();
            return asset != null && asset.Find(id) != null;
        }

        private void DrawToolbar()
        {
            EditorGUILayout.LabelField(room != null ? "Oda: " + roomId : "Oda seçilmedi", EditorStyles.largeLabel);
            bool newSnap = EditorGUILayout.ToggleLeft("Izgaraya yapıştır (yarım hücre) + zemine oturt", snap);
            if (newSnap != snap) EditorPrefs.SetBool(SnapPref, snap = newSnap);

            EditorGUILayout.BeginHorizontal();
            GUI.enabled = room != null;
            if (GUILayout.Button("Save room")) SaveRoom(roomId, true);
            GUI.enabled = true;
            if (GUILayout.Button("Save all")) SaveAll();
            GUI.enabled = room != null;
            if (GUILayout.Button("Revert room to code")) RevertRoom();
            GUI.enabled = true;
            if (GUILayout.Button("Rebuild scene")) Rebuild();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawPalette()
        {
            paletteTab = GUILayout.Toolbar(paletteTab, new[] { "Eşyalar (prop)", "Toplanabilir", "Dolaplar", "Işık" });
            paletteScroll = EditorGUILayout.BeginScrollView(paletteScroll, GUILayout.Height(190f));
            const float cell = 72f;
            int perRow = Mathf.Max(1, Mathf.FloorToInt((position.width - 200f) / (cell + 4f)));
            var entries = new List<(string label, Texture tex, System.Action spawn)>();
            switch (paletteTab)
            {
                case 0:
                    if (props != null)
                    {
                        var seen = new HashSet<Sprite>();
                        foreach (PropArtSet.Entry e in props.Entries)
                        {
                            if (e == null || e.Sprite == null || !seen.Add(e.Sprite)) continue;
                            string n = e.Name;
                            entries.Add((PropArt.Label(n), Thumb(e.Sprite), () => SpawnProp(n)));
                        }

                        if (props.ChandelierFrames.Length > 0) entries.Add(("CeilingLamp", Thumb(props.ChandelierFrames[0]), () => SpawnProp("CeilingLamp")));
                    }

                    break;
                case 1:
                    foreach (Item item in items)
                    {
                        Item it = item;
                        entries.Add((it.Id, it.Icon != null ? Thumb(it.Icon) : null, () => SpawnPickup(it)));
                    }

                    break;
                case 2:
                    foreach (string kind in BuildingLayout.ContainerKinds)
                    {
                        string k = kind;
                        BuildingLayout.ContainerSpec preset = BuildingLayout.ContainerPreset("x", k);
                        Sprite art = props != null ? props.Find(preset.Art) : null;
                        entries.Add((ContainerLabel(k), art != null ? Thumb(art) : null, () => SpawnContainer(k)));
                    }

                    break;
                default:
                    entries.Add(("Lamba ışığı", null, SpawnLight));
                    break;
            }

            for (int i = 0; i < entries.Count; i += perRow)
            {
                EditorGUILayout.BeginHorizontal();
                for (int j = i; j < Mathf.Min(entries.Count, i + perRow); j++)
                {
                    var e = entries[j];
                    var content = new GUIContent(e.tex != null ? "" : e.label, e.tex, e.label);
                    EditorGUILayout.BeginVertical(GUILayout.Width(cell));
                    if (GUILayout.Button(content, GUILayout.Width(cell), GUILayout.Height(cell))) e.spawn();
                    GUILayout.Label(e.label, EditorStyles.centeredGreyMiniLabel, GUILayout.Width(cell));
                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        /// <summary>Palette name of a container preset (Turkish, as the team calls them).</summary>
        private static string ContainerLabel(string kind)
        {
            switch (kind)
            {
                case "wardrobe": return "Gardırop";
                case "wardrobe_v2": return "Gardırop v2";
                case "cabinet": return "Dolap";
                case "kitchen_cabinet": return "Mutfak dolabı";
                case "bookshelf": return "Kitaplık";
                case "drawer": return "Çekmece";
                case "toolbox": return "Alet çantası";
                case "nightstand": return "Komodin";
                case "tv_unit": return "TV ünitesi";
                default: return kind;
            }
        }

        private static Texture Thumb(Sprite sprite)
        {
            Texture preview = AssetPreview.GetAssetPreview(sprite);
            return preview != null ? preview : sprite.texture;
        }

        private void DrawInspector()
        {
            LayoutObject tag = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponentInParent<LayoutObject>() : null;
            EditorGUILayout.Space();
            if (tag == null)
            {
                EditorGUILayout.HelpBox("Odada bir eşya seç (sürükle: Move aracı, W). Ctrl+D çoğaltır, Delete siler.", MessageType.None);
                return;
            }

            EditorGUILayout.LabelField($"{tag.Kind}  ·  {tag.Id}" + (tag.FromCode ? "  (kodda var)" : "  (yeni)") + (tag.Proxy ? "  · Rebuild sonrası gerçek obje" : ""), EditorStyles.boldLabel);
            if (tag.Kind == LayoutKind.Door)
            {
                DrawDoorInspector(tag);
                return;
            }

            if (tag.Kind != LayoutKind.Light) DrawTransformInspector(tag);
            EditorGUI.BeginChangeCheck();
            bool flip = EditorGUILayout.Toggle("Flip X", tag.FlipX);
            bool blocks = tag.BlocksNav;
            if (tag.Kind == LayoutKind.Prop || tag.Kind == LayoutKind.Container) blocks = EditorGUILayout.Toggle("Yolu kapatır (nav)", tag.BlocksNav);
            float lift = EditorGUILayout.FloatField("Yükseklik (lift)", tag.Lift);
            string itemId = tag.ItemId;
            float cost = tag.Cost;
            bool borrowed = tag.Borrowed;
            bool surface = tag.Surface;
            bool prepOnly = tag.PrepOnly;
            float seconds = tag.SearchSeconds;
            float buildingCost = tag.BuildingCost;
            bool hazard = tag.HazardProp;
            string hazardId = tag.HazardPropId;
            var contents = new List<string>(tag.Contents);
            string[] ids = BuildingLevelsGenerator.ItemIds;

            switch (tag.Kind)
            {
                case LayoutKind.Pickup:
                case LayoutKind.HiddenItem:
                    itemId = ids[Mathf.Max(0, EditorGUILayout.Popup("Item", System.Array.IndexOf(ids, itemId), ids))];
                    cost = EditorGUILayout.FloatField("Süre (sn)", cost);
                    borrowed = EditorGUILayout.Toggle("Ödünç (komşunun)", borrowed);
                    surface = EditorGUILayout.Toggle("Açık yüzeyde", surface);
                    prepOnly = EditorGUILayout.Toggle("Sadece hazırlıkta", prepOnly);
                    break;
                case LayoutKind.Container:
                    EditorGUILayout.LabelField("İçindekiler");
                    for (int i = 0; i < contents.Count; i++)
                    {
                        EditorGUILayout.BeginHorizontal();
                        contents[i] = ids[Mathf.Max(0, EditorGUILayout.Popup(System.Array.IndexOf(ids, contents[i]), ids))];
                        if (GUILayout.Button("–", GUILayout.Width(24f)))
                        {
                            contents.RemoveAt(i);
                            EditorGUILayout.EndHorizontal();
                            break;
                        }

                        EditorGUILayout.EndHorizontal();
                    }

                    if (GUILayout.Button("+ Item ekle")) contents.Add("food");
                    seconds = EditorGUILayout.FloatField("Arama süresi (gerçek sn)", seconds);
                    buildingCost = EditorGUILayout.FloatField("Bina süresi (sn)", buildingCost);
                    borrowed = EditorGUILayout.Toggle("Ödünç (komşunun)", borrowed);
                    prepOnly = EditorGUILayout.Toggle("Sadece hazırlıkta", prepOnly);
                    break;
                case LayoutKind.Prop:
                    hazard = EditorGUILayout.Toggle("Hazard prop", hazard);
                    if (hazard) hazardId = EditorGUILayout.TextField("Hazard prop id", hazardId);
                    break;
            }

            if (!EditorGUI.EndChangeCheck()) return;
            Undo.RecordObject(tag, "Edit layout object");
            if (flip != tag.FlipX)
            {
                SpriteRenderer r = tag.GetComponentInChildren<SpriteRenderer>();
                if (r != null)
                {
                    Undo.RecordObject(r, "Flip");
                    r.flipX = !r.flipX;
                }
            }

            if (!Mathf.Approximately(lift, tag.Lift))
            {
                Undo.RecordObject(tag.transform, "Lift");
                tag.transform.localPosition += new Vector3(0f, lift - tag.Lift, 0f);
            }

            tag.FlipX = flip;
            tag.BlocksNav = blocks;
            tag.Lift = lift;
            tag.ItemId = itemId;
            tag.Cost = cost;
            tag.Borrowed = borrowed;
            tag.Surface = surface;
            tag.PrepOnly = prepOnly;
            tag.Contents = contents;
            tag.SearchSeconds = seconds;
            tag.BuildingCost = buildingCost;
            tag.HazardProp = hazard;
            tag.HazardPropId = hazardId;
            EditorUtility.SetDirty(tag);
        }

        /// <summary>Uniform scale (slider 0.5-2, Reset, Match prop height) and the sorting nudge of a placed object.</summary>
        private void DrawTransformInspector(LayoutObject tag)
        {
            float current = LayoutOverrides.ScaleOf(tag);
            EditorGUI.BeginChangeCheck();
            float scale = EditorGUILayout.Slider("Ölçek", current, 0.5f, 2f);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset scale")) scale = 1f;
            float target = PropArt.TargetHeight(tag.PropName);
            SpriteRenderer r = MainRenderer(tag);
            GUI.enabled = target > 0f && r != null && r.sprite != null;
            if (GUILayout.Button(new GUIContent("Match prop height", target > 0f ? $"Hedef yükseklik {target:0.##} (PropArt tablosu)" : "Bu prop için tabloda yükseklik yok")))
            {
                // Height at scale 1 = sprite height x the renderer's scale relative to the object.
                float unit = r.sprite.bounds.size.y * Mathf.Abs(r.transform.lossyScale.y / tag.transform.lossyScale.y) * Mathf.Abs(tag.BaseScale.y);
                if (unit > 1e-4f) scale = Mathf.Round(target / unit * 100f) / 100f;
            }

            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Sıralama (önde +)", GUILayout.Width(146f));
            int nudge = tag.SortNudge;
            if (GUILayout.Button("–", GUILayout.Width(24f))) nudge -= 10;
            nudge = EditorGUILayout.IntField(nudge, GUILayout.Width(60f));
            if (GUILayout.Button("+", GUILayout.Width(24f))) nudge += 10;
            EditorGUILayout.EndHorizontal();
            if (!EditorGUI.EndChangeCheck()) return;

            if (!Mathf.Approximately(scale, current))
            {
                Undo.RecordObject(tag.transform, "Scale");
                SetScale(tag, Mathf.Clamp(scale, 0.25f, 4f));
            }

            if (nudge != tag.SortNudge)
            {
                Undo.RecordObject(tag, "Sorting nudge");
                foreach (SpriteRenderer sr in tag.GetComponentsInChildren<SpriteRenderer>()) sr.sortingOrder += nudge - tag.SortNudge;
                tag.SortNudge = nudge;
                EditorUtility.SetDirty(tag);
            }
        }

        /// <summary>Uniform scale against the built size; an "Approach" child keeps its world position.</summary>
        private static void SetScale(LayoutObject tag, float scale)
        {
            Transform approach = tag.transform.Find("Approach");
            Vector3 stand = approach != null ? approach.position : Vector3.zero;
            tag.transform.localScale = new Vector3(tag.BaseScale.x * scale, tag.BaseScale.y * scale, 1f);
            if (approach != null) approach.position = stand;
            tag.ObjectScale = scale;
            EditorUtility.SetDirty(tag);
        }

        /// <summary>The object's main sprite: a door's "Art", else the body / first sprite.</summary>
        private static SpriteRenderer MainRenderer(LayoutObject tag)
        {
            Transform art = tag.transform.Find("Art") ?? tag.transform.Find("Body");
            if (art != null && art.GetComponent<SpriteRenderer>() != null) return art.GetComponent<SpriteRenderer>();
            return tag.GetComponentInChildren<SpriteRenderer>();
        }

        private static readonly string[] DoorArts = { "Door", "Elevator", "Stairwell", "Stairs" };

        /// <summary>Popup labels of DoorArts (same order).</summary>
        private static readonly string[] DoorArtLabels = { "Door", "Elevator", "Stairwell door", "Stairs" };

        /// <summary>
        /// A door: its wall gap (drag it along the wall, or pick the edge), flip and art. Where it leads, its choice
        /// and costs are code-defined and shown read-only.
        /// </summary>
        private void DrawDoorInspector(LayoutObject tag)
        {
            BuildingLayout.RoomSpec spec = Spec;
            if (spec == null) return;
            EditorGUILayout.LabelField("Nereye", tag.DoorTwoWay ? tag.DoorTarget + "  (çift yönlü)" : tag.DoorTarget + "  (tek yön)");
            EditorGUILayout.LabelField("Seçim / süre", "kodda (BuildingLayout + düğüm seçimi), değişmez");
            string[] edges = BuildingLayout.WallEdges(spec).ToArray();
            EditorGUI.BeginChangeCheck();
            int edgeIndex = EditorGUILayout.Popup("Duvar boşluğu", Mathf.Max(0, System.Array.IndexOf(edges, tag.DoorEdge)), edges);
            bool flip = EditorGUILayout.Toggle("Flip X", tag.FlipX);
            int art = EditorGUILayout.Popup("Görsel", Mathf.Max(0, System.Array.IndexOf(DoorArts, tag.DoorArt)), DoorArtLabels);
            if (!EditorGUI.EndChangeCheck()) return;
            Undo.RecordObject(tag, "Edit door");
            Undo.RecordObject(tag.transform, "Move door");
            if (flip != tag.FlipX)
            {
                SpriteRenderer r = MainRenderer(tag);
                if (r != null)
                {
                    Undo.RecordObject(r, "Flip");
                    r.flipX = !r.flipX;
                }
            }

            tag.FlipX = flip;
            tag.DoorArt = DoorArts[art];
            PlaceDoor(tag, spec, edges[edgeIndex]);
            EditorUtility.SetDirty(tag);
        }

        /// <summary>Puts a door proxy on a wall edge (its art faces that wall; the real door comes with Rebuild scene).</summary>
        private static void PlaceDoor(LayoutObject tag, BuildingLayout.RoomSpec spec, string edge)
        {
            Vector2 c = BuildingLayout.EdgeCenter(spec, edge);
            tag.transform.localPosition = IsoMath.GridToWorld(c.x, c.y);
            if (tag.DoorEdge != edge)
            {
                SpriteRenderer r = MainRenderer(tag);
                if (r != null) r.flipX = edge.StartsWith("R") != tag.FlipX;
            }

            tag.DoorEdge = edge;
        }

        // ---------- rooms ----------

        private void Step(int delta)
        {
            if (specs == null || specs.Count == 0) return;
            int i = specs.FindIndex(s => s.Id == roomId);
            i = (i + delta + specs.Count) % specs.Count;
            SelectRoom(specs[i].Id);
        }

        private static Room FindSceneRoom(string id)
        {
            foreach (Room r in Object.FindObjectsByType<Room>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (r.RoomId == id) return r;
            }

            return null;
        }

        private void SelectRoom(string id)
        {
            ReleaseRoom();
            room = FindSceneRoom(id);
            roomId = id;
            if (room == null)
            {
                Debug.LogWarning($"[Layout] Room '{id}' is not in the open scene. Open Test_Building (or Rebuild scene).");
                return;
            }

            roomWasActive = room.gameObject.activeSelf;
            room.gameObject.SetActive(true);
            SceneVisibilityManager.instance.Isolate(room.gameObject, true);
            RegisterIds();
            Selection.activeGameObject = room.gameObject;
            Frame();
            Repaint();
        }

        /// <summary>Puts the shown room back as the builder left it (only the first room active) and ends isolation.</summary>
        private void ReleaseRoom()
        {
            if (room != null) room.gameObject.SetActive(roomWasActive);
            if (SceneVisibilityManager.instance.IsCurrentStageIsolated()) SceneVisibilityManager.instance.ExitIsolation();
            room = null;
        }

        private void Frame()
        {
            SceneView view = SceneView.lastActiveSceneView;
            if (view == null || room == null) return;
            Renderer[] renderers = room.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            Bounds b = renderers[0].bounds;
            foreach (Renderer r in renderers) b.Encapsulate(r.bounds);
            view.in2DMode = true;
            view.Frame(b, false);
        }

        // ---------- spawning ----------

        private Vector3 RoomCentreLocal()
        {
            BuildingLayout.RoomSpec spec = Spec;
            return spec != null ? IsoMath.GridToWorld(spec.Width * 0.5f, spec.Height * 0.5f) : Vector3.zero;
        }

        private LayoutObject Spawn(string objectName, Sprite sprite, LayoutKind kind, string idBase)
        {
            var go = new GameObject(objectName);
            Undo.RegisterCreatedObjectUndo(go, "Add layout object");
            go.transform.SetParent(room.transform, false);
            go.transform.localPosition = RoomCentreLocal();
            SpriteRenderer r = go.AddComponent<SpriteRenderer>();
            r.sprite = sprite != null ? sprite : AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderArt.Folder + "/ph_block.png");
            if (sprite == null) r.color = new Color(0.55f, 0.38f, 0.22f, 1f);
            r.sortingOrder = IsoMath.SortingOrderForY(go.transform.position.y);
            LayoutObject tag = go.AddComponent<LayoutObject>();
            tag.Kind = kind;
            tag.Id = NewId(idBase);
            tag.FromCode = false;
            tag.Proxy = true;
            idOwners[tag.Id] = tag;
            Selection.activeGameObject = go;
            Tools.current = Tool.Move;
            return tag;
        }

        private string NewId(string idBase)
        {
            string clean = string.IsNullOrEmpty(idBase) ? "obj" : idBase.ToLowerInvariant().Replace(' ', '_');
            string id;
            do
            {
                id = clean + "_" + System.Guid.NewGuid().ToString("N").Substring(0, 5);
            }
            while (idOwners.ContainsKey(id));
            return id;
        }

        /// <summary>Our own flat (Flat 11): things there are ours, everywhere else they are borrowed ("ödünç al").</summary>
        private bool OwnFlat => roomId != null && roomId.StartsWith("flat11");

        private void SpawnProp(string propName)
        {
            Sprite sprite = null;
            if (props != null) sprite = propName == "CeilingLamp" && props.ChandelierFrames.Length > 0 ? props.ChandelierFrames[0] : props.Find(propName);
            LayoutObject tag = Spawn(propName, sprite, LayoutKind.Prop, propName);
            tag.PropName = propName;
            tag.BlocksNav = PropFootprint.IsSolid(propName);
            if (propName == "Tv")
            {
                // The TV sits on top of the TV unit: start at the unit's height (drag it onto the unit).
                tag.Lift = PropArt.TvUnitHeight;
                tag.transform.localPosition += new Vector3(0f, tag.Lift, 0f);
            }
        }

        private void SpawnPickup(Item item)
        {
            LayoutObject tag = Spawn("Item_" + item.Id, item.Icon, LayoutKind.Pickup, item.Id);
            tag.ItemId = item.Id;
            tag.Borrowed = !OwnFlat;
        }

        private void SpawnContainer(string kind)
        {
            BuildingLayout.ContainerSpec preset = BuildingLayout.ContainerPreset("x", kind);
            Sprite art = props != null ? props.Find(preset.Art) : null;
            LayoutObject tag = Spawn("Container_" + kind, art, LayoutKind.Container, kind);
            if (art == null)
            {
                tag.transform.localScale = new Vector3(preset.Scale.x, preset.Scale.y, 1f);
                tag.BaseScale = preset.Scale;
            }

            tag.ContainerType = kind;
            tag.PropName = preset.Art;
            tag.BlocksNav = true;
            tag.Borrowed = !OwnFlat;
            tag.SearchSeconds = preset.Seconds;
            tag.BuildingCost = preset.Cost;
        }

        private void SpawnLight()
        {
            LayoutObject tag = Spawn("Light_new", null, LayoutKind.Light, "light");
            tag.GetComponent<SpriteRenderer>().color = new Color(1f, 0.85f, 0.4f, 0.6f);
            tag.transform.localScale = new Vector3(0.25f, 0.25f, 1f);
            tag.Lift = 0.8f;
            tag.transform.localPosition += new Vector3(0f, tag.Lift, 0f);
        }

        // ---------- ids (Ctrl+D) ----------

        private void RegisterIds()
        {
            idOwners.Clear();
            if (room == null) return;
            foreach (LayoutObject tag in room.GetComponentsInChildren<LayoutObject>(true)) idOwners[tag.Id ?? ""] = tag;
        }

        /// <summary>A duplicate (Ctrl+D) copies the tag and its id: the copy gets a new id and becomes a new object.</summary>
        private void OnHierarchyChanged()
        {
            if (room == null || EditorApplication.isPlaying) return;
            foreach (LayoutObject tag in room.GetComponentsInChildren<LayoutObject>(true))
            {
                string id = tag.Id ?? "";
                if (idOwners.TryGetValue(id, out LayoutObject owner) && owner != null && owner != tag)
                {
                    Undo.RecordObject(tag, "New layout id");
                    tag.Id = NewId(tag.Kind == LayoutKind.Prop ? tag.PropName : tag.Kind == LayoutKind.Container ? tag.ContainerType : tag.ItemId ?? "obj");
                    tag.FromCode = false;
                    EditorUtility.SetDirty(tag);
                }

                idOwners[tag.Id] = tag;
            }
        }

        // ---------- scene view: snap, grid, locked gizmos ----------

        private void OnSceneGui(SceneView view)
        {
            if (room == null || EditorApplication.isPlaying) return;
            BuildingLayout.RoomSpec spec = Spec;
            if (spec == null) return;
            Transform t = room.transform;

            if (Event.current.rawType == EventType.MouseUp)
            {
                // Doors always snap to a wall gap; the Scene scale tool stays uniform; the rest snaps when enabled.
                SyncSelection(spec);
                if (snap) SnapSelection();
            }

            // Iso grid; blocked nav cells in red (code blocks + furniture that blocks nav right now, scaled footprint).
            var blocked = new HashSet<Vector2Int>(spec.Blocked);
            foreach (LayoutObject tag in room.GetComponentsInChildren<LayoutObject>())
            {
                if (!tag.BlocksNav) continue;
                RoomLayoutOverrides.PlacedObject o = LayoutOverrides.FromTag(tag);
                foreach (Vector2Int cell in BuildingLayout.Footprint(o.gx, o.gy, o.Scale1)) blocked.Add(cell);
            }

            GridNav nav = BuildingLayout.NavFor(spec);
            foreach (Vector2Int c in nav.Cells)
            {
                Vector3[] diamond =
                {
                    t.TransformPoint(IsoMath.GridToWorld(c.x, c.y)), t.TransformPoint(IsoMath.GridToWorld(c.x + 1, c.y)),
                    t.TransformPoint(IsoMath.GridToWorld(c.x + 1, c.y + 1)), t.TransformPoint(IsoMath.GridToWorld(c.x, c.y + 1))
                };
                if (blocked.Contains(c))
                {
                    Handles.DrawSolidRectangleWithOutline(diamond, new Color(1f, 0.15f, 0.1f, 0.3f), new Color(1f, 0.2f, 0.1f, 0.9f));
                }
                else
                {
                    Handles.color = new Color(1f, 1f, 1f, 0.18f);
                    Handles.DrawPolyLine(diamond[0], diamond[1], diamond[2], diamond[3], diamond[0]);
                }
            }

            // Code-only things (placement logic, doors): locked, shown for reference.
            var style = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.6f, 0.85f, 1f, 1f) } };
            foreach (KeyValuePair<Vector2Int, string> p in LayoutOverrides.RequiredPoints(spec))
            {
                if (p.Value.StartsWith("item") || p.Value.StartsWith("container")) continue;
                Vector3 w = t.TransformPoint(IsoMath.GridToWorld(p.Key.x + 0.5f, p.Key.y + 0.5f));
                Handles.color = new Color(0.4f, 0.75f, 1f, 0.9f);
                Handles.DrawWireDisc(w, Vector3.forward, 0.08f);
                Handles.Label(w + new Vector3(0.05f, 0.12f, 0f), "🔒 " + p.Value, style);
            }
        }

        /// <summary>After a drag: a door jumps to the nearest wall gap; a scaled object becomes uniform again.</summary>
        private void SyncSelection(BuildingLayout.RoomSpec spec)
        {
            foreach (GameObject go in Selection.gameObjects)
            {
                LayoutObject tag = go.GetComponent<LayoutObject>();
                if (tag == null || tag.transform.parent != room.transform) continue;
                if (tag.Kind == LayoutKind.Door)
                {
                    string edge = BuildingLayout.NearestWallEdge(spec, IsoMath.WorldToGrid(tag.transform.localPosition));
                    Vector3 at = IsoMath.GridToWorld(BuildingLayout.EdgeCenter(spec, edge).x, BuildingLayout.EdgeCenter(spec, edge).y);
                    if (edge == tag.DoorEdge && (at - tag.transform.localPosition).sqrMagnitude < 1e-6f) continue;
                    Undo.RecordObject(tag, "Move door");
                    Undo.RecordObject(tag.transform, "Move door");
                    PlaceDoor(tag, spec, edge);
                    EditorUtility.SetDirty(tag);
                    continue;
                }

                if (tag.Kind == LayoutKind.Light || Mathf.Abs(tag.BaseScale.x) < 1e-4f) continue;
                float scale = LayoutOverrides.ScaleOf(tag);
                Vector3 uniform = new Vector3(tag.BaseScale.x * scale, tag.BaseScale.y * scale, 1f);
                if ((uniform - tag.transform.localScale).sqrMagnitude < 1e-6f && Mathf.Approximately(scale, tag.ObjectScale)) continue;
                Undo.RecordObject(tag.transform, "Uniform scale");
                Undo.RecordObject(tag, "Uniform scale");
                SetScale(tag, scale);
            }
        }

        private void SnapSelection()
        {
            foreach (GameObject go in Selection.gameObjects)
            {
                LayoutObject tag = go.GetComponent<LayoutObject>();
                if (tag == null || tag.transform.parent != room.transform || tag.Kind == LayoutKind.Door) continue;
                Vector3 local = tag.transform.localPosition - new Vector3(0f, tag.Lift, 0f);
                Vector2 grid = IsoMath.WorldToGrid(local);
                grid = new Vector2(Mathf.Round(grid.x / SnapStep) * SnapStep, Mathf.Round(grid.y / SnapStep) * SnapStep);
                Vector3 snapped = IsoMath.GridToWorld(grid.x, grid.y) + new Vector3(0f, tag.Lift, 0f);
                if ((snapped - tag.transform.localPosition).sqrMagnitude < 1e-6f) continue;
                Undo.RecordObject(tag.transform, "Snap to grid");
                tag.transform.localPosition = snapped;
                IsoSorter sorter = tag.GetComponent<IsoSorter>();
                if (sorter != null) sorter.Apply();
                else
                {
                    SpriteRenderer r = tag.GetComponent<SpriteRenderer>();
                    if (r != null) r.sortingOrder = IsoMath.SortingOrderForY(tag.transform.position.y);
                }
            }
        }

        // ---------- save / revert / rebuild ----------

        /// <summary>Writes the room's edits (against the code layout) into RoomLayoutOverrides.asset.</summary>
        public static int SaveRoomData(Room sceneRoom)
        {
            BuildingLayout.RoomSpec code = BuildingLayout.Rooms().Find(s => s.Id == sceneRoom.RoomId);
            if (code == null) return 0;
            var placed = new List<RoomLayoutOverrides.PlacedObject>();
            foreach (LayoutObject tag in sceneRoom.GetComponentsInChildren<LayoutObject>(true))
            {
                if (tag.transform.parent != sceneRoom.transform) continue;
                // A door is saved by the wall gap nearest to where it was dragged.
                if (tag.Kind == LayoutKind.Door && BuildingLayout.IsWallEdge(tag.DoorEdge ?? "F0")) tag.DoorEdge = BuildingLayout.NearestWallEdge(code, IsoMath.WorldToGrid(tag.transform.localPosition));
                placed.Add(LayoutOverrides.FromTag(tag));
            }

            List<RoomLayoutOverrides.PlacedObject> edits = LayoutOverrides.Diff(code, placed);
            RoomLayoutOverrides asset = LayoutOverrides.LoadOrCreate();
            Undo.RecordObject(asset, "Save room layout");
            asset.SetRoom(sceneRoom.RoomId, edits);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssetIfDirty(asset);
            return edits.Count;
        }

        private void SaveRoom(string id, bool log)
        {
            Room r = id == roomId && room != null ? room : FindSceneRoom(id);
            if (r == null) return;
            int count = SaveRoomData(r);
            Reload();
            if (!log) return;
            Debug.Log($"[Layout] Saved '{id}': {count} edit(s) in {LayoutOverrides.AssetPath}. Rebuild scene to build them for real.");
            BuildingLayout.RoomSpec spec = Spec;
            if (spec != null)
            {
                foreach (string problem in LayoutOverrides.Check(spec)) Debug.LogWarning("[Layout] " + problem);
            }
        }

        private void SaveAll()
        {
            int rooms = 0;
            foreach (Room r in Object.FindObjectsByType<Room>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                SaveRoomData(r);
                rooms++;
            }

            Reload();
            Debug.Log($"[Layout] Saved {rooms} room(s) to {LayoutOverrides.AssetPath}.");
            foreach (string problem in LayoutOverrides.CheckAll()) Debug.LogWarning("[Layout] " + problem);
        }

        private void RevertRoom()
        {
            RoomLayoutOverrides asset = LayoutOverrides.Load();
            if (asset == null || roomId == null) return;
            Undo.RecordObject(asset, "Revert room layout");
            asset.SetRoom(roomId, null);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssetIfDirty(asset);
            Reload();
            Debug.Log($"[Layout] '{roomId}' reverted to the code layout. Rebuild scene to see it.");
        }

        private void Rebuild()
        {
            string keep = roomId;
            ReleaseRoom();
            TestSceneBuilder.Build();
            Reload();
            if (!string.IsNullOrEmpty(keep)) SelectRoom(keep);
        }
    }
}
