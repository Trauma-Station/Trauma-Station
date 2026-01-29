using Content.Shared.Body.Components;
using Content.Shared.Coordinates;
using Content.Shared.Damage.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Trauma.Shared.BloodSplatter;

public sealed class BloodSplatterSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<BloodSplattererComponent, DamageChangedEvent>(OnDamage);
    }

    private void OnDamage(Entity<BloodSplattererComponent> ent, ref DamageChangedEvent args)
    {
        if (!_prototypes.TryIndex(ent.Comp.Entity, out var prototype) ||
            !prototype.TryGetComponent(out var spawner, Factory))
        {
            return;
        }

        if (!args.DamageIncreased || args.DamageDelta == null)
            return;

        if (args.DamageDelta.GetTotal() < ent.Comp.MinimalTriggerDamage)
            return;

        if (!_random.Prob(ent.Comp.Chance))
            return;

        if (!TryComp<BloodstreamComponent>(ent.Owner, out var bloodstream) || bloodstream.BleedAmount <= 1)
            return;

        Spawn(ent.Comp.Entity, ent.Owner.ToCoordinates());
    }
}
