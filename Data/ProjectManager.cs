namespace cocoding.Data;

/// <summary>
/// Methods for project management.
/// </summary>
public static partial class ProjectManager
{
    /// <summary>
    /// Creates a new project while checking the parameters, and assigns the given user as the owner.<br/>
    /// Issues are returned as exceptions.
    /// </summary>
    public static ProjectEntry CreateProject(string name, string? description, int indent, int userId)
    {
        if (description == "")
            description = null;
        CheckProjectInformation(name, description, indent);

        var project = Database.Transaction(cp =>
        {
            var p = ProjectTable.CreateProject(cp, name, description, indent);
            AssignmentTable.CreateAssignment(cp, p.Id, userId, ProjectRole.Owner, pinned: false);
            return p;
        });

        return project;
    }

    /// <summary>
    /// Edits an existing project while checking the parameters.<br/>
    /// Issues are returned as exceptions.
    /// </summary>
    public static bool EditProject(int projectId, string name, string? description, int indent)
    {
        if (description == "")
            description = null;
        CheckProjectInformation(name, description, indent);

        return Database.Transaction(cp =>
            ProjectTable.EditProject(cp, projectId, name, description, indent)
        );
    }

    /// <summary>
    /// Checks whether the entered project information is valid or not.  
    /// </summary>
    private static void CheckProjectInformation(string name, string? description, int indent)
    {
        if (name == "")
            throw new Exception("Geben Sie einen Namen ein!");
        if (name.Length > 50)
            throw new Exception("Projektnamen dürfen höchstens 50 Zeichen lang sein!");
        if (description != null && description.Length > 200)
            throw new Exception("Projektbeschreibungen dürfen höchstens 200 Zeichen lang sein!");
        if (indent is < 1 or > 20)
            throw new Exception("Der Wert für die Einrückung muss zwischen 1 und 20 Spaces liegen!");
    }

    /// <summary>
    /// Deletes the given project recursively.
    /// </summary>
    public static void DeleteProject(int projectId)
        => Database.Transaction(cp =>
            DeleteProject(cp, projectId));

    private static void DeleteProject(CommandProvider cp, int projectId)
    {
        foreach (var folder in FolderTable.ListByProjectRoot(cp, projectId))
            DeleteFolderRecursively(cp, folder.Id);

        foreach (var file in FileTable.ListByProjectRoot(cp, projectId))
            DeleteFile(file.Id);

        foreach (var assignment in AssignmentTable.ListByProject(cp, projectId))
            AssignmentTable.DeleteAssignment(cp, projectId, assignment.UserId);
        
        MessageManager.DeleteAllMessagesByProject(cp, projectId);

        ProjectTable.DeleteProject(cp, projectId);
    }

    /// <summary>
    /// Deletes the given folder recursively.
    /// </summary>
    public static void DeleteFolder(int folderId)
        => Database.Transaction(cp => DeleteFolderRecursively(cp, folderId));

    private static void DeleteFolderRecursively(CommandProvider cp, int folderId)
    {
        foreach (var folder in FolderTable.ListByFolder(cp, folderId))
            DeleteFolderRecursively(cp, folder.Id);

        foreach (var file in FileTable.ListByFolder(cp, folderId))
            DeleteFile(file.Id);

        FolderTable.DeleteFolder(cp, folderId);
    }

    /// <summary>
    /// Deletes the given file and all associated versions.
    /// </summary>
    public static void DeleteFile(int fileId)
    {
        Database.Transaction(cp =>
        {
            FileTable.DeleteFile(cp, fileId);
            
            foreach (VersionEntry version in VersionTable.ListVersions(cp, fileId))
                VersionTable.DeleteVersion(cp, version.VersionId);

            foreach (SelectionEntry selection in SelectionTable.ListSelectionsByFile(cp, fileId)) 
                SelectionTable.DeleteSelection(cp, selection.SelectionId);
        });
    }

    /// <summary>
    /// Deletes the user's assignments and projects where only the given user is the owner and returns the list of deleted project IDs.<br/>
    /// If justList is set to true, the projects and assignments won't actually be deleted.
    /// </summary>
    public static List<int> DeleteForDeletedUser(int userId, bool justList = false)
        => Database.Transaction(cp =>
        {
            List<int> deleted = [];
            foreach (var assignment in AssignmentTable.ListByUser(cp, userId))
            {
                if (!justList)
                    AssignmentTable.DeleteAssignment(cp, assignment.ProjectId, assignment.UserId);
                if (AssignmentTable.ListByProject(cp, assignment.ProjectId)
                    .All(a => a.UserId == userId || a.ProjectRole < ProjectRole.Owner))
                {
                    deleted.Add(assignment.ProjectId);
                    if (!justList)
                    {
                        DeleteProject(cp, assignment.ProjectId);
                    }
                }
            }
            return deleted;
        });
}