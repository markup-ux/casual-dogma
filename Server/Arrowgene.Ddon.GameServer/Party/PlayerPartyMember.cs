using Arrowgene.Ddon.GameServer.Characters;
using Arrowgene.Ddon.GameServer.Quests;
using Arrowgene.Ddon.Shared.Entity.PacketStructure;
using Arrowgene.Ddon.Shared.Entity.Structure;
using Arrowgene.Ddon.Shared.Model;

namespace Arrowgene.Ddon.GameServer.Party;

public class PlayerPartyMember : PartyMember
{
    private readonly DdonGameServer _server;

    public PlayerPartyMember(GameClient client, DdonGameServer server)
    {
        Client = client;
        _server = server;
        QuestState = new SoloQuestStateManager(this, server);
    }

    public GameClient Client { get; set; }

    public SoloQuestStateManager QuestState { get; set; }

    public override CDataPartyMember CDataPartyMember
    {
        get
        {
            var cdata = base.CDataPartyMember;
            var listElement = Client.Character.CDataCharacterListElement;
            if (_server.LevelSyncManager.TryGetActiveSync(Client.Character, out LevelSyncManager.ActiveSyncState sync))
            {
                listElement = _server.LevelSyncManager.ApplyDisplayToListElement(listElement, Client.Character, sync);
            }

            cdata.CharacterListElement = listElement;
            return cdata;
        }
    }

    public S2CContextGetPartyPlayerContextNtc GetPartyContext()
    {
        CDataContextPlayerInfo playerInfo = Client.Character.CDataContextPlayerInfo;
        if (_server.LevelSyncManager.TryGetActiveSync(Client.Character, out LevelSyncManager.ActiveSyncState sync))
        {
            _server.LevelSyncManager.ApplyDisplayToContextPlayerInfo(playerInfo, Client.Character, sync, Client.GameMode);
        }

        CDataPartyPlayerContext partyPlayerContext = new()
        {
            Base = Client.Character.CDataContextBase,
            PlayerInfo = playerInfo,
            ResistInfo = Client.Character.CDataContextResist,
            EditInfo = Client.Character.EditInfo
        };

        S2CContextGetPartyPlayerContextNtc partyPlayerContextNtc = new()
        {
            CharacterId = Client.Character.CharacterId,
            Context = partyPlayerContext
        };

        partyPlayerContextNtc.Context.Base.MemberIndex = MemberIndex;
        return partyPlayerContextNtc;
    }
}
