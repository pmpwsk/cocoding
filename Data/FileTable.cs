namespace cocoding.Data;

/// <summary>
/// Methods for the FILE table.
/// </summary>
public static partial class FileTable
{
    /// <summary>
    /// Returns the file with the given ID or null if no such file exists.
    /// </summary>
    public static FileEntry? GetById(CommandProvider cp, int fileId)
    {
        var cmd = cp.CreateCommand("select PID, PARENT, NAME, CHANGED, UID from FILE where FID = @FID;");
        cmd.Parameters.AddWithValue("@FID", fileId);

        using var reader = cmd.ExecuteReader();

        if (!reader.Read())
            return null;

        int projectId = reader.GetInt32(0);
        int? parentId = reader.GetNullableInt32(1);
        string name = reader.GetString(2);
        DateTime changed = new(reader.GetInt64(3));
        int userId = reader.GetInt32(4);

        return new(fileId, projectId, parentId, name, changed, userId);
    }
    
    /// <summary>
    /// Lists all files in the given project's root folder.
    /// </summary>
    public static List<FileEntry> ListByProjectRoot(CommandProvider cp, int projectId)
    {
        List<FileEntry> result = [];

        var cmd = cp.CreateCommand("select FID, NAME, CHANGED, UID from FILE where PID = @PID and PARENT is null;");
        cmd.Parameters.AddWithValue("@PID", projectId);

        using var reader = cmd.ExecuteReader();
        
        while (reader.Read())
        {
            int fileId = reader.GetInt32(0);
            string name = reader.GetString(1);
            DateTime changed = new(reader.GetInt64(2));
            int userId = reader.GetInt32(3);

            result.Add(new(fileId, projectId, null, name, changed, userId));
        }
        
        return result;
    }
    
    /// <summary>
    /// Lists all files in the given folder.
    /// </summary>
    public static List<FileEntry> ListByFolder(CommandProvider cp, int folderId)
    {
        List<FileEntry> result = [];

        var cmd = cp.CreateCommand("select FID, PID, NAME, CHANGED, UID from FILE where PARENT = @PARENT;");
        cmd.Parameters.AddWithValue("@PARENT", folderId);

        using var reader = cmd.ExecuteReader();
        
        while (reader.Read())
        {
            int fileId = reader.GetInt32(0);
            int projectId = reader.GetInt32(1);
            string name = reader.GetString(2);
            DateTime changed = new(reader.GetInt64(3));
            int userId = reader.GetInt32(4);

            result.Add(new(fileId, projectId, folderId, name, changed, userId));
        }
        
        return result;
    }

    /// <summary>
    /// Creates a new file with a unique ID using the given project ID, parent ID, name and change date.
    /// !!!DO NOT USE THIS METHOD DIRECTLY BUT USE THE METHOD IN FILE_MANAGER INSTEAD!!!
    /// </summary>
    public static FileEntry CreateFile(CommandProvider cp, int projectId, int? parentId, string name, DateTime changed, int userId)
    {
        int fileId;
        do fileId = Security.RandomInt();
        while (GetById(cp, fileId) != null);

        var cmd = cp.CreateCommand("insert into FILE (FID, PID, PARENT, NAME, CHANGED, UID) values (@FID, @PID, @PARENT, @NAME, @CHANGED, @UID);");
        cmd.Parameters.AddWithValue("@FID", fileId);
        cmd.Parameters.AddWithValue("@PID", projectId);
        cmd.Parameters.AddWithValue("@PARENT", parentId);
        cmd.Parameters.AddWithValue("@NAME", name);
        cmd.Parameters.AddWithValue("@CHANGED", changed.Ticks);
        cmd.Parameters.AddWithValue("@UID", userId);

        cmd.ExecuteNonQuery();
        
        SetFileState(fileId, [[0,0,0,0,0,0,1,0,0,0,0,0,0]]);

        return new(fileId, projectId, parentId, name, changed, userId);
    }

    /// <summary>
    /// Sets the changed date and user ID of the given file to the given date and user ID.
    /// </summary>
    public static bool SetChangeData(CommandProvider cp, int fileId, DateTime changed, int userId)
    {
        var cmd = cp.CreateCommand("update FILE set CHANGED = @CHANGED, UID = @UID where FID = @FID;");
        cmd.Parameters.AddWithValue("@FID", fileId);
        cmd.Parameters.AddWithValue("@CHANGED", changed.Ticks);
        cmd.Parameters.AddWithValue("@UID", userId);

        return cmd.ExecuteNonQuery() > 0;
    }

