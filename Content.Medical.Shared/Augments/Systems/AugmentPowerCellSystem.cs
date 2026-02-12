using Content.Medical.Common.Body;
using Content.Shared.Alert;
using Content.Shared.Body;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Medical.Shared.Augments;

public sealed class AugmentPowerCellSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly AugmentSystem _augment = default!;
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MobStateSystem _mob = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly SharedBatterySystem _battery = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private EntityQuery<PowerCellDrawComponent> _drawQuery;

    private TimeSpan _nextUpdate = TimeSpan.Zero;
    private static readonly TimeSpan _updateDelay = TimeSpan.FromSeconds(2);

    public static readonly ProtoId<OrganCategoryPrototype> SlotCategory = "AugmentPowerCell";

    public override void Initialize()
    {
        base.Initialize();

        _drawQuery = GetEntityQuery<PowerCellDrawComponent>();

        SubscribeLocalEvent<AugmentPowerCellSlotComponent, OrganDisabledEvent>(OnOrganDisabled);
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

    private void OnOrganEnabled(Entity<AugmentPowerCellSlotComponent> ent, ref OrganEnabledEvent args)
    {
        if (!_drawQuery.TryComp(ent, out var draw))
            return;

        var drawEnt = (ent.Owner, draw);
        UpdateDrawRate(drawEnt, args.Body);

        _powerCell.SetDrawEnabled(drawEnt, true);
        if (!_powerCell.HasDrawCharge(drawEnt))
            return;

        foreach (var augment in _augment.GetAugments(args.Body))
        {
            _body.EnableOrgan(augment);
        }
    }

    private void OnOrganDisabled(Entity<AugmentPowerCellSlotComponent> ent, ref OrganDisabledEvent args)
    {
        if (!_drawQuery.TryComp(ent, out var draw))
            return;

        var drawEnt = (ent.Owner, draw);
        UpdateDrawRate(drawEnt, args.Body);

        _powerCell.SetDrawEnabled(drawEnt, false);
        // disable every augment that needs power
        foreach (var augment in _augment.GetAugments(args.Body))
        {
            if (_drawQuery.HasComp(augment))
                _body.DisableOrgan(augment);
        }
    }

    private void OnCellEmpty(Entity<AugmentPowerCellSlotComponent> ent, ref PowerCellSlotEmptyEvent args)
    {
        if (_body.GetBody(ent) is not {} body)
            return;

        foreach (var augment in _augment.GetAugments(body))
        {
            if (_drawQuery.HasComp(augment))
                _body.DisableOrgan(augment);
        }

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
        var draw = _drawQuery.CompOrNull(augment)?.DrawRate ?? 0f;
        _popup.PopupClient(Loc.GetString("augments-power-cell-info", ("percent", percent), ("draw", draw)), user, user);
    }

    public float GetBodyDraw(EntityUid body)
    {
        var ev = new GetAugmentsPowerDrawEvent(body);
        _augment.RelayEvent(body, ref ev);
        return ev.TotalDraw;
    }

    /// <summary>
    /// Update the draw rate for a power cell slot augment.
    /// </summary>
    public void UpdateDrawRate(Entity<PowerCellDrawComponent?> ent, EntityUid? bodyUid = null)
    {
        if (!_drawQuery.Resolve(ent, ref ent.Comp))
            return;

        bodyUid ??= _body.GetBody(ent.Owner);
        var rate = bodyUid is {} body
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
        => _body.GetOrgan(body, SlotCategory) is {} organ && TryComp<AugmentPowerCellSlotComponent>(organ, out var slot)
            ? (organ, slot)
            : null;

    /// <summary>
    /// Gets a power cell for a body if it both:
    /// 1. has a power cell slot augment
    /// 2. that augment has a power cell installed
    /// Returns null otherwise.
    /// </summary>
    public Entity<BatteryComponent>? GetBodyCell(EntityUid body)
        => GetBodyAugment(body) is {} augment && _powerCell.TryGetBatteryFromSlot(augment.Owner, out var battery)
            ? battery
            : null;

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
