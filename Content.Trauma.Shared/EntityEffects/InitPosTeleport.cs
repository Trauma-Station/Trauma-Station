// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.InitPos;
using Content.Trauma.Shared.Teleportation.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.Hands.EntitySystems;

namespace Content.Trauma.Shared.Teleportation;

public sealed partial class InitPosTeleport : EntityEffectBase<InitPosTeleport>;

public sealed partial class InitPosTeleportEffectSystem : EntityEffectSystem<TransformComponent, InitPosTeleport>
{
    [Dependency] private TeleportSystem _teleport = default!;
    [Dependency] private SharedHandsSystem _hands = default!;

    protected override void Effect(Entity<TransformComponent> ent, ref EntityEffectEvent<InitPosTeleport> args)
    {
        Log.Info($"start");
        var handEntity = _hands.GetActiveItemOrSelf(ent.Owner);
        Log.Info($"{handEntity}");
        if (!TryComp<InitPosComponent>(handEntity, out var pos))
            return;
        Log.Info($"{pos}");
        _teleport.Teleport(ent.Owner, pos.Coordinates, user: args.User); // how the fuck can i relay the event???
    }
}
