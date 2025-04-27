namespace cocoding.Data;

/// <summary>
/// Entry of the PROJECT table.
/// </summary>
public class ProjectEntry(int id, string name, string? description, int indent)
{
    /// <summary>
    /// UID | INT
    /// </summary>
    public int Id = id;

    /// <summary>
    /// NAME | VARCHAR(50)
    /// </summary>
    public string Name = name;

    /// <summary>
    /// DESCRIPTION | VARCHAR(200) | can be NULL
    /// </summary>
    public string? Description = description;
    
    /// <summary>
    /// INDENT | INT
    /// </summary>
    public int Indent = indent;
}