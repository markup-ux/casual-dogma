/// Official pawn imported from MyPawn.csv.
/// CSV source: Puala, Spirit Lancer, original job level 1.

public class OfficialPawn : IOfficialPawnScript
{
    public override string Name => "Puala";
    public override JobId Job => JobId.SpiritLancer;
    public override int MinLevel => 1;
    public override float Quality => PawnQuality.Good;
    public override float RentalCostMultiplier => PawnQuality.ToCostMultiplier(Quality);

    public override CDataEditInfo EditInfo => new CDataEditInfo
    {
        Sex = 2,
        Voice = 1,
        VoicePitch = 30000,
        Personality = (PawnPersonality)1,
        SpeechFreq = 1,
        BodyType = 1,
        Hair = 16,
        Beard = 18,
        Makeup = 1,
        Scar = 0,
        EyePresetNo = 6,
        NosePresetNo = 1,
        MouthPresetNo = 2,
        EyebrowTexNo = 19,
        ColorSkin = 1,
        ColorHair = 69,
        ColorBeard = 0,
        ColorEyebrow = 69,
        ColorREye = 11,
        ColorLEye = 11,
        ColorMakeup = 21,
        Sokutobu = 29990,
        Hitai = 29660,
        MimiJyouge = 29595,
        Kannkaku = 29538,
        MabisasiJyouge = 29812,
        HanakuchiJyouge = 29343,
        AgoSakiHaba = 29261,
        AgoZengo = 29829,
        AgoSakiJyouge = 29622,
        HitomiOokisa = 30682,
        MeOokisa = 29853,
        MeKaiten = 29885,
        MayuKaiten = 20700,
        MimiOokisa = 29778,
        MimiMuki = 30085,
        ElfMimi = 30000,
        MikenTakasa = 30000,
        MikenHaba = 30142,
        HohoboneRyou = 29860,
        HohoboneJyouge = 29711,
        Hohoniku = 30004,
        ErahoneJyouge = 29670,
        ErahoneHaba = 29416,
        HanaJyouge = 29340,
        HanaHaba = 29713,
        HanaTakasa = 29528,
        HanaKakudo = 30455,
        KuchiHaba = 29473,
        KuchiAtsusa = 30064,
        EyebrowUVOffsetX = 30714,
        EyebrowUVOffsetY = 29724,
        Wrinkle = 30000,
        WrinkleAlbedoBlendRate = 30000,
        WrinkleDetailNormalPower = 30000,
        MuscleAlbedoBlendRate = 30000,
        MuscleDetailNormalPower = 30000,
        Height = 45100,
        HeadSize = 40025,
        NeckOffset = 29020,
        NeckScale = 32400,
        UpperBodyScaleX = 38790,
        BellySize = 41090,
        TeatScale = 48500,
        TekubiSize = 38500,
        KoshiOffset = 29270,
        KoshiSize = 30470,
        AnkleOffset = 29080,
        Fat = 23300,
        Muscle = 34800,
        MotionFilter = 29000,
    };

    public override RentalPawnRecord Generate(OfficialPawnContext ctx)
    {
        ctx.Builder
            .WithAutoEquipment(Quality)
            .WithAutoCustomSkills(Quality)
            .WithAutoAbilities(Quality)
            .WithAutoCraft(Quality);

        return ctx.Builder.Build();
    }
}

return new OfficialPawn();
