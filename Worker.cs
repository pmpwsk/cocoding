namespace cocoding;

/// <summary>
/// Worker to perform tasks regularly.
/// </summary>
public static class Worker
{
    public static Task Work()
    {
        // delete expired tokens
        Database.Transaction(cp =>
        {
            var cmd = cp.CreateCommand("delete from LOGIN where EXPIRATION < @EXPIRATION;");
            cmd.Parameters.AddWithValue("@EXPIRATION", DateTime.UtcNow.Ticks);
            cmd.ExecuteNonQuery();
        });

        // delete tokens if too many
        var userIds = Database.Transaction(cp =>
        {
            var cmd = cp.CreateCommand("select UID from USER;");
            using var reader = cmd.ExecuteReader();
            List<int> result = [];
            while (reader.Read())
                result.Add(reader.GetInt32(0));
            return result;
        });
        foreach (int userId in userIds)
        {
            var deleteUntil = Database.Transaction(cp =>
            {
                var cmd = cp.CreateCommand("select EXPIRATION from LOGIN where UID = @UID and (EXPIRATION < @EXPIRATION1 or EXPIRATION > @EXPIRATION2) order by EXPIRATION desc;");
                cmd.Parameters.AddWithValue("@UID", userId);
                cmd.Parameters.AddWithValue("@EXPIRATION1", DateTime.UtcNow);
                cmd.Parameters.AddWithValue("@EXPIRATION2", DateTime.UtcNow.AddMinutes(5));
                using var reader = cmd.ExecuteReader();
                for (int i = 0; i <= 20; i++)
                    if (!reader.Read())
                        return default;
                return new DateTime(reader.GetInt64(0));
            });
            if (deleteUntil != default)
                Database.Transaction(cp =>
                {
                    var cmd = cp.CreateCommand("delete from LOGIN where UID = @UID and EXPIRATION <= @EXPIRATION;");
                    cmd.Parameters.AddWithValue("@UID", userId);
                    cmd.Parameters.AddWithValue("@EXPIRATION", deleteUntil.Ticks);
                    cmd.ExecuteNonQuery();
                });
        }

        // persist editor states
        foreach (var fileGroup in EditorHub.FileGroups.Values)
            fileGroup.Persist();
        
        // delete loose ends
        NonQueryWithMessage("Deleted some logins without users.",
            "delete from LOGIN where not exists (select 1 from USER U where U.UID = LOGIN.UID);");
        NonQueryWithMessage("Deleted some projects without owner assignments.",
            "delete from PROJECT where not exists (select 1 from ASSIGNMENT A where A.PID = PROJECT.PID and A.PROLE = 2147483647);");
        NonQueryWithMessage("Deleted some assignments without users.",
            "delete from ASSIGNMENT where not exists (select 1 from USER U where U.UID = ASSIGNMENT.UID);");
        NonQueryWithMessage("Deleted some assignments without projects.",
            "delete from ASSIGNMENT where not exists (select 1 from PROJECT P where P.PID = ASSIGNMENT.PID);");
        NonQueryWithMessage("Deleted some folders without projects.",
            "delete from FOLDER where not exists (select 1 from PROJECT P where P.PID = FOLDER.PID);");
        NonQueryWithMessage("Deleted some folders without parent folders.",
            "delete from FOLDER where PARENT is not null and not exists (select 1 from FOLDER F where F.FID = FOLDER.PARENT);");
        NonQueryWithMessage("Deleted some files without projects.",
            "delete from FILE where not exists (select 1 from PROJECT P where P.PID = FILE.PID);");
        NonQueryWithMessage("Deleted some files without parent folders.",
            "delete from FILE where PARENT is not null and not exists (select 1 from FOLDER F where F.FID = FILE.PARENT);");
        NonQueryWithMessage("Deleted some versions without files",
            "delete from VERSION where not exists (select 1 from FILE F where F.FID = VERSION.FID);");
        NonQueryWithMessage("Deleted some selections without messages",
                "delete from SELECTION where not exists (select 1 from MESSAGE M where M.SID = SELECTION.SID);");
        NonQueryWithMessage("Deleted some selections without files",
            "delete from SELECTION where not exists (select 1 from FILE F where F.FID = SELECTION.FID);");
        NonQueryWithMessage("Deleted some messages without project",
            "delete from MESSAGE where not exists (select 1 from PROJECT P where P.PID = MESSAGE.PID);");
        NonQueryWithMessage("Deleted some messages without user",
            "delete from MESSAGE where not exists (select 1 from USER U where U.UID = MESSAGE.UID);");
        HashSet<int> dbFileIds = [];
        Database.Transaction(cp =>
        {
            var cmd = cp.CreateCommand("SELECT FID from FILE;");
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                dbFileIds.Add(reader.GetInt32(0));
        });
        HashSet<int> dbVersionFileIds = [];
        Database.Transaction(cp =>
        {
            var cmd = cp.CreateCommand("SELECT VID from VERSION;");
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                dbVersionFileIds.Add(reader.GetInt32(0));
        });
        bool deletedStateFiles = false;
        bool deletedVersionStateFiles = false;
        foreach (var file in new DirectoryInfo("../FileStates").GetFiles("*.bin", SearchOption.TopDirectoryOnly))
        {
            if (!file.Name.Contains('_'))
            {
                if (int.TryParse(file.Name[..^4], out var fileId) && !dbFileIds.Remove(fileId))
                {
                    file.Delete();
                    deletedStateFiles = true;
                }
            }
            else
            {
                int underscoreIndex = file.Name.IndexOf('_');
                string versionIdString = file.Name.Substring(underscoreIndex + 1, file.Name.Length - underscoreIndex - 1);
                
                if (int.TryParse(versionIdString[..^4], out var versionId) && !dbVersionFileIds.Remove(versionId))
                {
                    file.Delete();
                    deletedVersionStateFiles = true;
                }
            }

        }

        if (deletedStateFiles)
            Console.WriteLine("Deleted some file states without files.");
        if (dbFileIds.Count > 0)
            NonQueryWithMessage("Deleted some files without file states.",
                $"delete from FILE where FID in ({string.Join(", ", dbFileIds)});");
        if (deletedVersionStateFiles)
            Console.WriteLine("Deleted some version file states without versions.");
        if (dbFileIds.Count > 0)
            NonQueryWithMessage("Deleted some versions without file states.",
                $"delete from FILE where FID in ({string.Join(", ", dbFileIds)});");
        
        return Task.CompletedTask;
    }

    private static void NonQueryWithMessage(string message, string commandText)
    {
        if (Database.NonQuery(commandText))
            Console.WriteLine(message);
    }
}