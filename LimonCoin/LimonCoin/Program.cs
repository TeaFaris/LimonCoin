using Blazored.LocalStorage;
using LimonCoin.Components;
using LimonCoin.Configuration;
using LimonCoin.Controllers;
using LimonCoin.Data;
using LimonCoin.Hubs;
using LimonCoin.Services;
using LimonCoin.TelegramBot;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var config = builder.Configuration;

#if !DEBUG
builder.Services.AddLettuceEncrypt();

builder.WebHost.UseKestrel(options =>
{
    var appServices = options.ApplicationServices;

    options.ListenAnyIP(80);
    options.ListenAnyIP(443, config => config.UseHttps(h => h.UseLettuceEncrypt(appServices)));
});
#endif

// Configs
builder.Services
    .Configure<TelegramSettings>(config.GetSection(nameof(TelegramSettings)));

// Database
var connectionString = config.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDBContext>(options
    => options.UseNpgsql(connectionString));

// Hosted services
builder.Services.AddHostedService<UpdateEnergyBackgroundService>();

// SignalR
builder.Services.AddSignalR();

builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["application/octet-stream"]);
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

// ASP.NET services
builder.Services.AddControllers()
    .AddApplicationPart(typeof(UserController).Assembly);

// Client
builder.Services.AddBlazoredLocalStorage();

builder.Services.AddScoped(sp =>
{
    var absoluteUri = sp.GetRequiredService<NavigationManager>().BaseUri;

    return new HttpClient
    {
        BaseAddress = new Uri(absoluteUri)
    };
});

// Telegram Bot
builder.Services.AddTelegramBot();

var app = builder.Build();

// Database auto-migration
using (var provider = app.Services.CreateScope())
{
    var context = provider.ServiceProvider.GetRequiredService<ApplicationDBContext>();
    if (context.Database.GetPendingMigrations().Any())
    {
        context.Database.Migrate();
    }
}

app.Services.GetRequiredService<TelegramBotService>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapControllers();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(LimonCoin.Client._Imports).Assembly);

// SignalR Hubs
app.MapHub<ClickerHub>("api/hub/clicker");

app.Run();