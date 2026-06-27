using System;

namespace Arrowgene.Ddon.Shared.Model
{
    public class OfficialPawnContext
    {
        /// <summary>The hiring character's active job level at the time of hire.</summary>
        public int PlayerLevel { get; init; }

        /// <summary>The generated pawn's job level. Usually the player level unless the script fixes it.</summary>
        public int PawnLevel { get; init; }

        /// <summary>Character id of the hiring character.</summary>
        public uint CharacterId { get; init; }

        /// <summary>Deterministic RNG for this pawn and hiring character.</summary>
        public Random Rng { get; init; }

        /// <summary>Builder pre-populated with identity, appearance, job, and level.</summary>
        public OfficialPawnBuilder Builder { get; init; }
    }
}
