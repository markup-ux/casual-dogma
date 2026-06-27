// Official pawn tuning used by OfficialPawnModule.
// Crest pools are grouped by player level and equipment category.
// Crest ids use the same ItemId values applied at the crafting bench.

// Weapon crests

var WeaponCrestsLow = new List<uint>()
{
    // Base enemy-type crests.
    (uint)ItemId.CrestOfBeastHunting,
    (uint)ItemId.CrestOfDemonSlaying,
    (uint)ItemId.CrestOfManslaying,
    (uint)ItemId.CrestOfMassacre,
    (uint)ItemId.CrestOfMutilation,
    (uint)ItemId.CrestOfExorcism,
    (uint)ItemId.CrestOfGiantKilling,
    (uint)ItemId.CrestOfFleshCutting,

    // Early elemental damage crests.
    (uint)ItemId.CrestOfBurning0,
    (uint)ItemId.CrestOfElectrocution0,
    (uint)ItemId.CrestOfCrystallization0,
    (uint)ItemId.CrestOfDrenching0,
    (uint)ItemId.CrestOfDrowning0,

    // Early debuff crests.
    (uint)ItemId.CrestOfBlinding0,
    (uint)ItemId.CrestOfDeeperSleep0,
    (uint)ItemId.CrestOfDazzling0,
    (uint)ItemId.CrestOfFatalPoison0,

    // World quest rewards, levels 1-30.
    (uint)ItemId.CrestOfPoison0,
    (uint)ItemId.CrestOfTorpor0,
    (uint)ItemId.CrestOfDecreasedIceResist,
};

var WeaponCrestsMid = new List<uint>()
{
    // Tier II enemy-type crests.
    (uint)ItemId.CrestOfBeastHuntingII,
    (uint)ItemId.CrestOfDemonSlayingII,
    (uint)ItemId.CrestOfManslayingII,
    (uint)ItemId.CrestOfMassacreII,
    (uint)ItemId.CrestOfMutilationII,
    (uint)ItemId.CrestOfExorcismII,
    (uint)ItemId.CrestOfGiantKillingII,
    (uint)ItemId.CrestOfFleshCuttingII,

    // Elemental damage and stat debuffs.
    (uint)ItemId.CrestOfBurning0,
    (uint)ItemId.CrestOfElectrocution0,
    (uint)ItemId.CrestOfDwindledDefense0,
    (uint)ItemId.CrestOfDwindledStrength0,
    (uint)ItemId.CrestOfDwindledMagick0,
    (uint)ItemId.CrestOfDwindledMagickDefense0,

    // World quest rewards, levels 31-60.
    (uint)ItemId.CrestOfGreaterPower0,
    (uint)ItemId.CrestOfSheerPower0,
    (uint)ItemId.CrestOfHerculeanPower0,
    (uint)ItemId.CrestOfGreaterMagick0,
    (uint)ItemId.CrestOfSheerMagick0,
    (uint)ItemId.CrestOfSuperiorMagick0,
    (uint)ItemId.CrestOfDazzling0,
    (uint)ItemId.CrestOfDragonsbane,
    (uint)ItemId.CrestOfDragonsbaneII,
    (uint)ItemId.CrestOfGreaterTorpor0,
    (uint)ItemId.CrestOfDeeperSleep0,
    (uint)ItemId.CrestOfDrowning0,
    (uint)ItemId.CrestOfSealing0,
    (uint)ItemId.CrestOfPetrification0,
    (uint)ItemId.CrestOfCrystallization0,
    (uint)ItemId.CrestOfIncineration0,
    (uint)ItemId.CrestOfFreezing0,
    (uint)ItemId.CrestOfPermafrost0,
    (uint)ItemId.CrestOfElectrocution0,
    (uint)ItemId.CrestOfDiminishedFireResist,
    (uint)ItemId.CrestOfDiminishedIceResist,
    (uint)ItemId.CrestOfDiminishedLightningResist,
    (uint)ItemId.CrestOfDwindledStrength0,
    (uint)ItemId.CrestOfDwindledMagick0,
    (uint)ItemId.CrestOfPower1,
    (uint)ItemId.CrestOfMagick1,
    (uint)ItemId.CrestOfLoweredDefense1,
    (uint)ItemId.CrestOfLoweredMagickDefense1,
    (uint)ItemId.CrestOfLoweredStrength1,
    (uint)ItemId.CrestOfLoweredMagick1,
};

