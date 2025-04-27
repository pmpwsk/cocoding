using MySqlConnector;

namespace cocoding.Data;

/// <summary>
/// Creates commands for a given MySQL connection and transaction.
/// </summary>
public class CommandProvider(MySqlConnection connection, MySqlTransaction transaction)
{
    private readonly MySqlConnection Connection = connection;

    private readonly MySqlTransaction Transaction = transaction;

    /// <summary>
    /// Creates a command for a given MySQL connection and transaction using the given command text.
    /// </summary>
    public MySqlCommand CreateCommand(string commandText)
        => new()
        {
            Connection = Connection,
            Transaction = Transaction,
            CommandText = commandText
        };
}