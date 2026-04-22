// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Shared.Body;
using Content.Shared.Body;
using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Adds a body part to the target body part.
/// </summary>
public sealed partial class AddBodyPart : EntityEffectBase<AddBodyPart>;

public sealed class AddBodyPartEffectSystem : EntityEffectSystem<OrganComponent, AddBodyPart>
{
    [Dependency] private readonly BodyPartSystem _part = default!;

    protected override void Effect(Entity<OrganComponent> ent, ref EntityEffectEvent<AddBodyPart> args)
    {
        if (args.User is not { } part)
        {
            Log.Error("AddBodyPart effect missing actual part. Check the yaml.");
            return;
        }

        if (!TryComp<OrganComponent>(part, out var organComp) || organComp.Category is not { } organCategory)
            return;

        if (!_part.InsertOrgan(ent.Owner, part))
            _part.TryAddSlot(ent.Owner, organCategory);
    }
}
