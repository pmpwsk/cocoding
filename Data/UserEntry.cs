namespace cocoding.Data;

/// <summary>
/// Entry of the USER table without the password columns.
/// </summary>
public class UserEntry(int id, string username, string? name, Role role, Theme theme) : IComparable
{
    /// <summary>
    /// UID | INT
    /// </summary>
    public int Id = id;

    /// <summary>
    /// USERNAME | VARCHAR(20)
    /// </summary>
    public string Username = username;

    /// <summary>
    /// NAME | VARCHAR(50) | can be NULL
    /// </summary>
    public string? Name = name;

    /// <summary>
    /// ROLE | INT | converted to enum Role
    /// </summary>
    public Role Role = role;

    public Theme Theme = theme;

    /// <summary>
    /// The name if it isn't null, otherwise @username.
    /// </summary>
    public string DisplayName
        => Name ?? ('@' + Username);

    public int CompareTo(object? otherObj)
    {
        if (otherObj is not UserEntry other)
            return 1;
        
        var result = string.Compare(DisplayName, other.DisplayName, StringComparison.CurrentCulture);
        if (result == 0)
            result = Id.CompareTo(other.Id);
        
        return result;
    }
}