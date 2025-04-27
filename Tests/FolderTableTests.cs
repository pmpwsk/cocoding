using cocoding.Tests;

namespace cocoding.Data;

public static partial class FolderTable {
    
    /// <summary>
    /// Tests the FolderTable class.
    /// </summary>
    internal static void Test() {
        
        var project1 = Database.Transaction(cp => ProjectTable.CreateProject(cp, "project1", null, 4)); 
        var project2 = Database.Transaction(cp => ProjectTable.CreateProject(cp, "project2", null, 4)); 
    
        #region CreateFolder 
        
        var folderRoot1 = Database.Transaction(cp=> FolderTable.CreateFolder(cp, project1.Id, null, "folderRoot1"));
        
        Testing.ShouldBeTrue(()=>
            folderRoot1.ProjectId == project1.Id,
            "False project id for root folder 1 after creation.");
        
        Testing.ShouldBeTrue(()=>
            folderRoot1.ParentId == null, 
            "False parent id for root folder 1 after creation.");
        
        Testing.ShouldBeTrue(()=>
            folderRoot1.Name == "folderRoot1",
            "False name for root folder 1 after creation.");
        
        var folderRoot2 = Database.Transaction(cp=> FolderTable.CreateFolder(cp, project1.Id, null, "folderRoot2"));
        
        Testing.ShouldBeTrue(()=>
            folderRoot2.ProjectId == project1.Id,
            "False project id for root folder 2 after creation.");
        
        Testing.ShouldBeTrue(()=>
            folderRoot2.ParentId == null, 
            "False parent id for root folder 2 after creation.");
        
        Testing.ShouldBeTrue(()=>
            folderRoot2.Name == "folderRoot2",
            "False name for root folder 2 after creation.");
        
        var folderSub1 = Database.Transaction(cp=> FolderTable.CreateFolder(cp, project1.Id, folderRoot1.Id, "folderSub1"));
        
        Testing.ShouldBeTrue(()=>
            folderSub1.ProjectId == project1.Id,
            "False project id for sub folder 1 after creation.");
        
        Testing.ShouldBeTrue(()=>
            folderSub1.ParentId == folderRoot1.Id, 
            "False parent id for sub folder 1 after creation.");
        
        Testing.ShouldBeTrue(()=>
            folderSub1.Name == "folderSub1",
            "False name for sub folder 1 after creation.");
        
        var folderSub2 = Database.Transaction(cp=> FolderTable.CreateFolder(cp, project1.Id, folderRoot1.Id, "folderSub2"));
        
        Testing.ShouldBeTrue(()=>
            folderSub2.ProjectId == project1.Id,
            "False project id for sub folder 2 after creation.");
        
        Testing.ShouldBeTrue(()=>
            folderSub2.ParentId == folderRoot1.Id, 
            "False parent id for sub folder 2 after creation.");
        
        Testing.ShouldBeTrue(()=>
            folderSub2.Name == "folderSub2",
            "False name for sub folder 2 after creation.");
        
        #endregion
        
        int wrongFolderId = 0;

        while (folderRoot1.Id == wrongFolderId || folderSub1.Id == wrongFolderId || folderRoot2.Id == wrongFolderId)
            wrongFolderId++;
        
        #region GetById
        
        var folderByIdRoot = Database.Transaction(cp => GetById(cp, folderRoot1.Id));
        
        Testing.ShouldBeTrue(()=>
            folderByIdRoot?.Id == folderRoot1.Id,
            "GetById returned false FolderEntry.");
        
        Testing.ShouldBeTrue(()=>
            folderByIdRoot?.ParentId == null,
            "GetByIds returned FolderEntry's parent id is wrong for root folder. Data corrupted.");
        
        Testing.ShouldBeTrue(()=>
            folderByIdRoot?.Name == "folderRoot1",
            "GetByIds returned FolderEntry's name is wrong for root folder. Data corrupted.");
        
        var folderByIdSub = Database.Transaction(cp => GetById(cp, folderSub1.Id));
        
        Testing.ShouldBeTrue(()=>
            folderByIdSub?.Id == folderSub1.Id,
            "GetById returned false FolderEntry.");
        
        Testing.ShouldBeTrue(()=>
            folderByIdSub?.ParentId == folderRoot1.Id,
            "GetByIds returned FolderEntry's parent id is wrong for sub folder. Data corrupted.");
        
        Testing.ShouldBeTrue(()=>
            folderByIdSub?.Name == "folderSub1",
            "GetByIds returned FolderEntry's name is wrong for sub folder. Data corrupted.");

        var wrongIdFolder = Database.Transaction(cp => GetById(cp, wrongFolderId));

        Testing.ShouldBeTrue(()=>
            wrongIdFolder==null,
            "GetById returned FolderEntry for non existing id.");
        
        #endregion
        
        #region ListByFolder

        var listFolderRoot1 = Database.Transaction(cp => ListByFolder(cp, folderRoot1.Id));
        var listFolderRoot2 = Database.Transaction(cp => ListByFolder(cp, folderRoot2.Id));

        Testing.ShouldBeTrue(() =>
            listFolderRoot1.Count == 2,
            listFolderRoot1.Count switch
            {
                0 => "ListByFolder returned no entries for root folder in project1, expected 2.",
                1 => "ListByFolder returned 1 entry for root folder in project1, expected 2.",
                _ => "ListByFolder returned more than 2 entries for root folder in project1."
            });

        bool subFolder1Exists = false;
        bool subFolder2Exists = false;

        foreach (var folderEntry in listFolderRoot1)
        {
            if (folderEntry.Id == folderSub1.Id)
            {
                subFolder1Exists = true;
                
                Testing.ShouldBeTrue(()=>
                    folderEntry.ProjectId == project1.Id,
                    "FolderEntry in list returned by ListByFolder contains wrong project id. Data corrupted.");
                
                Testing.ShouldBeTrue(()=>
                    folderEntry.ParentId == folderRoot1.Id,
                    "FolderEntry in list returned by ListByFolder contains wrong parent id. Data corrupted.");
                
                Testing.ShouldBeTrue(()=>
                    folderEntry.Name == "folderSub1",
                    "FolderEntry in list returned by ListByFolder has wrong name. Data corrupted.");
                
            }

            if (folderEntry.Id == folderSub2.Id)
            {
                subFolder2Exists = true;
                
                Testing.ShouldBeTrue(()=>
                    folderEntry.ProjectId == project1.Id,
                    "FolderEntry in list returned by ListByFolder contains wrong project id. Data corrupted.");
                
                Testing.ShouldBeTrue(()=>
                    folderEntry.ParentId == folderRoot1.Id,
                    "FolderEntry in list returned by ListByFolder contains wrong parent id. Data corrupted.");
                
                Testing.ShouldBeTrue(()=>
                    folderEntry.Name == "folderSub2",
                    "FolderEntry in list returned by ListByFolder has wrong name. Data corrupted.");
            }

        }
        
        Testing.ShouldBeTrue(()=>
            subFolder1Exists && subFolder2Exists,
            "List returned by ListByFolder is missing entries.");
        
        Testing.ShouldBeTrue(()=>
            listFolderRoot2.Count == 0,
            "List returned by ListByFolder has entries when expected non.");

        #endregion

        #region ListByProjectRoot

        var byProjectList1 = Database.Transaction(cp => ListByProjectRoot(cp, project1.Id));
        var byProjectList2 = Database.Transaction(cp => ListByProjectRoot(cp, project2.Id));
         
        Testing.ShouldBeTrue(() =>
            byProjectList1.Count == 2,
            byProjectList1.Count switch
            {
                0 => "ListByProjectRoot returned no entries for root folder in project1, expected 2.",
                1 => "ListByProjectRoot returned 1 entry for root folder in project1, expected 2.",
                _ => "ListByProjectRoot returned more than 2 entries for root folder in project1."
            });

        bool rootFolder1Exists = false;
        bool rootFolder2Exists = false;

        foreach (var folderEntry in byProjectList1)
        {
            if (folderEntry.Id == folderRoot1.Id)
            {
                rootFolder1Exists = true;
                
                Testing.ShouldBeTrue(()=>
                    folderEntry.ProjectId == project1.Id,
                    "FolderEntry in list returned by ListByProjectRoot contains wrong project id. Data corrupted.");
                
                Testing.ShouldBeTrue(()=>
                    folderEntry.ParentId == null,
                    "FolderEntry in list returned by ListByProjectRoot contains wrong parent id. Data corrupted.");
                
                Testing.ShouldBeTrue(()=>
                    folderEntry.Name == "folderRoot1",
                    "FolderEntry in list returned by ListByProjectRoot has wrong name. Data corrupted.");
                
            }

            if (folderEntry.Id == folderRoot2.Id)
            {
                rootFolder2Exists = true;
                
                Testing.ShouldBeTrue(()=>
                    folderEntry.ProjectId == project1.Id,
                    "FolderEntry in list returned by ListByProjectRoot contains wrong project id. Data corrupted.");
                
                Testing.ShouldBeTrue(()=>
                    folderEntry.ParentId == null,
                    "FolderEntry in list returned by ListByProjectRoot contains wrong parent id. Data corrupted.");
                
                Testing.ShouldBeTrue(()=>
                    folderEntry.Name == "folderRoot2",
                    "FolderEntry in list returned by ListByProjectRoot has wrong name. Data corrupted.");
            }

        }
        
        Testing.ShouldBeTrue(()=>
            rootFolder1Exists && rootFolder2Exists,
            "List returned by ListByProjectRoot is missing entries.");
        
        Testing.ShouldBeTrue(()=>
            byProjectList2.Count == 0,
            "List returned by ListByProjectRoot has entries when expected non.");
        

        #endregion

        #region RenameFolder
        
        Testing.ShouldBeTrue(()=>
            Database.Transaction(cp => RenameFolder(cp, folderRoot1.Id, "renamedFolder")),
            "Renaming folder returned false -> database unchanged.");
        
        Testing.ShouldBeTrue(()=>
            !Database.Transaction(cp => RenameFolder(cp, wrongFolderId, "falseRenaming")),
            "RenameFolder returned true for non existing id -> changed rows in database.");

        var renamedFolderEntry = Database.Transaction(cp => GetById(cp, folderRoot1.Id));
        
        Testing.ShouldBeTrue(()=>
            renamedFolderEntry?.Name == "renamedFolder",
            "RenameFolder set the wrong name.");

        #endregion
        
        #region DeleteFolder

        Testing.ShouldBeTrue(()=>
            Database.Transaction(cp=>DeleteFolder(cp, folderSub2.Id)),
            "Deleting folder returned false -> database unchanged.");

        Testing.ShouldBeTrue(()=>
            Database.Transaction(cp=>GetById(cp, folderSub2.Id))==null,
            "DeleteFolder did not delete the right folder.");
        
        Testing.ShouldBeTrue(()=>
            Database.Transaction(cp=>DeleteFolder(cp, folderRoot1.Id)),
            "Deleting folder returned false -> database unchanged.");

        Testing.ShouldBeTrue(()=>
            Database.Transaction(cp=>GetById(cp, folderRoot1.Id))==null,
            "DeleteFolder did not delete the root folder.");
        
        Testing.ShouldBeTrue(()=>
            !Database.Transaction(cp=>DeleteFolder(cp, wrongFolderId)),
            "DeleteFolder deleted folder with non existing id -> changed rows in database.");

        #endregion

    }
}