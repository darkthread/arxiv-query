using System.Text;
using System.Security.Cryptography;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

public static class ProtectedEnvironmentVariables
{
    static byte[] additionalEntropy = { 2, 8, 8, 2, 5, 2, 5, 2 };

    public static string Get(string varName, bool setByConsoleWhenEmpty = false)
    {
        var val = Environment.GetEnvironmentVariable(varName, EnvironmentVariableTarget.User);
        if (string.IsNullOrEmpty(val))
        {
            if (!setByConsoleWhenEmpty) return val!;
            Console.Write($"Please [{varName}]:");
            val = Console.ReadLine() ?? string.Empty;
            var enc =
                Convert.ToBase64String(
                    ProtectedData.Protect(Encoding.UTF8.GetBytes(val), additionalEntropy, DataProtectionScope.CurrentUser));
            Environment.SetEnvironmentVariable(varName, enc, EnvironmentVariableTarget.User);
            return val;
        }
        try
        {
            return Encoding.UTF8.GetString(
                ProtectedData.Unprotect(Convert.FromBase64String(val), additionalEntropy, DataProtectionScope.CurrentUser));
        }
        catch
        {
            throw new ApplicationException("Failed to decrypt.");
        }
    }
}