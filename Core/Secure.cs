using System.Text;

namespace NotchIsland.Core;

/// Ochrana hesla: na Windows DPAPI (vázané na uživatele), jinde Base64 (jen obfuskace).
public static class Secure
{
    public static string Protect(string plain)
    {
        if (string.IsNullOrEmpty(plain)) return "";
#if WINDOWS
        try { return "dpapi:" + Convert.ToBase64String(System.Security.Cryptography.ProtectedData.Protect(Encoding.UTF8.GetBytes(plain), null, System.Security.Cryptography.DataProtectionScope.CurrentUser)); } catch { }
#endif
        return "b64:" + Convert.ToBase64String(Encoding.UTF8.GetBytes(plain));
    }
    public static string Unprotect(string stored)
    {
        if (string.IsNullOrEmpty(stored)) return "";
        try
        {
#if WINDOWS
            if (stored.StartsWith("dpapi:")) return Encoding.UTF8.GetString(System.Security.Cryptography.ProtectedData.Unprotect(Convert.FromBase64String(stored[6..]), null, System.Security.Cryptography.DataProtectionScope.CurrentUser));
#endif
            if (stored.StartsWith("b64:")) return Encoding.UTF8.GetString(Convert.FromBase64String(stored[4..]));
        }
        catch { }
        return "";
    }
}
