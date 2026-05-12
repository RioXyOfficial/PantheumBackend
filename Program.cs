using System.Net.WebSockets;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton(
    new DatabaseHelper(builder.Configuration.GetConnectionString("Default")));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var ex = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
        if (ex != null)
            Console.Error.WriteLine($"[ERREUR] {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");

        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync("{\"error\": \"Une erreur interne s'est produite. Veuillez réessayer.\"}");
    });
});

app.UseWebSockets();

app.Map("/ws/session", async context =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = 400;
        return;
    }

    string? token = context.Request.Headers["X-Session-Token"];
    if (string.IsNullOrEmpty(token))
    {
        context.Response.StatusCode = 401;
        return;
    }

    var db = context.RequestServices.GetRequiredService<DatabaseHelper>();
    using var conn = db.GetConnection();
    await conn.OpenAsync();

    int userId = await SessionValidator.GetUserIdFromTokenAsync(conn, token);
    if (userId == 0)
    {
        context.Response.StatusCode = 401;
        return;
    }

    var ws = await context.WebSockets.AcceptWebSocketAsync();
    SessionSocketManager.Register(userId, ws);

    var buffer = new byte[64];
    try
    {
        while (ws.State == WebSocketState.Open)
        {
            var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Fermeture", CancellationToken.None);
                break;
            }
        }
    }
    catch { }
    finally
    {
        SessionSocketManager.Unregister(userId, ws);
        LobbyStore.RemoveByHost(userId);
    }
});

app.MapControllers();

app.Run();
