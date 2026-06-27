using System;
using System.Security.Cryptography;
using System.Text;

namespace Launcher;

/// <summary>
/// Produces the level-sync patch token the server checks at login. The secret and scheme must
/// stay in sync with the server side (Arrowgene.Ddon.WebServer.PatchTokenValidator and the
/// WebServerSetting.DefaultPatchSharedSecret default).
/// </summary>
internal static class PatchAuth
{
    // Must match WebServerSetting.DefaultPatchSharedSecret on the server.
    public const string Secret = "CasualDogma-LevelSync-Patch-2f9c8a1b7e4d4a6f";

    // Bump when the patch package changes; the server can require a minimum via MinPatchVersion.
    public const int Version = 1;

    private const long BucketSeconds = 300;

    public static string Compute(string account)
    {
        long bucket = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / BucketSeconds;
        string input = $"{(account ?? string.Empty).ToLowerInvariant()}|{Version}|{bucket}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(Secret));
        byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
