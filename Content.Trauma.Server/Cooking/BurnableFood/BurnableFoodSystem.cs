// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Temperature.Components;
using Content.Shared.Popups;
using Content.Shared.Temperature;
using Content.Trauma.Shared.BurnableFood;

namespace Content.Trauma.Server.BurnableFood;

public sealed partial class BurnableFoodSystem : EntitySystem
{
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BurnableFoodComponent, OnTemperatureChangeEvent>(OnTempChange);
    }

    private void OnTempChange(Entity<BurnableFoodComponent> ent, ref OnTemperatureChangeEvent args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        if (!TryComp<InternalTemperatureComponent>(ent, out var internalTemperatureComp)
            || internalTemperatureComp.Temperature < ent.Comp.BurnTemp)
            return;

        var originalName = Name(ent);
        var newEnt = SpawnAtPosition(ent.Comp.BurnedFoodPrototype, Transform(ent.Owner).Coordinates);

        _meta.SetEntityName(newEnt, Loc.GetString(ent.Comp.BurnedPrefix, ("name", originalName)));
        _popup.PopupEntity(Loc.GetString(ent.Comp.BurnedPopup, ("name", originalName)), newEnt, PopupType.SmallCaution);

        QueueDel(ent);
    }
}
