namespace Content.Trauma.Client.Heretic;

[RegisterComponent]
public sealed partial class HereticSacrificeTargetComponent : Component
{
    [DataField]
    public TimeSpan RemovalTimer;

    [DataField]
    public TimeSpan RemovalTime = TimeSpan.FromSeconds(2);
}
