using System.Collections.Generic;
using SixtyOne.Audio;
using SixtyOne.Core;
using SixtyOne.Nodes;
using SixtyOne.Placement;
using SixtyOne.Prep;
using SixtyOne.Text;
using UnityEditor;
using UnityEngine;

namespace SixtyOne.Editor
{
    /// <summary>References to everything the sample generator produced (used by the test scene builder).</summary>
    public sealed class SampleAct1Assets
    {
        public GameContent Content;
        public TextDBSettings TextSettings;
        public GameSettings Settings;
        public AudioEventMap AudioMap;
        public List<AudioCue> Cues = new List<AudioCue>();
        public Dictionary<string, Hazard> Hazards = new Dictionary<string, Hazard>();
        public Dictionary<string, Item> Items = new Dictionary<string, Item>();
        public Dictionary<string, Tip> Tips = new Dictionary<string, Tip>();
    }

    /// <summary>
    /// SixtyOne > Generate Sample Act 1. Creates/updates ScriptableObjects for Act 1 (floor 6):
    /// AFAD 61s alert + prep (3 rooms) -> placement window -> quake -> exploration -> jammed door
    /// (ShowLockedFatal without the crowbar) -> stairs -> Act 2 entry (gas bomb).
    /// Idempotent: re-running overwrites the same assets.
    /// </summary>
    public static class SampleAct1Generator
    {
        public const string DataRoot = "Assets/_Game/Data";
        public const string NodesDir = DataRoot + "/Nodes";
        public const string HazardsDir = DataRoot + "/Hazards";
        public const string ItemsDir = DataRoot + "/Items";
        public const string TipsDir = DataRoot + "/Tips";
        public const string AudioDir = DataRoot + "/Audio";

        public const string LivingRoom = "living_room";
        public const string Kitchen = "kitchen";
        public const string Bedroom = "bedroom";
        public const string Stairwell = "stairwell";
        public const string Floor4 = "floor4";

        public const string NodePrep = "act1.n01_prep";
        public const string NodePlace = "act1.n02_place";
        public const string NodeExplore = "act1.n03_explore";
        public const string NodeDoor = "act1.n04_door";
        public const string NodeStairs = "act1.n05_stairs";
        public const string NodeAct2Entry = "act2.n01_entry";

        /// <summary>Act 1 countdown and placement window (seconds).</summary>
        public const float PrepSeconds = 61f;
        public const float PlacementWindowSeconds = 10f;

        /// <summary>Rooms the player can visit during the Act 1 prep phase.</summary>
        public static readonly string[] PrepRooms = { LivingRoom, Kitchen, Bedroom };

        private static readonly string[] TipIds =
        {
            "tip.drop_cover_hold", "tip.no_elevator", "tip.no_balcony", "tip.wardrobe", "tip.no_window",
            "tip.no_doorway", "tip.keep_child_close", "tip.no_switch_gas", "tip.no_flame", "tip.leave_quickly",
            "tip.gas_off", "tip.emergency_bag"
        };

        private static readonly string[] UsefulItems = { "wrench", "crowbar", "gloves", "first_aid", "flashlight", "whistle", "water" };
        private static readonly string[] TrapItems = { "gold", "laptop", "console" };

        [MenuItem("SixtyOne/Generate Sample Act 1", priority = 10)]
        public static void GenerateMenu()
        {
            Generate();
            Debug.Log("[Generator] Sample Act 1 generated under " + DataRoot + ". Next: SixtyOne > Build Test Scene.");
        }

