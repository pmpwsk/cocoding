using cocoding.Tests;

namespace cocoding.Data;

public static partial class VersionTable {
    /// <summary>
    /// Tests the VersionTable class.
    /// </summary>
    internal static void Test()
    {

        var project = Database.Transaction(cp => ProjectTable.CreateProject(cp, "project", null, 4));
        var file = Database.Transaction(cp => FileTable.CreateFile(cp, project.Id, null, "file1", DateTime.UtcNow, 0));
        var user = Database.Transaction(cp => UserTable.CreateUser(cp, "user", "Passwd!1"));
        VersionEntry version;
        VersionEntry versionNull;
        List<byte[]> versionState = [
            [1,2,3],
            [4,5,6],
            [7,8,9]
        ];

        #region CreateVersion
        
        DateTime changed = DateTime.UtcNow;
        
        version = Database.Transaction(cp => CreateVersion(cp, file.Id, file.Name, changed, user.Id,"label","comment", versionState));
        versionNull = Database.Transaction(cp => CreateVersion(cp, file.Id, file.Name, changed, user.Id,null,null, versionState));

        string stateFileVersion = $"../FileStates/{file.Id}_{version.VersionId}.bin";
        
        Testing.ShouldBeTrue(()=>
            File.Exists(stateFileVersion),
            "No file for file version state was created.");
        
        Testing.ShouldBeTrue(()=>
            FileTable.CompareByteLists(versionState, Database.Transaction(cp => FileTable.GetFileState(file.Id, version.VersionId, true))),
            "CreateVersion did not save the correct state.");
        
        #endregion
        
        int wrongVersionId = 0;
        
        while (version.VersionId == wrongVersionId) 
            wrongVersionId++;
        
        #region GetById

        var versionById = Database.Transaction(cp => GetById(cp, version.VersionId));
        
        Testing.ShouldBeTrue(()=>
            versionById != null,
            "GetById was not able to fetch VersionEntry.");
        
        Testing.ShouldBeTrue(()=>
            versionById?.VersionId == version.VersionId,
            "GetById returned false VersionEntry.");
        
        Testing.ShouldBeTrue(()=>
            versionById?.FileId == file.Id,
            "GetByIds returned VersionEntry's file id is wrong. Data corrupted.");
        
        Testing.ShouldBeTrue(()=>
            versionById?.Name == file.Name,
            "GetByIds returned VersionEntry's file name is wrong. Data corrupted.");
        
        Testing.ShouldBeTrue(()=>
            versionById?.Changed == changed,
            "GetByIds returned VersionEntry's changed date is wrong. Data corrupted.");
        
        Testing.ShouldBeTrue(()=>
            versionById?.UserId == user.Id,
            "GetByIds returned VersionEntry's user id is wrong. Data corrupted.");
        
        Testing.ShouldBeTrue(()=>
            versionById?.Label == "label",
            "GetByIds returned VersionEntry's label is wrong. Data corrupted.");
        
        Testing.ShouldBeTrue(()=>
            versionById?.Comment == "comment",
            "GetByIds returned VersionEntry's comment is wrong. Data corrupted.");
        
        var versionByIdNull = Database.Transaction(cp => GetById(cp, versionNull.VersionId));
        
        Testing.ShouldBeTrue(()=>
            versionByIdNull?.Label == null,
            "GetByIds returned VersionEntry's label is not null. Data corrupted.");
        
        Testing.ShouldBeTrue(()=>
            versionByIdNull?.Comment == null,
            "GetByIds returned VersionEntry's comment is not null. Data corrupted.");
        
        var wrongIdQuery = Database.Transaction(cp => GetById(cp, wrongVersionId));
        
        Testing.ShouldBeTrue(()=>
            wrongIdQuery == null,
            "GetByIds returned VersionEntry for arbitrary id."
            );
        
        #endregion

        #region DeleteVersion
        
        Database.Transaction(cp=>DeleteVersion(cp, version.VersionId));

        Testing.ShouldBeTrue(()=>
            Database.Transaction(cp=>GetById(cp, version.VersionId))==null,
            "DeleteVersion did not delete the version.");
        
        Testing.ShouldBeTrue(()=>
            FileTable.GetFileState(file.Id, version.VersionId, true)==null,
            "DeleteVersion did not delete the file version state.");
        
        
        Testing.ShouldBeTrue(()=>
            !File.Exists(stateFileVersion),
            "File for file version state was not deleted..");
            
        #endregion

    }
}