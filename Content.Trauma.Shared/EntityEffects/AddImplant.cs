// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Adds an implant to the target entity
/// Can implant anything so make sure it's a mob yourself.
/// </summary>
public sealed partial class AddImplant : EntityEffectBase<AddImplant>
{
    /// <summary>
    /// The implant entity to add.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId<SubdermalImplantComponent> Implant;
}

public sealed partial class AddImplantEffectSystem : EntityEffectSystem<TransformComponent, AddImplant>
{
    [Dependency] private SharedSubdermalImplantSystem _implant = default!;

    protected override void Effect(Entity<TransformComponent> ent, ref EntityEffectEvent<AddImplant> args)
    {
        _implant.AddImplant(ent.Owner, args.Effect.Implant.Id);
    }
}
