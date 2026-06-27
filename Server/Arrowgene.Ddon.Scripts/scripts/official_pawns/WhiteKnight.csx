/// Official pawn imported from MyPawn.csv.
/// CSV source: WhiteKnight, Hunter, original job level 1.

public class OfficialPawn : IOfficialPawnScript
{
    public override string Name => "WhiteKnight";
    public override JobId Job => JobId.Hunter;
    public override int MinLevel => 1;
    public override float Quality => PawnQuality.Good;
    public override float RentalCostMultiplier => PawnQuality.ToCostMultiplier(Quality);

    public override CDataEditInfo EditInfo => new CDataEditInfo
    {
        Sex = 2,
        Voice = 7,
        VoicePitch = 42000,
        Personality = (PawnPersonality)7,
        SpeechFreq = 1,
        BodyType = 1,
        Hair = 43,
        Beard = 18,
        Makeup = 0,
        Scar = 0,
        EyePresetNo = 2,
        NosePresetNo = 3,
        MouthPresetNo = 5,
        EyebrowTexNo = 8,
        ColorSkin = 0,
        ColorHair = 69,
        ColorBeard = 0,
        ColorEyebrow = 69,
        ColorREye = 55,
        ColorLEye = 55,
        ColorMakeup = 12,
        Sokutobu = 29850,
        Hitai = 29820,
        MimiJyouge = 30420,
        Kannkaku = 29573,
        MabisasiJyouge = 29623,
        HanakuchiJyouge = 29715,
        AgoSakiHaba = 29135,
        AgoZengo = 30172,
        AgoSakiJyouge = 29826,
        HitomiOokisa = 30492,
        MeOokisa = 30002,
        MeKaiten = 29905,
        MayuKaiten = 31800,
        MimiOokisa = 29400,
        MimiMuki = 30000,
        ElfMimi = 30000,
        MikenTakasa = 30147,
        MikenHaba = 29938,
        HohoboneRyou = 29775,
        HohoboneJyouge = 29954,
        Hohoniku = 29920,
        ErahoneJyouge = 29850,
        ErahoneHaba = 29200,
        HanaJyouge = 29404,
        HanaHaba = 29700,
        HanaTakasa = 29472,
        HanaKakudo = 30672,
        KuchiHaba = 29000,
        KuchiAtsusa = 29520,
        EyebrowUVOffsetX = 30645,
        EyebrowUVOffsetY = 30114,
        Wrinkle = 30000,
        WrinkleAlbedoBlendRate = 30000,
        WrinkleDetailNormalPower = 30000,
        MuscleAlbedoBlendRate = 30000,
        MuscleDetailNormalPower = 30000,
        Height = 48050,
        HeadSize = 39733,
        NeckOffset = 29180,
        NeckScale = 35480,
        UpperBodyScaleX = 38550,
        BellySize = 39410,
        TeatScale = 43443,
        TekubiSize = 38600,
        KoshiOffset = 30600,
        KoshiSize = 29333,
        AnkleOffset = 28700,
        Fat = 20900,
        Muscle = 30000,
        MotionFilter = 24900,
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