    /// <summary>
    /// Deletes the file with the given ID.
    /// </summary>
    public static void DeleteFile(CommandProvider cp, int fileId)
    {
        var cmd = cp.CreateCommand("delete from FILE where FID = @FID;");
        cmd.Parameters.AddWithValue("@FID", fileId);

        EditorHub.FileGroups.Remove(fileId, out _);

        cmd.ExecuteNonQuery();
        
        File.Delete($"../FileStates/{fileId}.bin");
    }

    /// <summary>
    /// Loads and deserializes the state of the given file (version).
    /// </summary>
    public static List<byte[]>? GetFileState(int fileId, int versionId = 0, bool version = false)
    {
        string versionString = version ? "_" + versionId : "";
        if (!File.Exists($"../FileStates/{fileId}{versionString}.bin"))
            return null;
        using FileStream stream = new($"../FileStates/{fileId}{versionString}.bin", FileMode.Open, FileAccess.Read);

        List<byte[]> result = [];

        // Read update count
        byte[] buffer = new byte[4];
        stream.ReadExactly(buffer);
        int count = BitConverter.ToInt32(buffer);

        for (int i = 0; i < count; i++)
        {
            // Read update length
            buffer = new byte[4];
            stream.ReadExactly(buffer);
            int length = BitConverter.ToInt32(buffer);

            // Read update
            buffer = new byte[length];
            stream.ReadExactly(buffer);

            result.Add(buffer);
        }

        return result;
    }

    /// <summary>
    /// Serializes and saves the given state for the given file (version).
    /// </summary>
    public static void SetFileState(int fileId, List<byte[]> state, int versionId = 0, bool version = false)
    {
        string versionString = version ? "_" + versionId : "";
        using FileStream stream = new($"../FileStates/{fileId}{versionString}.bin", FileMode.Create, FileAccess.Write);

        // Write update count
        stream.Write(BitConverter.GetBytes(state.Count));

        foreach (var update in state)
        {
            // Write update length
            stream.Write(BitConverter.GetBytes(update.Length));

            // Write update
            stream.Write(update);
        }
    }

    /// <summary>
    /// Changes the name of the given file to the given name.
    /// !!!DO NOT USE THIS METHOD DIRECTLY BUT USE THE METHOD IN FILE_MANAGER INSTEAD!!!
    /// </summary>
    public static bool RenameFile(CommandProvider cp, int fileId, string name)
    {
        var cmd = cp.CreateCommand("update FILE set NAME = @NAME where FID = @FID;");
        cmd.Parameters.AddWithValue("@FID", fileId);
        cmd.Parameters.AddWithValue("@NAME", name);

        return cmd.ExecuteNonQuery() > 0;
    }
    

    /// <summary>
    /// Lists all the files in the given folder and in subfolders
    /// </summary>
    public static List<FileEntry> ListByFolderRecursively(CommandProvider cp, int folderId, List<FileEntry>? result = null)
    {
        result ??= [];
        
        foreach (var folder in FolderTable.ListByFolder(cp, folderId))
            ListByFolderRecursively(cp, folder.Id, result);
        result.AddRange(ListByFolder(cp, folderId));

        return result;
    }


    /// <summary>
    /// Lists all the files in the given project and in its subfolders
    /// </summary>
    public static List<FileEntry> ListByProject(CommandProvider cp, int projectId)
    {
        List<FileEntry> result = [];

        var cmd = cp.CreateCommand("select FID, NAME, CHANGED, PARENT, UID from FILE where PID = @PID;");
        cmd.Parameters.AddWithValue("@PID", projectId);

        using var reader = cmd.ExecuteReader();
        
        while (reader.Read())
        {
            int fileId = reader.GetInt32(0);
            string name = reader.GetString(1);
            DateTime changed = new(reader.GetInt64(2));
            int? parentId = reader.GetNullableInt32(3);
            int userId = reader.GetInt32(4);

            result.Add(new(fileId, projectId, parentId, name, changed, userId));
        }
        
        return result;
    }
}