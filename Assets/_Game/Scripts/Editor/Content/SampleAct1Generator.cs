using System.Collections.Generic;
using SixtyOne.Audio;
using SixtyOne.Core;
using SixtyOne.Nodes;
using SixtyOne.Placement;
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
        public AudioEventMap AudioMap;
        public List<AudioCue> Cues = new List<AudioCue>();
        public Dictionary<string, Hazard> Hazards = new Dictionary<string, Hazard>();
        public Dictionary<string, Item> Items = new Dictionary<string, Item>();
        public Dictionary<string, Tip> Tips = new Dictionary<string, Tip>();
    }

    /// <summary>
    /// SixtyOne > Generate Sample Act 1. Creates/updates ScriptableObjects for Act 1 (floor 6):
    /// AFAD alert -> living room placement -> post-quake exploration -> stairs -> Act 2 entry (gas bomb).
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
        public const string Stairwell = "stairwell";
        public const string Floor4 = "floor4";

        public const string NodeAlert = "act1.n01_alert";
        public const string NodePlace = "act1.n02_place";
        public const string NodeExplore = "act1.n03_explore";
        public const string NodeStairs = "act1.n04_stairs";
        public const string NodeAct2Entry = "act2.n01_entry";

        private static readonly string[] TipIds =
        {
            "tip.drop_cover_hold", "tip.no_elevator", "tip.no_balcony", "tip.no_stairs_during",
            "tip.wardrobe", "tip.no_window", "tip.no_doorway", "tip.keep_child_close",
            "tip.no_switch_gas", "tip.no_flame", "tip.leave_quickly", "tip.no_valuables", "tip.gas_off"
        };

        [MenuItem("SixtyOne/Generate Sample Act 1", priority = 10)]
        public static void GenerateMenu()
        {
            Generate();
            Debug.Log("[Generator] Sample Act 1 generated under " + DataRoot + ". Next: SixtyOne > Build Test Scene.");
        }

        public static SampleAct1Assets Generate()
        {
            var result = new SampleAct1Assets();

            // Text settings.
            result.TextSettings = AssetUtil.LoadOrCreate<TextDBSettings>(DataRoot + "/TextDBSettings.asset");
            result.TextSettings.Configure(AssetDatabase.LoadAssetAtPath<TextAsset>(TextDB.DefaultCsvPath));
            if (result.TextSettings.Csv == null)
            {
                Debug.LogWarning("[Generator] strings_tr.csv not found at " + TextDB.DefaultCsvPath);
            }

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

            // Items.
            Item wrench = MakeItem(result, "wrench");
            MakeItem(result, "gloves");
            MakeItem(result, "first_aid");
            MakeItem(result, "flashlight");
            MakeItem(result, "gold").ConfigureTrap(new DeathData("death.gold", tips["tip.no_valuables"]));
            MakeItem(result, "laptop").ConfigureTrap(new DeathData("death.laptop", tips["tip.no_valuables"]));

            // Hazards.
            MakeHazard(result, "hz.wardrobe", HazardEffect.Death, HazardEffect.Death, HazardMotion.Topple, "wardrobe", "wardrobe", tips["tip.wardrobe"], crash);
            MakeHazard(result, "hz.window", HazardEffect.Death, HazardEffect.Injured, HazardMotion.Shatter, "window", "window", tips["tip.no_window"], glass);
            MakeHazard(result, "hz.doorway", HazardEffect.Death, HazardEffect.Injured, HazardMotion.Drop, "door_frame", "doorway", tips["tip.no_doorway"], crash);
            MakeHazard(result, "hz.balcony", HazardEffect.Death, HazardEffect.Death, HazardMotion.Drop, "balcony_rail", "balcony", tips["tip.no_balcony"], crash);
            Hazard openRoom = MakeHazard(result, "hz.open_room", HazardEffect.Death, HazardEffect.Injured, HazardMotion.Drop, "ceiling_lamp", "open_room", tips["tip.drop_cover_hold"], glass);

            // Nodes (create all first so they can reference each other).
            SceneNode n01 = AssetUtil.LoadOrCreate<SceneNode>($"{NodesDir}/Act1/{NodeAlert}.asset");
            PlacementNode n02 = AssetUtil.LoadOrCreate<PlacementNode>($"{NodesDir}/Act1/{NodePlace}.asset");
            SceneNode n03 = AssetUtil.LoadOrCreate<SceneNode>($"{NodesDir}/Act1/{NodeExplore}.asset");
            SceneNode n04 = AssetUtil.LoadOrCreate<SceneNode>($"{NodesDir}/Act1/{NodeStairs}.asset");
            SceneNode a2n01 = AssetUtil.LoadOrCreate<SceneNode>($"{NodesDir}/Act2/{NodeAct2Entry}.asset");

            // 1) AFAD alert: correct move is to place the family (Drop-Cover-Hold On); everything else is a trap.
            n01.Configure(NodeAlert, "act1.n01.title", "act1.n01.body", LivingRoom, 1, 10f);
            n01.SetAfadAlert(true);
            n01.AddChoice(new Choice("act1.n01.c_place").Then(Outcome.GoTo(n02)));
            n01.AddChoice(new Choice("act1.n01.c_elevator", ChoiceType.Trap).Kills(new DeathData("death.elevator", tips["tip.no_elevator"])));
            n01.AddChoice(new Choice("act1.n01.c_balcony", ChoiceType.Trap).Kills(new DeathData("death.balcony_jump", tips["tip.no_balcony"])));
            n01.AddChoice(new Choice("act1.n01.c_stairs", ChoiceType.Trap).Kills(new DeathData("death.stairs_run", tips["tip.no_stairs_during"])));
            n01.SetTimeoutChoice(n01.AddChoice(new Choice("act1.n01.c_freeze", ChoiceType.Trap).AsTimeoutOnly()
                .Kills(new DeathData("death.freeze", tips["tip.drop_cover_hold"]))));

            // 2) Placement in the living room.
            n02.Configure(NodePlace, "act1.n02.title", "act1.n02.body", LivingRoom, 1, 15f);
            n02.ConfigurePlacement(n03, openRoom, new DeathData("death.child_alone", tips["tip.keep_child_close"], MemberType.Child), 4f);

            // 3) Post-quake exploration: pickups in the world, valves, traps.
            n03.Configure(NodeExplore, "act1.n03.title", "act1.n03.body", LivingRoom, 1, 60f);
            n03.AddChoice(new Choice("act1.n03.c_gas")
                .ShowWhen(Condition.FlagFalse(FlagNames.GasOff))
                .Require(Condition.HasItem(wrench))
                .WhenLocked(FailBehaviour.ShowLocked, "act1.n03.c_gas.locked")
                .Then(Outcome.SetFlag(FlagNames.GasOff)));
            n03.AddChoice(new Choice("act1.n03.c_power")
                .ShowWhen(Condition.FlagFalse(FlagNames.PowerOff))
                .Require(Condition.FlagTrue(FlagNames.GasOff))
                .WhenLocked(FailBehaviour.ShowLocked, "act1.n03.c_power.locked")
                .Then(Outcome.SetFlag(FlagNames.PowerOff)));
            n03.AddChoice(new Choice("act1.n03.c_water")
                .ShowWhen(Condition.FlagFalse(FlagNames.WaterOff))
                .Then(Outcome.SetFlag(FlagNames.WaterOff)));
            n03.AddChoice(new Choice("act1.n03.c_light", ChoiceType.Trap)
                .ShowWhen(Condition.FlagFalse(FlagNames.GasOff))
                .Kills(new DeathData("death.switch_gas", tips["tip.no_switch_gas"], cue: fire)));
            n03.AddChoice(new Choice("act1.n03.c_tea", ChoiceType.Trap)
                .Kills(new DeathData("death.stove", tips["tip.no_flame"], cue: fire)));
            n03.AddChoice(new Choice("act1.n03.c_leave").Then(Outcome.GoTo(n04)));
            n03.SetTimeoutChoice(n03.AddChoice(new Choice("act1.n03.c_aftershock", ChoiceType.Trap).AsTimeoutOnly()
                .Kills(new DeathData("death.aftershock", tips["tip.leave_quickly"]))));

            // 4) Stairwell.
            n04.Configure(NodeStairs, "act1.n04.title", "act1.n04.body", Stairwell, 1);
            n04.AddChoice(new Choice("act1.n04.c_down").Then(Outcome.GoTo(a2n01)));
            n04.AddChoice(new Choice("act1.n04.c_elevator", ChoiceType.Trap).Kills(new DeathData("death.elevator_after", tips["tip.no_elevator"])));

            // 5) Act 2 entry: delayed bomb. Gas left open on floor 6 -> kitchen fire. Rewinds to the exploration node.
            a2n01.Configure(NodeAct2Entry, "act2.n01.title", "act2.n01.body", Floor4, 2);
            a2n01.AddBombCheck(new BombCheck()
                .When(Condition.FlagFalse(FlagNames.GasOff))
                .Kills(new DeathData("death.kitchen_fire", tips["tip.gas_off"], MemberType.Player, n03, fire)));

            // Registry.
            result.Content = AssetUtil.LoadOrCreate<GameContent>(DataRoot + "/GameContent.asset");
            result.Content.Configure(n01, new SceneNode[] { n01, n02, n03, n04, a2n01 }, result.Items.Values, result.Tips.Values);

            foreach (Object asset in new Object[] { n01, n02, n03, n04, a2n01, result.Content, result.AudioMap, result.TextSettings })
            {
                EditorUtility.SetDirty(asset);
            }

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
