// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Medical.Shared.Body;
using Content.Shared.Body;
using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Removes a body part to the target body part.
/// </summary>
public sealed partial class RemoveBodyPart : EntityEffectBase<RemoveBodyPart>;

public sealed class RemoveBodyPartEffectSystem : EntityEffectSystem<OrganComponent, RemoveBodyPart>
{
    [Dependency] private readonly BodyPartSystem _part = default!;

    protected override void Effect(Entity<OrganComponent> ent, ref EntityEffectEvent<RemoveBodyPart> args)
    {
        if (!TryComp<BodyPartComponent>(ent.Owner, out var bodyComp))
            return;

        var part = bodyComp.Children.LastOrDefault().Value;

        if (part == EntityUid.Invalid)
            return;

        _part.RemoveOrgan(ent.Owner, part);
    }
}
