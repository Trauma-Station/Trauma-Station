namespace Content.Client.Administration.UI.CustomControls;

public partial class CommandButton
{
    [ViewVariables(VVAccess.ReadWrite)]
    public bool Dangerous { get; set; }

    private void UpdateConfirmTime()
    {
        ResetTime = Dangerous
            ? TimeSpan.FromSeconds(2);
            : TimeSpan.Zero;
    }
}
