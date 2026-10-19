namespace SixtyOne.Tutorial
{
    /// <summary>
    /// Ids shared by the tutorial generator (editor) and TutorialDirector: rooms, nodes, choice keys, the container and
    /// the items it teaches with. Text keys live under ui.tutorial.* in strings_tr.csv.
    /// </summary>
    public static class TutorialIds
    {
        public const string Salon = "tut_salon";
        public const string Hall = "tut_hall";

        public const string StartNode = "tut.start";
        public const string QuakeNode = "tut.quake";
        public const string AfterNode = "tut.after";
        public const string DoneNode = "tut.done";

        public const string NodeTitle = "ui.tutorial.node.title";
        public const string QuakeBody = "ui.tutorial.quake.body";
        public const string ChoiceCover = "ui.tutorial.quake.c_cover";
        public const string ChoiceRun = "ui.tutorial.quake.c_run";
        public const string ChoiceWindow = "ui.tutorial.quake.c_window";
        public const string ChoiceExit = "ui.tutorial.exit.c_exit";

        /// <summary>The dolap the player searches in step 2.</summary>
        public const string Container = "tut_cabinet";

        /// <summary>What the dolap holds: the two craft ingredients and the two pocket (key) items.</summary>
        public static readonly string[] LootItems = { "cloth", "water", "flashlight", "whistle" };

        public const string CraftA = "cloth";
        public const string CraftB = "water";
        public const string CraftOutput = "wet_cloth";
    }
}
