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

        /// <summary>Set after the first quake has resolved (power is out from then on).</summary>
        public const string QuakeHappened = "QuakeHappened";

        /// <summary>Set by "Not bırak" in a neighbour's flat (epilogue).</summary>
        public const string LeftNote = "LeftNote";

        /// <summary>Set while the player has switched the flashlight off (backpack pocket click).</summary>
        public const string FlashlightOff = "FlashlightOff";

        /// <summary>The frightened man on the F2 stairs took our food (epilogue).</summary>
        public const string RobbedAtExit = "RobbedAtExit";

        /// <summary>The street aftershock is over: the assembly-area sign can be reached.</summary>
        public const string StreetQuakeDone = "StreetQuakeDone";

        /// <summary>The building timer hit 0 while the family was out of reach on the street (epilogue).</summary>
        public const string CollapsedBehind = "CollapsedBehind";

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
