namespace cocoding.Data;

/// <summary>
/// Methods for the FOLDER table.
/// </summary>
public static partial class FolderTable
{
    /// <summary>
    /// Returns the folder with the given ID or null if no such folder exists.
    /// </summary>
    public static FolderEntry? GetById(CommandProvider cp, int folderId)
    {
        var cmd = cp.CreateCommand("select PID, PARENT, NAME from FOLDER where FID = @FID;");
        cmd.Parameters.AddWithValue("@FID", folderId);

        using var reader = cmd.ExecuteReader();

        if (!reader.Read())
            return null;

        int projectId = reader.GetInt32(0);
        int? parentId = reader.GetNullableInt32(1);
        string name = reader.GetString(2);

        return new(folderId, projectId, parentId, name);
    }
    
    /// <summary>
    /// Lists all folders in the given project's root folder.
    /// </summary>
    public static List<FolderEntry> ListByProjectRoot(CommandProvider cp, int projectId)
    {
        List<FolderEntry> result = [];

        var cmd = cp.CreateCommand("select FID, NAME from FOLDER where PID = @PID and PARENT is null;");
        cmd.Parameters.AddWithValue("@PID", projectId);

        using var reader = cmd.ExecuteReader();
        
        while (reader.Read())
        {
            int folderId = reader.GetInt32(0);
            string name = reader.GetString(1);

            result.Add(new(folderId, projectId, null, name));
        }
        
        return result;
    }
    
    /// <summary>
    /// Lists all folders in the given folder.
    /// </summary>
    public static List<FolderEntry> ListByFolder(CommandProvider cp, int parentId)
    {
        List<FolderEntry> result = [];

        var cmd = cp.CreateCommand("select FID, PID, NAME from FOLDER where PARENT = @PARENT;");
        cmd.Parameters.AddWithValue("@PARENT", parentId);

        using var reader = cmd.ExecuteReader();
        
        while (reader.Read())
        {
            int folderId = reader.GetInt32(0);
            int projectId = reader.GetInt32(1);
            string name = reader.GetString(2);

            result.Add(new(folderId, projectId, parentId, name));
        }
        
        return result;
    }

    /// <summary>
    /// Creates a new folder with a unique ID using the given project ID, parent ID and name.
    /// !!!DO NOT USE THIS METHOD DIRECTLY BUT USE THE METHOD IN FILE_MANAGER INSTEAD!!!
    /// </summary>
    public static FolderEntry CreateFolder(CommandProvider cp, int projectId, int? parentId, string name)
    {
        int folderId;
        do folderId = Security.RandomInt();
        while (GetById(cp, folderId) != null);

        var cmd = cp.CreateCommand("insert into FOLDER (FID, PID, PARENT, NAME) values (@FID, @PID, @PARENT, @NAME);");
        cmd.Parameters.AddWithValue("@FID", folderId);
        cmd.Parameters.AddWithValue("@PID", projectId);
        cmd.Parameters.AddWithValue("@PARENT", parentId);
        cmd.Parameters.AddWithValue("@NAME", name);

        cmd.ExecuteNonQuery();

        return new(folderId, projectId, parentId, name);
    }

    /// <summary>
    /// Deletes the folder with the given ID.
    /// !!!DO NOT USE THIS METHOD DIRECTLY BUT USE THE METHOD IN PROJECT_MANAGER INSTEAD!!!
    /// </summary>
    public static bool DeleteFolder(CommandProvider cp, int folderId)
    {
        var cmd = cp.CreateCommand("delete from FOLDER where FID = @FID;");
        cmd.Parameters.AddWithValue("@FID", folderId);

        return cmd.ExecuteNonQuery() > 0;
    }

    /// <summary>
    /// Changes the name of the given folder to the given name.
    /// !!!DO NOT USE THIS METHOD DIRECTLY BUT USE THE METHOD IN FILE_MANAGER INSTEAD!!!
    /// </summary>
    public static bool RenameFolder(CommandProvider cp, int folderId, string name)
    {
        var cmd = cp.CreateCommand("update FOLDER set NAME = @NAME where FID = @FID;");
        cmd.Parameters.AddWithValue("@FID", folderId);
        cmd.Parameters.AddWithValue("@NAME", name);

        return cmd.ExecuteNonQuery() > 0;
    }
}