using Arrowgene.Ddon.GameServer.Characters;
using Arrowgene.Ddon.Server;
using Arrowgene.Ddon.Shared.Entity.PacketStructure;
using Arrowgene.Ddon.Shared.Model;
using Arrowgene.Ddon.Shared.Network;
using Arrowgene.Logging;
using System.Collections.Generic;

namespace Arrowgene.Ddon.GameServer.Handler
{
    public class WarpGetReturnLocationHandler : GameRequestPacketHandler<C2SWarpGetReturnLocationReq, S2CWarpGetReturnLocationRes>
    {
        private static readonly ServerLogger Logger = LogProvider.Logger<ServerLogger>(typeof(WarpGetReturnLocationHandler));

        /// <summary>
        /// What startpos to use for each map. This is usually in front of a riftstone.
        /// </summary>
        private static readonly Dictionary<uint, uint> RespawnStartPosMap = new()
        {
            { 2, 0}, // White Dragon Temple
            { 341, 2}, // Dana Centrum 
            { 487, 2}, // Fortress City Megado: Residential Level
            { 24, 1}, // White Deer Inn
            { 25, 2}, // Black Grape Inn
            { 26, 1}, // Sea Dragon Inn
            { 48, 1}, // Singing Winds Inn
            { 52, 1}, // Red Crystal Inn
            { 53, 1}, // Sleeping Wolf Inn
            { 61, 1}, // Golden Tankard Inn
            { 66, 2}, // Gritten Fort 
            { 78, 4}, // Pawn Cathedral 
            { 95, 2}, // Hobolic Cave 
            { 137, 2}, // Mysree Grove Shrine
            { 139, 2}, // Zandora Wastelands Shrine
            { 237, 1}, // Mergoda Residential Area
            { 317, 2}, // Expedition Garrison 
            { 339, 2}, // Protector's Retreat 
            { 340, 4}, // Morfaul Centrum 
            { 377, 1}, // Glyndwr Centrum 
            { 384, 1}, // Hollow of Beginnings
            { 400, 2}, // Tower of Ivanos
            { 411, 3}, // Manun Village 
            { 467, 6}, // Fort Thines 
            { 478, 2}, // Lookout Castle 
            { 480, 6}, // Bertha's Bandit Group
            { 511, 1}, // Piremoth Traveler's Inn
            { 512, 1}, // Rothgill Traveler's Inn
            { 520, 1}, // Mephite Traveler's Inn
            { 549, 1}, // Heroic Spirit Sleeping Path: Rathnite Foothills
            { 557, 2}, // Heroic Spirit Sleeping Path: Feryana Wilderness
            { 558, 5}, // Old Heroic Spirit Shrine
            { 584, 2}, // Eli Guard Tower
            { 594, 1}, // Northern Bandit Hideout
            { 602, 0}, // Bitterblack Maze Cove
        };

        /// <summary>
        /// Some safe areas are sub-areas of other safe areas, so redirect to the main one when needed.
        /// </summary>
        private static readonly Dictionary<uint, uint> SafeStageRedirect = new()
        {
            {4, 2 }, // Craft Room -> WDT
            {5, 2 }, // Cave Harbor -> WDT
            {141, 2 }, // Summer Beach Area -> WDT
            {347, 2 }, // Clan Hall -> WDT
            {348, 2 }, // Arisen's Room -> WDT
            {401, 411 }, // Spirit Arts Hut -> Manun Village
            {576, 467 }, // Fort Thines: Great Hall -> Fort Thines
            {580, 487 }, // Megado Craft Room -> Megado Residential Level
            {602 , 2 }, // Bitterblack Maze Cove -> WDT, but only for GameMode.Normal
        };

        public WarpGetReturnLocationHandler(DdonGameServer server) : base(server)
        {
        }

        /// <summary>
        /// Determines whether a stored logout stage can be safely used as the login resume target.
        /// Rejects unknown/invalid stages, the Lestania overworld travel map (not a normal loadable
        /// stage), and instanced content (Bitterblack Maze, Epitaph Road) whose instance will no
        /// longer exist on the next login. Anything rejected falls through to the safe-area logic.
        /// </summary>
        private static bool IsResumableLogoutStage(uint stageId)
        {
            if (stageId == 0)
            {
                return false;
            }

            // Must resolve to a real, client-loadable stage (ConvertIdToStageNo returns 0 when unknown).
            if (StageManager.ConvertIdToStageNo(stageId) == 0)
            {
                return false;
            }

            // The overworld travel map spans many areas and is not a concrete spawnable stage.
            if (stageId == Stage.Lestania.StageId)
            {
                return false;
            }

            // Instanced content is per-session; resuming into it could drop the player into a dead instance.
            if (StageManager.IsBitterBlackMazeStageId(stageId) || StageManager.IsEpitaphRoadStageId(stageId))
            {
                return false;
            }

            return true;
        }

        public override S2CWarpGetReturnLocationRes Handle(GameClient client, C2SWarpGetReturnLocationReq request)
        {
            S2CWarpGetReturnLocationRes response = new S2CWarpGetReturnLocationRes();

            // The first return-location request of a session is the login resume. When enabled, send the
            // player back to the exact stage they logged out in instead of their last visited safe area.
            // Later requests (death, return-to-town items, etc.) fall through to the regular safe-area logic.
            bool isLoginResume = !client.HasResolvedReturnLocation;
            client.HasResolvedReturnLocation = true;

            if (isLoginResume
                && Server.GameSettings.GameServerSettings.EnableReturnToLogoutLocation
                && client.GameMode == GameMode.Normal
                && IsResumableLogoutStage(client.Character.LastLogoutStageId))
            {
                response.JumpLocation.stageId = client.Character.LastLogoutStageId;
                response.JumpLocation.startPos = RespawnStartPosMap.GetValueOrDefault(client.Character.LastLogoutStageId, 0u);
                return response;
            }

            if (client.GameMode == GameMode.BitterblackMaze)
            {
                response.JumpLocation.stageId = 602;
            }
            else
            {
                if (BoardManager.BoardIdIsExm(client.Party.ContentId))
                {
                    response.JumpLocation.stageId = client.Character.Stage.Id;
                }
                else if (SafeStageRedirect.ContainsKey(client.Character.LastSafeStageId))
                {
                    response.JumpLocation.stageId = SafeStageRedirect[client.Character.LastSafeStageId];
                }
                else
                {
                    response.JumpLocation.stageId = client.Character.LastSafeStageId;
                }
            }

            if (RespawnStartPosMap.ContainsKey(response.JumpLocation.stageId))
            {
                response.JumpLocation.startPos = RespawnStartPosMap[response.JumpLocation.stageId];
            }
            else
            {
                response.JumpLocation.startPos = 0;
            }

            return response;
        }
    }
}
