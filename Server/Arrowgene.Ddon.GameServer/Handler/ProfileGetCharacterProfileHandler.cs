using Arrowgene.Ddon.GameServer.Characters;
using Arrowgene.Ddon.Server;
using Arrowgene.Ddon.Shared.Entity.PacketStructure;
using Arrowgene.Ddon.Shared.Entity.Structure;
using Arrowgene.Ddon.Shared.Model;
using Arrowgene.Logging;
using System.Linq;

namespace Arrowgene.Ddon.GameServer.Handler
{
    public class ProfileGetCharacterProfileHandler : GameRequestPacketHandler<C2SProfileGetCharacterProfileReq, S2CProfileGetCharacterProfileRes>
    {
        private static readonly ServerLogger Logger = LogProvider.Logger<ServerLogger>(typeof(ProfileGetCharacterProfileHandler));

        public ProfileGetCharacterProfileHandler(DdonGameServer server) : base(server)
        {
        }

        public override S2CProfileGetCharacterProfileRes Handle(GameClient client, C2SProfileGetCharacterProfileReq request)
        {
            Character targetCharacter = Server.ClientLookup.GetClientByCharacterId(request.CharacterId)?.Character
                ?? Server.CharacterManager.SelectCharacter(request.CharacterId, fetchPawns:false)
                ?? throw new ResponseErrorException(ErrorCode.ERROR_CODE_CHARACTER_DATA_INVALID_CHARACTER_ID);

            CDataCharacterJobData jobParam = targetCharacter.ActiveCharacterJobData;
            CDataCharacterLevelParam characterParam = targetCharacter.CDataCharacterLevelParam;
            byte profileJobLevel = (byte)targetCharacter.ActiveCharacterJobData.Lv;
            GameClient? targetClient = Server.ClientLookup.GetClientByCharacterId(request.CharacterId);
            GameMode gameMode = targetClient?.GameMode ?? client.GameMode;

            if (targetClient != null
                && Server.LevelSyncManager.TryGetActiveSync(targetCharacter, out LevelSyncManager.ActiveSyncState sync))
            {
                jobParam = Server.LevelSyncManager.CreateDisplayJobData(targetCharacter.ActiveCharacterJobData, sync, gameMode);
                characterParam = Server.LevelSyncManager.CreateDisplayLevelParam(targetCharacter, sync);
                profileJobLevel = (byte)sync.RecLevel;
            }

            S2CCharacterGetCharacterStatusNtc ntc = new()
            {
                CharacterId = targetCharacter.CharacterId,
                StatusInfo = targetCharacter.StatusInfo,
                JobParam = jobParam,
                CharacterParam = characterParam,
                EditInfo = targetCharacter.EditInfo,
                EquipDataList = targetCharacter.Equipment.AsCDataEquipItemInfo(EquipType.Performance),
                VisualEquipDataList = targetCharacter.Equipment.AsCDataEquipItemInfo(EquipType.Visual),
                EquipJobItemList = targetCharacter.EquipmentTemplate.JobItemsAsCDataEquipJobItem(targetCharacter.Job),
                HideHead = targetCharacter.HideEquipHead,
                HideLantern = targetCharacter.HideEquipLantern,
                JewelryNum = targetCharacter.ExtendedParams.JewelrySlot
            };

            client.Send(ntc);

            S2CProfileGetCharacterProfileRes res = new()
            {
                CharacterId = targetCharacter.CharacterId,
                CharacterName = targetCharacter.CDataCharacterName,
                JobId = targetCharacter.Job,
                JobLevel = profileJobLevel,
                ClanParam = Server.ClanManager.GetClan(targetCharacter.ClanId)
            };

            var (clanId, memberInfo) = Server.ClanManager.ClanMembership(targetCharacter.CharacterId);
            if (memberInfo != null)
            {
                res.ClanMemberRank = (uint)memberInfo.Rank;
            }

            res.JobLevelList = [.. targetCharacter.CharacterJobDataList.Select(jobData => new CDataJobBaseInfo()
            {
                Job = jobData.Job,
                Level = (byte)(jobData.Job == targetCharacter.Job ? profileJobLevel : jobData.Lv)
            })];
            res.MatchingProfile = targetCharacter.MatchingProfile;
            res.ArisenProfile = targetCharacter.CharacterProfile.CDataArisenProfile;
            // TODO: OnlineId

            return res;
        }
    }
}
