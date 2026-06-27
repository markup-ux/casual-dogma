namespace Arrowgene.Ddon.Shared.Model.Quest
{
    public class SubstoryProgress
    {
        public QuestSubstoryGroupId SubstoryGroupId { get; set; }
        public int SequenceStep { get; set; }
        public bool IsComplete { get; set; }
    }
}
