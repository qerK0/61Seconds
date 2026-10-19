using System;
using System.Collections.Generic;

namespace SixtyOne.Text
{
    /// <summary>Pure checks on the strings CSV (used by ContentValidator and LocalizationTests).</summary>
    public static class TextLint
    {
        /// <summary>Marker that must never appear in shipped text.</summary>
        public const string PlaceholderMarker = "TODO";

        /// <summary>Rows whose key, text or notes contain "TODO" (any case), joined with commas.</summary>
        public static List<string> TodoRows(string csvContent)
        {
            var rows = new List<string>();
            if (string.IsNullOrEmpty(csvContent)) return rows;
            foreach (List<string> row in CsvParser.Parse(csvContent))
            {
                foreach (string cell in row)
                {
                    if (cell != null && cell.IndexOf(PlaceholderMarker, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        rows.Add(string.Join(",", row));
                        break;
                    }
                }
            }

            return rows;
        }
    }
}
