using Arrowgene.Ddon.Shared.Entity.Structure;
using Arrowgene.Ddon.Shared.Model;
using Arrowgene.Logging;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Arrowgene.Ddon.Shared.Asset
{
    public class SkillDataAsset(Dictionary<JobId, List<CDataSkillParam>> skills, Dictionary<JobId, List<CDataAbilityParam>> abilities, List<CDataAbilityParam> secretAbilities)
    {
        public FrozenDictionary<JobId, List<CDataSkillParam>> Skills { get; private set; } = skills.ToFrozenDictionary();
        public FrozenDictionary<JobId, List<CDataAbilityParam>> Abilities { get; private set; } = abilities.ToFrozenDictionary();
        public FrozenSet<CDataAbilityParam> SecretAbilities { get; private set; } = [.. secretAbilities];

        public IEnumerable<CDataSkillParam> AllSkills { get => Skills.SelectMany(x => x.Value); }
        public IEnumerable<CDataAbilityParam> AllAbilities { get => Abilities.SelectMany(x => x.Value).Union(SecretAbilities); }
        public HashSet<AbilityId> SecretAbilityIds { get => [.. SecretAbilities.Select(x => x.AbilityNo)]; }

        public CDataSkillParam GetSkill(JobId job, uint releaseId)
        {
            return Skills.GetValueOrDefault(job)?.Where(x => x.SkillNo == releaseId).FirstOrDefault();
        }

        public CDataSkillParam GetSkill(CustomSkillId skillId)
        {
            return GetSkill(skillId.JobId(), skillId.ReleaseId());
        }

        public CDataSkillParam GetSkill(uint skillId)
        {
            return GetSkill((CustomSkillId)skillId);
        }

        public CDataAbilityParam GetAbility(AbilityId abilityId)
        {
            return AllAbilities.Where(x => x.AbilityNo == abilityId).FirstOrDefault();
        }

        public CDataAbilityParam GetAbility(uint acquirementNo)
        {
            return GetAbility((AbilityId)acquirementNo);
        }

        public bool IsSecret(AbilityId abilityId)
        {
            return SecretAbilityIds.Contains(abilityId);
        }
    }

    public class SkillDataAssetDeserializer : IAssetDeserializer<SkillDataAsset>
    {
        private static readonly ILogger Logger = LogProvider.Logger(typeof(SkillDataAssetDeserializer));

        public SkillDataAsset ReadPath(string path)
        {
            Logger.Info($"Reading {path}");

            string json = Util.ReadAllText(path);
            JsonDocument document = JsonDocument.Parse(json);

            var skillData = document.RootElement.GetProperty("skill_data")
                .Deserialize<List<CDataSkillParam>>()
                .GroupBy(x => x.Job)
                .ToDictionary(key => key.Key, val => val.ToList());
            var abilityData = document.RootElement.GetProperty("ability_data")
                .Deserialize<List<CDataAbilityParam>>()
                .GroupBy(x => x.Job)
                .ToDictionary(key => key.Key, val => val.ToList());
            var secretData = document.RootElement.GetProperty("secret_ability_data").Deserialize<List<CDataAbilityParam>>();


            return new(skillData, abilityData, secretData);
        }
    }
}
