namespace cocoding.Data;

/// <summary>
/// Entry of the FOLDER table.
/// </summary>
public class FolderEntry(int id, int projectId, int? parentId, string name)
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

    public int CompareTo(object? otherObj)
    {
        if (otherObj is not FolderEntry other)
            return 1;
        
        var result = string.Compare(Name, other.Name, StringComparison.CurrentCulture);
        if (result == 0)
            result = Id.CompareTo(other.Id);
        
        return result;
    }
}