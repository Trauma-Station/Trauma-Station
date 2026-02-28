using Content.Shared._DV.CosmicCult;
using Content.Shared._DV.CosmicCult.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Server._DV.CosmicCult.Abilities;

public sealed class CosmicStrideSystem : EntitySystem
{
    [Dependency] private readonly CosmicCultSystem _cult = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CosmicCultComponent, EventCosmicStride>(OnCosmicImposition);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<InfluenceStrideComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime < comp.Expiry)
                continue;

            RemCompDeferred(uid, comp);
        }
    }

    private void OnCosmicImposition(Entity<CosmicCultComponent> uid, ref EventCosmicStride args)
    {
        EnsureComp<InfluenceStrideComponent>(uid, out var comp);
        comp.Expiry = _timing.CurTime + uid.Comp.CosmicStrideDuration;
        Spawn(uid.Comp.ImpositionVFX, Transform(uid).Coordinates); // Uses imposition VFX because why not
        args.Handled = true;
        _audio.PlayPvs(uid.Comp.StrideSFX, uid, AudioParams.Default.WithVariation(0.05f));
        _cult.MalignEcho(uid);
    }
}
