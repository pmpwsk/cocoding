using cocoding.Tests;

namespace cocoding.Data;

public static partial class AssignmentTable
{
    /// <summary>
    /// Tests the AssignmentTable class.
    /// </summary>
    internal static void Test()
    {
        
        int userId1 = Database.Transaction(cp =>
            UserTable.CreateUser(cp, "user1", "Password1#", null))?.Id ?? throw new Exception("Failed to create user for AssignmentTable tests.");
        
        int userId2 = Database.Transaction(cp =>
            UserTable.CreateUser(cp, "user2", "Password1#", null))?.Id ?? throw new Exception("Failed to create user for AssignmentTable tests.");
        
        int projectId1 = Database.Transaction(cp =>
            ProjectTable.CreateProject(cp, "project1", null, 4))?.Id ?? throw new Exception("Failed to create project for AssignmentTable tests.");
        
        int projectId2 = Database.Transaction(cp =>
            ProjectTable.CreateProject(cp, "project2", null, 4))?.Id ?? throw new Exception("Failed to create project for AssignmentTable tests.");
        
        # region CreateAssignment
        
        AssignmentEntry assignmentUser1Project1AsOwner = Database.Transaction(cp => 
            CreateAssignment(cp, projectId1, userId1, ProjectRole.Owner, pinned: true));
        
        Testing.ShouldBeTrue(()=>
            assignmentUser1Project1AsOwner.ProjectRole == ProjectRole.Owner, 
            "Assignment entry contains wrong role after creation.");

        Testing.ShouldBeTrue(()=>
            assignmentUser1Project1AsOwner.UserId == userId1,
            "Assignment entry contains wrong user id after creation.");
           
        Testing.ShouldBeTrue(()=>
            assignmentUser1Project1AsOwner.ProjectId == projectId1,
            "Assignment entry contains wrong project id after creation.");

        Testing.ShouldBeTrue(() =>
            assignmentUser1Project1AsOwner.Pinned == true,
            "Assignment entry contains wrong pinned status after creation.");
        
        AssignmentEntry assignmentUser2Project1AsManager = Database.Transaction(cp => 
            CreateAssignment(cp, projectId1, userId2, ProjectRole.Manager, pinned: false));
        
        Testing.ShouldBeTrue(()=>
            assignmentUser2Project1AsManager.ProjectRole == ProjectRole.Manager, 
            "Assignment entry contains wrong role after creation.");

        Testing.ShouldBeTrue(()=>
            assignmentUser2Project1AsManager.UserId == userId2,
            "Assignment entry contains wrong user id after creation.");
           
        Testing.ShouldBeTrue(()=>
            assignmentUser2Project1AsManager.ProjectId == projectId1,
            "Assignment entry contains wrong project id after creation.");

        Testing.ShouldBeTrue(() =>
            assignmentUser2Project1AsManager.Pinned == false,
            "Assignment entry contains wrong pinned status after creation.");

        AssignmentEntry assignmentUser1Project2AsParticipant = Database.Transaction(cp => 
            CreateAssignment(cp, projectId2, userId1, ProjectRole.Participant, pinned: true));
        
        Testing.ShouldBeTrue(()=>
            assignmentUser1Project2AsParticipant.ProjectRole == ProjectRole.Participant, 
            "Assignment entry contains wrong role after creation.");

        Testing.ShouldBeTrue(()=>
            assignmentUser1Project2AsParticipant.UserId == userId1,
            "Assignment entry contains wrong user id after creation.");
           
        Testing.ShouldBeTrue(()=>
            assignmentUser1Project2AsParticipant.ProjectId == projectId2,
            "Assignment entry contains wrong project id after creation.");

        Testing.ShouldBeTrue(() =>
            assignmentUser1Project2AsParticipant.Pinned == true,
            "Assignment entry contains wrong pinned status after creation.");

        #endregion

        #region GetRole

        Testing.ShouldBeTrue(()=>
            Database.Transaction(cp=>
            GetRole(cp, projectId1, userId1)==ProjectRole.Owner),
            "GetRole returned wrong role for Owner.");
        
        Testing.ShouldBeTrue(()=>
            Database.Transaction(cp=>
            GetRole(cp, projectId1, userId2)==ProjectRole.Manager),
            "GetRole returned wrong role for Manager.");
        
        Testing.ShouldBeTrue(()=>
            Database.Transaction(cp=>
            GetRole(cp, projectId2, userId1)==ProjectRole.Participant),
            "GetRole returned wrong role for Participant.");
        
        Testing.ShouldBeTrue(()=>
            Database.Transaction(cp=>
            GetRole(cp, projectId2, userId2)==ProjectRole.None),
            "GetRole returned did not return None for user not assigned to project.");
        
        #endregion
        
        # region ListByProject

        List<AssignmentEntry> assignmentsProject1 = Database.Transaction(cp => ListByProject(cp, projectId1));
        
        Testing.ShouldBeTrue(()=>
            assignmentsProject1.Count == 2,
            "ListByProject returned wrong number of entries for project 1: Expected 2, actually got " + assignmentsProject1.Count);

        bool user1InProject1 = false;
        bool user2InProject1 = false;

        foreach (var assignmentEntry in assignmentsProject1)
        {
            Testing.ShouldBeTrue(()=>
                assignmentEntry.ProjectId == projectId1, 
                "ListByProject returned assignment entry contains wrong project id");
            
            if (assignmentEntry.UserId == userId1)
            {
                user1InProject1 = true;
                Testing.ShouldBeTrue(()=>
                    assignmentEntry.ProjectRole == assignmentUser1Project1AsOwner.ProjectRole,
                    "ListByProject returned wrong project role for participant. Data is corrupted.");

                Testing.ShouldBeTrue(() =>
                    assignmentEntry.Pinned == true,
                    "ListByProject returned wrong pinned status for participant. Data is corrupted.");
            }
            if (assignmentEntry.UserId == userId2)
            {
                user2InProject1 = true;
                Testing.ShouldBeTrue(()=>
                    assignmentEntry.ProjectRole == assignmentUser2Project1AsManager.ProjectRole,
                    "ListByProject returned wrong project role for participant. Data is corrupted.");

                Testing.ShouldBeTrue(() =>
                    assignmentEntry.Pinned == false,
                    "ListByProject returned wrong pinned status for participant. Data is corrupted.");
            }
        }
        
        if (!user1InProject1 || !user2InProject1)
            Testing.ShouldBeTrue(()=>false,
                "ListByProject failed to return all users for project 1.");
        
        List<AssignmentEntry> assignmentsProject2 = Database.Transaction(cp => ListByProject(cp, projectId2));
        
        Testing.ShouldBeTrue(()=>
            assignmentsProject2.Count == 1,
            "ListByProject returned wrong number of entries for project 2: Expected 1, actually got " + assignmentsProject2.Count);
        
        Testing.ShouldBeTrue(()=>
            assignmentsProject2[0].UserId==userId1,
            "ListByProject returned wrong user entry for project 2.");
        
        Testing.ShouldBeTrue(()=>
            assignmentsProject2[0].ProjectRole == assignmentUser1Project2AsParticipant.ProjectRole,
            "ListByProject returned wrong project role for participant. Data is corrupted.");

        Testing.ShouldBeTrue(() =>
            assignmentsProject2[0].Pinned == true,
            "ListByProject returned wrong pinned status for participant. Data is corrupted.");

        #endregion

        #region ListByUser

        List<AssignmentEntry> assignmentsUser1 = Database.Transaction(cp=>ListByUser(cp, userId1));
        
        Testing.ShouldBeTrue(() => 
            assignmentsUser1.Count == 2,
            "ListByUser returned wrong number of entries for user 1: expected 2, actually returned " + assignmentsUser1.Count); 
        
        bool project1InUser1 = false;
        bool project2InUser1 = false;

        foreach (var assignmentEntry in assignmentsUser1)
        {
            Testing.ShouldBeTrue(()=>
                assignmentEntry.UserId == userId1, 
                "ListByUser returned assignment entry contains wrong user id");
            
            if (assignmentEntry.ProjectId == projectId1)
            {
                project1InUser1 = true;
                Testing.ShouldBeTrue(()=>
                    assignmentEntry.ProjectRole == assignmentUser1Project1AsOwner.ProjectRole,
                    "ListByUser returned wrong project role for participant. Data is corrupted.");

                Testing.ShouldBeTrue(() =>
                    assignmentEntry.Pinned == true,
                    "ListByUser returned wrong pinned status for participant. Data is corrupted");
            }
            if (assignmentEntry.ProjectId == projectId2)
            {
                project2InUser1 = true;
                Testing.ShouldBeTrue(()=>
                    assignmentEntry.ProjectRole == assignmentUser1Project2AsParticipant.ProjectRole,
                    "ListByProject returned wrong project role for participant. Data is corrupted.");

                Testing.ShouldBeTrue(() =>
                    assignmentEntry.Pinned == true,
                    "ListByUser returned wrong pinned status for participant. Data is corrupted");
            }
        }
        
        if (!project1InUser1 || !project2InUser1)
            Testing.ShouldBeTrue(()=>false,
                "ListByUser failed to return all projects for user 1.");
        
        List<AssignmentEntry> assignmentsUser2 = Database.Transaction(cp=>ListByUser(cp, userId2));
        
        Testing.ShouldBeTrue(() => 
            assignmentsUser2.Count == 1,
            "ListByUser returned wrong number of entries for user 2: expected 1, actually returned " + assignmentsUser2.Count); 
        
        Testing.ShouldBeTrue(()=>
            assignmentsUser2[0].UserId==userId2,
            "ListByUser returned wrong user entry for user 2.");
        
        Testing.ShouldBeTrue(()=>
            assignmentsUser2[0].ProjectId==projectId1,
            "ListByUser returned wrong project for user 2. Data is corrupted.");
        
        Testing.ShouldBeTrue(()=>
            assignmentsUser2[0].ProjectRole == assignmentUser2Project1AsManager.ProjectRole,
            "ListByUser returned wrong project role for participant. Data is corrupted.");

        Testing.ShouldBeTrue(() =>
            assignmentsUser2[0].Pinned == false,
            "ListByUser returned wrong pinned status for participant. Data is corrupted");

        #endregion

        #region GetPinnedStatus

        Testing.ShouldBeTrue(() =>
            Database.Transaction(cp => GetPinnedStatus(cp, projectId1, userId1)) == true,
            "GetPinnedStatus returned inncorrect result of pinned status.");

        Testing.ShouldBeTrue(() =>
            Database.Transaction(cp => GetPinnedStatus(cp, projectId1, userId2)) == false,
            "GetPinnedStatus returned inncorrect result of pinned status.");

        Testing.ShouldBeTrue(() =>
            Database.Transaction(cp => GetPinnedStatus(cp, projectId2, userId1)) == true,
            "GetPinnedStatus returned inncorrect result of pinned status.");

        #endregion

        #region SetPinnedStatus

        List<AssignmentEntry> assignmentsBeforeUpdate = Database.Transaction(cp => ListByProject(cp, projectId1));

        Testing.ShouldBeTrue(() =>
            assignmentsBeforeUpdate.Count == 2,
            "ListByProject returned wrong number of entries before update pinned status.");

        bool user1BeforeUpdate = false;
        bool user2BeforeUpdate = false;

        foreach (var assignment in assignmentsBeforeUpdate)
        {
            if (assignment.UserId == userId1)
            {
                Testing.ShouldBeTrue(() =>
                    assignment.Pinned == true,
                    "User 1 pinned status is incorrect before update pinned status.");
                user1BeforeUpdate = true;
            }
            if (assignment.UserId == userId2)
            {
                Testing.ShouldBeTrue(() =>
                    assignment.Pinned == false,
                    "User 2 pinned status is incorrect before update pinned status.");
                user2BeforeUpdate = true;
            }
        }

        Testing.ShouldBeTrue(() =>
            user1BeforeUpdate && user2BeforeUpdate,
            "Not all users were found in ListByProject before update pinned status.");

        Database.Transaction(cp => SetPinnedStatus(cp, projectId1, userId1, newPinnedStatus: false));

        List<AssignmentEntry> assignmentsAfterUpdate = Database.Transaction(cp => ListByProject(cp, projectId1));

        Testing.ShouldBeTrue(() =>
            assignmentsAfterUpdate.Count == 2,
            "ListByProject returned wrong number of entries after update pinned status.");

        bool user1AfterUpdate = false;
        bool user2AfterUpdate = false;

        foreach (var assignment in assignmentsAfterUpdate)
        {
            if (assignment.UserId == userId1)
            {
                Testing.ShouldBeTrue(() =>
                    assignment.Pinned == false,
                    "User 1 pinned status is incorrect after update pinned status.");
                user1AfterUpdate = true;
            }
            if (assignment.UserId == userId2)
            {
                Testing.ShouldBeTrue(() =>
                    assignment.Pinned == false,
                    "User 2 pinned status is incorrect after update pinned status.");
                user2AfterUpdate = true;
            }
        }

        Testing.ShouldBeTrue(() =>
            user1AfterUpdate && user2AfterUpdate,
            "Not all users were found in ListByProject after update pinned status.");

        #endregion

        #region DeleteAssignment

        Database.Transaction(cp=>
            DeleteAssignment(cp, projectId1, userId1));
        
        List<AssignmentEntry> assignmentEntriesProject1 = Database.Transaction(cp=>ListByProject(cp, projectId1));
        
        Testing.ShouldBeTrue(()=>
            assignmentEntriesProject1.Count==1,
            assignmentEntriesProject1.Count switch
            {
                2 => "Delete assignment did not work.",
                0 => "Delete assignment deleted all entries instead of one.",
                _ => "Delete assignment created more assignments."
            });
        
        Testing.ShouldBeTrue(()=>
            assignmentEntriesProject1[0].UserId==userId2,
            "Delete assignment deleted wrong user."
        );

        Database.Transaction(cp=>
            DeleteAssignment(cp, projectId1, userId2));
        
        assignmentEntriesProject1 = Database.Transaction(cp=>ListByProject(cp, projectId1));
        
        Testing.ShouldBeTrue(() =>
            assignmentEntriesProject1.Count == 0,
            "Delete assignment failed to delete last assignment.");

        #endregion

        
    }
}