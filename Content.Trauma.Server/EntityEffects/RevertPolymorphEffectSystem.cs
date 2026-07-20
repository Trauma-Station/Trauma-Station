// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Polymorph.Components;
using Content.Server.Polymorph.Systems;
using Content.Shared.EntityEffects;
using Content.Trauma.Shared.EntityEffects;

namespace Content.Trauma.Server.EntityEffects;

public sealed partial class RevertPolymorphEffectSystem : EntityEffectSystem<PolymorphedEntityComponent, RevertPolymorph>
{
    [Dependency] private PolymorphSystem _polymorph = default!;

    protected override void Effect(Entity<PolymorphedEntityComponent> ent, ref EntityEffectEvent<RevertPolymorph> args)
    {
        _polymorph.Revert(ent.AsNullable());
    }
}
