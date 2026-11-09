using Content.Shared.IdentityManagement;
using Content.Shared.Mobs;
using Content.Shared.Polymorph.Systems;
using Content.Shared.Popups;
using Content.Shared.Station.Systems;
using Content.Trauma.Shared.Teleportation;

namespace Content.Trauma.Shared.Megafauna.FrostMiner;

public sealed partial class ResurrectionCrystalSystem : EntitySystem
{
    [Dependency] private SharedPolymorphSystem _polymorph = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private StationSystem _station = default!;
    [Dependency] private TeleportSystem _teleport = default!;

    [SubscribeLocalEvent]
    private void OnMobState(Entity<ResurrectionCrystalComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        if (!_station.TryFindRandomTile(out _, out _, out _, out var coords))
            return;

        _teleport.Teleport(ent, coords, ent);
        if (_polymorph.PolymorphEntity(ent, ent.Comp.Polymorph) is not { } newEnt)
            return;

        // Notify them so we don't get insta-KOS in case we have a bad polymorph lol...
        _popup.PopupEntity(
            "The crystal saves you from death!",
            $"{Identity.Name(ent, EntityManager)} got saved by the crystal!",
            newEnt,
            newEnt,
            PopupType.MediumCaution);
    }
}
