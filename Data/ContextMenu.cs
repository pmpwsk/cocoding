namespace cocoding.Data;

/// <summary>
/// Enumeration of possible context menus to show.
/// </summary>
public enum ContextMenu
{
    /// <summary>
    /// No context menu.
    /// </summary>
    None,
    
    
    /// <summary>
    /// Options for projects.
    /// </summary>
    Project_Options,
    
    /// <summary>
    /// Folder creation within a project.
    /// </summary>
    Project_CreateFolder,
    
    /// <summary>
    /// File creation within a project.
    /// </summary>
    Project_CreateFile,
    
    
    /// <summary>
    /// Options for folders.
    /// </summary>
    Folder_Options,
    
    /// <summary>
    /// Folder creation within a folder.
    /// </summary>
    Folder_CreateFolder,
    
    /// <summary>
    /// File creation within a folder.
    /// </summary>
    Folder_CreateFile,
    
    /// <summary>
    /// Folder renaming.
    /// </summary>
    Folder_Rename,
    
    /// <summary>
    /// Folder deletion.
    /// </summary>
    Folder_Delete,
    
    
    /// <summary>
    /// Options for files.
    /// </summary>
    File_Options,
    
    /// <summary>
    /// File renaming.
    /// </summary>
    File_Rename,
    
    /// <summary>
    /// File deletion.
    /// </summary>
    File_Delete,
    
    /// <summary>
    /// File details.
    /// </summary>
    File_Details
}