// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.CosmicCult;
using Content.Trauma.Shared.CosmicCult.Components;
using Content.Shared.Damage.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.CosmicCult.Abilities;

public sealed partial class CosmicImpositionSystem : EntitySystem
{
    [Dependency] private SharedCosmicCultSystem _cult = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<CosmicImposingComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (now < comp.Expiry)
                continue;

            RemComp(uid, comp);
        }
    }

    [SubscribeLocalEvent]
    private void OnCosmicImposition(Entity<CosmicCultComponent> ent, ref CosmicImpositionEvent args)
    {
        var comp = EnsureComp<CosmicImposingComponent>(ent);
        comp.Expiry = _timing.CurTime + ent.Comp.CosmicImpositionDuration;
        if (_net.IsServer)
            Spawn(ent.Comp.ImpositionVFX, Transform(ent).Coordinates);
        args.Handled = true;
        _audio.PlayPredicted(ent.Comp.ImpositionSFX, ent, ent, AudioParams.Default.WithVariation(0.05f));
        _cult.MalignEcho(ent);
    }

    [SubscribeLocalEvent]
    private void OnBeforeDamageChanged(Entity<CosmicImposingComponent> ent, ref BeforeDamageChangedEvent args)
    {
        args.Cancelled = true;
    }
}
