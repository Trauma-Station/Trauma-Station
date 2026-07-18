// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Power.Components;
using Content.Trauma.Shared.Silicon;
using Content.Trauma.Shared.Silicon.Charge;
using Content.Shared.DoAfter;
using Content.Shared.Popups;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;

namespace Content.Trauma.Shared.Power;

public sealed partial class BatteryDrinkerSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedBatterySystem _battery = default!;
    [Dependency] private PowerCellSystem _powerCell = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BatteryComponent, GetVerbsEvent<AlternativeVerb>>(AddAltVerb);
        SubscribeLocalEvent<PowerCellSlotComponent, GetVerbsEvent<AlternativeVerb>>(AddAltVerb);
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

        var args = new DoAfterArgs(EntityManager, user, doAfterTime, new BatteryDrinkerDoAfterEvent(), user, target)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            Broadcast = false,
            DistanceThreshold = 1.35f,
            RequireCanInteract = true,
            CancelDuplicate = false,
            MultiplyDelay = false,
        };

        _doAfter.TryStartDoAfter(args);
    }

    [SubscribeLocalEvent]
    private void OnDoAfter(Entity<BatteryDrinkerComponent> ent, ref BatteryDrinkerDoAfterEvent args)
    {
        if (args.Cancelled || args.Target is not { } source)
            return;

        if (!TryComp<BatteryDrinkerSourceComponent>(source, out var sourceComp))
        {
            Log.Error($"Somehow drained from invalid battery {ToPrettyString(source)}");
            return;
        }

        var sourceBattery = Comp<BatteryComponent>(source);

        if (!_powerCell.TryGetBatteryFromEntityOrSlot(ent.Owner, out var drinkerBattery))
        {
            Log.Error($"{ToPrettyString(ent)} has no battery!");
            return;
        }

        var networked = sourceBattery.NetSyncEnabled;
        if (!networked && _net.IsClient)
            return; // client cant predict APCs, SMESes, etc just batteries

        var drinkerBatt = drinkerBattery.Value.AsNullable();

        var amountToDrink = ent.Comp.DrinkMultiplier * 1000;

        amountToDrink = MathF.Min(amountToDrink, _battery.GetCharge((source, sourceBattery)));
        amountToDrink = MathF.Min(amountToDrink, drinkerBattery.Value.Comp.MaxCharge - _battery.GetCharge(drinkerBatt));

        if (sourceComp.MaxAmount > 0)
            amountToDrink = MathF.Min(amountToDrink, sourceComp.MaxAmount);

        if (amountToDrink <= 0)
        {
            _popup.PopupEntity(Loc.GetString("battery-drinker-empty", ("target", source)), ent, ent);
            return;
        }

        if (_battery.TryUseCharge((source, sourceBattery), amountToDrink))
            _battery.ChangeCharge(drinkerBatt, amountToDrink);

        _popup.PopupEntity(Loc.GetString("ipc-recharge-tip"), ent, ent, PopupType.SmallCaution);
        if (sourceComp.DrinkSound != null)
        {
            if (networked)
                _audio.PlayPredicted(sourceComp.DrinkSound, source, ent);
            else
                _audio.PlayPvs(sourceComp.DrinkSound, source);
        }
        PredictedSpawnAtPosition("EffectSparks", Transform(source).Coordinates);

        args.Repeat = !_battery.IsFull(drinkerBatt);
    }
}
