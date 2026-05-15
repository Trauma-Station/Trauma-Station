// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Trauma.Shared.Grab;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Grab-throws the target mob away from the user, speed scales with effect scale.
/// Does nothing if there is no user or it's at the same position as the target.
/// </summary>
public sealed partial class GrabThrow : EntityEffectBase<GrabThrow>
{
    [DataField]
    public float Speed = 5f;

    /// <summary>
    /// Damage dealt after hitting a wall.
    /// </summary>
    [DataField]
    public DamageSpecifier? Damage;

    [DataField]
    public bool DropItems = true;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null; // not used by reagents idc
}

public sealed partial class GrabThrowEffectSystem : EntityEffectSystem<TransformComponent, GrabThrow>
{
    [Dependency] private GrabThrownSystem _grabThrown = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    protected override void Effect(Entity<TransformComponent> ent, ref EntityEffectEvent<GrabThrow> args)
    {
        if (args.User is not {} user)
            return;

        var pos = _transform.GetMapCoordinates(ent.Comp).Position;
        var userPos = _transform.GetMapCoordinates(user).Position;
        if (pos == userPos)
            return;

        var direction = (pos - userPos).Normalized();

        var e = args.Effect;
        _grabThrown.Throw(ent,
            user,
            direction,
            e.Speed * args.Scale,
            damage: e.Damage,
            drop: e.DropItems);
    }
}
