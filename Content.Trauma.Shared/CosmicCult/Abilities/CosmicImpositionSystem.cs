// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage.Systems;
using Content.Shared.Damage.Events;
using Content.Trauma.Common.CosmicCult.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;
using Robust.Shared.Network;

namespace Content.Trauma.Shared.CosmicCult.Abilities;

public sealed class CosmicImpositionSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CosmicImposingComponent, BeforeDamageChangedEvent>(OnImpositionDamaged);
        SubscribeLocalEvent<CosmicImposingComponent, BeforeStaminaDamageEvent>(OnImpositionStaminaDamaged);
        SubscribeLocalEvent<CosmicCultistComponent, EventCosmicImposition>(OnCosmicImposition);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CosmicImposingComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime < comp.Expiry)
                continue;

            RemComp(uid, comp);
        }
    }

    private void OnCosmicImposition(Entity<CosmicCultistComponent> uid, ref EventCosmicImposition args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        args.Handled = true;

        EnsureComp<CosmicImposingComponent>(uid, out var comp);
        comp.Expiry = _timing.CurTime + uid.Comp.CosmicImpositionDuration;
        _audio.PlayPvs(uid.Comp.ImpositionSFX, uid, AudioParams.Default.WithVariation(0.05f));
        if (_net.IsServer) // Predicted spawn looks bad with animations
            PredictedSpawnAtPosition(uid.Comp.ImpositionVFX, Transform(uid).Coordinates);
    }

    private void OnImpositionDamaged(Entity<CosmicImposingComponent> uid, ref BeforeDamageChangedEvent args)
    {
        args.Cancelled = true;
    }

    private void OnImpositionStaminaDamaged(Entity<CosmicImposingComponent> uid, ref BeforeStaminaDamageEvent args)
    {
        args.Cancelled = true;
    }
}
