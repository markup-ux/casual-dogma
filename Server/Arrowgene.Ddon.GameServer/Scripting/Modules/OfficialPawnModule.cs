using Arrowgene.Ddon.GameServer.Scripting.Interfaces;
using Arrowgene.Ddon.GameServer.Characters;
using Arrowgene.Ddon.Server;
using Arrowgene.Ddon.Shared.Asset;
using Arrowgene.Ddon.Shared.Entity.Structure;
using Arrowgene.Ddon.Shared.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Arrowgene.Ddon.GameServer.Scripting.Modules
{
    public class OfficialPawnModule : GameServerScriptModule
    {
        /// <summary>Reserved pawn id range for generated official pawns.</summary>
        public const uint PawnIdOffset = 1_000_000_000u;

        public override string ModuleRoot => "official_pawns";
        public override string Filter => "*.csx";
        public override bool ScanSubdirectories => true;
        public override bool EnableHotLoad => true;

        private readonly Dictionary<uint, IOfficialPawnScript> _pawnsById = [];
        private readonly Dictionary<string, uint> _pawnIdByFilename = [];
        private readonly object _pawnsLock = new();

        private const string SettingsScript = "official_pawns";

        public bool IsOfficialPawn(uint pawnId)
        {
            lock (_pawnsLock)
            {
                return _pawnsById.ContainsKey(pawnId);
            }
        }

        public IOfficialPawnScript? GetById(uint pawnId)
        {
            lock (_pawnsLock)
            {
                return _pawnsById.GetValueOrDefault(pawnId);
            }
        }

        public IEnumerable<IOfficialPawnScript> GetAll()
        {
            lock (_pawnsLock)
            {
                return _pawnsById.Values.ToList();
            }
        }

        public IEnumerable<IOfficialPawnScript> GetForLevel(int playerLevel)
        {
            lock (_pawnsLock)
            {
                return _pawnsById.Values
                    .Where(p => playerLevel >= p.MinLevel && playerLevel <= p.MaxLevel)
                    .ToList();
            }
        }

        public bool IsAvailableToClient(IOfficialPawnScript script, GameClient client, DdonGameServer server)
        {
            int playerLevel = (int)(client.Character.ActiveCharacterJobData?.Lv ?? 1);
            return playerLevel >= script.MinLevel
                && playerLevel <= script.MaxLevel
                && script.IsUnlocked(client, server);
        }

        /// <summary>Generate a server-owned rental pawn for the hiring character.</summary>
        public RentalPawnRecord Generate(IOfficialPawnScript script, Character hiringCharacter, DdonGameServer server)
        {
            if (script.PawnId == 0)
            {
                throw new InvalidOperationException(
                    $"Official pawn '{script.Name}' has no assigned PawnId. The script must be registered by OfficialPawnModule before generation.");
            }

            int playerLevel = (int)(hiringCharacter.ActiveCharacterJobData?.Lv ?? 1);
            int pawnLevel = Math.Max(1, script.PawnLevel ?? playerLevel);
            int seed = StableRandomSeed.ForOfficialPawn(
                script.PawnId,
                hiringCharacter.CharacterId,
                "script",
                (uint)pawnLevel);
            var rng = new Random(seed);

            var builder = new OfficialPawnBuilder(
                script.Name,
                script.EditInfo,
                script.Job,
                script.PawnId,
                pawnLevel,
                hiringCharacter.CharacterId,
                server.AssetRepository.ClientItemInfos,
                server.AssetRepository.LearnedNormalSkillsAsset,
                server.AssetRepository.SkillData,
                server.AssetRepository.LimitBreakAsset);

            // Use settings-backed recommendations when auto defaults or scripts request them.
            ApplyRecommendedListsFromSettings(builder, script.Job, server);
            builder.WithAutoDefaults(script.Quality);

            var ctx = new OfficialPawnContext
            {
                PlayerLevel = playerLevel,
                PawnLevel = pawnLevel,
                CharacterId = hiringCharacter.CharacterId,
                Rng = rng,
                Builder = builder,
            };

            var record = script.Generate(ctx);

            // Fill remaining crest slots after the pawn script has run.
            ApplyAutoCrests(record, pawnLevel, script.Quality, hiringCharacter.CharacterId, server);

            // Apply automatic limit break unless the script already chose one.
            ApplyAutoLimitBreak(record, pawnLevel, script.Quality, hiringCharacter.CharacterId, server);

            return record;
        }

        private static void ApplyAutoCrests(
            RentalPawnRecord record,
            int playerLevel,
            float pawnQuality,
            uint hiringCharacterId,
            DdonGameServer server)
        {
            pawnQuality = Math.Clamp(pawnQuality, 0f, 1f);
            var itemInfos = server.AssetRepository.ClientItemInfos;
            var performanceItems = record.Equipment.GetItems(EquipType.Performance);

            foreach (EquipSlot slot in Enum.GetValues<EquipSlot>())
            {
                if (slot == EquipSlot.JobItemA || slot == EquipSlot.JobItemB)
                    continue;

                int slotIndex = (byte)slot - 1;
                if (slotIndex < 0 || slotIndex >= performanceItems.Count)
                    continue;

                var item = performanceItems[slotIndex];
                if (item == null)
                    continue;

                if (!itemInfos.TryGetValue((ItemId)item.ItemId, out var info))
                    continue;

                var slotRng = new Random(ComputeStableSeed(
                    record.PawnId,
                    hiringCharacterId,
                    "crests",
                    (uint)playerLevel,
                    (uint)record.Job,
                    (uint)slot,
                    item.ItemId));

                if (IsBitterblackJewelry(item))
                {
                    if (item.EquipElementParamList.Count > 0)
                        continue;

                    int targetBitterblackCrestSlots = RollAutoCrestSlotCount(1, pawnQuality, slotRng);
                    if (targetBitterblackCrestSlots == 0)
                        continue;

                    var bitterblackCrest = RollBitterblackJewelryCrest(item, record.Job, slotRng, server);
                    if (bitterblackCrest != null)
                        item.EquipElementParamList.Add(bitterblackCrest);

                    continue;
                }

                int availableCrestSlots = GetAvailableCrestSlots(slot, info, item);
                if (availableCrestSlots == 0)
                    continue;

                int targetCrestSlots = RollAutoCrestSlotCount(availableCrestSlots, pawnQuality, slotRng);
                int slotsToFill = Math.Max(0, targetCrestSlots - item.EquipElementParamList.Count);
                if (slotsToFill == 0)
                    continue;

                var occupiedSlots = item.EquipElementParamList
                    .Select(x => x.SlotNo)
                    .ToHashSet();

                var selectedCrestIds = SelectCrests(slot, slotsToFill, playerLevel, slotRng, server);
                for (int i = 0; i < selectedCrestIds.Count; i++)
                {
                    byte crestSlotNo = GetNextOpenCrestSlotNo(occupiedSlots, availableCrestSlots);
                    if (crestSlotNo == 0)
                        break;

                    item.EquipElementParamList.Add(new CDataEquipElementParam
                    {
                        SlotNo = crestSlotNo,
                        CrestId = selectedCrestIds[i],
                    });
                    occupiedSlots.Add(crestSlotNo);
                }
            }
        }

        private static bool IsBitterblackJewelry(Item item)
            => item.ItemId == (uint)ItemId.BitterblackBracelet
                || item.ItemId == (uint)ItemId.BitterblackEarring;

        private static CDataEquipElementParam? RollBitterblackJewelryCrest(Item item, JobId job, Random rng, DdonGameServer server)
        {
            try
            {
                if (item.ItemId == (uint)ItemId.BitterblackBracelet)
                {
                    return new CDataEquipElementParam
                    {
                        SlotNo = 1,
                        CrestId = AppraisalManager.RollBitterBlackMazeBraceletCrest(new HashSet<uint>(), rng),
                    };
                }

                if (item.ItemId == (uint)ItemId.BitterblackEarring)
                {
                    var mixin = server.ScriptManager.MixinModule.Get<IBitterblackEarringMixin>("bitterblack_earring");
                    return new CDataEquipElementParam
                    {
                        SlotNo = 1,
                        CrestId = AppraisalManager.RollBitterBlackMazeEarringCrest(new HashSet<uint>(), job, rng),
                        Add = mixin.RollBitterBlackMazeEarringPercent(job, rng),
                    };
                }
            }
            catch (ResponseErrorException ex) when (ex.ErrorCode == ErrorCode.ERROR_CODE_DISPEL_NO_OPTIONS)
            {
                return null;
            }

            return null;
        }

        private static int GetAvailableCrestSlots(EquipSlot slot, ClientItemInfo itemInfo, Item item)
        {
            int nativeCrestSlots = itemInfo.CrestSlots ?? 0;
            int enhancedCrestSlots = nativeCrestSlots + item.PlusValue;

            return Math.Clamp(enhancedCrestSlots, 0, GetMaxCrestSlots(slot));
        }

        private static int GetMaxCrestSlots(EquipSlot slot)
        {
            return slot switch
            {
                EquipSlot.WepMain or EquipSlot.WepSub => 4,
                EquipSlot.ArmorHelm or EquipSlot.ArmorBody or EquipSlot.ArmorArm or EquipSlot.ArmorLeg => 4,
                EquipSlot.Jewelry1 or EquipSlot.Jewelry2 or EquipSlot.Jewelry3 or EquipSlot.Jewelry4 or EquipSlot.Jewelry5 => 4,
                _ => 0,
            };
        }

        private static readonly HashSet<EquipSlot> CrestWeaponSlots = [EquipSlot.WepMain, EquipSlot.WepSub];

        private static readonly HashSet<EquipSlot> CrestJewelrySlots =
        [
            EquipSlot.Jewelry1, EquipSlot.Jewelry2, EquipSlot.Jewelry3,
            EquipSlot.Jewelry4, EquipSlot.Jewelry5,
        ];

        /// <summary>Pick crests from the configured pool for the slot and player level.</summary>
        private static IReadOnlyList<uint> SelectCrests(EquipSlot slot, int crestSlotCount, int playerLevel, Random rng, DdonGameServer server)
        {
            if (crestSlotCount <= 0)
                return [];

            var pool = BuildCrestPool(slot, playerLevel, server);
            if (pool.Count == 0)
                return [];

            // Fisher-Yates shuffle, then take the first N.
            var shuffled = new List<uint>(pool);
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }

            return shuffled.Take(crestSlotCount).ToList();
        }

        private static List<uint> BuildCrestPool(EquipSlot slot, int playerLevel, DdonGameServer server)
        {
            try
            {
                int mid  = server.GameSettings.Get<int>(SettingsScript, "CrestTierMidMinLevel");
                int high = server.GameSettings.Get<int>(SettingsScript, "CrestTierHighMinLevel");
                int max  = server.GameSettings.Get<int>(SettingsScript, "CrestTierMaxMinLevel");

                string tier = playerLevel >= max  ? "Max"
                            : playerLevel >= high ? "High"
                            : playerLevel >= mid  ? "Mid"
                            :                       "Low";

                string category = CrestWeaponSlots.Contains(slot)  ? "Weapon"
                                : CrestJewelrySlots.Contains(slot) ? "Jewelry"
                                :                                    "Armor";

                return server.GameSettings.Get<List<uint>>(SettingsScript, $"{category}Crests{tier}")
                    .Distinct()
                    .ToList();
            }
            catch (Exception ex) when (IsMissingSettingException(ex))
            {
                // Settings file not loaded or a pool key is missing.
                return [];
            }
        }

        private static int RollAutoCrestSlotCount(int availableCrestSlots, float quality, Random rng)
        {
            int[] weights = quality switch
            {
                >= PawnQuality.Legendary => [4],
                >= PawnQuality.Excellent => [3, 4, 4, 4],
                >= PawnQuality.Superior  => [2, 3, 3, 4],
                >= PawnQuality.Good      => [1, 2, 2, 3],
                >= PawnQuality.Normal    => [0, 1, 1, 2],
                >= PawnQuality.Poor      => [0, 0, 0, 1],
                _                        => [0],
            };

            return Math.Min(availableCrestSlots, weights[rng.Next(weights.Length)]);
        }

        private static byte GetNextOpenCrestSlotNo(HashSet<byte> occupiedSlots, int availableCrestSlots)
        {
            for (byte slotNo = 1; slotNo <= availableCrestSlots; slotNo++)
            {
                if (!occupiedSlots.Contains(slotNo))
                    return slotNo;
            }

            return 0;
        }

        public override bool EvaluateResult(string path, object result, IDictionary<string, object> variables)
        {
            if (result is not IOfficialPawnScript script)
                return false;

            string filename = Path.GetFileNameWithoutExtension(path);
            uint pawnId = ComputeStablePawnId(filename);
            lock (_pawnsLock)
            {
                if (_pawnsById.TryGetValue(pawnId, out var existingScript)
                    && (!_pawnIdByFilename.TryGetValue(filename, out uint existingPawnId) || existingPawnId != pawnId))
                {
                    throw new InvalidOperationException(
                        $"Official pawn id collision for '{filename}' and '{existingScript.Name}' using PawnId {pawnId}.");
                }

                script.PawnId = pawnId;
                _pawnsById[pawnId] = script;
                _pawnIdByFilename[filename] = pawnId;
            }

            return true;
        }

        /// <summary>Equipment slots that can receive automatic limit break rolls.</summary>
        private static readonly HashSet<EquipSlot> LimitBreakSlots =
        [
            EquipSlot.WepMain, EquipSlot.WepSub,
            EquipSlot.ArmorHelm, EquipSlot.ArmorBody, EquipSlot.ArmorArm, EquipSlot.ArmorLeg,
        ];

        private static void ApplyAutoLimitBreak(
            RentalPawnRecord record,
            int playerLevel,
            float pawnQuality,
            uint hiringCharacterId,
            DdonGameServer server)
        {
            int unlockLevel = server.GameSettings.Get<int>(SettingsScript, "LimitBreakUnlockLevel");
            if (playerLevel < unlockLevel)
                return;

            int lateGameMinLevel = server.GameSettings.Get<int>(SettingsScript, "LimitBreakLateGameMinLevel");
            pawnQuality = Math.Clamp(pawnQuality, 0f, 1f);
            double chance = GetLimitBreakChance(pawnQuality, playerLevel, lateGameMinLevel);
            if (chance <= 0.0)
                return;

            var performanceItems = record.Equipment.GetItems(EquipType.Performance);

            foreach (EquipSlot slot in LimitBreakSlots)
            {
                int slotIndex = (byte)slot - 1;
                if (slotIndex < 0 || slotIndex >= performanceItems.Count)
                    continue;

                var item = performanceItems[slotIndex];
                if (item == null)
                    continue;

                // Preserve limit break set explicitly by the pawn script.
                if (item.AddStatusParamList.Any(x => x.EnhanceType == EquipEnhanceType.LimitBreak))
                    continue;

                var slotRng = new Random(ComputeStableSeed(
                    record.PawnId,
                    hiringCharacterId,
                    "limit_break",
                    (uint)playerLevel,
                    (uint)record.Job,
                    (uint)slot,
                    item.ItemId));

                if (slotRng.NextDouble() >= chance)
                    continue;

                var preferredStats = GetPreferredLimitBreakStats(server, slot, record.Job);
                ushort enhanceId = RollLimitBreakEnhanceId(
                    server.AssetRepository.LimitBreakAsset,
                    slot,
                    playerLevel,
                    pawnQuality,
                    slotRng,
                    preferredStats);
                if (enhanceId == 0)
                    continue;

                item.AddStatusParamList.Add(new CDataAddStatusParam
                {
                    EnhanceType = EquipEnhanceType.LimitBreak,
                    EnhanceId = enhanceId,
                });
            }
        }

        /// <summary>Per-item limit break probability by quality and level.</summary>
        private static double GetLimitBreakChance(float quality, int level, int lateGameMinLevel)
        {
            bool isLateGame = level >= lateGameMinLevel;

            return quality switch
            {
                >= PawnQuality.Legendary => isLateGame ? 1.00 : 0.90,
                >= PawnQuality.Excellent => isLateGame ? 0.95 : 0.75,
                >= PawnQuality.Superior  => isLateGame ? 0.85 : 0.55,
                >= PawnQuality.Good      => isLateGame ? 0.50 : 0.12,
                >= PawnQuality.Normal    => isLateGame ? 0.15 : 0.00,
                _                        => 0.00,
            };
        }

        private static ushort RollLimitBreakEnhanceId(
            LimitBreakAsset asset,
            EquipSlot slot,
            int playerLevel,
            float quality,
            Random rng,
            IReadOnlyList<string> preferredStats)
        {
            var category = GetLimitBreakCategory(asset, slot);
            if (category == null || category.StatLottery.Count == 0)
                return 0;

            var statPools = SelectLimitBreakStatPools(category, preferredStats, quality, rng);
            if (statPools.Count == 0)
                return 0;

            var statPool = statPools[rng.Next(statPools.Count)];
            return RollLimitBreakId(statPool, playerLevel, quality, rng);
        }

        private static LimitBreakCategory? GetLimitBreakCategory(LimitBreakAsset asset, EquipSlot slot)
        {
            bool isWeapon = slot is EquipSlot.WepMain or EquipSlot.WepSub;
            if (isWeapon)
                return asset.Categories.FirstOrDefault(x => x.ShopListings.Contains(1))
                    ?? asset.Categories.FirstOrDefault();

            return asset.Categories.FirstOrDefault(x => x.ShopListings.Any(listing => listing != 1))
                ?? asset.Categories.Skip(1).FirstOrDefault()
                ?? asset.Categories.FirstOrDefault();
        }

        private static List<LimitStatLottery> SelectLimitBreakStatPools(
            LimitBreakCategory category,
            IReadOnlyList<string> preferredStats,
            float quality,
            Random rng)
        {
            var allPools = category.StatLottery;
            var preferredPools = allPools
                .Where(pool => preferredStats.Contains(pool.Name, StringComparer.OrdinalIgnoreCase))
                .ToList();

            if (preferredPools.Count == 0)
                return allPools;

            double preferredChance = quality switch
            {
                >= PawnQuality.Legendary => 1.00,
                >= PawnQuality.Excellent => 0.95,
                >= PawnQuality.Superior  => 0.85,
                >= PawnQuality.Good      => 0.70,
                >= PawnQuality.Normal    => 0.45,
                >= PawnQuality.Poor      => 0.20,
                _                        => 0.05,
            };

            return rng.NextDouble() < preferredChance ? preferredPools : allPools;
        }

        private static ushort RollLimitBreakId(LimitStatLottery statPool, int playerLevel, float quality, Random rng)
        {
            if (statPool.Rolls.Count == 0)
                return 0;

            int count = statPool.Rolls.Count;
            int greatSuccessIndex = Math.Clamp((int)statPool.MinGreatSuccessIndex, 0, count - 1);
            double levelFactor = Math.Min(1.0, playerLevel / 90.0);
            double rollFactor = Math.Clamp((quality * 0.7) + (levelFactor * 0.3), 0.0, 1.0);

            int lowerIndex = quality switch
            {
                >= PawnQuality.Legendary => greatSuccessIndex,
                >= PawnQuality.Excellent => Math.Max(0, greatSuccessIndex - 1),
                >= PawnQuality.Superior  => Math.Max(0, greatSuccessIndex - 2),
                >= PawnQuality.Good      => count / 3,
                >= PawnQuality.Normal    => 0,
                _                        => 0,
            };

            int upperExclusive = quality switch
            {
                >= PawnQuality.Superior => count,
                >= PawnQuality.Good     => Math.Max(lowerIndex + 1, (int)Math.Ceiling(count * 0.85)),
                >= PawnQuality.Normal   => Math.Max(lowerIndex + 1, (int)Math.Ceiling(count * 0.70)),
                >= PawnQuality.Poor     => Math.Max(lowerIndex + 1, (int)Math.Ceiling(count * 0.55)),
                _                       => Math.Max(lowerIndex + 1, count / 2),
            };

            if (rng.NextDouble() < rollFactor * 0.25)
                lowerIndex = Math.Min(greatSuccessIndex, upperExclusive - 1);

            int rollIndex = rng.Next(lowerIndex, upperExclusive);
            return statPool.Rolls[rollIndex];
        }

        private static IReadOnlyList<string> GetPreferredLimitBreakStats(DdonGameServer server, EquipSlot slot, JobId job)
        {
            string settingName = slot is EquipSlot.WepMain or EquipSlot.WepSub
                ? "PreferredWeaponLimitBreakStats"
                : "PreferredArmorLimitBreakStats";

            var statMap = server.GameSettings.Get<Dictionary<JobId, List<string>>>(SettingsScript, settingName);
            return statMap.TryGetValue(job, out var stats) ? stats : [];
        }

        private static void ApplyRecommendedListsFromSettings(OfficialPawnBuilder builder, JobId job, DdonGameServer server)
        {
            try
            {
                var skillMap = server.GameSettings.Get<Dictionary<JobId, List<CustomSkillId>>>(SettingsScript, "RecommendedCustomSkills");
                if (skillMap.TryGetValue(job, out var skills) && skills?.Count > 0)
                {
                    builder.WithRecommendedCustomSkills(skills);
                }
            }
            catch (Exception ex) when (IsMissingSettingException(ex))
            {
                // Without configured recommendations, the builder uses its generic job fallback.
            }

            try
            {
                var abilityMap = server.GameSettings.Get<Dictionary<JobId, List<AbilityId>>>(SettingsScript, "RecommendedAbilities");
                if (abilityMap.TryGetValue(job, out var abilities) && abilities?.Count > 0)
                {
                    builder.WithRecommendedAbilities(abilities);
                }
            }
            catch (Exception ex) when (IsMissingSettingException(ex))
            {
                // Without configured recommendations, the builder uses its generic job fallback.
            }
        }

        private static bool IsMissingSettingException(Exception ex)
        {
            return ex.Message.Contains("doesn't exist", StringComparison.Ordinal);
        }

        /// <summary>
        /// Hash the script filename into the reserved high-range PawnId space.
        /// Stable across restarts as long as the filename doesn't change.
        /// </summary>
        private static uint ComputeStablePawnId(string scriptName)
        {
            uint hash = StableRandomSeed.HashToUInt32(scriptName);
            return PawnIdOffset + hash % (uint.MaxValue - PawnIdOffset);
        }

        private static int ComputeStableSeed(uint pawnId, uint characterId, string stream, params uint[] discriminators)
            => StableRandomSeed.ForOfficialPawn(pawnId, characterId, stream, discriminators);
    }
}