var WeaponCrestsHigh = new List<uint>()
{
    // Tier III enemy-type crests.
    (uint)ItemId.CrestOfBeastHuntingIII,
    (uint)ItemId.CrestOfDemonSlayingIII,
    (uint)ItemId.CrestOfManslayingIII,
    (uint)ItemId.CrestOfMassacreIII,
    (uint)ItemId.CrestOfMutilationIII,
    (uint)ItemId.CrestOfExorcismIII,
    (uint)ItemId.CrestOfGiantKillingIII,
    (uint)ItemId.CrestOfFleshCuttingIII,
    (uint)ItemId.CrestOfDragonsbaneIII,

    // Higher elemental damage crests.
    (uint)ItemId.CrestOfBurning1,
    (uint)ItemId.CrestOfElectrocution1,
    (uint)ItemId.CrestOfBlinding1,
    (uint)ItemId.CrestOfDwindledDefense1,
    (uint)ItemId.CrestOfDwindledStrength1,

    // World quest rewards, levels 61-90.
    (uint)ItemId.CrestOfGreaterPower0,
    (uint)ItemId.CrestOfHerculeanPower0,
    (uint)ItemId.CrestOfSuperiorMagick0,
    (uint)ItemId.CrestOfIncineration0,
    (uint)ItemId.CrestOfShock0,
    (uint)ItemId.CrestOfElectrocution0,
    (uint)ItemId.CrestOfSanctity0,
    (uint)ItemId.CrestOfTwilight0,
    (uint)ItemId.CrestOfDiminishedFireResist,
    (uint)ItemId.CrestOfDiminishedIceResist,
    (uint)ItemId.CrestOfDiminishedLightningResist,
    (uint)ItemId.CrestOfDiminishedHolyResist,
    (uint)ItemId.CrestOfDiminishedDarkResist,
    (uint)ItemId.CrestOfDwindledDefense0,
    (uint)ItemId.CrestOfDwindledMagickDefense0,
    (uint)ItemId.CrestOfPower1,
    (uint)ItemId.CrestOfMagick1,
    (uint)ItemId.CrestOfBurning1,
    (uint)ItemId.CrestOfFreezing1,
    (uint)ItemId.CrestOfHolyDrain1,
    (uint)ItemId.CrestOfGreaterPower1,
    (uint)ItemId.CrestOfGreaterMagick1,
    (uint)ItemId.CrestOfHerculeanPowerBurning0,
    (uint)ItemId.CrestOfHerculeanPowerFreezing0,
    (uint)ItemId.CrestOfHerculeanPowerHolyDrain0,
    (uint)ItemId.CrestOfSuperiorMagickBurning0,
    (uint)ItemId.CrestOfSuperiorMagickFreezing0,
    (uint)ItemId.CrestOfSuperiorMagickHolyDrain0,
    (uint)ItemId.CrestOfHerculeanPowerStun0,
    (uint)ItemId.CrestOfHerculeanPowerLoweredDefense0,
    (uint)ItemId.CrestOfSuperiorMagickLoweredMagickDefense0,
    (uint)ItemId.CrestOfIncinerationDecreasedFireResist0,
    (uint)ItemId.CrestOfPermafrostDecreasedIceResist0,
    (uint)ItemId.CrestOfElectrocutionDecreasedLightningResist0,
    (uint)ItemId.CrestOfSanctityDecreasedHolyResist0,
    (uint)ItemId.CrestOfTwilightDecreasedDarkResist0,
    (uint)ItemId.CrestOfPermafrost1,
};

