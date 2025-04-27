namespace cocoding.Data;

/// <summary>
/// Entry of the MESSAGE table.
/// </summary>
public class MessageEntry(int id, int projectId, int userId, string text, DateTime timestamp, int? selectionId, int? responseTo) : IComparable
{
    /// <summary>
    /// MID | INT
    /// </summary>
    public int Id = id;
        
    /// <summary>
    /// PID | INT
    /// </summary>
    public int ProjectId = projectId;
        
    /// <summary>
    /// UID | INT
    /// </summary>
    public int UserId = userId;

    /// <summary>
    /// NAME | VARCHAR(200)
    /// </summary>
    public string Text = text;
    
    /// <summary>
    /// TIMESTAMP | BIGINT | ticks of DateTime, prefers date from memory
    /// </summary>
    public DateTime Timestamp = timestamp;
    
    /// <summary>
    /// SID | INT | can be null
    /// </summary>
    public int? SelectionId = selectionId;

    /// <summary>
    /// RESPONSE_TO | INT | can be null
    /// </summary>
    public int? ResponseTo = responseTo;
    
    public int CompareTo(object? otherObj)
    {
        if (otherObj is not MessageEntry other)
            return 1;
        
        var result = Timestamp.CompareTo(other.Timestamp);
        if (result == 0)
            result = string.Compare(Text, other.Text, StringComparison.CurrentCulture);
        
        return result;
    }
}