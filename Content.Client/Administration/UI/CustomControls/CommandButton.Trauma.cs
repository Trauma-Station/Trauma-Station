using Content.Client.Administration.Managers;

namespace Content.Client.Administration.UI.CustomControls;

public partial class CommandButton
{
    [Dependency] private IClientAdminManager _admin = default!;

    [ViewVariables(VVAccess.ReadWrite)]
    public string? Command
    {
        get => _command;
        set
        {
            _command = value;
            UpdateConfirmTime();
        }
    }

    private void UpdateConfirmTime()
    {
        ResetTime = string.IsNullOrEmpty(_command) || _admin.IsAnyCommand(_command.Split(' ')[0])
            ? TimeSpan.Zero // no confirming if anyone can run the command, it's probably not dangerous
            : TimeSpan.FromSeconds(2);
    }
}
