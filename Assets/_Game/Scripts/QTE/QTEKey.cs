namespace SixtyOne.QTE
{
    /// <summary>The only keys a quick-time event ever asks for.</summary>
    public enum QTEKey
    {
        W,
        A,
        S,
        D
    }

    /// <summary>Where a QTE is used (drives the view's title and feedback).</summary>
    public enum QTEContext
    {
        /// <summary>Fuse box / breaker: each correct key connects one wire.</summary>
        BreakerWiring,

        /// <summary>Slip past the frightened dog.</summary>
        DogEvasion
    }

    /// <summary>Why an attempt failed.</summary>
    public enum QTEFailReason
    {
        None,
        WrongKey,
        KeyTimeout,
        TotalTimeout
    }

    /// <summary>Result of one key press.</summary>
    public enum QTEPressResult
    {
        /// <summary>Not running (finished or failed); the press does nothing.</summary>
        Ignored,

        /// <summary>Correct key; more keys follow.</summary>
        Hit,

        /// <summary>Wrong key: the attempt failed.</summary>
        Miss,

        /// <summary>Correct last key: the attempt succeeded.</summary>
        Completed
    }
}
