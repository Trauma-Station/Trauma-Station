using Content.Shared.Light.Components;
using Content.Shared.Light.EntitySystems;
using Content.Shared.Mobs.Systems;
using Content.Trauma.Shared.CosmicCult;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Trauma.Server.CosmicCult;

public sealed partial class CosmicEffigySystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedPoweredLightSystem _poweredLight = default!;

    private HashSet<Entity<PoweredLightComponent>> _lights = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CosmicEffigyComponent, ComponentStartup>(OnEffigyStarted);
    }

    public override void Update(float frameTime)
    {
        var effigyQuery = EntityQueryEnumerator<CosmicEffigyComponent>();
        while (effigyQuery.MoveNext(out var ent, out var comp))
        {
            if (_timing.CurTime < comp.EffectTimer) continue;
            _audio.PlayPvs(comp.ActivationSfx, ent);
            Spawn(comp.ActivationVfx, Transform(ent).Coordinates);
            comp.EffectTimer = _timing.CurTime + comp.EffectTime;

            comp.SummonedCustodians.RemoveWhere(mob => !Exists(mob) || _mobState.IsDead(mob));
            comp.SummonedLodestars.RemoveWhere(mob => !Exists(mob) || _mobState.IsDead(mob));

            if (comp.SummonedLodestars.Count < comp.LodestarCap && _random.Prob(0.33f)) // 33% for lodestar, 66% for custodian
                comp.SummonedLodestars.Add(Spawn(comp.LodestarProto, Transform(ent).Coordinates));
            else if (comp.SummonedCustodians.Count < comp.CustodianCap)
                comp.SummonedCustodians.Add(Spawn(comp.CustodianProto, Transform(ent).Coordinates));

            _lookup.GetEntitiesInRange<PoweredLightComponent>(Transform(ent).Coordinates, comp.LightShatterRange, _lights);
            foreach (var light in _lights)
                if (_poweredLight.TryDestroyBulb(light))
                    Spawn(comp.ActivationVfx, Transform(light.Owner).Coordinates);
            comp.LightShatterRange += (comp.LightShatterRangeCap - comp.LightShatterRange) / 8f;
        }
    }

    private void OnEffigyStarted(Entity<CosmicEffigyComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.EffectTimer = _timing.CurTime + ent.Comp.EffectTime;
    }
}
