namespace cocoding.Data;

/// <summary>
/// Methods for the MESSAGE table.
/// </summary>
public static partial class MessageTable
{
    /// <summary>
    /// Returns the message with the given ID or null if no such message exists.
    /// </summary>
    public static MessageEntry? GetById(CommandProvider cp, int messageId)
    {
        var cmd = cp.CreateCommand("select PID, UID, TEXT, TIMESTAMP, SID, RESPONSE_TO from MESSAGE where MID = @MID;");
        cmd.Parameters.AddWithValue("@MID", messageId);

        using var reader = cmd.ExecuteReader();

        if (!reader.Read())
            return null;
        
        int projectId = reader.GetInt32(0);
        int userId = reader.GetInt32(1);
        string text = reader.GetString(2);
        DateTime timestamp = new(reader.GetInt64(3));
        int? selectionId = reader.GetNullableInt32(4);
        int? responseTo = reader.GetNullableInt32(5);

        return new(messageId, projectId, userId, text, timestamp, selectionId, responseTo);
    }

    /// <summary>
    /// Update the text field of the given message.
    /// </summary>
    public static void UpdateText(CommandProvider cp, int messageId, string text)
    {
        var cmd = cp.CreateCommand("UPDATE MESSAGE SET TEXT = @TEXT WHERE MID = @MID;");
        
        cmd.Parameters.AddWithValue("@MID", messageId);
        cmd.Parameters.AddWithValue("@TEXT", text);

        using var reader = cmd.ExecuteReader(); 
    }
    
    /// <summary>
    /// Update the selection field of the given message.
    /// !!!DO NOT USE THIS METHOD DIRECTLY BUT USE THE METHOD IN MESSAGE_MANAGER INSTEAD!!!
    /// </summary>
    public static void UpdateSelection(CommandProvider cp, int messageId, int? selectionId)
    {
        var cmd = cp.CreateCommand("UPDATE MESSAGE SET SID = @SID WHERE MID = @MID;");
        
        cmd.Parameters.AddWithValue("@MID", messageId);
        cmd.Parameters.AddWithValue("@SID", selectionId);

        using var reader = cmd.ExecuteReader(); 
    }
    
    /// <summary>
    /// Update the response to field of the given message.
    /// </summary>
    public static void UpdateResponseTo(CommandProvider cp, int messageId, int? responseTo)
    {
        var cmd = cp.CreateCommand("UPDATE MESSAGE SET RESPONSE_TO = @RESPONSE_TO WHERE MID = @MID;");
        
        cmd.Parameters.AddWithValue("@MID", messageId);
        cmd.Parameters.AddWithValue("@RESPONSE_TO", responseTo);

        using var reader = cmd.ExecuteReader(); 
    }
    
    /// <summary>
    /// Lists all messages of a project with the given project.
    /// </summary>
    public static List<MessageEntry> ListByProject(CommandProvider cp, int projectId)
    {
        List<MessageEntry> result = [];
        
        var cmd = cp.CreateCommand("select MID, UID, TEXT, TIMESTAMP, SID, RESPONSE_TO from MESSAGE where PID = @PID;");
        cmd.Parameters.AddWithValue("@PID", projectId);

        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            int messageId = reader.GetInt32(0);
            int userId = reader.GetInt32(1);
            string text = reader.GetString(2);
            DateTime timestamp = new(reader.GetInt64(3));
            int? selectionId = reader.GetNullableInt32(4);
            int? responseTo = reader.GetNullableInt32(5);

            result.Add(new(messageId, projectId, userId, text, timestamp, selectionId, responseTo));
        }

