namespace Content.Shared.GameTicking.Rules.Components;

public sealed partial class NukeopsRuleComponent
{
    /// <summary>
    /// The ratio of players per nuclear operative for war declaration scaling.
    /// Example: A value of 10 means one operative per 10 players.
    /// </summary>
    [DataField]
    public int WarNukiePlayerRatio = 12;

    /// <summary>
    /// Additional telecrystals granted per player on the server during war.
    /// Total bonus is divided by number of operatives.
    /// </summary>
    [DataField]
    public int WarTcPerPlayer = 20;

    /// <summary>
    /// Compensation telecrystals granted per missing nuclear operative.
    /// Total bonus is divided by number of operatives.
    /// </summary>
    [DataField]
    public int WarTcPerNukieMissing = 100;

    [DataField]
    public string LocalePrefix = "nukeops-";
}
