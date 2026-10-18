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
        public const string PanicLabel = "ui.panic.label";
        public const string StreetGlassDeath = "death.street_glass";
        public const string StreetPowerDeath = "death.street_power";
        public const string TimeSpent = "ui.time_spent";
        public const string BuildingTimerLabel = "ui.building.label";
        public const string PrepTimerLabel = "ui.prep.label";
        public const string BuildingAlarm = "ui.building.alarm";
        public const string CraftFail = "ui.craft.fail";
        public const string CraftDone = "ui.craft.done";
        public const string CraftTitle = "ui.craft.title";
        public const string CraftCombine = "ui.craft.combine";
        public const string CraftNeedsTwo = "ui.craft.needs_two";
        public const string CraftTimes = "ui.craft.times";
        public const string CraftUnknown = "ui.craft.unknown";
        public const string CraftSlotEmpty = "ui.craft.slot_empty";
        public const string CraftHint = "ui.craft.hint";
        public const string CraftOpen = "ui.craft.open";
        public const string EndingFallback = "ui.ending.fallback";
        public const string BagSelectHint = "ui.bag.select_hint";
        public const string ItemUsed = "ui.bag.used";
        public const string ItemNotUsable = "ui.bag.not_usable";
        public const string PoisonWarning = "ui.poison.warning";
        public const string MaskWorn = "ui.mask.worn";
        public const string StairsSign = "ui.sign.stairs";
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

        public const string MenuTitle = "ui.menu.title";
        public const string MenuStart = "ui.menu.start";
        public const string MenuSettings = "ui.menu.settings";
        public const string MenuQuit = "ui.menu.quit";
        public const string MenuSubtitle = "ui.menu.subtitle";
        public const string MenuGallery = "ui.menu.gallery";
        public const string MenuQuitConfirm = "ui.menu.quit_confirm";
        public const string MenuHudQuake = "ui.menu.hud_quake";
        public const string MenuHudCoords = "ui.menu.hud_coords";
        public const string MenuHudFeed = "ui.menu.hud_feed";
        public const string MenuHudZone = "ui.menu.hud_zone";
        public const string MenuVersion = "ui.menu.version";
        public const string MenuGalleryTitle = "ui.menu.gallery_title";
        public const string MenuGalleryCount = "ui.menu.gallery_count";
        public const string EndTitle = "ui.end.title";
        public const string EndLine = "ui.end.line";
        public const string EndLineHighlight = "ui.end.line_highlight";
        public const string EndPunchline = "ui.end.punchline";
        public const string EndStatLoops = "ui.end.stat_loops";
        public const string EndStatTips = "ui.end.stat_tips";
        public const string EndStatTime = "ui.end.stat_time";
        public const string EndStatTimeValue = "ui.end.stat_time_value";
        public const string EndStatCollapsed = "ui.end.stat_collapsed";
        public const string EndStatFamily = "ui.end.stat_family";
        public const string EndFamilySafe = "ui.end.family_safe";
        public const string EndFamilyInjured = "ui.end.family_injured";
        public const string EndRetry = "ui.end.retry";
        public const string EndMenu = "ui.end.menu";
        public const string EndAfad = "ui.end.afad";
        public const string SettingsTitle = "ui.settings.title";
        public const string SettingsMaster = "ui.settings.master";
        public const string SettingsMusic = "ui.settings.music";
        public const string SettingsSfx = "ui.settings.sfx";
        public const string SettingsReduce = "ui.settings.reduce";
        public const string SettingsBack = "ui.settings.back";
        public const string PauseTitle = "ui.pause.title";
        public const string PauseResume = "ui.pause.resume";
        public const string PauseSettings = "ui.pause.settings";
        public const string PauseMainMenu = "ui.pause.main_menu";
        public const string PauseQuit = "ui.pause.quit";
        public const string PauseConfirm = "ui.pause.confirm";
        public const string PauseYes = "ui.pause.yes";
        public const string PauseNo = "ui.pause.no";
        public const string FlashlightOn = "ui.pocket.flashlight_on";
        public const string FlashlightOff = "ui.pocket.flashlight_off";

        // Loot containers.
        public const string LootSearching = "ui.loot.searching";
        public const string LootTakeAll = "ui.loot.take_all";
        public const string LootClose = "ui.loot.close";
        public const string LootBorrowTitle = "ui.loot.borrow_title";
        public const string LootEmpty = "ui.loot.empty";

        // Robber and street finale.
        public const string RobbedItem = "ui.robbed.item";
        public const string StreetDropOpen = "ui.street.drop_open";
        public const string StreetSafe = "ui.street.safe";
        public const string StreetCollapseBehind = "ui.street.collapse_behind";

        /// <summary>A neighbour gave an item: {0} = giver, {1} = item ("Sevim Teyze: Ekmek verdi").</summary>
        public const string GiftReceived = "ui.gift.received";

        /// <summary>A gift that did not fit in the bag was left at the giver's feet: {0} = giver, {1} = item.</summary>
        public const string GiftDropped = "ui.gift.dropped";

        /// <summary>Cost chip of a heavy choice (locked at 0 stamina).</summary>
        public const string ChoiceHeavy = "ui.choice.heavy";

        /// <summary>Mehmet's name (his thought bubble / name tab).</summary>
        public const string PlayerName = "npc.mehmet.name";

        /// <summary>
        /// Messages that are narration (placement instructions, the street hints): they go to the top narration card,
        /// not the toast in the middle of the scene.
        /// </summary>
        public static bool IsNarration(string key)
        {
            return key == PlacementHint || key == PlacementPlayerLast || key == StreetDropOpen || key == StreetSafe || key == StreetCollapseBehind;
        }

        /// <summary>Intro briefing (start of every new run).</summary>
        public const string IntroTitle = "ui.intro.title";
        public const string IntroBody = "ui.intro.body";
        public const string IntroControls = "ui.intro.controls";
        public const string IntroSource = "ui.intro.source";
        public const string IntroStart = "ui.intro.start";

        /// <summary>Floor info card: "DEPREM BİLGİSİ  {0}/{1}" and "Oyunda: {0}".</summary>
        public const string InfoCounter = "ui.info.counter";
        public const string InfoGamePrefix = "ui.info.game";

        /// <summary>Galeri tabs: Deprem Notları / Bilgi Kartları, and the info-card counter.</summary>
        public const string MenuGalleryTabTips = "ui.menu.gallery_tab_tips";
        public const string MenuGalleryTabInfo = "ui.menu.gallery_tab_info";
        public const string MenuGalleryInfoCount = "ui.menu.gallery_info_count";

        /// <summary>The robber is gone (no one was hurt).</summary>
        public const string RobberAfter = "ui.robber.after";

        /// <summary>Destination label of an info card: stage 2 = 3. KAT, 3 = 2. KAT, 4 = SOKAK (stage 1 = 4. KAT).</summary>
        public static string FloorLabel(int stage)
        {
            switch (stage)
            {
                case 2: return Floor3;
                case 3: return Floor2;
                case 4: return FloorStreet;
                default: return Floor4;
            }
        }

        public const string Floor4 = "ui.floor.4";
        public const string Floor3 = "ui.floor.3";
        public const string Floor2 = "ui.floor.2";
        public const string FloorStreet = "ui.floor.street";

        /// <summary>Info cards 1..8: title, body, "Oyunda" line (literal constants so Text > Validate sees them).</summary>
        public const string Info1Title = "info.1.title";
        public const string Info1Body = "info.1.body";
        public const string Info1Game = "info.1.game";
        public const string Info2Title = "info.2.title";
        public const string Info2Body = "info.2.body";
        public const string Info2Game = "info.2.game";
        public const string Info3Title = "info.3.title";
        public const string Info3Body = "info.3.body";
        public const string Info3Game = "info.3.game";
        public const string Info4Title = "info.4.title";
        public const string Info4Body = "info.4.body";
        public const string Info4Game = "info.4.game";
        public const string Info5Title = "info.5.title";
        public const string Info5Body = "info.5.body";
        public const string Info5Game = "info.5.game";
        public const string Info6Title = "info.6.title";
        public const string Info6Body = "info.6.body";
        public const string Info6Game = "info.6.game";
        public const string Info7Title = "info.7.title";
        public const string Info7Body = "info.7.body";
        public const string Info7Game = "info.7.game";
        public const string Info8Title = "info.8.title";
        public const string Info8Body = "info.8.body";
        public const string Info8Game = "info.8.game";

        /// <summary>Info card N (1..8) title / body / "Oyunda" line.</summary>
        public static string InfoTitle(int card) => "info." + card + ".title";
        public static string InfoBody(int card) => "info." + card + ".body";
        public static string InfoGame(int card) => "info." + card + ".game";

        /// <summary>Room name shown on door hover: room.&lt;roomId&gt;.</summary>
        public static string RoomName(string roomId) => "room." + roomId;
    }
}
