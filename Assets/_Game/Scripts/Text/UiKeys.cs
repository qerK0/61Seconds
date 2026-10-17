namespace SixtyOne.Text
{
    /// <summary>Text keys referenced directly from code (UI chrome and system messages).</summary>
    [TextKeyConstants]
    public static class UiKeys
    {
        public const string Loop = "ui.loop";
        public const string Rewind = "ui.rewind";
        public const string TipHeader = "ui.tip_header";
        public const string JournalOpen = "ui.journal.open";
        public const string JournalTitle = "ui.journal.title";
        public const string JournalCount = "ui.journal.count";
        public const string JournalLocked = "ui.journal.locked";
        public const string JournalClose = "ui.journal.close";
        public const string AfadTitle = "ui.afad.title";
        public const string AfadBody = "ui.afad.body";
        public const string BagTitle = "ui.bag.title";
        public const string BagFull = "ui.bag.full";
        public const string Locked = "ui.locked";
        public const string Quake = "ui.quake";
        public const string PlacementHint = "ui.placement.hint";
        public const string PlacementPlayerLast = "ui.placement.player_last";
        public const string PlacementNoRoom = "ui.placement.no_room";
        public const string PlacementChildAlone = "ui.placement.child_alone";
        public const string PlacementInvalid = "ui.placement.invalid";
        public const string NeedsItem = "ui.needs_item";
        public const string DeathMissingItem = "ui.death.missing_item";
        public const string DialogueContinue = "ui.dialogue.continue";
        public const string StaminaExhausted = "ui.stamina.exhausted";
        public const string StaminaLabel = "ui.stamina.label";
        public const string TimeSpent = "ui.time_spent";
        public const string BuildingTimerLabel = "ui.building.label";
        public const string PrepTimerLabel = "ui.prep.label";
        public const string BuildingAlarm = "ui.building.alarm";
        public const string CraftFail = "ui.craft.fail";
        public const string CraftDone = "ui.craft.done";
        public const string BagSelectHint = "ui.bag.select_hint";
        public const string ItemUsed = "ui.bag.used";
        public const string ItemNotUsable = "ui.bag.not_usable";
        public const string PoisonWarning = "ui.poison.warning";
        public const string Borrowed = "ui.borrowed";

        // Bag: drop, dropped items, bedsheet rope, key items.
        public const string Drop = "ui.bag.drop";
        public const string ItemDropped = "ui.bag.dropped";
        public const string PickedUpAgain = "ui.bag.picked_up";
        public const string RopeTie = "ui.rope.tie";
        public const string RopeTied = "ui.rope.tied";
        public const string RopeNeedsSheets = "ui.rope.needs_sheets";
        public const string RopeDescentMissing = "ui.rope.descent_missing";
        public const string RopeDescentLabel = "ui.rope.descent";
        public const string RopeDescentLocked = "ui.rope.descent_locked";
        public const string KeyItemsTitle = "ui.key_items.title";
        public const string KeyItemOwned = "ui.key_items.owned";
        public const string KeyItemMissing = "ui.key_items.missing";

        // WASD quick-time events.
        public const string QteHint = "ui.qte.hint";
        public const string QteBreakerTitle = "ui.qte.breaker";
        public const string QteDogTitle = "ui.qte.dog";
        public const string QteMiss = "ui.qte.miss";
        public const string QteTimeout = "ui.qte.timeout";

        // Doors, hotspots and the collapsible HUD.
        public const string DoorUnavailable = "ui.door.unavailable";
        public const string NotNow = "ui.not_now";
        public const string BagCollapse = "ui.bag.collapse";
        public const string BagExpand = "ui.bag.expand";

        /// <summary>Room name shown on door hover: room.&lt;roomId&gt;.</summary>
        public static string RoomName(string roomId) => "room." + roomId;
    }
}
