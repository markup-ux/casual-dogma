using Arrowgene.Ddon.Shared.Asset;
using Arrowgene.Ddon.Shared.Entity.Structure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Arrowgene.Ddon.Shared.Model
{
    /// <summary>Quality tiers used by official pawn auto-generation.</summary>
    public static class PawnQuality
    {
        public const float Trash     = 0.00f;
        public const float Poor      = 0.20f;
        public const float Normal    = 0.40f;
        public const float Good      = 0.60f;
        public const float Superior  = 0.75f;
        public const float Excellent = 0.85f;
        public const float Legendary = 1.00f;

        /// <summary>Maps quality 0..1 to a rental cost multiplier from 0.5 to 2.0.</summary>
        public static float ToCostMultiplier(float quality) =>
            MathF.Round(0.5f + 1.5f * Math.Clamp(quality, 0f, 1f), 2);
    }

    /// <summary>
    /// Fluent builder that constructs a RentalPawnRecord for an official pawn at a specific player level.
    /// Automatic choices are deterministic from the stable seed supplied by the official pawn module.
    /// </summary>
    public class OfficialPawnBuilder
    {
        /// <summary>Level window used when choosing auto equipment.</summary>
        public const int LevelBand = 15;

        /// <summary>Maximum nearby-candidate window used when choosing equipment within the allowed band.</summary>
        private const float EquipmentQualityWindow = 0.18f;

        /// <summary>Rank cap used to keep low-level auto gear near normal progression.</summary>
        private static int GetMaxAutoEquipmentRank(int playerLevel, float quality)
        {
            int qualityAllowance = quality switch
            {
                >= PawnQuality.Legendary => 18,
                >= PawnQuality.Excellent => 12,
                >= PawnQuality.Superior  => 8,
                >= PawnQuality.Good      => 4,
                >= PawnQuality.Normal    => 2,
                >= PawnQuality.Poor      => 1,
                _                        => 0,
            };

            int progressionAllowance = playerLevel switch
            {
                >= 85 => 15,
                >= 70 => 10,
                >= 50 => 5,
                _     => 0,
            };

            return playerLevel + qualityAllowance + progressionAllowance;
        }

        /// <summary>Custom skills go up to LV.10 in DDON.</summary>
        public const int MaxCustomSkillLevel = 10;

        /// <summary>Abilities go up to LV.6 in DDON.</summary>
        public const int MaxAbilityLevel = 6;

        /// <summary>Maximum number of ability slots shown in the pawn UI.</summary>
        public const int MaxAbilitySlots = 8;

        /// <summary>Maximum primary crafting skill level supported by the normal crafting formulas.</summary>
        public const uint MaxCraftSkillLevel = 70;

        /// <summary>Maximum primary crafting skill level accepted by manual script helpers.</summary>
        public const uint MaxCraftSkillDisplayLevel = MaxCraftSkillLevel + 1;

        /// <summary>Maximum craft rank used by official pawn auto-generation.</summary>
        public const uint MaxCraftRank = 71;

        /// <summary>Player level where auto skill and ability levels reach their cap.</summary>
        private const int MaxSkillPlayerLevel = 90;

        private readonly int _playerLevel;
        private readonly JobId _job;
        private readonly uint _hiringCharacterId;
        private readonly ClientItemInfoAsset _itemInfos;
        private readonly LearnedNormalSkillsAsset _normalSkillsAsset;
        private readonly SkillDataAsset _skillData;
        private readonly LimitBreakAsset _limitBreakAsset;

        // Recommended lists can be overridden by the official_pawns settings script.
        private IReadOnlyList<CustomSkillId> _recommendedCustomSkills;
        private IReadOnlyList<AbilityId> _recommendedAbilities;

        private readonly string _name;
        private readonly CDataEditInfo _editInfo;
        private readonly uint _pawnId;

        private readonly Storage _equipStorage;
        private readonly Equipment _equipment;

        private readonly List<CustomSkill?> _customSkills = [null, null, null, null];
        private readonly List<Ability?> _abilities = new(Enumerable.Repeat<Ability?>(null, MaxAbilitySlots));
        private List<CDataNormalSkillParam> _normalSkills = [];

        private CDataCharacterJobData _jobData;
        private CDataOrbGainExtendParam _extendedParams = new();
        private CDataPawnCraftData _craftData = BuildDefaultCraftData(0);
        private float? _autoDefaultQuality;
        private float? _autoEquipmentQuality;
        private float? _autoCustomSkillsQuality;
        private float? _autoAbilitiesQuality;
        private float? _autoCraftQuality;
        private bool _explicitCraftData;

        private readonly HashSet<(EquipType Type, EquipSlot Slot)> _explicitEquipmentSlots = [];
        private readonly Dictionary<(EquipType Type, EquipSlot Slot), ushort> _limitBreakOverrides = [];
        private readonly bool[] _explicitCustomSkillSlots = new bool[4];
        private readonly bool[] _explicitAbilitySlots = new bool[MaxAbilitySlots];

        public OfficialPawnBuilder(
            string name,
            CDataEditInfo editInfo,
            JobId job,
            uint pawnId,
            int playerLevel,
            uint hiringCharacterId,
            ClientItemInfoAsset itemInfos,
            LearnedNormalSkillsAsset normalSkillsAsset,
            SkillDataAsset skillData,
            LimitBreakAsset limitBreakAsset)
        {
            _name = name;
            _editInfo = editInfo;
            _job = job;
            _pawnId = pawnId;
            _playerLevel = playerLevel;
            _hiringCharacterId = hiringCharacterId;
            _itemInfos = itemInfos;
            _normalSkillsAsset = normalSkillsAsset;
            _skillData = skillData;
            _limitBreakAsset = limitBreakAsset;
            _recommendedCustomSkills = [];
            _recommendedAbilities    = [];

            // 15 performance slots + 15 visual slots
            _equipStorage = new Storage(StorageType.PawnEquipment, (ushort)(EquipmentTemplate.TOTAL_EQUIP_SLOTS * 2));
            _equipment = new Equipment(_equipStorage, 0);

            _jobData = BuildDefaultJobData(job, playerLevel);

            // Extended params scale with player level. A level 104 pawn reaches
            // five jewelry slots and enough ability cost for a full augment set.
            _extendedParams = new CDataOrbGainExtendParam
            {
                JewelrySlot  = (ushort)Math.Min(4, playerLevel / 25),
                AbilityCost  = (ushort)Math.Min(130, (int)(playerLevel * 1.25)),
            };

            WithAutoNormalSkills();
        }

        /// <summary>Apply automatic defaults for categories the script does not configure.</summary>
        public OfficialPawnBuilder WithAutoDefaults(float quality)
        {
            _autoDefaultQuality = Math.Clamp(quality, 0f, 1f);
            return this;
        }

        // Equipment

        /// <summary>Set a specific item in an equipment slot by raw item id.</summary>
        public OfficialPawnBuilder WithEquipmentForSlot(
            EquipSlot slot,
            uint itemId,
            EquipType type = EquipType.Performance,
            byte plusValue = 0,
            params uint[] crestIds)
        {
            int typeOffset = type == EquipType.Performance ? 0 : EquipmentTemplate.TOTAL_EQUIP_SLOTS;
            ushort storageSlot = (ushort)(typeOffset + (byte)slot);

            var crests = BuildCrests(crestIds);
            var item = new Item { ItemId = itemId, PlusValue = plusValue };
            item.EquipElementParamList.AddRange(crests);

            _equipStorage.SetItem(item, 1, storageSlot);
            _explicitEquipmentSlots.Add((type, slot));
            return this;
        }

        /// <summary>Set a specific raw item id with named crest ids.</summary>
        public OfficialPawnBuilder WithEquipmentForSlot(
            EquipSlot slot,
            uint itemId,
            EquipType type = EquipType.Performance,
            byte plusValue = 0,
            params ItemId[] crestIds)
            => WithEquipmentForSlot(slot, itemId, type, plusValue, crestIds.Select(id => (uint)id).ToArray());

        /// <summary>Set a specific named item with raw crest ids.</summary>
        public OfficialPawnBuilder WithEquipmentForSlot(
            EquipSlot slot,
            ItemId itemId,
            EquipType type = EquipType.Performance,
            byte plusValue = 0,
            params uint[] crestIds)
            => WithEquipmentForSlot(slot, (uint)itemId, type, plusValue, crestIds);

        /// <summary>Set a specific named item with named crest ids.</summary>
        public OfficialPawnBuilder WithEquipmentForSlot(
            EquipSlot slot,
            ItemId itemId,
            EquipType type = EquipType.Performance,
            byte plusValue = 0,
            params ItemId[] crestIds)
            => WithEquipmentForSlot(slot, (uint)itemId, type, plusValue, crestIds.Select(id => (uint)id).ToArray());

        /// <summary>
        /// Auto-select equipment for the pawn's job and player level.
        /// Quality controls item position within the allowed level/rank band and
        /// the enhancement value.
        /// </summary>
        public OfficialPawnBuilder WithAutoEquipment(float quality = PawnQuality.Normal)
        {
            _autoEquipmentQuality = Math.Clamp(quality, 0f, 1f);
            return this;
        }

        private void ApplyAutoEquipment(float quality)
        {
            quality = Math.Clamp(quality, 0f, 1f);
            int maxAutoRank = GetMaxAutoEquipmentRank(_playerLevel, quality);
            var usedJewelry = new HashSet<ItemId>();
            var usedJewelryNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var usedJewelrySubCategoryCounts = new Dictionary<ItemSubCategory, int>();

            TrackExplicitJewelry(usedJewelry, usedJewelryNames, usedJewelrySubCategoryCounts);

            foreach (EquipSlot slot in Enum.GetValues<EquipSlot>())
            {
                if (slot == EquipSlot.JobItemA || slot == EquipSlot.JobItemB)
                    continue;

                if (_explicitEquipmentSlots.Contains((EquipType.Performance, slot)))
                    continue;

                if (IsJewelrySlot(slot) && slot > GetMaxUnlockedJewelrySlot())
                    continue;

                var itemRng = CreateStableRandom("equipment_item", (uint)slot);

                var allValid = _itemInfos.Values
                    .Where(i =>
                        IsValidForSlot(i, slot) &&
                        !IsExcludedFromAutoEquipment(i) &&
                        (i.Level == null || i.Level <= _playerLevel) &&
                        (i.JobIds == null || i.JobIds.Contains(_job)))
                    .ToList();

                if (allValid.Count == 0)
                    continue;

                var rankValid = allValid
                    .Where(i => i.Rank <= maxAutoRank)
                    .ToList();

                // Sparse early slots can miss the rank cap. Fall back to the
                // lowest-rank valid item rather than leaving the slot empty.
                if (rankValid.Count == 0)
                {
                    int lowestRank = allValid.Min(i => i.Rank);
                    rankValid = allValid
                        .Where(i => i.Rank == lowestRank)
                        .ToList();
                }

                if (slot == EquipSlot.Lantern)
                {
                    var lantern = rankValid
                        .OrderBy(i => i.ItemId)
                        .ElementAt(itemRng.Next(rankValid.Count));
                    ushort lanternStorageSlot = (ushort)(byte)slot;
                    _equipStorage.SetItem(new Item { ItemId = (uint)lantern.ItemId }, 1, lanternStorageSlot);
                    continue;
                }

                // Only consider items within LevelBand of the best available level after
                // applying the IR/rank cap.
                // Keep the result near the player's progression band.
                int maxLevel = rankValid.Max(i => i.Level ?? 0);
                var candidates = IsJewelrySlot(slot)
                    ? rankValid
                        .OrderByDescending(i => i.Rank)
                        .ThenByDescending(i => i.Quality ?? 0)
                        .ThenByDescending(i => i.Level ?? 0)
                        .ThenBy(i => i.ItemId)
                        .ToList()
                    : rankValid
                        .Where(i => (i.Level ?? 0) >= maxLevel - LevelBand)
                        .OrderByDescending(i => i.Rank)
                        .ThenByDescending(i => i.Quality ?? 0)
                        .ThenByDescending(i => i.Level ?? 0)
                        .ThenBy(i => i.ItemId)
                        .ToList();

                if (IsJewelrySlot(slot))
                {
                    candidates = PreferJewelryVariety(
                        candidates,
                        slot,
                        usedJewelry,
                        usedJewelryNames,
                        usedJewelrySubCategoryCounts);
                }

                int pickIndex = RollEquipmentCandidateIndex(candidates.Count, quality, itemRng);

                // Deterministic tie-breaking within the same rank.
                int pivot = pickIndex;
                while (pivot > 0 &&
                    candidates[pivot - 1].Rank == candidates[pickIndex].Rank &&
                    candidates[pivot - 1].Quality == candidates[pickIndex].Quality &&
                    candidates[pivot - 1].Level == candidates[pickIndex].Level)
                {
                    pivot--;
                }
                int tieCount = candidates
                    .Skip(pivot)
                    .TakeWhile(c =>
                        c.Rank == candidates[pickIndex].Rank &&
                        c.Quality == candidates[pickIndex].Quality &&
                        c.Level == candidates[pickIndex].Level)
                    .Count();
                int chosen = pivot + itemRng.Next(tieCount);

                var info = candidates[chosen];
                byte plusValue = CanAutoEnhance(slot, info)
                    ? RollPlusValue(quality, CreateStableRandom("equipment_plus", (uint)slot, (uint)info.ItemId))
                    : (byte)0;
                if (IsJewelrySlot(slot))
                {
                    usedJewelry.Add(info.ItemId);
                    usedJewelryNames.Add(NormalizeJewelryName(info.Name));
                    usedJewelrySubCategoryCounts[info.SubCategory] =
                        usedJewelrySubCategoryCounts.GetValueOrDefault(info.SubCategory) + 1;
                }

                ushort storageSlot = (ushort)(byte)slot;
                _equipStorage.SetItem(new Item { ItemId = (uint)info.ItemId, PlusValue = plusValue }, 1, storageSlot);
            }
        }

        // Limit break

        /// <summary>
        /// Add a limit break effect to an equipped item by enhance id.
        /// Enhance ids come from LimitBreak.json.
        /// </summary>
        public OfficialPawnBuilder WithLimitBreak(EquipSlot slot, ushort enhanceId, EquipType type = EquipType.Performance)
        {
            if (enhanceId == 0)
                return this;

            _limitBreakOverrides[(type, slot)] = enhanceId;
            return this;
        }

        /// <summary>Apply the best limit break roll for the named stat.</summary>
        public OfficialPawnBuilder WithLimitBreak(EquipSlot slot, string statName, EquipType type = EquipType.Performance)
        {
            ushort enhanceId = ResolveLimitBreakEnhanceId(slot, statName);
            return WithLimitBreak(slot, enhanceId, type);
        }

        // Normal skills

        /// <summary>Auto-unlock all normal skills where RequiredLevel is met.</summary>
        public OfficialPawnBuilder WithAutoNormalSkills()
        {
            _normalSkills = [];

            if (!_normalSkillsAsset.LearnedNormalSkills.TryGetValue(_job, out var jobSkills))
                return this;

            for (int i = 0; i < jobSkills.Count; i++)
            {
                var skill = jobSkills[i];
                if (skill.RequiredLevel > _playerLevel)
                    continue;

                foreach (uint skillNo in skill.SkillNo)
                {
                    _normalSkills.Add(new CDataNormalSkillParam
                    {
                        Job = _job,
                        Index = (uint)(i + 1),
                        SkillNo = skillNo,
                        PreSkillNo = 0,
                    });
                }
            }

            return this;
        }

        // Custom skills

        /// <summary>Set a custom skill in a specific slot, 1 through 4.</summary>
        public OfficialPawnBuilder WithCustomSkill(int slot, CustomSkillId skillId, byte level = 1)
        {
            if (slot < 1 || slot > 4) throw new ArgumentOutOfRangeException(nameof(slot));
            _customSkills[slot - 1] = new CustomSkill(skillId, level);
            _explicitCustomSkillSlots[slot - 1] = true;
            return this;
        }

        /// <summary>Set a custom skill only when the player is at least minLevel.</summary>
        public OfficialPawnBuilder WithCustomSkillAtLevel(int slot, CustomSkillId skillId, int minLevel, byte level = 1)
        {
            if (_playerLevel >= minLevel)
                WithCustomSkill(slot, skillId, level);
            return this;
        }

        /// <summary>Set a custom skill from the highest level tier the player meets.</summary>
        public OfficialPawnBuilder WithCustomSkillForLevels(
            int slot,
            params (int MinLevel, CustomSkillId SkillId, byte Level)[] tiers)
        {
            var match = tiers
                .Where(t => _playerLevel >= t.MinLevel)
                .OrderByDescending(t => t.MinLevel)
                .FirstOrDefault();

            if (match != default)
                WithCustomSkill(slot, match.SkillId, match.Level);
            return this;
        }

        /// <summary>Override the recommended custom skill list for this pawn's job.</summary>
        public OfficialPawnBuilder WithRecommendedCustomSkills(IReadOnlyList<CustomSkillId> skills)
        {
            _recommendedCustomSkills = skills;
            return this;
        }

        /// <summary>Set a custom skill by raw release id in the given slot.</summary>
        public OfficialPawnBuilder WithCustomSkillRaw(int slot, uint releaseId, byte level = 1)
        {
            if (slot < 1 || slot > 4) throw new ArgumentOutOfRangeException(nameof(slot));
            _customSkills[slot - 1] = new CustomSkill { Job = _job, SkillId = releaseId, SkillLv = level };
            _explicitCustomSkillSlots[slot - 1] = true;
            return this;
        }

        /// <summary>Auto-fill custom skill slots with recommended skills first.</summary>
        public OfficialPawnBuilder WithAutoCustomSkills(float quality = 1.0f)
        {
            _autoCustomSkillsQuality = Math.Clamp(quality, 0f, 1f);
            return this;
        }

        private void ApplyAutoCustomSkills(float quality)
        {
            quality = Math.Clamp(quality, 0f, 1f);
            float playerFactor = Math.Min(1.0f, _playerLevel / (float)MaxSkillPlayerLevel);
            var explicitReleaseIds = _customSkills
                .Where(skill => skill != null)
                .Select(skill => skill!.SkillId)
                .ToHashSet();

            var jobBaseSkills = BuildAutoCustomSkillCandidates(quality, playerFactor)
                .Where(skill => !explicitReleaseIds.Contains(skill.SkillId))
                .ToList();

            int candidateIndex = 0;
            for (int slot = 0; slot < _customSkills.Count && candidateIndex < jobBaseSkills.Count; slot++)
            {
                if (_explicitCustomSkillSlots[slot])
                    continue;

                _customSkills[slot] = jobBaseSkills[candidateIndex++];
            }
        }

        // Abilities

        /// <summary>Set an ability in a specific slot.</summary>
        public OfficialPawnBuilder WithAbility(int slot, AbilityId abilityId, byte level = 1)
        {
            if (slot < 1 || slot > MaxAbilitySlots) throw new ArgumentOutOfRangeException(nameof(slot));
            _abilities[slot - 1] = new Ability { AbilityId = abilityId, AbilityLv = level };
            _explicitAbilitySlots[slot - 1] = true;
            return this;
        }

        /// <summary>Set an ability only when the player is at least minLevel.</summary>
        public OfficialPawnBuilder WithAbilityAtLevel(int slot, AbilityId abilityId, int minLevel, byte level = 1)
        {
            if (slot < 1 || slot > MaxAbilitySlots) throw new ArgumentOutOfRangeException(nameof(slot));
            if (_playerLevel >= minLevel)
                WithAbility(slot, abilityId, level);
            return this;
        }

        /// <summary>Set an ability from the highest level tier the player meets.</summary>
        public OfficialPawnBuilder WithAbilityForLevels(
            int slot,
            params (int MinLevel, AbilityId AbilityId, byte Level)[] tiers)
        {
            var match = tiers
                .Where(t => _playerLevel >= t.MinLevel)
                .OrderByDescending(t => t.MinLevel)
                .FirstOrDefault();

            if (match != default)
                WithAbility(slot, match.AbilityId, match.Level);
            return this;
        }

        /// <summary>Override the recommended ability list for this pawn's job.</summary>
        public OfficialPawnBuilder WithRecommendedAbilities(IReadOnlyList<AbilityId> abilities)
        {
            _recommendedAbilities = abilities;
            return this;
        }

        /// <summary>Auto-fill ability slots within the pawn's ability cost budget.</summary>
        public OfficialPawnBuilder WithAutoAbilities(float quality = 1.0f)
        {
            _autoAbilitiesQuality = Math.Clamp(quality, 0f, 1f);
            return this;
        }

        private void ApplyAutoAbilities(float quality)
        {
            quality = Math.Clamp(quality, 0f, 1f);
            float playerFactor = Math.Min(1.0f, _playerLevel / (float)MaxSkillPlayerLevel);
            int slotCount = GetAutoAbilitySlotCount(quality);

            uint abilityCostBudget = CharacterCommon.BASE_ABILITY_COST_AMOUNT + _extendedParams.AbilityCost;
            uint abilityCostUsed = 0;
            int filledSlots = 0;
            var usedAbilityIds = new HashSet<AbilityId>();

            for (int i = 0; i < _abilities.Count; i++)
            {
                var ability = _abilities[i];
                if (_explicitAbilitySlots[i] && ability != null)
                {
                    abilityCostUsed += GetAbilityCost(ability.AbilityId);
                    filledSlots++;
                    usedAbilityIds.Add(ability.AbilityId);
                }
                else
                {
                    _abilities[i] = null;
                }
            }

            foreach (var ability in BuildAutoAbilityCandidates(quality, playerFactor))
            {
                if (filledSlots >= slotCount)
                    break;

                if (!usedAbilityIds.Add(ability.AbilityId))
                    continue;

                uint abilityCost = GetAbilityCost(ability.AbilityId);
                if (abilityCostUsed > abilityCostBudget || abilityCost > abilityCostBudget - abilityCostUsed)
                    continue;

                int openSlot = FindOpenAutoAbilitySlot();
                if (openSlot < 0)
                    break;

                _abilities[openSlot] = ability;
                abilityCostUsed += abilityCost;
                filledSlots++;
            }
        }

        // Stats

        /// <summary>Override the pawn's base job stats.</summary>
        public OfficialPawnBuilder WithJobData(CDataCharacterJobData jobData)
        {
            _jobData = jobData;
            return this;
        }

        /// <summary>Override the pawn's extended (orb-tree) stat bonuses.</summary>
        public OfficialPawnBuilder WithExtendedParams(CDataOrbGainExtendParam extendedParams)
        {
            _extendedParams = extendedParams;
            return this;
        }

        // Craft

        /// <summary>Auto-scale pawn crafting rank and craft skills by quality.</summary>
        public OfficialPawnBuilder WithAutoCraft(float quality = PawnQuality.Normal)
        {
            _autoCraftQuality = Math.Clamp(quality, 0f, 1f);
            _explicitCraftData = false;
            return this;
        }

        /// <summary>Replace the pawn's crafting data with an explicit hand-authored value.</summary>
        public OfficialPawnBuilder WithCraftData(CDataPawnCraftData craftData)
        {
            _craftData = craftData ?? throw new ArgumentNullException(nameof(craftData));
            EnsureAllCraftSkills(_craftData);
            ClampCraftSkillLevels(_craftData);
            _explicitCraftData = true;
            return this;
        }

        /// <summary>Set explicit crafting rank metadata and optional crafting skill levels.</summary>
        public OfficialPawnBuilder WithCraft(
            uint craftRank,
            uint craftRankLimit = MaxCraftRank,
            uint craftPoint = 0,
            uint craftExp = 0,
            IReadOnlyDictionary<CraftSkillType, uint>? skillLevels = null)
        {
            _craftData = BuildDefaultCraftData(ToStoredCraftSkillLevels(skillLevels ?? new Dictionary<CraftSkillType, uint>()));
            _craftData.CraftRank = Math.Clamp(craftRank, 1, MaxCraftRank);
            _craftData.CraftRankLimit = Math.Clamp(craftRankLimit, _craftData.CraftRank, MaxCraftRank);
            _craftData.CraftPoint = craftPoint;
            _craftData.CraftExp = craftExp;
            _explicitCraftData = true;
            return this;
        }

        /// <summary>Set one explicit crafting skill level while preserving the rest of the current craft data.</summary>
        public OfficialPawnBuilder WithCraftSkill(CraftSkillType type, uint level)
        {
            EnsureAllCraftSkills(_craftData);
            CDataPawnCraftSkill? skill = _craftData.PawnCraftSkillList.FirstOrDefault(skill => skill.Type == type);
            if (skill == null)
            {
                skill = new CDataPawnCraftSkill { Type = type };
                _craftData.PawnCraftSkillList.Add(skill);
            }

            skill.Level = IsPrimaryCraftSkill(type)
                ? ToStoredCraftSkillLevel(level)
                : level;
            _explicitCraftData = true;
            return this;
        }

        private void ApplyAutoCraft(float quality)
        {
            quality = Math.Clamp(quality, 0f, 1f);
            float playerFactor = Math.Min(1.0f, _playerLevel / (float)MaxSkillPlayerLevel);
            byte craftRank = RollAutoCraftRank(
                quality,
                playerFactor,
                CreateStableRandom("craft_rank"));
            var craftSkillLevels = RollAutoCraftSkillDistribution(
                craftRank,
                quality,
                CreateStableRandom("craft_skills"));
            _craftData = BuildDefaultCraftData(craftSkillLevels);
            _craftData.CraftRank = craftRank;
        }

        // Build

        public RentalPawnRecord Build()
        {
            ApplyAutoConfiguration();

            return new RentalPawnRecord
            {
                PawnId = _pawnId,
                CharacterId = Character.ServerCharacterId,
                CommonId = _pawnId,
                Name = _name,
                IsOfficialPawn = true,
                EditInfo = _editInfo,
                Job = _job,
                HideEquipHead = false,
                HideEquipLantern = false,
                CharacterJobData = _jobData,
                Equipment = _equipment,
                LearnedNormalSkills = _normalSkills,
                EquippedCustomSkills = _customSkills,
                EquippedAbilities = _abilities,
                ExtendedParams = _extendedParams,
                HireDate = DateTime.Now,
                CraftData = _craftData,
                PawnReactionList = [],
                TrainingStatus = new byte[64],
                SpSkills = [],
                PawnProfile = new CharacterProfile(),
            };
        }

        // Helpers

        private Random CreateStableRandom(string stream, params uint[] discriminators)
        {
            uint[] seedParts = new uint[2 + discriminators.Length];
            seedParts[0] = (uint)_playerLevel;
            seedParts[1] = (uint)_job;
            Array.Copy(discriminators, 0, seedParts, 2, discriminators.Length);

            return new Random(StableRandomSeed.ForOfficialPawn(_pawnId, _hiringCharacterId, stream, seedParts));
        }

        private static int RollEquipmentCandidateIndex(int candidateCount, float quality, Random rng)
        {
            if (candidateCount <= 1)
                return 0;

            if (quality >= PawnQuality.Legendary)
                return 0;

            if (quality <= PawnQuality.Trash)
                return candidateCount - 1;

            float edgeDistance = Math.Min(quality, 1.0f - quality);
            float varietyScale = MathF.Max(0.35f, edgeDistance * 2.0f);
            float idealIndex = (1.0f - quality) * (candidateCount - 1);
            int radius = Math.Max(1, (int)Math.Round(candidateCount * EquipmentQualityWindow * varietyScale));
            int minIndex = Math.Clamp((int)Math.Floor(idealIndex) - radius, 0, candidateCount - 1);
            int maxIndex = Math.Clamp((int)Math.Ceiling(idealIndex) + radius, 0, candidateCount - 1);

            return rng.Next(minIndex, maxIndex + 1);
        }

        private void ApplyAutoConfiguration()
        {
            if ((_autoEquipmentQuality ?? _autoDefaultQuality) is float equipmentQuality)
                ApplyAutoEquipment(equipmentQuality);

            if ((_autoCustomSkillsQuality ?? _autoDefaultQuality) is float customSkillsQuality)
                ApplyAutoCustomSkills(customSkillsQuality);

            if ((_autoAbilitiesQuality ?? _autoDefaultQuality) is float abilitiesQuality)
                ApplyAutoAbilities(abilitiesQuality);

            if (!_explicitCraftData && (_autoCraftQuality ?? _autoDefaultQuality) is float craftQuality)
                ApplyAutoCraft(craftQuality);

            ApplyLimitBreakOverrides();
        }

        private void ApplyLimitBreakOverrides()
        {
            foreach (var entry in _limitBreakOverrides)
            {
                var (type, slot) = entry.Key;
                ushort enhanceId = entry.Value;
                int typeOffset = type == EquipType.Performance ? 0 : EquipmentTemplate.TOTAL_EQUIP_SLOTS;
                ushort storageSlot = (ushort)(typeOffset + (byte)slot);

                if (_equipStorage.GetItem(storageSlot)?.Item1 is not Item item)
                    continue;

                var limitBreak = item.AddStatusParamList
                    .Find(x => x.EnhanceType == EquipEnhanceType.LimitBreak);

                if (limitBreak is null)
                {
                    limitBreak = new CDataAddStatusParam
                    {
                        EnhanceType = EquipEnhanceType.LimitBreak,
                    };
                    item.AddStatusParamList.Add(limitBreak);
                }

                limitBreak.EnhanceId = enhanceId;
            }
        }

        private ushort ResolveLimitBreakEnhanceId(EquipSlot slot, string statName)
        {
            var category = GetLimitBreakCategory(slot)
                ?? throw new InvalidOperationException($"No limit break category is available for {slot}.");

            var statPool = category.StatLottery
                .FirstOrDefault(x => string.Equals(x.Name, statName, StringComparison.OrdinalIgnoreCase));

            if (statPool == null)
                throw new ArgumentException($"Unknown limit break stat '{statName}' for {slot}.", nameof(statName));

            if (statPool.Rolls.Count == 0)
                throw new InvalidOperationException($"Limit break stat '{statName}' has no rolls.");

            return statPool.Rolls[^1];
        }

        private LimitBreakCategory? GetLimitBreakCategory(EquipSlot slot)
        {
            bool isWeapon = slot is EquipSlot.WepMain or EquipSlot.WepSub;
            if (isWeapon)
                return _limitBreakAsset.Categories.FirstOrDefault(x => x.ShopListings.Contains(1))
                    ?? _limitBreakAsset.Categories.FirstOrDefault();

            return _limitBreakAsset.Categories.FirstOrDefault(x => x.ShopListings.Any(listing => listing != 1))
                ?? _limitBreakAsset.Categories.Skip(1).FirstOrDefault()
                ?? _limitBreakAsset.Categories.FirstOrDefault();
        }

        private static List<CDataEquipElementParam> BuildCrests(uint[] crestIds)
        {
            var result = new List<CDataEquipElementParam>();
            for (int i = 0; i < Math.Min(crestIds.Length, 4); i++)
            {
                if (crestIds[i] == 0) continue;
                result.Add(new CDataEquipElementParam { SlotNo = (byte)(i + 1), CrestId = crestIds[i] });
            }
            return result;
        }

        private static bool IsJewelrySlot(EquipSlot slot)
            => slot is EquipSlot.Jewelry1 or EquipSlot.Jewelry2 or EquipSlot.Jewelry3
                or EquipSlot.Jewelry4 or EquipSlot.Jewelry5;

        private static bool CanAutoEnhance(EquipSlot slot, ClientItemInfo itemInfo)
            => slot != EquipSlot.Lantern && !IsBitterblackEquipment(itemInfo);

        private static bool IsBitterblackEquipment(ClientItemInfo itemInfo)
            => itemInfo.Name?.Contains("Bitterblack", StringComparison.OrdinalIgnoreCase) == true
                || itemInfo.Name?.Contains("Bitter Black", StringComparison.OrdinalIgnoreCase) == true
                || itemInfo.ItemId.ToString().Contains("Bitterblack", StringComparison.OrdinalIgnoreCase);

        private static bool IsExcludedFromAutoEquipment(ClientItemInfo itemInfo)
        {
            // Rusted weapons have misleadingly low IR/equip levels but can carry very high
            // hidden attack values. Keep them available for explicit script/admin assignment,
            // but do not auto-pick them for rental pawns.
            return itemInfo.Name?.Contains("Rusted", StringComparison.OrdinalIgnoreCase) == true
                || itemInfo.ItemId.ToString().Contains("Rusted", StringComparison.OrdinalIgnoreCase);
        }

        private EquipSlot GetMaxUnlockedJewelrySlot()
            => (EquipSlot)((byte)EquipSlot.Jewelry1 + Math.Clamp(_extendedParams.JewelrySlot, (ushort)0, (ushort)4));

        private static bool IsValidForSlot(ClientItemInfo itemInfo, EquipSlot slot)
        {
            if (itemInfo.EquipSlot == slot)
                return true;

            return IsJewelrySlot(slot) && itemInfo.EquipSlot == EquipSlot.Jewelry1;
        }

        private void TrackExplicitJewelry(
            HashSet<ItemId> usedItemIds,
            HashSet<string> usedNames,
            Dictionary<ItemSubCategory, int> usedSubCategoryCounts)
        {
            foreach (EquipSlot slot in Enum.GetValues<EquipSlot>())
            {
                if (!IsJewelrySlot(slot))
                    continue;

                var item = _equipStorage.GetItem((ushort)(byte)slot)?.Item1;
                if (item == null || !_itemInfos.TryGetValue((ItemId)item.ItemId, out var info))
                    continue;

                usedItemIds.Add(info.ItemId);
                usedNames.Add(NormalizeJewelryName(info.Name));
                usedSubCategoryCounts[info.SubCategory] =
                    usedSubCategoryCounts.GetValueOrDefault(info.SubCategory) + 1;
            }
        }

        private List<CustomSkill> BuildAutoCustomSkillCandidates(float quality, float playerFactor)
        {
            var result = new List<CustomSkill>();
            var usedReleaseIds = new HashSet<uint>();

            foreach (var skillId in _recommendedCustomSkills)
                TryAddAutoCustomSkill(result, usedReleaseIds, skillId, quality, playerFactor);

            foreach (var skillId in Enum.GetValues<CustomSkillId>().Where(id => id != CustomSkillId.None))
                TryAddAutoCustomSkill(result, usedReleaseIds, skillId, quality, playerFactor);

            return result;
        }

        private void TryAddAutoCustomSkill(
            List<CustomSkill> result,
            HashSet<uint> usedReleaseIds,
            CustomSkillId skillId,
            float quality,
            float playerFactor)
        {
            uint releaseId = skillId.ReleaseId();
            byte targetSkillLevel = RollAutoCustomSkillLevel(
                quality,
                playerFactor,
                CreateStableRandom("custom_skill_level", releaseId));
            if (!TryGetAutoCustomSkillLevel(skillId, targetSkillLevel, out byte skillLevel))
                return;

            try
            {
                var skill = new CustomSkill(skillId, skillLevel);
                if (skill.Job != _job || !usedReleaseIds.Add(skill.SkillId))
                    return;

                result.Add(skill);
            }
            catch
            {
                // Some enum values may not have complete release/job mappings.
            }
        }

        private bool TryGetAutoCustomSkillLevel(CustomSkillId skillId, byte targetSkillLevel, out byte skillLevel)
        {
            skillLevel = 0;
            uint releaseId = skillId.ReleaseId();
            if (releaseId == 0)
                return false;

            // Keep auto-selection on base skills for now. EX/promoted variants can still
            // be assigned explicitly in an official pawn script.
            if (releaseId > 14)
                return false;

            var skillData = _skillData.GetSkill(_job, releaseId);
            if (skillData?.Params == null || skillData.Params.Count == 0)
                return false;

            var highestAvailable = skillData.Params
                .Where(param => param.Lv <= targetSkillLevel && param.RequireJobLevel <= _playerLevel)
                .OrderByDescending(param => param.Lv)
                .FirstOrDefault();

            if (highestAvailable == null)
                return false;

            skillLevel = highestAvailable.Lv;
            return true;
        }

        private List<Ability> BuildAutoAbilityCandidates(float quality, float playerFactor)
        {
            var result = new List<Ability>();
            var usedAbilityIds = new HashSet<AbilityId>();

            foreach (var abilityId in _recommendedAbilities)
                TryAddAutoAbility(result, usedAbilityIds, abilityId, quality, playerFactor);

            foreach (var abilityId in Enum.GetValues<AbilityId>().Where(id => id != AbilityId.None))
            {
                if (abilityId.JobId() == _job)
                    TryAddAutoAbility(result, usedAbilityIds, abilityId, quality, playerFactor);
            }

            return result;
        }

        private uint GetAbilityCost(AbilityId abilityId)
            => _skillData.GetAbility(abilityId)?.Cost ?? 0;

        private int FindOpenAutoAbilitySlot()
        {
            for (int i = 0; i < _abilities.Count; i++)
            {
                if (!_explicitAbilitySlots[i] && _abilities[i] == null)
                    return i;
            }

            return -1;
        }

        private void TryAddAutoAbility(
            List<Ability> result,
            HashSet<AbilityId> usedAbilityIds,
            AbilityId abilityId,
            float quality,
            float playerFactor)
        {
            if (abilityId == AbilityId.None || !usedAbilityIds.Add(abilityId))
                return;

            byte abilityLevel = RollAutoAbilityLevel(
                quality,
                playerFactor,
                CreateStableRandom("ability_level", (uint)abilityId));
            result.Add(new Ability { AbilityId = abilityId, AbilityLv = abilityLevel });
        }

        private static List<ClientItemInfo> PreferJewelryVariety(
            List<ClientItemInfo> candidates,
            EquipSlot slot,
            HashSet<ItemId> usedItemIds,
            HashSet<string> usedNames,
            Dictionary<ItemSubCategory, int> usedSubCategoryCounts)
        {
            var unusedItems = candidates
                .Where(i => !usedItemIds.Contains(i.ItemId))
                .ToList();
            if (unusedItems.Count > 0)
                candidates = unusedItems;

            var underCategoryLimit = candidates
                .Where(i => IsUnderJewelrySubCategoryLimit(i.SubCategory, usedSubCategoryCounts))
                .ToList();
            if (underCategoryLimit.Count > 0)
                candidates = underCategoryLimit;

            var distinctNameCandidates = candidates
                .Where(i => !usedNames.Contains(NormalizeJewelryName(i.Name)))
                .ToList();
            if (distinctNameCandidates.Count > 0)
                candidates = distinctNameCandidates;

            var underrepresentedCategoryCandidates = candidates
                .GroupBy(i => i.SubCategory)
                .OrderBy(g => GetJewelrySubCategoryPriority(g.Key, slot))
                .ThenBy(g => usedSubCategoryCounts.GetValueOrDefault(g.Key))
                .FirstOrDefault()
                ?.ToList();
            if (underrepresentedCategoryCandidates?.Count > 0)
                return underrepresentedCategoryCandidates;

            return candidates;
        }

        private static bool IsUnderJewelrySubCategoryLimit(
            ItemSubCategory subCategory,
            Dictionary<ItemSubCategory, int> usedSubCategoryCounts)
        {
            int limit = subCategory switch
            {
                ItemSubCategory.EmblemStone => 1,
                ItemSubCategory.JewelryRing => 2,
                ItemSubCategory.JewelryBracelet => 2,
                ItemSubCategory.JewelryPierce => 2,
                ItemSubCategory.JewelryCommon => 5,
                _ => 5,
            };

            return usedSubCategoryCounts.GetValueOrDefault(subCategory) < limit;
        }

        private static int GetJewelrySubCategoryPriority(ItemSubCategory subCategory, EquipSlot slot)
        {
            var preferredOrder = GetPreferredJewelrySubCategoryOrder(slot);
            int index = Array.IndexOf(preferredOrder, subCategory);
            return index >= 0 ? index : preferredOrder.Length;
        }

        private static ItemSubCategory[] GetPreferredJewelrySubCategoryOrder(EquipSlot slot)
            => slot switch
            {
                EquipSlot.Jewelry1 =>
                [
                    ItemSubCategory.JewelryRing,
                    ItemSubCategory.JewelryBracelet,
                    ItemSubCategory.JewelryPierce,
                    ItemSubCategory.JewelryCommon,
                    ItemSubCategory.EmblemStone,
                ],
                EquipSlot.Jewelry2 =>
                [
                    ItemSubCategory.JewelryBracelet,
                    ItemSubCategory.JewelryPierce,
                    ItemSubCategory.JewelryRing,
                    ItemSubCategory.JewelryCommon,
                    ItemSubCategory.EmblemStone,
                ],
                EquipSlot.Jewelry3 =>
                [
                    ItemSubCategory.JewelryPierce,
                    ItemSubCategory.JewelryRing,
                    ItemSubCategory.JewelryBracelet,
                    ItemSubCategory.JewelryCommon,
                    ItemSubCategory.EmblemStone,
                ],
                EquipSlot.Jewelry4 =>
                [
                    ItemSubCategory.JewelryRing,
                    ItemSubCategory.JewelryBracelet,
                    ItemSubCategory.JewelryPierce,
                    ItemSubCategory.JewelryCommon,
                    ItemSubCategory.EmblemStone,
                ],
                _ =>
                [
                    ItemSubCategory.JewelryBracelet,
                    ItemSubCategory.JewelryPierce,
                    ItemSubCategory.JewelryRing,
                    ItemSubCategory.JewelryCommon,
                    ItemSubCategory.EmblemStone,
                ],
            };

        private static string NormalizeJewelryName(string name)
        {
            string trimmed = name.Trim();
            int lastNonDigit = trimmed.Length - 1;

            while (lastNonDigit >= 0 && char.IsDigit(trimmed[lastNonDigit]))
                lastNonDigit--;

            if (lastNonDigit < trimmed.Length - 1 && lastNonDigit >= 0 && char.IsWhiteSpace(trimmed[lastNonDigit]))
                return trimmed[..lastNonDigit].TrimEnd();

            return trimmed;
        }

        private byte RollPlusValue(float quality, Random rng)
        {
            byte[] weights = quality switch
            {
                >= PawnQuality.Legendary => [3],
                >= PawnQuality.Excellent => [2, 3, 3, 3],
                >= PawnQuality.Superior  => [1, 2, 2, 3],
                >= PawnQuality.Good      => [0, 1, 1, 2, 2],
                >= PawnQuality.Normal    => [0, 0, 1, 1],
                >= PawnQuality.Poor      => [0, 0, 0, 1],
                _                        => [0],
            };

            return weights[rng.Next(weights.Length)];
        }

        private byte RollAutoCustomSkillLevel(float quality, float playerFactor, Random rng)
        {
            int progressCap = Math.Clamp((int)Math.Round(playerFactor * MaxCustomSkillLevel), 1, MaxCustomSkillLevel);
            byte[] weights = quality switch
            {
                >= PawnQuality.Excellent => [10],
                >= PawnQuality.Superior  => [9, 10, 10, 10],
                >= PawnQuality.Good      => [9, 9, 10],
                >= PawnQuality.Normal    => [7, 8, 8],
                >= PawnQuality.Poor      => [5, 6],
                _                        => [3, 4, 5],
            };

            return (byte)Math.Min(progressCap, weights[rng.Next(weights.Length)]);
        }

        private byte RollAutoAbilityLevel(float quality, float playerFactor, Random rng)
        {
            int progressCap = Math.Clamp((int)Math.Round(playerFactor * MaxAbilityLevel), 1, MaxAbilityLevel);
            byte[] weights = quality switch
            {
                >= PawnQuality.Excellent => [6],
                >= PawnQuality.Superior  => [5, 6, 6],
                >= PawnQuality.Good      => [5, 5, 6],
                >= PawnQuality.Normal    => [4, 4, 5],
                >= PawnQuality.Poor      => [3, 4],
                _                        => [2, 3],
            };

            return (byte)Math.Min(progressCap, weights[rng.Next(weights.Length)]);
        }

        private byte RollAutoCraftRank(float quality, float playerFactor, Random rng)
        {
            int progressCap = Math.Clamp((int)Math.Round(playerFactor * 71), 1, 71);
            (int min, int max) = quality switch
            {
                >= PawnQuality.Legendary => (71, 71),
                >= PawnQuality.Excellent => (62, 71),
                >= PawnQuality.Superior  => (52, 64),
                >= PawnQuality.Good      => (40, 52),
                >= PawnQuality.Normal    => (20, 36),
                >= PawnQuality.Poor      => (8, 16),
                _                        => (1, 5),
            };

            return RollCappedRange(min, max, progressCap, rng);
        }

        private static IReadOnlyDictionary<CraftSkillType, uint> RollAutoCraftSkillDistribution(
            byte craftRank,
            float quality,
            Random rng)
        {
            uint points = craftRank > 0 ? (uint)craftRank - 1 : 0;
            var result = PrimaryCraftSkillTypes.ToDictionary(type => type, _ => 0u);
            if (points == 0)
                return result;

            double[] weights = PrimaryCraftSkillTypes
                .Select(type => GetCraftSkillWeight(type, quality, rng))
                .ToArray();

            for (uint i = 0; i < points; i++)
            {
                int index = PickWeightedIndex(weights, rng);
                result[PrimaryCraftSkillTypes[index]]++;

                // Diminishing returns keep a pawn from dumping every point into one stat.
                weights[index] *= 0.94;
            }

            return result;
        }

        private static double GetCraftSkillWeight(CraftSkillType type, float quality, Random rng)
        {
            double valueBias = type switch
            {
                CraftSkillType.ProductionSpeed => 1.10,
                CraftSkillType.EquipmentEnhancement => 1.20,
                CraftSkillType.EquipmentQuality => 1.15,
                CraftSkillType.ConsumableQuantity => 0.95,
                CraftSkillType.CostPerformance => 0.90,
                _ => 1.00,
            };
            double qualityBias = 1.0 + ((quality - PawnQuality.Normal) * (valueBias - 1.0));
            double personality = 0.80 + (rng.NextDouble() * 0.40);

            return Math.Max(0.10, valueBias * qualityBias * personality);
        }

        private static int PickWeightedIndex(IReadOnlyList<double> weights, Random rng)
        {
            double total = weights.Sum();
            double roll = rng.NextDouble() * total;
            double current = 0;

            for (int i = 0; i < weights.Count; i++)
            {
                current += weights[i];
                if (roll <= current)
                    return i;
            }

            return weights.Count - 1;
        }

        private static byte RollCappedRange(int min, int max, int progressCap, Random rng)
        {
            int cappedMax = Math.Clamp(max, 1, progressCap);
            int cappedMin = Math.Clamp(min, 1, cappedMax);
            return (byte)rng.Next(cappedMin, cappedMax + 1);
        }

        private static int GetAutoAbilitySlotCount(float quality)
            => quality switch
            {
                >= PawnQuality.Good      => MaxAbilitySlots,
                >= PawnQuality.Normal    => 6,
                >= PawnQuality.Poor      => 4,
                _                        => 2,
            };

        private static readonly CraftSkillType[] PrimaryCraftSkillTypes =
        [
            CraftSkillType.ProductionSpeed,
            CraftSkillType.EquipmentEnhancement,
            CraftSkillType.EquipmentQuality,
            CraftSkillType.ConsumableQuantity,
            CraftSkillType.CostPerformance,
        ];

        private static CDataPawnCraftData BuildDefaultCraftData(byte skillLevel)
            => BuildDefaultCraftData(PrimaryCraftSkillTypes.ToDictionary(type => type, _ => (uint)skillLevel));

        private static CDataPawnCraftData BuildDefaultCraftData(IReadOnlyDictionary<CraftSkillType, uint> skillLevels)
        {
            return new CDataPawnCraftData
            {
                CraftExp = 0,
                CraftRank = 1,
                CraftRankLimit = 71,
                CraftPoint = 0,
                PawnCraftSkillList =
                [
                    new() { Type = CraftSkillType.ProductionSpeed, Level = Math.Min(skillLevels.GetValueOrDefault(CraftSkillType.ProductionSpeed), MaxCraftSkillLevel) },
                    new() { Type = CraftSkillType.EquipmentEnhancement, Level = Math.Min(skillLevels.GetValueOrDefault(CraftSkillType.EquipmentEnhancement), MaxCraftSkillLevel) },
                    new() { Type = CraftSkillType.EquipmentQuality, Level = Math.Min(skillLevels.GetValueOrDefault(CraftSkillType.EquipmentQuality), MaxCraftSkillLevel) },
                    new() { Type = CraftSkillType.ConsumableQuantity, Level = Math.Min(skillLevels.GetValueOrDefault(CraftSkillType.ConsumableQuantity), MaxCraftSkillLevel) },
                    new() { Type = CraftSkillType.CostPerformance, Level = Math.Min(skillLevels.GetValueOrDefault(CraftSkillType.CostPerformance), MaxCraftSkillLevel) },
                    new() { Type = CraftSkillType.ConsumableProductionIsAlwaysGreatSuccess, Level = skillLevels.GetValueOrDefault(CraftSkillType.ConsumableProductionIsAlwaysGreatSuccess) },
                    new() { Type = CraftSkillType.CreatingHighQualityEquipmentIsAlwaysGreatSuccess, Level = skillLevels.GetValueOrDefault(CraftSkillType.CreatingHighQualityEquipmentIsAlwaysGreatSuccess) },
                    new() { Type = CraftSkillType.CostPerformanceEffectUpFactor1, Level = skillLevels.GetValueOrDefault(CraftSkillType.CostPerformanceEffectUpFactor1) },
                    new() { Type = CraftSkillType.CostPerformanceEffectUpFactor2, Level = skillLevels.GetValueOrDefault(CraftSkillType.CostPerformanceEffectUpFactor2) },
                    new() { Type = CraftSkillType.UnknownEffect10, Level = skillLevels.GetValueOrDefault(CraftSkillType.UnknownEffect10) },
                ]
            };
        }

        private static void EnsureAllCraftSkills(CDataPawnCraftData craftData)
        {
            craftData.PawnCraftSkillList ??= [];

            foreach (CraftSkillType type in Enum.GetValues<CraftSkillType>())
            {
                if (craftData.PawnCraftSkillList.Any(skill => skill.Type == type))
                    continue;

                craftData.PawnCraftSkillList.Add(new CDataPawnCraftSkill { Type = type, Level = 0 });
            }
        }

        private static void ClampCraftSkillLevels(CDataPawnCraftData craftData)
        {
            foreach (CDataPawnCraftSkill skill in craftData.PawnCraftSkillList)
            {
                if (IsPrimaryCraftSkill(skill.Type))
                    skill.Level = Math.Min(skill.Level, MaxCraftSkillLevel);
            }
        }

        private static bool IsPrimaryCraftSkill(CraftSkillType type)
            => PrimaryCraftSkillTypes.Contains(type);

        private static IReadOnlyDictionary<CraftSkillType, uint> ToStoredCraftSkillLevels(IReadOnlyDictionary<CraftSkillType, uint> displaySkillLevels)
        {
            return displaySkillLevels.ToDictionary(
                pair => pair.Key,
                pair => IsPrimaryCraftSkill(pair.Key) ? ToStoredCraftSkillLevel(pair.Value) : pair.Value);
        }

        private static uint ToStoredCraftSkillLevel(uint displayLevel)
        {
            uint clampedDisplayLevel = Math.Clamp(displayLevel, 1, MaxCraftSkillDisplayLevel);
            return clampedDisplayLevel - 1;
        }

        private static CDataCharacterJobData BuildDefaultJobData(JobId job, int level)
        {
            return new CDataCharacterJobData
            {
                Job = job,
                Lv = (uint)level,
                Exp = 0,
                JobPoint = 0,
                Atk = (ushort)(level * 5),
                Def = (ushort)(level * 5),
                MAtk = (ushort)(level * 5),
                MDef = (ushort)(level * 5),
                Strength = (ushort)(level * 2),
                DownPower = (ushort)(level * 3),
                ShakePower = (ushort)(level * 3),
                StunPower = (ushort)(level * 3),
                Constitution = (ushort)(level * 2),
                Guts = (ushort)(level * 2),
            };
        }
    }
}
