using System;
using System.Security.Cryptography;
using System.Text;

namespace Arrowgene.Ddon.WebServer
{
    /// <summary>
    /// Validates the level-sync patch token presented by the official launcher at login time.
    ///
    /// The token is an HMAC-SHA256 over "account|patchVersion|timeBucket" using a shared secret
    /// compiled into the launcher. The time bucket (5 minutes) gives the token a short lifetime so
    /// a captured value cannot be replayed indefinitely. This is a deterrent against casual use of
    /// third-party launchers, not a cryptographic guarantee (the secret lives in the launcher binary).
    /// </summary>
    public static class PatchTokenValidator
    {
        private const long BucketSeconds = 300;

        public static string Compute(string secret, string account, int patchVersion, long bucket)
        {
            string input = $"{(account ?? string.Empty).ToLowerInvariant()}|{patchVersion}|{bucket}";
            using HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret ?? string.Empty));
            byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        public static bool Validate(string secret, string account, int patchVersion, string presentedToken)
        {
            if (string.IsNullOrEmpty(presentedToken))
            {
                return false;
            }

            // Accept the current and previous bucket to tolerate clock skew and request latency.
            long nowBucket = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / BucketSeconds;
            for (long bucket = nowBucket; bucket >= nowBucket - 1; bucket--)
            {
                string expected = Compute(secret, account, patchVersion, bucket);
                if (FixedTimeEquals(expected, presentedToken))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool FixedTimeEquals(string a, string b)
        {
            byte[] ba = Encoding.ASCII.GetBytes(a);
            byte[] bb = Encoding.ASCII.GetBytes(b);
            if (ba.Length != bb.Length)
            {
                return false;
            }

            return CryptographicOperations.FixedTimeEquals(ba, bb);
        }
    }
}
