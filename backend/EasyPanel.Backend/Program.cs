using System.Text;
using System.Text.Json.Serialization;
using EasyPanel.Backend.Features.AuditLog.ListAuditEntries;
using EasyPanel.Backend.Features.Auth.Login;
using EasyPanel.Backend.Features.Instances.CreateInstance;
using EasyPanel.Backend.Features.Instances.GetInstanceStatus;
using EasyPanel.Backend.Features.Instances.ListInstances;
using EasyPanel.Backend.Features.Instances.StartInstance;
using EasyPanel.Backend.Features.Instances.StopInstance;
using EasyPanel.Backend.Features.Nodes.ListNodes;
using EasyPanel.Backend.Features.Nodes.RegisterNode;
using EasyPanel.Backend.Features.Staff.AssignServerPermissions;
using EasyPanel.Backend.Features.Users.CreateUser;
using EasyPanel.Backend.Infrastructure;
using EasyPanel.Backend.Infrastructure.Security;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddDbContext<AppDbContext>(options => options
    .UseNpgsql(builder.Configuration.GetConnectionString("Postgres"))
    .UseSnakeCaseNamingConvention());

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Missing required 'Jwt' configuration section.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Without this, the JwtBearer handler silently renames "sub" to the long legacy
        // ClaimTypes.NameIdentifier URI (and similarly for a few other claims) before our
        // code ever sees the ClaimsPrincipal — every FindFirstValue(Sub) lookup would
        // return null even though the token is valid. Keep claim types exactly as issued.
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidateLifetime = true,
        };
    })
    .AddScheme<NodeTokenAuthenticationOptions, NodeTokenAuthenticationHandler>(NodeTokenAuthenticationDefaults.SchemeName, _ => { });

builder.Services.AddAuthorization();
builder.Services.AddSignalR();

builder.Services.AddSingleton<IPasswordHasher, Argon2PasswordHasher>();
builder.Services.AddSingleton<IJwtTokenIssuer, JwtTokenIssuer>();
builder.Services.AddSingleton<INodeConnectionTracker, NodeConnectionTracker>();
builder.Services.AddScoped<IServerPermissionChecker, ServerPermissionChecker>();

builder.Services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();

builder.Services.AddScoped<LoginHandler>();
builder.Services.AddScoped<RegisterNodeHandler>();
builder.Services.AddScoped<ListNodesHandler>();
builder.Services.AddScoped<CreateInstanceHandler>();
builder.Services.AddScoped<StartInstanceHandler>();
builder.Services.AddScoped<StopInstanceHandler>();
builder.Services.AddScoped<GetInstanceStatusHandler>();
builder.Services.AddScoped<ListInstancesHandler>();
builder.Services.AddScoped<AssignServerPermissionsHandler>();
builder.Services.AddScoped<ListAuditEntriesHandler>();
builder.Services.AddScoped<CreateUserHandler>();

var app = builder.Build();

using (var startupScope = app.Services.CreateScope())
{
    var dbContext = startupScope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();

    var passwordHasher = startupScope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    var logger = startupScope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await AdminAccountSeeder.SeedIfNeededAsync(dbContext, passwordHasher, logger, CancellationToken.None);
}

app.UseAuthentication();
app.UseAuthorization();

app.MapLoginEndpoint();
app.MapRegisterNodeEndpoint();
app.MapListNodesEndpoint();
app.MapCreateInstanceEndpoint();
app.MapStartInstanceEndpoint();
app.MapStopInstanceEndpoint();
app.MapGetInstanceStatusEndpoint();
app.MapListInstancesEndpoint();
app.MapAssignServerPermissionsEndpoint();
app.MapListAuditEntriesEndpoint();
app.MapCreateUserEndpoint();

app.MapHub<DaemonControlHub>("/hubs/daemon-control");
app.MapHub<DashboardHub>("/hubs/dashboard");
app.MapHub<FileTransferHub>("/hubs/file-transfer");

app.Run();
