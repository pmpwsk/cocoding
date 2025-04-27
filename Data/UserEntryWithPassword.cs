namespace cocoding.Data;

/// <summary>
/// Complete entry of the USER table.
/// </summary>
public class UserEntryWithPassword(int id, string username, byte[] passwordHash, byte[] salt, string? name, Role role, Theme theme)
    : UserEntry(id, username, name, role, theme)
{

    /// <summary>
    /// PASSWORD | BINARY(32) | Argon2 hash
    /// </summary>
    public byte[] PasswordHash = passwordHash;

    /// <summary>
    /// SALT | BINARY(16) | salt for password hash
    /// </summary>
    public byte[] Salt = salt;
}