using Arrowgene.Ddon.Shared.Entity.Structure;
using System.Collections.Generic;

namespace Arrowgene.Ddon.Shared.Model
{
    public class CustomSkill
    {
        public JobId Job { get; set; }
        public uint SkillId { get; set; }
        public byte SkillLv { get; set; }

        public CustomSkill() { }

        public CustomSkill(CustomSkillId customSkillId, byte skillLv = 1)
        {
            Job = customSkillId.JobId();
            SkillId = customSkillId.ReleaseId();
            SkillLv = skillLv;
        }

        public CustomSkillId CustomSkillId
        {
            get
            {
                return CustomSkillIdExtension.gJobIdReleaseIdCustomSkillMap.GetValueOrDefault((Job, SkillId));
            }
            set
            {
                Job = value.JobId();
                SkillId = value.ReleaseId();
            }
        }

        public CDataSetAcquirementParam AsCDataSetAcquirementParam(byte slotNo)
        {
            return new CDataSetAcquirementParam()
            {
                Job = this.Job,
                SlotNo = slotNo,
                AcquirementNo = this.SkillId,
                AcquirementLv = this.SkillLv
            };
        }

        public CDataContextAcquirementData AsCDataContextAcquirementData(byte slotNo)
        {
            return new CDataContextAcquirementData()
            {
                SlotNo = slotNo,
                AcquirementNo = this.SkillId,
                AcquirementLv = this.SkillLv
            };
        }

        public CDataLearnedSetAcquirementParam AsCDataLearnedSetAcquirementParam()
        {
            return new CDataLearnedSetAcquirementParam()
            {
                Job = this.Job,
                Type = ReleaseType.CustomSkill,
                AcquirementNo = this.SkillId,
                AcquirementLv = this.SkillLv,
                AcquirementParamId = 0
            };
        }

        public CDataSkillLevelBaseParam AsCDataSkillLevelBaseParam()
        {
            return new CDataSkillLevelBaseParam()
            {
                Job = this.Job,
                SkillLv = this.SkillLv,
                SkillNo = this.SkillId,
            };
        }
    }
}
