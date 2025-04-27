using cocoding.Tests;

namespace cocoding.Data;

public static partial class FileManager {
    /// <summary>
    /// Tests the FileManager class.
    /// </summary>
    internal static void Test() {
    
        var project = Database.Transaction(cp => ProjectTable.CreateProject(cp, "project1", null, 4)); 
        var folder = Database.Transaction(cp => FolderTable.CreateFolder(cp, project.Id, null, "folder")); 
        var file = Database.Transaction(cp => FileTable.CreateFile(cp, project.Id, null, "file", DateTime.UtcNow, 0));
        
        #region CreateFile

        var fileEntryParent = CreateFile(project.Id, folder.Id, "fileParent", 0); 
        var fileEntryNoParent = CreateFile(project.Id, null, "fileNoParent", 0); 

        var dbFileEntryParent = Database.Transaction(cp => FileTable.GetById(cp, fileEntryParent.Id));
        var dbFileEntryNoParent = Database.Transaction(cp => FileTable.GetById(cp, fileEntryNoParent.Id));
        
        Testing.ShouldBeTrue(()=>
            dbFileEntryParent != null,
            "CreateFile did not create the file with parent (FileManager).");
        
        Testing.ShouldBeTrue(()=>
            dbFileEntryNoParent != null,
            "CreateFile did not create the file without parent (FileManager).");
        
        Testing.ShouldBeTrue(()=>
            dbFileEntryParent?.ParentId == folder.Id,
            "CreateFile did not assign parent folder to created file (FileManager).");
        
        Testing.ShouldBeTrue(()=>
            dbFileEntryNoParent?.ParentId == null,
            "Parent of file without parent created by CreateFile is not null (FileManager).");
        
        Testing.ShouldBeTrue(()=>
            dbFileEntryParent?.Name == "fileParent",
            "CreateFile created the file with parent with wrong name (FileManager).");
        
        Testing.ShouldBeTrue(()=>
            dbFileEntryNoParent?.Name == "fileNoParent",
            "CreateFile created the file without parent with wrong name (FileManager).");
        
        Testing.ShouldFail(()=>
            CreateFile(project.Id, null, "", 0),
            "CreateFile accepted empty name (FileManager).");
        
        Testing.ShouldFail(()=>
            CreateFile(project.Id, null, Security.RandomString(51), 0),
            "CreateFile accepted name longer than 50 characters (FileManager).");
        
        Testing.ShouldFail(()=>
            CreateFile(project.Id, null, file.Name, 0),
            "CreateFile accepted already existing file name (FileManager).");
        
        Testing.ShouldFail(()=>
            CreateFile(project.Id, null, folder.Name, 0),
            "CreateFile accepted already existing folder name (FileManager).");
        
        #endregion 
        
        #region GetFileType

        var fileJs = CreateFile(project.Id, null, "Test.js", 0);
        var filePy = CreateFile( project.Id, null, "Test.py", 0);
        var fileCss = CreateFile( project.Id, null, "Test.css", 0);
        var fileHtml = CreateFile( project.Id, null, "Test.html", 0);
        var fileText = CreateFile( project.Id, null, "Test", 0);
        
        Testing.ShouldBeTrue(() => 
            GetFileType(fileJs.Id) == FileType.Javascript,
            "GetFileType returned wrong file type for JS file.");
        
        Testing.ShouldBeTrue(() => 
            GetFileType(filePy.Id) == FileType.Python,
            "GetFileType returned wrong file type for Python file.");

        Testing.ShouldBeTrue(() => 
            GetFileType(fileCss.Id) == FileType.Css,
            "GetFileType returned wrong file type for CSS file.");

        Testing.ShouldBeTrue(() => 
            GetFileType(fileHtml.Id) == FileType.Html,
            "GetFileType returned wrong file type for HTML file.");

        Testing.ShouldBeTrue(() => 
            GetFileType(fileText.Id) == FileType.Text,
            "GetFileType returned wrong file type for file not corresponding to any supported file types.");
        #endregion
        #region CreateVersion
        Testing.ShouldFail(()=>
            CreateVersion(0,"name", DateTime.UtcNow, 0, Security.RandomString(26), null, []),
            "CreateVersion accepted label with more than 25 characters."
        );
        Testing.ShouldFail(()=>
            CreateVersion(0,"name", DateTime.UtcNow, 0, null, Security.RandomString(201), []),
            "CreateVersion accepted comment with more than 200 characters."
        );
        #endregion
        #region CreateFolder
        
        var folderEntryParent = CreateFolder(project.Id, folder.Id, "folderParent"); 
        var folderEntryNoParent = CreateFolder(project.Id, null, "folderNoParent"); 

        var dbFolderEntryParent = Database.Transaction(cp => FolderTable.GetById(cp, folderEntryParent.Id));
        var dbFolderEntryNoParent = Database.Transaction(cp => FolderTable.GetById(cp, folderEntryNoParent.Id));
        
        Testing.ShouldBeTrue(()=>
            dbFolderEntryParent != null,
            "CreateFolder did not create the folder with parent (FileManager).");
        
        Testing.ShouldBeTrue(()=>
            dbFolderEntryNoParent != null,
            "CreateFolder did not create the folder without parent (FileManager).");
        
        Testing.ShouldBeTrue(()=>
            dbFolderEntryParent?.ParentId == folder.Id,
            "CreateFolder did not assign parent folder to created folder (FileManager).");
        
        Testing.ShouldBeTrue(()=>
            dbFolderEntryNoParent?.ParentId == null,
            "Parent of folder without parent created by CreateFolder is not null (FileManager).");
        
        Testing.ShouldBeTrue(()=>
            dbFolderEntryParent?.Name == "folderParent",
            "CreateFolder created the folder with parent with wrong name (FileManager).");
        
        Testing.ShouldBeTrue(()=>
            dbFolderEntryNoParent?.Name == "folderNoParent",
            "CreateFolder created the folder without parent with wrong name (FileManager).");
        
        Testing.ShouldFail(()=>
            CreateFolder(project.Id, null, ""),
            "CreateFolder accepted empty name (FileManager).");
        
        Testing.ShouldFail(()=>
            CreateFolder(project.Id, null, Security.RandomString(51)),
            "CreateFolder accepted name longer than 50 characters (FileManager).");
        
        Testing.ShouldFail(()=>
            CreateFolder(project.Id, null, file.Name),
            "CreateFolder accepted already existing file name (FileManager).");
        
        Testing.ShouldFail(()=>
            CreateFolder(project.Id, null, folder.Name),
            "CreateFolder accepted already existing folder name (FileManager).");
        
        #endregion 
        
        #region RenameFile 
        
        var fileEntry = Database.Transaction(cp => FileTable.CreateFile(cp, project.Id, null, "newFile", DateTime.UtcNow, 0));
        
        int wrongFileId = 0;
        
        while (file.Id==wrongFileId || fileEntry.Id==wrongFileId || fileEntryParent.Id==wrongFileId || fileEntryNoParent.Id==wrongFileId)
            wrongFileId++;
        
        var wrongFileEntry = new FileEntry(wrongFileId, project.Id, null, "wrongFile", DateTime.UtcNow, 0);

        Testing.ShouldBeTrue(()=>
            RenameFile(fileEntry, "renamedFile"),
            "RenameFile returned false -> database unchanged (FileManager).");
        
        Testing.ShouldBeTrue(()=>
            !RenameFile(wrongFileEntry, "wrongName"),
            "RenameFile returned true for file with non existent id (FileManager).");
        
        var renamedFileEntry = Database.Transaction(cp => FileTable.GetById(cp, fileEntry.Id));
        
        Testing.ShouldBeTrue(()=>
            renamedFileEntry?.Name == "renamedFile",
            "RenameFile failed to rename file (FileManager).");
        
        Testing.ShouldFail(()=>
            RenameFile(fileEntry, ""),
            "RenameFile accepted empty name (FileManager).");
        
        Testing.ShouldFail(()=>
            RenameFile(fileEntry, Security.RandomString(51)),
            "RenameFile accepted name longer than 50 characters (FileManager).");
        
        Testing.ShouldFail(()=>
            RenameFile(fileEntry, file.Name),
            "RenameFile accepted already existing file name (FileManager).");
        
        Testing.ShouldFail(()=>
            RenameFile(fileEntry, folder.Name),
            "RenameFile accepted already existing folder name (FileManager).");
        
        #endregion
        
        #region RenameFolder 
        
        
        var folderEntry = Database.Transaction(cp => FolderTable.CreateFolder(cp, project.Id, null, "newFolder"));
        
        int wrongFolderId = 0;
        
        while (folder.Id==wrongFolderId || folderEntry.Id==wrongFolderId || folderEntryParent.Id==wrongFolderId || folderEntryNoParent.Id==wrongFolderId)
            wrongFolderId++;
        
        var wrongFolderEntry = new FolderEntry(wrongFolderId, project.Id, null, "wrongFolder");

        Testing.ShouldBeTrue(()=>
            RenameFolder(folderEntry, "renamedFolder"),
            "RenameFolder returned false -> database unchanged (FileManager).");
        
        Testing.ShouldBeTrue(()=>
            !RenameFolder(wrongFolderEntry, "wrongName"),
            "RenameFolder returned true for folder with non existent id (FileManager).");
        
        var renamedFolderEntry = Database.Transaction(cp => FolderTable.GetById(cp, folderEntry.Id));
        
        Testing.ShouldBeTrue(()=>
            renamedFolderEntry?.Name == "renamedFolder",
            "RenameFolder failed to rename folder (FileManager).");
        
        Testing.ShouldFail(()=>
            RenameFolder(folderEntry, ""),
            "RenameF accepted empty name (FileManager).");
        
        Testing.ShouldFail(()=>
            RenameFolder(folderEntry, Security.RandomString(51)),
            "RenameFolder accepted name longer than 50 characters (FileManager).");
        
        Testing.ShouldFail(()=>
            RenameFolder(folderEntry, file.Name),
            "RenameFolder accepted already existing file name (FileManager).");
        
        Testing.ShouldFail(()=>
            RenameFolder(folderEntry, folder.Name),
            "RenameFolder accepted already existing folder name (FileManager).");
        
        
        #endregion
        
        #region Events

        ProjectEntry? projectMetadataChanged = null;
        FolderEntry? folderMetadataChanged = null;
        FileEntry? fileMetadataChanged = null;
        FolderEntry? folderCreated = null;
        FolderEntry? folderDeleted = null;
        FileEntry? fileCreated = null;
        FileEntry? fileDeleted = null;
        Events.ProjectMetadataChanged += x => { projectMetadataChanged = x; return true; };
        Events.FolderMetadataChanged += x => { folderMetadataChanged = x; return true; };
        Events.FileMetadataChanged += x => { fileMetadataChanged = x; return true; };
        Events.FolderCreated += x => { folderCreated = x; return true; };
        Events.FolderDeleted += x => { folderDeleted = x; return true; };
        Events.FileCreated += x => { fileCreated = x; return true; };
        Events.FileDeleted += x => { fileDeleted = x; return true; };

        void CheckEvents(int index)
        {
            Testing.ShouldBeTrue(() =>
                   (projectMetadataChanged != null) == (index == 0)
                && (folderMetadataChanged != null) == (index == 1)
                && (fileMetadataChanged != null) == (index == 2)
                && (folderCreated != null) == (index == 3)
                && (folderDeleted != null) == (index == 4)
                && (fileCreated != null) == (index == 5)
                && (fileDeleted != null) == (index == 6),
                $"Event called incorrectly {index}"
            );
            projectMetadataChanged = null;
            folderMetadataChanged = null;
            fileMetadataChanged = null;
            folderCreated = null;
            folderDeleted = null;
            fileCreated = null;
            fileDeleted = null;
        }
        
        Events.ReportProjectMetadataChanged(new(0, "Project", null, 4));
        CheckEvents(0);
        Events.ReportFolderMetadataChanged(new(0, 0, null, "Folder"));
        CheckEvents(1);
        Events.ReportFileMetadataChanged(new(0, 0, null, "File", DateTime.UtcNow, 0));
        CheckEvents(2);
        Events.ReportFolderCreated(new(0, 0, null, "Folder"));
        CheckEvents(3);
        Events.ReportFolderDeleted(new(0, 0, null, "Folder"));
        CheckEvents(4);
        Events.ReportFileCreated(new(0, 0, null, "File", DateTime.UtcNow, 0));
        CheckEvents(5);
        Events.ReportFileDeleted(new(0, 0, null, "File", DateTime.UtcNow, 0));
        CheckEvents(6);

        #endregion
    }
}