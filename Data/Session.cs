namespace cocoding.Data;

/// <summary>
/// The <c>Session</c> part of <c>UserSession</c>.
/// </summary>
public class Session(string token, DateTime expiration)
{
    public string Token = token;

    public DateTime Expiration = expiration;
}