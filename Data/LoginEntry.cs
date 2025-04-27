namespace cocoding.Data;

/// <summary>
/// Complete entry of the LOGIN table.
/// </summary>
public class LoginEntry(int userId, string token, DateTime expiration)
{
    /// <summary>
    /// UID | INT | from table USER
    /// </summary>
    public int UserId = userId;
    
    /// <summary>
    /// TOKEN | CHAR(64)
    /// </summary>
    public string Token = token;

    /// <summary>
    /// EXPIRATION | BIGINT | ticks of DateTime
    /// </summary>
    public DateTime Expiration = expiration;
}