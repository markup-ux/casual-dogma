using Arrowgene.Ddon.Shared.Entity.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arrowgene.Ddon.Shared.Model
{
    public class SkillData
    {
        public static readonly Dictionary<JobId, uint> Em4CustomSkills = new Dictionary<JobId, uint>()
        {
            [JobId.Fighter] = CustomSkillId.BravesRaid.ReleaseId(),
            [JobId.Hunter] = CustomSkillId.DemonArrow.ReleaseId(),
            [JobId.Priest] = CustomSkillId.QuickCharge.ReleaseId(),
            [JobId.ShieldSage] = CustomSkillId.ForceAnchor.ReleaseId(),
            [JobId.Seeker] = CustomSkillId.EasyKill.ReleaseId(),
            [JobId.Sorcerer] = CustomSkillId.ProminentSphere.ReleaseId(),
            [JobId.ElementArcher] = CustomSkillId.GambleDraw.ReleaseId(),
            [JobId.Warrior] = CustomSkillId.AnnihilatorsWindSlash.ReleaseId(),
        };

        public static bool IsEm4Skill(JobId jobId, uint skillNo, uint skillLv)
        {
            if (!SkillData.Em4CustomSkills.TryGetValue(jobId, out uint value))
            {
                return false;
            }
            return (value == skillNo) && (skillLv == 1);
        }

        public static readonly Dictionary<JobId, HashSet<uint>> S2BoCustomSkill = new()
        {
            [JobId.Fighter] = [CustomSkillId.PierceSlash.ReleaseId(), CustomSkillId.FlowingShieldSpiral.ReleaseId()],
            [JobId.Hunter] = [CustomSkillId.SkyBurstShot.ReleaseId(), CustomSkillId.CombinedPierceShot.ReleaseId()],
            [JobId.Priest] = [CustomSkillId.SolaceRiser.ReleaseId(), CustomSkillId.BlastAddition.ReleaseId()],
            [JobId.ShieldSage] = [CustomSkillId.StoneLight.ReleaseId(), CustomSkillId.ProtectionSwing.ReleaseId()],
            [JobId.Seeker] = [CustomSkillId.ExplosiveFlameBlade.ReleaseId(), CustomSkillId.SoaringHawkSlash.ReleaseId()],
            [JobId.Sorcerer] = [CustomSkillId.IciclePierce.ReleaseId(), CustomSkillId.LightningStake.ReleaseId()],
            [JobId.ElementArcher] = [CustomSkillId.HealingFlash.ReleaseId(), CustomSkillId.TearingTentacleArrow.ReleaseId()],
            [JobId.Warrior] = [CustomSkillId.GreatGougingFang.ReleaseId(), CustomSkillId.EarthquakeFang.ReleaseId()],
            [JobId.Alchemist] = [CustomSkillId.AlchemicalBurst.ReleaseId(), CustomSkillId.DolusAeris.ReleaseId()],
            [JobId.SpiritLancer] = [CustomSkillId.CureGlasta.ReleaseId(), CustomSkillId.ScriosGuard.ReleaseId()],
            [JobId.HighScepter] =  [CustomSkillId.EclipseBright.ReleaseId() ],
        };

        public static bool IsS2BoSkill(JobId jobId, uint skillNo, uint skillLv)
        {
            if (!SkillData.S2BoCustomSkill.TryGetValue(jobId, out HashSet<uint> value))
            {
                return false;
            }
            return value.Contains(skillNo) && (skillLv == 1);
        }

        public static readonly Dictionary<JobId, HashSet<AbilityId>> S2BoAbility = new()
        {
            [JobId.Fighter] = [
                AbilityId.OnslaughtSlayer,
                AbilityId.DemolishingStrikeSlayer,
                AbilityId.GougeEradicate,
                AbilityId.CrushingBlow,
                AbilityId.DireOnslaughtSlayer,
                AbilityId.FirmShield,
                AbilityId.OnslaughtCrusher,
                AbilityId.DemolishingStrikeExterminator,
                AbilityId.GougeCrusher,
                AbilityId.Hardening,
                AbilityId.DireOnslaughtCrusher,
                AbilityId.PleasantThrust,
            ],
            [JobId.Hunter] = [
                AbilityId.ArcherySlayer,
                AbilityId.ExplodingArrowFury,
                AbilityId.ArrowheadStrikeFury,
                AbilityId.RigidStance,
                AbilityId.KeensightShotSlayer,
                AbilityId.RescueAssistance,
                AbilityId.ArcheryCrusher,
                AbilityId.ExplodingArrowCrusher,
                AbilityId.ArrowheadStrikeCrusher,
                AbilityId.AugmentedSpirit,
                AbilityId.KeensightCrusher,
                AbilityId.ClimaxBow,
            ],
            [JobId.Priest] = [
                AbilityId.HealingChant,
                AbilityId.BlastBitsSlayer,
                AbilityId.SaintAuraSlayer,
                AbilityId.AssistBoost,
                AbilityId.ShockwaveSlayer,
                AbilityId.Stagnation,
                AbilityId.HealAuraEndurer,
                AbilityId.BlastBitsCrusher,
                AbilityId.HolyAuraSavage,
                AbilityId.MagickalRush,
                AbilityId.ShockwaveCrusher,
                AbilityId.HardSpirit,
            ],
            [JobId.ShieldSage] = [
                AbilityId.ShieldbashSlayer,
                AbilityId.WeaklightFury,
                AbilityId.ForceBurstCrush,
                AbilityId.ProtectedMend,
                AbilityId.ShieldCounterSavage,
                AbilityId.Attention,
                AbilityId.ShieldBlowCrusher,
                AbilityId.WeakLightCrusher,
                AbilityId.ForceBurstSlayer,
                AbilityId.ShieldCollapse,
                AbilityId.ShieldSequenceCrusher,
                AbilityId.PleasantSight,
            ],
            [JobId.Seeker] = [
                AbilityId.CarveSlayer,
                AbilityId.ScarletKissesSlayer,
                AbilityId.ScarletSlashesCrush,
                AbilityId.EnduringSprint,
                AbilityId.RoundhouseKickSlayer,
                AbilityId.DeepAggression,
                AbilityId.CarveCrusher,
                AbilityId.ScarletKissesCrusher,
                AbilityId.ScarletSlashesExterminator,
                AbilityId.Stiffness,
                AbilityId.RoundhouseKickCrusher,
                AbilityId.PleasantRoll,
            ],
            [JobId.Sorcerer] = [
                AbilityId.MagickBoltSlayer,
                AbilityId.MagickTrapCrush,
                AbilityId.MagickCrackerSlayer,
                AbilityId.EnduringLevitation,
                AbilityId.MagickCrackerSmasher,
                AbilityId.CollapsingStrength,
                AbilityId.MagickBoltsCrusher,
                AbilityId.MagickTrapDuration,
                AbilityId.MagickCrackerSinger,
                AbilityId.Reduction,
                AbilityId.MagickTrapSinger,
                AbilityId.PleasantDrift,
            ],
            [JobId.ElementArcher] = [
                AbilityId.SeekerSlayer,
                AbilityId.AidArrowChant,
                AbilityId.ForwardKickSlayer,
                AbilityId.SteadyAdvance,
                AbilityId.InvigoratingArrowsDuration,
                AbilityId.DemonShield,
                AbilityId.SeekerArrowsBlink,
                AbilityId.AidArrowBlink,
                AbilityId.FrontKickCrusher,
                AbilityId.SalvationalMagick,
                AbilityId.InvigorationArrowExpand,
                AbilityId.CounterEye,
            ],
            [JobId.Warrior] = [
                AbilityId.HackSlayer,
                AbilityId.InverseSlashCrusher,
                AbilityId.SavageLashEradicate,
                AbilityId.Brandish,
                AbilityId.DevastateSlayer,
                AbilityId.AttackCover,
                AbilityId.HackCrusher,
                AbilityId.InverseSlashExterminator,
                AbilityId.SavageLashCrusher,
                AbilityId.GreatGrasp,
                AbilityId.DevastateCrusher,
                AbilityId.ExcessGrudge,
            ],
            [JobId.Alchemist] = [
                AbilityId.AlchemicStrikeSlayer,
                AbilityId.AlchemicEvadeSlayer,
                AbilityId.ElixerSlayer,
                AbilityId.EnduringVision,
                AbilityId.AlchemicalRadiusSlayer,
                AbilityId.Stubborn,
                AbilityId.AlchemyCrusher,
                AbilityId.AlchemicEvadeCrusher,
                AbilityId.ElixirCrusher,
                AbilityId.SkyAnnihilation,
                AbilityId.AlchemicalRadiusCrusher,
                AbilityId.DefenseAlchemy,
            ],
            [JobId.SpiritLancer] = [
                AbilityId.RushingSpearSlayer,
                AbilityId.RisingSpearSlayer,
                AbilityId.CrushingSpearSlayer,
                AbilityId.EnhancedVitality,
                AbilityId.SweepingSpearSlayer,
                AbilityId.ElementalDefense,
                AbilityId.RushingSpearDestroyer,
                AbilityId.RisingSpearDestroyer,
                AbilityId.CrushingSpearDestroyer,
                AbilityId.GreatEnchantment,
                AbilityId.SweepingSpearDestroyer,
                AbilityId.SpiritHoard,
            ],
            [JobId.HighScepter] = [
                AbilityId.QuadrupleSlashSlayer,
                AbilityId.ArcSlashSlayer,
                AbilityId.SkySlashSlayer,
                AbilityId.FallingSlashSlayer,
                AbilityId.OrdinaryAttack,
                AbilityId.Respiration,
            ]
        };

        public static bool IsS2BoAbility(JobId jobId, AbilityId abilityNo, uint abilityLv)
        {
            if (!SkillData.S2BoAbility.TryGetValue(jobId, out var value))
            {
                return false;
            }
            return value.Contains(abilityNo) && (abilityLv == 1);
        }

        public static bool IsS3HoSkill(JobId jobId, uint skillNo, uint skillLv)
        {
            if (!SkillData.S3HoSkill.TryGetValue(jobId, out HashSet<uint> value))
            {
                return false;
            }
            return value.Contains(skillNo) && (skillLv == 1);
        }

        public static readonly Dictionary<JobId, HashSet<uint>> S3HoSkill = new Dictionary<JobId, HashSet<uint>>()
        {
            [JobId.Fighter] = new()
            {
                CustomSkillId.TuskTossP.ReleaseId(),
                CustomSkillId.TuskTossT.ReleaseId(),
                CustomSkillId.CymbalAttackP.ReleaseId(),
                CustomSkillId.CymbalAttackT.ReleaseId(),
                CustomSkillId.SkywardLashP.ReleaseId(),
                CustomSkillId.SkywardLashT.ReleaseId(),
                CustomSkillId.DownthrustP.ReleaseId(),
                CustomSkillId.DownthrustT.ReleaseId(),
            },
            [JobId.Hunter] = new()
            {
                CustomSkillId.ThreefoldArrowP.ReleaseId(),
                CustomSkillId.ThreefoldArrowT.ReleaseId(),
                CustomSkillId.WhirlingArrowP.ReleaseId(),
                CustomSkillId.WhirlingArrowT.ReleaseId(),
                CustomSkillId.FullBendP.ReleaseId(),
                CustomSkillId.FullBendT.ReleaseId(),
                CustomSkillId.ExplosiveArrowVolleyP.ReleaseId(),
                CustomSkillId.ExplosiveArrowVolleyT.ReleaseId(),
            },
            [JobId.Priest] = new()
            {
                CustomSkillId.SolaceRiserP.ReleaseId(),
                CustomSkillId.SolaceRiserT.ReleaseId(),
                CustomSkillId.SeraphimFlapP.ReleaseId(),
                CustomSkillId.SeraphimFlapT.ReleaseId(),
                CustomSkillId.CuringSpotT.ReleaseId(),
                CustomSkillId.CuringSpotP.ReleaseId(),
                CustomSkillId.DefenseRiserP.ReleaseId(),
                CustomSkillId.DefenseRiserT.ReleaseId(),
            },
            [JobId.ShieldSage] = new()
            {
                CustomSkillId.EarthShakeP.ReleaseId(),
                CustomSkillId.EarthShakeT.ReleaseId(),
                CustomSkillId.ForceShieldP.ReleaseId(),
                CustomSkillId.ForceShieldT.ReleaseId(),
                CustomSkillId.RampartRaidP.ReleaseId(),
                CustomSkillId.RampartRaidT.ReleaseId(),
                CustomSkillId.HolyWallP.ReleaseId(),
                CustomSkillId.HolyWallT.ReleaseId(),
            },
            [JobId.Seeker] = new()
            {
                CustomSkillId.ExplosiveFlameBladeP.ReleaseId(),
                CustomSkillId.ExplosiveFlameBladeT.ReleaseId(),
                CustomSkillId.EasyKillP.ReleaseId(),
                CustomSkillId.EasyKillT.ReleaseId(),
                CustomSkillId.SteppingStoneP.ReleaseId(),
                CustomSkillId.SteppingStoneT.ReleaseId(),
                CustomSkillId.TossAndTriggerP.ReleaseId(),
                CustomSkillId.TossAndTriggerT.ReleaseId(),
            },
            [JobId.Sorcerer] = new()
            {
                CustomSkillId.DarknessMistP.ReleaseId(),
                CustomSkillId.DarknessMistT.ReleaseId(),
                CustomSkillId.FulminationP.ReleaseId(),
                CustomSkillId.FulminationT.ReleaseId(),
                CustomSkillId.ComestionP.ReleaseId(),
                CustomSkillId.ComestionT.ReleaseId(),
                CustomSkillId.FrigorP.ReleaseId(),
                CustomSkillId.FrigorT.ReleaseId(),
            },
            [JobId.ElementArcher] = new()
            {
                CustomSkillId.FlamingBowP.ReleaseId(),
                CustomSkillId.FlamingBowT.ReleaseId(),
                CustomSkillId.ExhaustingBowP.ReleaseId(),
                CustomSkillId.ExhaustingBowT.ReleaseId(),
                CustomSkillId.HealingFlashP.ReleaseId(),
                CustomSkillId.HealingFlashT.ReleaseId(),
                CustomSkillId.CripplingBowP.ReleaseId(),
                CustomSkillId.CripplingBowT.ReleaseId(),
            },
            [JobId.Warrior] = new()
            {
                CustomSkillId.SavageLungeP.ReleaseId(),
                CustomSkillId.SavageLungeT.ReleaseId(),
                CustomSkillId.PommelStrikeP.ReleaseId(),
                CustomSkillId.PommelStrikeT.ReleaseId(),
                CustomSkillId.EscapeSlashP.ReleaseId(),
                CustomSkillId.EscapeSlashT.ReleaseId(),
                CustomSkillId.SparkSlashP.ReleaseId(),
                CustomSkillId.SparkSlashT.ReleaseId(),
            },
            [JobId.Alchemist] = new()
            {
                CustomSkillId.PileBinderP.ReleaseId(),
                CustomSkillId.PileBinderT.ReleaseId(),
                CustomSkillId.AlmaPillarP.ReleaseId(),
                CustomSkillId.AlmaPillarT.ReleaseId(),
                CustomSkillId.AlmaWaveP.ReleaseId(),
                CustomSkillId.AlmaWaveT.ReleaseId(),
                CustomSkillId.RexElementaP.ReleaseId(),
                CustomSkillId.RexElementaT.ReleaseId(),
            },
            [JobId.SpiritLancer] = new()
            {
                CustomSkillId.AuromFangP.ReleaseId(),
                CustomSkillId.AuromFangT.ReleaseId(),
                CustomSkillId.WallGlastaP.ReleaseId(),
                CustomSkillId.WallGlastaT.ReleaseId(),
                CustomSkillId.CorrSpikeP.ReleaseId(),
                CustomSkillId.CorrSpikeT.ReleaseId(),
                CustomSkillId.CureGlastaP.ReleaseId(),
                CustomSkillId.CureGlastaT.ReleaseId(),
            },
            [JobId.HighScepter] = new()
            {
                CustomSkillId.PhantomEdge.ReleaseId()
            }
        };

        public static bool IsS3HoAbility(JobId jobId, AbilityId abilityNo, uint abilityLv)
        {
            if (!SkillData.S3HoAbility.TryGetValue(jobId, out var value))
            {
                return false;
            }
            return value.Contains(abilityNo) && (abilityLv == 1);
        }

        public static readonly Dictionary<JobId, HashSet<AbilityId>> S3HoAbility = new()
        {
            [JobId.HighScepter] =
            [
                AbilityId.QuadrupleSlashAbsorption,
                AbilityId.ArcSlashDestroyer,
                AbilityId.SkySlashAbsorption,
                AbilityId.FallingSlashAbsorption,
                AbilityId.RushAttack,
                AbilityId.FlowAttack,
            ]
        };

        public static bool IsUnlockableSkill(JobId jobId, uint skillNo, uint skillLv)
        {
            return SkillData.IsS2BoSkill(jobId, skillNo, skillLv) || SkillData.IsS3HoSkill(jobId, skillNo, skillLv);
        }

        public static bool IsUnlockableAbility(JobId jobId, AbilityId abilityNo, uint abilityLv)
        {
            return SkillData.IsS2BoAbility(jobId, abilityNo, abilityLv) || SkillData.IsS3HoAbility(jobId, abilityNo, abilityLv);
        }
    }
}
