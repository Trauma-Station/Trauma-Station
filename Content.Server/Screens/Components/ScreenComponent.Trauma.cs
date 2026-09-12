namespace Content.Server.Screens.Components;

public sealed partial class ScreenComponent
{
    /// <summary>
    /// Makes this screen ignore network payloads from comms and arrivals shuttle.
    /// </summary>
    [DataField]
    public bool IgnoreNetwork;
}
