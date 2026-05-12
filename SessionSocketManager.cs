using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

public static class SessionSocketManager
{
    private static readonly ConcurrentDictionary<int, WebSocket> _sockets = new();

    public static void Register(int userId, WebSocket ws)
        => _sockets[userId] = ws;

    public static void Unregister(int userId, WebSocket ws)
    {
        if (_sockets.TryGetValue(userId, out var stored) && ReferenceEquals(stored, ws))
            _sockets.TryRemove(userId, out _);
    }

    public static async Task KickAsync(int userId)
    {
        if (!_sockets.TryRemove(userId, out var ws)) return;
        if (ws.State != WebSocketState.Open) return;

        try
        {
            var msg = Encoding.UTF8.GetBytes("KICKED");
            await ws.SendAsync(new ArraySegment<byte>(msg), WebSocketMessageType.Text, true, CancellationToken.None);
            await ws.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Nouvelle session détectée", CancellationToken.None);
        }
        catch { }
    }
}
