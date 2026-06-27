using Arrowgene.Buffers;
using System.Collections.Generic;

namespace Arrowgene.Ddon.Shared.Entity.Structure
{
    public class CDataItemEquipElement
    {
        public string ItemUID { get; set; } = string.Empty;
        public List<CDataEquipElementParam> EquipElementList { get; set; } = [];
    
        public class Serializer : EntitySerializer<CDataItemEquipElement>
        {
            public override void Write(IBuffer buffer, CDataItemEquipElement obj)
            {
                WriteMtString(buffer, obj.ItemUID);
                WriteEntityList<CDataEquipElementParam>(buffer, obj.EquipElementList);
            }
        
            public override CDataItemEquipElement Read(IBuffer buffer)
            {
                CDataItemEquipElement obj = new CDataItemEquipElement();
                obj.ItemUID = ReadMtString(buffer);
                obj.EquipElementList = ReadEntityList<CDataEquipElementParam>(buffer);
                return obj;
            }
        }
    }
}
