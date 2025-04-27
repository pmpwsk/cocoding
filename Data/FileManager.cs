namespace cocoding.Data;

/// <summary>
/// Methods for file management.
/// </summary>
public static partial class FileManager
{
    /// <summary>
    /// General file system events.
    /// </summary>
    public static class Events
    {
        /// <summary>
        /// This event is called whenever a project's metadata was changed.<br/>
        /// Listeners receive the modified object as a parameter and should return whether anything they control was affected by the change.
        /// </summary>
        public static event Func<ProjectEntry, bool>? ProjectMetadataChanged;
        /// <summary>
        /// Calls the event indicating that the given project's metadata was changed.
        /// </summary>
        public static void ReportProjectMetadataChanged(ProjectEntry entry)
            => ProjectMetadataChanged?.Invoke(entry);
        
        /// <summary>
        /// This event is called whenever a folder's metadata was changed.<br/>
        /// Listeners receive the modified object as a parameter and should return whether anything they control was affected by the change.
        /// </summary>
        public static event Func<FolderEntry, bool>? FolderMetadataChanged;
        /// <summary>
        /// Calls the event indicating that the given folder's metadata was changed.
        /// </summary>
        public static void ReportFolderMetadataChanged(FolderEntry entry)
            => FolderMetadataChanged?.Invoke(entry);
        
        /// <summary>
        /// This event is called whenever a file's metadata was changed.<br/>
        /// Listeners receive the modified object as a parameter and should return whether anything they control was affected by the change.
        /// </summary>
        public static event Func<FileEntry, bool>? FileMetadataChanged;
        /// <summary>
        /// Calls the event indicating that the given file's metadata was changed.
        /// </summary>
        public static void ReportFileMetadataChanged(FileEntry entry)
            => FileMetadataChanged?.Invoke(entry);

        /// <summary>
        /// This event is called whenever a folder was created.<br/>
        /// Listeners receive the modified object as a parameter and should return whether anything they control was affected by the change.
        /// </summary>
        public static event Func<FolderEntry, bool>? FolderCreated;
        /// <summary>
        /// Calls the event indicating that the given folder was created.
        /// </summary>
        public static void ReportFolderCreated(FolderEntry entry)
            => FolderCreated?.Invoke(entry);
        
        /// <summary>
        /// This event is called whenever a folder was deleted.<br/>
        /// Listeners receive the modified object as a parameter and should return whether anything they control was affected by the change.
        /// </summary>
        public static event Func<FolderEntry, bool>? FolderDeleted;
        /// <summary>
        /// Calls the event indicating that the given folder was deleted.
        /// </summary>
        public static void ReportFolderDeleted(FolderEntry entry)
            => FolderDeleted?.Invoke(entry);
        
        /// <summary>
        /// This event is called whenever a file was created.<br/>
        /// Listeners receive the modified object as a parameter and should return whether anything they control was affected by the change.
        /// </summary>
        public static event Func<FileEntry, bool>? FileCreated;
        /// <summary>
        /// Calls the event indicating that the given file was created.
        /// </summary>
        public static void ReportFileCreated(FileEntry entry)
            => FileCreated?.Invoke(entry);
        
        /// <summary>
        /// This event is called whenever a file was deleted.<br/>
        /// Listeners receive the modified object as a parameter and should return whether anything they control was affected by the change.
        /// </summary>
        public static event Func<FileEntry, bool>? FileDeleted;
        /// <summary>
        /// Calls the event indicating that the given file was deleted.
        /// </summary>
        public static void ReportFileDeleted(FileEntry entry)
            => FileDeleted?.Invoke(entry);
    }
    
    /// <summary>
    /// Returns the FileType of the corresponding file.
    /// </summary>
    public static FileType GetFileType(int fileId)
    {
        //TODO complement missing file types???
        FileEntry? entry = Database.Transaction(cp => FileTable.GetById(cp, fileId));
        
        if (entry == null)
            throw new FileNotFoundException($"File with id {fileId} not found");
        
        string fileName = entry.Name;

        return GetFileType(fileName);
    }

    public static FileType GetFileType(FileEntry file)
    {
        return GetFileType(file.Name);  
    }

    public static FileType GetFileType(string fileName)
    {
        if (fileName.EndsWith(".js"))
            return FileType.Javascript;
        if (fileName.EndsWith(".py")) 
            return FileType.Python;
        if (fileName.EndsWith(".css"))
            return FileType.Css;
        if (fileName.EndsWith(".html"))
            return FileType.Html;
        return FileType.Text;
    }
    /// <summary>
    /// Creates a new file while checking the parameters.
    /// </summary>
    public static FileEntry CreateFile(int projectId, int? parentId, string name, int userId)
    {
        name = name.Trim();
        if (name == "")
            throw new Exception("Geben Sie einen Namen ein!");
        if (name.Length > 50)
            throw new Exception("Dateinamen dürfen höchstens 50 Zeichen lang sein!");
        if (Database.Transaction(cp =>
                parentId == null
                    ? FileTable.ListByProjectRoot(cp, projectId)
                    : FileTable.ListByFolder(cp, parentId.Value)
            ).Any(f => f.Name == name))
            throw new Exception("Es gibt bereits eine Datei mit diesem Namen!");
        if (Database.Transaction(cp =>
                parentId == null
                    ? FolderTable.ListByProjectRoot(cp, projectId)
                    : FolderTable.ListByFolder(cp, parentId.Value)
            ).Any(f => f.Name == name))
            throw new Exception("Es gibt bereits einen Ordner mit diesem Namen!");
        
        return Database.Transaction(cp => FileTable.CreateFile(cp, projectId, parentId, name, DateTime.UtcNow, userId));
    }