        public static SampleAct1Assets Generate()
        {
            var result = new SampleAct1Assets();

            // Text + settings.
            result.TextSettings = AssetUtil.LoadOrCreate<TextDBSettings>(DataRoot + "/TextDBSettings.asset");
            result.TextSettings.Configure(AssetDatabase.LoadAssetAtPath<TextAsset>(TextDB.DefaultCsvPath));
            if (result.TextSettings.Csv == null)
            {
                Debug.LogWarning("[Generator] strings_tr.csv not found at " + TextDB.DefaultCsvPath);
            }

            result.Settings = AssetUtil.LoadOrCreate<GameSettings>(DataRoot + "/GameSettings.asset");

            // Audio cues (clips left empty for the sound designer).
            AudioCue rumble = Cue(result, "cue_quake_rumble", 0.9f, loop: true);
            AudioCue death = Cue(result, "cue_death_sting", 1f);
            AudioCue rewind = Cue(result, "cue_rewind", 0.8f);
            AudioCue pickup = Cue(result, "cue_pickup", 0.7f, pitchMin: 0.95f, pitchMax: 1.05f);
            AudioCue alert = Cue(result, "cue_afad_alert", 1f);
            AudioCue denied = Cue(result, "cue_denied", 0.6f);
            AudioCue click = Cue(result, "cue_choice_click", 0.5f, pitchMin: 0.97f, pitchMax: 1.03f);
            AudioCue glass = Cue(result, "cue_glass_shatter", 0.9f);
            AudioCue crash = Cue(result, "cue_furniture_crash", 0.9f);
            AudioCue fire = Cue(result, "cue_fire", 0.9f);
            AudioCue tick = Cue(result, "cue_countdown_tick", 0.8f);
            AudioCue placeAlarm = Cue(result, "cue_placement_alarm", 1f);

            result.AudioMap = AssetUtil.LoadOrCreate<AudioEventMap>(AudioDir + "/AudioEventMap.asset");
            result.AudioMap.Configure(rumble, death, rewind, pickup, alert, denied, click);

            // Tips.
            foreach (string tipId in TipIds)
            {
                Tip tip = AssetUtil.LoadOrCreate<Tip>($"{TipsDir}/{tipId}.asset");
                tip.Configure(tipId, tipId + ".title", tipId);
                result.Tips[tipId] = tip;
            }

            Dictionary<string, Tip> tips = result.Tips;

            // Items: useful gear and useless valuables (they only waste a slot and prep time).
            foreach (string id in UsefulItems) MakeItem(result, id);
            foreach (string id in TrapItems) MakeItem(result, id).ConfigureTrap();
            Item wrench = result.Items["wrench"];
            Item crowbar = result.Items["crowbar"];

            // Hazards.
            MakeHazard(result, "hz.wardrobe", HazardEffect.Death, HazardEffect.Death, HazardMotion.Topple, "wardrobe", "wardrobe", tips["tip.wardrobe"], crash);
            MakeHazard(result, "hz.window", HazardEffect.Death, HazardEffect.Injured, HazardMotion.Shatter, "window", "window", tips["tip.no_window"], glass);
            MakeHazard(result, "hz.doorway", HazardEffect.Death, HazardEffect.Injured, HazardMotion.Drop, "door_frame", "doorway", tips["tip.no_doorway"], crash);
            MakeHazard(result, "hz.balcony", HazardEffect.Death, HazardEffect.Death, HazardMotion.Drop, "balcony_rail", "balcony", tips["tip.no_balcony"], crash);
            Hazard openRoom = MakeHazard(result, "hz.open_room", HazardEffect.Death, HazardEffect.Injured, HazardMotion.Drop, "ceiling_lamp", "open_room", tips["tip.drop_cover_hold"], glass);

            // Nodes (create all first so they can reference each other).
            PrepPhaseNode prep = AssetUtil.LoadOrCreate<PrepPhaseNode>($"{NodesDir}/Act1/{NodePrep}.asset");
            PlacementNode place = AssetUtil.LoadOrCreate<PlacementNode>($"{NodesDir}/Act1/{NodePlace}.asset");
            SceneNode explore = AssetUtil.LoadOrCreate<SceneNode>($"{NodesDir}/Act1/{NodeExplore}.asset");
            SceneNode door = AssetUtil.LoadOrCreate<SceneNode>($"{NodesDir}/Act1/{NodeDoor}.asset");
            SceneNode stairs = AssetUtil.LoadOrCreate<SceneNode>($"{NodesDir}/Act1/{NodeStairs}.asset");
            SceneNode act2Entry = AssetUtil.LoadOrCreate<SceneNode>($"{NodesDir}/Act2/{NodeAct2Entry}.asset");

            // 1) AFAD "61 saniye" alert -> prep in three rooms (gear is world interactables, see Build Test Scene).
            //    The panel offers the classic do-not-do panic moves as traps.
            prep.Configure(NodePrep, "act1.n01.title", "act1.n01.body", LivingRoom, 1);
            prep.ConfigurePrep(PrepSeconds, PlacementWindowSeconds, place, PrepRooms);
            prep.ConfigureAlerts("act1.prep.alert.title", "act1.prep.alert.body", "act1.prep.place.title", "act1.prep.place.body", tick, alert, placeAlarm);
            prep.AddChoice(new Choice("act1.n01.c_elevator", ChoiceType.Trap).Kills(new DeathData("death.elevator", tips["tip.no_elevator"])));
            prep.AddChoice(new Choice("act1.n01.c_balcony", ChoiceType.Trap).Kills(new DeathData("death.balcony_jump", tips["tip.no_balcony"])));

            // 2) Placement for the last 10 seconds (timer must equal the prep placement window).
            place.Configure(NodePlace, "act1.n02.title", "act1.n02.body", LivingRoom, 1, PlacementWindowSeconds);
            place.ConfigurePlacement(explore, openRoom, new DeathData("death.child_alone", tips["tip.keep_child_close"], MemberType.Child), 4f);

            // 3) Post-quake exploration: valves (if not done during prep) and traps.
            explore.Configure(NodeExplore, "act1.n03.title", "act1.n03.body", LivingRoom, 1, 60f);
            explore.AddChoice(new Choice("act1.n03.c_gas")
                .ShowWhen(Condition.FlagFalse(FlagNames.GasOff))
                .Require(Condition.HasItem(wrench))
                .WhenLocked(FailBehaviour.ShowLocked, "act1.n03.c_gas.locked")
                .Then(Outcome.SetFlag(FlagNames.GasOff)));
            explore.AddChoice(new Choice("act1.n03.c_power")
                .ShowWhen(Condition.FlagFalse(FlagNames.PowerOff))
                .Require(Condition.FlagTrue(FlagNames.GasOff))
                .WhenLocked(FailBehaviour.ShowLocked, "act1.n03.c_power.locked")
                .Then(Outcome.SetFlag(FlagNames.PowerOff)));
            explore.AddChoice(new Choice("act1.n03.c_water")
                .ShowWhen(Condition.FlagFalse(FlagNames.WaterOff))
                .Then(Outcome.SetFlag(FlagNames.WaterOff)));
            explore.AddChoice(new Choice("act1.n03.c_light", ChoiceType.Trap)
                .ShowWhen(Condition.FlagFalse(FlagNames.GasOff))
                .Kills(new DeathData("death.switch_gas", tips["tip.no_switch_gas"], cue: fire)));
            explore.AddChoice(new Choice("act1.n03.c_tea", ChoiceType.Trap)
                .Kills(new DeathData("death.stove", tips["tip.no_flame"], cue: fire)));
            explore.AddChoice(new Choice("act1.n03.c_leave").Then(Outcome.GoTo(door)));
            explore.SetTimeoutChoice(explore.AddChoice(new Choice("act1.n03.c_aftershock", ChoiceType.Trap).AsTimeoutOnly()
                .Kills(new DeathData("death.aftershock", tips["tip.leave_quickly"]))));

            // 4) Jammed door: without the crowbar this kills ("Keşke yanıma alsaydım...") and macro-rewinds to the prep start.
            door.Configure(NodeDoor, "act1.n04.title", "act1.n04.body", LivingRoom, 1);
            door.AddChoice(new Choice("act1.n04.c_crowbar")
                .FatalWithout(crowbar, "act1.n04.c_crowbar.locked", "death.missing.crowbar")
                .WithDeath(new DeathData("death.missing.crowbar", tips["tip.emergency_bag"], target: RewindTarget.PrepStart))
                .Then(Outcome.GoTo(stairs)));
            door.AddChoice(new Choice("act1.n04.c_jump", ChoiceType.Trap).Kills(new DeathData("death.door_jump", tips["tip.no_balcony"])));

            // 5) Stairwell.
            stairs.Configure(NodeStairs, "act1.n05.title", "act1.n05.body", Stairwell, 1);
            stairs.AddChoice(new Choice("act1.n05.c_down").Then(Outcome.GoTo(act2Entry)));
            stairs.AddChoice(new Choice("act1.n05.c_elevator", ChoiceType.Trap).Kills(new DeathData("death.elevator_after", tips["tip.no_elevator"])));

            // 6) Act 2 entry: delayed bomb. Gas left open on floor 6 (neither in prep nor after the quake) -> kitchen fire.
            //    Rewinds to the exploration node, where the gas can still be closed.
            act2Entry.Configure(NodeAct2Entry, "act2.n01.title", "act2.n01.body", Floor4, 2);
            act2Entry.AddBombCheck(new BombCheck()
                .When(Condition.FlagFalse(FlagNames.GasOff))
                .Kills(new DeathData("death.kitchen_fire", tips["tip.gas_off"], MemberType.Player, explore, fire)));

            // Registry.
            SceneNode[] nodes = { prep, place, explore, door, stairs, act2Entry };
            result.Content = AssetUtil.LoadOrCreate<GameContent>(DataRoot + "/GameContent.asset");
            result.Content.Configure(prep, nodes, result.Items.Values, result.Tips.Values);

            foreach (Object asset in nodes) EditorUtility.SetDirty(asset);
            foreach (Object asset in new Object[] { result.Content, result.AudioMap, result.TextSettings, result.Settings }) EditorUtility.SetDirty(asset);
            foreach (Object asset in result.Tips.Values) EditorUtility.SetDirty(asset);
            foreach (Object asset in result.Items.Values) EditorUtility.SetDirty(asset);
            foreach (Object asset in result.Hazards.Values) EditorUtility.SetDirty(asset);
            foreach (Object asset in result.Cues) EditorUtility.SetDirty(asset);

            AssetDatabase.SaveAssets();
            return result;
        }

