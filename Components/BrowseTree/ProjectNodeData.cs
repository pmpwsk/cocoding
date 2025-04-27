using cocoding.Data;

namespace cocoding.Components.BrowseTree;

public class ProjectNodeData(ProjectEntry nodeProject) : IBrowseNodeData
{
    /// <summary>
    /// This node's associated project.
    /// </summary>
    public ProjectEntry Project = nodeProject;
    
    public override string Name => Project.Name;
    
    protected override int ProjectId => Project.Id;

    protected override void LoadChildren()
    {
        Files = new(Database.Transaction(cp => FileTable.ListByProjectRoot(cp, Project.Id)));
        Folders = new (Database.Transaction(cp => FolderTable.ListByProjectRoot(cp, Project.Id)).Select(f => new FolderNodeData(f)));
    }

    /// <summary>
    /// Handles the change of a project's metadata.
    /// </summary>
    public bool HandleProjectMetadataChanged(ProjectEntry project)
    {
        if (project.Id == Project.Id)
        {
            Project = project;
            Instance?.CallStateHasChanged();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Handles the change of a folder's metadata.
    /// </summary>
    public bool HandleFolderMetadataChanged(FolderEntry folder)
    {
        if (folder.ProjectId == ProjectId && Folders != null && Folders.Any(f => f.HandleFolderMetadataChanged(folder)))
        {
            if (folder.ParentId == null)
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
        if (folder.ProjectId != Project.Id || Folders == null)
            return false;

        if (folder.ParentId == null)
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
        if (file.ProjectId != Project.Id || Folders == null || Files == null)
            return false;

        if (file.ParentId == null)
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
        if (!Expanded)
            Expanded = true;
        
        if (Folders != null)
            foreach (var folderData in Folders)
                folderData.Expand(folderIds);
    }
}