/// /exportappearance
///   Exports the player character's own EditInfo as CSX code.
///
/// /exportappearance pawn [slot=1]
///   Exports the EditInfo of the player's pawn at the given slot.
///
/// Output is written to the server log.  Copy the printed block into an
/// official_pawns/*.csx script's EditInfo property.
///
/// Admin-only command.
public class ChatCommand : IChatCommand
{
    public override AccountStateType AccountState => AccountStateType.Admin;
    public override string CommandName => "exportappearance";
    public override string HelpText =>
        "usage: `/exportappearance` - export your character's appearance\n" +
        "       `/exportappearance pawn [slot=1]` - export a pawn's appearance";

    public override void Execute(DdonGameServer server, string[] command, GameClient client, ChatMessage message, List<ChatResponse> responses)
    {
        bool isPawn = command.Length > 0 && command[0].Equals("pawn", StringComparison.OrdinalIgnoreCase);

        CDataEditInfo editInfo;
        string label;

        if (isPawn)
        {
            int slot = 1;
            if (command.Length > 1 && int.TryParse(command[1], out int parsed))
                slot = parsed;

            var pawn = client.Character.Pawns.ElementAtOrDefault(slot - 1);
            if (pawn == null)
            {
                responses.Add(ChatResponse.CommandError(client, $"No pawn found in slot {slot}."));
                return;
            }

            editInfo = pawn.EditInfo;
            label = $"Pawn '{pawn.Name}' (slot {slot}, job {pawn.Job})";
        }
        else
        {
            editInfo = client.Character.EditInfo;
            label = $"Character '{client.Character.FirstName}' (job {client.Character.Job})";
        }

        var output = BuildCsxBlock(editInfo, label);
        Arrowgene.Logging.LogProvider.Logger(typeof(ChatCommand)).Info(output);

        responses.Add(ChatResponse.ServerMessage(client,
            $"[exportappearance] {label} — EditInfo written to server log. " +
            "Paste the block into your official_pawns CSX script's EditInfo property."));
    }