        private static AudioCue Cue(SampleAct1Assets result, string id, float volume, bool loop = false, float pitchMin = 1f, float pitchMax = 1f)
        {
            AudioCue cue = AssetUtil.LoadOrCreate<AudioCue>($"{AudioDir}/{id}.asset");
            cue.Configure(id, volume, loop, pitchMin, pitchMax);
            result.Cues.Add(cue);
            return cue;
        }

        private static Item MakeItem(SampleAct1Assets result, string id)
        {
            Item item = AssetUtil.LoadOrCreate<Item>($"{ItemsDir}/item.{id}.asset");
            item.Configure(id, $"item.{id}.name", $"item.{id}.desc");
            result.Items[id] = item;
            return item;
        }

        private static Hazard MakeHazard(SampleAct1Assets result, string id, HazardEffect player, HazardEffect wife, HazardMotion motion, string propId, string textSuffix, Tip tip, AudioCue cue)
        {
            Hazard hazard = AssetUtil.LoadOrCreate<Hazard>($"{HazardsDir}/{id}.asset");
            hazard.Configure(id, ActMask.All, player, wife, HazardEffect.Trapped, motion, propId);
            hazard.ConfigureText($"death.{textSuffix}", $"death.wife.{textSuffix}", $"death.child.{textSuffix}", tip, cue);
            result.Hazards[id] = hazard;
            return hazard;
        }
    }
}
