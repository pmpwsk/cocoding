using cocoding.Data;

namespace cocoding.Tests;

/// <summary>
/// Helper class for tests.
/// </summary>
public static class Testing
{
    /// <summary>
    /// Runs all tests and throws exceptions for failures.
    /// </summary>
    public static void RunAll()
    {
        Global.Config = new()
        {
            DatabaseConnection = "server=127.0.0.1; database=cocoding_test; user=cocoding; password=cocoding"
        };

        foreach (var table in Database.ListTables())
            Database.NonQuery($"drop table {table};");
        Database.UpgradeTables(false);
        
        ClearDatabase();
        UserTable.Test();
        
        ClearDatabase();
        LoginTable.Test();
        
        ClearDatabase();
        UserManager.Test();
        
        ClearDatabase();
        ProjectTable.Test();
        
        ClearDatabase();
        AssignmentTable.Test();
        
        ClearDatabase();
        FolderTable.Test();
        
        ClearDatabase();
        FileTable.Test();
        
        ClearDatabase();
        FileManager.Test();
        
        ClearDatabase();
        VersionTable.Test();
        
        ClearDatabase();
        ProjectManager.Test();
        
        ClearDatabase();
        SelectionTable.Test(); 
        
        ClearDatabase();
        MessageTable.Test();
        
        ClearDatabase();
        MessageManager.Test();
        
        ClearDatabase();
    }

    /// <summary>
    /// Deletes all rows of all tested tables in the database.
    /// </summary>
    public static void ClearDatabase()
    {
        foreach (var table in Database.ListTables().Where(table => table != "DBV"))
            Database.NonQuery($"delete from {table};");
    }

    /// <summary>
    /// Throws a <c>TestFailedException</c> if the given function doesn't return <c>true</c>.
    /// </summary>
    public static void ShouldBeTrue(Func<bool> function, string description)
    {
        if (!function())
            throw new TestFailedException(description);
    }

    /// <summary>
    /// Throws a <c>TestFailedException</c> if the given function doesn't cause an exception.
    /// </summary>
    public static void ShouldFail(Action function, string description)
    {
        try
        {
            function();
            throw new TestFailedException(description);
        }
        catch (TestFailedException)
        {
            throw;
        }
        catch { }
    }
}