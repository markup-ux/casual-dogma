#load "libs.csx"

public class SkillAugmentation : ISkillAugmentation
{
    public override JobId JobId => JobId.Seeker;
    public override OrbTreeType OrbTreeType => OrbTreeType.Season3;
}

var skillAugmentation = new SkillAugmentation();

#region TIER1
// Row 1
skillAugmentation.AddNode(1)
    .Location(2, 1)
    .BloodOrbCost(3100)
    .Unlocks(OrbGainParamType.JobHpMax, 30);
skillAugmentation.AddNode(2)
    .Location(4, 1)
    .BloodOrbCost(3300)
    .Unlocks(OrbGainParamType.JobPhysicalAttack, 1);
skillAugmentation.AddNode(3)
    .Location(6, 1)
    .BloodOrbCost(3100)
    .Unlocks(OrbGainParamType.JobHpMax, 30);
// Row 2
skillAugmentation.AddNode(4)
    .Location(3, 2)
    .BloodOrbCost(3500)
    .HasUnlockDependencies(1)
    .Unlocks(OrbGainParamType.JobMagicalAttack, 1);
skillAugmentation.AddNode(5)
    .Location(4, 2)
    .BloodOrbCost(3500)
    .HasUnlockDependencies(2)
    .Unlocks(OrbGainParamType.JobMagicalDefence, 1);
skillAugmentation.AddNode(6)
    .Location(5, 2)
    .BloodOrbCost(3500)
    .HasUnlockDependencies(3)
    .Unlocks(OrbGainParamType.JobMagicalAttack, 1);
// Row 3
skillAugmentation.AddNode(7)
    .Location(2, 3)
    .HighOrbCost(200)
    .HasUnlockDependencies(4)
    .Unlocks(OrbGainParamType.JobPhysicalAttack, 1);
skillAugmentation.AddNode(8)
    .Location(4, 3)
    .BloodOrbCost(4000)
    .Unlocks(CustomSkillId.EasyKillT)
    .HasUnlockDependencies(4, 5, 6);
skillAugmentation.AddNode(9)
    .Location(6, 3)
    .HighOrbCost(200)
    .HasUnlockDependencies(6)
    .HasSpecialConditionDependencies(1)
    .Unlocks(OrbGainParamType.JobPhysicalAttack, 1);
// Row 4
skillAugmentation.AddNode(10)
    .Location(1, 4)
    .BloodOrbCost(3500)
    .HasUnlockDependencies(7)
    .Unlocks(OrbGainParamType.JobHpMax, 35);
skillAugmentation.AddNode(11)
    .Location(3, 4)
    .HighOrbCost(400)
    .HasUnlockDependencies(8)
    .Unlocks(OrbGainParamType.JobMagicalAttack, 1);
skillAugmentation.AddNode(12)
    .Location(5, 4)
    .HighOrbCost(400)
    .HasUnlockDependencies(8)
    .Unlocks(OrbGainParamType.JobMagicalAttack, 1);
skillAugmentation.AddNode(13)
    .Location(7, 4)
    .BloodOrbCost(3500)
    .HasUnlockDependencies(9)
    .Unlocks(OrbGainParamType.JobHpMax, 35);
// Row 5
skillAugmentation.AddNode(14)
    .Location(1, 5)
    .BloodOrbCost(4000)
    .HasUnlockDependencies(10)
    .Unlocks(OrbGainParamType.JobMagicalDefence, 1);
skillAugmentation.AddNode(15)
    .Location(3, 5)
    .HighOrbCost(400)
    .HasUnlockDependencies(11)
    .Unlocks(OrbGainParamType.JobMagicalDefence, 1);
skillAugmentation.AddNode(16)
    .Location(5, 5)
    .HighOrbCost(400)
    .HasUnlockDependencies(12)
    .Unlocks(OrbGainParamType.JobPhysicalDefence, 1);
skillAugmentation.AddNode(17)
    .Location(7, 5)
    .BloodOrbCost(4000)
    .HasUnlockDependencies(13)
    .Unlocks(OrbGainParamType.JobPhysicalDefence, 1);
