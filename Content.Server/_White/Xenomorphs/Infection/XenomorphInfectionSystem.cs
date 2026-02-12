using Content.Shared._White.Xenomorphs.Infection;
using Content.Shared._White.Xenomorphs.Larva;
using Content.Shared.Body;
using Content.Shared.EntityEffects;
using Content.Shared.Mobs.Systems;
using Robust.Server.Containers;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._White.Xenomorphs.Infection;

public sealed class XenomorphInfectionSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _effects = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenomorphInfectionComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<XenomorphInfectionComponent, OrganGotInsertedEvent>(OnOrganInserted);
        SubscribeLocalEvent<XenomorphInfectionComponent, OrganGotRemovedEvent>(OnOrganRemoved);
    }

    private void OnShutdown(EntityUid uid, XenomorphInfectionComponent component, ComponentShutdown args)
    {
        if (component.Infected.HasValue)
            RemComp<XenomorphInfectedComponent>(component.Infected.Value);
    }

    private void OnOrganInserted(EntityUid uid, XenomorphInfectionComponent component, ref OrganGotInsertedEvent args)
    {
        var xenomorphInfected = EnsureComp<XenomorphInfectedComponent>(args.Target);
        xenomorphInfected.Infection = uid;
        xenomorphInfected.InfectedIcons = component.InfectedIcons;
        Dirty(args.Target, xenomorphInfected);

        component.Infected = args.Target;
    }

    private void OnOrganRemoved(EntityUid uid, XenomorphInfectionComponent component, ref OrganGotRemovedEvent args)
    {
        RemComp<XenomorphPreventSuicideComponent>(args.Target);
        RemComp<XenomorphInfectedComponent>(args.Target);
        component.Infected = null;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var time = _timing.CurTime;

        var query = EntityQueryEnumerator<XenomorphInfectionComponent>();
        while (query.MoveNext(out var uid, out var infection))
        {
            if (!infection.Infected.HasValue || infection.GrowthStage >= infection.MaxGrowthStage || time < infection.NextPointsAt)
                continue;

            infection.NextPointsAt = time + infection.GrowTime;

            if (_mobState.IsDead(infection.Infected.Value) || !_random.Prob(infection.GrowProb))
                continue;

            infection.GrowthStage++;
            if (TryComp<XenomorphInfectedComponent>(infection.Infected.Value, out var xenomorphInfected))
            {
                xenomorphInfected.GrowthStage = infection.GrowthStage;
                DirtyField(infection.Infected.Value, xenomorphInfected, nameof(XenomorphInfectedComponent.GrowthStage));
            }

            if (infection.Effects.TryGetValue(infection.GrowthStage, out var effects))
            {
                _effects.ApplyEffects(infection.Infected.Value, effects);
            }

            if (infection.GrowthStage < infection.MaxGrowthStage)
                continue;

            if (!_container.TryGetContainingContainer((uid, null, null), out var container))
            {
                QueueDel(uid);
                continue;
            }

            var larva = Spawn(infection.LarvaPrototype);

            var larvaComponent = EnsureComp<XenomorphLarvaComponent>(larva);
            larvaComponent.Victim = infection.Infected.Value;

            var larvaVictim = EnsureComp<XenomorphLarvaVictimComponent>(infection.Infected.Value);
            if (infection.InfectedIcons.TryGetValue(infection.GrowthStage, out var infectedIcon))
            {
                larvaVictim.InfectedIcon = infectedIcon;
                Dirty(infection.Infected.Value, larvaVictim);
            }

            _container.Remove(uid, container);
            _container.Insert(larva, container);

            QueueDel(uid);
        }
    }
}
