using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

[ApiController]
[Route("api/[controller]")]
public class StatsController : ControllerBase
{
    private readonly DatabaseHelper _db;

    public StatsController(IConfiguration config)
    {
        _db = new DatabaseHelper(config.GetConnectionString("Default"));
    }

    // MISE A JOUR DES STATS EN FIN DE PARTIE
    [HttpPost("update")]
    public async Task<IActionResult> UpdateStats([FromBody] StatsRequest req)
    {
        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        var cmd = new MySqlCommand(@"
            INSERT INTO statistics (user_id, games_played, victories, playtime)
            VALUES (@uid, 1, @vic, @time)
            ON DUPLICATE KEY UPDATE
                games_played = games_played + 1,
                victories = victories + @vic,
                playtime = playtime + @time", conn);

        cmd.Parameters.AddWithValue("@uid", req.UserId);
        cmd.Parameters.AddWithValue("@vic", req.Victory ? 1 : 0);
        cmd.Parameters.AddWithValue("@time", req.Playtime);

        await cmd.ExecuteNonQueryAsync();
        return Ok("Statistiques mises à jour.");
    }

    // RECUPERATION DES STATS
    [HttpGet("{userId}")]
    public async Task<IActionResult> GetStats(int userId)
    {
        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        var cmd = new MySqlCommand("SELECT * FROM statistics WHERE user_id=@uid", conn);
        cmd.Parameters.AddWithValue("@uid", userId);

        using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return NotFound("Aucune statistique trouvée.");

        return Ok(new {
            gamesPlayed = reader.GetInt32("games_played"),
            victories = reader.GetInt32("victories"),
            playtime = reader.GetInt32("playtime")
        });
    }
}

public record StatsRequest(int UserId, bool Victory, int Playtime);