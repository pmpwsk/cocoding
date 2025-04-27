using System.Security.Cryptography;

namespace cocoding.Data;

/// <summary>
/// Methods for the USER table.
/// </summary>
public static partial class UserTable
{
    /// <summary>
    /// Returns the user with the given ID or null if no such user exists.
    /// </summary>
    public static UserEntry? GetById(CommandProvider cp, int userId)
    {
        var cmd = cp.CreateCommand("select USERNAME, NAME, ROLE, THEME from USER where UID = @UID;");
        cmd.Parameters.AddWithValue("@UID", userId);

        using var reader = cmd.ExecuteReader();

        if (!reader.Read())
            return null;

        string username = reader.GetString(0);
        string? name = reader.GetNullableString(1);
        Role role = (Role)reader.GetInt32(2);
        Theme theme = (Theme)reader.GetInt32(3);

        return new(userId, username, name, role, theme);
    }

    /// <summary>
    /// Returns the user (without the password columns) with the given username or null if no such user exists.
    /// </summary>
    public static UserEntry? GetByUsername(CommandProvider cp, string username)
    {
        var cmd = cp.CreateCommand("select UID, NAME, ROLE, THEME from USER where USERNAME = @USERNAME;");
        cmd.Parameters.AddWithValue("@USERNAME", username);

        using var reader = cmd.ExecuteReader();

        if (!reader.Read())
            return null;

        int userId = reader.GetInt32(0);
        string? name = reader.GetNullableString(1);
        Role role = (Role)reader.GetInt32(2);
        Theme theme = (Theme)reader.GetInt32(3);

        return new(userId, username, name, role, theme);
    }

    /// <summary>
    /// Returns the user (with the password columns) with the given username or null if no such user exists.
    /// </summary>
    public static UserEntryWithPassword? GetByUsernameWithPassword(CommandProvider cp, string username)
    {
        var cmd = cp.CreateCommand("select UID, PASSWORD, SALT, NAME, ROLE, THEME from USER where USERNAME = @USERNAME;");
        cmd.Parameters.AddWithValue("@USERNAME", username);

        using var reader = cmd.ExecuteReader();

        if (!reader.Read())
            return null;

        int userId = reader.GetInt32(0);
        byte[] passwordHash = reader.GetFixedBytes(1, 32);
        byte[] salt = reader.GetFixedBytes(2, 16);
        string? name = reader.GetNullableString(3);
        Role role = (Role)reader.GetInt32(4);
        Theme theme = (Theme)reader.GetInt32(5);
        
        return new(userId, username, passwordHash, salt, name, role, theme);
    }

    /// <summary>
    /// Creates a new user with a unique ID using the given username, password and name.
    /// </summary>
    public static UserSession CreateUser(CommandProvider cp, string username, string password, string? name = null)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var passwordHash = Security.HashPassword(password, salt);

        int userId;
        do userId = Security.RandomInt();
        while (GetById(cp, userId) != null);

        var cmd = cp.CreateCommand("insert into USER (UID, USERNAME, PASSWORD, SALT, NAME, ROLE, THEME) values (@UID, @USERNAME, @PASSWORD, @SALT, @NAME, @ROLE, @THEME);");
        cmd.Parameters.AddWithValue("@UID", userId);
        cmd.Parameters.AddWithValue("@USERNAME", username);
        cmd.Parameters.AddWithValue("@PASSWORD", passwordHash);
        cmd.Parameters.AddWithValue("@SALT", salt);
        cmd.Parameters.AddWithValue("@NAME", name);
        cmd.Parameters.AddWithValue("@ROLE", (int)Role.User);
        cmd.Parameters.AddWithValue("@THEME", (int)Theme.System);

        cmd.ExecuteNonQuery();

        return new(userId, username, null, Role.User, Theme.System, null);
    }

    /// <summary>
    /// Sets the role of the user with the given ID.
    /// </summary>
    public static bool SetRole(CommandProvider cp, int userId, Role role)
    {
        var cmd = cp.CreateCommand("update USER set ROLE = @ROLE where UID = @UID;");
        cmd.Parameters.AddWithValue("@UID", userId);
        cmd.Parameters.AddWithValue("@ROLE", (int)role);

        return cmd.ExecuteNonQuery() > 0;
    }

    /// <summary>
    /// Sets the theme setting of the user with the given ID.
    /// </summary>
    public static bool SetTheme(CommandProvider cp, int userId, Theme theme)
    {
        var cmd = cp.CreateCommand("update USER set THEME = @THEME where UID = @UID;");
        cmd.Parameters.AddWithValue("@UID", userId);
        cmd.Parameters.AddWithValue("@THEME", (int)theme);

        return cmd.ExecuteNonQuery() > 0;
    }

    /// <summary>
    /// Returns all users as a list.
    /// </summary>
    public static List<UserEntry> ListAll(CommandProvider cp)
    {
        List<UserEntry> result = [];

        var cmd = cp.CreateCommand("select UID, USERNAME, NAME, ROLE, THEME from USER;");

        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            int userId = reader.GetInt32(0);
            string username = reader.GetString(1);
            string? name = reader.GetNullableString(2);
            Role role = (Role)reader.GetInt32(3);
            Theme theme = (Theme)reader.GetInt32(4);

            result.Add(new(userId, username, name, role, theme));
        }
        
        return result;
    }

    /// <summary>
    /// Sets the password of the user with the given ID.
    /// </summary>
    public static bool SetPassword(CommandProvider cp, int userId, string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var passwordHash = Security.HashPassword(password, salt);

        var cmd = cp.CreateCommand("update USER set PASSWORD = @PASSWORD, SALT = @SALT where UID = @UID;");
        cmd.Parameters.AddWithValue("@UID", userId);
        cmd.Parameters.AddWithValue("@PASSWORD", passwordHash);
        cmd.Parameters.AddWithValue("@SALT", salt);

        return cmd.ExecuteNonQuery() > 0;
    }

    /// <summary>
    /// Deletes the user with the given ID.
    /// </summary>
    public static void DeleteUser(CommandProvider cp, int userId)
    {
        var cmd = cp.CreateCommand("delete from USER where UID = @UID;");
        cmd.Parameters.AddWithValue("@UID", userId);

        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Changes the account with the given ID using the given parameters.
    /// </summary>
    public static bool EditUser(CommandProvider cp, int userId, string username, string? name)
    {
        var cmd = cp.CreateCommand("update USER set USERNAME = @USERNAME, NAME = @NAME where UID = @UID;");
        cmd.Parameters.AddWithValue("@UID", userId);
        cmd.Parameters.AddWithValue("@USERNAME", username);
        cmd.Parameters.AddWithValue("@NAME", name);

        return cmd.ExecuteNonQuery() > 0;
    }

    /// <summary>
    /// Changes the account with the given ID using the given parameters.
    /// </summary>
    public static bool EditUser(CommandProvider cp, int userId, string username, string? name, string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var passwordHash = Security.HashPassword(password, salt);

        var cmd = cp.CreateCommand("update USER set USERNAME = @USERNAME, NAME = @NAME, PASSWORD = @PASSWORD, SALT = @SALT where UID = @UID;");
        cmd.Parameters.AddWithValue("@UID", userId);
        cmd.Parameters.AddWithValue("@USERNAME", username);
        cmd.Parameters.AddWithValue("@NAME", name);
        cmd.Parameters.AddWithValue("@PASSWORD", passwordHash);
        cmd.Parameters.AddWithValue("@SALT", salt);

        return cmd.ExecuteNonQuery() > 0;
    }
}