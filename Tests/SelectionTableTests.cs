using cocoding.Tests;

namespace cocoding.Data;

public static partial class SelectionTable {
    /// <summary>
    /// Tests the SelectionTable class.
    /// </summary>
    internal static void Test()
    {

        var project = Database.Transaction(cp => ProjectTable.CreateProject(cp, "project", null, 4));
        var file = Database.Transaction(cp => FileTable.CreateFile(cp, project.Id, null, "file1", DateTime.UtcNow, 0));
        var fileNoSelection = Database.Transaction(cp => FileTable.CreateFile(cp, project.Id, null, "file1", DateTime.UtcNow, 0));
        SelectionEntry selection;
        
        #region CreateSelection 
        
        selection = Database.Transaction(cp => CreateSelection(cp, file.Id, 0, 1, 2, 3));

        
        
        #endregion
        
        int wrongSelectionId = 0;
        
        while (selection.SelectionId == wrongSelectionId) 
            wrongSelectionId++;
        
        #region GetById

        var selectionById = Database.Transaction(cp => GetById(cp, selection.SelectionId));
        
        Testing.ShouldBeTrue(()=>
            selectionById != null,
            "GetById was not able to fetch SelectionEntry.");
        
        Testing.ShouldBeTrue(()=>
            selectionById?.SelectionId == selection.SelectionId,
            "GetById returned false SelectionEntry.");
        
        Testing.ShouldBeTrue(()=>
            selectionById?.FileId == file.Id,
            "GetByIds returned SelectionEntry's file id is wrong. Data corrupted.");
        
        Testing.ShouldBeTrue(()=>
            selectionById?.ClientStart == 0,
            "GetByIds returned SelectionEntry's ClientStart is wrong. Data corrupted.");
        
        Testing.ShouldBeTrue(()=>
            selectionById?.ClockStart == 1,
            "GetByIds returned SelectionEntry's ClockStart is wrong. Data corrupted.");

        Testing.ShouldBeTrue(()=>
                selectionById?.ClientEnd == 2,
            "GetByIds returned SelectionEntry's ClientEnd is wrong. Data corrupted.");
        
        Testing.ShouldBeTrue(()=>
                selectionById?.ClockEnd == 3,
            "GetByIds returned SelectionEntry's ClockEnd is wrong. Data corrupted.");
        
        var wrongIdQuery = Database.Transaction(cp => GetById(cp, wrongSelectionId));
        
        Testing.ShouldBeTrue(()=>
            wrongIdQuery == null,
            "GetByIds returned SelectionEntry for arbitrary id."
            );
        
        #endregion
        
        #region ListSelectionsByFile

        var selectionList = Database.Transaction(cp => ListSelectionsByFile(cp, file.Id));

        Testing.ShouldBeTrue(() => 
                selectionList.Count == 1,
        "ListSelectionsByFile returns a list with wrong number of elements. Expected: 1, returned: " +
            selectionList.Count); 

        var selectionListEmpty = Database.Transaction(cp => ListSelectionsByFile(cp, fileNoSelection.Id));

        Testing.ShouldBeTrue(() =>
            selectionListEmpty.Count == 0, 
                "ListSelectionsByFile returns non empty list.");
        
        #endregion

        #region UpdateSelection
        
        Database.Transaction(cp => UpdateSelection(cp, selection.SelectionId, 4, 5, 6, 7));
        
        var selectionUpdated = Database.Transaction(cp => GetById(cp, selection.SelectionId));
        
        Testing.ShouldBeTrue(()=>
                selectionUpdated != null,
            "GetById was not able to fetch SelectionEntry after update.");
        
        Testing.ShouldBeTrue(()=>
                selectionUpdated?.SelectionId == selection.SelectionId,
            "GetById returned false SelectionEntry after update.");
        
        Testing.ShouldBeTrue(()=>
                selectionUpdated?.FileId == file.Id,
            "GetByIds returned SelectionEntry's file id is wrong after update. Data corrupted.");
        
        Testing.ShouldBeTrue(()=>
                selectionUpdated?.ClientStart == 4,
            "GetByIds returned SelectionEntry's ClientStart is wrong  after update. Data corrupted.");
        
        Testing.ShouldBeTrue(()=>
                selectionUpdated?.ClockStart == 5,
            "GetByIds returned SelectionEntry's ClockStart is wrong  after update. Data corrupted.");

        Testing.ShouldBeTrue(()=>
                selectionUpdated?.ClientEnd == 6,
            "GetByIds returned SelectionEntry's ClientEnd is wrong after update. Data corrupted.");
        
        Testing.ShouldBeTrue(()=>
                selectionUpdated?.ClockEnd == 7,
            "GetByIds returned SelectionEntry's ClockEnd is wrong after update. Data corrupted.");

        
        
        #endregion
        
        #region DeleteVersion

        Database.Transaction(cp=>DeleteSelection(cp, selection.SelectionId));

        var selectionDeleted = Database.Transaction(cp => GetById(cp, selection.SelectionId)); 
        
        Testing.ShouldBeTrue(()=>
            selectionDeleted == null,
            "DeleteSelection did not delete the selection.");
        
        #endregion

    }
}
