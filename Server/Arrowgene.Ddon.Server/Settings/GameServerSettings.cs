using Arrowgene.Ddon.Server.Scripting.utils;
using Arrowgene.Ddon.Shared.Model;
using Arrowgene.Ddon.Shared.Model.Quest;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Arrowgene.Ddon.Server.Settings
{
    public class GameServerSettings : IGameSettings
    {
        public GameServerSettings(ScriptableSettings settingsData) : base(settingsData, typeof(GameServerSettings).Name)
        {
        }

        /// <summary>
        /// Additional factor to change how long crafting a recipe will take to finish.
        /// </summary>
        [DefaultValue(_AdditionalProductionSpeedFactor)]
        public double AdditionalProductionSpeedFactor
        {
            set
            {
                SetSetting("AdditionalProductionSpeedFactor", value);
            }
            get
            {
                return TryGetSetting("AdditionalProductionSpeedFactor", _AdditionalProductionSpeedFactor);
            }
        }
        private const double _AdditionalProductionSpeedFactor = 1.0;

        /// <summary>
        /// Additional factor to change how much a recipe will cost.
        /// </summary>
        [DefaultValue(_AdditionalCostPerformanceFactor)]
        public double AdditionalCostPerformanceFactor
        {
            set
            {
                SetSetting("AdditionalCostPerformanceFactor", value);
            }
            get
            {
                return TryGetSetting("AdditionalCostPerformanceFactor", _AdditionalCostPerformanceFactor);
            }
        }
        private const double _AdditionalCostPerformanceFactor = 1.0;

        /// <summary>
        /// The amount of seconds that the partner pawn must be a member of the
        /// party, adventuring in a non-safe area to receive adventure credit
        /// for the day.
        /// </summary>
        [DefaultValue(_PartnerPawnAdventureDurationInSeconds)]
        public uint PartnerPawnAdventureDurationInSeconds
        {
            set
            {
                SetSetting("PartnerPawnAdventureDurationInSeconds", value);
            }
            get
            {
                return TryGetSetting("PartnerPawnAdventureDurationInSeconds", _PartnerPawnAdventureDurationInSeconds);
            }
        }
        private const uint _PartnerPawnAdventureDurationInSeconds = 1800;

        /// <summary>
        /// Determines the maximum amount of consumable items that can be crafted in one go with a pawn.
        /// The default is a value of 10 which is equivalent to the original game's behavior.
        /// </summary>
        [DefaultValue(_CraftConsumableProductionTimesMax)]
        public byte CraftConsumableProductionTimesMax
        {
            set
            {
                SetSetting("CraftConsumableProductionTimesMax", value);
            }
            get
            {
                return TryGetSetting("CraftConsumableProductionTimesMax", _CraftConsumableProductionTimesMax);
            }
        }
        private const byte _CraftConsumableProductionTimesMax = 10;

        /// <summary>
        /// Determines the maximum amount of items you can recycle/disassemble at Craig before a reset is required.
        /// </summary>
        [DefaultValue(_CraftItemRecycleMax)]
        public byte CraftItemRecycleMax
        {
            set
            {
                SetSetting("CraftItemRecycleMax", value);
            }
            get
            {
                return TryGetSetting("CraftItemRecycleMax", _CraftItemRecycleMax);
            }
        }
        private const byte _CraftItemRecycleMax = 10;

        /// <summary>
        /// The amount of Golden Gemstones (GG) required to reset the recycle/disassemble count.
        /// </summary>
        [DefaultValue(_CraftItemRecycleResetCost)]
        public byte CraftItemRecycleResetGGCost
        {
            set
            {
                SetSetting("CraftItemRecycleResetCost", value);
            }
            get
            {
                return TryGetSetting("CraftItemRecycleResetCost", _CraftItemRecycleResetCost);
            }
        }
        private const byte _CraftItemRecycleResetCost = 1;

        /// <summary>
        /// Modifier used to skew the randomness during equipment unlimit.
        ///
        /// Example bias values (note that fractional amounts are also valid):
        /// Bias of -1.0 Inverts the bias, favoring higher indices
        /// Bias of 0.0 No bias, equal probabilty for all.
        /// Bias of 1.0 Balanced bias towers lower indices
        /// Bias of 2.0 strongly prefers lower indices
        /// </summary>
        [DefaultValue(_EquipmentLimitBreakBias)]
        public double EquipmentLimitBreakBias
        {
            set
            {
                SetSetting("EquipmentLimitBreakBias", value);
            }
            get
            {
                return TryGetSetting("EquipmentLimitBreakBias", _EquipmentLimitBreakBias);
            }
        }
        private const double _EquipmentLimitBreakBias = 1.5;


        /// <summary>
        /// The number of real world minutes that make up an in-game day.
        /// </summary>
        [DefaultValue(_GameClockTimescale)]
        public uint GameClockTimescale
        {
            set
            {
                SetSetting("GameClockTimescale", value);
            }
            get
            {
                return TryGetSetting("GameClockTimescale", _GameClockTimescale);
            }
        }
        private const uint _GameClockTimescale = 90;

        /// <summary>
        /// Use a poisson process to randomly generate a weather cycle containing this many events, using the statistics in WeatherStatistics.
        /// </summary>
        [DefaultValue(_WeatherSequenceLength)]
        public uint WeatherSequenceLength
        {
            set
            {
                SetSetting("WeatherSequenceLength", value);
            }
            get
            {
                return TryGetSetting("WeatherSequenceLength", _WeatherSequenceLength);
            }
        }
        private const uint _WeatherSequenceLength = 20;

        /// <summary>
        /// Statistics that drive semirandom weather generation. List is expected to be in (Fair, Cloudy, Rainy) order.
        /// meanLength: Average length of the weather, in seconds, when it gets rolled.
        /// weight: Relative weight of rolling that weather. Set to 0 to disable.
        /// </summary>
        [DefaultValue("new List<(uint MeanLength, uint Weight)>\n" +
            "{\n" +
            "    (60 * 30, 1), // Fair\n" +
            "    (60 * 30, 1), // Cloudy\n" +
            "    (60 * 30, 1), // Windy\n" +
            "}"
        )]
        public List<(uint MeanLength, uint Weight)> WeatherStatistics
        {
            set
            {
                SetSetting("WeatherStatistics", value);
            }
            get
            {
                return TryGetSetting("WeatherStatistics", new List<(uint MeanLength, uint Weight)>
                {
                    (60 * 30, 1), // Fair
                    (60 * 30, 1), // Cloudy
                    (60 * 30, 1), // Windy
                });
            }
        }

        /// <summary>
        /// Configures the default time in seconds a lantern is active after igniting it.
        /// </summary>
        [DefaultValue(_LanternBurnTimeInSeconds)]
        public uint LanternBurnTimeInSeconds
        {
            set
            {
                SetSetting("LanternBurnTimeInSeconds", value);
            }
            get
            {
                return TryGetSetting("LanternBurnTimeInSeconds", _LanternBurnTimeInSeconds);
            }
        }
        private const uint _LanternBurnTimeInSeconds = 1500;

        /// <summary>
        /// When using the adventure guide, configures the listing level range +/- the value
        /// of the level of the current job when displaying world quests.
        /// </summary>
        [DefaultValue(_AdventureGuideLevelRangeFilter)]
        public uint AdventureGuideLevelRangeFilter
        {
            set
            {
                SetSetting("AdventureGuideLevelRangeFilter", value);
            }
            get
            {
                return TryGetSetting("AdventureGuideLevelRangeFilter", _AdventureGuideLevelRangeFilter);
            }
        }
        private const uint _AdventureGuideLevelRangeFilter = 10;

        /// <summary>
        /// Configures the maximum amount of quests to display in the adventure guide
        /// at one time.
        /// </summary>
        [DefaultValue(_AdventureGuideMaxQuestList)]
        public uint AdventureGuideMaxQuestList
        {
            set
            {
                SetSetting("AdventureGuideMaxQuestList", value);
            }
            get
            {
                return TryGetSetting("AdventureGuideMaxQuestList", _AdventureGuideMaxQuestList);
            }
        }
        private const uint _AdventureGuideMaxQuestList = 50;

        /// <summary>
        /// Uses the automatic exp calculation system for all enemies instead of just using the
        /// ones marked in quest files.
        /// </summary>
        [DefaultValue(_EnableAutomaticExpCalculationForAll)]
        public bool EnableAutomaticExpCalculationForAll
        {
            set
            {
                SetSetting("EnableAutomaticExpCalculationForAll", value);
            }
            get
            {
                return TryGetSetting("EnableAutomaticExpCalculationForAll", _EnableAutomaticExpCalculationForAll);
            }
        }
        private const bool _EnableAutomaticExpCalculationForAll = false;

        /// <summary>
        /// When set to true, if the party leader has the content unlock of "OrbEnemy", random enemies
        /// will appear as "Blood Orb [name]" each time the instance is reset. The amount of BO will
        /// be calculated based on the enemy level.
        /// </summary>
        [DefaultValue(_EnableRandomizedBoEnemies)]
        public bool EnableRandomizedBoEnemies
        {
            set
            {
                SetSetting("EnableRandomizedBoEnemies", value);
            }
            get
            {
                return TryGetSetting("EnableRandomizedBoEnemies", _EnableRandomizedBoEnemies);
            }
        }
        private const bool _EnableRandomizedBoEnemies = false;

        /// <summary>
        /// If EnableRandomizedBoEnemies is set to true, this setting configures the chance % that
        /// an enemy will be upgraded to being a BoEnemy instead of a normal enemy.
        /// </summary>
        [DefaultValue(_RandomizedBoEnemyChance)]
        public double RandomizedBoEnemyChance
        {
            set
            {
                SetSetting("RandomizedBoEnemyChance", value);
            }
            get
            {
                return TryGetSetting("RandomizedBoEnemyChance", _RandomizedBoEnemyChance);
            }
        }
        private const double _RandomizedBoEnemyChance = 0.05;


        /// <summary>
        /// Maximum amount of play points the client will display in the UI. 
        /// Play points past this point will also trigger a chat log message saying you've reached the cap.
        /// </summary>
        [DefaultValue(_PlayPointMax)]
        public uint PlayPointMax
        {
            set
            {
                SetSetting("PlayPointMax", value);
            }
            get
            {
                return TryGetSetting("PlayPointMax", _PlayPointMax);
            }
        }
        private const uint _PlayPointMax = 2000;

        /// <summary>
        /// Maximum level for each job. 
        /// Shared with the login server.
        /// Level caps based on season release
        /// Alpha:        10
        /// CBT           15
        /// Season 1.0:   40
        /// Season 1.1:   45
        /// Season 1.2:   55
        /// Season 1.3:   60
        /// Season 2.0:   65
        /// Season 2.1:   70
        /// Season 2.2:   75
        /// Season 2.3:   80
        /// Season 3.0:   85
        /// Season 3.1:   90
        /// Season 3.2:   95
        /// Season 3.3:  100
        /// Season 3.41: 105
        /// Season 3.42: 110
        /// Season 3.43: 120
        /// </summary>
        [DefaultValue(_JobLevelMax)]
        public uint JobLevelMax
        {
            set
            {
                SetSetting("JobLevelMax", value);
            }
            get
            {
                return TryGetSetting("JobLevelMax", _JobLevelMax);
            }
        }
        private const uint _JobLevelMax = 120;

        /// <summary>
        /// OPTIONAL per-StageId override for the level-sync system.
        ///
        /// By default, recommended levels are auto-detected from the client's stage data for all dungeon/
        /// recommended-level zones (towns and open-world fields are intentionally never synced, so power-leveling
        /// out in the world is unaffected). You normally do NOT need to put anything here.
        ///
        /// Entries in this map override the auto-detected value for a specific StageId:
        ///   * Map a StageId to a level to force that zone's recommended level.
        ///   * Map a StageId to 0 to DISABLE sync for that zone entirely.
        ///
        /// How sync works: when a player enters a synced zone and their job level is HIGHER than the recommended
        /// level, the server computes per-character attack scale factors (using job base stats and, when
        /// <see cref="LevelSyncGearAwareScaling"/> is enabled, equipped gear tier) and writes them to a signal
        /// file. A small external client-side applier reads that signal and temporarily lowers the player's (and own pawns') live
        /// combat attack values so an over-leveled character fights fairly. The server no longer fakes level, EXP, or
        /// stats: the HUD shows the REAL level and a real EXP bar with no level-up popups, and gear/equip
        /// requirements/progression are unaffected. Sync is down-only and reverts on leaving the zone.
        /// Example: new Dictionary&lt;uint, uint&gt; { { 90, 13 }, { 301, 0 } } // force stage 90 to Lv13, disable stage 301
        /// </summary>
        [DefaultValue("new Dictionary<uint, uint>()")]
        public Dictionary<uint, uint> StageRecommendedLevels
        {
            set
            {
                SetSetting("StageRecommendedLevels", value);
            }
            get
            {
                return TryGetSetting("StageRecommendedLevels", new Dictionary<uint, uint>());
            }
        }

        /// <summary>
        /// Shapes how harsh level sync is. The raw attack scale factor is the ratio of level-scaled base stats at the
        /// recommended level vs. the player's real level (e.g. ~0.41 for Lv20 in a Lv3 zone). That ratio is then raised
        /// to this exponent: 1.0 = pure ratio, &gt;1.0 = harsher (lower damage), &lt;1.0 = gentler. Example: 1.5 turns
        /// a 0.41 ratio into ~0.27.
        /// </summary>
        [DefaultValue(_LevelSyncAttackFactorExponent)]
        public double LevelSyncAttackFactorExponent
        {
            set
            {
                SetSetting("LevelSyncAttackFactorExponent", value);
            }
            get
            {
                return TryGetSetting("LevelSyncAttackFactorExponent", _LevelSyncAttackFactorExponent);
            }
        }
        private const double _LevelSyncAttackFactorExponent = 1.5;

        /// <summary>
        /// Lower bound for the level-sync attack scale factor, so attacks are never reduced below this fraction of
        /// their real value (prevents zero/near-zero damage in very low-level zones).
        /// </summary>
        [DefaultValue(_LevelSyncMinAttackFactor)]
        public double LevelSyncMinAttackFactor
        {
            set
            {
                SetSetting("LevelSyncMinAttackFactor", value);
            }
            get
            {
                return TryGetSetting("LevelSyncMinAttackFactor", _LevelSyncMinAttackFactor);
            }
        }
        private const double _LevelSyncMinAttackFactor = 0.05;

        /// <summary>
        /// When true, while a player is in a recommended-level (synced) stage that is below their real level, the
        /// server repaints the HUD job-level number (and anchors the EXP bar) to the stage's recommended level for
        /// immersion, then restores the real level when they leave. This is purely a DISPLAY change: the real level,
        /// EXP total, gear/equip requirements and database writes are always the true values, and combat down-scaling
        /// is still performed by the client-side applier. Set false to leave the real level number on the HUD.
        /// </summary>
        [DefaultValue(_LevelSyncDisplayRecommendedLevel)]
        public bool LevelSyncDisplayRecommendedLevel
        {
            set
            {
                SetSetting("LevelSyncDisplayRecommendedLevel", value);
            }
            get
            {
                return TryGetSetting("LevelSyncDisplayRecommendedLevel", _LevelSyncDisplayRecommendedLevel);
            }
        }
        private const bool _LevelSyncDisplayRecommendedLevel = true;

        /// <summary>
        /// When true, parties entering a level-sync dungeon receive enemies capped to the zone's
        /// recommended level. World layout spawns keep automatic client scaling (IsManualSet=false)
        /// so mobs retain AI and collision; the client-side applier handles player combat down-scaling.
        /// </summary>
        [DefaultValue(_LevelSyncEnemyLevels)]
        public bool LevelSyncEnemyLevels
        {
            set
            {
                SetSetting("LevelSyncEnemyLevels", value);
            }
            get
            {
                return TryGetSetting("LevelSyncEnemyLevels", _LevelSyncEnemyLevels);
            }
        }
        private const bool _LevelSyncEnemyLevels = true;

        /// <summary>
        /// When true, boss-gauge enemies in level-sync dungeons are capped to the zone's recommended level
        /// (same as trash). When false, bosses keep their authored level so they stay harder than the zone cap.
        /// </summary>
        [DefaultValue(_LevelSyncBossLevels)]
        public bool LevelSyncBossLevels
        {
            set
            {
                SetSetting("LevelSyncBossLevels", value);
            }
            get
            {
                return TryGetSetting("LevelSyncBossLevels", _LevelSyncBossLevels);
            }
        }
        private const bool _LevelSyncBossLevels = true;

        /// <summary>
        /// When true, attack scale factors also account for equipped gear tier (max of item level and IR).
        /// Over-leveled players wielding endgame weapons in starter dungeons are scaled down much further
        /// than the job base-stat ratio alone would allow. Tier-limited scaling is linear (no exponent).
        /// </summary>
        [DefaultValue(_LevelSyncGearAwareScaling)]
        public bool LevelSyncGearAwareScaling
        {
            set
            {
                SetSetting("LevelSyncGearAwareScaling", value);
            }
            get
            {
                return TryGetSetting("LevelSyncGearAwareScaling", _LevelSyncGearAwareScaling);
            }
        }
        private const bool _LevelSyncGearAwareScaling = true;

        /// <summary>
        /// When true, other players in your party (and anyone who inspects your profile while you are
        /// online in a combat-synced zone) see your recommended level and scaled offensive stats instead
        /// of your real level and base job stats. Your local HUD repaint is controlled separately by
        /// <see cref="LevelSyncDisplayRecommendedLevel"/>.
        /// </summary>
        [DefaultValue(_LevelSyncBroadcastDisplayToOthers)]
        public bool LevelSyncBroadcastDisplayToOthers
        {
            set
            {
                SetSetting("LevelSyncBroadcastDisplayToOthers", value);
            }
            get
            {
                return TryGetSetting("LevelSyncBroadcastDisplayToOthers", _LevelSyncBroadcastDisplayToOthers);
            }
        }
        private const bool _LevelSyncBroadcastDisplayToOthers = true;

        /// <summary>
        /// When true, EXP penalty scripts and automatic enemy EXP use the zone's recommended level instead of
        /// the real job level for party members who are in a level-sync stage above that cap. This lets mixed
        /// real-level parties run low sync dungeons together without the party spread penalty treating a Lv 60
        /// character as 60 levels above a Lv 10 ally. Members outside the kill stage still count at their real level.
        /// </summary>
        [DefaultValue(_LevelSyncUseDisplayLevelForExp)]
        public bool LevelSyncUseDisplayLevelForExp
        {
            set
            {
                SetSetting("LevelSyncUseDisplayLevelForExp", value);
            }
            get
            {
                return TryGetSetting("LevelSyncUseDisplayLevelForExp", _LevelSyncUseDisplayLevelForExp);
            }
        }
        private const bool _LevelSyncUseDisplayLevelForExp = true;

        /// <summary>
        /// When true, characters below <see cref="JobLevelMax"/> do not lose recoverable HP (gray / WhiteHP)
        /// from combat damage. Green HP still drops normally and death still clears both bars. The server
        /// clamps periodic RPC updates and echoes the corrected value back to vanilla clients. At max job
        /// level the original loss-gauge behavior applies.
        /// </summary>
        [DefaultValue(_DisableRecoverableHpLossBelowMaxLevel)]
        public bool DisableRecoverableHpLossBelowMaxLevel
        {
            set
            {
                SetSetting("DisableRecoverableHpLossBelowMaxLevel", value);
            }
            get
            {
                return TryGetSetting("DisableRecoverableHpLossBelowMaxLevel", _DisableRecoverableHpLossBelowMaxLevel);
            }
        }
        private const bool _DisableRecoverableHpLossBelowMaxLevel = true;

        /// <summary>
        /// The maximum job points which a job can own at a given time.
        /// job points past this point will trigger a UI message saying
        /// you can't earn anymore.
        /// </summary>
        [DefaultValue(_JobPointMax)]
        public uint JobPointMax
        {
            set
            {
                SetSetting("JobPointMax", value);
            }
            get
            {
                return TryGetSetting("JobPointMax", _JobPointMax);
            }
        }
        private const uint _JobPointMax = 500000;

        /// <summary>
        /// When enabled, every core (normal) skill, custom skill, EX skill and augment (including
        /// secret / cross-job augments) for all jobs is automatically learned at rank/level 1 from
        /// the start, so they are immediately usable without visiting a trainer or spending JP. Ranks
        /// already raised with JP are preserved (never downgraded), and players can still spend JP to
        /// advance any skill/augment beyond rank 1. The augment cost budget (augment "slots") is maxed
        /// out and the level/training requirements for learning higher ranks are bypassed.
        /// Applies to both characters and their pawns, and is granted idempotently on login/pawn
        /// creation so existing characters are upgraded automatically.
        /// </summary>
        [DefaultValue(_UnlockAllSkillsAtLevelOne)]
        public bool UnlockAllSkillsAtLevelOne
        {
            set
            {
                SetSetting("UnlockAllSkillsAtLevelOne", value);
            }
            get
            {
                return TryGetSetting("UnlockAllSkillsAtLevelOne", _UnlockAllSkillsAtLevelOne);
            }
        }
        private const bool _UnlockAllSkillsAtLevelOne = true;

        /// <summary>
        /// When enabled, every vocation (job) is unlocked from the start, regardless of quest
        /// progress. This releases the "Change Vocations" and "Change to High Scepter" content,
        /// along with each vocation's Job Training, so any character can freely switch to and
        /// train any vocation at job level 1.
        /// Applies to both characters and their pawns.
        /// </summary>
        [DefaultValue(_UnlockAllVocationsAtLevelOne)]
        public bool UnlockAllVocationsAtLevelOne
        {
            set
            {
                SetSetting("UnlockAllVocationsAtLevelOne", value);
            }
            get
            {
                return TryGetSetting("UnlockAllVocationsAtLevelOne", _UnlockAllVocationsAtLevelOne);
            }
        }
        private const bool _UnlockAllVocationsAtLevelOne = true;

        /// <summary>
        /// Maximum number of members in a single clan. 
        /// Shared with the login server.
        /// </summary>
        [DefaultValue(_ClanMemberMax)]
        public uint ClanMemberMax
        {
            set
            {
                SetSetting("ClanMemberMax", value);
            }
            get
            {
                return TryGetSetting("ClanMemberMax", _ClanMemberMax);
            }
        }
        private const uint _ClanMemberMax = 100;

        /// <summary>
        /// Maximum number of characters per account. 
        /// Shared with the login server.
        /// </summary>
        [DefaultValue(_CharacterNumMax)]
        public byte CharacterNumMax
        {
            set
            {
                SetSetting("CharacterNumMax", value);
            }
            get
            {
                return TryGetSetting("CharacterNumMax", _CharacterNumMax);
            }
        }
        private const byte _CharacterNumMax = 4;

        /// <summary>
        /// Toggles the visual equip set for all characters. 
        /// Shared with the login server.
        /// </summary>
        [DefaultValue(_EnableVisualEquip)]
        public bool EnableVisualEquip
        {
            set
            {
                SetSetting("EnableVisualEquip", value);
            }
            get
            {
                return TryGetSetting("EnableVisualEquip", _EnableVisualEquip);
            }
        }
        private const bool _EnableVisualEquip = true;

        /// <summary>
        /// Maximum entries in the friends list. 
        /// Shared with the login server.
        /// </summary>
        [DefaultValue(_FriendListMax)]
        public uint FriendListMax
        {
            set
            {
                SetSetting("FriendListMax", value);
            }
            get
            {
                return TryGetSetting("FriendListMax", _FriendListMax);
            }
        }
        private const uint _FriendListMax = 200;

        /// <summary>
        /// Limits for each wallet type.
        /// </summary>
        [DefaultValue("new Dictionary<WalletType, uint>()\n" +
            "{\n" +
            "    {WalletType.Gold, 999999999},\n" +
            "    {WalletType.RiftPoints, 999999999},\n" +
            "    {WalletType.BloodOrbs, 500000},\n" +
            "    {WalletType.SilverTickets, 999999999},\n" +
            "    {WalletType.GoldenGemstones, 99999},\n" +
            "    {WalletType.RentalPoints, 99999},\n" +
            "    {WalletType.ResetJobPoints, 99},\n" +
            "    {WalletType.ResetCraftSkills, 99},\n" +
            "    {WalletType.HighOrbs, 5000},\n" +
            "    {WalletType.DominionPoints, 999999999},\n" +
            "    {WalletType.AdventurePassPoints, 80},\n" +
            "    {WalletType.CustomMadeServiceTickets, 999999999},\n" +
            "    {WalletType.BitterblackMazeResetTicket, 3},\n" +
            "    {WalletType.GoldenDragonMark, 30},\n" +
            "    {WalletType.SilverDragonMark, 150},\n" +
            "    {WalletType.RedDragonMark, 99999},\n" +
            "}"
        )]
        public Dictionary<WalletType, uint> WalletLimits
        {
            set
            {
                SetSetting("WalletLimits", value);
            }
            get
            {
                return TryGetSetting("WalletLimits", new Dictionary<WalletType, uint>()
                {
                    {WalletType.Gold, 999999999},
                    {WalletType.RiftPoints, 999999999},
                    {WalletType.BloodOrbs, 500000},
                    {WalletType.SilverTickets, 999999999},
                    {WalletType.GoldenGemstones, 99999},
                    {WalletType.RentalPoints, 99999},
                    {WalletType.ResetJobPoints, 99},
                    {WalletType.ResetCraftSkills, 99},
                    {WalletType.HighOrbs, 5000},
                    {WalletType.DominionPoints, 999999999},
                    {WalletType.AdventurePassPoints, 80},
                    {WalletType.CustomMadeServiceTickets, 999999999},
                    {WalletType.BitterblackMazeResetTicket, 3},
                    {WalletType.GoldenDragonMark, 30},
                    {WalletType.SilverDragonMark, 150},
                    {WalletType.RedDragonMark, 99999},
                });
            }
        }

        /// <summary>
        /// Number of bazaar entries that are given to new characters.
        /// </summary>
        [DefaultValue(_DefaultMaxBazaarExhibits)]
        public uint DefaultMaxBazaarExhibits
        {
            set
            {
                SetSetting("DefaultMaxBazaarExhibits", value);
            }
            get
            {
                return TryGetSetting("DefaultMaxBazaarExhibits", _DefaultMaxBazaarExhibits);
            }
        }
        private const uint _DefaultMaxBazaarExhibits = 5;

        /// <summary>
        /// Number of favorite warps that are given to new characters.
        /// </summary>
        [DefaultValue(_DefaultWarpFavorites)]
        public uint DefaultWarpFavorites
        {
            set
            {
                SetSetting("DefaultWarpFavorites", value);
            }
            get
            {
                return TryGetSetting("DefaultWarpFavorites", _DefaultWarpFavorites);
            }
        }
        private const uint _DefaultWarpFavorites = 5;

        /// <summary>
        /// When enabled, new characters start with warp points unlocked for level-sync destinations
        /// up to <see cref="StarterLevelSyncWarpMaxRecommendedLevel"/> (see
        /// <see cref="StarterLevelSyncWarpPointTable"/>). Higher-level teleports remain locked for exploration.
        /// </summary>
        [DefaultValue(_UnlockStarterLevelSyncWarps)]
        public bool UnlockStarterLevelSyncWarps
        {
            set
            {
                SetSetting("UnlockStarterLevelSyncWarps", value);
            }
            get
            {
                return TryGetSetting("UnlockStarterLevelSyncWarps", _UnlockStarterLevelSyncWarps);
            }
        }
        private const bool _UnlockStarterLevelSyncWarps = true;

        /// <summary>
        /// Maximum recommended level for starter warp unlocks when
        /// <see cref="UnlockStarterLevelSyncWarps"/> is enabled. Regenerate
        /// <see cref="StarterLevelSyncWarpPointTable"/> if this value changes.
        /// </summary>
        [DefaultValue(_StarterLevelSyncWarpMaxRecommendedLevel)]
        public uint StarterLevelSyncWarpMaxRecommendedLevel
        {
            set
            {
                SetSetting("StarterLevelSyncWarpMaxRecommendedLevel", value);
            }
            get
            {
                return TryGetSetting("StarterLevelSyncWarpMaxRecommendedLevel", _StarterLevelSyncWarpMaxRecommendedLevel);
            }
        }
        private const uint _StarterLevelSyncWarpMaxRecommendedLevel = 20;

        /// <summary>
        /// Controls the party size for regular adventuring content. 
        /// Used to control main pawns auto-joining parties alongside their owners.
        /// </summary>
        [DefaultValue(_NormalPartySize)]
        public uint NormalPartySize
        {
            set
            {
                SetSetting("NormalPartySize", value);
            }
            get
            {
                return TryGetSetting("NormalPartySize", _NormalPartySize);
            }
        }
        private const uint _NormalPartySize = 4;

        /// <summary>
        /// Global modifier for enemy exp calculations to scale up or down.
        /// </summary>
        [DefaultValue(_EnemyExpModifier)]
        public double EnemyExpModifier
        {
            set
            {
                SetSetting("EnemyExpModifier", value);
            }
            get
            {
                return TryGetSetting("EnemyExpModifier", _EnemyExpModifier);
            }
        }
        private const double _EnemyExpModifier = 1.0;

        /// <summary>
        /// Global modifier for BBM enemy exp calculations to scale up or down.
        /// </summary>
        [DefaultValue(_BBMEnemyExpModifier)]
        public double BBMEnemyExpModifier
        {
            set
            {
                SetSetting("BBMEnemyExpModifier", value);
            }
            get
            {
                return TryGetSetting("BBMEnemyExpModifier", _BBMEnemyExpModifier);
            }
        }
        private const double _BBMEnemyExpModifier = 1.0;

        /// <summary>
        /// Global modifier for quest exp calculations to scale up or down.
        /// </summary>
        [DefaultValue(_QuestExpModifier)]
        public double QuestExpModifier
        {
            set
            {
                SetSetting("QuestExpModifier", value);
            }
            get
            {
                return TryGetSetting("QuestExpModifier", _QuestExpModifier);
            }
        }
        private const double _QuestExpModifier = 1.0;

        /// <summary>
        /// Global modifier for playpoint calculations to scale up or down.
        /// </summary>
        [DefaultValue(_PpModifier)]
        public double PpModifier
        {
            set
            {
                SetSetting("PpModifier", value);
            }
            get
            {
                return TryGetSetting("PpModifier", _PpModifier);
            }
        }
        private const double _PpModifier = 1.0;

        /// <summary>
        /// Global modifier for Gold calculations to scale up or down.
        /// </summary>
        [DefaultValue(_GoldModifier)]
        public double GoldModifier
        {
            set
            {
                SetSetting("GoldModifier", value);
            }
            get
            {
                return TryGetSetting("GoldModifier", _GoldModifier);
            }
        }
        private const double _GoldModifier = 1.0;

        /// <summary>
        /// Global modifier for Rift calculations to scale up or down.
        /// </summary>
        [DefaultValue(_RiftModifier)]
        public double RiftModifier
        {
            set
            {
                SetSetting("RiftModifier", value);
            }
            get
            {
                return TryGetSetting("RiftModifier", _RiftModifier);
            }
        }
        private const double _RiftModifier = 1.0;

        /// <summary>
        /// Global modifier for BO calculations to scale up or down.
        /// </summary>
        [DefaultValue(_BoModifier)]
        public double BoModifier
        {
            set
            {
                SetSetting("BoModifier", value);
            }
            get
            {
                return TryGetSetting("BoModifier", _BoModifier);
            }
        }
        private const double _BoModifier = 1.0;

        /// <summary>
        /// Global modifier for HO calculations to scale up or down.
        /// </summary>
        [DefaultValue(_HoModifier)]
        public double HoModifier
        {
            set
            {
                SetSetting("HoModifier", value);
            }
            get
            {
                return TryGetSetting("HoModifier", _HoModifier);
            }
        }
        private const double _HoModifier = 1.0;

        /// <summary>
        /// Global modifier for JP calculations to scale up or down.
        /// </summary>
        [DefaultValue(_JpModifier)]
        public double JpModifier
        {
            set
            {
                SetSetting("JpModifier", value);
            }
            get
            {
                return TryGetSetting("JpModifier", _JpModifier);
            }
        }
        private const double _JpModifier = 1.0;

        /// <summary>
        /// Global modifier for AP calculations to scale up or down.
        /// </summary>
        [DefaultValue(_ApModifier)]
        public double ApModifier
        {
            set
            {
                SetSetting("ApModifier", value);
            }
            get
            {
                return TryGetSetting("ApModifier", _ApModifier);
            }
        }
        private const double _ApModifier = 1.0;

        /// <summary>
        /// Configures the maximum amount of reward box slots.
        /// </summary>
        [DefaultValue(_RewardBoxMax)]
        public byte RewardBoxMax
        {
            set
            {
                SetSetting("RewardBoxMax", value);
            }
            get
            {
                return TryGetSetting("RewardBoxMax", _RewardBoxMax);
            }
        }
        private const byte _RewardBoxMax = 100;

        /// <summary>
        /// Configures the maximum amount of quests that can be ordered at one time.
        /// </summary>
        [DefaultValue(_QuestOrderMax)]
        public byte QuestOrderMax
        {
            set
            {
                SetSetting("QuestOrderMax", value);
            }
            get
            {
                return TryGetSetting("QuestOrderMax", _QuestOrderMax);
            }
        }
        private const byte _QuestOrderMax = 20;

        /// <summary>
        /// When enabled, players resume at the exact stage they logged out in instead of being sent
        /// back to the last safe area (inn/town) they visited. The location is saved on logout and
        /// restored on the next login. Only applies to the normal game mode; instanced content such as
        /// Bitterblack Maze always resumes at its own entrance. Set to false to restore the original
        /// behavior of always returning to the last safe area.
        /// </summary>
        [DefaultValue(_EnableReturnToLogoutLocation)]
        public bool EnableReturnToLogoutLocation
        {
            set
            {
                SetSetting("EnableReturnToLogoutLocation", value);
            }
            get
            {
                return TryGetSetting("EnableReturnToLogoutLocation", _EnableReturnToLogoutLocation);
            }
        }
        private const bool _EnableReturnToLogoutLocation = true;

        /// <summary>
        /// Configures if epitaph rewards are limited once per weekly reset.
        /// </summary>
        [DefaultValue(_EnableEpitaphWeeklyRewards)]
        public bool EnableEpitaphWeeklyRewards
        {
            set
            {
                SetSetting("EnableEpitaphWeeklyRewards", value);
            }
            get
            {
                return TryGetSetting("EnableEpitaphWeeklyRewards", _EnableEpitaphWeeklyRewards);
            }
        }
        private const bool _EnableEpitaphWeeklyRewards = true;

        /// <summary>
        /// Enables main pawns in party to gain EXP and JP from quests
        /// Original game apparantly did not have pawns share quest reward, so will set to false for default, 
        /// change as needed
        /// </summary>
        [DefaultValue(_EnableMainPartyPawnsQuestRewards)]
        public bool EnableMainPartyPawnsQuestRewards
        {
            set
            {
                SetSetting("EnableMainPartyPawnsQuestRewards", value);
            }
            get
            {
                return TryGetSetting("EnableMainPartyPawnsQuestRewards", _EnableMainPartyPawnsQuestRewards);
            }
        }
        private const bool _EnableMainPartyPawnsQuestRewards = false;

        /// <summary>
        /// Specifies the time in seconds that a bazaar exhibit will last.
        /// By default, the equivalent of 3 days
        /// </summary>
        [DefaultValue("(ulong) TimeSpan.FromDays(3).TotalSeconds")]
        public ulong BazaarExhibitionTimeSeconds
        {
            set
            {
                SetSetting("BazaarExhibitionTimeSeconds", value);
            }
            get
            {
                return TryGetSetting<ulong>("BazaarExhibitionTimeSeconds", (ulong) TimeSpan.FromDays(3).TotalSeconds);
            }
        }

        /// <summary>
        /// Specifies the time in seconds that a slot in the bazaar won't be able to be used again.
        /// By default, the equivalent of 1 day
        /// </summary>
        [DefaultValue("(ulong) TimeSpan.FromDays(1).TotalSeconds")]
        public ulong BazaarCooldownTimeSeconds
        {
            set
            {
                SetSetting("BazaarCooldownTimeSeconds", value);
            }
            get
            {
                return TryGetSetting("BazaarCooldownTimeSeconds", (ulong)TimeSpan.FromDays(1).TotalSeconds);
            }
        }

        /// <summary>
        /// Minimum price in G for a single item on the bazaar.
        /// </summary>
        [DefaultValue(_BazaarExhibitionMinPrice)]
        public uint BazaarExhibitionMinPrice
        {
            set
            {
                SetSetting("BazaarExhibitionMinPrice", value);
            }
            get
            {
                return TryGetSetting("BazaarExhibitionMinPrice", _BazaarExhibitionMinPrice);
            }
        }
        private const uint _BazaarExhibitionMinPrice = 1;

        /// <summary>
        /// Maximum price in G for a single item on the bazaar.
        /// This ends up being interpreted as a signed integer by the client, so its capped at ~2 billion.
        /// </summary>
        [DefaultValue(_BazaarExhibitionMaxPrice)]
        public uint BazaarExhibitionMaxPrice
        {
            set
            {
                SetSetting("BazaarExhibitionMaxPrice", value);
            }
            get
            {
                return TryGetSetting("BazaarExhibitionMaxPrice", _BazaarExhibitionMaxPrice);
            }
        }
        private const uint _BazaarExhibitionMaxPrice = 99999;

        /// <summary>
        /// Number of items that can be included in a single exhibition on the bazaar.
        /// </summary>
        [DefaultValue(_BazaarExhibitionMaxItemNum)]
        public ushort BazaarExhibitionMaxItemNum
        {
            set
            {
                SetSetting("BazaarExhibitionMaxItemNum", value);
            }
            get
            {
                return TryGetSetting("BazaarExhibitionMaxItemNum", _BazaarExhibitionMaxItemNum);
            }
        }
        private const ushort _BazaarExhibitionMaxItemNum = 20;

        /// <summary>
        /// Ties area rank progress to various paths to dungeons.
        /// </summary>
        [DefaultValue(_EnableAreaRankSpotLocks)]
        public bool EnableAreaRankSpotLocks
        {
            set
            {
                SetSetting("EnableAreaRankSpotLocks", value);
            }
            get
            {
                return TryGetSetting("EnableAreaRankSpotLocks", _EnableAreaRankSpotLocks);
            }
        }
        private const bool _EnableAreaRankSpotLocks = false;

        /// <summary>
        /// Confgures the amount of AP to be rewarded when clearing an area or dungeon boss
        /// in the normal game mode.
        /// </summary>
        [DefaultValue(_AreaBossApReward)]
        public uint AreaBossApReward
        {
            set
            {
                SetSetting("AreaBossApReward", value);
            }
            get
            {
                return TryGetSetting("AreaBossApReward", _AreaBossApReward);
            }
        }
        private const uint _AreaBossApReward = 500;

        /// <summary>
        /// When enabled, the main story quest (MSQ) is treated as optional: every piece of
        /// story-gated content (menus, world quests, crafting, area master, vocation changes,
        /// emblems, etc.) is released for all characters regardless of how far they have
        /// progressed through the MSQ. Characters can still play the MSQ normally; this simply
        /// removes the requirement to complete it in order to access the rest of the game.
        /// Pair with GrantMaxAreaRank to also remove the area-rank progression gates.
        /// </summary>
        [DefaultValue(_UnlockAllStoryContent)]
        public bool UnlockAllStoryContent
        {
            set
            {
                SetSetting("UnlockAllStoryContent", value);
            }
            get
            {
                return TryGetSetting("UnlockAllStoryContent", _UnlockAllStoryContent);
            }
        }
        private const bool _UnlockAllStoryContent = false;

        /// <summary>
        /// When enabled, every quest is treated as optional by stripping the quest-completion
        /// prerequisites (e.g. "complete the main story quest", "clear world quest X", "clear
        /// substory Y", "clear personal quest Z", "clear extreme mission W") from the order
        /// conditions reported to the client. This lets players order any quest without first
        /// completing the quests that would normally chain in front of it. Readiness gates that
        /// are not quest prerequisites (minimum level, item rank, party/solo requirements, etc.)
        /// are left intact. Pair with UnlockAllStoryContent and GrantMaxAreaRank to remove the
        /// story- and area-rank-based gates as well.
        /// </summary>
        [DefaultValue(_MakeAllQuestsOptional)]
        public bool MakeAllQuestsOptional
        {
            set
            {
                SetSetting("MakeAllQuestsOptional", value);
            }
            get
            {
                return TryGetSetting("MakeAllQuestsOptional", _MakeAllQuestsOptional);
            }
        }
        private const bool _MakeAllQuestsOptional = false;

        /// <summary>
        /// When enabled, every character is granted the maximum area rank in all areas on login,
        /// unlocking the dungeon paths, area-rank spots and supplies that would normally require
        /// grinding area points and completing area trials. This is the area-rank counterpart to
        /// UnlockAllStoryContent and helps make the main story quest optional.
        /// </summary>
        [DefaultValue(_GrantMaxAreaRank)]
        public bool GrantMaxAreaRank
        {
            set
            {
                SetSetting("GrantMaxAreaRank", value);
            }
            get
            {
                return TryGetSetting("GrantMaxAreaRank", _GrantMaxAreaRank);
            }
        }
        private const bool _GrantMaxAreaRank = true;

        /// <summary>
        /// When enabled, characters and main pawns receive all five jewelry slots on login without
        /// spending orb unlocks on accessory-slot upgrades.
        /// </summary>
        [DefaultValue(_GrantMaxJewelrySlots)]
        public bool GrantMaxJewelrySlots
        {
            set
            {
                SetSetting("GrantMaxJewelrySlots", value);
            }
            get
            {
                return TryGetSetting("GrantMaxJewelrySlots", _GrantMaxJewelrySlots);
            }
        }
        private const bool _GrantMaxJewelrySlots = true;

        /// <summary>
        /// When enabled, normal-mode dungeon trash (non-boss, non-quest) gains a single in-place
        /// repop so small dungeons can be farmed without leaving the instance.
        /// </summary>
        [DefaultValue(_EnableDungeonMobRepop)]
        public bool EnableDungeonMobRepop
        {
            set
            {
                SetSetting("EnableDungeonMobRepop", value);
            }
            get
            {
                return TryGetSetting("EnableDungeonMobRepop", _EnableDungeonMobRepop);
            }
        }
        private const bool _EnableDungeonMobRepop = true;

        /// <summary>
        /// Multiplier applied to the resolved base repop wait (default base is syncLevel ×
        /// <see cref="ExplorationMobRepopSecondsPerSyncLevel"/> in sync dungeons).
        /// </summary>
        [DefaultValue(_ExplorationMobRepopWaitMultiplier)]
        public double ExplorationMobRepopWaitMultiplier
        {
            set
            {
                SetSetting("ExplorationMobRepopWaitMultiplier", value);
            }
            get
            {
                return TryGetSetting("ExplorationMobRepopWaitMultiplier", _ExplorationMobRepopWaitMultiplier);
            }
        }
        private const double _ExplorationMobRepopWaitMultiplier = 0.35;

        /// <summary>
        /// Fixed base repop wait in seconds. When 0, wait is derived from sync or mob level.
        /// </summary>
        [DefaultValue(_ExplorationMobRepopBaseSeconds)]
        public uint ExplorationMobRepopBaseSeconds
        {
            set
            {
                SetSetting("ExplorationMobRepopBaseSeconds", value);
            }
            get
            {
                return TryGetSetting("ExplorationMobRepopBaseSeconds", _ExplorationMobRepopBaseSeconds);
            }
        }
        private const uint _ExplorationMobRepopBaseSeconds = 0;

        /// <summary>
        /// Base wait contribution per sync recommended level when
        /// <see cref="ExplorationMobRepopBaseSeconds"/> is 0 (rec-3 dungeon → 3 × this value).
        /// </summary>
        [DefaultValue(_ExplorationMobRepopSecondsPerSyncLevel)]
        public uint ExplorationMobRepopSecondsPerSyncLevel
        {
            set
            {
                SetSetting("ExplorationMobRepopSecondsPerSyncLevel", value);
            }
            get
            {
                return TryGetSetting("ExplorationMobRepopSecondsPerSyncLevel", _ExplorationMobRepopSecondsPerSyncLevel);
            }
        }
        private const uint _ExplorationMobRepopSecondsPerSyncLevel = 2;

        [DefaultValue(_ExplorationMobRepopMinWaitSeconds)]
        public uint ExplorationMobRepopMinWaitSeconds
        {
            set
            {
                SetSetting("ExplorationMobRepopMinWaitSeconds", value);
            }
            get
            {
                return TryGetSetting("ExplorationMobRepopMinWaitSeconds", _ExplorationMobRepopMinWaitSeconds);
            }
        }
        private const uint _ExplorationMobRepopMinWaitSeconds = 5;

        [DefaultValue(_ExplorationMobRepopMaxWaitSeconds)]
        public uint ExplorationMobRepopMaxWaitSeconds
        {
            set
            {
                SetSetting("ExplorationMobRepopMaxWaitSeconds", value);
            }
            get
            {
                return TryGetSetting("ExplorationMobRepopMaxWaitSeconds", _ExplorationMobRepopMaxWaitSeconds);
            }
        }
        private const uint _ExplorationMobRepopMaxWaitSeconds = 90;

        /// <summary>
        /// When enabled, any area point reward is converted into character/job EXP at a 1:1 ratio
        /// (e.g. a reward of 80 area points instead grants 80 EXP) and no area points are added to
        /// the player's area rank. This applies to every area point source: quest/board completion,
        /// area and dungeon boss kills, area point consumable items and the /areapoint command.
        /// Intended to pair with GrantMaxAreaRank, redirecting the now-redundant area-rank reward
        /// stream into leveling EXP.
        /// </summary>
        [DefaultValue(_ConvertAreaPointsToExp)]
        public bool ConvertAreaPointsToExp
        {
            set
            {
                SetSetting("ConvertAreaPointsToExp", value);
            }
            get
            {
                return TryGetSetting("ConvertAreaPointsToExp", _ConvertAreaPointsToExp);
            }
        }
        private const bool _ConvertAreaPointsToExp = true;

        /// <summary>
        /// When enabled, every gold (G) cost in the game is treated as free: shops, crafting,
        /// inns, the bazaar, valuable-item recovery and any other gold spend never deduct gold,
        /// and the prices shown to the client for these are reported as 0 so nothing is ever
        /// blocked for "insufficient funds". Other currencies (Rift Points, Blood Orbs, etc.)
        /// are unaffected.
        /// </summary>
        [DefaultValue(_MakeGoldFree)]
        public bool MakeGoldFree
        {
            set
            {
                SetSetting("MakeGoldFree", value);
            }
            get
            {
                return TryGetSetting("MakeGoldFree", _MakeGoldFree);
            }
        }
        private const bool _MakeGoldFree = true;

        /// <summary>
        /// When enabled, any gold (G) reward is converted into character/job EXP at a 1:1 ratio
        /// (e.g. a reward of 500 gold instead grants 500 EXP) and no gold is added to the
        /// player's wallet. This applies to every gold source: quest/board completion, selling
        /// items, equipment recycling, bazaar proceeds, coin pouch items, the Dragon Force orb
        /// tree, login stamps and the /motherlode command. Intended to pair with MakeGoldFree so
        /// the now-redundant gold reward stream is redirected into leveling EXP.
        /// </summary>
        [DefaultValue(_ConvertGoldRewardsToExp)]
        public bool ConvertGoldRewardsToExp
        {
            set
            {
                SetSetting("ConvertGoldRewardsToExp", value);
            }
            get
            {
                return TryGetSetting("ConvertGoldRewardsToExp", _ConvertGoldRewardsToExp);
            }
        }
        private const bool _ConvertGoldRewardsToExp = true;

        /// <summary>
        /// When enabled, quests that require items to be turned in (delivery/turn-in quests)
        /// auto-complete the turn-in as soon as the player is holding the required items, instead
        /// of requiring the player to walk back and hand them to an NPC. The required items are
        /// still consumed when the turn-in step is satisfied.
        /// </summary>
        [DefaultValue(_AutoCompleteTurnInQuests)]
        public bool AutoCompleteTurnInQuests
        {
            set
            {
                SetSetting("AutoCompleteTurnInQuests", value);
            }
            get
            {
                return TryGetSetting("AutoCompleteTurnInQuests", _AutoCompleteTurnInQuests);
            }
        }
        private const bool _AutoCompleteTurnInQuests = true;

        /// <summary>
        /// When enabled, items dropped by defeated enemies are sent straight to the player's
        /// inventory (and gold/coin pouches handled per the gold settings) instead of spawning a
        /// loot bag that must be picked up manually. Anything that does not fit (e.g. a full
        /// inventory) still drops as a bag for manual pickup.
        /// </summary>
        [DefaultValue(_EnableAutoLoot)]
        public bool EnableAutoLoot
        {
            set
            {
                SetSetting("EnableAutoLoot", value);
            }
            get
            {
                return TryGetSetting("EnableAutoLoot", _EnableAutoLoot);
            }
        }
        private const bool _EnableAutoLoot = true;

        /// <summary>
        /// Minutes between automatic Revival Power and Golden Gemstone recharges.
        /// </summary>
        [DefaultValue(_RevivalRechargeIntervalMinutes)]
        public uint RevivalRechargeIntervalMinutes
        {
            set
            {
                SetSetting("RevivalRechargeIntervalMinutes", value);
            }
            get
            {
                return TryGetSetting("RevivalRechargeIntervalMinutes", _RevivalRechargeIntervalMinutes);
            }
        }
        private const uint _RevivalRechargeIntervalMinutes = 45;

        /// <summary>
        /// Maximum Revival Power stock a character can hold.
        /// </summary>
        [DefaultValue(_RevivalPowerMax)]
        public byte RevivalPowerMax
        {
            set
            {
                SetSetting("RevivalPowerMax", value);
            }
            get
            {
                return TryGetSetting("RevivalPowerMax", _RevivalPowerMax);
            }
        }
        private const byte _RevivalPowerMax = 3;

        /// <summary>
        /// Golden Gemstones added to the wallet on each recharge.
        /// </summary>
        [DefaultValue(_RevivalRechargeGoldenGemstoneAmount)]
        public uint RevivalRechargeGoldenGemstoneAmount
        {
            set
            {
                SetSetting("RevivalRechargeGoldenGemstoneAmount", value);
            }
            get
            {
                return TryGetSetting("RevivalRechargeGoldenGemstoneAmount", _RevivalRechargeGoldenGemstoneAmount);
            }
        }
        private const uint _RevivalRechargeGoldenGemstoneAmount = 1;

        /// <summary>
        /// Shared/world/main quests already grant their rewards to every party member. Personal
        /// quests (Light board quests, Tutorial, Substory) normally reward only the player who turns
        /// them in. When enabled, those personal quest rewards (items, gold/exp/RP/etc.) are also
        /// spread to every party member, matching shared-quest behaviour. Content/feature unlocks
        /// stay with the turn-in player so story/feature progression is not granted to others.
        /// </summary>
        [DefaultValue(_SharePersonalQuestRewardsWithParty)]
        public bool SharePersonalQuestRewardsWithParty
        {
            set
            {
                SetSetting("SharePersonalQuestRewardsWithParty", value);
            }
            get
            {
                return TryGetSetting("SharePersonalQuestRewardsWithParty", _SharePersonalQuestRewardsWithParty);
            }
        }
        private const bool _SharePersonalQuestRewardsWithParty = true;

        /// <summary>
        /// Configures the chance that various gathering tools can break
        /// when the player performs a gathering action.
        /// </summary>
        [DefaultValue(@"new Dictionary<ItemId,double>
{
    [ItemId.Pickaxe] = 0.3,
    [ItemId.EnhancedPickaxe] = 0.2,
    [ItemId.ArtisansPickaxe] = 0.1,
    [ItemId.LumberKnife] = 0.3,
    [ItemId.EnhancedLumberKnife] = 0.2,
    [ItemId.ArtisansLumberKnife] = 0.1,
    [ItemId.Lockpick] = 0.3,
    [ItemId.EnhancedLockpick] = 0.2,
    [ItemId.AllPurposeLockpick] = 0.1,
};")]
        public Dictionary<ItemId,double> ToolBreakChance
        {
            set
            {
                SetSetting("ToolBreakChance", value);
            }
            get
            {
                return TryGetSetting("ToolBreakChance", new Dictionary<ItemId,double>
                {
                    [ItemId.Pickaxe] = 0.3,
                    [ItemId.EnhancedPickaxe] = 0.2,
                    [ItemId.ArtisansPickaxe] = 0.1,
                    [ItemId.LumberKnife] = 0.3,
                    [ItemId.EnhancedLumberKnife] = 0.2,
                    [ItemId.ArtisansLumberKnife] = 0.1,
                    [ItemId.Lockpick] = 0.3,
                    [ItemId.EnhancedLockpick] = 0.2,
                    [ItemId.AllPurposeLockpick] = 0.1,
                });
            }
        }

        /// <summary>
        /// The maximum number of drop slots in a gather point
        /// generated by the default drop generator.
        /// </summary>
        [DefaultValue(_DefaultGatherDropMaxSlots)]
        public int DefaultGatherDropMaxSlots
        {
            set
            {
                SetSetting("DefaultGatherDropMaxSlots", value);
            }
            get
            {
                return TryGetSetting("DefaultGatherDropMaxSlots", _DefaultGatherDropMaxSlots);
            }
        }
        private const int _DefaultGatherDropMaxSlots = 3;

        /// <summary>
        /// The maximum number of drops to be generated on a single roll
        /// when auto generating gathering drops.
        /// </summary>
        [DefaultValue(_MaximumDropsPerDefaultGatherRoll)]
        public int MaximumDropsPerDefaultGatherRoll
        {
            set
            {
                SetSetting("MaximumDropsPerDefaultGatherRoll", value);
            }
            get
            {
                return TryGetSetting("MaximumDropsPerDefaultGatherRoll", _MaximumDropsPerDefaultGatherRoll);
            }
        }
        private const int _MaximumDropsPerDefaultGatherRoll = 3;

        /// <summary>
        /// Controls how punishing the gathering results are.
        /// A high value is more punishing than a lower value.
        /// </summary>
        [DefaultValue(_DefaultGatherDropsRandomBias)]
        public double DefaultGatherDropsRandomBias
        {
            set
            {
                SetSetting("DefaultGatherDropsRandomBias", value);
            }
            get
            {
                return TryGetSetting("DefaultGatherDropsRandomBias", _DefaultGatherDropsRandomBias);
            }
        }
        private const double _DefaultGatherDropsRandomBias = 2.0;

        /// <summary>
        /// If set to true, enables the server to generate gathering drops
        /// populated by ddon-tools.
        /// </summary>
        [DefaultValue(_EnableToolGatheringDrops)]
        public bool EnableToolGatheringDrops
        {
            set
            {
                SetSetting("EnableToolGatheringDrops", value);
            }
            get
            {
                return TryGetSetting("EnableToolGatheringDrops", _EnableToolGatheringDrops);
            }
        }
        private const bool _EnableToolGatheringDrops = true;

        /// <summary>
        /// If set to true, enables the automatically generate gathering drops
        /// based on data scraped from wikis.
        /// @note Experimental: This feature is still in development and needs
        ///                     more balance and testing before being enabled
        ///                     all the time.
        /// </summary>
        [DefaultValue(_EnableDefaultGatheringDrops)]
        public bool EnableDefaultGatheringDrops
        {
            set
            {
                SetSetting("EnableDefaultGatheringDrops", value);
            }
            get
            {
                return TryGetSetting("EnableDefaultGatheringDrops", _EnableDefaultGatheringDrops);
            }
        }
        private const bool _EnableDefaultGatheringDrops = false;

        /// <summary>
        /// When enabled, defeated enemies can drop craftable performance gear (weapons, armor,
        /// jewelry, lanterns) that would normally require pawn crafting. Drops are level-matched
        /// to the enemy killed. Pair with EnableWildCosmeticDrops for dress/cosmetic items.
        /// </summary>
        [DefaultValue(_EnableWildCraftedGearDrops)]
        public bool EnableWildCraftedGearDrops
        {
            set
            {
                SetSetting("EnableWildCraftedGearDrops", value);
            }
            get
            {
                return TryGetSetting("EnableWildCraftedGearDrops", _EnableWildCraftedGearDrops);
            }
        }
        private const bool _EnableWildCraftedGearDrops = false;

        /// <summary>
        /// When enabled, defeated enemies can drop dress/cosmetic equipment (clothing, overwear,
        /// ensembles) from the full item list, not only craft recipes.
        /// </summary>
        [DefaultValue(_EnableWildCosmeticDrops)]
        public bool EnableWildCosmeticDrops
        {
            set
            {
                SetSetting("EnableWildCosmeticDrops", value);
            }
            get
            {
                return TryGetSetting("EnableWildCosmeticDrops", _EnableWildCosmeticDrops);
            }
        }
        private const bool _EnableWildCosmeticDrops = true;

        /// <summary>
        /// Per-kill chance (0.0–1.0) to roll a crafted performance gear drop when
        /// <see cref="EnableWildCraftedGearDrops"/> is enabled. Bosses use
        /// <see cref="WildCraftedGearBossDropChance"/> instead.
        /// </summary>
        [DefaultValue(_WildCraftedGearDropChance)]
        public double WildCraftedGearDropChance
        {
            set
            {
                SetSetting("WildCraftedGearDropChance", value);
            }
            get
            {
                return TryGetSetting("WildCraftedGearDropChance", _WildCraftedGearDropChance);
            }
        }
        private const double _WildCraftedGearDropChance = 0.04;

        /// <summary>
        /// Per-kill chance (0.0–1.0) for crafted performance gear drops from boss enemies.
        /// </summary>
        [DefaultValue(_WildCraftedGearBossDropChance)]
        public double WildCraftedGearBossDropChance
        {
            set
            {
                SetSetting("WildCraftedGearBossDropChance", value);
            }
            get
            {
                return TryGetSetting("WildCraftedGearBossDropChance", _WildCraftedGearBossDropChance);
            }
        }
        private const double _WildCraftedGearBossDropChance = 0.15;

        /// <summary>
        /// Per-kill chance (0.0–1.0) to roll a cosmetic/dress equipment drop when
        /// <see cref="EnableWildCosmeticDrops"/> is enabled. Bosses use
        /// <see cref="WildCosmeticBossDropChance"/> instead.
        /// </summary>
        [DefaultValue(_WildCosmeticDropChance)]
        public double WildCosmeticDropChance
        {
            set
            {
                SetSetting("WildCosmeticDropChance", value);
            }
            get
            {
                return TryGetSetting("WildCosmeticDropChance", _WildCosmeticDropChance);
            }
        }
        private const double _WildCosmeticDropChance = 0.02;

        /// <summary>
        /// Per-kill chance (0.0–1.0) for cosmetic/dress equipment drops from boss enemies.
        /// </summary>
        [DefaultValue(_WildCosmeticBossDropChance)]
        public double WildCosmeticBossDropChance
        {
            set
            {
                SetSetting("WildCosmeticBossDropChance", value);
            }
            get
            {
                return TryGetSetting("WildCosmeticBossDropChance", _WildCosmeticBossDropChance);
            }
        }
        private const double _WildCosmeticBossDropChance = 0.08;

        /// <summary>
        /// How many levels above/below an enemy's level a wild gear/cosmetic drop candidate
        /// may be. An enemy at Lv 20 can drop items with equip levels in [15, 25] when this is 5.
        /// </summary>
        [DefaultValue(_WildGearDropLevelTolerance)]
        public uint WildGearDropLevelTolerance
        {
            set
            {
                SetSetting("WildGearDropLevelTolerance", value);
            }
            get
            {
                return TryGetSetting("WildGearDropLevelTolerance", _WildGearDropLevelTolerance);
            }
        }
        private const uint _WildGearDropLevelTolerance = 5;

        /// <summary>
        /// When enabled, defeated enemies drop finished performance gear and regional craft materials
        /// tiered by player job level, area rank, and dungeon sync level. Personal loot is handled
        /// per party member by the kill handler. Pair with <see cref="RemoveCombatGearFromShops"/>.
        /// </summary>
        [DefaultValue(_EnableExplorationProgressionDrops)]
        public bool EnableExplorationProgressionDrops
        {
            set
            {
                SetSetting("EnableExplorationProgressionDrops", value);
            }
            get
            {
                return TryGetSetting("EnableExplorationProgressionDrops", _EnableExplorationProgressionDrops);
            }
        }
        private const bool _EnableExplorationProgressionDrops = true;

        /// <summary>
        /// Removes weapons, armor, jewelry, and lanterns from gold shops and the job value shop
        /// so combat gear comes from exploration drops only.
        /// </summary>
        [DefaultValue(_RemoveCombatGearFromShops)]
        public bool RemoveCombatGearFromShops
        {
            set
            {
                SetSetting("RemoveCombatGearFromShops", value);
            }
            get
            {
                return TryGetSetting("RemoveCombatGearFromShops", _RemoveCombatGearFromShops);
            }
        }
        private const bool _RemoveCombatGearFromShops = true;

        /// <summary>
        /// When switching jobs, grant starter main/sub weapons from PawnStartGear.csv if the
        /// new job has none in storage. Used with pure-drop gear progression.
        /// </summary>
        [DefaultValue(_GrantStarterWeaponsOnJobChange)]
        public bool GrantStarterWeaponsOnJobChange
        {
            set
            {
                SetSetting("GrantStarterWeaponsOnJobChange", value);
            }
            get
            {
                return TryGetSetting("GrantStarterWeaponsOnJobChange", _GrantStarterWeaponsOnJobChange);
            }
        }
        private const bool _GrantStarterWeaponsOnJobChange = true;

        /// <summary>
        /// Per-kill chance (0.0–1.0) to roll a finished gear drop from
        /// <see cref="EnableExplorationProgressionDrops"/>. At ~0.35, a 9-mob clear
        /// averages about three new pieces before duplicate filtering.
        /// </summary>
        [DefaultValue(_ExplorationGearDropChance)]
        public double ExplorationGearDropChance
        {
            set
            {
                SetSetting("ExplorationGearDropChance", value);
            }
            get
            {
                return TryGetSetting("ExplorationGearDropChance", _ExplorationGearDropChance);
            }
        }
        private const double _ExplorationGearDropChance = 0.35;

        /// <summary>
        /// Boss per-kill chance for finished gear drops.
        /// </summary>
        [DefaultValue(_ExplorationGearBossDropChance)]
        public double ExplorationGearBossDropChance
        {
            set
            {
                SetSetting("ExplorationGearBossDropChance", value);
            }
            get
            {
                return TryGetSetting("ExplorationGearBossDropChance", _ExplorationGearBossDropChance);
            }
        }
        private const double _ExplorationGearBossDropChance = 0.25;

        /// <summary>
        /// After this many consecutive kills without exploration gear, the next normal kill
        /// guarantees a gear roll (still subject to duplicate filtering and band expansion).
        /// Set to 0 to disable pity.
        /// </summary>
        [DefaultValue(_ExplorationGearPityKillThreshold)]
        public uint ExplorationGearPityKillThreshold
        {
            set
            {
                SetSetting("ExplorationGearPityKillThreshold", value);
            }
            get
            {
                return TryGetSetting("ExplorationGearPityKillThreshold", _ExplorationGearPityKillThreshold);
            }
        }
        private const uint _ExplorationGearPityKillThreshold = 3;

        /// <summary>
        /// Added to <see cref="ExplorationGearDropChance"/> per empty performance equip slot
        /// (weapons, armor, jewelry, lantern — not clothing/overwear).
        /// </summary>
        [DefaultValue(_ExplorationEmptySlotDropChanceBonus)]
        public double ExplorationEmptySlotDropChanceBonus
        {
            set
            {
                SetSetting("ExplorationEmptySlotDropChanceBonus", value);
            }
            get
            {
                return TryGetSetting("ExplorationEmptySlotDropChanceBonus", _ExplorationEmptySlotDropChanceBonus);
            }
        }
        private const double _ExplorationEmptySlotDropChanceBonus = 0.05;

        /// <summary>
        /// Chance (0.0–1.0) that a gear roll tries job-appropriate weapons before armor or jewelry.
        /// </summary>
        [DefaultValue(_ExplorationWeaponFirstRollChance)]
        public double ExplorationWeaponFirstRollChance
        {
            set
            {
                SetSetting("ExplorationWeaponFirstRollChance", value);
            }
            get
            {
                return TryGetSetting("ExplorationWeaponFirstRollChance", _ExplorationWeaponFirstRollChance);
            }
        }
        private const double _ExplorationWeaponFirstRollChance = 0.55;

        /// <summary>
        /// Per-kill chance (0.0–1.0) to roll a regional craft material drop.
        /// </summary>
        [DefaultValue(_ExplorationMaterialDropChance)]
        public double ExplorationMaterialDropChance
        {
            set
            {
                SetSetting("ExplorationMaterialDropChance", value);
            }
            get
            {
                return TryGetSetting("ExplorationMaterialDropChance", _ExplorationMaterialDropChance);
            }
        }
        private const double _ExplorationMaterialDropChance = 0.22;

        /// <summary>
        /// Boss per-kill chance for regional craft material drops.
        /// </summary>
        [DefaultValue(_ExplorationMaterialBossDropChance)]
        public double ExplorationMaterialBossDropChance
        {
            set
            {
                SetSetting("ExplorationMaterialBossDropChance", value);
            }
            get
            {
                return TryGetSetting("ExplorationMaterialBossDropChance", _ExplorationMaterialBossDropChance);
            }
        }
        private const double _ExplorationMaterialBossDropChance = 0.45;

        /// <summary>
        /// Half-width of the finished gear level band around the resolved effective tier.
        /// </summary>
        [DefaultValue(_ExplorationLootBandRadius)]
        public uint ExplorationLootBandRadius
        {
            set
            {
                SetSetting("ExplorationLootBandRadius", value);
            }
            get
            {
                return TryGetSetting("ExplorationLootBandRadius", _ExplorationLootBandRadius);
            }
        }
        private const uint _ExplorationLootBandRadius = 2;

        /// <summary>
        /// Half-width of the craft material item_level band around the resolved effective tier.
        /// </summary>
        [DefaultValue(_ExplorationMaterialLootBandRadius)]
        public uint ExplorationMaterialLootBandRadius
        {
            set
            {
                SetSetting("ExplorationMaterialLootBandRadius", value);
            }
            get
            {
                return TryGetSetting("ExplorationMaterialLootBandRadius", _ExplorationMaterialLootBandRadius);
            }
        }
        private const uint _ExplorationMaterialLootBandRadius = 2;

        /// <summary>
        /// Multiplier applied to area rank when computing the area tier component of loot level
        /// (areaTier = floor(areaRank * multiplier)).
        /// </summary>
        [DefaultValue(_ExplorationAreaRankTierMultiplier)]
        public double ExplorationAreaRankTierMultiplier
        {
            set
            {
                SetSetting("ExplorationAreaRankTierMultiplier", value);
            }
            get
            {
                return TryGetSetting("ExplorationAreaRankTierMultiplier", _ExplorationAreaRankTierMultiplier);
            }
        }
        private const double _ExplorationAreaRankTierMultiplier = 2.5;

        /// <summary>
        /// When enabled, area rank does not inflate loot tier inside level-sync dungeons;
        /// tier is max(player job level, stage recommended level) only.
        /// </summary>
        [DefaultValue(_ExplorationIgnoreAreaRankInSyncZones)]
        public bool ExplorationIgnoreAreaRankInSyncZones
        {
            set
            {
                SetSetting("ExplorationIgnoreAreaRankInSyncZones", value);
            }
            get
            {
                return TryGetSetting("ExplorationIgnoreAreaRankInSyncZones", _ExplorationIgnoreAreaRankInSyncZones);
            }
        }
        private const bool _ExplorationIgnoreAreaRankInSyncZones = true;

        /// <summary>
        /// The amount of golden gemstones it costs to use the beauty parlor.
        /// </summary>
        [DefaultValue(_BeautyParlorGGPrice)]
        public uint BeautyParlorGGPrice
        {
            set
            {
                SetSetting("BeautyParlorGGPrice", value);
            }
            get
            {
                return TryGetSetting("BeautyParlorGGPrice", _BeautyParlorGGPrice);
            }
        }
        private const uint _BeautyParlorGGPrice = 5;

        /// <summary>
        /// The amount of silver tickets it costs to use the beauty parlor.
        /// </summary>
        [DefaultValue(_BeautyParlorSTPrice)]
        public uint BeautyParlorSTPrice
        {
            set
            {
                SetSetting("BeautyParlorSTPrice", value);
            }
            get
            {
                return TryGetSetting("BeautyParlorSTPrice", _BeautyParlorSTPrice);
            }
        }
        private const uint _BeautyParlorSTPrice = 200;

        /// <summary>
        /// The amount of golden gemstones it costs to use the reincarnation menu.
        /// </summary>
        [DefaultValue(_ReincarnationGGPrice)]
        public uint ReincarnationGGPrice
        {
            set
            {
                SetSetting("ReincarnationGGPrice", value);
            }
            get
            {
                return TryGetSetting("ReincarnationGGPrice", _ReincarnationGGPrice);
            }
        }
        private const uint _ReincarnationGGPrice = 5;

        /// <summary>
        /// Controls the relative weight of drop items to gathering items when generating delivery board quests.
        /// Values less than 1 encourage gathering items, values greater than 1 encourage drop items.
        /// </summary>
        [DefaultValue(_LightQuestGenerationDropItemWeight)]
        public double LightQuestGenerationDropItemWeight
        {
            set
            {
                SetSetting("LightQuestGenerationDropItemWeight", value);
            }
            get
            {
                return TryGetSetting("LightQuestGenerationDropItemWeight", _LightQuestGenerationDropItemWeight);
            }
        }
        private const double _LightQuestGenerationDropItemWeight = 0.5;

        /// <summary>
        /// When generating light quests, controls the amount of attempts that will be made to meet restraints on level bounds and uniqueness.
        /// </summary>
        [DefaultValue(_LightQuestGenerationAttemptsPerQuest)]
        public int LightQuestGenerationAttemptsPerQuest
        {
            set
            {
                SetSetting("LightQuestGenerationAttemptsPerQuest", value);
            }
            get
            {
                return TryGetSetting("LightQuestGenerationAttemptsPerQuest", _LightQuestGenerationAttemptsPerQuest);
            }
        }
        private const int _LightQuestGenerationAttemptsPerQuest = 20;

        /// <summary>
        /// The number of times a player can repeat a board quest before it is no longer offered. Resets when quests rotate.
        /// </summary>
        [DefaultValue(_LightQuestRepeatsPerDay)]
        public uint Board
        {
            set
            {
                SetSetting("LightQuestRepeatsPerDay", value);
            }
            get
            {
                return TryGetSetting("LightQuestRepeatsPerDay", _LightQuestRepeatsPerDay);
            }
        }
        private const uint _LightQuestRepeatsPerDay = 10000;

        /// <summary>
        /// 
        /// </summary>
        [DefaultValue(_UrlDomain)]
        public string UrlDomain
        {
            set
            {
                SetSetting("UrlDomain", value);
            }
            get
            {
                return TryGetSetting("UrlDomain", _UrlDomain);
            }
        }
        private const string _UrlDomain = "http://localhost:{52099}";

        /// <summary>
        /// Various URLs used by the client.
        /// Shared with the login server.
        /// </summary>
        [DefaultValue(_UrlManual)]
        public string UrlManual
        {
            set
            {
                SetSetting("UrlManual", value);
            }
            get
            {
                return TryGetSetting("UrlManual", _UrlManual);
            }
        }
        private const string _UrlManual = "http://localhost:{52099}/manual_nfb/";

        /// <summary>
        /// 
        /// </summary>
        [DefaultValue(_UrlShopDetail)]
        public string UrlShopDetail
        {
            set
            {
                SetSetting("UrlShopDetail", value);
            }
            get
            {
                return TryGetSetting("UrlShopDetail", _UrlShopDetail);
            }
        }
        private const string _UrlShopDetail = "http://localhost:{52099}/shop/ingame/stone/detail";

        /// <summary>
        /// 
        /// </summary>
        [DefaultValue(_UrlShopCounterA)]
        public string UrlShopCounterA
        {
            set
            {
                SetSetting("UrlShopCounterA", value);
            }
            get
            {
                return TryGetSetting("UrlShopCounterA", _UrlShopCounterA);
            }
        }
        private const string _UrlShopCounterA = "http://localhost:{52099}/shop/ingame/counter?";

        /// <summary>
        /// 
        /// </summary>
        [DefaultValue(_UrlShopAttention)]
        public string UrlShopAttention
        {
            set
            {
                SetSetting<string>("UrlShopAttention", value);
            }
            get
            {
                return TryGetSetting("UrlShopAttention", _UrlShopAttention);
            }
        }
        private const string _UrlShopAttention = "http://localhost:{52099}/shop/ingame/attention?";

        /// <summary>
        /// 
        /// </summary>
        [DefaultValue(_UrlShopStoneLimit)]
        public string UrlShopStoneLimit
        {
            set
            {
                SetSetting("UrlShopStoneLimit", value);
            }
            get
            {
                return TryGetSetting("UrlShopStoneLimit", _UrlShopStoneLimit);
            }
        }
        private const string _UrlShopStoneLimit = "http://localhost:{52099}/shop/ingame/stone/limit";

        /// <summary>
        /// 
        /// </summary>
        [DefaultValue(_UrlShopCounterB)]
        public string UrlShopCounterB
        {
            set
            {
                SetSetting("UrlShopCounterB", value);
            }
            get
            {
                return TryGetSetting("UrlShopCounterB", _UrlShopCounterB);
            }
        }
        private const string _UrlShopCounterB = "http://localhost:{52099}/shop/ingame/counter?";

        /// <summary>
        /// 
        /// </summary>
        [DefaultValue(_UrlChargeCallback)]
        public string UrlChargeCallback
        {
            set
            {
                SetSetting("UrlChargeCallback", value);
            }
            get
            {
                return TryGetSetting("UrlChargeCallback", _UrlChargeCallback);
            }
        }
        private const string _UrlChargeCallback = "http://localhost:{52099}/opening/entry/ddo/cog_callback/charge";

        /// <summary>
        /// 
        /// </summary>
        [DefaultValue(_UrlChargeA)]
        public string UrlChargeA
        {
            set
            {
                SetSetting("UrlChargeA", value);
            }
            get
            {
                return TryGetSetting("UrlChargeA", _UrlChargeA);
            }
        }
        private const string _UrlChargeA = "http://localhost:{52099}/sp_ingame/charge/";

        /// <summary>
        /// 
        /// </summary>
        [DefaultValue(_UrlSample9)]
        public string UrlSample9
        {
            set
            {
                SetSetting("UrlSample9", value);
            }
            get
            {
                return TryGetSetting("UrlSample9", _UrlSample9);
            }
        }
        private const string _UrlSample9 = "http://sample09.html";

        /// <summary>
        /// 
        /// </summary>
        [DefaultValue(_UrlSample10)]
        public string UrlSample10
        {
            set
            {
                SetSetting("UrlSample10", value);
            }
            get
            {
                return TryGetSetting("UrlSample10", _UrlSample10);
            }
        }
        private const string _UrlSample10 = "http://sample10.html";

        /// <summary>
        /// 
        /// </summary>
        [DefaultValue(_UrlCampaignBanner)]
        public string UrlCampaignBanner
        {
            set
            {
                SetSetting("UrlCampaignBanner", value);
            }
            get
            {
                return TryGetSetting("UrlCampaignBanner", _UrlCampaignBanner);
            }
        }
        private const string _UrlCampaignBanner = "http://localhost:{52099}/sp_ingame/campaign/bnr/bnr01.html?";

        /// <summary>
        /// 
        /// </summary>
        [DefaultValue(_UrlSupportIndex)]
        public string UrlSupportIndex
        {
            set
            {
                SetSetting("UrlSupportIndex", value);
            }
            get
            {
                return TryGetSetting("UrlSupportIndex", _UrlSupportIndex);
            }
        }
        private const string _UrlSupportIndex = "http://localhost:{52099}/sp_ingame/support/index.html";

        /// <summary>
        /// 
        /// </summary>
        [DefaultValue(_UrlPhotoupAuthorize)]
        public string UrlPhotoupAuthorize
        {
            set
            {
                SetSetting("UrlPhotoupAuthorize", value);
            }
            get
            {
                return TryGetSetting("UrlPhotoupAuthorize", _UrlPhotoupAuthorize);
            }
        }
        private const string _UrlPhotoupAuthorize = "http://localhost:{52099}/api/photoup/authorize";

        /// <summary>
        /// 
        /// </summary>
        [DefaultValue(_UrlApiA)]
        public string UrlApiA
        {
            set
            {
                SetSetting("UrlApiA", value);
            }
            get
            {
                return TryGetSetting("UrlApiA", _UrlApiA);
            }
        }
        private const string _UrlApiA = "http://localhost:{52099}/link/api";

        /// <summary>
        /// 
        /// </summary>
        [DefaultValue(_UrlApiB)]
        public string UrlApiB
        {
            set
            {
                SetSetting("UrlApiB", value);
            }
            get
            {
                return TryGetSetting("UrlApiB", _UrlApiB);
            }
        }
        private const string _UrlApiB = "http://localhost:{52099}/link/api";

        /// <summary>
        /// 
        /// </summary>
        [DefaultValue(_UrlIndex)]
        public string UrlIndex
        {
            set
            {
                SetSetting("UrlIndex", value);
            }
            get
            {
                return TryGetSetting("UrlIndex", _UrlIndex);
            }
        }
        private const string _UrlIndex = "http://localhost:{52099}/sp_ingame/link/index.html";

        /// <summary>
        /// 
        /// </summary>
        [DefaultValue(_UrlCampaign)]
        public string UrlCampaign
        {
            set
            {
                SetSetting("UrlCampaign", value);
            }
            get
            {
                return TryGetSetting("UrlCampaign", _UrlCampaign);
            }
        }
        private const string _UrlCampaign = "http://localhost:{52099}/sp_ingame/campaign/bnr/slide.html";

        /// <summary>
        /// 
        /// </summary>
        [DefaultValue(_UrlChargeB)]
        public string UrlChargeB
        {
            set
            {
                SetSetting("UrlChargeB", value);
            }
            get
            {
                return TryGetSetting("UrlChargeB", _UrlChargeB);
            }
        }
        private const string _UrlChargeB = "http://localhost:{52099}/sp_ingame/charge/";

        /// <summary>
        /// 
        /// </summary>
        [DefaultValue(_UrlCompanionImage)]
        public string UrlCompanionImage
        {
            set
            {
                SetSetting("UrlCompanionImage", value);
            }
            get
            {
                return TryGetSetting("UrlCompanionImage", _UrlCompanionImage);
            }
        }
        private const string _UrlCompanionImage = "http://localhost:{52099}/";
        
        /// <summary>
        /// How many pawns to consider for random sampling e.g. for clan hall pawns.
        /// Specifically this affects how many rows of the DB should be considered for randomization.
        /// 0 disables random pawns, which might cause undefined behavior, a minimum of 100 is advised.
        /// Avoid very large values like Integer.MAX_VALUE to not degrade performance.
        /// </summary>
        [DefaultValue(_RandomPawnMaxSample)]
        public uint RandomPawnMaxSample
        {
            set
            {
                SetSetting("RandomPawnMaxSample", value);
            }
            get
            {
                return TryGetSetting("RandomPawnMaxSample", _RandomPawnMaxSample);
            }
        }
        private const uint _RandomPawnMaxSample = 10000;

        /// <summary>
        /// The bonus for Job Training kills with a Partner Pawn present.
        /// Setting this to 0 effectively disables bonus kills with a Partner Pawn.
        /// </summary>
        [DefaultValue(_JobTrainingPartnerBonus)]
        public uint JobTrainingPartnerBonus
        {
            set
            {
                SetSetting("JobTrainingPartnerBonus", value);
            }
            get
            {
                return TryGetSetting("JobTrainingPartnerBonus", _JobTrainingPartnerBonus);
            }
        }
        private const uint _JobTrainingPartnerBonus = 1;

        /// <summary>
        /// Configures the amount BO that that 1 HO will convert to.
        /// </summary>
        [DefaultValue(_HighOrbConversionRate)]
        public uint HighOrbConversionRate
        {
            set
            {
                SetSetting("HighOrbConversionRate", value);
            }
            get
            {
                return TryGetSetting("HighOrbConversionRate", _HighOrbConversionRate);
            }
        }
        private const uint _HighOrbConversionRate = 100;

        /// <summary>
        /// Configures if the HO exchange is enabled or not.
        /// @warning Current implementation is able to be exploited for infinite conversion.
        /// </summary>
        [DefaultValue(_EnableHighOrbConversion)]
        public bool EnableHighOrbConversion
        {
            set
            {
                SetSetting("EnableHighOrbConversion", value);
            }
            get
            {
                return TryGetSetting("EnableHighOrbConversion", _EnableHighOrbConversion);
            }
        }
        private const bool _EnableHighOrbConversion = false;

        /// <summary>
        /// When set to true, allows pawns to bypass Job Training requirements
        /// and learn any skill or augment they otherwise meet the requirements of.
        /// </summary>
        [DefaultValue(_PawnSkipJobTraining)]
        public bool PawnSkipJobTraining
        {
            set
            {
                SetSetting("PawnSkipJobTraining", value);
            }
            get
            {
                return TryGetSetting("PawnSkipJobTraining", _PawnSkipJobTraining);
            }
        }
        private const bool _PawnSkipJobTraining = true;

        /// <summary>
        /// The number of adventure charges that a support pawn has when hired.
        /// Other pieces of the UI seemingly expect this to be 10, but it may be more flexible.
        /// </summary>
        [DefaultValue(_RentalPawnAdventureCount)]
        public byte RentalPawnAdventureCount
        {
            set
            {
                SetSetting("RentalPawnAdventureCount", value);
            }
            get
            {
                return TryGetSetting("RentalPawnAdventureCount", _RentalPawnAdventureCount);
            }
        }
        private const byte _RentalPawnAdventureCount = 10;

        /// <summary>
        /// The number of crafting charges that a support pawn has when hired.
        /// Other pieces of the UI seemingly expect this to be 5, but it may be more flexible.
        /// </summary>
        [DefaultValue(_RentalPawnCraftCount)]
        public byte RentalPawnCraftCount
        {
            set
            {
                SetSetting("RentalPawnCraftCount", value);
            }
            get
            {
                return TryGetSetting("RentalPawnCraftCount", _RentalPawnCraftCount);
            }
        }
        private const byte _RentalPawnCraftCount = 10;

        /// <summary>
        /// Time, in seconds, that a support pawn must be adventuring before it loses one of its adventuring charges.
        /// By default, 1350 seconds = 22.5 minutes, or 6 hours Lestanian time.
        /// </summary>
        [DefaultValue(_RentalPawnAdventureTimer)]
        public uint RentalPawnAdventureTimer
        {
            set
            {
                SetSetting("RentalPawnAdventureTimer", value);
            }
            get
            {
                return TryGetSetting("RentalPawnAdventureTimer", _RentalPawnAdventureTimer);
            }
        }
        private const uint _RentalPawnAdventureTimer = 1350;

        /// <summary>
        /// If true, active rental pawn timers are automatically reset upon returning to a safe area, even if the instance wouldn't normally reset.
        /// This is a QOL feature, since removing and readding them to the party would reset the timer anyways.
        /// </summary>
        [DefaultValue(_RentalPawnAdventureTimerAutoReset)]
        public bool RentalPawnAdventureTimerAutoReset
        {
            set
            {
                SetSetting("RentalPawnAdventureTimerAutoReset", value);
            }
            get
            {
                return TryGetSetting("RentalPawnAdventureTimerAutoReset", _RentalPawnAdventureTimerAutoReset);
            }
        }
        private const bool _RentalPawnAdventureTimerAutoReset = true;

        /// <summary>
        /// If true, rental pawns will consume an adventure charge when starting an EXM, but won't have their usual adventure timer running.
        /// </summary>
        [DefaultValue(_RentalPawnAdventureConsumeOnEXM)]
        public bool RentalPawnAdventureConsumeOnEXM
        {
            set
            {
                SetSetting("RentalPawnAdventureConsumeOnEXM", value);
            }
            get
            {
                return TryGetSetting("RentalPawnAdventureConsumeOnEXM", _RentalPawnAdventureConsumeOnEXM);
            }
        }
        private const bool _RentalPawnAdventureConsumeOnEXM = true;

        /// <summary>
        /// The number of rental points (RP), gained by renting and returning pawns, required to buy one JP for a pawn.
        /// </summary>
        [DefaultValue(_RentalPointConversionRate)]
        public uint RentalPointConversionRate
        {
            set
            {
                SetSetting("RentalPointConversionRate", value);
            }
            get
            {
                return TryGetSetting("RentalPointConversionRate", _RentalPointConversionRate);
            }
        }
        private const uint _RentalPointConversionRate = 10;

        /// <summary>
        /// The maximum number of effects that can be sealed in BBM using red marks.
        /// </summary>
        [DefaultValue(_DispelSealMax)]
        public uint DispelSealMax
        {
            set
            {
                SetSetting("DispelSealMax", value);
            }
            get
            {
                return TryGetSetting("DispelSealMax", _DispelSealMax);
            }
        }
        private const uint _DispelSealMax = 80;

        /// <summary>
        /// The base rate for each seal in BBM, paid using red marks.
        /// The first seal costs N, the second seal costs 2N, the third 3N, and so on.
        /// </summary>
        [DefaultValue(_DispelSealCostRate)]
        public uint DispelSealCostRate
        {
            set
            {
                SetSetting("DispelSealCostRate", value);
            }
            get
            {
                return TryGetSetting("DispelSealCostRate", _DispelSealCostRate);
            }
        }
        private const uint _DispelSealCostRate = 2;

        /// <summary>
        /// The cost of resetting the seals in BBM, paid using red marks.
        /// </summary>
        [DefaultValue(_DispelSealResetRate)]
        public uint DispelSealResetRate
        {
            set
            {
                SetSetting("DispelSealResetRate", value);
            }
            get
            {
                return TryGetSetting("DispelSealResetRate", _DispelSealResetRate);
            }
        }
        private const uint _DispelSealResetRate = 500;

        /// <summary>
        /// The number of reset tickets given for BBM weekly.
        /// </summary>
        [DefaultValue(_BBMWeeklyResetTickets)]
        public uint BBMWeeklyResetTickets
        {
            set
            {
                SetSetting("BBMWeeklyResetTickets", value);
            }
            get
            {
                return TryGetSetting("BBMWeeklyResetTickets", _BBMWeeklyResetTickets);
            }
        }
        private const uint _BBMWeeklyResetTickets = 3;

        /// <summary>
        /// The maximum number of times you can reset BBM using GG, each week.
        /// </summary>
        [DefaultValue(_BBMWeeklyGGResets)]
        public uint BBMWeeklyGGResets
        {
            set
            {
                SetSetting("BBMWeeklyGGResets", value);
            }
            get
            {
                return TryGetSetting("BBMWeeklyGGResets", _BBMWeeklyGGResets);
            }
        }
        private const uint _BBMWeeklyGGResets = 6;

        /// <summary>
        /// The cost of resetting BBM using GG.
        /// </summary>
        [DefaultValue(_BBMResetGGCost)]
        public uint BBMResetGGCost
        {
            set
            {
                SetSetting("BBMResetGGCost", value);
            }
            get
            {
                return TryGetSetting("BBMResetGGCost", _BBMResetGGCost);
            }
        }
        private const uint _BBMResetGGCost = 1;

        /// <summary>
        /// Controls how world quests are rolled and refreshed.
        /// Both modes cannot be active simultaneously.
        /// Valid values:
        ///   InstanceReset - each party instance rolls quests independently on area entry.
        ///   ServerReset   - all players share a single server-wide pool that rotates on a weekly schedule (original game behavior).
        /// </summary>
        [DefaultValue("WorldQuestSystemMode.ServerReset")]
        public WorldQuestSystemMode WorldQuestSystem
        {
            set
            {
                SetSetting("WorldQuestSystem", value);
            }
            get
            {
                return TryGetSetting("WorldQuestSystem", _WorldQuestSystem);
            }
        }
        private const WorldQuestSystemMode _WorldQuestSystem = WorldQuestSystemMode.ServerReset;


        /// <summary>
        /// Timezone used for all calendar-aligned task scheduler resets (daily, weekly) and world
        /// quest seed computation. Set this to the same value on every shard.
        ///
        /// Use the named constants in <see cref="TimeZoneId"/> - they cover both DST-observing and fixed-offset
        /// timezones, so no manual update is ever needed when clocks change:
        ///   TimeZoneInfo ServerTimeZone = TimeZoneId.Japan;          // Japan (JST) - original game timezone
        ///   TimeZoneInfo ServerTimeZone = TimeZoneId.CentralEurope;  // Germany, France, Spain, etc. (CET/CEST auto)
        ///   TimeZoneInfo ServerTimeZone = TimeZoneId.EasternEurope;  // Finland, Greece, Romania, etc. (EET/EEST auto)
        ///   TimeZoneInfo ServerTimeZone = TimeZoneId.UKIreland;      // UK/Ireland (GMT/BST auto)
        ///   TimeZoneInfo ServerTimeZone = TimeZoneId.Eastern;        // US/Canada Eastern (EST/EDT auto)
        ///   TimeZoneInfo ServerTimeZone = TimeZoneId.UTC;            // UTC
        ///
        /// For a timezone not listed in <see cref="TimeZoneId"/>, use FindSystemTimeZoneById with any IANA ID:
        ///   TimeZoneInfo ServerTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Indiana/Knox");
        ///
        /// Full list of IANA timezone IDs:
        ///   https://en.wikipedia.org/wiki/List_of_tz_database_time_zones
        ///
        /// For a fully custom fixed offset with no IANA ID:
        ///   TimeZoneInfo ServerTimeZone = TimeZoneInfo.CreateCustomTimeZone("custom", TimeSpan.FromHours(5.5), "UTC+5:30", "UTC+5:30");
        /// </summary>
        [DefaultValue("TimeZoneId.Japan")]
        public TimeZoneInfo ServerTimeZone
        {
            set
            {
                SetSetting("ServerTimeZone", value);
            }
            get
            {
                return TryGetSetting("ServerTimeZone", _ServerTimeZone);
            }
        }
        private static readonly TimeZoneInfo _ServerTimeZone =
            TimeZoneInfo.TryFindSystemTimeZoneById("Asia/Tokyo", out var _jst) ? _jst : TimeZoneInfo.Utc;

        /// <summary>
        /// Returns the UTC offset for the configured ServerTimeZone at the current moment.
        /// </summary>
        public TimeSpan GetEffectiveUtcOffset() => ServerTimeZone.GetUtcOffset(DateTimeOffset.UtcNow);

        /// <summary>
        /// When true, world quests that the party leader does not meet the area rank requirement for
        /// are hidden. In InstanceReset mode the slot is re-rolled with an eligible quest. In
        /// ServerReset mode the ineligible quest is simply removed without replacement.
        /// Applies to both WorldQuestSystem modes.
        /// </summary>
        [DefaultValue(_WorldQuestFilterByLeaderAreaRank)]
        public bool WorldQuestFilterByLeaderAreaRank
        {
            set
            {
                SetSetting("WorldQuestFilterByLeaderAreaRank", value);
            }
            get
            {
                return TryGetSetting("WorldQuestFilterByLeaderAreaRank", _WorldQuestFilterByLeaderAreaRank);
            }
        }
        private const bool _WorldQuestFilterByLeaderAreaRank = false;

        /// <summary>
        /// When true, world quests use a first-clear / repeat-clear reward system per period.
        /// First clear per period: full rewards (fixed, random, selectable).
        /// Repeat clears: reduced random item pool (if defined per quest) plus configurable wallet reward penalties.
        /// The WorldQuestResetTask resets first-clear records when it fires, regardless of WorldQuestSystem mode.
        /// When false, every clear gives full rewards as if it were a first clear.
        /// </summary>
        [DefaultValue(_WorldQuestFirstClearRewards)]
        public bool WorldQuestFirstClearRewards
        {
            set { SetSetting("WorldQuestFirstClearRewards", value); }
            get { return TryGetSetting("WorldQuestFirstClearRewards", _WorldQuestFirstClearRewards); }
        }
        private const bool _WorldQuestFirstClearRewards = true;

        /// <summary>
        /// EXP reward ratio for repeat world quest clears (0.0 = none, 1.0 = full).
        /// Only applies when WorldQuestFirstClearRewards = true.
        /// </summary>
        [DefaultValue(_WorldQuestRepeatClearExpPct)]
        public double WorldQuestRepeatClearExpPct
        {
            set { SetSetting("WorldQuestRepeatClearExpPct", value); }
            get { return TryGetSetting("WorldQuestRepeatClearExpPct", _WorldQuestRepeatClearExpPct); }
        }
        private const double _WorldQuestRepeatClearExpPct = 1.0;

        /// <summary>
        /// Rift Points reward ratio for repeat world quest clears (0.0 = none, 1.0 = full).
        /// Only applies when WorldQuestFirstClearRewards = true.
        /// </summary>
        [DefaultValue(_WorldQuestRepeatClearRpPct)]
        public double WorldQuestRepeatClearRpPct
        {
            set { SetSetting("WorldQuestRepeatClearRpPct", value); }
            get { return TryGetSetting("WorldQuestRepeatClearRpPct", _WorldQuestRepeatClearRpPct); }
        }
        private const double _WorldQuestRepeatClearRpPct = 1.0;

        /// <summary>
        /// Gold reward ratio for repeat world quest clears (0.0 = none, 1.0 = full).
        /// Only applies when WorldQuestFirstClearRewards = true.
        /// </summary>
        [DefaultValue(_WorldQuestRepeatClearGoldPct)]
        public double WorldQuestRepeatClearGoldPct
        {
            set { SetSetting("WorldQuestRepeatClearGoldPct", value); }
            get { return TryGetSetting("WorldQuestRepeatClearGoldPct", _WorldQuestRepeatClearGoldPct); }
        }
        private const double _WorldQuestRepeatClearGoldPct = 1.0;

        /// <summary>
        /// Job Points reward ratio for repeat world quest clears (0.0 = none, 1.0 = full).
        /// Only applies when WorldQuestFirstClearRewards = true.
        /// </summary>
        [DefaultValue(_WorldQuestRepeatClearJpPct)]
        public double WorldQuestRepeatClearJpPct
        {
            set { SetSetting("WorldQuestRepeatClearJpPct", value); }
            get { return TryGetSetting("WorldQuestRepeatClearJpPct", _WorldQuestRepeatClearJpPct); }
        }
        private const double _WorldQuestRepeatClearJpPct = 1.0;

        /// <summary>
        /// When enabled, items dropped by players are stored in persistent public supply caches instead of disappearing.
        /// </summary>
        [DefaultValue(_SupplyCachesEnabled)]
        public bool SupplyCachesEnabled
        {
            set => SetSetting("SupplyCachesEnabled", value);
            get => TryGetSetting("SupplyCachesEnabled", _SupplyCachesEnabled);
        }
        private const bool _SupplyCachesEnabled = true;

        /// <summary>
        /// Radius in meters to search for an existing supply cache when dropping an item.
        /// </summary>
        [DefaultValue(_SupplyCacheMergeRadius)]
        public double SupplyCacheMergeRadius
        {
            set => SetSetting("SupplyCacheMergeRadius", value);
            get => TryGetSetting("SupplyCacheMergeRadius", _SupplyCacheMergeRadius);
        }
        private const double _SupplyCacheMergeRadius = 8.0;

        /// <summary>
        /// Minimum spawn distance from the dropping player in meters.
        /// </summary>
        [DefaultValue(_SupplyCacheSpawnRadiusMin)]
        public double SupplyCacheSpawnRadiusMin
        {
            set => SetSetting("SupplyCacheSpawnRadiusMin", value);
            get => TryGetSetting("SupplyCacheSpawnRadiusMin", _SupplyCacheSpawnRadiusMin);
        }
        private const double _SupplyCacheSpawnRadiusMin = 0.0;

        /// <summary>
        /// Maximum spawn distance from the dropping player in meters.
        /// </summary>
        [DefaultValue(_SupplyCacheSpawnRadiusMax)]
        public double SupplyCacheSpawnRadiusMax
        {
            set => SetSetting("SupplyCacheSpawnRadiusMax", value);
            get => TryGetSetting("SupplyCacheSpawnRadiusMax", _SupplyCacheSpawnRadiusMax);
        }
        private const double _SupplyCacheSpawnRadiusMax = 0.0;

        /// <summary>
        /// Maximum number of item stacks stored in a single supply cache.
        /// </summary>
        [DefaultValue(_SupplyCacheMaxItemsPerCache)]
        public uint SupplyCacheMaxItemsPerCache
        {
            set => SetSetting("SupplyCacheMaxItemsPerCache", value);
            get => TryGetSetting("SupplyCacheMaxItemsPerCache", _SupplyCacheMaxItemsPerCache);
        }
        private const uint _SupplyCacheMaxItemsPerCache = 200;

        /// <summary>
        /// Maximum number of supply caches allowed on a single map.
        /// </summary>
        [DefaultValue(_SupplyCacheMaxCachesPerMap)]
        public uint SupplyCacheMaxCachesPerMap
        {
            set => SetSetting("SupplyCacheMaxCachesPerMap", value);
            get => TryGetSetting("SupplyCacheMaxCachesPerMap", _SupplyCacheMaxCachesPerMap);
        }
        private const uint _SupplyCacheMaxCachesPerMap = 500;

        /// <summary>
        /// Maximum number of supply caches allowed server-wide.
        /// </summary>
        [DefaultValue(_SupplyCacheMaxCachesServerWide)]
        public uint SupplyCacheMaxCachesServerWide
        {
            set => SetSetting("SupplyCacheMaxCachesServerWide", value);
            get => TryGetSetting("SupplyCacheMaxCachesServerWide", _SupplyCacheMaxCachesServerWide);
        }
        private const uint _SupplyCacheMaxCachesServerWide = 10000;

        /// <summary>
        /// Number of placement attempts before spawning beside the player.
        /// </summary>
        [DefaultValue(_SupplyCacheSpawnRetries)]
        public int SupplyCacheSpawnRetries
        {
            set => SetSetting("SupplyCacheSpawnRetries", value);
            get => TryGetSetting("SupplyCacheSpawnRetries", _SupplyCacheSpawnRetries);
        }
        private const int _SupplyCacheSpawnRetries = 10;

        /// <summary>
        /// Load supply caches from the database when the game server starts.
        /// </summary>
        [DefaultValue(_SupplyCachePersistAcrossRestart)]
        public bool SupplyCachePersistAcrossRestart
        {
            set => SetSetting("SupplyCachePersistAcrossRestart", value);
            get => TryGetSetting("SupplyCachePersistAcrossRestart", _SupplyCachePersistAcrossRestart);
        }
        private const bool _SupplyCachePersistAcrossRestart = true;

        /// <summary>
        /// Run scheduled removal of supply caches that exceeded SupplyCacheLifetimeDays.
        /// </summary>
        [DefaultValue(_SupplyCacheCleanupWeekly)]
        public bool SupplyCacheCleanupWeekly
        {
            set => SetSetting("SupplyCacheCleanupWeekly", value);
            get => TryGetSetting("SupplyCacheCleanupWeekly", _SupplyCacheCleanupWeekly);
        }
        private const bool _SupplyCacheCleanupWeekly = true;

        /// <summary>
        /// Days after creation before a supply cache is removed. Set to 0 to disable expiry.
        /// </summary>
        [DefaultValue(_SupplyCacheLifetimeDays)]
        public uint SupplyCacheLifetimeDays
        {
            set => SetSetting("SupplyCacheLifetimeDays", value);
            get => TryGetSetting("SupplyCacheLifetimeDays", _SupplyCacheLifetimeDays);
        }
        private const uint _SupplyCacheLifetimeDays = 7;

        /// <summary>
        /// Allow quest key items to be placed into supply caches.
        /// </summary>
        [DefaultValue(_SupplyCacheAllowQuestItems)]
        public bool SupplyCacheAllowQuestItems
        {
            set => SetSetting("SupplyCacheAllowQuestItems", value);
            get => TryGetSetting("SupplyCacheAllowQuestItems", _SupplyCacheAllowQuestItems);
        }
        private const bool _SupplyCacheAllowQuestItems = false;

        /// <summary>
        /// Emit [SUPPLY_CACHE_DIAG] lines to the server log when discarding / polling drop lists.
        /// </summary>
        [DefaultValue(_SupplyCacheDiagnosticsEnabled)]
        public bool SupplyCacheDiagnosticsEnabled
        {
            set => SetSetting("SupplyCacheDiagnosticsEnabled", value);
            get => TryGetSetting("SupplyCacheDiagnosticsEnabled", _SupplyCacheDiagnosticsEnabled);
        }
        private const bool _SupplyCacheDiagnosticsEnabled = true;
    }
}
