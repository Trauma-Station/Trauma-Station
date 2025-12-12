using Content.Shared.Alert;
using Content.Shared.Body.Systems;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Content.Shared._Shitmed.Body.Organ;
using Robust.Shared.Timing;

namespace Content.Goobstation.Shared.Augments;

public sealed class AugmentPowerCellSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly AugmentSystem Augment = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MobStateSystem _mob = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly PredictedBatterySystem _battery = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private EntityQuery<PowerCellDrawComponent> _drawQuery;

    private TimeSpan _nextUpdate = TimeSpan.Zero;
    private static readonly TimeSpan _updateDelay = TimeSpan.FromSeconds(2);

    public override void Initialize()
    {
        base.Initialize();

        _drawQuery = GetEntityQuery<PowerCellDrawComponent>();

        SubscribeLocalEvent<AugmentPowerCellSlotComponent, OrganEnableChangedEvent>(OnEnableChanged);
        SubscribeLocalEvent<AugmentPowerCellSlotComponent, PowerCellSlotEmptyEvent>(OnCellEmpty);

        SubscribeLocalEvent<HasAugmentPowerCellSlotComponent, FindBatteryEvent>(OnFindBattery);
        SubscribeLocalEvent<HasAugmentPowerCellSlotComponent, AugmentBatteryAlertEvent>(OnBatteryAlert);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // don't need to burn server tps on alerts
        var now = _timing.CurTime;
        if (now < _nextUpdate)
            return;

        _nextUpdate = now + _updateDelay;

        var query = EntityQueryEnumerator<HasAugmentPowerCellSlotComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            if (_mob.IsDead(uid) || GetBodyAugment(uid) is not {} augment)
                continue;

            if (!_powerCell.TryGetBatteryFromSlot(augment.Owner, out var battery))
            {
                if (_alerts.IsShowingAlert(uid, augment.Comp.BatteryAlert))
                {
                    _alerts.ClearAlert(uid, augment.Comp.BatteryAlert);
                    _alerts.ShowAlert(uid, augment.Comp.NoBatteryAlert);
                }
                continue;
            }

            _alerts.ClearAlert(uid, augment.Comp.NoBatteryAlert);

            var batt = battery.Value;
            // number from 0-10... it works
            var chargePercent = (short) _battery.GetMaxUses(batt.AsNullable(), batt.Comp.MaxCharge * 0.1f);
            _alerts.ShowAlert(uid, augment.Comp.BatteryAlert, chargePercent);
        }
    }

    private void OnEnableChanged(Entity<AugmentPowerCellSlotComponent> ent, ref OrganEnableChangedEvent args)
    {
        if (!_drawQuery.TryComp(ent, out var draw))
            return;

        UpdateDrawRate((ent, draw));

        _powerCell.SetDrawEnabled(ent.Owner, args.Enabled);
        if (Augment.GetBody(ent) is not {} body)
            return;

        if (args.Enabled && _powerCell.HasDrawCharge((ent.Owner, draw)))
        {
            var ev = new AugmentGainedPowerEvent(body);
            Augment.RelayEvent(body, ref ev);
        }
        else
        {
            var ev = new AugmentLostPowerEvent(body);
            Augment.RelayEvent(body, ref ev);
        }
    }

    private void OnCellEmpty(Entity<AugmentPowerCellSlotComponent> ent, ref PowerCellSlotEmptyEvent args)
    {
        if (Augment.GetBody(ent) is not {} body)
            return;

        var ev = new AugmentLostPowerEvent(body);
        Augment.RelayEvent(body, ref ev);

        // stop drawing if it loses power
        UpdateDrawRate(ent.Owner);
    }

    private void OnFindBattery(Entity<HasAugmentPowerCellSlotComponent> ent, ref FindBatteryEvent args)
    {
        args.FoundBattery ??= GetBodyCell(ent);
    }

    private void OnBatteryAlert(Entity<HasAugmentPowerCellSlotComponent> ent, ref AugmentBatteryAlertEvent args)
    {
        var user = args.User;
        if (GetBodyAugment(ent) is not {} augment || !_powerCell.TryGetBatteryFromSlot(augment.Owner, out var battery))
        {
            _popup.PopupClient(Loc.GetString("power-cell-no-battery"), user, user, PopupType.MediumCaution);
            return;
        }

        var batt = battery.Value;
        var percent = _battery.GetMaxUses(batt.AsNullable(), batt.Comp.MaxCharge * 0.01f);
        var draw = CompOrNull<PowerCellDrawComponent>(augment)?.DrawRate ?? 0f;
        _popup.PopupClient(Loc.GetString("augments-power-cell-info", ("percent", percent), ("draw", draw)), user, user);
    }

    public float GetBodyDraw(EntityUid body)
    {
        var ev = new GetAugmentsPowerDrawEvent(body);
        Augment.RelayEvent(body, ref ev);
        return ev.TotalDraw;
    }

    /// <summary>
    /// Update the draw rate for a power cell slot augment.
    /// </summary>
    public void UpdateDrawRate(Entity<PowerCellDrawComponent?> ent)
    {
        if (!_drawQuery.Resolve(ent, ref ent.Comp))
            return;

        var rate = Augment.GetBody(ent) is {} body
            ? GetBodyDraw(body)
            : 0f;
        if (ent.Comp.DrawRate == rate)
            return;

        ent.Comp.DrawRate = rate;
        Dirty(ent, ent.Comp);
    }

    /// <summary>
    /// Get a body's power cell slot augment, or null if it has none.
    /// </summary>
    public Entity<AugmentPowerCellSlotComponent>? GetBodyAugment(EntityUid body)
    {
        foreach (var augment in _body.GetBodyOrganEntityComps<AugmentPowerCellSlotComponent>(body))
        {
            return (augment.Owner, augment.Comp1);
        }

        return null;
    }

    /// <summary>
    /// Gets a power cell for a body if it both:
    /// 1. has a power cell slot augment
    /// 2. that augment has a power cell installed
    /// Returns null otherwise.
    /// </summary>
    public Entity<PredictedBatteryComponent>? GetBodyCell(EntityUid body)
    {
        if (GetBodyAugment(body) is not {} augment)
            return null;

        return _powerCell.TryGetBatteryFromSlot(augment.Owner, out var battery)
            ? battery
            : null;
    }

    /// <summary>
    /// Tries to use charge from a body's power cell slot augment.
    /// Does a popup for the user if it fails.
    /// </summary>
    public bool TryUseChargeBody(EntityUid body, float amount)
    {
        if (GetBodyAugment(body) is not {} slot)
        {
            _popup.PopupClient(Loc.GetString("augments-no-power-cell-slot"), body, body, PopupType.MediumCaution);
            return false;
        }

        if (!_powerCell.TryGetBatteryFromSlot(slot.Owner, out var battery))
        {
            _popup.PopupClient(Loc.GetString("power-cell-no-battery"), body, body, PopupType.MediumCaution);
            return false;
        }

        if (!_battery.TryUseCharge(battery.Value.AsNullable(), amount))
        {
            _popup.PopupClient(Loc.GetString("power-cell-insufficient"), body, body, PopupType.MediumCaution);
            return false;
        }

        return true;
    }
}
