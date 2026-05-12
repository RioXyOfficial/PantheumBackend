using MySqlConnector;

public static class SessionValidator
{
    public static async Task<bool> IsValidAsync(MySqlConnection conn, string? token)
    {
        if (string.IsNullOrEmpty(token)) return false;
        var cmd = new MySqlCommand(
            "SELECT COUNT(*) FROM users WHERE session_token=@t", conn);
        cmd.Parameters.AddWithValue("@t", token);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
    }

    public static async Task<int> GetUserIdFromTokenAsync(MySqlConnection conn, string? token)
    {
        if (string.IsNullOrEmpty(token)) return 0;
        var cmd = new MySqlCommand(
            "SELECT id FROM users WHERE session_token=@t", conn);
        cmd.Parameters.AddWithValue("@t", token);
        var result = await cmd.ExecuteScalarAsync();
        return result == null ? 0 : Convert.ToInt32(result);
    }
}
