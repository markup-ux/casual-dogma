using System;
using System.Collections.Generic;
using System.Linq;
using Arrowgene.Ddon.Shared.Entity.Structure;

namespace Arrowgene.Ddon.Shared.Model
{
    public class Item
    {
        public string UId
        {
            get
            {
                if (_uid == null)
                {
                    UpdateUId();
                }

                return _uid;
            }
            set => _uid = value;
        }

        public uint ItemId { get; set; }
        public byte SafetySetting { get; set; } // This is safety setting.
        public byte Color { get; set; }
        public byte PlusValue { get; set; } // This is Equipment Quality, +0/1/2/3/
        public uint EquipPoints { get; set; }
        public List<CDataEquipElementParam> EquipElementParamList { get; set; }
        public List<CDataAddStatusParam> AddStatusParamList { get; set; } // Used for Limit Break and Extreme Synthesis
        public List<CDataEquipStatParam> EquipStatParamList { get; set; } // used for emblem, vocation stones, etc.

        private string _uid;

        public Item()
        {
            EquipElementParamList = [];
            AddStatusParamList = [];
            EquipStatParamList = [];
        }

        public Item(Item obj)
        {
            this._uid = UpdateUId();
            this.ItemId = obj.ItemId;
            this.SafetySetting = obj.SafetySetting;
            this.Color = obj.Color;
            this.PlusValue = obj.PlusValue;
            this.EquipPoints = obj.EquipPoints;
            this.EquipElementParamList = [.. obj.EquipElementParamList.Select(x => new CDataEquipElementParam(x))];
            this.AddStatusParamList = [.. obj.AddStatusParamList.Select(x => new CDataAddStatusParam(x))];
            this.EquipStatParamList = [.. obj.EquipStatParamList.Select(x => new CDataEquipStatParam(x))];
        }

        private string UpdateUId()
        {
            _uid = Guid.CreateVersion7(DateTimeOffset.UtcNow).ToString();
            return _uid;
        }
    }
}
