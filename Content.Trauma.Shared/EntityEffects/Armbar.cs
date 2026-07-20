// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Trauma.Shared.MartialArts.Components;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Adds <see cref="ArmbarredComponent" to the target entity if the effect has a user.
/// </summary>
public sealed partial class Armbar : EntityEffectBase<Armbar>
{
    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;
}

public sealed partial class ArmbarEffectSystem : EntityEffectSystem<TransformComponent, Armbar>
{
    [Dependency] private SharedTransformSystem _transform = default!;

    protected override void Effect(Entity<TransformComponent> ent, ref EntityEffectEvent<Armbar> args)
    {
        if (args.User is not {} user)
            return;

        var comp = EnsureComp<ArmbarredComponent>(ent);
        comp.User = user;
        Dirty(ent, comp);
    }

    // nowhere better to put it
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ArmbarredComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (TerminatingOrDeleted(comp.User) || !_transform.InRange(uid, comp.User, comp.Range))
                RemCompDeferred(uid, comp);
        }
    }
}
