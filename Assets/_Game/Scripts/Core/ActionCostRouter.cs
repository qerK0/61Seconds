namespace SixtyOne.Core
{
    /// <summary>Receives building-time and stamina costs (implemented by the survival driver).</summary>
    public interface IActionCostSink
    {
        /// <summary>Subtracts seconds from the building timer. isMove applies the injured/exhausted multipliers.</summary>
        void SpendTime(float seconds, bool isMove);

        /// <summary>Subtracts stamina.</summary>
        void SpendStamina(float amount);
    }

    /// <summary>Static entry point so nodes, interactables and the bag can pay costs without knowing the driver.</summary>
    public static class ActionCostRouter
    {
        public static IActionCostSink Sink { get; set; }

        public static void SpendTime(float seconds, bool isMove = false)
        {
            if (seconds > 0f && Sink != null) Sink.SpendTime(seconds, isMove);
        }

        public static void SpendStamina(float amount)
        {
            if (amount > 0f && Sink != null) Sink.SpendStamina(amount);
        }

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset()
        {
            Sink = null;
        }
    }
}
