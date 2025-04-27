using cocoding.Components.BrowseTree;
using cocoding.Data;

namespace cocoding;

/// <summary>
/// Injectable service for shared data.
/// </summary>
public class SharedDataService
{
    
    /// <summary>
    /// Event that gets called if any of the values (except <c>ExpandedFolderIds</c>, <c>ContextProjectData</c>, <c>ContextFolderData</c> and <c>ContextFile</c>) are modified.
    /// </summary>
    public event Action? Changed;

    /// <summary>
    /// Invoked when the ydoc is loaded.
    /// </summary>
    public Func<Task>? OnDocLoaded { get; set; } 
    
    private bool _MobileSidebarShown = false;
    /// <summary>
    /// Whether the sidebar is being shown on mobile devices or in narrow windows.
    /// </summary>
    public bool MobileSidebarShown
    {
        get => _MobileSidebarShown;
        set
        {
            _MobileSidebarShown = value;
            Changed?.Invoke();
        }
    }

    private FileEntry? _ViewedFile = null;
    /// <summary>
    /// The currently viewed file in the editor.
    /// </summary>
    public FileEntry? ViewedFile
    {
        get => _ViewedFile;
        set
        {
            _ViewedFile = value;
            Changed?.Invoke();
        }
    }

    /// <summary>
    /// The set of expanded folder IDs.<br/>
    /// Modifying the set does not call the <c>Changed</c> event!
    /// </summary>
    public readonly Dictionary<int, HashSet<int>> ExpandedFolderIds = [];

    private ContextMenu _ContextMenu = ContextMenu.None;
    /// <summary>
    /// The selected context menu option.
    /// </summary>
    public ContextMenu ContextMenu
    {
        get => _ContextMenu;
        set
        {
            _ContextMenu = value;
            Changed?.Invoke();
        }
    }

    /// <summary>
    /// The last project data selected for a context menu. <br/>
    /// The value isn't being set to null to avoid flashing during the context menu's closing animation!<br/>
    /// Modifying the set does not call the <c>Changed</c> event!
    /// </summary>
    public ProjectNodeData? ContextProjectData { get; internal set; } = null;

    /// <summary>
    /// The last folder data selected for a context menu. <br/>
    /// The value isn't being set to null to avoid flashing during the context menu's closing animation!<br/>
    /// Modifying the set does not call the <c>Changed</c> event!
    /// </summary>
    public FolderNodeData? ContextFolderData { get; internal set; } = null;

    /// <summary>
    /// The last file selected for a context menu. <br/>
    /// The value isn't being set to null to avoid flashing during the context menu's closing animation!<br/>
    /// Modifying the set does not call the <c>Changed</c> event!
    /// </summary>
    public FileEntry? ContextFile { get; internal set; } = null;

    /// <summary>
    /// Opens the "options" context menu for the given project data.
    /// </summary>
    public void OpenMenu(ProjectNodeData projectNodeData)
    {
        ContextProjectData = projectNodeData;
        ContextMenu = ContextMenu.Project_Options;
        Changed?.Invoke();
    }

    /// <summary>
    /// Opens the "options" context menu for the given folder data.
    /// </summary>
    public void OpenMenu(FolderNodeData folderNodeData)
    {
        ContextFolderData = folderNodeData;
        ContextMenu = ContextMenu.Folder_Options;
        Changed?.Invoke();
    }

    /// <summary>
    /// Opens the "options" context menu for the given file.
    /// </summary>
    public void OpenMenu(FileEntry file)
    {
        ContextFile = file;
        ContextMenu = ContextMenu.File_Options;
        Changed?.Invoke();
    }

    /// <summary>
    /// Closes the context menu.
    /// </summary>
    public void HideAllMenus()
    {
        ContextMenu = ContextMenu.None;
        Changed?.Invoke();
    }
}