var WeaponCrestsMax = new List<uint>()
{
    // Tier IV enemy-type crests.
    (uint)ItemId.CrestOfBeastHuntingIV,
    (uint)ItemId.CrestOfDemonSlayingIV,
    (uint)ItemId.CrestOfManslayingIV,
    (uint)ItemId.CrestOfMassacreIV,
    (uint)ItemId.CrestOfMutilationIV,
    (uint)ItemId.CrestOfExorcismIV,
    (uint)ItemId.CrestOfGiantKillingIV,
    (uint)ItemId.CrestOfFleshCuttingIV,
    (uint)ItemId.CrestOfDragonsbaneIV,

    // High-rank elemental and debuff crests.
    (uint)ItemId.CrestOfBurning1,
    (uint)ItemId.CrestOfElectrocution1,
    (uint)ItemId.CrestOfDwindledDefense1,
    (uint)ItemId.CrestOfDwindledMagick1,
    (uint)ItemId.CrestOfDwindledStrength1,
    (uint)ItemId.CrestOfDwindledMagickDefense1,
    (uint)ItemId.CrestOfAmplifiedAttack1,

    // World quest rewards, levels 91+.
    (uint)ItemId.CrestOfGreaterPower0,
    (uint)ItemId.CrestOfHerculeanPowerBurning0,
    (uint)ItemId.CrestOfSuperiorMagickBurning0,
    (uint)ItemId.CrestOfHerculeanPowerBurning1,
    (uint)ItemId.CrestOfSuperiorMagickBurning1,
};

// Armor crests

var ArmorCrestsLow = new List<uint>()
{
    // Early elemental resistance crests.
    (uint)ItemId.CrestOfFireWarding0,
    (uint)ItemId.CrestOfColdWarding0,
    (uint)ItemId.CrestOfElectricityWarding0,
    (uint)ItemId.CrestOfDarkWarding0,

    // Base status prevention crests.
    (uint)ItemId.CrestOfBurnPrevention,
    (uint)ItemId.CrestOfBlindPrevention,
    (uint)ItemId.CrestOfDrenchPrevention0,

    // General protection crests.
    (uint)ItemId.CrestOfFirmProtection0,
    (uint)ItemId.CrestOfExpulsion,
    (uint)ItemId.CrestOfPurification,
    (uint)ItemId.CrestOfDismantling,
};

var ArmorCrestsMid = new List<uint>()
{
    // Higher elemental resistance crests.
    (uint)ItemId.CrestOfFireWarding1,
    (uint)ItemId.CrestOfColdWarding1,
    (uint)ItemId.CrestOfElectricityWarding1,
    (uint)ItemId.CrestOfDarkWarding1,

    // Status warding crests.
    (uint)ItemId.CrestOfBurnWarding0,
    (uint)ItemId.CrestOfBlindWarding0,
    (uint)ItemId.CrestOfDrenchWarding0,
    (uint)ItemId.CrestOfCurseWarding0,

    // Enemy-type resistance and general protection.
    (uint)ItemId.CrestOfExpulsionII,
    (uint)ItemId.CrestOfPurificationII,
    (uint)ItemId.CrestOfDismantlingII,
    (uint)ItemId.CrestOfFirmProtection0,

    // World quest rewards, levels 31-60.
    (uint)ItemId.CrestOfUnburdening0,
    (uint)ItemId.CrestOfUnburdening1,
    (uint)ItemId.CrestOfGreaterProtection0,
    (uint)ItemId.CrestOfSheerProtection0,
    (uint)ItemId.CrestOfGreaterIntelligence0,
    (uint)ItemId.CrestOfSheerIntelligence0,
    (uint)ItemId.CrestOfWisdom0,
    (uint)ItemId.CrestOfPerseverance0,
    (uint)ItemId.CrestOfStubbornPerseverance0,
    (uint)ItemId.CrestOfRestoration0,
    (uint)ItemId.CrestOfFortitude0,
    (uint)ItemId.CrestOfIceWarding0,
    (uint)ItemId.CrestOfFreezeWarding0,
    (uint)ItemId.CrestOfFreezePrevention,
    (uint)ItemId.CrestOfShockWarding0,
    (uint)ItemId.CrestOfHolyDrainWarding0,
    (uint)ItemId.CrestOfHolyDrainPrevention,
    (uint)ItemId.CrestOfSleepWarding0,
    (uint)ItemId.CrestOfSleepPrevention0,
    (uint)ItemId.CrestOfStunningWarding0,
    (uint)ItemId.CrestOfStunningPrevention0,
    (uint)ItemId.CrestOfTarPrevention0,
    (uint)ItemId.CrestOfStifleWarding0,
    (uint)ItemId.CrestOfStiflePrevention0,
    (uint)ItemId.CrestOfCursePrevention0,
    (uint)ItemId.CrestOfPetrificationWarding0,
    (uint)ItemId.CrestOfPetrificationPrevention0,
    (uint)ItemId.CrestOfProtection1,
    (uint)ItemId.CrestOfIntelligence1,
    (uint)ItemId.CrestOfPerseverance1,
    (uint)ItemId.CrestOfRestoration1,
};

