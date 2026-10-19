namespace SixtyOne.Core
{
    /// <summary>Health state of a family member. The child never reaches Dead, only Trapped.</summary>
    public enum MemberHealth
    {
        Healthy = 0,
        Injured = 1,
        Trapped = 2,
        Dead = 3
    }
}
