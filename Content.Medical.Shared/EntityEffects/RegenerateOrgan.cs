// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Medical.Shared.Body;
using Content.Shared.Body;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Medical.Shared.EntityEffects;

/// <summary>
/// Spawns and attaches an organ from the body's initial organs, to this body part entity.
/// </summary>
public sealed partial class RegenerateOrgan : EntityEffectBase<RegenerateOrgan>
{
    /// <summary>
    /// The part slot to regenerate.
    /// It must exist on this part and in the initial body organs.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<OrganCategoryPrototype> Slot;

    /// <summary>
    /// Whether to also regenerate child organs.
    /// </summary>
    [DataField]
    public bool Recursive = true;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-regenerate-part", ("chance", Probability), ("slot", prototype.Index(Slot).Name));
}

public sealed class RegenerateOrganEffectSystem : EntityEffectSystem<BodyPartComponent, RegenerateOrgan>
{
    [Dependency] private readonly BodyPartSystem _part = default!;

    protected override void Effect(Entity<BodyPartComponent> ent, ref EntityEffectEvent<RegenerateOrgan> args)
    {
        var e = args.Effect;
        _part.RestoreInitialChild(ent.AsNullable(), e.Slot, recursive: e.Recursive);
    }
}
