namespace SixtyOne.Placement
{
    /// <summary>
    /// True while the family is in a Drop-Cover-Hold during a placement quake (crouching, held or standing back
    /// up): the player mover and the followers do not move. Set only by CrouchDirector.
    /// </summary>
    public static class CrouchGate
    {
        public static bool Frozen { get; set; }
    }
}
