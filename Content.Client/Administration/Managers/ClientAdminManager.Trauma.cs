namespace Content.Client.Administration.Managers;

public sealed partial class ClientAdminManager
{
    /// <summary>
    /// Returns true if anyone is allowed to run this command.
    /// </summary>
    bool IClientAdminManager.IsAnyCommand(string cmdName)
        => _localCommandPermissions.AnyCommands.Contains(cmdName);
}
