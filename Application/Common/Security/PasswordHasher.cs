using System.Security.Cryptography;
using System.Text;

namespace Application.Common.Security;

public static class PasswordHasher
{
    // Size of salt
    private const int SaltSize = 16;
    
    // Size of hash
    private const int HashSize = 20;
    
    // Number of iterations
    private const int Iterations = 100000;

    public static string HashPassword(string password)
    {
        // Generate a salt
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);

        // Create hash
        var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
        byte[] hash = pbkdf2.GetBytes(HashSize);

        // Combine salt and hash
        byte[] hashBytes = new byte[SaltSize + HashSize];
        Array.Copy(salt, 0, hashBytes, 0, SaltSize);
        Array.Copy(hash, 0, hashBytes, SaltSize, HashSize);

        // Convert to base64
        return Convert.ToBase64String(hashBytes);
    }

    public static bool VerifyPassword(string password, string hashedPassword)
    {
        // Convert base64 string to byte array
        byte[] hashBytes = Convert.FromBase64String(hashedPassword);

        // Get salt
        byte[] salt = new byte[SaltSize];
        Array.Copy(hashBytes, 0, salt, 0, SaltSize);

        // Create hash with given salt
        var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
        byte[] hash = pbkdf2.GetBytes(HashSize);

        // Compare stored hash with computed hash
        for (int i = 0; i < HashSize; i++)
        {
            if (hashBytes[i + SaltSize] != hash[i])
                return false;
        }

        return true;
    }
}