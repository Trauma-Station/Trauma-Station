namespace Content.Server.Administration.Managers;

public sealed partial class AdminManager
{
    bool IAdminManager.CanAnyoneRunCommand(string name)
        => _commandPermissions.AnyCommands.Contains(name);
}