    private static string BuildCsxBlock(CDataEditInfo e, string label)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"// {label}");
        sb.AppendLine("// NOTE: All fields are ushort (unsigned). Negative in-game values are");
        sb.AppendLine("// stored as their two's-complement ushort equivalent (65536 + negative).");
        sb.AppendLine("public override CDataEditInfo EditInfo => new CDataEditInfo");
        sb.AppendLine("{");
        sb.AppendLine($"    Sex              = {e.Sex},");
        sb.AppendLine($"    Voice            = {e.Voice},");
        sb.AppendLine($"    VoicePitch       = {e.VoicePitch},");
        sb.AppendLine($"    Personality      = (PawnPersonality){(byte)e.Personality},");
        sb.AppendLine($"    SpeechFreq       = {e.SpeechFreq},");
        sb.AppendLine($"    BodyType         = {e.BodyType},");
        sb.AppendLine($"    Hair             = {e.Hair},");
        sb.AppendLine($"    Beard            = {e.Beard},");
        sb.AppendLine($"    Makeup           = {e.Makeup},");
        sb.AppendLine($"    Scar             = {e.Scar},");
        sb.AppendLine($"    EyePresetNo      = {e.EyePresetNo},");
        sb.AppendLine($"    NosePresetNo     = {e.NosePresetNo},");
        sb.AppendLine($"    MouthPresetNo    = {e.MouthPresetNo},");
        sb.AppendLine($"    EyebrowTexNo     = {e.EyebrowTexNo},");
        sb.AppendLine($"    ColorSkin        = {e.ColorSkin},");
        sb.AppendLine($"    ColorHair        = {e.ColorHair},");
        sb.AppendLine($"    ColorBeard       = {e.ColorBeard},");
        sb.AppendLine($"    ColorEyebrow     = {e.ColorEyebrow},");
        sb.AppendLine($"    ColorREye        = {e.ColorREye},");
        sb.AppendLine($"    ColorLEye        = {e.ColorLEye},");
        sb.AppendLine($"    ColorMakeup      = {e.ColorMakeup},");
        sb.AppendLine($"    Sokutobu         = {e.Sokutobu},");
        sb.AppendLine($"    Hitai            = {e.Hitai},");
        sb.AppendLine($"    MimiJyouge       = {e.MimiJyouge},");
        sb.AppendLine($"    Kannkaku         = {e.Kannkaku},");
        sb.AppendLine($"    MabisasiJyouge   = {e.MabisasiJyouge},");
        sb.AppendLine($"    HanakuchiJyouge  = {e.HanakuchiJyouge},");
        sb.AppendLine($"    AgoSakiHaba      = {e.AgoSakiHaba},");
        sb.AppendLine($"    AgoZengo         = {e.AgoZengo},");
        sb.AppendLine($"    AgoSakiJyouge    = {e.AgoSakiJyouge},");
        sb.AppendLine($"    HitomiOokisa     = {e.HitomiOokisa},");
        sb.AppendLine($"    MeOokisa         = {e.MeOokisa},");
        sb.AppendLine($"    MeKaiten         = {e.MeKaiten},");
        sb.AppendLine($"    MayuKaiten       = {e.MayuKaiten},");
        sb.AppendLine($"    MimiOokisa       = {e.MimiOokisa},");
        sb.AppendLine($"    MimiMuki         = {e.MimiMuki},");
        sb.AppendLine($"    ElfMimi          = {e.ElfMimi},");
        sb.AppendLine($"    MikenTakasa      = {e.MikenTakasa},");
        sb.AppendLine($"    MikenHaba        = {e.MikenHaba},");
        sb.AppendLine($"    HohoboneRyou     = {e.HohoboneRyou},");
        sb.AppendLine($"    HohoboneJyouge   = {e.HohoboneJyouge},");
        sb.AppendLine($"    Hohoniku         = {e.Hohoniku},");
        sb.AppendLine($"    ErahoneJyouge    = {e.ErahoneJyouge},");
        sb.AppendLine($"    ErahoneHaba      = {e.ErahoneHaba},");
        sb.AppendLine($"    HanaJyouge       = {e.HanaJyouge},");
        sb.AppendLine($"    HanaHaba         = {e.HanaHaba},");
        sb.AppendLine($"    HanaTakasa       = {e.HanaTakasa},");
        sb.AppendLine($"    HanaKakudo       = {e.HanaKakudo},");
        sb.AppendLine($"    KuchiHaba        = {e.KuchiHaba},");
        sb.AppendLine($"    KuchiAtsusa      = {e.KuchiAtsusa},");
        sb.AppendLine($"    EyebrowUVOffsetX = {e.EyebrowUVOffsetX},");
        sb.AppendLine($"    EyebrowUVOffsetY = {e.EyebrowUVOffsetY},");
        sb.AppendLine($"    Wrinkle                      = {e.Wrinkle},");
        sb.AppendLine($"    WrinkleAlbedoBlendRate       = {e.WrinkleAlbedoBlendRate},");
        sb.AppendLine($"    WrinkleDetailNormalPower     = {e.WrinkleDetailNormalPower},");
        sb.AppendLine($"    MuscleAlbedoBlendRate        = {e.MuscleAlbedoBlendRate},");
        sb.AppendLine($"    MuscleDetailNormalPower      = {e.MuscleDetailNormalPower},");
        sb.AppendLine($"    Height           = {e.Height},");
        sb.AppendLine($"    HeadSize         = {e.HeadSize},");
        sb.AppendLine($"    NeckOffset       = {e.NeckOffset},");
        sb.AppendLine($"    NeckScale        = {e.NeckScale},");
        sb.AppendLine($"    UpperBodyScaleX  = {e.UpperBodyScaleX},");
        sb.AppendLine($"    BellySize        = {e.BellySize},");
        sb.AppendLine($"    TeatScale        = {e.TeatScale},");
        sb.AppendLine($"    TekubiSize       = {e.TekubiSize},");
        sb.AppendLine($"    KoshiOffset      = {e.KoshiOffset},");
        sb.AppendLine($"    KoshiSize        = {e.KoshiSize},");
        sb.AppendLine($"    AnkleOffset      = {e.AnkleOffset},");
        sb.AppendLine($"    Fat              = {e.Fat},");
        sb.AppendLine($"    Muscle           = {e.Muscle},");
        sb.AppendLine($"    MotionFilter     = {e.MotionFilter},");
        sb.AppendLine("};");
        return sb.ToString();
    }
}

return new ChatCommand();
