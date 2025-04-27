namespace cocoding.Data;

/// <summary>
/// Entry of the FILE table.
/// </summary>
public class FileEntry(int id, int projectId, int? parentId, string name, DateTime changed, int userId)
{
    /// <summary>
    /// FID | INT
    /// </summary>
    public int Id = id;
        
    /// <summary>
    /// PID | INT
    /// </summary>
    public int ProjectId = projectId;
        
    /// <summary>
    /// PARENT | INT | can be null
    /// </summary>
    public int? ParentId = parentId;

    /// <summary>
    /// NAME | VARCHAR(50)
    /// </summary>
    public string Name = name;

    private DateTime _Changed = changed;
    /// <summary>
    /// CHANGED | BIGINT | ticks of DateTime, prefers date from memory
    /// </summary>
    public DateTime Changed
        => EditorHub.FileGroups.TryGetValue(Id, out var fileGroup) ? fileGroup.Changed : _Changed;

    private int _UserId = userId;

    /// <summary>
    /// UID | INT
    /// </summary>
    public int UserId
        => EditorHub.FileGroups.TryGetValue(Id, out var fileGroup) ? fileGroup.UserId : _UserId;
}