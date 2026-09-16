using Microsoft.EntityFrameworkCore;
using PodcastApi.Data;
using PodcastApi.Filters;
using PodcastApi.Parsing;
using PodcastApi.Rss;
using PodcastApi.Sync;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Console for `docker compose logs`, rolling file so a refresh that ran days ago
// is still auditable. In Docker the directory points at the mounted volume.
var logDirectory = builder.Configuration["Logging:Directory"] ?? "logs";
builder.Host.UseSerilog((context, loggerConfig) => loggerConfig
    .ReadFrom.Configuration(context.Configuration)
    .WriteTo.Console()
    .WriteTo.File(
        Path.Combine(logDirectory, "api-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 31,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"));

builder.Services.AddDbContext<AppDbContext>(options => 
    options.UseSqlite(builder.Configuration["ConnectionString:Default"]));
    
builder.Services.AddHttpClient<RssFeedService>();
builder.Services.AddSingleton<EpisodeParser>();
builder.Services.AddScoped<SyncService>();
builder.Services.AddScoped<ApiKeyAuthFilter>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();     

if (builder.Configuration.GetValue<bool>("Sync:Enabled"))
    builder.Services.AddHostedService<ScheduledSyncService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope()) scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}    

app.MapGet("/",  () => "PodcastApi is Running!");
app.MapControllers();

app.Run();