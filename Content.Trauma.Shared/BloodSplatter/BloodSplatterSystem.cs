// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Coordinates;
using Content.Shared.Damage.Systems;
using Content.Shared.Gibbing;
using Content.Shared.Spawners.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.BloodSplatter;

public sealed class BloodSplatterSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedBloodstreamSystem _bloodstream = default!;

    private static readonly EntProtoId SlashProto = "Slash";
    private static readonly EntProtoId PierceProto = "Piercing";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BloodSplattererComponent, DamageChangedEvent>(OnDamage);
        SubscribeLocalEvent<BloodSplattererComponent, BeingGibbedEvent>(OnGib);
    }

    private void OnGib(Entity<BloodSplattererComponent> ent, ref BeingGibbedEvent args)
    {
        if (!TryComp<BloodstreamComponent>(ent.Owner, out var bloodstream))
            return;

        SpawnDecal(ent, bloodstream, ent.Comp.GibbedDecal);
    }

    private void OnDamage(Entity<BloodSplattererComponent> ent, ref DamageChangedEvent args)
    {
        var time = _timing.CurTime;

        if (ent.Comp.NextSplashAvailable > time)
            return;

        if (!args.DamageIncreased || args.DamageDelta == null)
            return;

        args.DamageDelta.DamageDict.TryGetValue(PierceProto, out var piercing);
        args.DamageDelta.DamageDict.TryGetValue(SlashProto, out var slash);

        if (args.DamageDelta.GetTotal() < ent.Comp.MinimalTriggerDamage
            || piercing == 0 && slash == 0)
            return;

        if (!TryComp<BloodstreamComponent>(ent.Owner, out var bloodstream)
            || _bloodstream.GetBloodLevel((ent.Owner, bloodstream)) <= 0.5f)
            return;

        ent.Comp.Chance += (float)args.DamageDelta.GetTotal() / 50; // Higher damage has higher change to splatter

        if (ent.Comp.Chance >= 1)
            ent.Comp.Chance = 1;

        if (!_random.Prob(ent.Comp.Chance))
            return;

        SpawnDecal(ent, bloodstream, ent.Comp.Decal);

        ent.Comp.NextSplashAvailable = _timing.CurTime + ent.Comp.SplashCooldown;
    }

    private void SpawnDecal(Entity<BloodSplattererComponent> ent, BloodstreamComponent bloodstream, string decal)
    {
        var entitybloodstream = bloodstream.BloodReferenceSolution;
        var spawnedDecal = EntityManager.CreateEntityUninitialized(decal, ent.Owner.ToCoordinates());

        if (TryComp<RandomDecalSpawnerComponent>(spawnedDecal, out var randomDecal))
        {
            randomDecal.Color = entitybloodstream.GetColor(_prototypes);
        }

        EntityManager.InitializeAndStartEntity(spawnedDecal);
    }
}
