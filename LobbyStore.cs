using System.Collections.Concurrent;

public class LobbyInfo
{
    public int    Id             { get; set; }
    public string Name           { get; set; } = "";
    public int    HostUserId     { get; set; }
    public string HostIp         { get; set; } = "";
    public string GameMode       { get; set; } = "1v1";
    public int    MaxPlayers     { get; set; } = 2;
    public int    CurrentPlayers { get; set; } = 1;
}

public static class LobbyStore
{
    private static readonly ConcurrentDictionary<int, LobbyInfo> _lobbies = new();
    private static int _nextId = 0;

    public static LobbyInfo Create(string name, int hostUserId, string hostIp, string gameMode, int maxPlayers)
    {
        RemoveByHost(hostUserId);

        var lobby = new LobbyInfo
        {
            Id             = System.Threading.Interlocked.Increment(ref _nextId),
            Name           = name,
            HostUserId     = hostUserId,
            HostIp         = hostIp,
            GameMode       = gameMode,
            MaxPlayers     = maxPlayers,
            CurrentPlayers = 1
        };
        _lobbies[lobby.Id] = lobby;
        return lobby;
    }

    public static IEnumerable<LobbyInfo> GetAll() => _lobbies.Values;

    public static bool Remove(int id, int hostUserId)
    {
        if (_lobbies.TryGetValue(id, out var lobby) && lobby.HostUserId == hostUserId)
            return _lobbies.TryRemove(id, out _);
        return false;
    }

    public static void RemoveByHost(int hostUserId)
    {
        foreach (var kvp in _lobbies)
            if (kvp.Value.HostUserId == hostUserId)
                _lobbies.TryRemove(kvp.Key, out _);
    }
}
