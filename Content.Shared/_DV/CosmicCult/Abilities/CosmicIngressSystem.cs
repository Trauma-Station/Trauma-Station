using Content.Shared.Doors.Systems;
using Content.Shared._DV.CosmicCult;
using Content.Shared._DV.CosmicCult.Components;
using Content.Shared.Doors.Components;
using Content.Shared.Lock;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;

namespace Content.Shared._DV.CosmicCult.Abilities;

public sealed class CosmicIngressSystem : EntitySystem
{
    [Dependency] private readonly SharedCosmicCultSystem _cult = default!;
    [Dependency] private readonly SharedDoorSystem _door = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly LockSystem _lock = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CosmicCultComponent, EventCosmicIngress>(OnCosmicIngress);
    }

    private void OnCosmicIngress(Entity<CosmicCultComponent> ent, ref EventCosmicIngress args)
    {
        var target = args.Target;

        if (args.Handled)
            return;

        if (TryComp<DoorComponent>(target, out var doorComp))
        {
            if (TryComp<DoorBoltComponent>(target, out var boltComp) && boltComp.BoltsDown)
            {
                if (!ent.Comp.CosmicEmpowered)
                {
                    _popup.PopupClient(Loc.GetString("cosmicability-ingress-not-empowered-door"), ent, ent);
                    return;
                }
                _door.SetBoltsDown((target, boltComp), false, user: ent, predicted: true);
            }
            _door.StartOpening(target, doorComp, user: ent, predicted: true);
        }
        else if (TryComp<LockComponent>(target, out var lockComp))
            _lock.Unlock(target, ent, lockComp);

        args.Handled = true;
        _audio.PlayPredicted(ent.Comp.IngressSFX, ent, ent);
        PredictedSpawnAtPosition(ent.Comp.AbsorbVFX, Transform(target).Coordinates);
        _cult.MalignEcho(ent);
    }
}
