namespace cocoding.Data;

/// <summary>
/// Enumeration of possible participant roles in a project.
/// </summary>
public enum ProjectRole
{
    /// <summary>
    /// Not part of the project.
    /// </summary>
    None = 0,
    
    /// <summary>
    /// A regular participant of the project.
    /// </summary>
    Participant = 1,

    /// <summary>
    /// A project manager.
    /// </summary>
    Manager = int.MaxValue-1,

    /// <summary>
    /// A project owner.
    /// </summary>
    Owner = int.MaxValue
}