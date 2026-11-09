using Core;
using Master;
using Microsoft.AspNetCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ConsistentHashRing>();
builder.Services.AddSingleton<SlaveRegistry>();
builder.Services.AddHttpClient<SlaveClient>(c => c.Timeout = TimeSpan.FromSeconds(5));
builder.Services.AddSingleton<MasterService>();

// The React dev server runs on a different origin, so the browser needs CORS headers.
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins("http://localhost:3000").AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// ---- Middleware ----

// One place that turns exceptions from MasterService into HTTP statuses.
app.UseExceptionHandler(errorApp => errorApp.Run(async ctx =>
{
    Exception? ex = ctx.Features.Get<IExceptionHandlerFeature>()?.Error;

    ctx.Response.StatusCode = ex switch
    {
        KeyNotFoundException => StatusCodes.Status404NotFound,              // unknown slave id
        InvalidOperationException => StatusCodes.Status409Conflict,         // last slave, empty ring, migration rejected
        HttpRequestException or TaskCanceledException => StatusCodes.Status502BadGateway, // slave down or timed out
        _ => StatusCodes.Status500InternalServerError
    };

    // The handler clears earlier headers, so repeat the CORS header or the browser hides the error.
    ctx.Response.Headers["Access-Control-Allow-Origin"] = "http://localhost:5173";
    await ctx.Response.WriteAsJsonAsync(new { error = ex?.Message ?? "Unexpected error" });
}));

app.UseCors();

// ---- Endpoints ----

// Client write: the ring picks the slave, the response says which one stored it.
app.MapGet("/setkey", async (string? key, string? value, MasterService master) =>
{
    if (string.IsNullOrEmpty(key))
        return Results.BadRequest(new { error = "key is required" });
    if (string.IsNullOrEmpty(value))
        return Results.BadRequest(new { error = "value is required" });

    int? slaveId = await master.SetAsync(key, value);

    return slaveId is null
        ? Results.Json(new { error = "slave rejected the write" }, statusCode: StatusCodes.Status502BadGateway)
        : Results.Ok(new { key, value, slaveId });
});

// Client read: 404 means a miss (never set, or evicted by the LRU).
app.MapGet("/getkey", async (string? key, MasterService master) =>
{
    if (string.IsNullOrEmpty(key))
        return Results.BadRequest(new { error = "key is required" });

    string? value = await master.GetAsync(key);

    return value is null
        ? Results.NotFound(new { key, error = "key not found" })
        : Results.Ok(new { key, value });
});

// Dashboard: which cards to draw.
app.MapGet("/api/slaves", (MasterService master) =>
    Results.Ok(new { slaves = master.GetSlaves() }));

// Dashboard: one card's contents. Shape matches what the React app reads (data.data).
app.MapGet("/api/slave/data", async (int? slaveId, MasterService master) =>
{
    if (slaveId is null)
        return Results.BadRequest(new { error = "slaveId is required" });

    var data = await master.GetSlaveDataAsync(slaveId.Value);
    return Results.Ok(new { data, slave_id = slaveId.Value });
});

// "Add Slave" button. The new slave process must already be running.
app.MapGet("/api/addSlave", async (MasterService master) =>
{
    var slaves = await master.AddSlaveAsync();
    return Results.Ok(new { slaves });
});

// "Remove Slave" button. The id comes from the request.
app.MapGet("/api/popSlave", async (int? slaveId, MasterService master) =>
{
    if (slaveId is null)
        return Results.BadRequest(new { error = "slaveId is required" });

    var slaves = await master.RemoveSlaveAsync(slaveId.Value);
    return Results.Ok(new { slaves });
});




app.Run();
