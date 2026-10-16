using SixtyOne.Nodes;

namespace SixtyOne.Editor
{
    public static partial class BuildingLevelsGenerator
    {
        public const string Flat7 = "flat7";
        public const string F2Aftershock = "f2.aftershock";

        private static SceneNode f2ArrivalNode;

        /// <summary>Floor 2 arrival. Placeholder end node until Floor 2 content lands.</summary>
        private static SceneNode Floor2Arrival()
        {
            if (f2ArrivalNode != null && result.Nodes.ContainsKey(F2Aftershock)) return f2ArrivalNode;
            f2ArrivalNode = Node<SceneNode>("Floor2", F2Aftershock);
            Setup(f2ArrivalNode, F2Aftershock, Flat7, 3);
            f2ArrivalNode.SetCheckpoint(true);
            return f2ArrivalNode;
        }
    }
}
