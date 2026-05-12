using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

[ApiController]
[Route("api/[controller]")]
public class LobbiesController : ControllerBase
{
    private readonly DatabaseHelper _db;

    public LobbiesController(DatabaseHelper db) => _db = db;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLobbyRequest req)
    {
        string? token = Request.Headers["X-Session-Token"];
        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        int userId = await SessionValidator.GetUserIdFromTokenAsync(conn, token);
        if (userId == 0) return Unauthorized("Session invalide.");

        var lobby = LobbyStore.Create(req.Name, userId, req.HostIp, req.GameMode, req.MaxPlayers);
        return Ok(new { id = lobby.Id });
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        string? token = Request.Headers["X-Session-Token"];
        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        if (!await SessionValidator.IsValidAsync(conn, token))
            return Unauthorized("Session invalide.");

        var lobbies = LobbyStore.GetAll().Select(l => new {
            id             = l.Id,
            name           = l.Name,
            hostIp         = l.HostIp,
            gameMode       = l.GameMode,
            maxPlayers     = l.MaxPlayers,
            currentPlayers = l.CurrentPlayers
        });

        return Ok(new { lobbies });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Close(int id)
    {
        string? token = Request.Headers["X-Session-Token"];
        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        int userId = await SessionValidator.GetUserIdFromTokenAsync(conn, token);
        if (userId == 0) return Unauthorized("Session invalide.");

        LobbyStore.Remove(id, userId);
        return Ok();
    }
}

public record CreateLobbyRequest(string Name, string HostIp, string GameMode, int MaxPlayers);
