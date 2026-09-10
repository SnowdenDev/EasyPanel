using System.Text.Json.Serialization;
using EasyPanel.Contracts.Control.BackendToDaemon;
using EasyPanel.Contracts.Control.DaemonToBackend;
using EasyPanel.Contracts.FileTransfer;

namespace EasyPanel.Contracts.Serialization;

/// <summary>
/// Source-generated JSON metadata for every DTO that crosses the daemon&lt;-&gt;backend
/// SignalR wire. Required because setting PublishAot=true on the daemon disables
/// reflection-based System.Text.Json serialization — not just for an actual AOT publish,
/// but for every `dotnet build`/`dotnet run` too, so the SDK's dev-time behavior matches
/// what a real AOT-published binary would do. Wired into the daemon's HubConnectionBuilder
/// via AddJsonProtocol — see ControlHubConnection.
/// </summary>
[JsonSerializable(typeof(DaemonHeartbeat))]
[JsonSerializable(typeof(InstanceStatusChanged))]
[JsonSerializable(typeof(ConsoleOutputLine))]
[JsonSerializable(typeof(InstanceCrashed))]
[JsonSerializable(typeof(InstanceStatusSnapshot))]
[JsonSerializable(typeof(HashComputationResult))]
[JsonSerializable(typeof(LaunchInstanceCommand))]
[JsonSerializable(typeof(StopInstanceCommand))]
[JsonSerializable(typeof(SendConsoleInputCommand))]
[JsonSerializable(typeof(ComputeExecutableHashCommand))]
[JsonSerializable(typeof(FileTransferRequest))]
[JsonSerializable(typeof(FileTransferMetadata))]
[JsonSerializable(typeof(FileChunk))]
[JsonSerializable(typeof(FileTransferResult))]
[JsonSerializable(typeof(InstanceStatusSnapshot[]))]
[JsonSerializable(typeof(string))]
public partial class ContractsJsonContext : JsonSerializerContext;
