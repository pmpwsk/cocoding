namespace cocoding.Data;

/// <summary>
/// Entry of the VERSION table.
/// </summary>
public class VersionEntry(int versionId, int fileId, string name, DateTime changed, int userId, string? label, string? comment)
{
    /// <summary>
    /// VID | INT
    /// </summary>
    public readonly int VersionId = versionId;
    
    /// <summary>
    /// FID | INT
    /// </summary>
    public readonly int FileId = fileId;
    
    /// <summary>
    /// NAME | VARCHAR(50)
    /// </summary>
    public readonly string Name = name;

    /// <summary>
    /// CHANGED | BIGINT
    /// </summary>
    public readonly DateTime Changed = changed;
    
    /// <summary>
    /// UID | INT
    /// </summary>
    public readonly int UserId = userId;
    
    
    /// <summary>
    /// LABEL | VARCHAR(25)
    /// </summary>
    public readonly string? Label = label;
    
    /// <summary>
    /// COMMENT | VARCHAR(200)
    /// </summary>
    public readonly string? Comment = comment;
}