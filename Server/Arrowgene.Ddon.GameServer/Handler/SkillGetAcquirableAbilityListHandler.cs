using Arrowgene.Ddon.Server;
using Arrowgene.Ddon.Shared.Entity.PacketStructure;
using Arrowgene.Ddon.Shared.Model;
using Arrowgene.Logging;
using System.Linq;

namespace Arrowgene.Ddon.GameServer.Handler
{
    public class SkillGetAcquirableAbilityListHandler : GameRequestPacketHandler<C2SSkillGetAcquirableAbilityListReq, S2CSkillGetAcquirableAbilityListRes>
    {
        private static readonly ServerLogger Logger = LogProvider.Logger<ServerLogger>(typeof(SkillGetAcquirableAbilityListHandler));

        public SkillGetAcquirableAbilityListHandler(DdonGameServer server) : base(server)
        {
        }

        public override S2CSkillGetAcquirableAbilityListRes Handle(GameClient client, C2SSkillGetAcquirableAbilityListReq request)
        {
            // This list probably shouldn't be filtered because of caching,
            // but its fine to return the normal gamemode result no matter what because the BBM character can't interact with abilities/augments.

            S2CSkillGetAcquirableAbilityListRes response = new();

            bool unlockAll = Server.GameSettings.GameServerSettings.UnlockAllSkillsAtLevelOne;

            if (request.Job == JobId.None)
            {
                // This is the list of unlocked secret augments, cached as part of CharacterManager::CalculateAcquirableAbilities.
                response.AbilityParamList = [.. client.Character.AcquirableAbilities[JobId.None]];
            }
            else if (unlockAll)
            {
                // Every augment (including normally locked ones) is available regardless of progress.
                response.AbilityParamList = [.. client.Character.AcquirableAbilities[request.Job]];
            }
            else
            {
                if (request.CharacterId != 0 && Server.GameSettings.GameServerSettings.PawnSkipJobTraining)
                {
                    var allDefaultAbilities = Server.AssetRepository.SkillData.Abilities[request.Job]
                        .Where(x => x.Job == request.Job && !SkillData.IsUnlockableAbility(request.Job, x.AbilityNo, 1));
                    var pawnUnlocks = Server.AssetRepository.SkillData.Abilities[request.Job]
                        .Where(x => SkillData.IsUnlockableAbility(request.Job, x.AbilityNo, 1) 
                            && IsAbilityUnlocked(client.Character, request.Job, x.AbilityNo));

                    response.AbilityParamList = [.. allDefaultAbilities, .. pawnUnlocks];
                }
                else
                {
                    response.AbilityParamList = [.. client.Character.AcquirableAbilities[request.Job]
                    .Where(x => !SkillData.IsUnlockableAbility(request.Job, x.AbilityNo, 1)
                        || IsAbilityUnlocked(client.Character, request.Job, x.AbilityNo))
                    ];
                }
            }

            return response;
        }

        private bool IsAbilityUnlocked(Character character, JobId jobId, AbilityId abilityNo)
        {
            return character.UnlockedAbilities[jobId].Contains(abilityNo);
        }
    }
}
