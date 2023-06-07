using AlacrityCore.Infrastructure;
using AlacrityServer.Hubs;
using AlacrityServer.Infrastructure;
using AlacrityServer.Infrastructure.SessionToken;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Serilog;
using static System.Net.Mime.MediaTypeNames;


var builder = WebApplication.CreateBuilder(args);

var serverLogger = ALogger.GetAspNetCoreLogger(builder.Configuration);
var backServiceLogger = ALogger.GetBackServiceLogger();
var exchangeLogger = ALogger.GetExchangeLogger();
Log.Logger = serverLogger;
Log.Logger.Warning("Configuring Alacrity Server");

SqlMapper.AddTypeHandler(new DateTimeHandler());


builder.Logging.ClearProviders();
builder.Host.UseSerilog();

builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.CheckConsentNeeded = context => false;
    options.MinimumSameSitePolicy = SameSiteMode.Strict;
});
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{    
    options.IdleTimeout = TimeSpan.FromDays(1);
    options.Cookie.Name = "ALACRITY_S";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

builder.Services.AddMvcCore();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers();
builder.Services
    .AddSwaggerGen()
    .AddCors();

builder.Services.AddAuthentication()
    .AddScheme<SessionTokenAuthenticationSchemeOptions, SessionTokenAuthenticationSchemeHandler>(
        "SessionTokens", options => { }
    );

builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .AddAuthenticationSchemes("SessionTokens")
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddSignalR();

// Set up our own services
StartUp.SetUpCoreService(serverLogger, backServiceLogger, builder.Services);

var app = builder.Build();

// Set Up Streaming Events
await MessageManager.SetupMessageManager(serverLogger, app.Services);

// Put all backend services into a working state
await StartUp.InitStaticDependencies(serverLogger, exchangeLogger, app.Services);

app.UseSerilogRequestLogging();

app.UseCors(options =>
{
    options
        .WithOrigins(new[]
        {
            "http://localhost:8000",
            "https://localhost:8001",
            "http://localhost:5173"
            //"http://*:8000",
            //"https://*:8001"
        })
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials()
        .WithHeaders(new[]
        {
            "authorization", "accept", "content-type", "origin"
        })
        .WithExposedHeaders("*");
});

// Setup Exception Handler
app.UseExceptionHandler(handler =>
{
    handler.Run(async context =>
    {
        var feature = context.Features.Get<IExceptionHandlerPathFeature>();
        context.Response.ContentType = Text.Plain;
        context.Response.StatusCode = feature.Error is ArgumentException
            ? StatusCodes.Status400BadRequest
            : StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsync("Request Failed");
    });
});

app.UsePathBase("/api");
app.UseHttpsRedirection()
    .UseDefaultFiles()
    .UseStaticFiles()
    .UseRouting();

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI()
        .UseSwagger();
}

app.UseSession()
    .UseAuthentication()
    .UseAuthorization();

app.MapControllers()
    .RequireAuthorization();

app.MapHub<CentralHub>("/central");

app.Run();

StartUp.Stop();
await serverLogger.DisposeAsync();
await exchangeLogger.KillAsync();