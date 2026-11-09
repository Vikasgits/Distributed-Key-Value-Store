using Core;

var builder = WebApplication.CreateBuilder(args);

//One Cache for the whole processof each slave 
builder.Services.AddSingleton(new LRUCache<string, string>(10));

var app = builder.Build();
int slaveId = app.Configuration.GetValue<int>("SlaveId");


//Controllers

    app.MapGet("/getKey",(string? key, LRUCache<string, string> cache) =>
    {
        if (string.IsNullOrEmpty(key) )
            return Results.BadRequest(
                new { error = "key is required" });
        

        return cache.TryGet(key, out var value) ? Results.Ok(new { key, value }): Results.NotFound(new { key, error = "key not found" });
    });

    app.MapGet("/setKey",(string? key, string? value , LRUCache<string, string> cache) =>
    {
        if (string.IsNullOrEmpty(key))
            return Results.BadRequest(new { error = "key is required" });

        if (string.IsNullOrEmpty(value))
            return Results.BadRequest(new { error = "Value is required" });

        cache.Set(key, value) ;
        return Results.Ok(new { key, value });
    });

    app.MapGet("/pop",(string? key, LRUCache<string, string> cache) =>
    {
        if (string.IsNullOrEmpty(key) )
            return Results.BadRequest(
                new { error = "key is required" });
        

        return cache.TryRemove(key, out var value) ? Results.Ok(new { value }): Results.NotFound(new { key, error = "key not found" });
    });

    app.MapGet("/getData",( LRUCache<string, string> cache) =>
    {
        var data = cache.Snapshot().ToDictionary(kv => kv.Key, kv => kv.Value);
        return Results.Ok(new { data, slave_id = slaveId, message = "Data from slave" });
    });



app.Run();
