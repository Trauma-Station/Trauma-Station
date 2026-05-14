// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Shared.Database;
using Robust.Shared.Console;

namespace Content.Trauma.Server.Administration;

/// <summary>
/// Logs admin commands used to the AdminCommands admin log category.
/// </summary>
public sealed partial class CommandLoggingSystem : EntitySystem
{
    [Dependency] private IAdminManager _admin = default!;
    [Dependency] private IAdminLogManager _adminLog = default!;
    [Dependency] private IConsoleHost _console = default!;

    public static readonly Dictionary<string, LogImpact> CommandImpacts = new()
    {
        // Extreme
        {"cvar", LogImpact.Extreme},
        {"endround", LogImpact.Extreme},
        {"golobby", LogImpact.Extreme},
        {"restartround", LogImpact.Extreme},
        {"restartroundnow", LogImpact.Extreme},
        // High
        {"ban", LogImpact.High},
        {"pardon", LogImpact.High},
        {"playglobalsound", LogImpact.High}, // chudmin
        {"readmin", LogImpact.High}, // heres to you yonsim
        // Medium
        {"aghost", LogImpact.Medium},
        {"delaystart", LogImpact.Medium},
        {"startround", LogImpact.Medium}
    };
    public static readonly List<string> Ignored = new()
    {
        "adminlogs" // yeah... dont need to spam db for this
    };
    public const LogImpact DefaultImpact = LogImpact.Low;

    public override void Initialize()
    {
        base.Initialize();

        _console.AnyCommandExecuted += OnAnyCommandExecuted;
    }

    private void OnAnyCommandExecuted(IConsoleShell shell, string command, string argStr, string[] args)
    {
        // ignore non-admins to avoid spamming/DOS
        if (shell.Player is { } player && _admin.GetAdminData(player) == null ||
            Ignored.Contains(command))
            return;

        if (!CommandImpacts.TryGetValue(command, out var impact))
            impact = DefaultImpact;
        _adminLog.Add(LogType.AdminCommands, impact, $"Admin {shell.Player} ran command '{argStr}'");
    }
}
