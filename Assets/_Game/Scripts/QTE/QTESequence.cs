using System;
using System.Collections.Generic;

namespace SixtyOne.QTE
{
    /// <summary>Pure random WASD sequence generation. Every attempt rolls a fresh sequence; no fixed patterns.</summary>
    public static class QTESequence
    {
        /// <summary>The four keys, in a fixed order for sampling.</summary>
        public static readonly QTEKey[] AllKeys = { QTEKey.W, QTEKey.A, QTEKey.S, QTEKey.D };

        /// <summary>Longest allowed run of the same key (3+ identical in a row feels bad).</summary>
        public const int MaxRun = 2;

        /// <summary>A random length in [minLength, maxLength] (both clamped to at least 1).</summary>
        public static int RollLength(Random rng, int minLength, int maxLength)
        {
            int min = Math.Max(1, minLength);
            int max = Math.Max(min, maxLength);
            return rng.Next(min, max + 1);
        }

        /// <summary>Random W/A/S/D keys; never more than MaxRun identical keys in a row.</summary>
        public static QTEKey[] Generate(Random rng, int length)
        {
            if (rng == null) throw new ArgumentNullException(nameof(rng));
            length = Math.Max(1, length);
            var keys = new QTEKey[length];
            var choices = new List<QTEKey>(AllKeys.Length);
            for (int i = 0; i < length; i++)
            {
                choices.Clear();
                bool runFull = i >= MaxRun && AllSame(keys, i - MaxRun, i);
                foreach (QTEKey key in AllKeys)
                {
                    if (!runFull || key != keys[i - 1]) choices.Add(key);
                }

                keys[i] = choices[rng.Next(choices.Count)];
            }

            return keys;
        }

        /// <summary>Longest run of identical consecutive keys.</summary>
        public static int LongestRun(IReadOnlyList<QTEKey> keys)
        {
            int best = 0;
            int run = 0;
            for (int i = 0; i < keys.Count; i++)
            {
                run = i > 0 && keys[i] == keys[i - 1] ? run + 1 : 1;
                if (run > best) best = run;
            }

            return best;
        }

        private static bool AllSame(QTEKey[] keys, int from, int to)
        {
            for (int i = from + 1; i < to; i++)
            {
                if (keys[i] != keys[from]) return false;
            }

            return true;
        }
    }
}
