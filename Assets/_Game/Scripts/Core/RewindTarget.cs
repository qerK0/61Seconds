namespace SixtyOne.Core
{
    /// <summary>
    /// Where a death rewinds to. LastNode = micro rewind (last decision point).
    /// PrepStart = macro rewind (start of the prep phase: empty bag, family at start positions).
    /// </summary>
    public enum RewindTarget
    {
        LastNode = 0,
        PrepStart = 1
    }
}
