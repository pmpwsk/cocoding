using cocoding.Tests;

namespace cocoding.Data;

public static partial class ProjectTable
{
    /// <summary>
    /// Tests the ProjectTable class.
    /// </summary>
    internal static void Test()
    {
        # region CreateProject
        ProjectEntry createdProject1 = Database.Transaction(cp =>
            CreateProject(cp, "Test project", "Test description", 4));

        ProjectEntry? project1 = Database.Transaction(cp =>
            GetById(cp, createdProject1.Id));
        
        Testing.ShouldBeTrue(() =>
            project1 != null,
            "Project doesn't exist after creation (1).");
        if (project1 == null) return;
        
        Testing.ShouldBeTrue(() =>
            project1.Id == createdProject1.Id,
            "Project ID doesn't match after creation.");
        
        Testing.ShouldBeTrue(() =>
            project1.Name == "Test project",
            "Project name doesn't match after creation.");
        
        Testing.ShouldBeTrue(() =>
            project1.Description == "Test description",
            "Project description (non-null) doesn't match after creation.");
        
        Testing.ShouldBeTrue(() =>
                project1.Indent == createdProject1.Indent,
            "Project indentation doesn't match after creation.");
        
        ProjectEntry createdProject2 = Database.Transaction(cp =>
            CreateProject(cp, "Project without description", null, 4));

        ProjectEntry? project2 = Database.Transaction(cp =>
            GetById(cp, createdProject2.Id));
        Testing.ShouldBeTrue(() =>
            project2 != null,
            "Project doesn't exist after creation (2).");
        if (project2 == null) return;

        Testing.ShouldBeTrue(() =>
            project2.Description == null,
            "Project description (null) doesn't match after creation.");
        # endregion
        
        # region EditProject
        Testing.ShouldBeTrue(() => Database.Transaction(cp =>
            EditProject(cp, createdProject1.Id, "Name 1", null, 4)),
            "Project not updated (1).");

        project1 = Database.Transaction(cp =>
            GetById(cp, createdProject1.Id));
        Testing.ShouldBeTrue(() =>
            project1 != null,
            "Project doesn't exist after update (1).");
        if (project1 == null) return;
        
        Testing.ShouldBeTrue(() =>
            project1.Id == createdProject1.Id,
            "Project ID doesn't match after update.");
        
        Testing.ShouldBeTrue(() =>
            project1.Name == "Name 1",
            "Project name doesn't match after update.");
        
        Testing.ShouldBeTrue(() =>
            project1.Description == null,
            "Project description (null) doesn't match after update.");
        
        Testing.ShouldBeTrue(() =>
                project1.Indent == createdProject1.Indent,
            "Project indentation doesn't match after creation.");
        
        Testing.ShouldBeTrue(() => Database.Transaction(cp =>
                EditProject(cp, createdProject1.Id, "Name 2", "Description 2", 4)),
            "Project not updated (2).");

        project1 = Database.Transaction(cp =>
            GetById(cp, createdProject1.Id));
        Testing.ShouldBeTrue(() =>
                project1 != null,
            "Project doesn't exist after update (2).");
        if (project1 == null) return;
        
        Testing.ShouldBeTrue(() =>
                project1.Name == "Name 2",
            "Project name doesn't match after update.");
        
        Testing.ShouldBeTrue(() =>
                project1.Description == "Description 2",
            "Project description (non-null) doesn't match after update.");
        
        Testing.ShouldBeTrue(() => Database.Transaction(cp =>
                EditProject(cp, createdProject1.Id, "Name 3", "Description 3", 8)),
            "Project not updated (3).");

        project1 = Database.Transaction(cp =>
            GetById(cp, createdProject1.Id));
        Testing.ShouldBeTrue(() =>
                project1 != null,
            "Project doesn't exist after update (3).");
        if (project1 == null) return;
        
        Testing.ShouldBeTrue(() =>
                project1.Name == "Name 3",
            "Project name doesn't match after update.");
        
        Testing.ShouldBeTrue(() =>
                project1.Description == "Description 3",
            "Project description (non-null) doesn't match after update.");
        
        Testing.ShouldBeTrue(() =>
                project1.Indent == 8,
            "Project indentation doesn't match after creation.");
        # endregion
        
        # region DeleteProject
        Testing.ShouldBeTrue(() => Database.Transaction(cp =>
            DeleteProject(cp, createdProject1.Id)),
            "Project not deleted.");
        
        Testing.ShouldBeTrue(() => Database.Transaction(cp =>
            GetById(cp, createdProject1.Id) == null),
            "Project still exists after deletion.");
        
        Testing.ShouldBeTrue(() => Database.Transaction(cp =>
                GetById(cp, createdProject2.Id) != null),
            "Project deletion deleted another project.");
        # endregion
    }
}