namespace Content.Trauma.Server.Heretic.Components;

[RegisterComponent]
public sealed partial class HereticSacrificeTargetComponent : Component
{
    [DataField]
    public HashSet<EntityUid> HereticMinds = new();
}
