using cocoding.Tests;

namespace cocoding.Data;

public static partial class ProjectManager
{
    /// <summary>
    /// Tests the ProjectManager class.
    /// </summary>
    internal static void Test()
    {
        var user = Database.Transaction(cp =>
            UserTable.CreateUser(cp, "username", "Password1#", null));
        
        # region CreateProject
        CreateProject(Security.RandomString(50), Security.RandomString(200), 4, user.Id);

        ProjectEntry createdProject = CreateProject("Test", null, 4, user.Id);
        
        ProjectEntry? project = Database.Transaction(cp =>
            ProjectTable.GetById(cp, createdProject.Id));
        Testing.ShouldBeTrue(() =>
            project != null,
            "Project doesn't exist after creation (ProjectManager).");
        if (project == null) return;
        
        Testing.ShouldBeTrue(() =>
            project.Description == null,
            "Project description wasn't set to null properly while creating project.");
        
        Testing.ShouldFail(() =>
            CreateProject("", "Description", 4, user.Id),
            "Missing project name was accepted while creating project.");
        
        Testing.ShouldFail(() =>
            CreateProject(Security.RandomString(51), "Description", 4, user.Id),
            "Long project name was accepted while creating project.");
        
        Testing.ShouldFail(() =>
            CreateProject(Security.RandomString(50), Security.RandomString(201), 4, user.Id),
            "Long project description was accepted while creating project.");
        
        Testing.ShouldFail(() =>
                CreateProject("Test", "Description", -5, user.Id),
            "Project indentation should between 1 and 20 spaces.");
        
        Testing.ShouldFail(() =>
                CreateProject("Test", "Description", 0, user.Id),
            "Project indentation should between 1 and 20 spaces.");
        
        Testing.ShouldFail(() =>
                CreateProject("Test", "Description", 100, user.Id),
            "Project indentation should between 1 and 20 spaces.");
        # endregion
        
        # region EditProject
        Testing.ShouldBeTrue(() =>
            EditProject(createdProject.Id, Security.RandomString(50), Security.RandomString(200), 4),
            "Project wasn't updated (1, ProjectManager)");
        
        Testing.ShouldBeTrue(() =>
                EditProject(createdProject.Id, Security.RandomString(50), null, 4),
            "Project wasn't updated (2, ProjectManager)");
        
        project = Database.Transaction(cp =>
            ProjectTable.GetById(cp, createdProject.Id));
        Testing.ShouldBeTrue(() =>
                project != null,
            "Project doesn't exist after update (ProjectManager).");
        if (project == null) return;
        
        Testing.ShouldBeTrue(() =>
                project.Description == null,
            "Project description wasn't set to null properly while updating project.");
        
        Testing.ShouldFail(() =>
                EditProject(createdProject.Id, "", "Description", 4),
            "Missing project name was accepted while updating project.");
        
        Testing.ShouldFail(() =>
                EditProject(createdProject.Id, Security.RandomString(51), "Description", 4),
            "Long project name was accepted while updating project.");
        
        Testing.ShouldFail(() =>
                EditProject(createdProject.Id, Security.RandomString(50), Security.RandomString(201), 4),
            "Long project description was accepted updating creating project.");
        
        Testing.ShouldFail(() =>
                EditProject(createdProject.Id, Security.RandomString(50), Security.RandomString(201), -5),
            "Project indentation should between 1 and 20 spaces.");
        
        Testing.ShouldFail(() =>
                EditProject(createdProject.Id, Security.RandomString(50), Security.RandomString(201), 0),
            "Project indentation should between 1 and 20 spaces.");
        
        Testing.ShouldFail(() =>
                EditProject(createdProject.Id, Security.RandomString(50), Security.RandomString(201), 100),
            "Project indentation should between 1 and 20 spaces.");
        # endregion
        
        # region DeleteFolder

        var folderRoot = Database.Transaction(cp => FolderTable.CreateFolder(cp, project.Id, null, "folderRoot"));
        var folderRootWithSub = Database.Transaction(cp => FolderTable.CreateFolder(cp, project.Id, null, "folderRootWithSub"));
        var folderSub = Database.Transaction(cp => FolderTable.CreateFolder(cp, project.Id, folderRootWithSub.Id, "folderSub"));
        var fileInFolder = Database.Transaction(cp => FileTable.CreateFile(cp, project.Id, folderRoot.Id, "fileInFolder", DateTime.UtcNow, 0));
        
        DeleteFolder(folderRoot.Id);
        
        var deletedFolderRoot = Database.Transaction(cp => FolderTable.GetById(cp, folderRoot.Id));
        
        Testing.ShouldBeTrue(()=>
            deletedFolderRoot == null,
            "DeleteFolder failed to delete root folder (ProjectManager).");

        var deletedFileInFolder = Database.Transaction(cp => FileTable.GetById(cp, fileInFolder.Id));
        
        Testing.ShouldBeTrue(()=>
            deletedFileInFolder == null,
            "DeleteFolder failed to delete file in folder (ProjectManager).");
        
        DeleteFolder(folderRootWithSub.Id);
        
        var deletedFolderRootWithSub = Database.Transaction(cp => FolderTable.GetById(cp, folderRootWithSub.Id));
        
        Testing.ShouldBeTrue(()=>
            deletedFolderRootWithSub == null,
            "DeleteFolder failed to delete root folder with sub directory (ProjectManager).");

        var deletedFolderSub = Database.Transaction(cp => FolderTable.GetById(cp, folderSub.Id));
        
        Testing.ShouldBeTrue(()=>
            deletedFolderSub == null,
            "DeleteFolder failed to delete sub directory (ProjectManager).");
        
        #endregion

        #region DeleteFile

        
        //Test whether version and selection are removed when the according file is deleted
        FileEntry file = Database.Transaction(cp => FileTable.CreateFile(cp, project.Id, null, "file", DateTime.UtcNow, 0));
        VersionEntry version = Database.Transaction(cp => VersionTable.CreateVersion(cp, file.Id, "version", DateTime.UtcNow, user.Id,null,null, []));
        SelectionEntry selection = Database.Transaction(cp => SelectionTable.CreateSelection(cp, file.Id, 0,1,2,3));
        DeleteFile(file.Id);

        string stateFileVersion = $"../FileStates/{file.Id}_{version.VersionId}.bin";
        
        Testing.ShouldBeTrue(()=>
            Database.Transaction(cp=>VersionTable.GetById(cp, version.VersionId))==null,
            "DeleteVersion did not delete the version when file was deleted.");
        
        Testing.ShouldBeTrue(()=>
                Database.Transaction(cp=>SelectionTable.GetById(cp, selection.SelectionId))==null,
            "DeleteSelection did not delete the selection when file was deleted.");
        
        Testing.ShouldBeTrue(()=>
            FileTable.GetFileState(file.Id, version.VersionId, true)==null,
            "DeleteVersion did not delete the file version state when file was deleted.");
        
       
        Testing.ShouldBeTrue(()=>
            !File.Exists(stateFileVersion),
            "File for file version state was not deleted when file was deleted.");
        
        #endregion

        # region DeleteForDeletedUser

        var userToRemove = Database.Transaction(cp => UserTable.CreateUser(cp, "userToRemove", "Password1!"));
        var projectBeingRemoved = CreateProject("projectBeingRemoved", null, 4, userToRemove.Id);
        
        Database.Transaction(cp => AssignmentTable.CreateAssignment(cp, project.Id, userToRemove.Id, ProjectRole.Participant, pinned: true));
        
        List<int> deleteResult = DeleteForDeletedUser(userToRemove.Id, true);

        Testing.ShouldBeTrue(() =>
            deleteResult.Count == 1,
            deleteResult.Count switch
            {

                0 => "DeleteForDeletedUser did not return any projects for justList==true (ProjectManager).",
                _ => "DeleteForDeletedUser did return too many projects for justList==true (ProjectManager)."

            });
        
        Testing.ShouldBeTrue(()=>
            deleteResult[0] == projectBeingRemoved.Id,
            "DeleteForDeletedUser returned wrong project id for justList==true (ProjectManager).");

        //invokes DeleteForDeletedUser
        UserManager.DeleteUser(userToRemove.Id);

        var assignmentsDeleted = Database.Transaction(cp => AssignmentTable.ListByUser(cp, userToRemove.Id));

        Testing.ShouldBeTrue(() =>
            assignmentsDeleted.Count == 0,
            assignmentsDeleted.Count switch {
            2 => "DeleteForDeletedUser did not remove assignments  (ProjectManager).",
            1 => assignmentsDeleted[0].ProjectId == projectBeingRemoved.Id ? 
            "DeleteForDeletedUser did not remove assignment for project that should have been removed (ProjectManager)." : 
            "DeleteForDeletedUser did not remove assignments for project that was not removed (ProjectManager).",
            _ => "DeleteForDeletedUser created more assignments. (ProjectManager)."
        });
        
        Testing.ShouldBeTrue(()=>
            Database.Transaction(cp => ProjectTable.GetById(cp, projectBeingRemoved.Id))== null,
            "DeleteForDeletedUser did not remove project only being assigned to deleted user (ProjectManager).");
        
        #endregion

        # region DeleteProject

        //SET UP FOLDERS AND FILES (USER ALREADY HAS TO EXIST FOR THIS PROJECT)
        folderRoot = Database.Transaction(cp => FolderTable.CreateFolder(cp, project.Id, null, "folderRoot"));
        folderRootWithSub = Database.Transaction(cp => FolderTable.CreateFolder(cp, project.Id, null, "folderRootWithSub"));
        folderSub = Database.Transaction(cp => FolderTable.CreateFolder(cp, project.Id, folderRootWithSub.Id, "folderSub"));
        
        file = Database.Transaction(cp => FileTable.CreateFile(cp, project.Id, null, "file", DateTime.UtcNow, 0));
        fileInFolder = Database.Transaction(cp => FileTable.CreateFile(cp, project.Id, folderRoot.Id, "fileInFolder", DateTime.UtcNow, 0));
        
        //DELETE PROJECT
        DeleteProject(project.Id);
        
        //CHECK EVERYTHING HAS BEEN REMOVED 
        var getByIdDeleted = Database.Transaction(cp => ProjectTable.GetById(cp, project.Id));
        
        Testing.ShouldBeTrue(()=>
            getByIdDeleted == null,
            "DeleteProject failed to remove project (ProjectManager).");
        
        assignmentsDeleted = Database.Transaction(cp => AssignmentTable.ListByProject(cp, project.Id));
        
        Testing.ShouldBeTrue(()=>
            assignmentsDeleted.Count == 0,
            "DeleteProject failed to remove assignments (ProjectManager).");
        
        deletedFolderRoot = Database.Transaction(cp => FolderTable.GetById(cp, folderRoot.Id));
        
        Testing.ShouldBeTrue(()=>
            deletedFolderRoot == null,
            "DeleteProject failed to delete root folder (ProjectManager).");

        deletedFolderRootWithSub = Database.Transaction(cp => FolderTable.GetById(cp, folderRootWithSub.Id));
        
        Testing.ShouldBeTrue(()=>
            deletedFolderRootWithSub == null,
            "DeleteProject failed to delete root folder with sub directory (ProjectManager).");

        deletedFolderSub = Database.Transaction(cp => FolderTable.GetById(cp, folderSub.Id));
        
        Testing.ShouldBeTrue(()=>
            deletedFolderSub == null,
            "DeleteProject failed to delete sub directory (ProjectManager).");
        
        var deletedFile = Database.Transaction(cp => FileTable.GetById(cp, file.Id));
        deletedFileInFolder = Database.Transaction(cp => FileTable.GetById(cp, fileInFolder.Id));
       
        Testing.ShouldBeTrue(()=>
            deletedFile == null,
            "DeleteProject failed to delete file (ProjectManager).");
        
        Testing.ShouldBeTrue(()=>
            deletedFileInFolder == null,
            "DeleteProject failed to delete file inside folder (ProjectManager).");

        #endregion

    }
}