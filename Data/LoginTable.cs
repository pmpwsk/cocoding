namespace cocoding.Data;

/// <summary>
/// Methods for the LOGIN table.
/// </summary>
public static partial class LoginTable
{
    /// <summary>
    /// Creates a new token for the given user ID.
    /// </summary>
    public static Session GenerateToken(CommandProvider cp, int userId)
    {
        DateTime expiration = DateTime.UtcNow.AddDays(40);

        string token;
        do token = Security.RandomString(64);
        while (GetByToken(cp, token) != null);

        var cmd = cp.CreateCommand("insert into LOGIN (TOKEN, UID, EXPIRATION) values (@TOKEN, @UID, @EXPIRATION);");
        cmd.Parameters.AddWithValue("@TOKEN", token);
        cmd.Parameters.AddWithValue("@UID", userId);
        cmd.Parameters.AddWithValue("@EXPIRATION", expiration.Ticks);

        cmd.ExecuteNonQuery();

        return new(token, expiration);
    }

    /// <summary>
    /// Returns the session with the given token or null if no such session exists.
    /// </summary>
    public static LoginEntry? GetByToken(CommandProvider cp, string? token)
    {
        if (token == null)
            return null;

        var cmd = cp.CreateCommand("select UID, EXPIRATION from LOGIN where TOKEN = @TOKEN;");
        cmd.Parameters.AddWithValue("@TOKEN", token);

        using var reader = cmd.ExecuteReader();

        if (!reader.Read())
            return null;

        int userId = reader.GetInt32(0);
        DateTime expiration = new(reader.GetInt64(1));

        return new(userId, token, expiration);
    }

    /// <summary>
    /// Deletes the given token.
    /// </summary>
    public static void DeleteToken(CommandProvider cp, string? token)
    {
        if (token == null)
            return;

        var cmd = cp.CreateCommand("delete from LOGIN where TOKEN = @TOKEN;");
        cmd.Parameters.AddWithValue("@TOKEN", token);

        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Deletes all tokens for the given user ID.
    /// </summary>
    public static void DeleteAllTokens(CommandProvider cp, int userId)
    {
        var cmd = cp.CreateCommand("delete from LOGIN where UID = @UID;");
        cmd.Parameters.AddWithValue("@UID", userId);

        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Sets the expiration of the given token.
    /// </summary>
    public static bool SetExpiration(CommandProvider cp, string token, DateTime expiration)
    {
        var cmd = cp.CreateCommand("update LOGIN set EXPIRATION = @EXPIRATION where TOKEN = @TOKEN;");
        cmd.Parameters.AddWithValue("@EXPIRATION", expiration.Ticks);
        cmd.Parameters.AddWithValue("@TOKEN", token);

        return cmd.ExecuteNonQuery() > 0;
    }
}