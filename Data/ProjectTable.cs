namespace cocoding.Data;

/// <summary>
/// Methods for the PROJECT table.
/// </summary>
public static partial class ProjectTable
{
    /// <summary>
    /// Returns the project with the given ID or null if no such project exists.
    /// </summary>
    public static ProjectEntry? GetById(CommandProvider cp, int projectId)
    {
        var cmd = cp.CreateCommand("select NAME, DESCRIPTION, INDENT from PROJECT where PID = @PID;");
        cmd.Parameters.AddWithValue("@PID", projectId);

        using var reader = cmd.ExecuteReader();

        if (!reader.Read())
            return null;

        string name = reader.GetString(0);
        string? description = reader.GetNullableString(1);
        int indent = reader.GetInt32(2);

        return new(projectId, name, description, indent);
    }

    /// <summary>
    /// Lists all projects.
    /// </summary>
    public static List<ProjectEntry> ListAll(CommandProvider cp)
    {
        var cmd = cp.CreateCommand("select PID, NAME, DESCRIPTION, INDENT from PROJECT order by NAME;");

        using var reader = cmd.ExecuteReader();

        List<ProjectEntry> result = [];
        while (reader.Read())
        {
            int projectId = reader.GetInt32(0);
            string name = reader.GetString(1);
            string? description = reader.GetNullableString(2);
            int indent = reader.GetInt32(3);
            result.Add(new(projectId, name, description, indent));
        }

        return result;
    }

    /// <summary>
    /// Creates a new project with a unique ID using the given name and description.
    /// </summary>
    public static ProjectEntry CreateProject(CommandProvider cp, string name, string? description, int indent)
    {
        int projectId;
        do projectId = Security.RandomInt();
        while (GetById(cp, projectId) != null);

        var cmd = cp.CreateCommand("insert into PROJECT (PID, NAME, DESCRIPTION, INDENT) values (@PID, @NAME, @DESCRIPTION, @INDENT)");
        cmd.Parameters.AddWithValue("@PID", projectId);
        cmd.Parameters.AddWithValue("@NAME", name);
        cmd.Parameters.AddWithValue("@DESCRIPTION", description);
        cmd.Parameters.AddWithValue("@INDENT", indent);

        cmd.ExecuteNonQuery();

        return new(projectId, name, description, indent);
    }
    
    /// <summary>
    /// Edits an existing project.
    /// </summary>
    public static bool EditProject(CommandProvider cp, int projectId, string name, string? description, int indent)
    {
        var cmd = cp.CreateCommand("update PROJECT set NAME = @NAME, DESCRIPTION = @DESCRIPTION, INDENT = @INDENT where PID = @PID");
        cmd.Parameters.AddWithValue("@PID", projectId);
        cmd.Parameters.AddWithValue("@NAME", name);
        cmd.Parameters.AddWithValue("@DESCRIPTION", description);
        cmd.Parameters.AddWithValue("@INDENT", indent);

        return cmd.ExecuteNonQuery() > 0;
    }
    
    /// <summary>
    /// Deletes an existing project.
    /// </summary>
    public static bool DeleteProject(CommandProvider cp, int projectId)
    {
        var cmd = cp.CreateCommand("delete from PROJECT where PID = @PID");
        cmd.Parameters.AddWithValue("@PID", projectId);

        return cmd.ExecuteNonQuery() > 0;
    }
}