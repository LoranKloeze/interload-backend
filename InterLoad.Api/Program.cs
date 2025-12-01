using System.Net;
using System.Text.RegularExpressions;
using Hangfire;
using Hangfire.Console;
using Hangfire.PostgreSql;
using InterLoad;
using InterLoad.Data;
using InterLoad.Filters;
using InterLoad.Jobs;
using InterLoad.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Npgsql;
using Scalar.AspNetCore;
using Serilog;
using IPNetwork = Microsoft.AspNetCore.HttpOverrides.IPNetwork;


// ---- logging / builder ----
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File(
        path: "logs/app.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        shared: true,
        outputTemplate:
        "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] ({SourceContext}) {Message:lj}{NewLine}{Exception}"
    )
    .MinimumLevel.Information()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSerilog();
builder.Services.AddControllersWithViews();
builder.Services.AddTransient<IClaimsTransformation, OidcClaimsTransformation>();
const string corsPolicy = "AllowLocalhost5173";

// This is a comment
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: corsPolicy, policy =>
    {
        policy
            .WithOrigins("https://interload.local", "https://interload.codedivision.nl", "https://interload-frontend-wifj3-03af066d8bb1.herokuapp.com")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto |
                               ForwardedHeaders.XForwardedHost;
    options.KnownNetworks.Clear();
    options.KnownNetworks.Add(new IPNetwork(IPAddress.Parse("172.18.0.0"), 16));
    options.KnownProxies.Clear();
    options.KnownProxies.Add(IPAddress.Parse("127.0.0.1"));
});


string connectionString;
if (builder.Environment.IsProduction())
{
    var match = Regex.Match(Environment.GetEnvironmentVariable("DATABASE_URL") ?? "",
        @"postgres://(.*):(.*)@(.*):(.*)/(.*)");
    connectionString =
        $"Server={match.Groups[3]};Port={match.Groups[4]};User Id={match.Groups[1]};Password={match.Groups[2]};Database={match.Groups[5]};sslmode=Prefer;Trust Server Certificate=true";
}
else
{
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                       throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
}

var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
dataSourceBuilder.EnableDynamicJson();
var dataSource = dataSourceBuilder.Build();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(dataSource);
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
    }
});
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddOpenApi(c =>
{
    c.AddDocumentTransformer((document, _, _) =>
    {
        const string schemeId = "BearerAuth";

        var bearerScheme = new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\""
        };

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, OpenApiSecurityScheme>();
        document.Components.SecuritySchemes[schemeId] = bearerScheme;

        document.SecurityRequirements ??= new List<OpenApiSecurityRequirement>();
        document.SecurityRequirements.Add(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = schemeId
                    }
                },
                Array.Empty<string>()
            }
        });

        return Task.CompletedTask;
    });
});


// ---- Hangfire ----
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseConsole()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(c =>
        c.UseNpgsqlConnection(connectionString)));
builder.Services.AddHangfireServer(options =>
{
    options.Queues = ["default"];
    options.SchedulePollingInterval = TimeSpan.FromSeconds(3);
});

// ========== OPTION 2: Cookie (authenticate) + OIDC (challenge) for MVC, JWT Bearer for /api ==========
var oidc = builder.Configuration.GetSection("OpenIDConnectSettings");
const string jwtAudience = "account";

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme; // MVC reads/writes the auth cookie
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme; // Unauthenticated → OIDC challenge
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.Cookie.Name = ".InterLoad.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // require HTTPS in production
        options.Cookie.SameSite = SameSiteMode.None;
        options.LoginPath = "/account/login";
        options.LogoutPath = "/account/logout";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8); // session lifetime
    }) // cookie for MVC
    .AddJwtBearer(options =>
    {
        var authority = oidc["Authority"];
        if (string.IsNullOrEmpty(authority))
            throw new InvalidOperationException("OIDC Authority is not configured.");
        options.Authority = authority; // uses OIDC discovery for JWKS
        options.RequireHttpsMetadata = oidc["RequireHttpsMetadata"] == "true";

        options.TokenValidationParameters = TokenValidationParametersFactory
            .Create(authority, jwtAudience);
        // No per-handler claim mutations here; all enrichment happens in IClaimsTransformation
    });

builder.Services.AddAuthorization(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.DefaultPolicy = new AuthorizationPolicyBuilder()
            .RequireAssertion(_ => true) // always allow
            .Build();
    }
});

builder.Services.AddHttpClient();
builder.Services.AddScoped<SyncUltraOfficeToProjectsService>();
builder.Services.AddScoped<SyncUltraOfficeToSubProjectsService>();
builder.Services.AddScoped<SyncUltraOfficeToCollectablesService>();
builder.Services.AddScoped<SyncUltraOfficeToStockPeriodsService>();
builder.Services.AddScoped<SyncUltraOfficeToCollectDemandsService>();
builder.Services.AddScoped<SyncUltraOfficeToCollectGroupsService>();
builder.Services.AddScoped<SyncUltraOfficeToCollectGroupEntriesService>();
builder.Services.AddScoped<ReconcileActiveCollectGroupsService>();
builder.Services.AddScoped<CollectOrderService>();


var app = builder.Build();
app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseCors(corsPolicy);
app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireDashboard("/hangfire",
    new DashboardOptions
    {
        Authorization = [new HangfireAuthorizationFilter()],
        StatsPollingInterval = 60000
    });

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
        {
            var newOptions = options
                .AddPreferredSecuritySchemes("BearerAuth")
                .AddHttpAuthentication("BearerAuth",
                    auth => { auth.Token = builder.Configuration["JwtDevToken"] ?? string.Empty; })
                .ExpandAllResponses()
                .EnablePersistentAuthentication(); // <– stores auth in localStorage
            newOptions.CustomCss =
                ".scalar-client{ max-width: 100% !important; } .scalar-client section:nth-child(1) { max-width: 25%; }";
        }
    );
}

app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action}/{id?}");

// Redirect to /hangfire when accessing root
app.MapGet("/", context =>
{
    context.Response.Redirect("/hangfire");
    return Task.CompletedTask;
});

const bool enableCrons = true;
SetupJobs.Setup(enableCrons);

if (args.Length > 0 && args[0] == "cmd")
{
    args = args.Skip(1).ToArray();
    var runner = new CommandRunner(app.Services.CreateScope(), args);
    await runner.RunAsync();
}
else
{
    app.Run();
}