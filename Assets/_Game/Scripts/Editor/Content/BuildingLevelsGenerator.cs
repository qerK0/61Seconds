using System.Collections.Generic;
using System.IO;
using SixtyOne.Audio;
using SixtyOne.Building;
using SixtyOne.Core;
using SixtyOne.Dialogue;
using SixtyOne.Nodes;
using SixtyOne.Placement;
using SixtyOne.Text;
using UnityEditor;
using UnityEngine;

namespace SixtyOne.Editor
{
    /// <summary>References to everything the building generator produced (used by the test scene builder).</summary>
    public sealed class BuildingAssets
    {
        public GameContent Content;
        public TextDBSettings TextSettings;
        public GameSettings Settings;
        public LevelConfig Level;
        public AudioEventMap AudioMap;
        public List<AudioCue> Cues = new List<AudioCue>();
        public Dictionary<string, Hazard> Hazards = new Dictionary<string, Hazard>();
        public Dictionary<string, Item> Items = new Dictionary<string, Item>();
        public Dictionary<string, Tip> Tips = new Dictionary<string, Tip>();
        public Dictionary<string, SceneNode> Nodes = new Dictionary<string, SceneNode>();
        public SixtyOne.Items.TraderProfile Trader;
        public SixtyOne.Items.RecipeBook RecipeBook;
        public SixtyOne.Placement.TransitionConfig Transition;
        public SixtyOne.QTE.QTEConfig QteBreaker;
        public SixtyOne.QTE.QTEConfig QteDog;
        public List<SixtyOne.Items.Recipe> Recipes = new List<SixtyOne.Items.Recipe>();
    }

    /// <summary>
    /// SixtyOne > Generate Building Levels. One earthquake, one building timer, three floors top to bottom
    /// (4 → 3 → 2 → exit). Every node, item, tip, hazard and the LevelConfig is (re)created here; assets this
    /// run no longer produces are deleted so Validate Content stays clean. Text keys follow
    /// &lt;node&gt;.title / .body / .c_&lt;choice&gt; / .l&lt;n&gt; (dialogue lines).
    /// </summary>
    public static partial class BuildingLevelsGenerator
    {
        public const string DataRoot = "Assets/_Game/Data";
        public const string NodesDir = DataRoot + "/Nodes";
        public const string HazardsDir = DataRoot + "/Hazards";
        public const string ItemsDir = DataRoot + "/Items";
        public const string TipsDir = DataRoot + "/Tips";
        public const string AudioDir = DataRoot + "/Audio";

        // Rooms (Room.RoomId). Floors are groups of rooms in one scene.
        public const string Flat11Salon = "flat11_salon";
        public const string Flat11Kitchen = "flat11_kitchen";
        public const string Flat11Hall = "flat11_hall";
        public const string Flat11Bedroom = "flat11_bedroom";
        public const string F4Corridor = "f4_corridor";
        public const string Flat12 = "flat12";
        public const string F3Corridor = "f3_corridor";

        // Flags.
        public const string GasOffF4 = FlagNames.GasOff;
        public const string KnowsFlat12 = "KnowsFlat12";
        public const string IntroCutShort = "IntroCutShort";
        public const string GiftDone = "GiftDone";
        public const string NeighborGone = "NeighborGone";
        public const string SevimDone = "SevimDone";

        /// <summary>Every item id in the game. No firearms anywhere.</summary>
        public static readonly string[] ItemIds =
        {
            "food", "water", "cloth", "rope", "tape", "flashlight", "lighter", "first_aid", "gloves", "whistle",
            "child_harness", "wet_cloth", "insulated_grip", "wrench", "bedsheet", "bedsheet_rope",
            // Trap loot in our own flat: valuables that waste a bag slot and prep seconds.
            "gold", "laptop", "console"
        };

        /// <summary>Key items: no bag slot, not droppable, shown in the corner HUD.</summary>
        public static readonly string[] KeyItemIds = { "flashlight", "whistle" };

        public const string QteDir = DataRoot + "/QTE";

        private static readonly string[] TipIds =
        {
            "tip.drop_cover_hold", "tip.no_elevator", "tip.no_balcony", "tip.wardrobe", "tip.no_window", "tip.no_doorway",
            "tip.keep_child_close", "tip.no_switch_gas", "tip.no_flame", "tip.gas_off", "tip.emergency_bag",
            "tip.collapse", "tip.gas_poison", "tip.electric", "tip.aftershock"
        };

        /// <summary>Entering these nodes blends the cinematic look to "we got out".</summary>
        public static readonly string[] ExitNodeIds = { F2Epilogue };

        private static readonly HashSet<string> Produced = new HashSet<string>();
        private static BuildingAssets result;

        [MenuItem("SixtyOne/Generate Building Levels", priority = 10)]
        public static void GenerateMenu()
        {
            Generate();
            Debug.Log("[Generator] Building levels generated under " + DataRoot + ". Next: SixtyOne > Build Test Scene.");
        }

