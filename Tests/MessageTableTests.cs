using cocoding.Tests;

namespace cocoding.Data;

public static partial class MessageTable
{
    /// <summary>
    /// Tests the MessageTable class.
    /// </summary>
    internal static void Test()
    {
        var project = Database.Transaction(cp => ProjectTable.CreateProject(cp, "project", "project for automated tests", 4));
        var user = Database.Transaction(cp => UserTable.CreateUser(cp, "user", "abcdefghiJ%!D"));
        var file = Database.Transaction(cp => FileTable.CreateFile(cp, project.Id, null, "string", DateTime.UtcNow, user.Id));
        var selection = Database.Transaction(cp => SelectionTable.CreateSelection(cp, file.Id, 0, 1, 2, 3));
        
        #region CreateMessageWithoutResponse
        var timestamp = DateTime.UtcNow;
        
        MessageEntry expectedMessage = Database.Transaction(cp => CreateMessage(cp, project.Id, user.Id, "text", timestamp));
        MessageEntry? actualMessage = Database.Transaction(cp => GetById(cp, expectedMessage.Id));
        
        MessageEntry expectedMessageWithSelection = Database.Transaction(cp => CreateMessage(cp, project.Id, user.Id, "text", timestamp, selection.SelectionId));
        MessageEntry? actualMessageWithSelection = Database.Transaction(cp => GetById(cp, expectedMessageWithSelection.Id));
        
        Testing.ShouldBeTrue(() =>
                actualMessage != null,
            "Message doesn't exist after creation.");
        
        if (actualMessage == null) return;
        
        Testing.ShouldBeTrue(() =>
                actualMessage.Id == expectedMessage.Id,
            "Message ID doesn't match after creation.");
        
        Testing.ShouldBeTrue(() =>
                actualMessage.ProjectId == expectedMessage.ProjectId,
            "Message project ID doesn't match after creation.");
        
        Testing.ShouldBeTrue(() =>
                actualMessage.UserId == expectedMessage.UserId,
            "Message user ID doesn't match after creation.");
        
        Testing.ShouldBeTrue(() =>
                actualMessage.Text == expectedMessage.Text,
            "Message text doesn't match after creation.");
        
        Testing.ShouldBeTrue(() =>
                actualMessage.Timestamp == expectedMessage.Timestamp,
            "Message timestamp doesn't match after creation.");
        
        Testing.ShouldBeTrue(() =>
                actualMessage.SelectionId == null,
            "Message selectionId is not null after creation.");
        
        if (actualMessageWithSelection == null) 
            return;
        
        Testing.ShouldBeTrue(() =>
                actualMessageWithSelection.SelectionId == selection.SelectionId,
            "Message selectionId does not match after creation.");
        
        #endregion
        
        #region CreateMessageWithResponse
        var userResponse = Database.Transaction(cp => UserTable.CreateUser(cp, "userResponse", "abcdefghiJ%!D"));
        var timestampResponse = DateTime.UtcNow;
        MessageEntry response = Database.Transaction(cp => CreateMessage(cp, project.Id, userResponse.Id, "this is a response", timestampResponse));
        
        MessageEntry expectedMessageResponse = Database.Transaction(cp => CreateMessage(cp, project.Id, user.Id, "text", timestamp, null, response.Id));
        MessageEntry? actualMessageResponse = Database.Transaction(cp => GetById(cp, expectedMessageResponse.Id));
        
        Testing.ShouldBeTrue(() =>
                actualMessageResponse != null,
            "Message doesn't exist after creation.");
        
        if (actualMessageResponse == null) return;
        
        Testing.ShouldBeTrue(() =>
                actualMessageResponse.Id == expectedMessageResponse.Id,
            "Message ID doesn't match after creation.");
        
        Testing.ShouldBeTrue(() =>
                actualMessageResponse.ProjectId == expectedMessageResponse.ProjectId,
            "Message project ID doesn't match after creation.");
        
        Testing.ShouldBeTrue(() =>
                actualMessageResponse.UserId == expectedMessageResponse.UserId,
            "Message user ID doesn't match after creation.");
        
        Testing.ShouldBeTrue(() =>
                actualMessageResponse.Text == expectedMessageResponse.Text,
            "Message text doesn't match after creation.");
        
        Testing.ShouldBeTrue(() =>
                actualMessageResponse.Timestamp == expectedMessageResponse.Timestamp,
            "Message timestamp doesn't match after creation.");
        
        Testing.ShouldBeTrue(() =>
                actualMessageResponse.SelectionId == null,
            "Message selectionId is not null after creation.");
        
        Testing.ShouldBeTrue(() =>
                actualMessageResponse.ResponseTo == response.Id,
            "Message responseTo ID doesn't match with the actual message ID of the response after creation.");
        #endregion
        
        #region UPDATE METHODS

        Database.Transaction(cp => {
            string newText = "newText";
            UpdateText(cp, actualMessage.Id, newText);
            var newSelection = SelectionTable.CreateSelection(cp, file.Id, 0, 1, 2, 3 );
            UpdateSelection(cp, actualMessage.Id, newSelection.SelectionId);
            var newMessageToRespondTo = CreateMessage(cp, project.Id, user.Id, "text", timestamp);
            UpdateResponseTo(cp, actualMessage.Id, newMessageToRespondTo.Id);
            
            var updatedMessage = GetById(cp, actualMessage.Id);
            
            Testing.ShouldBeTrue(()=>
                updatedMessage?.Text == newText,
                "UpdateText did not update the text field.");
            
            Testing.ShouldBeTrue(()=>
                updatedMessage?.SelectionId == newSelection.SelectionId,
                "UpdateText did not update the text field.");
            Testing.ShouldBeTrue(()=>
                updatedMessage?.ResponseTo == newMessageToRespondTo.Id,
                "UpdateText did not update the text field.");
        });
        
        #endregion
        
        #region DeleteMessage
        MessageEntry messageToDelete;
        MessageEntry? messageAfterDelete;
        Database.Transaction(cp => {
            messageToDelete = CreateMessage(cp, project.Id, user.Id, "text", timestamp);
            DeleteMessage(cp, messageToDelete.Id);
            messageAfterDelete = GetById(cp, messageToDelete.Id);
            Testing.ShouldBeTrue(()=>
                       messageAfterDelete == null,
            "DeleteMessage did not delete the message.");
        });
        #endregion
        
        #region DeleteMessagesByProject
        
        // test case 1: check if method "MessageTable.DeleteAllMessagesByProject" works as expected
        Testing.ShouldBeTrue(() => Database.Transaction(cp =>
                DeleteAllMessagesByProject(cp, project.Id)),
            "Messages were not deleted by project.");
        
        Testing.ShouldBeTrue(() => Database.Transaction(cp =>
                GetById(cp, expectedMessage.Id) == null),
            "Message still exists after deletion by project.");
        
        // test case 2: check if all messages of a project were deleted automatically after deleting the project
        MessageEntry expectedMessageDeletedByProjectDeletion = Database.Transaction(cp => CreateMessage(cp, project.Id, user.Id, "textToDelete", timestamp)); 
        
        ProjectManager.DeleteProject(project.Id);
        
        MessageEntry? actualMessageDeletedByProjectDeletion = Database.Transaction(cp => GetById(cp, expectedMessageDeletedByProjectDeletion.Id));

        if (actualMessageDeletedByProjectDeletion == null) return;
        
        Testing.ShouldBeTrue(() =>
                actualMessageDeletedByProjectDeletion.Id == expectedMessageDeletedByProjectDeletion.Id,
            "Messages were not deleted by project deletion.");
        
        #endregion
        
        #region DeleteMessagesByUser
        
        // test case 1: check if method "MessageTable.DeleteAllMessagesByUser" works as expected
        // create new project because the first project was deleted within the test region DeleteMessagesByProject
        var project2 = Database.Transaction(cp => ProjectTable.CreateProject(cp, "project2", "project2 for automated tests", 4));
        
        // create new message because the first message was deleted within the test region DeleteMessagesByProject
        MessageEntry expectedMessage2 = Database.Transaction(cp => CreateMessage(cp, project2.Id, user.Id, "text2", timestamp));
        
        Testing.ShouldBeTrue(() => Database.Transaction(cp =>
                DeleteAllMessagesByUser(cp, user.Id)),
            "Messages were not deleted by user.");
        
        Testing.ShouldBeTrue(() => Database.Transaction(cp =>
                GetById(cp, expectedMessage2.Id) == null),
            "Message still exists after deletion by user.");
        
        // test case 2: check if all messages of a project were deleted automatically after deleting the user
        MessageEntry expectedMessageDeletedByUserDeletion = Database.Transaction(cp => CreateMessage(cp, project2.Id, user.Id, "textToDelete", timestamp)); 
        
        UserManager.DeleteUser(user.Id);
        
        MessageEntry? actualMessageDeletedByUserDeletion = Database.Transaction(cp => GetById(cp, expectedMessageDeletedByProjectDeletion.Id));

        if (actualMessageDeletedByUserDeletion == null) return;
        
        Testing.ShouldBeTrue(() =>
                actualMessageDeletedByProjectDeletion.Id == expectedMessageDeletedByUserDeletion.Id,
            "Messages were not deleted by user deletion.");
        
        // test case 3: check if all messages of a user remain when the user leaves the project
        var projectOld = Database.Transaction(cp => ProjectTable.CreateProject(cp, "projectOld", "projectOld for automated tests", 4));
        var projectNew = Database.Transaction(cp => ProjectTable.CreateProject(cp, "projectNew", "projectOld for automated tests", 4));
        
        // create new user because the first user was deleted within the test case 2
        var user2 = Database.Transaction(cp => UserTable.CreateUser(cp, "user2", "abcdefghiJ%!D"));
        
        MessageEntry messageProjectOld = Database.Transaction(cp => CreateMessage(cp, projectOld.Id, user2.Id, "textProjectOld", timestamp));
        
        Database.Transaction(cp => 
            AssignmentTable.CreateAssignment(cp, projectOld.Id, user2.Id, ProjectRole.Participant));
        
        // check if user is part of the old project at the beginning
        Testing.ShouldBeTrue(() => 
                Database.Transaction(cp => 
                    AssignmentTable.ListByProject(cp, projectOld.Id)
                        .Find(x => x.UserId == user2.Id)?.ProjectId == projectOld.Id),
            "User is not part of old project 'projectOld'.");
        
        // check if there are any written messages from the user in the old project
        var messsagesOfProjectOld = Database.Transaction(cp => ListByProject(cp, projectOld.Id));

        var foundMessageOfProjectOld = messsagesOfProjectOld.Find(m => m.UserId == user2.Id);
        
        Testing.ShouldBeTrue(() => 
                foundMessageOfProjectOld?.ProjectId == messageProjectOld.ProjectId &&
                foundMessageOfProjectOld?.UserId == messageProjectOld.UserId && 
                foundMessageOfProjectOld?.Id == messageProjectOld.Id,
            "User has not written any messages in old project 'projectOld'.");
        
        // assign user to another project and remove him from old one
        // check if assignment worked as expected
        Database.Transaction(cp => 
            AssignmentTable.CreateAssignment(cp, projectNew.Id, user2.Id, ProjectRole.Participant));
        
        Database.Transaction(cp => 
            AssignmentTable.DeleteAssignment(cp, projectOld.Id, user2.Id));
        
        Testing.ShouldBeTrue(() => 
                Database.Transaction(cp => 
                    AssignmentTable.ListByProject(cp, projectOld.Id)
                        .Find(x => x.UserId == user2.Id) == null),
            "User is still part of old project 'projectOld'.");
        
        Testing.ShouldBeTrue(() => 
                Database.Transaction(cp => 
                    AssignmentTable.ListByProject(cp, projectNew.Id)
                        .Find(x => x.UserId == user2.Id)?.ProjectId == projectNew.Id),
            "User is not part of new project 'projectNew'.");
        
        // simulate chatting in new project
        MessageEntry messageProjectNew = Database.Transaction(cp => CreateMessage(cp, projectNew.Id, user2.Id, "textProjectNew", timestamp));
        
        // check if there are any written messages from the user in the new project
        var messsagesOfProjectNew = Database.Transaction(cp => ListByProject(cp, projectNew.Id));
        var foundMessageOfProjectNew = messsagesOfProjectNew.Find(m => m.UserId == user2.Id);
        
        Testing.ShouldBeTrue(() => 
                foundMessageOfProjectNew?.ProjectId == messageProjectNew.ProjectId &&
                foundMessageOfProjectNew?.UserId == messageProjectNew.UserId && 
                foundMessageOfProjectNew?.Id == messageProjectNew.Id,
            "User has not written any messages in new project 'projectNew'.");
        
        // check the written messages from user are still in the old project after chatting in the new project
        messsagesOfProjectOld = Database.Transaction(cp => ListByProject(cp, projectOld.Id));

        foundMessageOfProjectOld = messsagesOfProjectOld.Find(m => m.UserId == user2.Id);
        
        Testing.ShouldBeTrue(() => 
                foundMessageOfProjectOld?.ProjectId == messageProjectOld.ProjectId &&
                foundMessageOfProjectOld?.UserId == messageProjectOld.UserId && 
                foundMessageOfProjectOld?.Id == messageProjectOld.Id,
            "User has no more messages in old project 'projectOld' after joining new project 'projectNew'.");
        
        #endregion
    }
}