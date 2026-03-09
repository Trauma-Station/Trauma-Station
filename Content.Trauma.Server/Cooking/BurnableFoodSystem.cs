// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Popups;
using Content.Server.Temperature.Components;
using Content.Shared.Temperature;
using Content.Trauma.Shared.BurnableFood;

namespace Content.Trauma.Server.BurnableFood;

public sealed partial class BurnableFoodSystem : EntitySystem
{
    [Dependency] private readonly MetaDataSystem _metaSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<BurnableFoodComponent, OnTemperatureChangeEvent>(OnTempChange);
    }

    private void OnTempChange(Entity<BurnableFoodComponent> ent, ref OnTemperatureChangeEvent args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        if (!TryComp<InternalTemperatureComponent>(ent, out var internalTemperatureComp)
            || internalTemperatureComp.Temperature < ent.Comp.BurnTemp)
            return;

        var originalName = MetaData(ent).EntityName;
        var newEnt = SpawnAtPosition(ent.Comp.BurnedFoodPrototype, Transform(ent.Owner).Coordinates);

        _metaSystem.SetEntityName(newEnt, Loc.GetString(ent.Comp.BurnedPrefix, ("name", originalName)));
        _popupSystem.PopupEntity(Loc.GetString(ent.Comp.BurnedPopup, ("name", originalName)), newEnt, PopupType.SmallCaution);

        QueueDel(ent);
    }
}
