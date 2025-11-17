using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Adds an organ slot to the target entity, which must be a body part.
/// </summary>
public sealed partial class AddOrganSlot : EntityEffectBase<AddOrganSlot>
{
    [DataField(required: true)]
    public string Slot = string.Empty;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-part-add-slot", ("chance", Probability), ("slot", Slot));
}

public sealed class AddOrganSlotEffectSystem : EntityEffectSystem<BodyPartComponent, AddOrganSlot>
{
    [Dependency] private readonly SharedBodySystem _body = default!;

    protected override void Effect(Entity<BodyPartComponent> ent, ref EntityEffectEvent<AddOrganSlot> args)
    {
        _body.CreateOrganSlot((ent, ent.Comp), args.Effect.Slot);
    }
}
