namespace cocoding;

public class FileGroupSession(string id, string color, int userId, string circuitId, bool isMobileDevice, int? lineNumber = null)
{
    public readonly string Id = id;

    public readonly string Color = color;

    public readonly int UserId = userId;

    public int? LineNumber = lineNumber;

    public readonly HashSet<string> CircuitIds = [circuitId];

    public readonly bool IsMobileDevice = isMobileDevice;

    public DateTime TimeStamp = DateTime.UtcNow;

}