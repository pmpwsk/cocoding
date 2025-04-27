namespace cocoding.Data;

/// <summary>
/// Entry of the SELECTION table.
/// </summary>
public class SelectionEntry(int selectionId, int fileId, double clientStart, double clockStart, double clientEnd, double clockEnd)
{
    /// <summary>
    /// SID | INT
    /// </summary>
    public readonly int SelectionId = selectionId;

    /// <summary>
    /// FID | INT
    /// </summary>
    public readonly int FileId = fileId;

    /// <summary>
    /// CLIENT_START | BIGINT
    /// </summary>
    public readonly double ClientStart = clientStart;

    /// <summary>
    /// CLOCK_START | BIGINT
    /// </summary>
    public readonly double ClockStart = clockStart;

    /// <summary>
    /// CLIENT_END | BIGINT
    /// </summary>
    public readonly double ClientEnd = clientEnd;

    /// <summary>
    /// CLOCK_END | BIGINT 
    /// </summary>
    public readonly double ClockEnd = clockEnd;

}