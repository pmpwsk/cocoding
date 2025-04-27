using cocoding.Tests;

namespace cocoding.Data;

public static partial class FileTable {
    /// <summary>
    /// Tests the FileTable class.
    /// </summary>
    internal static void Test()
    {

        var project1 = Database.Transaction(cp => ProjectTable.CreateProject(cp, "project1", null, 4));
        var project2 = Database.Transaction(cp => ProjectTable.CreateProject(cp, "project2", null, 4));
        var folder1 = Database.Transaction(cp => FolderTable.CreateFolder(cp,project1.Id,null, "folder1"));
        var folder2 = Database.Transaction(cp => FolderTable.CreateFolder(cp,project1.Id, folder1.Id,"folder1"));
        var folder3 = Database.Transaction(cp => FolderTable.CreateFolder(cp, project1.Id, folder2.Id, "folder3"));

        bool file1InFolderExists = false;
        bool file2InFolderExists = false;
        bool fileRoot1Exists = false;
        bool fileRoot2Exists = false;
        bool fileFolder1Exists = false;
        bool fileFolder2Exists = false;
        bool fileFolder3_1Exists = false;
        bool fileFolder3_2Exists = false;

        #region CreateFile

        var dtFileRoot1 = DateTime.UtcNow;
        var fileRoot1 = Database.Transaction(cp => CreateFile(cp, project1.Id, null, "fileRoot1", dtFileRoot1, 123));
        
        string stateFilePathRoot1 = $"../FileStates/{fileRoot1.Id}.bin";
        
        Testing.ShouldBeTrue(()=>
            File.Exists(stateFilePathRoot1),
            "No file for file state was created.");
        
        var dtFileRoot2 = DateTime.UtcNow;
        var fileRoot2 = Database.Transaction(cp => CreateFile(cp, project1.Id, null, "fileRoot2", dtFileRoot2, 1232));
        
        string stateFilePathRoot2 = $"../FileStates/{fileRoot2.Id}.bin";
        
        Testing.ShouldBeTrue(()=>
            File.Exists(stateFilePathRoot2),
            "No file for file state was created.");
        
        var dtFileFolder1 = DateTime.UtcNow;
        var fileFolder1 = Database.Transaction(cp => CreateFile(cp, project1.Id, folder1.Id, "fileFolder1", dtFileFolder1, 456));
        
        string stateFilePathFolder1 = $"../FileStates/{fileFolder1.Id}.bin";
        
        Testing.ShouldBeTrue(()=>
            File.Exists(stateFilePathFolder1),
            "No file for file state was created.");
        
        var dtFileFolder2 = DateTime.UtcNow;
        var fileFolder2 = Database.Transaction(cp => CreateFile(cp, project1.Id, folder1.Id, "fileFolder2", dtFileFolder2, 4562));
        
        string stateFilePathFolder2 = $"../FileStates/{fileFolder2.Id}.bin";
        
        Testing.ShouldBeTrue(()=>
            File.Exists(stateFilePathFolder2),
            "No file for file state was created.");

        var dtFileFolder3_1 = DateTime.UtcNow;
        var fileFolder3_1 = Database.Transaction(cp => CreateFile(cp, project1.Id, folder3.Id, "fileFolder3_1", dtFileFolder3_1, 111));

        string stateFilePathFolder3_1 = $"../FileStates/{fileFolder3_1.Id}.bin";

        Testing.ShouldBeTrue(() =>
            File.Exists(stateFilePathFolder3_1),
            "No file for file state was created.");

        var dtFileFolder3_2 = DateTime.UtcNow;
        var fileFolder3_2 = Database.Transaction(cp => CreateFile(cp, project1.Id, folder3.Id, "fileFolder3_2", dtFileFolder3_2, 222));

        string stateFilePathFolder3_2 = $"../FileStates/{fileFolder3_2.Id}.bin";

        Testing.ShouldBeTrue(() =>
            File.Exists(stateFilePathFolder3_2),
            "No file for file state was created.");

        #endregion

        int wrongFileId = 0;
        
        while (fileRoot1.Id == wrongFileId || fileRoot2.Id == wrongFileId || fileFolder1.Id == wrongFileId || fileFolder2.Id == wrongFileId) 
            wrongFileId++;

        #region GetById

        var fileByIdRoot = Database.Transaction(cp => GetById(cp, fileRoot1.Id));
        
        Testing.ShouldBeTrue(()=>
            fileByIdRoot?.Id == fileRoot1.Id,
            "GetById returned false FileEntry.");
        
        Testing.ShouldBeTrue(()=>
            fileByIdRoot?.ProjectId == project1.Id,
            "GetByIds returned FileEntry's project id is wrong for root file. Data corrupted.");
        
        Testing.ShouldBeTrue(()=>
            fileByIdRoot?.ParentId == null,
            "GetByIds returned FileEntry's parent id is wrong for root file. Data corrupted.");
        
        Testing.ShouldBeTrue(()=>
            fileByIdRoot?.Name == "fileRoot1",
            "GetByIds returned FileEntry's name is wrong for root file. Data corrupted.");
        
        Testing.ShouldBeTrue(()=>
            fileByIdRoot?.Changed == dtFileRoot1,
            "GetByIds returned FileEntry's changed date is wrong for root file. Data corrupted.");
        
        Testing.ShouldBeTrue(()=>
            fileByIdRoot?.UserId == 123,
            "GetByIds returned FileEntry's user ID is wrong for root file. Data corrupted.");
        
        
        var fileByIdFolder = Database.Transaction(cp => GetById(cp, fileFolder1.Id));
        
        Testing.ShouldBeTrue(()=>
            fileByIdFolder?.Id == fileFolder1.Id,
            "GetById returned false FileEntry.");
        
        Testing.ShouldBeTrue(()=>
            fileByIdFolder?.ProjectId == project1.Id,
            "GetByIds returned FileEntry's project id is wrong for folder file. Data corrupted.");
        
        Testing.ShouldBeTrue(()=>
            fileByIdFolder?.ParentId == folder1.Id,
            "GetByIds returned FileEntry's parent id is wrong for folder file. Data corrupted.");
        
        Testing.ShouldBeTrue(()=>
            fileByIdFolder?.Name == "fileFolder1",
            "GetByIds returned FolderEntry's name is wrong for folder file. Data corrupted.");

        Testing.ShouldBeTrue(()=>
            fileByIdFolder?.Changed == dtFileFolder1,
            "GetByIds returned FileEntry's changed date is wrong for folder file. Data corrupted.");
        
        Testing.ShouldBeTrue(()=>
            fileByIdFolder?.UserId == 456,
            "GetByIds returned FileEntry's user ID is wrong for folder file. Data corrupted.");
        
        var wrongIdFile = Database.Transaction(cp => GetById(cp, wrongFileId));

        Testing.ShouldBeTrue(()=>
            wrongIdFile==null,
            "GetById returned FileEntry for non existing id.");

 
        
        #endregion

        #region ListByFolder

        var listFileFolder1 = Database.Transaction(cp => ListByFolder(cp, folder1.Id));
        var listFileFolder2 = Database.Transaction(cp => ListByFolder(cp, folder2.Id));

        Testing.ShouldBeTrue(() =>
            listFileFolder1.Count == 2,
            listFileFolder1.Count switch
            {
                0 => "ListByFolder returned no file entries for folder 1, expected 2.",
                1 => "ListByFolder returned 1 file entry for folder 1, expected 2.",
                _ => "ListByFolder returned more than 2 file entries for folder 1."
            });

        foreach (var fileEntry in listFileFolder1)
        {
            if (fileEntry.Id == fileFolder1.Id)
            {
                file1InFolderExists = true;
                
                Testing.ShouldBeTrue(()=>
                    fileEntry.ProjectId == project1.Id,
                    "FileEntry in list returned by ListByFolder contains wrong project id. Data corrupted.");
                
                Testing.ShouldBeTrue(()=>
                    fileEntry.ParentId == folder1.Id,
                    "FileEntry in list returned by ListByFolder contains wrong parent id. Data corrupted.");
                
                Testing.ShouldBeTrue(()=>
                    fileEntry.Name == "fileFolder1",
                    "FileEntry in list returned by ListByFolder has wrong name. Data corrupted.");
                
                Testing.ShouldBeTrue(()=>
                    fileEntry.Changed == dtFileFolder1,
                    "FileEntry in list returned by ListByFolder contains wrong changed date. Data corrupted.");
        
                Testing.ShouldBeTrue(()=>
                    fileEntry.UserId == 456,
                    "FileEntry in list returned by ListByFolder contains wrong user ID. Data corrupted.");
                
            }

            if (fileEntry.Id == fileFolder2.Id)
            {
                file2InFolderExists = true;
                
                Testing.ShouldBeTrue(()=>
                    fileEntry.ProjectId == project1.Id,
                    "FileEntry in list returned by ListByFolder contains wrong project id. Data corrupted.");
                
                Testing.ShouldBeTrue(()=>
                    fileEntry.ParentId == folder1.Id,
                    "FileEntry in list returned by ListByFolder contains wrong parent id. Data corrupted.");
                
                Testing.ShouldBeTrue(()=>
                    fileEntry.Name == "fileFolder2",
                    "FolderEntry in list returned by ListByFolder has wrong name. Data corrupted.");
                
                Testing.ShouldBeTrue(()=>
                    fileEntry.Changed == dtFileFolder2,
                    "FileEntry in list returned by ListByFolder contains wrong changed date. Data corrupted.");
        
                Testing.ShouldBeTrue(()=>
                    fileEntry.UserId == 4562,
                    "FileEntry in list returned by ListByFolder contains wrong user ID. Data corrupted.");
            }

        }
        
        Testing.ShouldBeTrue(()=>
            file1InFolderExists && file2InFolderExists,
            "List returned by ListByFolder is missing file entries.");
        
        Testing.ShouldBeTrue(()=>
            listFileFolder2.Count == 0,
            "List returned by ListByFolder has file entries when expected non.");

        #endregion

        #region ListByProjectRoot

        var byProjectList1 = Database.Transaction(cp => ListByProjectRoot(cp, project1.Id));
        var byProjectList2 = Database.Transaction(cp => ListByProjectRoot(cp, project2.Id));
         
        Testing.ShouldBeTrue(() =>
            byProjectList1.Count == 2,
            byProjectList1.Count switch
            {
                0 => "ListByProjectRoot returned no file entries for root folder in project1, expected 2.",
                1 => "ListByProjectRoot returned 1 file entry for root folder in project1, expected 2.",
                _ => "ListByProjectRoot returned more than 2 file entries for root folder in project1."
            });

        bool rootFile1Exists = false;
        bool rootFile2Exists = false;

        foreach (var fileEntry in byProjectList1)
        {
            if (fileEntry.Id == fileRoot1.Id)
            {
                rootFile1Exists = true;
                
                Testing.ShouldBeTrue(()=>
                    fileEntry.ProjectId == project1.Id,
                    "FileEntry in list returned by ListByProjectRoot contains wrong project id. Data corrupted.");
                
                Testing.ShouldBeTrue(()=>
                    fileEntry.ParentId == null,
                    "FileEntry in list returned by ListByProjectRoot contains wrong parent id. Data corrupted.");
                
                Testing.ShouldBeTrue(()=>
                    fileEntry.Name == "fileRoot1",
                    "FileEntry in list returned by ListByProjectRoot has wrong name. Data corrupted.");
                
                Testing.ShouldBeTrue(()=>
                    fileEntry.Changed == dtFileRoot1,
                    "FileEntry in list returned by ListByProjectRoot contains wrong changed date. Data corrupted.");
        
                Testing.ShouldBeTrue(()=>
                    fileEntry.UserId == 123,
                    "FileEntry in list returned by ListByProjectRoot contains wrong user ID. Data corrupted.");
            }

            if (fileEntry.Id == fileRoot2.Id)
            {
                rootFile2Exists = true;
                
                Testing.ShouldBeTrue(()=>
                    fileEntry.ProjectId == project1.Id,
                    "FileEntry in list returned by ListByProjectRoot contains wrong project id. Data corrupted.");
                
                Testing.ShouldBeTrue(()=>
                    fileEntry.ParentId == null,
                    "FileEntry in list returned by ListByProjectRoot contains wrong parent id. Data corrupted.");
                
                Testing.ShouldBeTrue(()=>
                    fileEntry.Name == "fileRoot2",
                    "FileEntry in list returned by ListByProjectRoot has wrong name. Data corrupted.");
                
                Testing.ShouldBeTrue(()=>
                    fileEntry.Changed == dtFileRoot2,
                    "FileEntry in list returned by ListByFolder contains wrong changed date. Data corrupted.");
        
                Testing.ShouldBeTrue(()=>
                    fileEntry.UserId == 1232,
                    "FileEntry in list returned by ListByProjectRoot contains wrong user ID. Data corrupted.");
            }

        }
        
        Testing.ShouldBeTrue(()=>
            rootFile1Exists && rootFile2Exists,
            "List returned by ListByProjectRoot is missing file entries.");
        
        Testing.ShouldBeTrue(()=>
            byProjectList2.Count == 0,
            "List returned by ListByProjectRoot has file entries when expected non.");

        #endregion

        #region GetFilesInProject

        var listFilesProject1 = Database.Transaction(cp => ListByProject(cp, project1.Id));
        var listFilesProject2 = Database.Transaction(cp => ListByProject(cp, project2.Id));

        Testing.ShouldBeTrue(() =>
            listFilesProject1.Count == 6,
            listFilesProject1.Count switch
            {
                0 => "GetFilesInProject returned no file entries for project1, expected 6.",
                1 => "GetFilesInProject returned 1 file entry for project1, expected 6.",
                2 => "GetFilesInProject returned 2 file entries for project1, expected 6.",
                3 => "GetFilesInProject returned 3 file entries for project1, expected 6.",
                4 => "GetFilesInProject returned 4 file entries for project1, expected 6.",
                5 => "GetFilesInProject returned 5 file entries for project1, expected 6.",
                _ => "GetFilesInProject returned more than 6 file entries for project1."
            });

        foreach (var fileEntry in listFilesProject1)
        {
            if (fileEntry.Id == fileRoot1.Id)
            {
                fileRoot1Exists = true;

                Testing.ShouldBeTrue(() =>
                    fileEntry.ProjectId == project1.Id,
                    "FileEntry in list returned by GetFilesInProject contains wrong project id. Data corrupted.");

                Testing.ShouldBeTrue(() =>
                    fileEntry.ParentId == null,
                    "FileEntry in list returned by GetFilesInProject contains wrong parent id. Data corrupted.");

                Testing.ShouldBeTrue(() =>
                    fileEntry.Name == "fileRoot1",
                    "FileEntry in list returned by GetFilesInProject has wrong name. Data corrupted.");

                Testing.ShouldBeTrue(() =>
                    fileEntry.Changed == dtFileRoot1,
                    "FileEntry in list returned by GetFilesInProject contains wrong changed date. Data corrupted.");
        
                Testing.ShouldBeTrue(()=>
                    fileEntry.UserId == 123,
                    "FileEntry in list returned by GetFilesInProject contains wrong user ID. Data corrupted.");

            }

            if (fileEntry.Id == fileRoot2.Id)
            {
                fileRoot2Exists = true;

                Testing.ShouldBeTrue(() =>
                    fileEntry.ProjectId == project1.Id,
                    "FileEntry in list returned by GetFilesInProject contains wrong project id. Data corrupted.");

                Testing.ShouldBeTrue(() =>
                    fileEntry.ParentId == null,
                    "FileEntry in list returned by GetFilesInProject contains wrong parent id. Data corrupted.");

                Testing.ShouldBeTrue(() =>
                    fileEntry.Name == "fileRoot2",
                    "FileEntry in list returned by GetFilesInProject has wrong name. Data corrupted.");

                Testing.ShouldBeTrue(() =>
                    fileEntry.Changed == dtFileRoot2,
                    "FileEntry in list returned by GetFilesInProject contains wrong changed date. Data corrupted.");
        
                Testing.ShouldBeTrue(()=>
                    fileEntry.UserId == 1232,
                    "FileEntry in list returned by GetFilesInProject contains wrong user ID. Data corrupted.");

            }

            if (fileEntry.Id == fileFolder1.Id)
            {
                fileFolder1Exists = true;

                Testing.ShouldBeTrue(() =>
                    fileEntry.ProjectId == project1.Id,
                    "FileEntry in list returned by GetFilesInProject contains wrong project id. Data corrupted.");

                Testing.ShouldBeTrue(() =>
                    fileEntry.ParentId == folder1.Id,
                    "FileEntry in list returned by GetFilesInProject contains wrong parent id. Data corrupted.");

                Testing.ShouldBeTrue(() =>
                    fileEntry.Name == "fileFolder1",
                    "FileEntry in list returned by GetFilesInProject has wrong name. Data corrupted.");

                Testing.ShouldBeTrue(() =>
                    fileEntry.Changed == dtFileFolder1,
                    "FileEntry in list returned by GetFilesInProject contains wrong changed date. Data corrupted.");
        
                Testing.ShouldBeTrue(()=>
                    fileEntry.UserId == 456,
                    "FileEntry in list returned by GetFilesInProject contains wrong user ID. Data corrupted.");

            }

            if (fileEntry.Id == fileFolder2.Id)
            {
                fileFolder2Exists = true;

                Testing.ShouldBeTrue(() =>
                    fileEntry.ProjectId == project1.Id,
                    "FileEntry in list returned by GetFilesInProject contains wrong project id. Data corrupted.");

                Testing.ShouldBeTrue(() =>
                    fileEntry.ParentId == folder1.Id,
                    "FileEntry in list returned by GetFilesInProject contains wrong parent id. Data corrupted.");

                Testing.ShouldBeTrue(() =>
                    fileEntry.Name == "fileFolder2",
                    "FolderEntry in list returned by GetFilesInProject has wrong name. Data corrupted.");

                Testing.ShouldBeTrue(() =>
                    fileEntry.Changed == dtFileFolder2,
                    "FileEntry in list returned by GetFilesInProject contains wrong changed date. Data corrupted.");
        
                Testing.ShouldBeTrue(()=>
                    fileEntry.UserId == 4562,
                    "FileEntry in list returned by GetFilesInProject contains wrong user ID. Data corrupted.");
            }

            if (fileEntry.Id == fileFolder3_1.Id)
            {
                fileFolder3_1Exists = true;

                Testing.ShouldBeTrue(() =>
                    fileEntry.ProjectId == project1.Id,
                    "FileEntry in list returned by GetFilesInProject contains wrong project id. Data corrupted.");

                Testing.ShouldBeTrue(() =>
                    fileEntry.ParentId == folder3.Id,
                    "FileEntry in list returned by GetFilesInProject contains wrong parent id. Data corrupted.");

                Testing.ShouldBeTrue(() =>
                    fileEntry.Name == "fileFolder3_1",
                    "FolderEntry in list returned by GetFilesInProject has wrong name. Data corrupted.");

                Testing.ShouldBeTrue(() =>
                    fileEntry.Changed == dtFileFolder3_1,
                    "FileEntry in list returned by GetFilesInProject contains wrong changed date. Data corrupted.");
        
                Testing.ShouldBeTrue(()=>
                    fileEntry.UserId == 111,
                    "FileEntry in list returned by GetFilesInProject contains wrong user ID. Data corrupted.");
            }

            if (fileEntry.Id == fileFolder3_2.Id)
            {
                fileFolder3_2Exists = true;

                Testing.ShouldBeTrue(() =>
                    fileEntry.ProjectId == project1.Id,
                    "FileEntry in list returned by GetFilesInProject contains wrong project id. Data corrupted.");

                Testing.ShouldBeTrue(() =>
                    fileEntry.ParentId == folder3.Id,
                    "FileEntry in list returned by GetFilesInProject contains wrong parent id. Data corrupted.");

                Testing.ShouldBeTrue(() =>
                    fileEntry.Name == "fileFolder3_2",
                    "FolderEntry in list returned by GetFilesInProject has wrong name. Data corrupted.");

                Testing.ShouldBeTrue(() =>
                    fileEntry.Changed == dtFileFolder3_2,
                    "FileEntry in list returned by GetFilesInProject contains wrong changed date. Data corrupted.");
        
                Testing.ShouldBeTrue(()=>
                    fileEntry.UserId == 222,
                    "FileEntry in list returned by GetFilesInProject contains wrong user ID. Data corrupted.");
            }

        }

        Testing.ShouldBeTrue(() =>
            fileRoot1Exists && fileRoot2Exists && fileFolder1Exists && fileFolder2Exists && fileFolder3_1Exists && fileFolder3_2Exists,
            "List returned by GetFilesInProject is missing file entries.");

        Testing.ShouldBeTrue(() =>
            listFilesProject2.Count == 0,
            "List returned by GetFilesInProject has file entries when expected non.");

        #endregion

        #region GetFilesInFolder

        var listFilesFolder1 = Database.Transaction(cp => ListByFolderRecursively(cp, folder1.Id));
        var listFilesFolder2 = Database.Transaction(cp => ListByFolderRecursively(cp, folder2.Id));

        Testing.ShouldBeTrue(() =>
            listFilesFolder1.Count == 4,
            listFilesFolder1.Count switch
            {
                0 => "GetFilesInFolder returned no file entries for folder1, expected 4.",
                1 => "GetFilesInFolder returned 1 file entry for folder 1, expected 4.",
                2 => "GetFilesInFolder returned 2 file entries for folder 1, expected 4.",
                3 => "GetFilesInFolder returned 3 file entries for folder 1, expected 4.",
                _ => "GetFilesInFolder returned more than 4 file entries for folder 1."
            });

        foreach (var fileEntry in listFilesFolder1)
        {
            if (fileEntry.Id == fileFolder1.Id)
            {
                fileFolder1Exists = true;

                Testing.ShouldBeTrue(() =>
                    fileEntry.ProjectId == project1.Id,
                    "FileEntry in list returned by GetFilesInFolder contains wrong project id. Data corrupted.");

                Testing.ShouldBeTrue(() =>
                    fileEntry.ParentId == folder1.Id,
                    "FileEntry in list returned by GetFilesInFolder contains wrong parent id. Data corrupted.");

                Testing.ShouldBeTrue(() =>
                    fileEntry.Name == "fileFolder1",
                    "FileEntry in list returned by GetFilesInFolder has wrong name. Data corrupted.");

                Testing.ShouldBeTrue(() =>
                    fileEntry.Changed == dtFileFolder1,
                    "FileEntry in list returned by GetFilesInFolder contains wrong changed date. Data corrupted.");
        
                Testing.ShouldBeTrue(()=>
                    fileEntry.UserId == 456,
                    "FileEntry in list returned by GetFilesInFolder contains wrong user ID. Data corrupted.");

            }

            if (fileEntry.Id == fileFolder2.Id)
            {
                fileFolder2Exists = true;

                Testing.ShouldBeTrue(() =>
                    fileEntry.ProjectId == project1.Id,
                    "FileEntry in list returned by GetFilesInFolder contains wrong project id. Data corrupted.");

                Testing.ShouldBeTrue(() =>
                    fileEntry.ParentId == folder1.Id,
                    "FileEntry in list returned by GetFilesInFolder contains wrong parent id. Data corrupted.");

                Testing.ShouldBeTrue(() =>
                    fileEntry.Name == "fileFolder2",
                    "FolderEntry in list returned by GetFilesInFolder has wrong name. Data corrupted.");

                Testing.ShouldBeTrue(() =>
                    fileEntry.Changed == dtFileFolder2,
                    "FileEntry in list returned by GetFilesInFolder contains wrong changed date. Data corrupted.");
        
                Testing.ShouldBeTrue(()=>
                    fileEntry.UserId == 4562,
                    "FileEntry in list returned by GetFilesInFolder contains wrong user ID. Data corrupted.");
            }

            if (fileEntry.Id == fileFolder3_1.Id)
            {
                fileFolder3_1Exists = true;

                Testing.ShouldBeTrue(() =>
                    fileEntry.ProjectId == project1.Id,
                    "FileEntry in list returned by GetFilesInFolder contains wrong project id. Data corrupted.");

                Testing.ShouldBeTrue(() =>
                    fileEntry.ParentId == folder3.Id,
                    "FileEntry in list returned by GetFilesInFolder contains wrong parent id. Data corrupted.");

                Testing.ShouldBeTrue(() =>
                    fileEntry.Name == "fileFolder3_1",
                    "FolderEntry in list returned by GetFilesInFolder has wrong name. Data corrupted.");

                Testing.ShouldBeTrue(() =>
                    fileEntry.Changed == dtFileFolder3_1,
                    "FileEntry in list returned by GetFilesInFolder contains wrong changed date. Data corrupted.");
        
                Testing.ShouldBeTrue(()=>
                    fileEntry.UserId == 111,
                    "FileEntry in list returned by GetFilesInFolder contains wrong user ID. Data corrupted.");
            }

            if (fileEntry.Id == fileFolder3_2.Id)
            {
                fileFolder3_2Exists = true;

                Testing.ShouldBeTrue(() =>
                    fileEntry.ProjectId == project1.Id,
                    "FileEntry in list returned by GetFilesInFolder contains wrong project id. Data corrupted.");

                Testing.ShouldBeTrue(() =>
                    fileEntry.ParentId == folder3.Id,
                    "FileEntry in list returned by GetFilesInFolder contains wrong parent id. Data corrupted.");

                Testing.ShouldBeTrue(() =>
                    fileEntry.Name == "fileFolder3_2",
                    "FolderEntry in list returned by GetFilesInFolder has wrong name. Data corrupted.");

                Testing.ShouldBeTrue(() =>
                    fileEntry.Changed == dtFileFolder3_2,
                    "FileEntry in list returned by GetFilesInFolder contains wrong changed date. Data corrupted.");
        
                Testing.ShouldBeTrue(()=>
                    fileEntry.UserId == 222,
                    "FileEntry in list returned by GetFilesInFolder contains wrong user ID. Data corrupted.");
            }

        }

        Testing.ShouldBeTrue(() =>
            fileFolder1Exists && fileFolder2Exists && fileFolder3_1Exists && fileFolder3_2Exists,
            "List returned by GetFilesInFolder is missing file entries.");

        Testing.ShouldBeTrue(() =>
            listFilesFolder2.Count == 2,
            listFilesFolder2.Count switch
            {
                0 => "GetFilesInFolder returned no file entries for folder2, expected 2.",
                1 => "GetFilesInFolder returned 1 file entry for folder 2, expected 2.",
                _ => "GetFilesInFolder returned more than 2 file entries for folder 2."
            });

        #endregion

        #region SetChanged

        var newDt = DateTime.UtcNow;
        var newUserId = 777;
        
        Testing.ShouldBeTrue(()=>
            Database.Transaction(cp=>SetChangeData(cp, fileRoot1.Id, newDt, newUserId)),
            "SetChanged did not return true -> database unchanged.");

        var changedDateEntry = Database.Transaction(cp => GetById(cp, fileRoot1.Id));
        Testing.ShouldBeTrue(()=>
            changedDateEntry?.Changed == newDt,
            "SetChanged did not set the right date.");

        Testing.ShouldBeTrue(()=>
                changedDateEntry?.UserId == newUserId,
            "SetChanged did not set the right user ID.");
        
        Testing.ShouldBeTrue(()=>
            !Database.Transaction(cp=>SetChangeData(cp, wrongFileId, newDt, newUserId)),
            "SetChanged changed changed date for non existing id -> rows in database have been altered.");
        
        #endregion

        #region SetFileState/GetFileState

        List<byte[]> testBytes = [
            [1,2,3],
            [4,5,6],
            [7,8,9]
        ];

        List<byte[]> initialState = [[0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0]];

        Testing.ShouldBeTrue(()=>
            CompareByteLists(GetFileState(fileRoot1.Id), initialState),
            "File state has not been initialized properly.");
        
        SetFileState(fileRoot1.Id, testBytes);
        
        Testing.ShouldBeTrue(()=>
            CompareByteLists(GetFileState(fileRoot1.Id), testBytes),
            CompareByteLists(GetFileState(fileRoot1.Id), initialState)? 
            "File state has not been changed by SetFileState." :
            "GetFileState returned wrong data.");
        
        #endregion
        
        #region RenameFile

        Testing.ShouldBeTrue(()=>
            Database.Transaction(cp => RenameFile(cp, fileRoot1.Id, "renamedFile")),
            "Renaming file returned false -> database unchanged.");
        
        Testing.ShouldBeTrue(()=>
            !Database.Transaction(cp => RenameFile(cp, wrongFileId, "falseRenaming")),
            "RenameFile returned true for non existing id -> changed rows in database.");

        var renamedFileEntry = Database.Transaction(cp => GetById(cp, fileRoot1.Id));
        
        Testing.ShouldBeTrue(()=>
            renamedFileEntry?.Name == "renamedFile",
            "RenameFile set the wrong name.");


        #endregion

        #region DeleteFile
        
        Database.Transaction(cp=>DeleteFile(cp, fileRoot1.Id));
        Database.Transaction(cp => DeleteFile(cp, fileFolder1.Id));

        Testing.ShouldBeTrue(()=>
            Database.Transaction(cp=>GetById(cp, fileRoot1.Id))==null,
            "DeleteFile did not delete the root file.");
        
        Testing.ShouldBeTrue(()=>
            Database.Transaction(cp=>GetById(cp, fileFolder1.Id))==null,
            "DeleteFile did not delete the file in folder 1.");
        
        Testing.ShouldBeTrue(()=>
            GetFileState(fileRoot1.Id)==null,
            "DeleteFile did not delete the file state for the root file.");

        Testing.ShouldBeTrue(() =>
                GetFileState(fileFolder1.Id) == null,
            "DeleteFile did not delete the file state for the file in folder 1.");
        
        Testing.ShouldBeTrue(()=>
            !File.Exists(stateFilePathRoot1),
            "File for file state for root file was not deleted..");
        
        Testing.ShouldBeTrue(()=>
            !File.Exists(stateFilePathFolder1),
            "File for file state for folder file was not deleted..");
        
        #endregion


    }
    
    public static bool CompareByteLists(List<byte[]>? list1, List<byte[]>? list2)
    {
        if (list1 == null || list2 == null || list1.Count != list2.Count)
            return false;

        for (int i = 0; i < list1.Count; i++)
        {
            if (!list1[i].SequenceEqual(list2[i]))
                return false;
        }

        return true;
    }
}