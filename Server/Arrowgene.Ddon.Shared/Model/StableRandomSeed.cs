using System;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Arrowgene.Ddon.Shared.Model
{
    public static class StableRandomSeed
    {
        public static int ForOfficialPawn(uint pawnId, uint characterId, string stream, params uint[] discriminators)
        {
            byte[] numericParts = new byte[8 + (discriminators.Length * sizeof(uint))];
            BinaryPrimitives.WriteUInt32LittleEndian(numericParts[..4], pawnId);
            BinaryPrimitives.WriteUInt32LittleEndian(numericParts[4..8], characterId);

            for (int i = 0; i < discriminators.Length; i++)
            {
                int offset = 8 + (i * sizeof(uint));
                BinaryPrimitives.WriteUInt32LittleEndian(numericParts.AsSpan(offset, sizeof(uint)), discriminators[i]);
            }

            return HashToInt32(numericParts, stream);
        }

        public static int ForStream(int baseSeed, string stream, params uint[] discriminators)
        {
            byte[] numericParts = new byte[4 + (discriminators.Length * sizeof(uint))];
            BinaryPrimitives.WriteInt32LittleEndian(numericParts[..4], baseSeed);

            for (int i = 0; i < discriminators.Length; i++)
            {
                int offset = 4 + (i * sizeof(uint));
                BinaryPrimitives.WriteUInt32LittleEndian(numericParts.AsSpan(offset, sizeof(uint)), discriminators[i]);
            }

            return HashToInt32(numericParts, stream);
        }

        public static uint HashToUInt32(string value)
        {
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(value.ToUpperInvariant()));
            return BinaryPrimitives.ReadUInt32LittleEndian(hash);
        }

        private static int HashToInt32(ReadOnlySpan<byte> numericParts, string stream)
        {
            byte[] streamBytes = Encoding.UTF8.GetBytes(stream);
            byte[] input = new byte[numericParts.Length + streamBytes.Length];
            numericParts.CopyTo(input);
            streamBytes.CopyTo(input.AsSpan(numericParts.Length));

            byte[] hash = SHA256.HashData(input);
            return BinaryPrimitives.ReadInt32LittleEndian(hash);
        }
    }
}
