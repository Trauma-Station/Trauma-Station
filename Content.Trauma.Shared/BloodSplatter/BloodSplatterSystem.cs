// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Vomiting;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Coordinates;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Gibbing;
using Content.Shared.Spawners.Components;
using Content.Shared.Throwing;
using Content.Trauma.Shared.Medical.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.BloodSplatter;

public sealed class BloodSplatterSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedBloodstreamSystem _bloodstream = default!;

    private static readonly ProtoId<DamageTypePrototype> SlashProto = "Slash";
    private static readonly ProtoId<DamageTypePrototype> PierceProto = "Piercing";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BloodSplattererComponent, DamageChangedEvent>(OnDamage);
        SubscribeLocalEvent<BloodSplattererComponent, BeingGibbedEvent>(OnGib);
        SubscribeLocalEvent<BrainSplattererComponent, BeingGibbedEvent>(OnBrainGib);
        SubscribeLocalEvent<BloodSplattererComponent, VomitedEvent>(OnVomit);

        SubscribeLocalEvent<BloodSplatterOnLandComponent, LandEvent>(OnLand);
    }

    private void OnLand(Entity<BloodSplatterOnLandComponent> ent, ref LandEvent args)
    {
        SpawnDecal(ent, ent.Comp.Color, ent.Comp.Decal);

        if (ent.Comp.DeleteEntity)
            PredictedQueueDel(ent);
    }

    private void OnBrainGib(Entity<BrainSplattererComponent> ent, ref BeingGibbedEvent args)
    {
        Spawn(ent.Comp.BrainSplatterDecal, ent.Owner.ToCoordinates());
    }

    private void OnVomit(Entity<BloodSplattererComponent> ent, ref VomitedEvent args)
    {
        Spawn(ent.Comp.VomitDecal, ent.Owner.ToCoordinates());
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

        if (args.DamageDelta.GetTotal() <= ent.Comp.MinorTriggerDamage)
        {
            SpawnDecal(ent, bloodstream, ent.Comp.MinorDecal);
            return;
        }

        SpawnDecal(ent, bloodstream, ent.Comp.Decal);

        ent.Comp.NextSplashAvailable = _timing.CurTime + ent.Comp.SplashCooldown;
    }

    private void SpawnDecal(EntityUid ent, BloodstreamComponent bloodstream, string decal)
    {
        var entitybloodstream = bloodstream.BloodReferenceSolution;
        SpawnDecal(ent, entitybloodstream.GetColor(_prototypes), decal);
    }

    private void SpawnDecal(EntityUid ent, Color color, string decal)
    {
        var spawnedDecal = EntityManager.CreateEntityUninitialized(decal, ent.ToCoordinates());

        if (TryComp<RandomDecalSpawnerComponent>(spawnedDecal, out var randomDecal))
        {
            randomDecal.Color = color;
        }

        EntityManager.InitializeAndStartEntity(spawnedDecal);
    }
}