        public static BuildingAssets Generate()
        {
            Produced.Clear();
            result = new BuildingAssets();
            f3ArrivalNode = null;
            f2ArrivalNode = null;

            result.TextSettings = Asset<TextDBSettings>(DataRoot + "/TextDBSettings.asset");
            result.TextSettings.Configure(AssetDatabase.LoadAssetAtPath<TextAsset>(TextDB.DefaultCsvPath));
            result.Settings = Asset<GameSettings>(DataRoot + "/GameSettings.asset");

            AudioCue rumble = Cue("cue_quake_rumble", 0.9f, loop: true);
            AudioCue death = Cue("cue_death_sting", 1f);
            AudioCue rewind = Cue("cue_rewind", 0.8f);
            AudioCue pickup = Cue("cue_pickup", 0.7f, 0.95f, 1.05f);
            AudioCue alert = Cue("cue_afad_alert", 1f);
            AudioCue denied = Cue("cue_denied", 0.6f);
            AudioCue click = Cue("cue_choice_click", 0.5f, 0.97f, 1.03f);
            Cue("cue_glass_shatter", 0.9f);
            Cue("cue_furniture_crash", 0.9f);
            Cue("cue_fire", 0.9f);
            AudioCue tick = Cue("cue_countdown_tick", 0.8f);
            AudioCue alarm = Cue("cue_building_alarm", 1f);
            AudioCue door = Cue("cue_door", 0.7f, 0.95f, 1.05f);
            AudioCue footsteps = Cue("cue_footsteps", 0.6f, 0.95f, 1.05f);
            AudioCue descend = Cue("cue_descend", 0.8f);
            AudioCue qteHit = Cue("cue_qte_hit", 0.6f, 0.97f, 1.08f);
            AudioCue qteMiss = Cue("cue_qte_miss", 0.9f);
            AudioCue qteSuccess = Cue("cue_qte_success", 0.9f);
            result.AudioMap = Asset<AudioEventMap>(AudioDir + "/AudioEventMap.asset");
            result.AudioMap.Configure(rumble, death, rewind, pickup, alert, denied, click);

            foreach (string tipId in TipIds)
            {
                Tip tip = Asset<Tip>($"{TipsDir}/{tipId}.asset");
                tip.Configure(tipId, tipId + ".title", tipId);
                result.Tips[tipId] = tip;
            }

            BuildItems();
            BuildHazards();
            BuildFloor3Hazards();
            BuildFloor2Hazards();
            BuildRecipes();

            result.Level = Asset<LevelConfig>(DataRoot + "/LevelConfig.asset");
            result.Level.ConfigureTimer(300f, 60f, 30f, "death.collapse", Tip("tip.collapse"), alarm, tick);
            result.Level.ConfigurePoison(GasZones(), Item("wet_cloth"), "death.gas_poison", Tip("tip.gas_poison"));
            result.Level.ConfigurePrep(PrepSeconds, 10);
            result.Level.ConfigureBag(6, Item("bedsheet"), Item("bedsheet_rope"), 3, 5f, 3f);

            result.Transition = Asset<SixtyOne.Placement.TransitionConfig>(DataRoot + "/TransitionConfig.asset");
            result.Transition.Configure(0.35f, 0.6f, 0.6f, 1.2f, door, footsteps, descend);

            // Breaker wiring: 4-6 keys, 1.2s each. Dog evasion: shorter windows, higher penalty (more tension).
            result.QteBreaker = Asset<SixtyOne.QTE.QTEConfig>(QteDir + "/qte.breaker.asset");
            result.QteBreaker.Configure(4, 6, 1.2f, 8f, 5f, qteHit, qteMiss, qteSuccess);
            result.QteDog = Asset<SixtyOne.QTE.QTEConfig>(QteDir + "/qte.dog.asset");
            result.QteDog.Configure(5, 6, 1.0f, 7f, 6f, qteHit, qteMiss, qteSuccess);

            BuildFloor4();
            BuildFloor3();
            BuildFloor2();

            result.Content = Asset<GameContent>(DataRoot + "/GameContent.asset");
            result.Content.Configure(result.Nodes[F4PrepSalon], result.Nodes.Values, result.Items.Values, result.Tips.Values);

            DeleteStaleAssets();
            foreach (string path in Produced)
            {
                Object asset = AssetDatabase.LoadMainAssetAtPath(path);
                if (asset != null) EditorUtility.SetDirty(asset);
            }

            AssetDatabase.SaveAssets();
            return result;
        }

        // ---------- items, hazards, gas ----------

        private static void BuildItems()
        {
            foreach (string id in ItemIds)
            {
                Item item = Asset<Item>($"{ItemsDir}/item.{id}.asset");
                item.Configure(id, $"item.{id}.name", $"item.{id}.desc");
                result.Items[id] = item;
            }

            Item("food").ConfigureUse(35f, false, 2f);
            Item("water").ConfigureUse(25f, false, 2f);
            Item("first_aid").ConfigureUse(0f, true, 6f);
            foreach (string id in KeyItemIds) Item(id).ConfigureKeyItem();
        }