// Row 6
skillAugmentation.AddNode(18)
    .Location(1, 6)
    .BloodOrbCost(5000)
    .HasUnlockDependencies(14)
    .Unlocks(OrbGainParamType.JobHpMax, 40);
skillAugmentation.AddNode(19)
    .Location(4, 6)
    .HighOrbCost(400)
    .HasUnlockDependencies(15, 16)
    .Unlocks(OrbGainParamType.JobPhysicalDefence, 1);
skillAugmentation.AddNode(20)
    .Location(7, 6)
    .BloodOrbCost(5000)
    .HasUnlockDependencies(17)
    .Unlocks(OrbGainParamType.JobHpMax, 40);
// Row 7
skillAugmentation.AddNode(21)
    .Location(1, 7)
    .BloodOrbCost(5500)
    .HasUnlockDependencies(18)
    .Unlocks(OrbGainParamType.AllJobsPhysicalAttack, 1);
skillAugmentation.AddNode(22)
    .Location(4, 7)
    .HighOrbCost(200)
    .HasUnlockDependencies(19)
    .HasSpecialConditionDependencies(2)
    .Unlocks(OrbGainParamType.JobPhysicalAttack, 1);
skillAugmentation.AddNode(23)
    .Location(7, 7)
    .BloodOrbCost(5500)
    .HasUnlockDependencies(20)
    .Unlocks(OrbGainParamType.AllJobsStaminaMax, 10);
// Row 8
skillAugmentation.AddNode(24)
    .Location(4, 8)
    .HighOrbCost(600)
    .HasUnlockDependencies(22)
    .Unlocks(OrbGainParamType.JobHpMax, 40);
// Row 9
skillAugmentation.AddNode(25)
    .Location(4, 9)
    .HighOrbCost(800)
    .HasUnlockDependencies(24)
    .HasSpecialConditionDependencies(3)
    .Unlocks(CustomSkillId.EasyKillP);
#endregion

#region TIER2
// Row 10
skillAugmentation.AddNode(26)
    .Location(4, 10)
    .BloodOrbCost(3500)
    .HasUnlockDependencies(25)
    .HasQuestDependency(QuestId.HerosRestFeryanaRegion)
    .Unlocks(OrbGainParamType.JobMagicalAttack, 1);
// Row 11
skillAugmentation.AddNode(27)
    .Location(3, 11)
    .BloodOrbCost(3600)
    .HasUnlockDependencies(26)
    .Unlocks(OrbGainParamType.JobPhysicalDefence, 1);
skillAugmentation.AddNode(28)
    .Location(4, 11)
    .BloodOrbCost(3600)
    .HasUnlockDependencies(26)
    .Unlocks(OrbGainParamType.JobHpMax, 30);
skillAugmentation.AddNode(29)
    .Location(5, 11)
    .BloodOrbCost(3600)
    .HasUnlockDependencies(26)
    .Unlocks(OrbGainParamType.JobMagicalDefence, 1);
// Row 12
skillAugmentation.AddNode(30)
    .Location(2, 12)
    .HighOrbCost(250)
    .HasUnlockDependencies(27)
    .HasSpecialConditionDependencies(4)
    .Unlocks(OrbGainParamType.JobHpMax, 35);
skillAugmentation.AddNode(31)
    .Location(4, 12)
    .BloodOrbCost(4000)
    .HasUnlockDependencies(27, 28, 29)
    .Unlocks(OrbGainParamType.JobPhysicalAttack, 1);
skillAugmentation.AddNode(32)
    .Location(6, 12)
    .HighOrbCost(250)
    .HasUnlockDependencies(29)
    .HasSpecialConditionDependencies(5)
    .Unlocks(OrbGainParamType.JobHpMax, 35);
// Row 13
skillAugmentation.AddNode(33)
    .Location(1, 13)
    .BloodOrbCost(3600)
    .HasUnlockDependencies(30)
    .Unlocks(OrbGainParamType.JobMagicalAttack, 1);
