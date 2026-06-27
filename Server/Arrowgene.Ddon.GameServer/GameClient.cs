using Arrowgene.Ddon.Database.Model;
using Arrowgene.Ddon.GameServer.Characters;
using Arrowgene.Ddon.GameServer.GatheringItems;
using Arrowgene.Ddon.GameServer.Party;
using Arrowgene.Ddon.GameServer.Quests;
using Arrowgene.Ddon.GameServer.Shop;
using Arrowgene.Ddon.Server.Network;
using Arrowgene.Ddon.Shared.Entity.PacketStructure;
using Arrowgene.Ddon.Shared.Entity.Structure;
using Arrowgene.Ddon.Shared.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using Arrowgene.Networking.SAEAServer;

namespace Arrowgene.Ddon.GameServer
{
    public class GameClient : Client
    {
        public GameClient(ClientHandle clientHandle, PacketFactory packetFactory, DdonGameServer server) : base(clientHandle, packetFactory)
        {
            UpdateIdentity();
            InstanceGatheringItemManager = new InstanceGatheringItemManager(this, server);
            InstanceDropItemManager = new(this, server);
            InstanceShopManager = new InstanceShopManager(server.ShopManager);
            SupplyCacheDropTracker = new SupplyCacheDropTracker();
            SupplyCacheDiagnostics = new SupplyCacheSessionDiagnostics();
            RegistrationAudit = new SupplyCacheRegistrationAudit();
            GameMode = GameMode.Normal;
        }

        public void UpdateIdentity()
        {
            // ClientHandle.Identity throws ObjectDisposedException once the handle has been
            // recycled (e.g. the client disconnected). Fall back to the last known identity
            // so logging never crashes on a stale handle.
            string handleIdentity;
            try
            {
                handleIdentity = ClientHandle.Identity;
            }
            catch (ObjectDisposedException)
            {
                handleIdentity = Identity ?? "disconnected";
            }

            string newIdentity = $"[GameClient#{Id}@{handleIdentity}]";
            if (Account != null)
            {
                newIdentity += $"[Acc:({Account.Id}){Account.NormalName}]";
            }

            if (Character != null)
            {
                newIdentity += $"[Cha:({Character.CharacterId}){Character.FirstName} {Character.LastName}]";
            }

            Identity = newIdentity;
        }

        public Account Account { get; set; }

        public Character Character { get; set; }

        public PartyGroup Party { get; set; }
        public InstanceShopManager InstanceShopManager { get; }
        public InstanceGatheringItemManager InstanceGatheringItemManager { get; }
        public InstanceDropItemManager InstanceDropItemManager { get; }
        public SupplyCacheDropTracker SupplyCacheDropTracker { get; }
        public SupplyCacheSessionDiagnostics SupplyCacheDiagnostics { get; }
        public SupplyCacheRegistrationAudit RegistrationAudit { get; }

        public GameMode GameMode { get; set; }

        public QuestStateManager QuestState { get
            {
                return ((PlayerPartyMember)Party?.GetPartyMemberByCharacter(Character))?.QuestState;
            } 
        }

        public bool IsPartyLeader()
        {
            return Party.Leader?.Client == this;
        }

        // TODO: Place somewhere else more sensible
        public uint LastWarpPointId { get; set; }
        public DateTime LastWarpDateTime { get; set; }

        /// <summary>
        /// Whether this session has already resolved a return location at least once.
        /// The very first return-location request of a session corresponds to logging in, where we
        /// want to resume the player where they logged out. Subsequent requests (e.g. dying and
        /// returning to town) keep the regular "return to a safe area" behavior.
        /// </summary>
        public bool HasResolvedReturnLocation { get; set; }
        public S2CLobbyLobbyDataMsgNotice LastLobbyDataMsg { get; set; }

        /// <summary>
        /// Consecutive normal enemy kills that did not grant exploration progression gear.
        /// Used by <see cref="GatheringItems.Generators.ExplorationProgressionDropGenerator"/> pity.
        /// </summary>
        public uint ExplorationKillsWithoutGear { get; set; }

