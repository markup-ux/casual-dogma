using System.Security.Cryptography;
using System.Text;

namespace Launcher;

/// <summary>
/// Encrypts saved credentials with Windows DPAPI (per Windows user), so the
/// password is never written to disk in plaintext.
/// </summary>
public static class CredentialProtector
{
    public static string Protect(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext)) return "";
        try
        {
            byte[] data = Encoding.UTF8.GetBytes(plaintext);
            byte[] enc = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(enc);
        }
        catch
        {
            return "";
        }
    }

    public static string Unprotect(string protectedBase64)
    {
        if (string.IsNullOrEmpty(protectedBase64)) return "";
        try
        {
            byte[] enc = Convert.FromBase64String(protectedBase64);
            byte[] data = ProtectedData.Unprotect(enc, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(data);
        }
        catch
        {
            return "";
        }
    }
}
