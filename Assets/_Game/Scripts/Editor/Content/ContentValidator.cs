using System.Collections.Generic;
using SixtyOne.Building;
using SixtyOne.Core;
using SixtyOne.Dialogue;
using SixtyOne.Items;
using SixtyOne.Nodes;
using SixtyOne.QTE;
using SixtyOne.Placement;
using SixtyOne.Prep;
using SixtyOne.World;
using SixtyOne.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SixtyOne.Editor
{
    /// <summary>
    /// SixtyOne > Validate Content. Checks: GoToNode targets, Trap/Death text + tip, ShowLockedFatal
    /// missing-item text + item, dialogue lines (keys, cost &gt;= 0), recipes (inputs + output exist), trader
    /// items, every gas room has a valve choice, checkpoints reachable, every placement moment has anchors
    /// (safe + trap) and hazards in its room (open scene), hazards linked to an anchor or used as an unplaced
    /// hazard, world interactables wired, no firearm item ids, key items (flashlight, whistle) flagged IsKeyItem,
    /// bag/rope config, QTE nodes (config + next node) and rope descent points, reachability from the start node,
    /// that every referenced text key exists, and that strings_tr.csv has no "TODO" anywhere (fails hard).
    /// Each problem is a clickable console entry.
    /// </summary>
    public static class ContentValidator
    {
        private const string Root = "Assets/_Game";

        [MenuItem("SixtyOne/Validate Content", priority = 12)]
        public static void Validate()
        {
            int problems = 0;
            List<SceneNode> nodes = LoadAll<SceneNode>();
            AddAll<PlacementNode>(nodes);
            AddAll<DialogueNode>(nodes);
            AddAll<QTENode>(nodes);
            AddAll<EpilogueNode>(nodes);
            AddAll<PrepPhaseNode>(nodes);

            foreach (SceneNode node in nodes)
            {
                problems += CheckNode(node);
            }

            foreach (Hazard hazard in LoadAll<Hazard>())
            {
                problems += CheckHazard(hazard);
            }

            problems += CheckHazardLinks(nodes);
            problems += CheckWorldInteractables();
            problems += CheckRecipes();
            problems += CheckTraders();
            problems += CheckGasValves(nodes);
            problems += CheckPlacementRooms(nodes);
            problems += CheckNoFirearms();
            problems += CheckKeyItems();
            problems += CheckBagConfig();
            problems += CheckQteConfigs();
            problems += CheckRopeDescents();
            problems += CheckDoors(nodes);
            problems += CheckNoTodoStrings();
            problems += CheckReachability(nodes);
            problems += CheckPrep(nodes);
            problems += CheckTextKeys();

            string summary = $"[Content] Validate: {nodes.Count} nodes checked, {problems} problem(s).";
            if (problems > 0) Debug.LogWarning(summary); else Debug.Log(summary);
        }

        private static int CheckNode(SceneNode node)
        {
            int problems = 0;
            for (int i = 0; i < node.Choices.Count; i++)
            {
                Choice choice = node.Choices[i];
                if (choice == null) continue;
                string where = $"node '{node.Id}' choice {i} ({choice.LabelKey.Key})";
                problems += CheckOutcomes(choice.Outcomes, node, where);

                bool kills = choice.Type == ChoiceType.Trap || HasDeath(choice.Outcomes);
                if (kills) problems += CheckDeath(choice.Death, node, where);
                if (choice.Type == ChoiceType.Trap && !HasDeath(choice.Outcomes))
                {
                    problems += Report($"{where} is a Trap without a Death outcome (it still kills).", node);
                }

                if (choice.FailBehaviour == FailBehaviour.ShowLockedFatal)
                {
                    problems += CheckLockedFatal(choice, node, where);
                }
            }

            problems += CheckOutcomes(node.OnEnter, node, $"node '{node.Id}' onEnter");
            for (int i = 0; i < node.BombChecks.Count; i++)
            {
                BombCheck bomb = node.BombChecks[i];
                if (bomb == null) continue;
                string where = $"node '{node.Id}' bomb {i}";
                problems += CheckOutcomes(bomb.Outcomes, node, where);
                if (HasDeath(bomb.Outcomes)) problems += CheckDeath(bomb.Death, node, where);
            }

            if (node.TimerSeconds > 0f && !(node is PlacementNode) && node.GetTimeoutChoice() == null)
            {
                problems += Report($"node '{node.Id}' has a timer but no timeout choice and no Trap choice.", node);
            }

            if (node is DialogueNode dialogue)
            {
                if (dialogue.Lines.Count == 0) problems += Report($"dialogue node '{node.Id}' has no lines.", node);
                for (int i = 0; i < dialogue.Lines.Count; i++)
                {
                    DialogueLine line = dialogue.Lines[i];
                    if (line == null || line.TextKey.IsEmpty) problems += Report($"dialogue node '{node.Id}' line {i} has no text key.", node);
                    else if (line.SpeakerKey.IsEmpty) problems += Report($"dialogue node '{node.Id}' line {i} has no speaker key.", node);
                    if (line != null && line.TimeCost < 0f) problems += Report($"dialogue node '{node.Id}' line {i} has a negative time cost.", node);
                }
            }

            if (node is QTENode qte)
            {
                if (qte.Config == null) problems += Report($"QTE node '{node.Id}' has no QTEConfig.", node);
                if (qte.NextNode == null) problems += Report($"QTE node '{node.Id}' has no next node.", node);
                if (qte.PromptKey.IsEmpty) problems += Report($"QTE node '{node.Id}' has no prompt key.", node);
                problems += CheckOutcomes(qte.SuccessOutcomes, node, $"QTE node '{node.Id}' success");
                problems += CheckOutcomes(qte.FailOutcomes, node, $"QTE node '{node.Id}' fail");
            }


            if (node is PlacementNode placement)
            {
                if (placement.NextNode == null) problems += Report($"placement node '{node.Id}' has no next node.", node);
                if (placement.TimerSeconds <= 0f) problems += Report($"placement node '{node.Id}' needs a timer (the quake starts on timeout).", node);
                problems += CheckDeath(placement.ChildAloneDeath, node, $"placement node '{node.Id}' child-alone death");
            }

            return problems;
        }

        private static int CheckLockedFatal(Choice choice, Object owner, string where)
        {
            int problems = 0;
            if (choice.MissingItem == null) problems += Report($"{where}: ShowLockedFatal without a missing item.", owner);
            if (choice.MissingItemTextKey.IsEmpty) problems += Report($"{where}: ShowLockedFatal without a missing-item text key.", owner);
            if (choice.Death == null || choice.Death.Tip == null) problems += Report($"{where}: ShowLockedFatal has no tip (set it with WithDeath).", owner);

            bool requiresItem = false;
            foreach (Condition condition in choice.Conditions)
            {
                if (condition == null) continue;
                if (condition.Type == ConditionType.HasItem && condition.Item == choice.MissingItem) requiresItem = true;
                if (condition.Type == ConditionType.HasAnyItem && Contains(condition.AnyItems, choice.MissingItem)) requiresItem = true;
            }

            if (choice.MissingItem != null && !requiresItem)
            {
                problems += Report($"{where}: ShowLockedFatal does not require its missing item '{choice.MissingItem.Id}' (HasItem condition).", owner);
            }

            return problems;
        }

        private static int CheckWorldInteractables()
        {
            int problems = 0;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    foreach (WorldInteractable interactable in root.GetComponentsInChildren<WorldInteractable>(true))
                    {
                        string where = $"interactable '{interactable.Id}'";
                        if (interactable.LabelKey.IsEmpty) problems += Report($"{where} has no label key.", interactable);
                        if (interactable.GrantsItem == null && interactable.Outcomes.Count == 0) problems += Report($"{where} does nothing (no item, no outcomes).", interactable);
                    }
                }
            }

            return problems;
        }

        private static int CheckRecipes()
        {
            int problems = 0;
            foreach (Recipe recipe in LoadAll<Recipe>())
            {
                if (recipe.InputA == null || recipe.InputB == null) problems += Report($"recipe '{recipe.name}' is missing an input item.", recipe);
                if (recipe.Output == null) problems += Report($"recipe '{recipe.name}' has no output item.", recipe);
                if (recipe.TimeCost < 0f || recipe.StaminaCost < 0f) problems += Report($"recipe '{recipe.name}' has a negative cost.", recipe);
            }

            foreach (RecipeBook book in LoadAll<RecipeBook>())
            {
                foreach (Recipe recipe in book.Recipes)
                {
                    if (recipe == null) problems += Report($"recipe book '{book.name}' has an empty entry.", book);
                }
            }

            return problems;
        }

        private static int CheckTraders()
        {
            int problems = 0;
            foreach (TraderProfile trader in LoadAll<TraderProfile>())
            {
                if (trader.Fallback.Count == 0) problems += Report($"trader '{trader.name}' has no fallback food/water.", trader);
                foreach (Item item in trader.Needs) if (item == null) problems += Report($"trader '{trader.name}' has an empty need entry.", trader);
                foreach (Item item in trader.Fallback) if (item == null) problems += Report($"trader '{trader.name}' has an empty fallback entry.", trader);
            }

            return problems;
        }

        /// <summary>Every gas zone needs a choice in that room that sets its valve flag (otherwise poison is unavoidable).</summary>
        private static int CheckGasValves(List<SceneNode> nodes)
        {
            int problems = 0;
            foreach (LevelConfig level in LoadAll<LevelConfig>())
            {
                foreach (GasZone zone in level.GasZones)
                {
                    if (zone == null) continue;
                    if (string.IsNullOrEmpty(zone.ValveFlag))
                    {
                        problems += Report($"gas zone in room '{zone.RoomId}' has no valve flag.", level);
                        continue;
                    }

                    bool hasValve = false;
                    foreach (SceneNode node in nodes)
                    {
                        if (node.RoomId != zone.RoomId) continue;
                        foreach (Choice choice in node.Choices)
                        {
                            if (choice != null && SetsFlag(choice.Outcomes, zone.ValveFlag)) hasValve = true;
                        }
                    }

                    if (!hasValve) problems += Report($"gas room '{zone.RoomId}' has no choice that closes its valve ('{zone.ValveFlag}').", level);
                }
            }

            return problems;
        }

        /// <summary>Placement moments need a safe anchor, a trap anchor with a hazard (open scene) and an unplaced hazard.</summary>
        private static int CheckPlacementRooms(List<SceneNode> nodes)
        {
            int problems = 0;
            var safe = new Dictionary<string, int>();
            var traps = new Dictionary<string, int>();
            bool anyRoom = false;
            foreach (Anchor anchor in FindAnchors())
            {
                Room room = anchor.GetComponentInParent<Room>(true);
                if (room == null) continue;
                anyRoom = true;
                Dictionary<string, int> bucket = anchor.Tag == AnchorTag.Trap ? traps : safe;
                if (anchor.Tag == AnchorTag.Trap && anchor.Hazard == null) continue;
                bucket.TryGetValue(room.RoomId, out int count);
                bucket[room.RoomId] = count + 1;
            }

            foreach (SceneNode node in nodes)
            {
                if (!(node is PlacementNode placement)) continue;
                if (placement.UnplacedHazard == null) problems += Report($"placement node '{node.Id}' has no unplaced hazard.", node);
                if (!anyRoom) continue;
                if (!safe.ContainsKey(node.RoomId)) problems += Report($"placement node '{node.Id}': room '{node.RoomId}' has no safe anchor (open Test_Building).", node);
                if (!traps.ContainsKey(node.RoomId)) problems += Report($"placement node '{node.Id}': room '{node.RoomId}' has no trap anchor with a hazard.", node);
            }

            if (!anyRoom) Debug.Log("[Content] No room in the open scene; placement anchor checks skipped (open Test_Building to run them).");
            return problems;
        }

        private static readonly string[] FirearmWords = { "gun", "pistol", "rifle", "shotgun", "revolver", "firearm", "silah", "tabanca", "tufek", "tüfek" };

        private static int CheckNoFirearms()
        {
            int problems = 0;
            foreach (Item item in LoadAll<Item>())
            {
                string id = (item.Id ?? string.Empty).ToLowerInvariant();
                foreach (string word in FirearmWords)
                {
                    if (id.Contains(word)) problems += Report($"item '{item.Id}' looks like a firearm; firearms are not allowed in this game.", item);
                }
            }

            return problems;
        }

        /// <summary>The flashlight and whistle must be key items (no bag slot, not droppable, corner HUD).</summary>
        private static int CheckKeyItems()
        {
            int problems = 0;
            var items = LoadAll<Item>();
            foreach (string id in BuildingLevelsGenerator.KeyItemIds)
            {
                Item item = items.Find(i => i.Id == id);
                if (item == null) problems += Report($"key item '{id}' does not exist.", null);
                else if (!item.IsKeyItem) problems += Report($"item '{id}' must have IsKeyItem = true.", item);
            }

            return problems;
        }

        private static int CheckBagConfig()
        {
            int problems = 0;
            foreach (LevelConfig level in LoadAll<LevelConfig>())
            {
                if (level.MaxInventorySlots < 1) problems += Report("LevelConfig has no bag slots.", level);
                if (level.BedsheetItem == null || level.BedsheetRopeItem == null) problems += Report("LevelConfig is missing the bedsheet or bedsheet rope item.", level);
                if (level.RequiredBedsheetsForRope > level.MaxInventorySlots) problems += Report("LevelConfig needs more bedsheets for the rope than the bag can hold.", level);
            }

            return problems;
        }

        private static int CheckQteConfigs()
        {
            int problems = 0;
            foreach (QTEConfig config in LoadAll<QTEConfig>())
            {
                if (config.SequenceLengthMin < 1) problems += Report($"QTE config '{config.name}' has an empty sequence.", config);
                if (config.TimePerKey <= 0f) problems += Report($"QTE config '{config.name}' needs a time per key above 0.", config);
            }

            return problems;
        }

        /// <summary>Rope descent points in open scenes need the rope item and a target node.</summary>
        private static int CheckRopeDescents()
        {
            int problems = 0;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    foreach (RopeDescentInteractable rope in root.GetComponentsInChildren<RopeDescentInteractable>(true))
                    {
                        if (rope.RequiredItem == null) problems += Report($"rope descent '{rope.Id}' has no required item.", rope);
                        if (rope.TargetNode == null) problems += Report($"rope descent '{rope.Id}' has no target node.", rope);
                        if (rope.FamilyItem == null) problems += Report($"rope descent '{rope.Id}' has no family item (the child must come down safely).", rope);
                        if (rope.FamilyItem != null && rope.LockedKey.IsEmpty) problems += Report($"rope descent '{rope.Id}' has no locked reason.", rope);
                    }
                }
            }

            return problems;
        }

        /// <summary>Fails if any key, text or note in strings_tr.csv contains "TODO" (any case).</summary>
        private static int CheckNoTodoStrings()
        {
            TextAsset csv = AssetDatabase.LoadAssetAtPath<TextAsset>(TextDB.DefaultCsvPath);
            string content = csv != null ? csv.text : (System.IO.File.Exists(TextDB.DefaultCsvPath) ? System.IO.File.ReadAllText(TextDB.DefaultCsvPath) : null);
            if (content == null) return Report("strings_tr.csv not found; cannot check for TODO strings.", null);

            int problems = 0;
            foreach (string row in TextLint.TodoRows(content))
            {
                // Hard failure: placeholder text must never ship.
                Debug.LogError("[Content] strings_tr.csv row contains TODO: " + row, csv);
                problems++;
            }

            return problems;
        }

        private static bool SetsFlag(IReadOnlyList<Outcome> outcomes, string flag)
        {
            foreach (Outcome o in outcomes)
            {
                if (o != null && o.Type == OutcomeType.SetFlag && o.Flag == flag && o.FlagValue) return true;
            }

            return false;
        }

        private static bool Contains(IReadOnlyList<Item> items, Item item)
        {
            if (items == null) return false;
            foreach (Item i in items) if (i == item) return true;
            return false;
        }

        private static int CheckOutcomes(IReadOnlyList<Outcome> outcomes, Object owner, string where)
        {
            int problems = 0;
            foreach (Outcome outcome in outcomes)
            {
                if (outcome == null) continue;
                if (outcome.Type == OutcomeType.GoToNode && outcome.TargetNode == null)
                {
                    problems += Report($"{where}: GoToNode without a target.", owner);
                }
                else if (outcome.Type == OutcomeType.SetFlag && string.IsNullOrEmpty(outcome.Flag))
                {
                    problems += Report($"{where}: SetFlag without a flag name.", owner);
                }
                else if ((outcome.Type == OutcomeType.AddItem || outcome.Type == OutcomeType.RemoveItem) && outcome.Item == null)
                {
                    problems += Report($"{where}: {outcome.Type} without an item.", owner);
                }
            }

            return problems;
        }

        private static int CheckDeath(DeathData death, Object owner, string where)
        {
            int problems = 0;
            if (death == null || death.TextKey.IsEmpty) problems += Report($"{where}: death has no text key.", owner);
            if (death == null || death.Tip == null) problems += Report($"{where}: death has no tip.", owner);
            return problems;
        }

        private static int CheckHazard(Hazard hazard)
        {
            int problems = 0;
            if (hazard.TextKey.IsEmpty) problems += Report($"hazard '{hazard.Id}' has no text key.", hazard);
            if (hazard.Tip == null) problems += Report($"hazard '{hazard.Id}' has no tip.", hazard);
            if (hazard.Acts == ActMask.None) problems += Report($"hazard '{hazard.Id}' is active in no act.", hazard);
            return problems;
        }

        private static int CheckHazardLinks(List<SceneNode> nodes)
        {
            int problems = 0;
            var linked = new HashSet<Hazard>();
            foreach (Anchor anchor in FindAnchors())
            {
                if (anchor.Hazard != null) linked.Add(anchor.Hazard);
                else if (anchor.Tag == AnchorTag.Trap) problems += Report($"Trap anchor '{anchor.Id}' has no hazard.", anchor);
            }

            foreach (SceneNode node in nodes)
            {
                if (node is PlacementNode placement && placement.UnplacedHazard != null) linked.Add(placement.UnplacedHazard);
            }

            foreach (Hazard hazard in LoadAll<Hazard>())
            {
                if (!linked.Contains(hazard))
                {
                    problems += Report($"hazard '{hazard.Id}' is not linked to any anchor (open the scene that uses it, e.g. Test_Building).", hazard);
                }
            }

            return problems;
        }

        private static int CheckReachability(List<SceneNode> nodes)
        {
            var contents = LoadAll<GameContent>();
            if (contents.Count == 0)
            {
                return Report("No GameContent asset found; cannot check reachability.", null);
            }

            int problems = 0;
            foreach (GameContent content in contents)
            {
                if (content.StartNode == null)
                {
                    problems += Report($"GameContent '{content.name}' has no start node.", content);
                    continue;
                }

                var reached = new HashSet<SceneNode>();
                var queue = new Queue<SceneNode>();
                queue.Enqueue(content.StartNode);
                while (queue.Count > 0)
                {
                    SceneNode node = queue.Dequeue();
                    if (node == null || !reached.Add(node)) continue;
                    foreach (SceneNode next in Targets(node)) queue.Enqueue(next);
                }

                foreach (SceneNode node in nodes)
                {
                    if (reached.Contains(node)) continue;
                    string what = node.IsCheckpoint ? "floor checkpoint" : "node";
                    problems += Report($"{what} '{node.Id}' is unreachable from '{content.StartNode.Id}'.", node);
                }

                foreach (SceneNode node in reached)
                {
                    if (content.FindNode(node.Id) == null)
                    {
                        problems += Report($"node '{node.Id}' is reachable but missing from GameContent '{content.name}' (rewinds to it will fail).", content);
                    }
                }
            }

            return problems;
        }

        /// <summary>
        /// Pre-quake prep: every prep node links a placement; with prep configured the start node must be the prep
        /// checkpoint (macro rewinds return to the loot phase); the AFAD alert has keys; prep-only interactables
        /// sit in prep rooms (otherwise they could never be used).
        /// </summary>
        private static int CheckPrep(List<SceneNode> nodes)
        {
            int problems = 0;
            var prepRooms = new HashSet<string>();
            foreach (SceneNode node in nodes)
            {
                if (!(node is PrepPhaseNode prep)) continue;
                prepRooms.Add(prep.RoomId);
                if (prep.QuakeNode == null) problems += Report($"prep node '{prep.Id}' has no quake (placement) node.", prep);
                if (prep.ShowAlertOnEnter && (prep.AlertTitleKey.IsEmpty || prep.AlertBodyKey.IsEmpty))
                {
                    problems += Report($"prep node '{prep.Id}' shows the AFAD alert but has no title/body key.", prep);
                }

                if (prep.TimerSeconds > 0f) problems += Report($"prep node '{prep.Id}' must be untimed (the prep countdown drives it).", prep);
            }

            foreach (LevelConfig level in LoadAll<LevelConfig>())
            {
                if (level.PrepSeconds <= 0f) continue;
                foreach (GameContent content in LoadAll<GameContent>())
                {
                    if (!(content.StartNode is PrepPhaseNode start) || !start.IsCheckpoint)
                    {
                        problems += Report($"LevelConfig has {level.PrepSeconds}s prep but the start node of '{content.name}' is not a checkpoint PrepPhaseNode.", content);
                    }
                }

                if (level.PrepTickFrom > level.PrepSeconds) problems += Report("LevelConfig prepTickFrom is longer than the prep itself.", level);
            }

            Scene scene = SceneManager.GetActiveScene();
            if (scene.IsValid() && scene.isLoaded)
            {
                foreach (WorldInteractable w in Object.FindObjectsByType<WorldInteractable>(FindObjectsInactive.Include))
                {
                    if (!w.PrepOnly) continue;
                    Room room = w.GetComponentInParent<Room>(true);
                    if (room == null || !prepRooms.Contains(room.RoomId))
                    {
                        problems += Report($"prep-only interactable '{w.Id}' is not in a prep room; it can never be used.", w);
                    }
                }
            }

            return problems;
        }

        private static IEnumerable<SceneNode> Targets(SceneNode node)
        {
            foreach (Choice choice in node.Choices)
            {
                if (choice == null) continue;
                foreach (Outcome o in choice.Outcomes)
                {
                    if (o != null && o.Type == OutcomeType.GoToNode && o.TargetNode != null) yield return o.TargetNode;
                }
            }

            foreach (Outcome o in node.OnEnter)
            {
                if (o != null && o.Type == OutcomeType.GoToNode && o.TargetNode != null) yield return o.TargetNode;
            }

            if (node is PlacementNode placement && placement.NextNode != null) yield return placement.NextNode;
            if (node is QTENode qte && qte.NextNode != null) yield return qte.NextNode;
            if (node is PrepPhaseNode prep && prep.QuakeNode != null) yield return prep.QuakeNode;
        }

        private static int CheckTextKeys()
        {
            TextMenu.Reload();
            Dictionary<string, List<Object>> refs = TextKeyCollector.Collect();
            int problems = 0;
            foreach (string key in TextKeyCollector.FindMissing(refs, includeEmpty: false))
            {
                List<Object> owners = refs[key];
                problems += Report($"text key '{key}' is missing from strings_tr.csv.", owners.Count > 0 ? owners[0] : null);
            }

            return problems;
        }

        private static bool HasDeath(IReadOnlyList<Outcome> outcomes)
        {
            foreach (Outcome o in outcomes)
            {
                if (o != null && o.Type == OutcomeType.Death) return true;
            }

            return false;
        }

        private static IEnumerable<Anchor> FindAnchors()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    foreach (Anchor anchor in root.GetComponentsInChildren<Anchor>(true)) yield return anchor;
                }
            }

            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { Root }))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (prefab == null) continue;
                foreach (Anchor anchor in prefab.GetComponentsInChildren<Anchor>(true)) yield return anchor;
            }
        }

        private static void AddAll<T>(List<SceneNode> nodes) where T : SceneNode
        {
            foreach (T node in LoadAll<T>())
            {
                if (!nodes.Contains(node)) nodes.Add(node);
            }
        }

        private static List<T> LoadAll<T>() where T : ScriptableObject
        {
            var list = new List<T>();
            foreach (string guid in AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { Root }))
            {
                T asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null) list.Add(asset);
            }

            return list;
        }

        /// <summary>
        /// Doors lead to existing rooms with an arrival point; hotspots name a real choice; every move choice of an
        /// exploration node that leaves its zone has a door in that zone (the old room buttons are gone).
        /// </summary>
        private static int CheckDoors(List<SceneNode> nodes)
        {
            int problems = 0;
            Room[] rooms = Object.FindObjectsByType<Room>(FindObjectsInactive.Include);
            if (rooms.Length == 0) return 0;
            var zoneOf = new Dictionary<string, string>();
            foreach (Room room in rooms) zoneOf[room.RoomId] = room.ZoneId;
            System.Func<string, string> zone = id => id != null && zoneOf.TryGetValue(id, out string z) ? z : id;

            var choiceKeys = new HashSet<string>();
            foreach (SceneNode node in nodes) foreach (Choice c in node.Choices) if (c != null) choiceKeys.Add(c.LabelKey.Key);

            foreach (Room room in rooms)
            {
                foreach (RoomLink link in room.Links)
                {
                    if (link is Door door)
                    {
                        if (!zoneOf.ContainsKey(door.TargetRoomId)) problems += Report($"door in '{room.RoomId}' leads to unknown room '{door.TargetRoomId}'.", door);
                        if (door.ArrivalPoint == null) problems += Report($"door in '{room.RoomId}' to '{door.TargetRoomId}' has no arrival point.", door);
                    }
                    else if (link is ChoiceHotspot hotspot && !choiceKeys.Contains(hotspot.ChoiceKey))
                    {
                        problems += Report($"hotspot '{hotspot.name}' in '{room.RoomId}' plays unknown choice '{hotspot.ChoiceKey}'.", hotspot);
                    }
                }
            }

            foreach (SceneNode node in nodes)
            {
                if (node is DialogueNode || node is PlacementNode || node is QTENode || node is EpilogueNode) continue;
                string here = zone(node.RoomId);
                foreach (Choice c in node.Choices)
                {
                    if (c == null || !c.IsMove) continue;
                    foreach (Outcome o in c.Outcomes)
                    {
                        if (o == null || o.Type != OutcomeType.GoToNode || o.TargetNode == null) continue;
                        string there = zone(o.TargetNode.RoomId);
                        if (there == here) continue;
                        bool hasDoor = false;
                        foreach (Room room in rooms)
                        {
                            if (room.ZoneId != here) continue;
                            foreach (RoomLink link in room.Links) if (link is Door d && zone(d.TargetRoomId) == there) hasDoor = true;
                        }

                        // Heavy descents (cables, collapsed floor) stay panel choices on purpose.
                        if (!hasDoor && !c.Heavy) problems += Report($"node '{node.Id}' move '{c.LabelKey.Key}' to zone '{there}' has no door in zone '{here}' (it stays a panel button).", node);
                    }
                }
            }

            return problems;
        }

        private static int Report(string message, Object context)
        {
            Debug.LogWarning("[Content] " + message, context);
            return 1;
        }
    }
}
