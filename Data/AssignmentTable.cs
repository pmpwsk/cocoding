namespace cocoding.Data;

/// <summary>
/// Methods for the ASSIGNMENT table.
/// </summary>
public static partial class AssignmentTable
{
    /// <summary>
    /// Returns the role of the given user in the given project (ProjectRole.None if the user is not part of the project).
    /// </summary>
    public static ProjectRole GetRole(CommandProvider cp, int projectId, int userId)
    {
        var cmd = cp.CreateCommand("select PROLE from ASSIGNMENT where PID = @PID and UID = @UID;");
        cmd.Parameters.AddWithValue("@PID", projectId);
        cmd.Parameters.AddWithValue("@UID", userId);

        using var reader = cmd.ExecuteReader();

        return reader.Read() ? (ProjectRole)reader.GetInt32(0) : ProjectRole.None;
    }

    /// <summary>
    /// Lists the assignments of the given project.
    /// </summary>
    public static List<AssignmentEntry> ListByProject(CommandProvider cp, int projectId)
    {
        var cmd = cp.CreateCommand("select UID, PROLE, PINNED from ASSIGNMENT where PID = @PID;");
        cmd.Parameters.AddWithValue("@PID", projectId);

        using var reader = cmd.ExecuteReader();

        List<AssignmentEntry> result = [];
        while (reader.Read())
        {
            int userId = reader.GetInt32(0);
            ProjectRole projectRole = (ProjectRole)reader.GetInt32(1);
            bool pinned = reader.GetInt32(2) == 1;
            result.Add(new(projectId, userId, projectRole, pinned));
        }

        return result;
    }

    /// <summary>
    /// Lists the assignments of the given user.
    /// </summary>
    public static List<AssignmentEntry> ListByUser(CommandProvider cp, int userId)
    {
        var cmd = cp.CreateCommand("select PID, PROLE, PINNED from ASSIGNMENT where UID = @UID;");
        cmd.Parameters.AddWithValue("@UID", userId);

        using var reader = cmd.ExecuteReader();

        List<AssignmentEntry> result = [];
        while (reader.Read())
        {
            int projectId = reader.GetInt32(0);
            ProjectRole projectRole = (ProjectRole)reader.GetInt32(1);
            bool pinned = reader.GetInt32(2) == 1;
            result.Add(new(projectId, userId, projectRole, pinned));
        }

        return result;
    }

    /// <summary>
    /// Creates a new assignment of the given user to the given project, giving the user the given role.
    /// </summary>
    public static AssignmentEntry CreateAssignment(CommandProvider cp, int projectId, int userId, ProjectRole projectRole, bool pinned = false)
    {
        var cmd = cp.CreateCommand("insert into ASSIGNMENT (PID, UID, PROLE, PINNED) values (@PID, @UID, @PROLE, @PINNED);");
        cmd.Parameters.AddWithValue("@PID", projectId);
        cmd.Parameters.AddWithValue("@UID", userId);
        cmd.Parameters.AddWithValue("@PROLE", (int)projectRole);
        cmd.Parameters.AddWithValue("@PINNED", pinned ? 1 : 0);

        cmd.ExecuteNonQuery();

        return new(projectId, userId, projectRole, pinned);
    }

    /// <summary>
    /// Deletes the assignment of the given user to the given project.
    /// </summary>
    public static void DeleteAssignment(CommandProvider cp, int projectId, int userId)
    {
        var cmd = cp.CreateCommand("delete from ASSIGNMENT where PID = @PID and UID = @UID;");
        cmd.Parameters.AddWithValue("@PID", projectId);
        cmd.Parameters.AddWithValue("@UID", userId);

        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Sets the given pinned status for the given project and the given user
    /// </summary>
    public static void SetPinnedStatus(CommandProvider cp, int projectId, int userId, bool newPinnedStatus)
    {
        var cmd = cp.CreateCommand("UPDATE ASSIGNMENT SET PINNED = @PINNED WHERE PID = @PID AND UID = @UID;");
        cmd.Parameters.AddWithValue("@PID", projectId);
        cmd.Parameters.AddWithValue("@UID", userId);
        cmd.Parameters.AddWithValue("@PINNED", newPinnedStatus ? 1 : 0);

        cmd.ExecuteNonQuery();
    }
    
    /// <summary>
    /// Returns whether the given project was pinned by the given user.
    /// </summary>
    public static bool GetPinnedStatus(CommandProvider cp, int projectId, int userId)
    {
        var cmd = cp.CreateCommand("select PINNED from ASSIGNMENT where PID = @PID and UID = @UID;");
        cmd.Parameters.AddWithValue("@PID", projectId);
        cmd.Parameters.AddWithValue("@UID", userId);

        using var reader = cmd.ExecuteReader();

        return reader.Read() && reader.GetBoolean(0);
    }
}