using MySqlConnector;

public class DatabaseHelper
{
    private readonly string _connectionString;

    public DatabaseHelper(string? connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public MySqlConnection GetConnection()
    {
        return new MySqlConnection(_connectionString);
    }
    
}