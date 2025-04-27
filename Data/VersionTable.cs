namespace cocoding.Data;

/// <summary>
/// Methods for the VERSION table.
/// </summary>
public static partial class VersionTable
{

    /// <summary>
    /// Returns the version with the given ID or null if none exists.
    /// </summary>
    public static VersionEntry? GetById(CommandProvider cp, int versionId)
    {
        var cmd = cp.CreateCommand("select * from VERSION where VID = @VID;");
        cmd.Parameters.AddWithValue("@VID", versionId);

        using var reader = cmd.ExecuteReader();

        if (!reader.Read())
            return null;

        int fileId = reader.GetInt32(1);
        string name = reader.GetString(2);
        DateTime changed = new(reader.GetInt64(3));
        int userId = reader.GetInt32(4);
        string? label = reader.IsDBNull(5) ? null : reader.GetString(5);
        string? comment = reader.IsDBNull(6) ? null : reader.GetString(6);
        return new(versionId, fileId, name, changed, userId, label, comment);
    }

    /// <summary>
    /// Lists all versions for the given file.
    /// </summary>
    public static List<VersionEntry> ListVersions(CommandProvider cp, int fileOfInterestId)
    {
        List<VersionEntry> result = [];

        var cmd = cp.CreateCommand("select * from VERSION where FID = @FID;");
        cmd.Parameters.AddWithValue("@FID", fileOfInterestId);

        using var reader = cmd.ExecuteReader();
        
        while (reader.Read())
        {
            int versionId = reader.GetInt32(0); 
            int fileId = reader.GetInt32(1);
            string name = reader.GetString(2);
            DateTime changed = new(reader.GetInt64(3));
            int userId = reader.GetInt32(4);
            string? label = reader.IsDBNull(5) ? null : reader.GetString(5);
            string? comment = reader.IsDBNull(6) ? null : reader.GetString(6);
            result.Add(new(versionId, fileId, name, changed, userId, label, comment));
        }
        
        return result;
    }
    
    /// <summary>
    /// Creates a new version with a unique ID using the given the file ID, name, change date and user ID.
    /// </summary>
    public static VersionEntry CreateVersion(CommandProvider cp, int fileId, string name, DateTime changed, int userId, string? label, string? comment, List<byte[]> state)
    {
        int versionId;
        do versionId = Security.RandomInt();
        while (GetById(cp, versionId) != null);

        var cmd = cp.CreateCommand("insert into VERSION (VID, FID, NAME, CHANGED, UID, LABEL, COMMENT) values (@VID, @FID, @NAME, @CHANGED, @UID, @LABEL, @COMMENT);");
        cmd.Parameters.AddWithValue("@VID", versionId);
        cmd.Parameters.AddWithValue("@FID", fileId);
        cmd.Parameters.AddWithValue("@NAME", name);
        cmd.Parameters.AddWithValue("@CHANGED", changed.Ticks);
        cmd.Parameters.AddWithValue("@UID", userId);
        cmd.Parameters.AddWithValue("@LABEL", label);
        cmd.Parameters.AddWithValue("@COMMENT", comment);

        cmd.ExecuteNonQuery();
        
        FileTable.SetFileState(fileId, state, versionId, true);
        
        return new(versionId, fileId, name, changed, userId, label, comment);
    }

    /// <summary>
    /// Deletes the version with the given ID.
    /// </summary>
    public static void DeleteVersion(CommandProvider cp, int versionId)
    {
        VersionEntry? entry = GetById(cp, versionId);
        
        if (entry == null)
            return;
        
        var cmd = cp.CreateCommand("delete from VERSION where VID = @VID;");
        cmd.Parameters.AddWithValue("@VID", versionId);

        cmd.ExecuteNonQuery();
        
        File.Delete($"../FileStates/{entry.FileId}_{versionId}.bin");
    }

}