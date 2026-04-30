using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly DatabaseHelper _db;

    public AuthController(IConfiguration config)
    {
        _db = new DatabaseHelper(config.GetConnectionString("Default"));
    }

    // INSCRIPTION
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        // Vérifier si username ou email existe déjà
        var check = new MySqlCommand("SELECT COUNT(*) FROM users WHERE username=@u OR email=@e", conn);
        check.Parameters.AddWithValue("@u", req.Username);
        check.Parameters.AddWithValue("@e", req.Email);
        var count = Convert.ToInt32(await check.ExecuteScalarAsync());
        if (count > 0) return BadRequest("Utilisateur ou email déjà existant.");

        // Hasher le mot de passe
        string hash = BCrypt.Net.BCrypt.HashPassword(req.Password);

        // Insérer l'utilisateur
        var cmd = new MySqlCommand("INSERT INTO users (username, email, password_hash) VALUES (@u, @e, @p)", conn);
        cmd.Parameters.AddWithValue("@u", req.Username);
        cmd.Parameters.AddWithValue("@e", req.Email);
        cmd.Parameters.AddWithValue("@p", hash);
        await cmd.ExecuteNonQueryAsync();

        return Ok("Inscription réussie.");
    }

    // CONNEXION
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        var cmd = new MySqlCommand("SELECT id, password_hash FROM users WHERE username=@u", conn);
        cmd.Parameters.AddWithValue("@u", req.Username);

        using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return Unauthorized("Utilisateur introuvable.");

        int userId = reader.GetInt32("id");
        string hash = reader.GetString("password_hash");

        if (!BCrypt.Net.BCrypt.Verify(req.Password, hash))
            return Unauthorized("Mot de passe incorrect.");

        return Ok(new { userId, req.Username });
    }
}

public record RegisterRequest(string Username, string Email, string Password);
public record LoginRequest(string Username, string Password);