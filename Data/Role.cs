namespace cocoding.Data;

/// <summary>
/// Enumeration of possible user roles in the system.
/// </summary>
public enum Role
{
    /// <summary>
    /// A regular user of the system.
    /// </summary>
    User = 1,

    /// <summary>
    /// A system administrator.
    /// </summary>
    Administrator = int.MaxValue
}