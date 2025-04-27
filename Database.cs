using System.Data;
using cocoding.Data;
using MySqlConnector;

namespace cocoding;

/// <summary>
/// Delegate for database transaction functions with a return type.
/// </summary>
public delegate T DatabaseTransactionDelegate<T>(CommandProvider commandProvider);

/// <summary>
/// Database methods and constants.
/// </summary>
public static class Database
{
    /// <summary>
    /// Runs the given function with a new database connection and transaction, and returns the result.
    /// </summary>
    public static T Transaction<T>(DatabaseTransactionDelegate<T> function)
    {
        using MySqlConnection con = new(Global.Config.DatabaseConnection);
        con.Open();
        var tra = con.BeginTransaction();
        try
        {
            T result = function(new(con, tra));
            tra.Commit();
            return result;
        }
        catch
        {
            tra.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Runs the given function with a new database connection and transaction.
    /// </summary>
    public static void Transaction(Action<CommandProvider> function)
    {
        using MySqlConnection con = new(Global.Config.DatabaseConnection);
        con.Open();
        var tra = con.BeginTransaction();
        try
        {
            function(new(con, tra));
            tra.Commit();
        }
        catch
        {
            tra.Rollback();
            throw;
        }
    }
    
    /// <summary>
    /// Runs the given SQL command as a non-query command in a new transaction and returns whether any rows were affected.
    /// </summary>
    public static bool NonQuery(string commandText)
        => Transaction(cp => cp.CreateCommand(commandText).ExecuteNonQuery() > 0);
    
    /// <summary>
    /// Runs the given SQL command as a non-query command in a new transaction and returns whether any rows were affected.
    /// </summary>
    public static bool NonQuery(CommandProvider cp, string commandText)
        => cp.CreateCommand(commandText).ExecuteNonQuery() > 0;

    /// <summary>
    /// Attempts to connect to the database and outputs the result.<br/>
    /// If the connection fails, the causing exception will be written to the console.
    /// </summary>
    public static void TestConnection()
    {
        try
        {
            using MySqlConnection con = new(Global.Config.DatabaseConnection);
            con.Open();
            Console.WriteLine(con.State == ConnectionState.Open ? "Database connection successful." : $"Database connection failed! State: {con.State}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to connect to database: {ex.Message}\n{ex.StackTrace}");
        }
    }

    /// <summary>
    /// Upgrades all tables to the latest schema.
    /// </summary>
    public static void UpgradeTables(bool showMessages = true)
    {
        Transaction(cp =>
        {
            var dbVersion = GetVersion(cp);
            
            ApplyTableChanges(1, 0, 0, 0,
                "create table USER (UID int not null, USERNAME varchar(20) not null, PASSWORD binary(32), SALT binary(16), NAME varchar(50), ROLE int not null, key USERNAME_INDEX (USERNAME) using hash, primary key (UID));",
                "create table LOGIN (TOKEN char(64) not null, UID int not null, EXPIRATION bigint not null, key UID_INDEX (UID) using hash, key EXPIRATION_INDEX (EXPIRATION) using btree, primary key (TOKEN));"
            );
            
            ApplyTableChanges(2, 0, 0, 0,
                "create table PROJECT (PID int not null, NAME varchar(50) not null, DESCRIPTION varchar(200), key NAME_INDEX (NAME) using btree, primary key (PID));"
            );
            
            ApplyTableChanges(3, 0, 0, 0,
                "create table ASSIGNMENT (PID int not null, UID int not null, PROLE int not null, key PID_INDEX (PID) using hash, key UID_INDEX (UID) using hash, primary key (PID, UID));",
                "create table FILE (FID int not null, PID int not null, PARENT int, NAME varchar(50) not null, CHANGED bigint not null, key PROJECT_INDEX (PID) using hash, key FOLDER_INDEX (PARENT) using hash, key NAME_INDEX (NAME) using btree, primary key (FID));",
                "create table FOLDER (FID int not null, PID int not null, PARENT int, NAME varchar(50) not null, key PROJECT_INDEX (PID) using hash, key FOLDER_INDEX (PARENT) using hash, key NAME_INDEX (NAME) using btree, primary key (FID));"
            );
            
            ApplyTableChanges(5, 0, 0, 0,
                "create table VERSION (VID int not null, FID int not null, NAME varchar(50) not null, CHANGED bigint not null, UID int not null, key FILE_INDEX (FID) using hash, key NAME_INDEX (NAME) using btree, primary key (VID));",
                "alter table ASSIGNMENT add PINNED bool not null default false;"
            );
            
            ApplyTableChanges(6, 0, 0, 0,
                "alter table VERSION add (LABEL varchar(25) default null, COMMENT varchar(200) default null);",
                "create table SELECTION (SID int not null, FID int not null, CLIENT_START bigint not null, CLOCK_START bigint not null, CLIENT_END bigint not null, CLOCK_END bigint not null, key FILE_INDEX (FID), primary key (SID));",
                "create table MESSAGE (MID int not null, PID int not null, UID int not null, TEXT varchar(200) not null, TIMESTAMP bigint not null, SID int, key PROJECT_INDEX (PID) using hash, key TIMESTAMP_INDEX (TIMESTAMP) using btree, primary key (MID));"
            );
            
            ApplyTableChanges(7, 0, 0, 0,
                "alter table PROJECT add (INDENT int not null default 4);",
                "alter table FILE add (UID int not null);"
            );
            
            ApplyTableChanges(8, 0, 0, 0,
                "alter table USER add (THEME int not null default -1);",
                "alter table MESSAGE add (RESPONSE_TO int);"
            );
            
            SetVersion(cp, Global.Version);
            return;

            void ApplyTableChanges(int v1, int v2, int v3, int v4, params string[] commands)
            {
                if (dbVersion < new Version(v1, v2, v3, v4))
                {
                    if (showMessages)
                        Console.WriteLine($"Upgraded database to version v{v1}.{v2}.{v3}.{v4}.");
                    foreach (var command in commands)
                        NonQuery(cp, command);
                }
            }
        });
    }

    /// <summary>
    /// Returns the program version saved in the database.
    /// </summary>
    private static Version GetVersion(CommandProvider cp)
    {
        try
        {
            var cmd = cp.CreateCommand("select V1, V2, V3, V4 from DBV;");

            using var reader = cmd.ExecuteReader();

            if (reader.Read())
                return new(
                    reader.GetInt32(0),
                    reader.GetInt32(1),
                    reader.GetInt32(2),
                    reader.GetInt32(3)
                );
        }
        catch (MySqlException ex)
        {
            if (ex.ErrorCode == MySqlErrorCode.NoSuchTable)
                NonQuery(cp, "create table DBV (V1 int not null, V2 int not null, V3 int not null, V4 int not null, primary key (V1, V2, V3, V4));");
            else throw;
        }
        
        return new(0, 0, 0, 0);
    }

    /// <summary>
    /// Sets the program version saved in the database.
    /// </summary>
    private static void SetVersion(CommandProvider cp, Version version)
    {
        NonQuery(cp, "delete from DBV;");
        
        var cmd = cp.CreateCommand("insert into DBV (V1, V2, V3, V4) values (@V1, @V2, @V3, @V4);");
        cmd.Parameters.AddWithValue("@V1", version.Major);
        cmd.Parameters.AddWithValue("@V2", version.Minor);
        cmd.Parameters.AddWithValue("@V3", version.Build);
        cmd.Parameters.AddWithValue("@V4", version.Revision);

        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Returns a list of table names present in the current database.
    /// </summary>
    public static List<string> ListTables()
        => Transaction(cp =>
        {
            using var reader = cp.CreateCommand("show tables;").ExecuteReader();
            List<string> tables = [];
            while (reader.Read())
                tables.Add(reader.GetString(0));
            return tables;
        });
}
