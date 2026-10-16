namespace SixtyOne.Core
{
    /// <summary>Well-known flag names shared by code and generated content.</summary>
    public static class FlagNames
    {
        public const string GasOff = "GasOff";
        public const string PowerOff = "PowerOff";
        public const string WaterOff = "WaterOff";
        public const string PlayerInjured = "PlayerInjured";
        public const string WifeInjured = "WifeInjured";
        public const string ChildInjured = "ChildInjured";

        /// <summary>Flag set when a world pickup has been collected.</summary>
        public static string Picked(string itemId)
        {
            return "Picked_" + itemId;
        }

        /// <summary>Injury flag for a member (e.g. WifeInjured).</summary>
        public static string Injured(MemberType member)
        {
            switch (member)
            {
                case MemberType.Wife: return WifeInjured;
                case MemberType.Child: return ChildInjured;
                default: return PlayerInjured;
            }
        }
    }
}