var ArmorCrestsHigh = new List<uint>()
{
    // High-tier elemental resistance.
    (uint)ItemId.CrestOfFireProtection0,
    (uint)ItemId.CrestOfDarkProtection0,

    // Higher status warding crests.
    (uint)ItemId.CrestOfBurnWarding1,
    (uint)ItemId.CrestOfBlindWarding1,
    (uint)ItemId.CrestOfDrenchWarding1,
    (uint)ItemId.CrestOfCurseWarding1,

    // Status immunity crests.
    (uint)ItemId.CrestOfBurnImmunity0,
    (uint)ItemId.CrestOfBlindImmunity0,

    // General protection crests.
    (uint)ItemId.CrestOfExpulsionIII,
    (uint)ItemId.CrestOfPurificationIII,
    (uint)ItemId.CrestOfDismantlingIII,
    (uint)ItemId.CrestOfDragonsbaneIII,
    (uint)ItemId.CrestOfFirmProtection1,

    // World quest rewards, levels 61-90.
    (uint)ItemId.CrestOfLightness0,
    (uint)ItemId.CrestOfShockImmunity0,
    (uint)ItemId.CrestOfHolyDrainWarding0,
    (uint)ItemId.CrestOfHolyDrainPrevention,
    (uint)ItemId.CrestOfFirmProtectionHealth0,
    (uint)ItemId.CrestOfFirmProtectionEndurance0,
    (uint)ItemId.CrestOfFirmProtectionVitality0,
    (uint)ItemId.CrestOfFirmProtectionLightness0,
    (uint)ItemId.CrestOfWisdomHealth0,
    (uint)ItemId.CrestOfWisdomEndurance0,
    (uint)ItemId.CrestOfWisdomVitality0,
    (uint)ItemId.CrestOfWisdomLightness0,
    (uint)ItemId.CrestOfFireBarrier,
    (uint)ItemId.CrestOfIceBarrier,
    (uint)ItemId.CrestOfDoubleResistPoisonTorpor,
    (uint)ItemId.CrestOfDoubleResistSleepStun,
    (uint)ItemId.CrestOfDoubleResistPetrifyGild,
    (uint)ItemId.CrestOfDoubleResistCurseStifle,
    (uint)ItemId.CrestOfDoubleResistLoweredStrengthDefense0,
    (uint)ItemId.CrestOfDoubleResistLoweredMagickMagickDefense0,
    (uint)ItemId.CrestOfFireProtection0,
    (uint)ItemId.CrestOfIceProtection0,
};

var ArmorCrestsMax = new List<uint>()
{
    // Endgame immunity and high protection.
    (uint)ItemId.CrestOfBurnImmunity1,
    (uint)ItemId.CrestOfBlindImmunity1,
    (uint)ItemId.CrestOfFireProtection1,
    (uint)ItemId.CrestOfDarkProtection1,

    // Endgame double-resist crests.
    (uint)ItemId.CrestOfDoubleResistPoisonTorpor,
    (uint)ItemId.CrestOfDoubleResistSleepStun,
    (uint)ItemId.CrestOfDoubleResistPetrifyGild,
    (uint)ItemId.CrestOfDoubleResistCurseStifle,
    (uint)ItemId.CrestOfDoubleResistLoweredStrengthDefense0,
    (uint)ItemId.CrestOfDoubleResistLoweredMagickMagickDefense0,

    // General endgame crests.
    (uint)ItemId.CrestOfExpulsionIV,
    (uint)ItemId.CrestOfPurificationIV,
    (uint)ItemId.CrestOfDismantlingIV,
    (uint)ItemId.CrestOfDragonsbaneIV,
    (uint)ItemId.CrestOfFirmProtection1,
    (uint)ItemId.CrestOfFirmProtectionHealth0,
    (uint)ItemId.CrestOfFirmProtectionEndurance0,

    // World quest rewards, levels 91+.
    (uint)ItemId.CrestOfShockImmunity0,
    (uint)ItemId.CrestOfHolyDrainWarding0,
};

