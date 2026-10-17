using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace SixtyOne.Placement
{
    /// <summary>
    /// Pure frame ordering for sprite sequences named like "mehmetrun (10).png": sorted by the number in
    /// parentheses, so (2) comes before (10). Shared by the frame importer and the tests.
    /// </summary>
    public static class FrameOrder
    {
        private static readonly Regex Number = new Regex(@"\((\d+)\)");

        /// <summary>The number in parentheses of a frame file name or path, or 0 when there is none.</summary>
        public static int NumberOf(string path)
        {
            if (string.IsNullOrEmpty(path)) return 0;
            Match match = Number.Match(Path.GetFileNameWithoutExtension(path));
            return match.Success ? int.Parse(match.Groups[1].Value) : 0;
        }

        /// <summary>Sorts in place by frame number (ties keep a stable ordinal name order).</summary>
        public static void Sort(List<string> paths)
        {
            paths.Sort((a, b) =>
            {
                int byNumber = NumberOf(a).CompareTo(NumberOf(b));
                return byNumber != 0 ? byNumber : string.CompareOrdinal(a, b);
            });
        }
    }
}