    /// <summary>
    /// Creates a new version for a file while checking the parameters.
    /// </summary>
    public static VersionEntry CreateVersion(int fileId, string name, DateTime changed, int userId, string? label, string? comment, List<byte[]> state)
    {
        if (label?.Length > 25)
            throw new Exception("Das Label ist zu lang.");
        if (comment?.Length > 200)
            throw new Exception("Der Kommentar ist zu lang.");
        return Database.Transaction(cp => VersionTable.CreateVersion(cp, fileId, name, changed, userId, label, comment, state));
    }
    
    /// <summary>
    /// Creates a new folder while checking the parameters.
    /// </summary>
    public static FolderEntry CreateFolder(int projectId, int? parentId, string name)
    {
        name = name.Trim();
        if (name == "")
            throw new Exception("Geben Sie einen Namen ein!");
        if (name.Length > 50)
            throw new Exception("Ordnernamen dürfen höchstens 50 Zeichen lang sein!");
        if (Database.Transaction(cp =>
                parentId == null
                    ? FileTable.ListByProjectRoot(cp, projectId)
                    : FileTable.ListByFolder(cp, parentId.Value)
            ).Any(f => f.Name == name))
            throw new Exception("Es gibt bereits eine Datei mit diesem Namen!");
        if (Database.Transaction(cp =>
                parentId == null
                    ? FolderTable.ListByProjectRoot(cp, projectId)
                    : FolderTable.ListByFolder(cp, parentId.Value)
            ).Any(f => f.Name == name))
            throw new Exception("Es gibt bereits einen Ordner mit diesem Namen!");
        
        return Database.Transaction(cp => FolderTable.CreateFolder(cp, projectId, parentId, name));
    }

    /// <summary>
    /// Changes the name of the given file while checking the parameters.
    /// </summary>
    public static bool RenameFile(FileEntry file, string name)
    {
        name = name.Trim();
        if (name == "")
            throw new Exception("Geben Sie einen Namen ein!");
        if (name.Length > 50)
            throw new Exception("Dateinamen dürfen höchstens 50 Zeichen lang sein!");
        if (Database.Transaction(cp =>
                file.ParentId == null
                    ? FileTable.ListByProjectRoot(cp, file.ProjectId)
                    : FileTable.ListByFolder(cp, file.ParentId.Value)
            ).Any(f => f.Name == name && f.Id != file.Id))
            throw new Exception("Es gibt bereits eine Datei mit diesem Namen!");
        if (Database.Transaction(cp =>
                file.ParentId == null
                    ? FolderTable.ListByProjectRoot(cp, file.ProjectId)
                    : FolderTable.ListByFolder(cp, file.ParentId.Value)
            ).Any(f => f.Name == name))
            throw new Exception("Es gibt bereits einen Ordner mit diesem Namen!");
        
        return Database.Transaction(cp => FileTable.RenameFile(cp, file.Id, name));
    }

    /// <summary>
    /// Changes the name of the given folder while checking the parameters.
    /// </summary>
    public static bool RenameFolder(FolderEntry folder, string name)
    {
        name = name.Trim();
        if (name == "")
            throw new Exception("Geben Sie einen Namen ein!");
        if (name.Length > 50)
            throw new Exception("Ordnernamen dürfen höchstens 50 Zeichen lang sein!");
        if (Database.Transaction(cp =>
                folder.ParentId == null
                    ? FileTable.ListByProjectRoot(cp, folder.ProjectId)
                    : FileTable.ListByFolder(cp, folder.ParentId.Value)
            ).Any(f => f.Name == name))
            throw new Exception("Es gibt bereits eine Datei mit diesem Namen!");
        if (Database.Transaction(cp =>
                folder.ParentId == null
                    ? FolderTable.ListByProjectRoot(cp, folder.ProjectId)
                    : FolderTable.ListByFolder(cp, folder.ParentId.Value)
            ).Any(f => f.Name == name && f.Id != folder.Id))
            throw new Exception("Es gibt bereits einen Ordner mit diesem Namen!");
        
        return Database.Transaction(cp => FolderTable.RenameFolder(cp, folder.Id, name));
    }

    /// <summary>
    /// Returns a list of all users currently using any of the given files.
    /// </summary>
    private static List<UserEntry> ListActiveUsers(CommandProvider cp, List<FileEntry> files)
    {
        Dictionary<int, UserEntry?> users = [];
        foreach (var file in files)
            if (EditorHub.FileGroups.TryGetValue(file.Id, out var fileGroup))
                foreach (var session in fileGroup.Sessions.Values)
                    if (!users.ContainsKey(session.UserId))
                        users[session.UserId] = UserTable.GetById(cp, session.UserId);
        return users.Values.OfType<UserEntry>().Order().ToList();
    }

    /// <summary>
    /// Returns a list of all users currently using the given file.
    /// </summary>
    public static List<UserEntry> ListActiveUsers(FileEntry file)
        => Database.Transaction(cp => ListActiveUsers(cp, [file]));

    /// <summary>
    /// Returns a list of all users currently active in the given folder.
    /// </summary>
    public static List<UserEntry> ListActiveUsers(FolderEntry folder)
        => Database.Transaction(cp => ListActiveUsers(cp, FileTable.ListByFolderRecursively(cp, folder.Id)));

    /// <summary>
    /// Returns a list of all users currently active in the given project.
    /// </summary>
    public static List<UserEntry> ListActiveUsers(ProjectEntry project)
        => Database.Transaction(cp => ListActiveUsers(cp, FileTable.ListByProject(cp, project.Id)));
}