// Jewelry crests
// Bitterblack Maze jewelry uses its own appraisal crest rolls.

var JewelryCrestsLow  = ArmorCrestsLow;
var JewelryCrestsMid  = ArmorCrestsMid;
var JewelryCrestsHigh = ArmorCrestsHigh;
var JewelryCrestsMax  = ArmorCrestsMax;

// Player level at which each tier begins.
// Adjust these if your server's quest level curve differs.
var CrestTierMidMinLevel  = 31;
var CrestTierHighMinLevel = 61;
var CrestTierMaxMinLevel  = 91;

// Player level at which official pawns may receive automatic limit break rolls.
// Limit break existed later in normal progression, so keep this high enough that
// early rentals do not feel overbuilt.
var LimitBreakUnlockLevel = 20;

// Player level at which automatic limit break becomes common for good+ pawns.
// This roughly maps to Season 3 / late-game progression.
var LimitBreakLateGameMinLevel = 85;

// Names must match the "name" values in LimitBreak.json.
// Each job can choose a preferred pool for weapons and armor. The generator still
// sometimes rolls outside these pools at lower quality, but good+ pawns tend to
// stay inside their configured preferences.

var LimitBreakWeaponStatsPhysical = new List<string>()
{
    "Blow Power",
    "Knockout Power",
    "Inflict Torpor",
    "Inflict Poison",
    "Inflict Physical Def. Down",
    "Inflict Ice Resist Down",
};

var LimitBreakWeaponStatsMagick = new List<string>()
{
    "Inflict Torpor",
    "Inflict Sleep",
    "Inflict Poison",
    "Inflict Magick Def. Down",
    "Inflict Dark Resist Down",
    "Inflict Ice Resist Down",
};

var LimitBreakArmorStatsHealingFirst = new List<string>()
{
    "Healing Power",
    "Endurance",
    "Resist Knockout",
    "Resist Torpor",
    "Resist Sleep",
};

var PreferredWeaponLimitBreakStats = new Dictionary<JobId, List<string>>()
{
    [JobId.Fighter]        = LimitBreakWeaponStatsPhysical,
    [JobId.Hunter]         = LimitBreakWeaponStatsPhysical,
    [JobId.Seeker]         = LimitBreakWeaponStatsPhysical,
    [JobId.Warrior]        = LimitBreakWeaponStatsPhysical,
    [JobId.Priest]         = LimitBreakWeaponStatsMagick,
    [JobId.ShieldSage]     = LimitBreakWeaponStatsMagick,
    [JobId.Sorcerer]       = LimitBreakWeaponStatsMagick,
    [JobId.ElementArcher]  = LimitBreakWeaponStatsMagick,
    [JobId.Alchemist]      = LimitBreakWeaponStatsMagick,
    [JobId.SpiritLancer]   = LimitBreakWeaponStatsMagick,
    [JobId.HighScepter]    = LimitBreakWeaponStatsMagick,
};

var PreferredArmorLimitBreakStats = new Dictionary<JobId, List<string>>()
{
    [JobId.Fighter]        = LimitBreakArmorStatsHealingFirst,
    [JobId.Hunter]         = LimitBreakArmorStatsHealingFirst,
    [JobId.Seeker]         = LimitBreakArmorStatsHealingFirst,
    [JobId.Warrior]        = LimitBreakArmorStatsHealingFirst,
    [JobId.Priest]         = LimitBreakArmorStatsHealingFirst,
    [JobId.ShieldSage]     = LimitBreakArmorStatsHealingFirst,
    [JobId.Sorcerer]       = LimitBreakArmorStatsHealingFirst,
    [JobId.ElementArcher]  = LimitBreakArmorStatsHealingFirst,
    [JobId.Alchemist]      = LimitBreakArmorStatsHealingFirst,
    [JobId.SpiritLancer]   = LimitBreakArmorStatsHealingFirst,
    [JobId.HighScepter]    = LimitBreakArmorStatsHealingFirst,
};