        return result;
    }
    
    /// <summary>
    /// Lists all messages of a user with the given user id.
    /// </summary>
    public static List<MessageEntry> ListByUser(CommandProvider cp, int userId)
    {
        List<MessageEntry> result = [];
        
        var cmd = cp.CreateCommand("select MID, PID, TEXT, TIMESTAMP, SID, RESPONSE_TO from MESSAGE where UID = @UID;");
        cmd.Parameters.AddWithValue("@UID", userId);

        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            int messageId = reader.GetInt32(0);
            int projectId = reader.GetInt32(1);
            string text = reader.GetString(2);
            DateTime timestamp = new(reader.GetInt64(3));
            int? selectionId = reader.GetNullableInt32(4);
            int? responseTo = reader.GetNullableInt32(5);

            result.Add(new(messageId, projectId, userId, text, timestamp, selectionId, responseTo));
        }

        return result;
    }
    
    /// <summary>
    /// Lists the last five messages of a project with the given project.
    /// </summary>
    public static List<MessageEntry> ListLastFiveMessagesByProject(CommandProvider cp, int projectId)
    {
        List<MessageEntry> result = [];
        
        var cmd = cp.CreateCommand("select MID, UID, TEXT, TIMESTAMP, SID, RESPONSE_TO from MESSAGE where PID = @PID order by TIMESTAMP desc limit 5;");
        cmd.Parameters.AddWithValue("@PID", projectId);

        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            int messageId = reader.GetInt32(0);
            int userId = reader.GetInt32(1);
            string text = reader.GetString(2);
            DateTime timestamp = new(reader.GetInt64(3));
            int? selectionId = reader.GetNullableInt32(4);
            int? responseTo = reader.GetNullableInt32(5);

            result.Add(new(messageId, projectId, userId, text, timestamp, selectionId, responseTo));
        }

        return result;
    }

    
    /// <summary>
    /// Lists five older messages of a project with the given project.
    /// </summary>
    public static List<MessageEntry> ListOlderMessages(CommandProvider cp, int projectId, DateTime olderThan)
    {
        List<MessageEntry> result = [];
        
        var cmd = cp.CreateCommand("select MID, UID, TEXT, TIMESTAMP, SID, RESPONSE_TO from MESSAGE where PID = @PID and TIMESTAMP < @DATE order by TIMESTAMP desc limit 5;");
        cmd.Parameters.AddWithValue("@PID", projectId);
        cmd.Parameters.AddWithValue("@DATE", olderThan.Ticks);

        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            int messageId = reader.GetInt32(0);
            int userId = reader.GetInt32(1);
            string text = reader.GetString(2);
            DateTime timestamp = new(reader.GetInt64(3));
            int? selectionId = reader.GetNullableInt32(4);
            int? responseTo = reader.GetNullableInt32(5);

            result.Add(new(messageId, projectId, userId, text, timestamp, selectionId, responseTo));
        }

        return result;
    }
    
    /// <summary>
    /// Lists all messages that are younger/newer then the clicked response but older
    /// than the latest message that is already loaded in the chat so that we don't load already
    /// loaded messages again.
    /// </summary>
    public static List<MessageEntry> ListMessagesUntilClickedResponse(CommandProvider cp, int projectId,
        DateTime? youngerThan, DateTime? olderThan)
    {
        List<MessageEntry> result = [];
        
        var cmd = cp.CreateCommand("select MID, UID, TEXT, TIMESTAMP, SID, RESPONSE_TO from MESSAGE " + 
                                   "where PID = @PID and TIMESTAMP >= @YOUNGERTHANDATE and TIMESTAMP < @OLDERTHANDATE order by TIMESTAMP desc;");
        cmd.Parameters.AddWithValue("@PID", projectId);
        cmd.Parameters.AddWithValue("@YOUNGERTHANDATE", youngerThan?.Ticks);
        cmd.Parameters.AddWithValue("@OLDERTHANDATE", olderThan?.Ticks);

        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            int messageId = reader.GetInt32(0);
            int userId = reader.GetInt32(1);
            string text = reader.GetString(2);
            DateTime timestamp = new(reader.GetInt64(3));
            int? selectionId = reader.GetNullableInt32(4);
            int? responseTo = reader.GetNullableInt32(5);

            result.Add(new(messageId, projectId, userId, text, timestamp, selectionId, responseTo));
        }

        return result;
    }
    
    /// <summary>
    /// Creates a new message with a unique ID using the given user ID, project ID, text, timestamp and selection ID.
    /// </summary>
    public static MessageEntry CreateMessage(CommandProvider cp, int projectId, int userId, string text, DateTime timestamp, int? selectionId = null, int? responseTo = null)
    {
        int messageId;
        do messageId = Security.RandomInt();
        while (GetById(cp, messageId) != null);

        var cmd = cp.CreateCommand("insert into MESSAGE (MID, PID, UID, TEXT, TIMESTAMP, SID, RESPONSE_TO) values (@MID, @PID, @UID, @TEXT, @TIMESTAMP, @SID, @RESPONSE_TO);");
        cmd.Parameters.AddWithValue("@MID", messageId);
        cmd.Parameters.AddWithValue("@PID", projectId);
        cmd.Parameters.AddWithValue("@UID", userId);
        cmd.Parameters.AddWithValue("@TEXT", text);
        cmd.Parameters.AddWithValue("@TIMESTAMP", timestamp.Ticks);
        cmd.Parameters.AddWithValue("@SID", selectionId);
        cmd.Parameters.AddWithValue("@RESPONSE_TO", responseTo);

        cmd.ExecuteNonQuery();

        return new(messageId, projectId, userId, text, timestamp, selectionId, responseTo);
    }

    /// <summary>
    /// Deletes the message with the given message ID.
    /// !!!DO NOT USE THIS METHOD DIRECTLY BUT USE THE METHOD IN MESSAGE_MANAGER INSTEAD!!!
    /// </summary>
    public static bool DeleteMessage(CommandProvider cp, int messageId)
    {
        var cmd = cp.CreateCommand("delete from MESSAGE where MID = @MID;");
        cmd.Parameters.AddWithValue("@MID", messageId);

        return cmd.ExecuteNonQuery() > 0;
    }
    
    /// <summary>
    /// Deletes all messages of a project with the given project ID.
    /// !!!DO NOT USE THIS METHOD DIRECTLY BUT USE THE METHOD IN MESSAGE_MANAGER INSTEAD!!!
    /// </summary>
    public static bool DeleteAllMessagesByProject(CommandProvider cp, int projectId)
    {
        var cmd = cp.CreateCommand("delete from MESSAGE where PID = @PID;");
        cmd.Parameters.AddWithValue("@PID", projectId);

        return cmd.ExecuteNonQuery() > 0;
    }
    
    /// <summary>
    /// Deletes all sent messages of a user with the given user ID.
    /// !!!DO NOT USE THIS METHOD DIRECTLY BUT USE THE METHOD IN MESSAGE_MANAGER INSTEAD!!!
    /// </summary>
    public static bool DeleteAllMessagesByUser(CommandProvider cp, int userId)
    {
        var cmd = cp.CreateCommand("delete from MESSAGE where UID = @UID;");
        cmd.Parameters.AddWithValue("@UID", userId);

       return cmd.ExecuteNonQuery() > 0;
    }
    
}