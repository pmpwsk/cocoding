namespace cocoding.Data;

/// <summary>
/// Entry of the ASSIGNMENT table.
/// </summary>
public class AssignmentEntry(int projectId, int userId, ProjectRole projectRole, bool pinned = false)
{
    /// <summary>
    /// PID | INT
    /// </summary>
    public int ProjectId = projectId;
    
    /// <summary>
    /// UID | INT
    /// </summary>
    public int UserId = userId;
    
    /// <summary>
    /// PROLE | INT | converted to enum ProjectRole
    /// </summary>
    public ProjectRole ProjectRole = projectRole;

    /// <summary>
    /// PINNED | TINYINT | standard value '0'
    /// </summary>
    public bool Pinned = pinned;
}