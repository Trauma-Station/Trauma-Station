// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Trauma.Shared.Heretic.Systems.PathSpecific.Blade;

namespace Content.Trauma.Shared.Heretic.EntityEffects;

public sealed partial class AddUserProtectiveBlade : EntityEffectBase<AddUserProtectiveBlade>;

public sealed partial class AddProtectiveBladeEffectSystem : EntityEffectSystem<TransformComponent, AddUserProtectiveBlade>
{
    [Dependency] private ProtectiveBladeSystem _pblade = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<AddUserProtectiveBlade> args)
    {
        if (args.User is not { } user)
            return;

        _pblade.AddProtectiveBlade(user, user);
    }
}
