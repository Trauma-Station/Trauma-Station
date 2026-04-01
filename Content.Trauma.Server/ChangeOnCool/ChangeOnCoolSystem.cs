// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Temperature.Components;
using Content.Shared.Popups;
using Content.Shared.Temperature;
using Content.Trauma.Shared.ChangeOnCool;
using NetCord.Gateway.Voice;

namespace Content.Trauma.Server.ChangeOnCool;

public sealed partial class ChangeOnCoolSystem : EntitySystem
{

    [Dependency] private readonly EntityQuery<InternalTemperatureComponent> _internalQuery = default!;

    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangeOnCoolComponent, OnTemperatureChangeEvent>(OnTempChange);
    }

    private void OnTempChange(Entity<ChangeOnCoolComponent> ent, ref OnTemperatureChangeEvent args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        if (!_internalQuery.TryComp(ent, out var internalTemp)
            || internalTemp.Temperature > ent.Comp.CoolTemp)
            return;

        var originalName = Name(ent);
        var newEnt = SpawnAtPosition(ent.Comp.CooledPrototype, Transform(ent.Owner).Coordinates);

        _popup.PopupEntity(Loc.GetString(ent.Comp.CooledPopup, ("name", originalName)), newEnt, PopupType.SmallCaution);
        QueueDel(ent);
    }

}
