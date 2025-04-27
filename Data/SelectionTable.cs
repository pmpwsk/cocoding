namespace cocoding.Data;

/// <summary>
/// Methods for the SELECTION table.
/// </summary>
public static partial class SelectionTable
{
    /// <summary>
    /// Creates a new selection with a unique ID using the given file id, client and clock id 
    /// of the selection start and the selection end (see documentation Yjs -> RelativePosition).  
    /// </summary>
    public static SelectionEntry CreateSelection(CommandProvider cp, int fileId, double clientStart, double clockStart, double clientEnd, double clockEnd)
    {
        int selectionId; 
        // Generate a unique selection ID
        do selectionId = Security.RandomInt(); // Generate a random integer for selection ID
        // Ensure that the generated ID does not already exist in the database
        while (GetById(cp, selectionId) != null);

        // Prepare SQL command to insert a new selection into the SELECTION table
        var cmd = cp.CreateCommand("insert into SELECTION (SID, FID, CLIENT_START, CLOCK_START, CLIENT_END, CLOCK_END) values (@SID, @FID, @CLIENT_START, @CLOCK_START, @CLIENT_END, @CLOCK_END);");
        // Add parameters to the command
        cmd.Parameters.AddWithValue("@SID", selectionId);
        cmd.Parameters.AddWithValue("@FID", fileId);
        cmd.Parameters.AddWithValue("@CLIENT_START", clientStart);
        cmd.Parameters.AddWithValue("@CLOCK_START", clockStart);
        cmd.Parameters.AddWithValue("@CLIENT_END", clientEnd);
        cmd.Parameters.AddWithValue("@CLOCK_END", clockEnd);

        // Execute the command to insert the new selection
        cmd.ExecuteNonQuery();

        // Return a new SelectionEntry object with the provided parameters
        return new(selectionId, fileId, clientStart, clockStart, clientEnd, clockEnd);
    }

    /// <summary>
    /// Lists all selections for the given file.
    /// </summary>
    public static List<SelectionEntry> ListSelectionsByFile(CommandProvider cp, int fileIdToCheck)
    {
        List<SelectionEntry> result = new(); // Initialize a list to hold selection entries

        // Create SQL command 
        var cmd = cp.CreateCommand("SELECT * FROM SELECTION WHERE FID = @FID;");
        cmd.Parameters.AddWithValue("@FID", fileIdToCheck);

        // Execute the command and read the results
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            // Read selection data from the database
            var selectionId = reader.GetInt32(0); // SID
            var fileId = reader.GetInt32(1); // FID
            var clientStart = reader.GetInt64(2); // CLIENT_START
            var clockStart = reader.GetInt64(3); // CLOCK_START
            var clientEnd = reader.GetInt64(4); // CLIENT_END
            var clockEnd = reader.GetInt64(5); // CLOCK_END

            // Add a new SelectionEntry to the result list
            result.Add(new SelectionEntry(selectionId, fileId, clientStart, clockStart, clientEnd, clockEnd));
        }

        return result; 
    }

    /// <summary>
    /// Updates an existing selection with new values.
    /// </summary>
    public static void UpdateSelection(CommandProvider cp, int selectionId, double clientStart, double clockStart, double clientEnd, double clockEnd)
    {
        // Prepare SQL command to update the selection in the SELECTION table
        var cmd = cp.CreateCommand("UPDATE SELECTION SET CLIENT_START = @CLIENT_START, CLOCK_START = @CLOCK_START, CLIENT_END = @CLIENT_END, CLOCK_END = @CLOCK_END WHERE SID = @SID;");
        // Add parameters to the command
        cmd.Parameters.AddWithValue("@SID", selectionId);
        cmd.Parameters.AddWithValue("@CLIENT_START", clientStart);
        cmd.Parameters.AddWithValue("@CLOCK_START", clockStart);
        cmd.Parameters.AddWithValue("@CLIENT_END", clientEnd);
        cmd.Parameters.AddWithValue("@CLOCK_END", clockEnd);

        // Execute the command to update the selection
        using var reader = cmd.ExecuteReader(); 
    }

    /// <summary>
    /// Retrieves a selection entry by its unique ID.
    /// </summary>
    public static SelectionEntry? GetById(CommandProvider cp, int selectionId)
    {
        // Prepare SQL command to select a specific entry from the SELECTION table
        var cmd = cp.CreateCommand("SELECT * FROM SELECTION WHERE SID = @SID;");
        cmd.Parameters.AddWithValue("@SID", selectionId);

        using var reader = cmd.ExecuteReader();
        if (reader.Read()) // If a record is found
        {
            // Read selection data from the database
            var fileId = reader.GetInt32(1);
            var clientStart = reader.GetInt64(2);
            var clockStart = reader.GetInt64(3);
            var clientEnd = reader.GetInt64(4);
            var clockEnd = reader.GetInt64(5);

            // Return a new SelectionEntry object with the retrieved data
            return new SelectionEntry(selectionId, fileId, clientStart, clockStart, clientEnd, clockEnd);
        }

        return null; // Return null if no record was found
    }

    /// <summary>
    /// Deletes the selection with the given ID.
    /// </summary>
    public static void DeleteSelection(CommandProvider cp, int selectionId)
    {
        // Retrieve the selection entry to ensure it exists
        var entry = GetById(cp, selectionId);

        if (entry == null) // If no entry is found, exit the method
            return;

        // Prepare SQL command to delete the selection from the SELECTION table
        var cmd = cp.CreateCommand("delete from SELECTION where SID = @SID;");
        cmd.Parameters.AddWithValue("@SID", selectionId);

        // Execute the command to delete the selection
        cmd.ExecuteNonQuery();
    }
}
