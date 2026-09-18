using Microsoft.EntityFrameworkCore;
using PodcastApi.Data;
using PodcastApi.Filters;
using PodcastApi.Parsing;
using PodcastApi.Rss;
using PodcastApi.Sync;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

var logDirectory = builder.Configuration["Logging:Directory"] ?? "logs";
builder.Host.UseSerilog((context, loggerConfig) => loggerConfig
    .MinimumLevel.Warning()
    .MinimumLevel.Override("PodcastApi", LogEventLevel.Information)
    .WriteTo.Console()
    .WriteTo.File(
        Path.Combine(logDirectory, "api-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 31,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"));

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

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/",  () => "PodcastApi is Running!");
app.MapControllers();

app.Lifetime.ApplicationStarted.Register(() =>
{
    var urls = app.Configuration["PublicUrl"] is { Length: > 0 } publicUrl ? [publicUrl] : app.Urls.ToArray();
    app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("PodcastApi")
        .LogInformation("Listening on {Urls} (Swagger at {Swagger})",
            string.Join(", ", urls), urls[0].TrimEnd('/') + "/swagger");
});

app.Run();