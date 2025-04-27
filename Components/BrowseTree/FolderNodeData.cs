using cocoding.Data;

namespace cocoding.Components.BrowseTree;

public class FolderNodeData(FolderEntry nodeFolder) : IBrowseNodeData
{
    /// <summary>
    /// This node's associated folder.
    /// </summary>
    public FolderEntry Folder = nodeFolder;
    
    public override string Name => Folder.Name;
    
    protected override int ProjectId => Folder.ProjectId;

    protected override void LoadChildren()
    {
        Files = new(Database.Transaction(cp => FileTable.ListByFolder(cp, Folder.Id)));
        Folders = new(Database.Transaction(cp => FolderTable.ListByFolder(cp, Folder.Id)).Select(f => new FolderNodeData(f)));
    }

    /// <summary>
    /// Handles the change of a folder's metadata.
    /// </summary>
    public bool HandleFolderMetadataChanged(FolderEntry folder)
    {
        if (folder.ProjectId != ProjectId)
            return false;

        if (folder.Id == Folder.Id)
        {
            Folder = folder;
            Instance?.CallStateHasChanged();
            return true;
        }
        
        if (Folders != null && Folders.Any(f => f.HandleFolderMetadataChanged(folder)))
        {
            if (folder.ParentId == Folder.Id)
                Instance?.CallStateHasChanged();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Handles the creation of a folder.
    /// </summary>
    public bool HandleFolderCreated(FolderEntry folder)
    {
        if (folder.ProjectId != Folder.ProjectId || Folders == null)
            return false;

        if (folder.ParentId == Folder.Id)
        {
            Folders.Add(new(folder) { Expanded = true });
            Instance?.CallStateHasChanged();
            return true;
        }
        
        return Folders.Any(child => child.HandleFolderCreated(folder));
    }

    /// <summary>
    /// Handles the creation of a file.
    /// </summary>
    public bool HandleFileCreated(FileEntry file)
    {
        if (file.ProjectId != Folder.ProjectId || Folders == null || Files == null)
            return false;

        if (file.ParentId == Folder.Id)
        {
            Files.Add(file);
            Instance?.CallStateHasChanged();
            return true;
        }

        return Folders.Any(child => child.HandleFileCreated(file));
    }

    /// <summary>
    /// Recursively expands the given set of folder IDs while removing the IDs that were successfully expanded.
    /// </summary>
    public void Expand(HashSet<int> folderIds)
    {
        if (!folderIds.Remove(Folder.Id))
            return;
        
        if (!Expanded)
            Expanded = true;
        
        if (Folders != null)
            foreach (var folderData in Folders)
                folderData.Expand(folderIds);
    }
}