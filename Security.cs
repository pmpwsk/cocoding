using System.Security.Cryptography;
using System.Text;
using Isopoh.Cryptography.Argon2;

namespace cocoding;

/// <summary>
/// Methods for security.
/// </summary>
public static class Security
{
    /// <summary>
    /// Returns a random string of the given length. Possible characters are lowercase and uppercase letters and digits.
    /// </summary>
    public static string RandomString(int length)
        => RandomString(length, true, true, true);

    /// <summary>
    /// Returns a random string of the given length. Possible character sets are lowercase and uppercase letters and digits, they are individually selected.
    /// </summary>
    public static string RandomString(int length, bool lowercase, bool uppercase, bool digits)
    {
        string sets = "";
        if (lowercase)
            sets += 'a';
        if (uppercase)
            sets += 'A';
        if (digits)
            sets += '1';
        if (sets.Length == 0)
            throw new Exception("At least one character set needs to be selected!");

        StringBuilder result = new();
        while (result.Length < length)
            result.Append(RandomItem(RandomItem(sets) switch
            {
                'a' => "abcdefghijklmnopqrstuvwxyz",
                'A' => "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
                '1' => "0123456789",
                _ => throw new Exception("Unrecognized character set!")
            }));
        return result.ToString();
    }

    /// <summary>
    /// Returns a random item from the given array.
    /// </summary>
    public static T RandomItem<T>(T[] values)
        => values[RandomNumberGenerator.GetInt32(values.Length)];

    /// <summary>
    /// Returns a random character from the given string.
    /// </summary>
    public static char RandomItem(string characters)
        => characters[RandomNumberGenerator.GetInt32(characters.Length)];

    /// <summary>
    /// Returns a random value of Int32.
    /// </summary>
    public static int RandomInt()
        => BitConverter.ToInt32(RandomNumberGenerator.GetBytes(4));

    /// <summary>
    /// Hashes the given password with Argon2 using the given salt.<br/>
    /// Parameters: Type=Hybrid/id, Version=19, Memory=32KB, Lanes=8, Length=32
    /// </summary>
    public static byte[] HashPassword(string password, byte[] salt)
    {
        using var secureHash = new Argon2(new Argon2Config()
        {
            Type = Argon2Type.HybridAddressing,
            Version = Argon2Version.Nineteen,
            TimeCost = 10,
            MemoryCost = 32768,
            Lanes = 8,
            Threads = Environment.ProcessorCount,
            Password = Encoding.UTF8.GetBytes(password),
            Salt = salt,
            HashLength = 32,
            ClearPassword = true
        }).Hash();
        byte[] hash = new byte[secureHash.Buffer.Length];
        for (int i = 0; i < hash.Length; i++)
            hash[i] = secureHash[i];
        secureHash.Dispose();
        return hash;
    }
}