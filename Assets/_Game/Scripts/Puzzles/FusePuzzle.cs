namespace SixtyOne.Puzzles
{
    /// <summary>Pure fuse-box logic: a row of switches that must match a solution pattern.</summary>
    public static class FusePuzzle
    {
        /// <summary>True if every switch matches the solution (lengths must match).</summary>
        public static bool Matches(bool[] switches, bool[] solution)
        {
            if (switches == null || solution == null || switches.Length != solution.Length) return false;
            for (int i = 0; i < switches.Length; i++)
            {
                if (switches[i] != solution[i]) return false;
            }

            return true;
        }

        /// <summary>Flips one switch (ignores out-of-range indexes).</summary>
        public static void Toggle(bool[] switches, int index)
        {
            if (switches != null && index >= 0 && index < switches.Length) switches[index] = !switches[index];
        }
    }
}
