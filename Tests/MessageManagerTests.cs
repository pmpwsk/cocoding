using cocoding.Tests;

namespace cocoding.Data;

public static partial class MessageManager
{
    internal static void Test()
    {
        
        var project = Database.Transaction(cp => ProjectTable.CreateProject(cp, "project", "project for automated tests", 4));
        var user = Database.Transaction(cp => UserTable.CreateUser(cp, "user", "abcdefghiJ%!D"));
        var file = Database.Transaction(cp => FileTable.CreateFile(cp, project.Id, null, "string", DateTime.UtcNow, user.Id));
        
        #region UpdateSelectin && DeleteMessage
        Database.Transaction(cp => {
            var selection = SelectionTable.CreateSelection(cp, file.Id, 0, 1, 2, 3);
            var messageWithSelection = MessageTable.CreateMessage(cp, project.Id, user.Id, "string", DateTime.UtcNow, selection.SelectionId);

            var newSelection = SelectionTable.CreateSelection(cp, file.Id, 0, 1, 2, 3);
            
            UpdateSelection(cp, messageWithSelection, newSelection.SelectionId);
            
            UpdateSelection(cp, messageWithSelection, newSelection.SelectionId);
            Testing.ShouldBeTrue(() =>
                SelectionTable.GetById(cp, selection.SelectionId) == null,
                "UpdateSelection did not delete old selection."
            );
            
            DeleteMessage(cp, messageWithSelection.Id);
            Testing.ShouldBeTrue(() =>
                SelectionTable.GetById(cp, newSelection.SelectionId) == null,
                "DeleteMessage did not delete selection."
            );
        });

        #endregion
        
        #region DeleteAllMessagesByProject
        var projectSelection = Database.Transaction(cp => SelectionTable.CreateSelection(cp, file.Id, 0, 1, 2, 3));
        var projectMessage = Database.Transaction(cp => MessageTable.CreateMessage(cp, project.Id,user.Id, "string", DateTime.UtcNow, projectSelection.SelectionId));
        
        Database.Transaction(cp => DeleteAllMessagesByProject(cp, project.Id));
            Testing.ShouldBeTrue(()=>
                Database.Transaction(cp => SelectionTable.GetById(cp, projectSelection.SelectionId)) == null, 
                "DeleteAllMessagesByProject did not delete selection."
                );
        #endregion
        
        #region DeleteAllMessagesByUser
        var userSelection = Database.Transaction(cp => SelectionTable.CreateSelection(cp, file.Id, 0, 1, 2, 3));
        var userMessage = Database.Transaction(cp => MessageTable.CreateMessage(cp, project.Id,user.Id, "string", DateTime.UtcNow, userSelection.SelectionId));

        Database.Transaction(cp => DeleteAllMessagesByUser(cp, user.Id));
        Testing.ShouldBeTrue(()=>
                Database.Transaction(cp => SelectionTable.GetById(cp, userSelection.SelectionId)) == null, 
            "DeleteAllMessagesByUser did not delete selection."
        );
        
        #endregion

    
    }
}