skillAugmentation.AddNode(34)
    .Location(4, 13)
    .BloodOrbCost(5500)
    .HasUnlockDependencies(31)
    .Unlocks(CustomSkillId.ExplosiveFlameBladeT);
skillAugmentation.AddNode(35)
    .Location(7, 13)
    .BloodOrbCost(3600)
    .HasUnlockDependencies(32)
    .Unlocks(OrbGainParamType.JobMagicalAttack, 1);
// Row 14
skillAugmentation.AddNode(36)
    .Location(1, 14)
    .BloodOrbCost(3700)
    .HasUnlockDependencies(33)
    .Unlocks(OrbGainParamType.JobHpMax, 35);
skillAugmentation.AddNode(37)
    .Location(3, 14)
    .HighOrbCost(400)
    .HasUnlockDependencies(30, 34)
    .Unlocks(OrbGainParamType.JobPhysicalDefence, 1);
skillAugmentation.AddNode(38)
    .Location(5, 14)
    .HighOrbCost(400)
    .HasUnlockDependencies(32, 34)
    .Unlocks(OrbGainParamType.JobMagicalDefence, 1);
skillAugmentation.AddNode(39)
    .Location(7, 14)
    .BloodOrbCost(3700)
    .HasUnlockDependencies(35)
    .Unlocks(OrbGainParamType.JobHpMax, 35);
// Row 15
skillAugmentation.AddNode(40)
    .Location(4, 15)
    .HighOrbCost(650)
    .HasUnlockDependencies(37, 38)
    .Unlocks(OrbGainParamType.JobMagicalAttack, 1);
// Row 16
skillAugmentation.AddNode(41)
    .Location(1, 16)
    .BloodOrbCost(3800)
    .HasUnlockDependencies(36)
    .Unlocks(OrbGainParamType.JobMagicalAttack, 1);
skillAugmentation.AddNode(42)
    .Location(4, 16)
    .HighOrbCost(400)
    .HasUnlockDependencies(40)
    .HasSpecialConditionDependencies(6)
    .Unlocks(OrbGainParamType.JobPhysicalAttack, 1);
skillAugmentation.AddNode(43)
    .Location(7, 16)
    .BloodOrbCost(3800)
    .HasUnlockDependencies(39)
    .Unlocks(OrbGainParamType.JobMagicalAttack, 1);
// Row 17
skillAugmentation.AddNode(44)
    .Location(1, 17)
    .BloodOrbCost(4000)
    .HasUnlockDependencies(41)
    .Unlocks(OrbGainParamType.JobPhysicalAttack, 1);
skillAugmentation.AddNode(45)
    .Location(2, 17)
    .BloodOrbCost(5500)
    .HasUnlockDependencies(41)
    .Unlocks(OrbGainParamType.AllJobsPhysicalAttack, 1);
skillAugmentation.AddNode(46)
    .Location(3, 17)
    .HighOrbCost(650)
    .HasUnlockDependencies(42)
    .Unlocks(OrbGainParamType.JobHpMax, 40);
skillAugmentation.AddNode(47)
    .Location(5, 17)
    .HighOrbCost(650)
    .HasUnlockDependencies(42)
    .Unlocks(OrbGainParamType.JobHpMax, 40);
skillAugmentation.AddNode(48)
    .Location(6, 17)
    .BloodOrbCost(5500)
    .HasUnlockDependencies(43)
    .Unlocks(OrbGainParamType.AllJobsStaminaMax, 10);
skillAugmentation.AddNode(49)
    .Location(7, 17)
    .BloodOrbCost(4000)
    .HasUnlockDependencies(43)
    .Unlocks(OrbGainParamType.JobPhysicalAttack, 1);
// Row 18
skillAugmentation.AddNode(50)
    .Location(4, 18)
    .HighOrbCost(850)
    .HasUnlockDependencies(46, 47)
    .Unlocks(CustomSkillId.ExplosiveFlameBladeP);
#endregion

return skillAugmentation;
