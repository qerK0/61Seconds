using System.Collections.Generic;

namespace SixtyOne.Core
{
    /// <summary>
    /// Pure rules of the floor-transition info cards ("Deprem Bilgisi", cards 1..8): which floor stage a node is on,
    /// which card a transition or a macro rewind prefers, and which card is actually shown (the preferred one if
    /// unseen, else the next unseen, cycling once all are seen).
    /// </summary>
    public static class InfoCardRules
    {
        public const int CardCount = 8;
        public const int StreetStage = 4;
        public const string StreetRoomId = "street";

        /// <summary>Floor stage: 1 = floor 4, 2 = floor 3, 3 = floor 2, 4 = the street.</summary>
        public static int StageFor(int act, string roomId) => roomId == StreetRoomId ? StreetStage : act;

        /// <summary>True when entering this stage after the previous one is a floor transition (0 = no previous).</summary>
        public static bool IsTransition(int previousStage, int stage) => previousStage > 0 && stage > previousStage;

        /// <summary>The preferred card when arriving on a stage: F3 = 3 (stairs), F2 = 4 (gas), street = 8 (outside).</summary>
        public static int PreferredForStage(int stage)
        {
            switch (stage)
            {
                case 2: return 3;
                case 3: return 4;
                case StreetStage: return 8;
                default: return 0;
            }
        }

        /// <summary>The card linked to a death cause (its tip id), or 0 when none matches.</summary>
        public static int PreferredForDeath(string tipId)
        {
            switch (tipId)
            {
                case "tip.gas_off":
                case "tip.gas_poison":
                case "tip.no_flame":
                case "tip.no_switch_gas":
                    return 4;
                case "tip.electric":
                    return 5;
                case "tip.wardrobe":
                case "tip.no_window":
                case "tip.no_doorway":
                case "tip.no_balcony":
                    return 2;
                case "tip.no_elevator":
                    return 3;
                case "tip.collapse":
                case "tip.supplies":
                case "tip.emergency_bag":
                    return 6;
                case "tip.street":
                    return 8;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// The card to show: the preferred card if unseen; else the next unseen after it (wrapping); with nothing
        /// unseen, the preferred card, or the one after the last shown. preferred 0 = no preference.
        /// </summary>
        public static int Pick(int preferred, ICollection<int> seen, int lastShown)
        {
            int start = preferred >= 1 && preferred <= CardCount ? preferred : 1;
            for (int i = 0; i < CardCount; i++)
            {
                int card = (start - 1 + i) % CardCount + 1;
                if (seen == null || !seen.Contains(card)) return card;
            }

            if (preferred >= 1 && preferred <= CardCount) return preferred;
            return lastShown >= 1 && lastShown <= CardCount ? lastShown % CardCount + 1 : 1;
        }
    }
}
