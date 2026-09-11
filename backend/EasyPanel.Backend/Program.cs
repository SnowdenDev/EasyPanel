using System.Text;
using System.Text.Json.Serialization;
using EasyPanel.Backend.Features.AuditLog.ListAuditEntries;
using EasyPanel.Backend.Features.Auth.Login;
using EasyPanel.Backend.Features.FileManager.DownloadFile;
using EasyPanel.Backend.Features.FileManager.UploadFile;
using EasyPanel.Backend.Features.Instances.CreateInstance;
using EasyPanel.Backend.Features.Instances.GetInstanceStatus;
using EasyPanel.Backend.Features.Instances.ListInstances;
using EasyPanel.Backend.Features.Instances.RestartInstance;
using EasyPanel.Backend.Features.Instances.StartInstance;
using EasyPanel.Backend.Features.Instances.StopInstance;
using EasyPanel.Backend.Features.Instances.DeleteInstance;
using EasyPanel.Backend.Features.Instances.UpdateInstance;
using EasyPanel.Backend.Features.Nodes.ComputeExecutableHash;
using EasyPanel.Backend.Features.Nodes.ListNodes;
using EasyPanel.Backend.Features.Nodes.RegisterNode;
using EasyPanel.Backend.Features.Nodes.DeleteNode;
using EasyPanel.Backend.Features.Nodes.UpdateNode;
using EasyPanel.Backend.Features.Staff.AssignServerPermissions;
using EasyPanel.Backend.Features.Staff.GetStaffPermissions;
using EasyPanel.Backend.Features.Users.CreateUser;
using EasyPanel.Backend.Features.Users.ListUsers;
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

// No CORS policy: the browser never talks to this backend directly for anything. The
// dashboard's own server proxies every REST call (Server Actions/Route Handlers, plain
// server-to-server fetch, which browser CORS doesn't apply to) and bridges the
// DashboardHub's live console/node-status events to the browser as Server-Sent Events
// from its own origin — see the dashboard's app/api/console-stream and app/api/nodes-stream
// routes. The only client that connects to a hub here directly is the Daemon
// (DaemonControlHub/FileTransferHub, node-token auth), which is never a browser and isn't
// subject to CORS either. See docs/architecture.md's Phase 3 notes for why this changed
// from an earlier design that did expose DashboardHub to the browser.
builder.Services.AddSignalR(options =>
{
    // Default is 32 KB — a 64 KB file chunk, base64-encoded plus JSON envelope overhead,
    // comfortably exceeds that. SignalR doesn't throw when this is hit; it just closes the
    // connection, so an undersized limit here shows up as file chunks silently never
    // arriving rather than as any visible error. See docs/architecture.md.
    options.MaximumReceiveMessageSize = 1024 * 1024;
});

builder.Services.AddSingleton<IPasswordHasher, Argon2PasswordHasher>();
builder.Services.AddSingleton<IJwtTokenIssuer, JwtTokenIssuer>();
builder.Services.AddSingleton<INodeConnectionTracker, NodeConnectionTracker>();
builder.Services.AddSingleton<FileTransferCoordinator>();
builder.Services.AddSingleton<PendingHashComputationTracker>();
builder.Services.AddScoped<IServerPermissionChecker, ServerPermissionChecker>();

builder.Services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();

builder.Services.AddScoped<LoginHandler>();
builder.Services.AddScoped<RegisterNodeHandler>();
builder.Services.AddScoped<ListNodesHandler>();
builder.Services.AddScoped<CreateInstanceHandler>();
builder.Services.AddScoped<StartInstanceHandler>();
builder.Services.AddScoped<StopInstanceHandler>();
builder.Services.AddScoped<RestartInstanceHandler>();
builder.Services.AddScoped<GetInstanceStatusHandler>();
builder.Services.AddScoped<ListInstancesHandler>();
builder.Services.AddScoped<AssignServerPermissionsHandler>();
builder.Services.AddScoped<GetStaffPermissionsHandler>();
builder.Services.AddScoped<ListAuditEntriesHandler>();
builder.Services.AddScoped<CreateUserHandler>();
builder.Services.AddScoped<ListUsersHandler>();
builder.Services.AddScoped<DownloadFileHandler>();
builder.Services.AddScoped<UploadFileHandler>();
builder.Services.AddScoped<ComputeExecutableHashHandler>();
builder.Services.AddScoped<UpdateInstanceHandler>();
builder.Services.AddScoped<DeleteInstanceHandler>();
builder.Services.AddScoped<UpdateNodeHandler>();
builder.Services.AddScoped<DeleteNodeHandler>();

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
app.MapRestartInstanceEndpoint();
app.MapGetInstanceStatusEndpoint();
app.MapListInstancesEndpoint();
app.MapAssignServerPermissionsEndpoint();
app.MapGetStaffPermissionsEndpoint();
app.MapListAuditEntriesEndpoint();
app.MapCreateUserEndpoint();
app.MapListUsersEndpoint();
app.MapDownloadFileEndpoint();
app.MapUploadFileEndpoint();
app.MapComputeExecutableHashEndpoint();
app.MapUpdateInstanceEndpoint();
app.MapDeleteInstanceEndpoint();
app.MapUpdateNodeEndpoint();
app.MapDeleteNodeEndpoint();

app.MapHub<DaemonControlHub>("/hubs/daemon-control");
app.MapHub<DashboardHub>("/hubs/dashboard");
app.MapHub<FileTransferHub>("/hubs/file-transfer");

app.Run();
