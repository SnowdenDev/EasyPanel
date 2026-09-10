using EasyPanel.Contracts.Enums;

namespace EasyPanel.Contracts.Control.DaemonToBackend;

public sealed record ConsoleOutputLine(
    Guid InstanceId,
    ConsoleStreamKind StreamKind,
    string Text,
    DateTimeOffset TimestampUtc,
    long SequenceNumber
);
