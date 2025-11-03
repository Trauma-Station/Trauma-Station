using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Removes components from the target entity.
/// </summary>
public sealed partial class RemoveComponents : EntityEffectBase<RemoveComponents>
{
    /// <summary>
    /// Components to remove.
    /// </summary>
    [DataField(required: true)]
    public ComponentRegistry Components;

    /// <summary>
    /// Text to use for the guidebook entry for reagents.
    /// </summary>
    [DataField(required: true)]
    public LocId GuidebookText;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString(GuidebookText, ("chance", Probability));
}

public sealed class RemoveComponentsEffectSystem : EntityEffectSystem<MetaDataComponent, RemoveComponents>
{
    protected override void Effect(Entity<MetaDataComponent> ent, ref EntityEffectEvent<RemoveComponents> args)
    {
        EntityManager.RemoveComponents(ent, args.Effect.Components);
    }
}
