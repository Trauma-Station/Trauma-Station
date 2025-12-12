// didn't even put the original authors :face_holding_back_tears:
// SPDX-FileCopyrightText: 2024 gluesniffler <159397573+gluesniffler@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aviu00 <93730715+Aviu00@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Piras314 <p1r4s@proton.me>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DoAfter;
using Content.Shared.PowerCell.Components;
using Content.Shared._EinsteinEngines.Silicon;
using Content.Shared._EinsteinEngines.Silicon.Charge;
using Content.Shared.Verbs;
using Robust.Shared.Utility;
using Content.Shared.Power.EntitySystems;
using Content.Shared.PowerCell;
using Content.Shared.Popups;
using Content.Shared.Power.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
// Goobstation Start - Energycrit
using Content.Shared._EinsteinEngines.Power.Components;
using Content.Shared.Whitelist;
// Goobstation End

namespace Content.Shared._EinsteinEngines.Power;

public sealed class BatteryDrinkerSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly PredictedBatterySystem _battery = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!; // Goobstation - Energycrit

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PredictedBatteryComponent, GetVerbsEvent<AlternativeVerb>>(AddAltVerb);
        SubscribeLocalEvent<PowerCellSlotComponent, GetVerbsEvent<AlternativeVerb>>(AddAltVerb); // Goobstation - Energycrit

        SubscribeLocalEvent<BatteryDrinkerComponent, BatteryDrinkerDoAfterEvent>(OnDoAfter);
    }

    private void AddAltVerb<TComp>(EntityUid uid, TComp component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!TryComp<BatteryDrinkerComponent>(args.User, out var drinkerComp) ||
            // Goobstation Start - Energycrit
            _whitelist.IsWhitelistPass(drinkerComp.Blacklist, uid) ||
            !_powerCell.TryGetBatteryFromEntityOrSlot(args.User, out _) ||
            !_powerCell.TryGetBatteryFromEntityOrSlot(uid, out var battery) ||
            !HasComp<BatteryDrinkerSourceComponent>(battery.Value)) // can't eat literally any battery
            // Goobstation End - Energycrit
            return;

        AlternativeVerb verb = new()
        {
            // Goobstation - Energycrit
            Act = () => DrinkBattery(battery.Value, args.User, drinkerComp),
            Text = Loc.GetString("battery-drinker-verb-drink"),
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/smite.svg.192dpi.png")),
            // Goobstation - Energycrit: dont block removing power cells
            Priority = -5
        };

        args.Verbs.Add(verb);
    }

    private void DrinkBattery(EntityUid target, EntityUid user, BatteryDrinkerComponent drinkerComp)
    {
        if (!TryComp<BatteryDrinkerSourceComponent>(target, out var sourceComp))
            return;

        var doAfterTime = drinkerComp.DrinkSpeed * sourceComp.DrinkSpeedMulti;

        var args = new DoAfterArgs(EntityManager, user, doAfterTime, new BatteryDrinkerDoAfterEvent(), user, target) // TODO: Make this doafter loop, once we merge Upstream.
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            Broadcast = false,
            DistanceThreshold = 1.35f,
            RequireCanInteract = true,
            CancelDuplicate = false,
            MultiplyDelay = false, // Goobstation
        };

        _doAfter.TryStartDoAfter(args);
    }

    private void OnDoAfter(EntityUid uid, BatteryDrinkerComponent drinkerComp, DoAfterEvent args)
    {
        if (args.Cancelled || args.Target is not {} source)
            return;

        if (!TryComp<BatteryDrinkerSourceComponent>(source, out var sourceComp))
            return;

        var sourceBattery = Comp<PredictedBatteryComponent>(source);

        var drinker = uid;
        if (!_powerCell.TryGetBatteryFromEntityOrSlot(drinker, out var drinkerBattery))
            return;

        var drinkerBatt = drinkerBattery.Value.AsNullable();

        var amountToDrink = drinkerComp.DrinkMultiplier * 1000;

        amountToDrink = MathF.Min(amountToDrink, _battery.GetCharge((source, sourceBattery)));
        amountToDrink = MathF.Min(amountToDrink, drinkerBattery.Value.Comp.MaxCharge - _battery.GetCharge(drinkerBatt));

        if (sourceComp.MaxAmount > 0)
            amountToDrink = MathF.Min(amountToDrink, (float) sourceComp.MaxAmount);

        if (amountToDrink <= 0)
        {
            _popup.PopupClient(Loc.GetString("battery-drinker-empty", ("target", source)), drinker, drinker);
            return;
        }

        if (_battery.TryUseCharge((source, sourceBattery), amountToDrink))
            _battery.ChangeCharge(drinkerBatt, amountToDrink);

        if (sourceComp != null && sourceComp.DrinkSound != null)
        {
            _popup.PopupClient(Loc.GetString("ipc-recharge-tip"), drinker, drinker, PopupType.SmallCaution);
            _audio.PlayPredicted(sourceComp.DrinkSound, source, drinker);
            PredictedSpawnAtPosition("EffectSparks", Transform(source).Coordinates);
        }
    }
}
