namespace cocoding.Data;

/// <summary>
/// Possible statuses a user can have.
/// </summary>
public enum UserStatus
{
    /// <summary>
    /// User is online in the same file.
    /// </summary>
    Online = 0,
        
    /// <summary>
    /// User is online but in a different file or project.
    /// </summary>
    Elsewhere = 1,
        
    /// <summary>
    /// User is not online in any editor instance.
    /// </summary>
    Offline = 2
}