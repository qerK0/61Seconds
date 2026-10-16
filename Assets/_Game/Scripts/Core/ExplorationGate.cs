namespace SixtyOne.Core
{
    /// <summary>
    /// True while the player may click world interactables (plain exploration nodes). False during
    /// dialogue, placement, puzzles and death. Set by NodePlayer.
    /// </summary>
    public static class ExplorationGate
    {
        public static bool Allowed { get; set; }

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset()
        {
            Allowed = false;
        }
    }
}
