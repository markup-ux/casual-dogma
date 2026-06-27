using Arrowgene.Ddon.Shared.Entity.Structure;

namespace Arrowgene.Ddon.Shared.Model
{
    public class CharacterProfile
    {
        public byte BackgroundId { get; set; }
        public CDataAchievementIdentifier Title { get; set; } = new();
        public ushort MotionId { get; set; }
        public uint MotionFrameNo { get; set; }
        public string Comment { get; set; } = string.Empty; //Only for pawns

        public CDataArisenProfile CDataArisenProfile 
        { 
            get
            {
                return new()
                {
                    BackgroundId = BackgroundId,
                    Title = Title,
                    MotionId = MotionId,
                    MotionFrameNo = MotionFrameNo,
                };
            }
            set
            {
                BackgroundId = value.BackgroundId;
                Title = value.Title;
                MotionId = value.MotionId;
                MotionFrameNo = value.MotionFrameNo;
            }
        }
    }
}
