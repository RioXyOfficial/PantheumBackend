using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly DatabaseHelper _db;

    public AuthController(DatabaseHelper db) => _db = db;

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        var check = new MySqlCommand("SELECT COUNT(*) FROM users WHERE username=@u OR email=@e", conn);
        check.Parameters.AddWithValue("@u", req.Username);
        check.Parameters.AddWithValue("@e", req.Email);
        if (Convert.ToInt32(await check.ExecuteScalarAsync()) > 0)
            return BadRequest("Utilisateur ou email déjà existant.");

        string hash = BCrypt.Net.BCrypt.HashPassword(req.Password);
        var cmd = new MySqlCommand("INSERT INTO users (username, email, password_hash) VALUES (@u, @e, @p)", conn);
        cmd.Parameters.AddWithValue("@u", req.Username);
        cmd.Parameters.AddWithValue("@e", req.Email);
        cmd.Parameters.AddWithValue("@p", hash);
        await cmd.ExecuteNonQueryAsync();

        return Ok("Inscription réussie.");
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        int userId;
        string hash;

        var selectCmd = new MySqlCommand("SELECT id, password_hash FROM users WHERE username=@u", conn);
        selectCmd.Parameters.AddWithValue("@u", req.Username);
        using (var reader = await selectCmd.ExecuteReaderAsync())
        {
            if (!await reader.ReadAsync()) return Unauthorized("Utilisateur introuvable.");
            userId = reader.GetInt32("id");
            hash   = reader.GetString("password_hash");
        }

        if (!BCrypt.Net.BCrypt.Verify(req.Password, hash))
            return Unauthorized("Mot de passe incorrect.");

        string token = Guid.NewGuid().ToString("N");
        var updateCmd = new MySqlCommand("UPDATE users SET session_token=@t WHERE id=@id", conn);
        updateCmd.Parameters.AddWithValue("@t", token);
        updateCmd.Parameters.AddWithValue("@id", userId);
        await updateCmd.ExecuteNonQueryAsync();

        await SessionSocketManager.KickAsync(userId);

        return Ok(new { userId, username = req.Username, sessionToken = token });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        string? token = Request.Headers["X-Session-Token"];
        if (string.IsNullOrEmpty(token)) return Ok();

        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        var cmd = new MySqlCommand("UPDATE users SET session_token=NULL WHERE session_token=@t", conn);
        cmd.Parameters.AddWithValue("@t", token);
        await cmd.ExecuteNonQueryAsync();

        return Ok();
    }

    [HttpPost("rename")]
    public async Task<IActionResult> Rename([FromBody] RenameRequest req)
    {
        string? token = Request.Headers["X-Session-Token"];
        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        int userId = await SessionValidator.GetUserIdFromTokenAsync(conn, token);
        if (userId == 0) return Unauthorized("Session invalide.");

        if (string.IsNullOrWhiteSpace(req.NewUsername) || req.NewUsername.Length > 30)
            return BadRequest("Nom d'utilisateur invalide.");

        var check = new MySqlCommand("SELECT COUNT(*) FROM users WHERE username=@u AND id!=@id", conn);
        check.Parameters.AddWithValue("@u", req.NewUsername);
        check.Parameters.AddWithValue("@id", userId);
        if (Convert.ToInt32(await check.ExecuteScalarAsync()) > 0)
            return BadRequest("Nom d'utilisateur déjà pris.");

        var cmd = new MySqlCommand("UPDATE users SET username=@u WHERE id=@id", conn);
        cmd.Parameters.AddWithValue("@u", req.NewUsername);
        cmd.Parameters.AddWithValue("@id", userId);
        await cmd.ExecuteNonQueryAsync();

        return Ok();
    }
}

public record RegisterRequest(string Username, string Email, string Password);
public record LoginRequest(string Username, string Password);
public record RenameRequest(string NewUsername);
