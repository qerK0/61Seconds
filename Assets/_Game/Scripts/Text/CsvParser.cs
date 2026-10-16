using System.Collections.Generic;
using System.Text;

namespace SixtyOne.Text
{
    /// <summary>
    /// RFC 4180 CSV parser: quoted fields, "" escapes, commas and newlines inside quotes,
    /// CRLF/LF line endings, optional UTF-8 BOM. Completely blank lines are skipped.
    /// </summary>
    public static class CsvParser
    {
        public static List<List<string>> Parse(string text)
        {
            var rows = new List<List<string>>();
            if (string.IsNullOrEmpty(text))
            {
                return rows;
            }

            var row = new List<string>();
            var field = new StringBuilder();
            bool inQuotes = false;
            bool fieldQuoted = false;
            int i = text[0] == '﻿' ? 1 : 0;

            for (; i < text.Length; i++)
            {
                char c = text[i];
                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < text.Length && text[i + 1] == '"')
                        {
                            field.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        field.Append(c);
                    }

                    continue;
                }

                switch (c)
                {
                    case '"':
                        inQuotes = true;
                        fieldQuoted = true;
                        break;
                    case ',':
                        row.Add(field.ToString());
                        field.Clear();
                        fieldQuoted = false;
                        break;
                    case '\r':
                    case '\n':
                        if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                        {
                            i++;
                        }

                        EndRow(rows, ref row, field, fieldQuoted);
                        fieldQuoted = false;
                        break;
                    default:
                        field.Append(c);
                        break;
                }
            }

            if (field.Length > 0 || row.Count > 0 || fieldQuoted)
            {
                EndRow(rows, ref row, field, fieldQuoted);
            }

            return rows;
        }

        private static void EndRow(List<List<string>> rows, ref List<string> row, StringBuilder field, bool fieldQuoted)
        {
            bool blankLine = row.Count == 0 && field.Length == 0 && !fieldQuoted;
            if (!blankLine)
            {
                row.Add(field.ToString());
                rows.Add(row);
                row = new List<string>();
            }

            field.Clear();
        }

        /// <summary>Quotes a value for writing if it contains a comma, quote or newline.</summary>
        public static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            bool needsQuotes = value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0;
            return needsQuotes ? "\"" + value.Replace("\"", "\"\"") + "\"" : value;
        }
    }
}
