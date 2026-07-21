using Content.Shared.Body;
using Content.Shared.Damage.Systems;
using Robust.Shared.Audio.Systems;

namespace Content.Trauma.Shared.Body.Part;

public sealed partial class BurnableWingsSystem : EntitySystem
{
    [Dependency] private DamageableSystem _dmg = default!;
    [Dependency] private BodySystem _body = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    [SubscribeLocalEvent]
    private void OnDamageChanged(Entity<BurnableWingsComponent> ent, ref DamageDealtEvent args)
    {
        var allDmg = _dmg.GetAllDamage(ent.Owner);
        if (!allDmg.DamageDict.TryGetValue(ent.Comp.DamageType, out var dmg) || dmg < ent.Comp.DamageThreshold)
            return;

        var coords = Transform(ent).Coordinates;
        _audio.PlayPredicted(ent.Comp.BurnSound, coords, args.Origin);
        var newWings = PredictedSpawnAtPosition(ent.Comp.BurntWings, coords);

        if (TryComp(ent, out OrganComponent? organ) && Exists(organ.Body))
        {
            var body = organ.Body.Value;
            if (_body.RemoveOrgan(body, (ent, organ)))
                _body.InsertOrgan(body, newWings);
        }

        PredictedQueueDel(ent);
    }
}
