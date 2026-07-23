namespace Content.Server.GameTicking.Rules.Components;

public sealed partial class NukeopsRuleComponent
{
    /// <summary>
    /// The amount of players alive on spawn, used for the antag or shuttle call.
    /// </summary>
    [DataField]
    public int AmountAliveOnSpawn;
}
