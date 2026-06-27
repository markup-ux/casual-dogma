using Arrowgene.Ddon.Shared.Entity.Structure;
using Arrowgene.Ddon.Shared.Model;

namespace Arrowgene.Ddon.GameServer.Scripting.Interfaces
{
    public abstract class IOfficialPawnScript
    {
        /// <summary>Stable virtual pawn id assigned from the script filename.</summary>
        public uint PawnId { get; internal set; }

        public abstract string Name { get; }
        public abstract JobId Job { get; }
        public abstract CDataEditInfo EditInfo { get; }

        /// <summary>Minimum player job level required to see and hire this pawn.</summary>
        public virtual int MinLevel => 1;

        /// <summary>Maximum player job level that can hire this pawn. Default 999 = no cap.</summary>
        public virtual int MaxLevel => 999;

        /// <summary>Fixed generated pawn level. null scales the pawn to the hiring player's active job level.</summary>
        public virtual int? PawnLevel => null;

        /// <summary>Multiplier applied on top of the normal rental cost formula.</summary>
        public virtual float RentalCostMultiplier => 1.0f;

        /// <summary>Quality tier used by automatic official pawn systems.</summary>
        public virtual float Quality => PawnQuality.Normal;

        /// <summary>Maximum number of adventures after hire. null uses the server default.</summary>
        public virtual byte? AdventureCount => null;

        /// <summary>Maximum number of crafts after hire. null uses the server default.</summary>
        public virtual byte? CraftCount => null;

        /// <summary>Return true when the hiring client has unlocked this official pawn.</summary>
        public virtual bool IsUnlocked(GameClient client, DdonGameServer server) => true;

        /// <summary>Generate a rental pawn record for the given hiring context.</summary>
        public abstract RentalPawnRecord Generate(OfficialPawnContext ctx);
    }
}
