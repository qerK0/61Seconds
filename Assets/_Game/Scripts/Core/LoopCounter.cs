namespace SixtyOne.Core
{
    /// <summary>Counts deaths/rewinds. Persists across snapshot restores.</summary>
    public sealed class LoopCounter
    {
        /// <summary>Number of rewinds so far.</summary>
        public int Count { get; private set; }

        public LoopCounter(int start = 0)
        {
            Count = start;
        }

        public int Increment()
        {
            return ++Count;
        }

        public void Reset()
        {
            Count = 0;
        }
    }
}
