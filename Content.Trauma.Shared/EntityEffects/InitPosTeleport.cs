// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.InitPos;
using Content.Trauma.Shared.Teleportation.Systems;
using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.Teleportation;

public sealed partial class InitPosTeleport : EntityEffectBase<InitPosTeleport>;

public sealed partial class InitPosTeleportEffectSystem : EntityEffectSystem<InitPosComponent, InitPosTeleport>
{
    [Dependency] private TeleportSystem _teleport = default!;

    protected override void Effect(Entity<InitPosComponent> ent, ref EntityEffectEvent<InitPosTeleport> args)
    {
        _teleport.Teleport(ent.Owner, ent.Comp.Coordinates, user: args.User);
    }
}