        /// <summary>
        /// Highest recoverable (white) HP seen this session while loss-gauge protection is active.
        /// </summary>
        public ushort ProtectedRecoverableHpCeiling { get; set; }

        /// <summary>
        /// Last instance layout with a non-zero group id from enemy group entry / set list requests.
        /// StageAreaChange resets Character.Stage to (stageId, 0, 0); this keeps the active area group.
        /// </summary>
        public StageLayoutId InstanceLayoutId { get; set; }

        /// <summary>
        /// Full loot list sent immediately after an empty GetDropItemListRes when drop-set
        /// registration had to be applied mid-interaction.
        /// </summary>
        public S2CInstanceGetDropItemListRes? PendingSupplyCacheLootListFollowUp { get; set; }

        /// <summary>
        /// Set after the first PERIODIC_TOP with a valid world position so proximity sync can run once on zone-in.
        /// </summary>
        public bool SupplyCachePositionKnown { get; set; }

        /// <summary>
        /// Adopts a layout from instance load packets. Skips group=0 payloads that would
        /// overwrite a specific area layout (e.g. 1.0.0 preload clobbering active 1.0.1).
        /// </summary>
        public bool TryAdoptInstanceLayout(StageLayoutId candidate)
        {
            if (candidate.Id == 0)
            {
                return false;
            }

            StageLayoutId current = InstanceLayoutId;
            if (current.Id == candidate.Id && current.GroupId != 0 && candidate.GroupId == 0)
            {
                return false;
            }

            InstanceLayoutId = candidate;
            return true;
        }

        private readonly Dictionary<StageLayoutId, S2CInstanceGetEnemySetListRes> _lastEnemySetListResponses = new();

        public void RememberEnemySetListRes(StageLayoutId layout, S2CInstanceGetEnemySetListRes response)
        {
            _lastEnemySetListResponses[layout] = new S2CInstanceGetEnemySetListRes
            {
                LayoutId = response.LayoutId,
                SubGroupId = response.SubGroupId,
                RandomSeed = response.RandomSeed,
                QuestId = response.QuestId,
                EnemyList = new List<CDataLayoutEnemyData>(response.EnemyList),
                DropItemSetList = new List<CDataDropItemSetInfo>(response.DropItemSetList),
                NamedParamList = new List<CDataNamedEnemyParamClient>(response.NamedParamList),
            };
        }

        public bool TryGetLastEnemySetListRes(StageLayoutId layout, out S2CInstanceGetEnemySetListRes response) =>
            _lastEnemySetListResponses.TryGetValue(layout, out response!);

        public S2CInstanceGetEnemySetListRes BuildEnemySetListResWithDropSets(
            StageLayoutId layout,
            List<CDataDropItemSetInfo> dropItemSetList)
        {
            if (TryGetLastEnemySetListRes(layout, out S2CInstanceGetEnemySetListRes cached))
            {
                return new S2CInstanceGetEnemySetListRes
                {
                    LayoutId = cached.LayoutId,
                    SubGroupId = cached.SubGroupId,
                    RandomSeed = cached.RandomSeed,
                    QuestId = cached.QuestId,
                    EnemyList = new List<CDataLayoutEnemyData>(cached.EnemyList),
                    DropItemSetList = dropItemSetList,
                    NamedParamList = new List<CDataNamedEnemyParamClient>(cached.NamedParamList),
                };
            }

            return new S2CInstanceGetEnemySetListRes
            {
                LayoutId = layout.ToCDataStageLayoutId(),
                DropItemSetList = dropItemSetList,
            };
        }

        public void ClearEnemySetListCache(uint mapId)
        {
            foreach (StageLayoutId layout in _lastEnemySetListResponses.Keys.Where(x => x.Id == mapId).ToList())
            {
                _lastEnemySetListResponses.Remove(layout);
            }
        }
    }
}
