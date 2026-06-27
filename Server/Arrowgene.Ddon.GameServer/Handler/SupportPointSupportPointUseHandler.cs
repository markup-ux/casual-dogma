using Arrowgene.Ddon.Server;
using Arrowgene.Ddon.Server.Network;
using Arrowgene.Ddon.Shared.Entity.PacketStructure;
using Arrowgene.Ddon.Shared.Entity.Structure;
using Arrowgene.Ddon.Shared.Model;
using Arrowgene.Logging;
using System.Linq;

namespace Arrowgene.Ddon.GameServer.Handler
{
    public class SupportPointSupportPointUseHandler(DdonGameServer server) : GameRequestPacketQueueHandler<C2SSupportPointSupportPointUseReq, S2CSupportPointSupportPointUseRes>(server)
    {
        private static readonly ServerLogger Logger = LogProvider.Logger<ServerLogger>(typeof(SupportPointSupportPointUseHandler));

        
        public override PacketQueue Handle(GameClient client, C2SSupportPointSupportPointUseReq request)
        {
            var queue = new PacketQueue();

            // This structure should only ever have one entry in it.
            var usePoint = request.UsePointList.Single();
            uint adjustPoint = usePoint.UsePoint / Server.GameSettings.GameServerSettings.RentalPointConversionRate;

            var pawn = client.Character.PawnById(usePoint.PawnId, PawnType.Main);

            CDataCharacterJobData selectedJob = pawn.CharacterJobDataList.Where(jobData => jobData.Job == usePoint.JobType).Single();
            selectedJob.JobPoint += adjustPoint;

            Server.Database.ExecuteInTransaction(connection =>
            {
                client.Enqueue(Server.WalletManager.RemoveFromWalletNtc2(client.Character, WalletType.RentalPoints, usePoint.UsePoint, connection), queue);
                Server.Database.UpdateCharacterJobData(pawn.CommonId, selectedJob, connection);
            });

            client.Enqueue(new S2CJobPawnJobPointNtc()
            {
                PawnId = pawn.PawnId,
                Job = selectedJob.Job,
                AddJobPoint = adjustPoint,
                TotalJobPoint = selectedJob.JobPoint,
            }, queue);

            client.Enqueue(new S2CSupportPointSupportPointUseRes()
            {
                UsePoint = [ new() {
                        UseSupportPoint = usePoint,
                        AdjustPoint = adjustPoint,
                        TotalPoint = selectedJob.JobPoint,
                    }]
            }, queue);

            return queue;
        }
    }
}
