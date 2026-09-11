using EasyPanel.Daemon.Features.ComputeExecutableHash;
using EasyPanel.Daemon.Features.FileTransfer;
using EasyPanel.Daemon.Features.LaunchInstance;
using EasyPanel.Daemon.Features.StopInstance;
using EasyPanel.Daemon.Infrastructure;
using EasyPanel.Daemon.Transport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<NodeIdentityOptions>(builder.Configuration.GetSection(NodeIdentityOptions.SectionName));

builder.Services.AddSingleton<LaunchedProcessRegistry>();
builder.Services.AddSingleton<SystemStatsCollector>();
builder.Services.AddSingleton<LaunchInstanceCommandHandler>();
builder.Services.AddSingleton<StopInstanceCommandHandler>();
builder.Services.AddSingleton<ComputeExecutableHashCommandHandler>();

// MVP default, not yet admin-configurable — 20 MB/s per node, shared across all of that
// node's simultaneous transfers (see docs/architecture.md's file transfer section).
builder.Services.AddSingleton(_ => new TokenBucketThrottle(bytesPerSecond: 20 * 1024 * 1024));
builder.Services.AddSingleton<FileTransferCommandHandler>();

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

// Same dual-role singleton pattern as ControlHubConnection, same reason.
builder.Services.AddSingleton<FileTransferHubConnection>();
builder.Services.AddSingleton<IHostedService>(services => services.GetRequiredService<FileTransferHubConnection>());

var host = builder.Build();
await host.RunAsync();
