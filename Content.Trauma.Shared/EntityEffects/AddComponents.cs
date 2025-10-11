using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Adds components to the target entity.
/// </summary>
public sealed partial class AddComponents : EntityEffect
{
    /// <summary>
    /// Components to add.
    /// </summary>
    [DataField(required: true)]
    public ComponentRegistry Components;

    /// <summary>
    /// Text to use for the guidebook entry for reagents.
    /// </summary>
    [DataField(required: true)]
    public LocId GuidebookText;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString(GuidebookText);

    public override void Effect(EntityEffectBaseArgs args)
    {
        args.EntityManager.AddComponents(args.TargetEntity, Components);
    }
}
