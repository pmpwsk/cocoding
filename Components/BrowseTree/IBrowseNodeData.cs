using cocoding.Data;

namespace cocoding.Components.BrowseTree;

/// <summary>
/// Abstract class for project and folder node data.
/// </summary>
public abstract class IBrowseNodeData
{
    private bool _Expanded = false;
    /// <summary>
    /// Whether this node is expanded or not.<br/>
    /// When the value is set to true and the children haven't been loaded yet, the children will be loaded. 
    /// </summary>
    public bool Expanded
    {
        get => _Expanded;
        set
        {
            if (value && (Folders == null || Files == null))
                LoadChildren();
            
            _Expanded = value;
            Instance?.CallStateHasChanged();
        }
    }

    /// <summary>
    /// This node's immediate folder children as data objects.
    /// </summary>
    public List<FolderNodeData>? Folders;
    
    /// <summary>
    /// This node's immediate file children.
    /// </summary>
    public List<FileEntry>? Files;

    /// <summary>
    /// The <c>BrowseNode</c> component instance associated with this node to notify about changes.
    /// </summary>
    public BrowseNode? Instance;
    
    /// <summary>
    /// Gets the displayed name for this node.
    /// </summary>
    public abstract string Name { get; }
    
    /// <summary>
    /// Gets the current project ID.
    /// </summary>
    protected abstract int ProjectId { get; }

    /// <summary>
    /// Loads the children into <c>Folders</c> and <c>Files</c>.
    /// </summary>
    protected abstract void LoadChildren();
    
    /// <summary>
    /// Recursively enumerates the IDs of all expanded folder children.
    /// </summary>
    public IEnumerable<int> EnumerateExpandedFolderIds()
    {
        if (Folders == null)
            yield break;
        
        foreach (var folderData in Folders)
        {
            if (folderData.Expanded)
                yield return folderData.Folder.Id;
            
            foreach (var id in folderData.EnumerateExpandedFolderIds())
                yield return id;
        }
    }

    /// <summary>
    /// Handles the change of a file's metadata.
    /// </summary>
    public bool HandleFileMetadataChanged(FileEntry file)
    {
        if (file.ProjectId != ProjectId)
            return false;
        
        if (Files != null && Files.RemoveAll(f => f.Id == file.Id) > 0)
        {
            Files.Add(file);
            Instance?.CallStateHasChanged();
            return true;
        }

        return Folders != null && Folders.Any(f => f.HandleFileMetadataChanged(file));
    }

    /// <summary>
    /// Handles the deletion of a folder.
    /// </summary>
    public bool HandleFolderDeleted(FolderEntry folder)
    {
        if (folder.ProjectId != ProjectId || Folders == null)
            return false;
        
        foreach (var child in Folders.Where(child => child.Folder.Id == folder.Id))
        {
            Folders.Remove(child);
            Instance?.CallStateHasChanged();
            return true;
        }

        return Folders.Any(child => child.HandleFolderDeleted(folder));
    }

    /// <summary>
    /// Handles the deletion of a file.
    /// </summary>
    public bool HandleFileDeleted(FileEntry file)
    {
        if (file.ProjectId != ProjectId || Folders == null || Files == null)
            return false;
        
        foreach (var child in Files.Where(child => child.Id == file.Id))
        {
            Files.Remove(child);
            Instance?.CallStateHasChanged();
            return true;
        }

        return Folders.Any(child => child.HandleFileDeleted(file));
    }
}