// Skills listed first in each job's array are preferred when filling the pawn's
// four skill slots. Remaining slots fall back to the job's base skill set in
// enum order. Edit these to change which skills official pawns of each job prioritise.
// Jobs not listed here fall back entirely to the builder's enum-order default.
//
// Only base (non-promoted) CustomSkillId values are valid for auto-selection.
// Use WithCustomSkill() in an official pawn script for promoted variants.

var RecommendedCustomSkills = new Dictionary<JobId, List<CustomSkillId>>()
{
    [JobId.Fighter] = new List<CustomSkillId>()
    {
        CustomSkillId.BlinkStrike,
        CustomSkillId.TuskToss,
        CustomSkillId.SkywardLash,
        CustomSkillId.HindsightSlash,
    },
    [JobId.Seeker] = new List<CustomSkillId>()
    {
        CustomSkillId.BitingWind,
        CustomSkillId.TossAndTrigger,
        CustomSkillId.BackKick,
        CustomSkillId.FalconKick,
    },
    [JobId.Hunter] = new List<CustomSkillId>()
    {
        CustomSkillId.ThreefoldArrow,
        CustomSkillId.CloudburstVolley,
        CustomSkillId.WhirlingArrow,
        CustomSkillId.StormArrow,
    },
    [JobId.Priest] = new List<CustomSkillId>()
    {
        CustomSkillId.EnergySpot,
        CustomSkillId.SeraphimFlap,
        CustomSkillId.GuardBit,
        CustomSkillId.AttackRiser,
        CustomSkillId.CuringSpot,
        CustomSkillId.QuickCharge,
    },
    [JobId.ShieldSage] = new List<CustomSkillId>()
    {
        CustomSkillId.ForceShield,
        CustomSkillId.HolyWall,
        CustomSkillId.EarthShake,
        CustomSkillId.RampartRaid,
    },
    [JobId.Sorcerer] = new List<CustomSkillId>()
    {
        CustomSkillId.BlackHaze,
        CustomSkillId.Frigor,
        CustomSkillId.Comestion,
        CustomSkillId.Levin,
        CustomSkillId.DarknessMist,
    },
    [JobId.Warrior] = new List<CustomSkillId>()
    {
        CustomSkillId.SparkSlash,
        CustomSkillId.HeavenThrust,
        CustomSkillId.Clarity,
        CustomSkillId.GreatWindmill,
        CustomSkillId.PommelStrike,
    },
    [JobId.Alchemist] = new List<CustomSkillId>()
    {
        CustomSkillId.RexElementa,
        CustomSkillId.DolusMorsus,
        CustomSkillId.GoldaAurum,
        CustomSkillId.AlmaSector,
        CustomSkillId.AlmaWave,
    },
    [JobId.SpiritLancer] = new List<CustomSkillId>()
    {
        CustomSkillId.WallGlasta,
        CustomSkillId.EadromCounter,
        CustomSkillId.CorrMeteor,
        CustomSkillId.ScriosBlast,
        CustomSkillId.ScriosGuard,
    },
    [JobId.ElementArcher] = new List<CustomSkillId>()
    {
        CustomSkillId.HealingBolt,
        CustomSkillId.EnergizingBolt,
        CustomSkillId.CuringBolt,
        CustomSkillId.FlamingBow,
        CustomSkillId.MagickalFlare,
        CustomSkillId.FourfoldBolt,
    },
    [JobId.HighScepter] = new List<CustomSkillId>()
    {
        CustomSkillId.MirageShift,
        CustomSkillId.BlackFlashFang,
        CustomSkillId.WallBarrier,
        CustomSkillId.TerrorBlast,
        CustomSkillId.EclipseBright,
        CustomSkillId.FullMoonLight,
    },
};

