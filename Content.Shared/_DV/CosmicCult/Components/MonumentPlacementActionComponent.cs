namespace Content.Shared._DV.CosmicCult.Components;

[RegisterComponent]
public sealed partial class MonumentPlacementActionComponent : Component
{
    /// <summary>
    /// The mark created by this action. If not null, using the action would revoke the mark instead.
    /// </summary>
    [DataField]
    public EntityUid? Mark;
}
