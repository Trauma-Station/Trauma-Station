namespace Content.Shared.Decals;

public sealed partial class Decal
{
    /// <summary>
    /// The decal entity this belongs to.
    /// Default if not spawned yet.
    /// </summary>
    [ViewVariables, NonSerialized]
    public Entity<DecalComponent> Ent = default!;
}
