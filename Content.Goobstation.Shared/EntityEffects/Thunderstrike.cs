// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Smites;
using Content.Shared.EntityEffects;

namespace Content.Goobstation.Shared.EntityEffects;

/// <summary>
/// Zaps the target entity and turns it to ash.
/// </summary>
/// <remarks>
/// Kill yourself... NOW!
/// </remarks>
public sealed partial class Thunderstrike : EntityEffectBase<Thunderstrike>
{
    [DataField]
    public bool Kill = true;
}

public sealed partial class ThunderstrikeEffectSystem : EntityEffectSystem<TransformComponent, Thunderstrike>
{
    [Dependency] private ThunderstrikeSystem _thunderstrike = default!;

    protected override void Effect(Entity<TransformComponent> ent, ref EntityEffectEvent<Thunderstrike> args)
    {
        _thunderstrike.Smite(ent.AsNullable(), args.Effect.Kill, args.Predicted, args.User);
    }
}
