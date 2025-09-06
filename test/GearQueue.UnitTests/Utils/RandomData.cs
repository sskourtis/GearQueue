using System.Security.Cryptography;
using System.Text;

namespace GearQueue.UnitTests.Utils;

public static class RandomData
{
    public static byte[] GetRandomBytes(int length)
    {
        return RandomNumberGenerator.GetBytes(length);
    }
    
    private const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    
    public static string GetString(int length)
    {
        if (length <= 0)
            throw new ArgumentOutOfRangeException(nameof(length), "Length must be greater than zero.");

        var result = new StringBuilder(length);
        var bytes = new byte[length];

        // Use cryptographically secure random numbers
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);

        foreach (var b in bytes)
        {
            result.Append(Chars[b % Chars.Length]);
        }

        return result.ToString();
    }
}