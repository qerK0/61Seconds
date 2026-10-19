using System.Collections.Generic;
using SixtyOne.Nodes;
using SixtyOne.Tutorial;
using UnityEditor;
using UnityEngine;

namespace SixtyOne.Editor
{
    /// <summary>
    /// SixtyOne > Tutorial > Generate Tutorial Content: the tutorial's own nodes and GameContent under Data/Tutorial
    /// (kept out of Data/Nodes, so the building generator never deletes them and the building checks skip them).
    /// tut.start (explore) → tut.quake (decision: Çök-Kapan-Tutun goes on; running out or the window explain and ask
    /// again, TutorialDirector shows why) → tut.after (explore; the exit door plays its choice) → tut.done.
    /// Items and tips are the game's own.
    /// </summary>
    public static class TutorialContentGenerator
    {
        public const string DataDir = BuildingLevelsGenerator.DataRoot + "/Tutorial";
        public const string ContentPath = DataDir + "/TutorialContent.asset";

        [MenuItem("SixtyOne/Tutorial/Generate Tutorial Content", priority = 31)]
        public static void GenerateMenu()
        {
            Generate();
            Debug.Log("[Tutorial] Content generated under " + DataDir + ".");
        }

        public static GameContent Generate()
        {
            AssetUtil.EnsureFolder(DataDir);
            var main = AssetDatabase.LoadAssetAtPath<GameContent>(BuildingLevelsGenerator.DataRoot + "/GameContent.asset");
            if (main == null) Debug.LogError("[Tutorial] Data/GameContent.asset is missing: run SixtyOne > Generate Building Levels first.");

            SceneNode start = Node(TutorialIds.StartNode, TutorialIds.Salon, string.Empty);
            SceneNode quake = Node(TutorialIds.QuakeNode, TutorialIds.Salon, TutorialIds.QuakeBody);
            SceneNode after = Node(TutorialIds.AfterNode, TutorialIds.Salon, string.Empty);
            SceneNode done = Node(TutorialIds.DoneNode, TutorialIds.Hall, string.Empty);

            // The right answer in the middle; the wrong ones have no outcome (the same node lists them again).
            quake.AddChoice(new Choice(TutorialIds.ChoiceRun));
            quake.AddChoice(new Choice(TutorialIds.ChoiceCover).Then(Outcome.GoTo(after)));
            quake.AddChoice(new Choice(TutorialIds.ChoiceWindow));
            after.AddChoice(new Choice(TutorialIds.ChoiceExit).Then(Outcome.GoTo(done)));

            var nodes = new List<SceneNode> { start, quake, after, done };
            GameContent content = AssetUtil.LoadOrCreate<GameContent>(ContentPath);
            content.Configure(start, nodes, main != null ? main.Items : new Item[0], main != null ? main.Tips : new Tip[0]);

            foreach (SceneNode node in nodes) EditorUtility.SetDirty(node);
            EditorUtility.SetDirty(content);
            AssetDatabase.SaveAssets();
            return content;
        }

        private static SceneNode Node(string id, string room, string bodyKey)
        {
            SceneNode node = AssetUtil.LoadOrCreate<SceneNode>($"{DataDir}/{id}.asset");
            node.Configure(id, TutorialIds.NodeTitle, bodyKey, room, 1);
            return node;
        }
    }
}
