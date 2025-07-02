using System.Security.Cryptography;

namespace Application.Common.Security;

public static class TokenGenerator
{
    private const int TokenLength = 64; // 64 bytes = 512 bits

    public static string GenerateToken()
    {
        // Generate a cryptographically secure random token
        var randomBytes = new byte[TokenLength];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        
        // Convert to Base64 string (URL safe)
        return Convert.ToBase64String(randomBytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .Replace("=", "");
    }
}