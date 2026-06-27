using System;
using System.IO;
using Arrowgene.Ddon.GameServer;
using Arrowgene.Ddon.GameServer.Characters;
using Arrowgene.Ddon.Server;
using Arrowgene.Ddon.Server.Scripting.utils;
using Arrowgene.Ddon.Shared;
using Arrowgene.Ddon.Shared.Entity.RpcPacketStructure;
using Arrowgene.Ddon.Shared.Entity.Structure;
using Arrowgene.Ddon.Shared.Model;
using Arrowgene.Ddon.Test.Database;
using Xunit;

namespace Arrowgene.Ddon.Test.GameServer.Characters;

public class RecoverableHpManagerTest
{
    private readonly DdonGameServer _server;
    private readonly RecoverableHpManager _manager;

    public RecoverableHpManagerTest()
    {
        var settings = new GameServerSetting();
        var scriptableSettings = new ScriptableSettings();
        var gameSettings = new GameSettings(scriptableSettings);
        gameSettings.GameServerSettings.DisableRecoverableHpLossBelowMaxLevel = true;

        var assets = new AssetRepository("Files/Assets");
        assets.Initialize();
        _server = new DdonGameServer(settings, gameSettings, new MockDatabase(), assets);
        _manager = _server.RecoverableHpManager;
    }

    private static Character MakeCharacter(uint level, uint greenHp = 5000, uint whiteHp = 4000)
    {
        return new Character
        {
            FirstName = "Test",
            LastName = "Arisen",
            CommonId = 42,
            GreenHp = greenHp,
            WhiteHp = whiteHp,
            CharacterJobDataList =
            [
                new CDataCharacterJobData
                {
                    Job = JobId.Fighter,
                    Lv = level
                }
            ],
            Job = JobId.Fighter
        };
    }

    [Fact]
    public void ShouldPin_WhenBelowCapAndSettingEnabled()
    {
        Assert.True(_manager.ShouldPin(MakeCharacter(level: 45)));
    }

    [Fact]
    public void ShouldPin_FalseAtCap()
    {
        Assert.False(_manager.ShouldPin(MakeCharacter(level: 120)));
    }

    [Fact]
    public void GetProtectedWhiteHp_RestoresLossGaugeAttrition()
    {
        ushort ceiling = 2000;

        ushort protectedWhite = RecoverableHpManager.GetProtectedWhiteHp(ref ceiling, greenHp: 1200, reportedWhiteHp: 1600);

        Assert.Equal(2000, protectedWhite);
    }

    [Fact]
    public void GetProtectedWhiteHp_RaisesCeilingWhenHealedHigher()
    {
        ushort ceiling = 1500;

        ushort protectedWhite = RecoverableHpManager.GetProtectedWhiteHp(ref ceiling, greenHp: 1800, reportedWhiteHp: 2200);

        Assert.Equal(2200, protectedWhite);
        Assert.Equal(2200, ceiling);
    }

    [Fact]
    public void ClampPeriodicUpdate_PinsWhiteHpToSessionPeak()
    {
        Character character = MakeCharacter(level: 45, greenHp: 4500, whiteHp: 5000);
        ushort ceiling = 0;

        _manager.ClampPeriodicUpdate(character, ref ceiling);
        Assert.Equal(5000u, character.WhiteHp);
        Assert.Equal(5000, ceiling);

        character.GreenHp = 4200;
        character.WhiteHp = 3800;
        _manager.ClampPeriodicUpdate(character, ref ceiling);

        Assert.Equal(4200u, character.GreenHp);
        Assert.Equal(5000u, character.WhiteHp);
    }

    [Fact]
    public void ClampPeriodicUpdate_AcceptsDeath()
    {
        Character character = MakeCharacter(level: 45, greenHp: 5000, whiteHp: 5000);
        ushort ceiling = 5000;
        _manager.ClampPeriodicUpdate(character, ref ceiling);

        character.GreenHp = 0;
        character.WhiteHp = 0;
        _manager.ClampPeriodicUpdate(character, ref ceiling);

        Assert.Equal(0u, character.GreenHp);
        Assert.Equal(0u, character.WhiteHp);
        Assert.Equal(0, ceiling);
    }

