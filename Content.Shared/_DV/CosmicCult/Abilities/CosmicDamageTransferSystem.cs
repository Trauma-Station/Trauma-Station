using Content.Medical.Common.Healing;
using Content.Shared._DV.CosmicCult.Components;
using Content.Shared.Administration.Systems;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;

namespace Content.Shared._DV.CosmicCult.Abilities;

public sealed class CosmicDamageTransferSystem : EntitySystem
{
    [Dependency] private readonly SharedCosmicCultSystem _cult = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly RejuvenateSystem _rejuvenate = default!;

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

        var damage = _damage.GetDamage((args.Target, damageComp));
        _damage.TryChangeDamage(ent.Owner, damage, ignoreResistances: true);
        _rejuvenate.PerformRejuvenate(args.Target);

        _audio.PlayPredicted(ent.Comp.TransferSFX, ent, ent);
        if (_net.IsServer) // Predicted spawn looks bad with animations
            PredictedSpawnAtPosition(ent.Comp.TransferVFX, Transform(args.Target).Coordinates);
    }
}
