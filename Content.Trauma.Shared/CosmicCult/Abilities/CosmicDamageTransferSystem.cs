// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.CosmicCult.Components;
using Content.Shared.Administration.Systems;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Robust.Shared.Audio.Systems;

namespace Content.Trauma.Shared.CosmicCult.Abilities;

public sealed partial class CosmicDamageTransferSystem : EntitySystem
{
    [Dependency] private SharedCosmicCultSystem _cult = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private RejuvenateSystem _rejuvenate = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CosmicLesserCultistComponent, EventCosmicDamageTransfer>(OnTransfer);
    }

    private void OnTransfer(Entity<CosmicLesserCultistComponent> ent, ref EventCosmicDamageTransfer args)
    {
        if (args.Handled || !_cult.EntityIsCultist(args.Target) || !TryComp<DamageableComponent>(args.Target, out var damageComp))
            return;

        args.Handled = true;

        var damage = _damage.GetAllDamage((args.Target, damageComp));
        _damage.TryChangeDamage(ent.Owner, damage, ignoreResistances: true);
        _rejuvenate.PerformRejuvenate(args.Target);

        _audio.PlayPredicted(ent.Comp.TransferSFX, ent, ent);
        if (_net.IsServer) // Predicted spawn looks bad with animations
            PredictedSpawnAtPosition(ent.Comp.TransferVFX, Transform(args.Target).Coordinates);
    }
}