    [Fact]
    public void EvaluateCharacter_WritesPinSignalForSubCapCharacter()
    {
        string signalPath = Path.Combine(Path.GetTempPath(), $"ddon_recoverable_hp_{Guid.NewGuid():N}.json");
        Environment.SetEnvironmentVariable("DDON_RECOVERABLE_HP_FILE", signalPath);

        try
        {
            var scriptableSettings = new ScriptableSettings();
            var gameSettings = new GameSettings(scriptableSettings);
            gameSettings.GameServerSettings.DisableRecoverableHpLossBelowMaxLevel = true;
            var localServer = new DdonGameServer(
                new GameServerSetting(),
                gameSettings,
                new MockDatabase(),
                _server.AssetRepository);
            var localManager = localServer.RecoverableHpManager;

            localManager.EvaluateCharacter(MakeCharacter(level: 30));

            string json = File.ReadAllText(signalPath);
            Assert.Contains("Test Arisen", json);
            Assert.Contains("PinRecoverableHp", json);
            Assert.Contains("true", json, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Environment.SetEnvironmentVariable("DDON_RECOVERABLE_HP_FILE", null);
            if (File.Exists(signalPath))
            {
                File.Delete(signalPath);
            }
        }
    }

    [Fact]
    public void ClampPeriodicUpdate_RaisesWhiteHpToSessionCeiling()
    {
        Character character = MakeCharacter(level: 45, greenHp: 447, whiteHp: 545);
        ushort ceiling = 545;

        _manager.ClampPeriodicUpdate(character, ref ceiling);

        Assert.Equal(447u, character.GreenHp);
        Assert.Equal(545u, character.WhiteHp);
    }

    [Fact]
    public void EvaluateCharacter_SeedsCeilingFromStatusWhenDamaged()
    {
        Character character = MakeCharacter(level: 7, greenHp: 447, whiteHp: 545);
        character.StatusInfo.MaxHP = 760;
        character.StatusInfo.GainHP = 35;
        character.StatusInfo.HP = 447;
        character.StatusInfo.WhiteHP = 545;

        ushort ceiling = 0;
        RecoverableHpManager.GetProtectedWhiteHp(ref ceiling, 447, 545);

        Assert.Equal(545, ceiling);

        character.GreenHp = 400;
        character.WhiteHp = 400;
        _manager.ClampPeriodicUpdate(character, ref ceiling);

        Assert.Equal(545u, character.WhiteHp);
    }

    [Fact]
    public void EvaluateCharacter_SyncsStatusInfoWhenCombatMirrorsAreZero()
    {
        Character character = MakeCharacter(level: 7, greenHp: 0, whiteHp: 0);
        character.StatusInfo.MaxHP = 760;
        character.StatusInfo.GainHP = 35;
        character.StatusInfo.HP = 447;
        character.StatusInfo.WhiteHP = 545;

        _manager.EvaluateCharacter(character);

        Assert.Equal(447u, character.GreenHp);
        Assert.Equal(545u, character.WhiteHp);
    }

    [Fact]
    public void TryPatchPeriodicRpc_RewritesWhiteHpToSessionCeiling()
    {
        Character character = MakeCharacter(level: 7, greenHp: 447, whiteHp: 545);
        ushort ceiling = 760;

        byte[] rpc = BuildPlayerPeriodicTop(greenHp: 447, whiteHp: 545);
        Assert.True(_manager.TryPatchPeriodicRpc(character, ref ceiling, rpc));
        Assert.True(RpcCtrlPeriodicTop.TryReadWhiteHp(rpc, out ushort patched));
        Assert.Equal(760, patched);
    }

    [Fact]
    public void TryPatchPeriodicRpc_RewritesWhiteHpInPeriodicPacket()
    {
        Character character = MakeCharacter(level: 45, greenHp: 4200, whiteHp: 5000);
        ushort ceiling = 0;
        _manager.ClampPeriodicUpdate(character, ref ceiling);
        character.WhiteHp = 3800;

        byte[] rpc = BuildPlayerPeriodicTop(greenHp: 4200, whiteHp: 3800);
        Assert.True(_manager.TryPatchPeriodicRpc(character, ref ceiling, rpc));
        Assert.True(RpcCtrlPeriodicTop.TryReadWhiteHp(rpc, out ushort patched));
        Assert.Equal(5000, patched);
    }

    [Fact]
    public void TryPatchPeriodicRpc_NoOpWhenAlreadyAtCeiling()
    {
        Character character = MakeCharacter(level: 45, greenHp: 4500, whiteHp: 5000);
        ushort ceiling = 0;
        _manager.ClampPeriodicUpdate(character, ref ceiling);

        byte[] rpc = BuildPlayerPeriodicTop(whiteHp: 5000);
        Assert.False(_manager.TryPatchPeriodicRpc(character, ref ceiling, rpc));
    }

    [Fact]
    public void TryReadPeriodicTopHp_UsesExpectedOffsets()
    {
        byte[] rpc = BuildPlayerPeriodicTop(greenHp: 400, whiteHp: 350);

        Assert.True(RpcCtrlPeriodicTop.TryReadPeriodicTopHp(rpc, out ushort greenHp, out ushort whiteHp));
        Assert.Equal(400, greenHp);
        Assert.Equal(350, whiteHp);
    }

    private static byte[] BuildPlayerPeriodicTop(ushort whiteHp, ushort greenHp = 4500)
    {
        byte[] rpc = new byte[RpcCtrlPeriodicTop.MinPacketSize];
        WriteUInt16Be(rpc, 12, (ushort)RpcNetMsgDti.cNetMsgCtrlAction);
        WriteUInt16Be(rpc, 14, (ushort)RpcMsgIdControl.NET_MSG_ID_PERIODIC_TOP);
        WriteUInt16Be(rpc, RpcCtrlPeriodicTop.GreenHpOffset, greenHp);
        WriteUInt16Be(rpc, RpcCtrlPeriodicTop.WhiteHpOffset, whiteHp);
        return rpc;
    }

    private static void WriteUInt16Be(byte[] data, int offset, ushort value)
    {
        data[offset] = (byte)(value >> 8);
        data[offset + 1] = (byte)(value & 0xFF);
    }
}
