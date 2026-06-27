using Arrowgene.Ddon.Shared.Entity.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arrowgene.Ddon.Shared.Model
{
    public class RentalPawn : Pawn
    {
        public uint OwningCharacterId { get; set; }

        public byte MaxAdventureCount { get; set; }
        public byte AdventureCount { get; set; }
        public byte MaxCraftCount { get; set; }
        public byte CraftCount { get; set; }
        public uint KillCount { get; set; }
        public DateTime HireDate { get; set; }

        public CDataRentedPawnList CDataRentedPawnList 
        { 
            get
            {
                return new CDataRentedPawnList()
                {
                    Name = Name,
                    PawnId = PawnId,
                    AdventureCount = AdventureCount,
                    CraftCount = CraftCount,
                    PawnType = PawnType,
                    Sex = EditInfo.Sex,
                    PawnState =PawnState,
                    PawnListData = new CDataPawnListData()
                    {
                        Job = Job,
                        CraftRank = CraftData.CraftRank,
                        Level = ActiveCharacterJobData.Lv,
                        PawnCraftSkillList = CraftData.PawnCraftSkillList,
                    }
                };
            } 
        }

        public override CDataPawnInfo CDataPawnInfo
        {
            get
            {
                var data = base.CDataPawnInfo;
                data.AdventureCount = AdventureCount;
                data.CraftCount = CraftCount;

                data.MaxAdventureCount = MaxAdventureCount;
                data.MaxCraftCount = MaxCraftCount;

                return data;
            }
        }
    }
}
