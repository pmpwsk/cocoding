namespace cocoding.Data;

/// <summary>
/// Contains the token state of a session and some data about the authenticated user.
/// </summary>
public class UserSession(int id, string username, string? name, Role role, Theme theme, Session? session)
    : UserEntry(id, username, name, role, theme)
{
    public Session? Session = session;
}