// Abilities listed first are preferred. The builder fills slots within the
// ability cost budget, so higher-cost abilities should go near the top if they
// are the priority. Remaining budget is filled from the job's full ability list.

var RecommendedAbilities = new Dictionary<JobId, List<AbilityId>>()
{
    [JobId.Fighter] = new List<AbilityId>()
    {
        AbilityId.AgileMotion,
        AbilityId.CombatMomentum,
        AbilityId.BraveEffort,
        AbilityId.Onslaught,
        AbilityId.StrongShield,
        AbilityId.DeftFooting,
    },
    [JobId.Seeker] = new List<AbilityId>()
    {
        AbilityId.AgileMotion,
        AbilityId.ShadowAttack,
        AbilityId.Precision,
        AbilityId.Concentration,
        AbilityId.DeftFooting,
    },
    [JobId.Hunter] = new List<AbilityId>()
    {
        AbilityId.Precision,
        AbilityId.Concentration,
        AbilityId.SkilledReload,
        AbilityId.AggressionArrow,
        AbilityId.ReloadForce,
        AbilityId.ArrowIncrease,
    },
    [JobId.Priest] = new List<AbilityId>()
    {
        AbilityId.DivineProtection,
        AbilityId.LongExposure,
        AbilityId.HardSpirit,
        AbilityId.SpiritOverflow,
        AbilityId.Robust,
        AbilityId.FiredUp,
        AbilityId.ElementalDefense,
    },
    [JobId.ShieldSage] = new List<AbilityId>()
    {
        AbilityId.SturdyForm,
        AbilityId.MenacingForm,
        AbilityId.ToughSkin,
        AbilityId.Hardy,
        AbilityId.SteadfastStand,
        AbilityId.Obstinacy,
    },
    [JobId.Sorcerer] = new List<AbilityId>()
    {
        AbilityId.ContinuedChant,
        AbilityId.GracefulChant,
        AbilityId.Calmness,
        AbilityId.MagickBoost,
        AbilityId.TwinMagick,
        AbilityId.FireAttack,
        AbilityId.IceAttack,
        AbilityId.ThunderAttack,
        AbilityId.DarkAttack,
    },
    [JobId.Warrior] = new List<AbilityId>()
    {
        AbilityId.Onslaught,
        AbilityId.CombatMomentum,
        AbilityId.BraveEffort,
        AbilityId.HeavyAttack,
        AbilityId.BraveAttack,
        AbilityId.FightingSpirit,
        AbilityId.DeftFooting,
    },
    [JobId.Alchemist] = new List<AbilityId>()
    {
        AbilityId.DivineProtection,
        AbilityId.ElementalDefense,
        AbilityId.EnduringVision,
        AbilityId.DeftFooting,
        AbilityId.Supercharge,
        AbilityId.Willpower,
        AbilityId.FiredUp,
        AbilityId.Robust,
    },
    [JobId.SpiritLancer] = new List<AbilityId>()
    {
        AbilityId.ElementalDefense,
        AbilityId.Robust,
        AbilityId.FiredUp,
        AbilityId.DivineProtection,
        AbilityId.HardSpirit,
        AbilityId.SpiritOverflow,
        AbilityId.Willpower,
        AbilityId.DeftFooting,
    },
    [JobId.ElementArcher] = new List<AbilityId>()
    {
        AbilityId.DivineProtection,
        AbilityId.MagickBoost,
        AbilityId.Calmness,
        AbilityId.TwinMagick,
        AbilityId.GracefulChant,
        AbilityId.ContinuedChant,
        AbilityId.ElementalDefense,
    },
    [JobId.HighScepter] = new List<AbilityId>()
    {
        AbilityId.Onslaught,
        AbilityId.CombatMomentum,
        AbilityId.BraveEffort,
        AbilityId.SourceLuck,
        AbilityId.MagickalChant,
        AbilityId.DarkAttack,
        AbilityId.VigorousSupression,
        AbilityId.CourageousSupression,
    },
};
