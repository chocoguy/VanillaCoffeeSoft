
using System.Threading.RateLimiting;
using Api.Helpers;
using Api.Services;
using Api.SQLService;
using Api.WebDataService;
using Dapper;
using Microsoft.AspNetCore.HttpOverrides;


SqlMapper.RemoveTypeMap(typeof(DateTime));
SqlMapper.RemoveTypeMap(typeof(DateTime?));
SqlMapper.AddTypeHandler(new SqliteDateTimeHandler());
SqlMapper.AddTypeHandler(new SqliteNullableDateTimeHandler());

var builder = WebApplication.CreateBuilder(args);
var malApiKey = builder.Configuration.GetSection("MalClientId").Value;
var metraApiKey = builder.Configuration.GetSection("MetraGtfsKey").Value;
var systemVersion = builder.Configuration.GetSection("SystemVersion").Value;


builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
builder.Logging.AddFilter("System", LogLevel.Warning);
builder.Logging.AddFilter("Api", LogLevel.Information);



//Services

builder.Services.AddScoped<IAnimeRespository, AnimeRepository>();
builder.Services.AddScoped<IWindyPointsRepository, WindyPointsRepository>();
builder.Services.AddScoped<IBlueTrainsRepository, BlueTrainsRepository>();
builder.Services.AddScoped<IBlueTrainsStaticImportRepository, BlueTrainsStaticImportRepository>();
builder.Services.AddScoped<IBlueTrainsRealtimeRepository, BlueTrainsRealtimeRepository>();
builder.Services.AddScoped<IMalWebData, MalWebData>();
builder.Services.AddScoped<IMetraWebData, MetraWebData>();
builder.Services.AddHostedService<StaleRecordSyncService>();
builder.Services.AddHostedService<BlueTrainsStaticSyncService>();
builder.Services.AddHostedService<BlueTrainsRealtimeSyncService>();

builder.Services.AddOpenApi();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new Rfc3339JsonConverter());
        options.JsonSerializerOptions.Converters.Add(new NullableRfc3339JsonConverter());
    });
builder.Services.AddHttpClient(
    "MalClient",
    client =>
    {client.BaseAddress = new Uri("https://api.myanimelist.net/v2/"); client.DefaultRequestHeaders.Add("X-MAL-CLIENT-ID", malApiKey);}
);

builder.Services.AddHttpClient(
    "MetraClientRealTime",
    client =>
    {
        client.BaseAddress = new Uri("https://gtfspublic.metrarr.com/gtfs/public/");
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {metraApiKey}");
    }
);

builder.Services.AddHttpClient(
    "MetraClientStatic",
    client =>
    {
        client.BaseAddress = new Uri("https://schedules.metrarail.com/gtfs/");
    }
);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});


builder.Services.AddSingleton<IAniTrakDbConnectionFactory, AniTrakDbConnectionFactory>();
builder.Services.AddSingleton<IWindyPointsDbConnectionFactory, WindyPointsDbConnectionFactory>();
builder.Services.AddSingleton<IBlueTrainsStaticDbConnectionFactory, BlueTrainsStaticDbConnectionFactory>();
builder.Services.AddSingleton<IBlueTrainsRealtimeDbConnectionFactory, BlueTrainsRealtimeDbConnectionFactory>();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});


var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});
app.UseCors("AllowAll");
app.UseRateLimiter();

// Landing page: "/" serves wwwroot/index.html
app.UseDefaultFiles();
app.UseStaticFiles();


if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();
app.MapGet("/api/version", () => Results.Ok("VCS Api Version: " + systemVersion));

app.Run();
