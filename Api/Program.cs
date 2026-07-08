
using Api.Helpers;
using Api.SQLService;
using Api.WebDataService;
using Dapper;
using Microsoft.AspNetCore.HttpOverrides;

// All dates cross the DB boundary as RFC 3339 UTC with 'Z' — must run before any query.
// RemoveTypeMap is required: DateTime is in Dapper's built-in type map, which would
// otherwise take precedence over the handlers when binding parameters.
SqlMapper.RemoveTypeMap(typeof(DateTime));
SqlMapper.RemoveTypeMap(typeof(DateTime?));
SqlMapper.AddTypeHandler(new SqliteDateTimeHandler());
SqlMapper.AddTypeHandler(new SqliteNullableDateTimeHandler());

var builder = WebApplication.CreateBuilder(args);
var malApiKey = builder.Configuration.GetSection("MalClientId").Value;
var systemVersion = builder.Configuration.GetSection("SystemVersion").Value;


builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
builder.Logging.AddFilter("System", LogLevel.Warning);
builder.Logging.AddFilter("Api", LogLevel.Information);



//Services

builder.Services.AddScoped<IAnimeRespository, AnimeRepository>();
builder.Services.AddScoped<IMalWebData, MalWebData>();

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

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});


builder.Services.AddSingleton<IDbConnectionFactory, SqliteConnectionFactory>();


var app = builder.Build();

app.UseCors("AllowAll");
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});


if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();
app.MapGet("/", () => Results.Ok("VCS Api Version: " + systemVersion));

app.Run();
