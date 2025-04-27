namespace cocoding.Data;

using Components.Pages;

/// <summary>
/// Methods for message management.
/// </summary>
public static partial class MessageManager
{
    /// <summary>
    /// This event is called whenever a chat message was sent.<br/>
    /// Listeners receive the message object as a parameter.
    /// </summary>
    public static event Action<MessageEntry, Chat.ReceiveMode>? MessageSent;

    /// <summary>
    /// Calls the event indicating that the given message was sent.
    /// </summary>
    public static void ReportMessageSent(MessageEntry entry, Chat.ReceiveMode mode = Chat.ReceiveMode.Post)
        => MessageSent?.Invoke(entry, mode);

    /// <summary>
    /// Update selection field and remove old selection.
    /// </summary>
    public static void UpdateSelection(CommandProvider cp, MessageEntry message, int? selectionId)
    {
        if (message.SelectionId != null)
            SelectionTable.DeleteSelection(cp, (int)message.SelectionId);
        
        MessageTable.UpdateSelection(cp, message.Id, selectionId);
    }
    /// <summary>
    /// Delete the message with the given message ID.
    /// </summary>
    public static void DeleteMessage(CommandProvider cp, MessageEntry? message)
    {
        if (message == null)
            return;
        MessageTable.DeleteMessage(cp, message.Id);

        if (message.SelectionId == null)
            return;
        SelectionTable.DeleteSelection(cp, (int)message.SelectionId);
    }
    public static void DeleteMessage(CommandProvider cp, int messageId)
    {
        DeleteMessage(cp, MessageTable.GetById(cp, messageId));
    }
    /// <summary>
    /// Deletes all messages of a project with the given project ID.
    /// </summary>
    public static void DeleteAllMessagesByProject(CommandProvider cp, int projectId)
    {
        var messageList = MessageTable.ListByProject(cp, projectId);
        foreach (var message in messageList)
        {
            if (message.SelectionId != null)
                SelectionTable.DeleteSelection(cp, (int)message.SelectionId);
        }

        MessageTable.DeleteAllMessagesByProject(cp, projectId);
    }

    /// <summary>
    /// Deletes all messages of a user with the given userID.
    /// </summary>
    public static void DeleteAllMessagesByUser(CommandProvider cp, int userId)
    {
        var messageList = MessageTable.ListByUser(cp, userId);
        foreach (var message in messageList)
        {
            if (message.SelectionId != null)
                SelectionTable.DeleteSelection(cp, (int)message.SelectionId);
        }

        MessageTable.DeleteAllMessagesByUser(cp, userId);
    }
}