namespace SixtyOne.Core
{
    /// <summary>
    /// Where a death rewinds to. LastNode = micro rewind (last decision point).
    /// Checkpoint = macro rewind to the current floor's checkpoint (bag, flags, timer and family positions
    /// as they were when the floor started).
    /// </summary>
    public enum RewindTarget
    {
        LastNode = 0,
        Checkpoint = 1
    }
}
