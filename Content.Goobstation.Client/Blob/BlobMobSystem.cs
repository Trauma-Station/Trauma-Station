// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Weapons.Melee;
using Content.Goobstation.Shared.Blob;
using Content.Goobstation.Shared.Blob.Components;
using Content.Goobstation.Shared.Blob.Events;
using Robust.Shared.Map;

namespace Content.Goobstation.Client.Blob;

public sealed partial class BlobMobSystem : SharedBlobMobSystem
{
    [Dependency] private MeleeWeaponSystem _melee = default!;

    private static EntProtoId HealEffect = "EffectHealPlusTripleYellow";
    private static readonly EntProtoId Animation = "WeaponArcPunch";

    public override void NodePulse(Entity<BlobMobComponent> ent)
    {
        base.NodePulse(ent);

        SpawnAttachedTo(HealEffect, new EntityCoordinates(ent, Vector2.Zero));
    }

    [SubscribeLocalEvent, SubscribeNetworkEvent]
    private void OnBlobAttack(BlobAttackEvent ev)
    {
        if (!TryGetEntity(ev.BlobEntity, out var user))
            return;

        _melee.DoLunge(user.Value, user.Value, Angle.Zero, ev.Position, Animation, Angle.Zero, false);
    }
}
