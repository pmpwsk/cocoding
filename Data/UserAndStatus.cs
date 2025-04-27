namespace cocoding.Data;

/// <summary>
/// Contains a user entry along with the user's status and editor color.
/// </summary>
public class UserAndStatus(UserEntry user, UserStatus status, string color, int? projectId = null, int? fileId = null, string? fileName = null, int? lineNumber = null, bool isMobile = false)
{
    /// <summary>
    /// The user object.
    /// </summary>
    public UserEntry User = user;

    /// <summary>
    /// The user's status.
    /// </summary>
    public UserStatus Status = status;

    /// <summary>
    /// The user's color in the viewed file.
    /// </summary>
    public string Color = color;

    /// <summary>
    /// The current project id of the user.
    /// </summary>
    public int? CurrentProjectId = projectId;
    
    /// <summary>
    /// The id of the viewed file.
    /// </summary>
    public int? CurrentFileId = fileId;
    
    /// <summary>
    /// The name of the viewed file.
    /// </summary>
    public string? CurrentFileName = fileName;
    
    /// <summary>
    /// The line number the user is currently on in the viewed file.
    /// </summary>
    public int? LineNumber = lineNumber;

    /// <summary>
    /// True if the user is currently using a mobile device. Otherwise isMobile is false.
    /// </summary>
    public bool IsMobile = isMobile;

    /// <summary>
    /// Builds a CSS style to show the online status as a colored circle if the user is in the same file. Otherwise, an empty string is returned.
    /// </summary>
    public string IconStyle
        => Status == UserStatus.Online ? $"status.online; background: {Color}" : "";
}