using Microsoft.EntityFrameworkCore;
using PodcastApi.Data;
//using PodcastApi.Filters;
using PodcastApi.Parsing;
using PodcastApi.Rss;
using PodcastApi.Sync;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options => 
    options.UseSqlite(builder.Configuration["ConnectionString:Default"]));
    
builder.Services.AddHttpClient<RssFeedService>();
builder.Services.AddSingleton<EpisodeParser>();
builder.Services.AddScoped<SyncService>();
//builder.Services.AddScoped<ApiKeyAuthFilter>();
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