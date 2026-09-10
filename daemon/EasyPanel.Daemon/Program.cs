using EasyPanel.Daemon.Features.LaunchInstance;
using EasyPanel.Daemon.Features.StopInstance;
using EasyPanel.Daemon.Infrastructure;
using EasyPanel.Daemon.Transport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<NodeIdentityOptions>(builder.Configuration.GetSection(NodeIdentityOptions.SectionName));

builder.Services.AddSingleton<LaunchedProcessRegistry>();
builder.Services.AddSingleton<LaunchInstanceCommandHandler>();
builder.Services.AddSingleton<StopInstanceCommandHandler>();

// One ControlHubConnection instance plays both roles — the hosted service that owns the
// connection lifecycle, and the IBackendReporter every feature reports through — so both
// registrations below must resolve to the exact same singleton. ControlHubConnection
// resolves LaunchInstanceCommandHandler/StopInstanceCommandHandler lazily via
// IServiceProvider rather than as constructor parameters — seeing IBackendReporter here
// too would make this a genuine dependency cycle (ControlHubConnection ->
// LaunchInstanceCommandHandler -> IBackendReporter -> ControlHubConnection), which the DI
// container deadlocks on instead of rejecting, since the cycle runs through an opaque
// factory delegate its cycle detector can't see through. See ControlHubConnection's
// class-level remarks.
builder.Services.AddSingleton<ControlHubConnection>();
builder.Services.AddSingleton<IBackendReporter>(services => services.GetRequiredService<ControlHubConnection>());
builder.Services.AddSingleton<IHostedService>(services => services.GetRequiredService<ControlHubConnection>());

var host = builder.Build();
await host.RunAsync();
