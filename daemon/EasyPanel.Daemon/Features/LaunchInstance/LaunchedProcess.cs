using System.Diagnostics;
using EasyPanel.Contracts.Control.BackendToDaemon;
using EasyPanel.Daemon.JobObjects;

namespace EasyPanel.Daemon.Features.LaunchInstance;

/// <summary>Command is kept around so a crash-triggered auto-restart can relaunch with the same parameters.</summary>
internal sealed record LaunchedProcess(Process Process, ManagedJobObject JobObject, LaunchInstanceCommand Command);