        private static void BuildHazards()
        {
            // Flat 11 salon (opening quake).
            Hazard("hz.wardrobe", HazardEffect.Death, HazardEffect.Death, HazardMotion.Topple, "wardrobe", "wardrobe", "tip.wardrobe");
            Hazard("hz.window", HazardEffect.Death, HazardEffect.Injured, HazardMotion.Shatter, "window", "window", "tip.no_window");
            Hazard("hz.doorway", HazardEffect.Death, HazardEffect.Injured, HazardMotion.Drop, "door_frame", "doorway", "tip.no_doorway");
            Hazard("hz.balcony", HazardEffect.Death, HazardEffect.Death, HazardMotion.Drop, "balcony_rail", "balcony", "tip.no_balcony");
            Hazard("hz.open_room", HazardEffect.Death, HazardEffect.Injured, HazardMotion.Drop, "ceiling_lamp", "open_room", "tip.drop_cover_hold");
        }

        private static IEnumerable<GasZone> GasZones()
        {
            yield return new GasZone(Flat11Kitchen, GasOffF4, 0.025f);
            yield return new GasZone(Flat10, GasOffF3, 0.03f);
            yield return new GasZone(Flat7, GasOffF2, 0.035f);
        }

        // ---------- helpers ----------

        private static T Asset<T>(string path) where T : ScriptableObject
        {
            Produced.Add(path);
            return AssetUtil.LoadOrCreate<T>(path);
        }

        private static AudioCue Cue(string id, float volume, float pitchMin = 1f, float pitchMax = 1f, bool loop = false)
        {
            AudioCue cue = Asset<AudioCue>($"{AudioDir}/{id}.asset");
            cue.Configure(id, volume, loop, pitchMin, pitchMax);
            result.Cues.Add(cue);
            return cue;
        }

        private static Item Item(string id) => result.Items[id];
        private static Tip Tip(string id) => result.Tips[id];

        private static void Hazard(string id, HazardEffect player, HazardEffect wife, HazardMotion motion, string propId, string textSuffix, string tipId)
        {
            Hazard hazard = Asset<Hazard>($"{HazardsDir}/{id}.asset");
            hazard.Configure(id, ActMask.All, player, wife, HazardEffect.Trapped, motion, propId);
            hazard.ConfigureText($"death.{textSuffix}", $"death.wife.{textSuffix}", $"death.child.{textSuffix}", Tip(tipId));
            result.Hazards[id] = hazard;
        }

        private static T Node<T>(string floor, string id) where T : SceneNode
        {
            T node = Asset<T>($"{NodesDir}/{floor}/{id}.asset");
            result.Nodes[id] = node;
            return node;
        }

        /// <summary>Configures a node with the standard key pattern (id.title / id.body).</summary>
        private static T Setup<T>(T node, string id, string room, int floorStage, float timer = 0f) where T : SceneNode
        {
            node.Configure(id, id + ".title", id + ".body", room, floorStage, timer);
            return node;
        }

        /// <summary>Choice with the key id.c_suffix.</summary>
        private static Choice C(string nodeId, string suffix, ChoiceType type = ChoiceType.Normal)
        {
            return new Choice($"{nodeId}.c_{suffix}", type);
        }

        /// <summary>Locked-reason key for a choice: id.c_suffix.locked.</summary>
        private static TextKey Locked(string nodeId, string suffix) => $"{nodeId}.c_{suffix}.locked";

        private static DeathData Death(string textKey, string tipId, SceneNode rewindTo = null)
        {
            return new DeathData(textKey, Tip(tipId), MemberType.Player, rewindTo);
        }

        private static DialogueLine Line(string speakerKey, string nodeId, int n, float seconds)
        {
            return new DialogueLine(speakerKey, $"{nodeId}.l{n}", seconds);
        }

        /// <summary>Deletes node/item/tip/hazard assets from earlier designs that this run did not produce.</summary>
        private static void DeleteStaleAssets()
        {
            string[] folders = { NodesDir, ItemsDir, TipsDir, HazardsDir, DataRoot + "/Traders", DataRoot + "/Recipes", QteDir };
            foreach (string folder in folders)
            {
                if (!AssetDatabase.IsValidFolder(folder)) continue;
                string[] guids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { folder });
                if (guids == null) continue;
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!Produced.Contains(path))
                    {
                        AssetDatabase.DeleteAsset(path);
                        Debug.Log("[Generator] Deleted stale asset " + path);
                    }
                }
            }

            // Old act folders hold PrepPhaseNode assets whose script is gone (missing script: FindAssets cannot type them).
            foreach (string stale in new[] { NodesDir + "/Act1", NodesDir + "/Act2" })
            {
                if (AssetDatabase.IsValidFolder(stale) && Directory.Exists(stale))
                {
                    AssetDatabase.DeleteAsset(stale);
                    Debug.Log("[Generator] Deleted old folder " + stale);
                }
            }
        }
    }
}
