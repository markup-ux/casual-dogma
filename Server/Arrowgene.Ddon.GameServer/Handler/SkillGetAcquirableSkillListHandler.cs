using System.Collections.Generic;
using System.Linq;
using Arrowgene.Ddon.Server;
using Arrowgene.Ddon.Shared.Entity.PacketStructure;
using Arrowgene.Ddon.Shared.Model;
using Arrowgene.Logging;

namespace Arrowgene.Ddon.GameServer.Handler
{
    public class SkillGetAcquirableSkillListHandler : GameRequestPacketHandler<C2SSkillGetAcquirableSkillListReq, S2CSkillGetAcquirableSkillListRes>
    {
        private static readonly ServerLogger Logger = LogProvider.Logger<ServerLogger>(typeof(SkillGetCurrentSetSkillListHandler));

        public SkillGetAcquirableSkillListHandler(DdonGameServer server) : base(server)
        {
        }

        public override S2CSkillGetAcquirableSkillListRes Handle(GameClient client, C2SSkillGetAcquirableSkillListReq request)
        {
            // This list can't be filtered based on progress because it's cached between BBM and normal gameplay.
            //CharacterId = 0 in the request is for the player

            if (Server.GameSettings.GameServerSettings.UnlockAllSkillsAtLevelOne)
            {
                // Every custom skill (including normally locked ones) is released at max rank from level 1.
                return new S2CSkillGetAcquirableSkillListRes()
                {
                    SkillParamList = client.Character.AcquirableSkills[request.Job]
                };
            }

            if (request.CharacterId == 0 || Server.GameSettings.GameServerSettings.PawnSkipJobTraining == false)
            {
                return new S2CSkillGetAcquirableSkillListRes()
                {
                    SkillParamList = client.Character.AcquirableSkills[request.Job]
                };
            }
            else 
            {
                var allDefaultSkills = Server.AssetRepository.SkillData.Skills
                    .GetValueOrDefault(request.Job, [])
                    .Where(x => !SkillData.IsUnlockableSkill(request.Job, x.SkillNo, 1));
                var pawnUnlocks = Server.AssetRepository.SkillData.Skills
                    .GetValueOrDefault(request.Job, [])
                    .Where(x => SkillData.IsUnlockableSkill(request.Job, x.SkillNo, 1)
                    && IsSkillUnlocked(client.Character, request.Job, x.SkillNo)
                    );
                return new S2CSkillGetAcquirableSkillListRes()
                {
                    SkillParamList = [.. allDefaultSkills, .. pawnUnlocks]
                };
            }
        }

        private bool IsSkillUnlocked(Character character, JobId jobId, uint skillNo)
        {
            return character.UnlockedCustomSkills[jobId].Contains(skillNo);
        }
    }
}
