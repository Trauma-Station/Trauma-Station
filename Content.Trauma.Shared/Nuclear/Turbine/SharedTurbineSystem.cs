// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Administration.Logs;
using Content.Shared.Construction.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.DeviceNetwork;
using Content.Shared.Electrocution;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Repairable;
using Content.Shared.Tools.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;

namespace Content.Trauma.Shared.Nuclear.Turbine;

public abstract partial class SharedTurbineSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] protected SharedAudioSystem Audio = default!;
    [Dependency] private SharedDeviceLinkSystem _device = default!;
    [Dependency] protected SharedPopupSystem Popup = default!;
    [Dependency] private SharedToolSystem _tool = default!;
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private EntityQuery<NuclearPropertiesComponent> _propsQuery = default!;

    private const string BladeContainer = "blade_slot";
    private const string StatorContainer = "stator_slot";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TurbineComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<TurbineComponent, ExaminedEvent>(OnExamined);

        SubscribeLocalEvent<TurbineComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<TurbineComponent, RepairDoAfterEvent>(OnRepairDoAfter);

        SubscribeLocalEvent<TurbineComponent, ItemSlotInsertAttemptEvent>(OnInsertAttempt);
        SubscribeLocalEvent<TurbineComponent, ItemSlotEjectAttemptEvent>(OnEjectAttempt);
        SubscribeLocalEvent<TurbineComponent, EntInsertedIntoContainerMessage>(OnPartInserted);
        SubscribeLocalEvent<TurbineComponent, EntRemovedFromContainerMessage>(OnPartEjected);

        SubscribeLocalEvent<TurbineComponent, SignalReceivedEvent>(OnSignalReceived);

        SubscribeLocalEvent<TurbineComponent, UnanchorAttemptEvent>(OnUnanchorAttempt);
    }

    private void OnInit(Entity<TurbineComponent> ent, ref ComponentInit args)
    {
        _device.EnsureSourcePorts(ent.Owner, ent.Comp.SpeedPort, ent.Comp.SpeedHighPort, ent.Comp.SpeedLowPort);
        _device.EnsureSinkPorts(ent.Owner, ent.Comp.StatorLoadPort, ent.Comp.FlowRatePort);
    }

    private void OnExamined(Entity<TurbineComponent> ent, ref ExaminedEvent args)
    {
        var comp = ent.Comp;
        if (!Transform(ent).Anchored || !args.IsInDetailsRange) // Not anchored? Out of range? No status.
            return;

        using (args.PushGroup(nameof(TurbineComponent)))
        {
            if (comp.CurrentStator == null)
                args.PushMarkup("It seems to be missing a stator.");

            if (comp.CurrentBlade == null)
                args.PushMarkup("It seems to be missing blades.");
            else
            {
                args.PushMarkup(comp.RPM switch
                {
                    <= 1f => "The blades are not spinning.",
                    <= 60f => "The blades are turning slowly.",
                    _ when comp.RPM <= comp.BestRPM * 0.5 => "The blades are spinning.",
                    _ when comp.RPM <= comp.BestRPM * 1.2 => "The blades are spinning quickly.",
                    _ => "[color=red]The blades are spinning out of control![/color]"
                });
            }

            if (comp.Ruined)
            {
                args.PushMarkup("[color=red]It's completely broken![/color]");
            }
            else
            {
                var health = (float) comp.BladeHealth / comp.BladeHealthMax;
                args.PushMarkup(health switch
                {
                    < 0.25f => "[color=orange]It's critically damaged![/color]",
                    < 0.5f => "[color=yellow]The turbine looks badly damaged.[/color]",
                    < 0.75f => "The turbine looks a bit scuffed.",
                    _ => "It appears to be in good condition."
                });
            }
        }
    }

    protected void UpdateAppearance(EntityUid uid, TurbineComponent? comp = null, AppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref comp, ref appearance, false))
            return;

        _appearance.SetData(uid, TurbineVisuals.TurbineRuined, comp.Ruined, appearance);

        _appearance.SetData(uid, TurbineVisuals.DamageSpark, comp.IsSparking, appearance);
        _appearance.SetData(uid, TurbineVisuals.DamageSmoke, comp.IsSmoking, appearance);
    }

    #region Repairs
    private void OnInteractUsing(EntityUid uid, TurbineComponent comp, ref InteractUsingEvent args)
    {
        if (args.Handled || !_tool.HasQuality(args.Used, comp.RepairTool))
            return;

        args.Handled = true;

        var user = args.User;
        if (comp.CurrentBlade == null)
        {
            Popup.PopupClient(Loc.GetString("gas-turbine-repair-fail-blade"), user, user, PopupType.MediumCaution);
            return;
        }

        if (comp.CurrentStator == null)
        {
            Popup.PopupClient(Loc.GetString("gas-turbine-repair-fail-stator"), user, user, PopupType.MediumCaution);
            return;
        }

        if (comp.BladeHealth >= comp.BladeHealthMax && !comp.Ruined)
        {
            Popup.PopupClient("The blade is already in perfect condition.", user, user);
            return;
        }

        _tool.UseTool(args.Used, user, uid, comp.RepairDelay, comp.RepairTool, new RepairDoAfterEvent(), comp.RepairFuelCost);
    }

    private void OnRepairDoAfter(Entity<TurbineComponent> ent, ref RepairDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        if (ent.Comp.Ruined)
        {
            SetRuined(ent, false);
            if (ent.Comp.BladeHealth <= 0)
            {
                ent.Comp.BladeHealth = 1;
                DirtyField(ent, ent.Comp, nameof(TurbineComponent.BladeHealth));
            }
            UpdateHealthIndicators(ent, args.User);
        }
        else if (ent.Comp.BladeHealth < ent.Comp.BladeHealthMax)
        {
            ent.Comp.BladeHealth++;
            DirtyField(ent, ent.Comp, nameof(TurbineComponent.BladeHealth));
            UpdateHealthIndicators(ent, args.User);
        }

        Popup.PopupClient(Loc.GetString("turbine-repair", ("target", ent), ("tool", args.Used!)), ent, args.User);
        _damage.ClearAllDamage(ent.Owner);
    }

    private void OnEjectAttempt(EntityUid uid, TurbineComponent comp, ref ItemSlotEjectAttemptEvent args)
    {
        args.Cancelled |= comp.RPM >= 1;
    }

    private void OnInsertAttempt(EntityUid uid, TurbineComponent comp, ref ItemSlotInsertAttemptEvent args)
    {
        args.Cancelled |= comp.RPM >= 1;
    }

    private void OnPartInserted(Entity<TurbineComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        switch (args.Container.ID)
        {
            case BladeContainer:
                ent.Comp.CurrentBlade = args.Entity;
                break;
            case StatorContainer:
                ent.Comp.CurrentStator = args.Entity;
                break;
            default:
                return;
        }
        UpdatePartValues(ent);
    }

    private void OnPartEjected(Entity<TurbineComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        switch (args.Container.ID)
        {
            case BladeContainer:
                ent.Comp.CurrentBlade = null;
                break;
            case StatorContainer:
                ent.Comp.CurrentStator = null;
                break;
            default:
                return;
        }
        UpdatePartValues(ent);
    }

    private void OnSignalReceived(Entity<TurbineComponent> ent, ref SignalReceivedEvent args)
    {
        int value = 0;
        if (args.Data?.TryGetValue("logic_int", out value) != true)
            return; // ignore non circuits

        if (args.Port == ent.Comp.StatorLoadPort)
            SetStatorLoad(ent, (float) value);
        else if (args.Port == ent.Comp.FlowRatePort)
            SetFlowRate(ent, (float) value);
    }

    private void OnUnanchorAttempt(Entity<TurbineComponent> ent, ref UnanchorAttemptEvent args)
    {
        if (ent.Comp.RPM < 1)
            return;

        Popup.PopupClient(Loc.GetString("turbine-unanchor-warning"), args.User, args.User, PopupType.LargeCaution);
        args.Cancel();
    }

    private void UpdatePartValues(Entity<TurbineComponent> ent)
    {
        if (_propsQuery.TryComp(ent.Comp.CurrentBlade, out var blade))
        {
            ent.Comp.TurbineMass = Math.Max(200, 200 * blade.Density);
            ent.Comp.BladeHealthMax = (int)Math.Max(1, 5 * blade.Hardness);
            ent.Comp.BladeHealth = ent.Comp.BladeHealthMax;
        }

        if (_propsQuery.TryComp(ent.Comp.CurrentStator, out var stator))
        {
            ent.Comp.PowerMultiplier = (float)Math.Max(0.2, 0.2 * stator.ElectricalConductivity);
        }
    }

    protected void UpdateHealthIndicators(Entity<TurbineComponent> ent, EntityUid? user = null)
    {
        var (uid, comp) = ent;
        if (comp.BladeHealth <= 0.75 * comp.BladeHealthMax && !comp.IsSparking)
        {
            comp.IsSparking = true;
            Audio.PlayPredicted(new SoundPathSpecifier("/Audio/Effects/PowerSink/electric.ogg"), uid, user, AudioParams.Default.WithPitchScale(0.75f));
            Popup.PopupPredicted(Loc.GetString("turbine-spark", ("owner", uid)), uid, user, PopupType.MediumCaution);
        }
        else if (comp.BladeHealth > 0.75 * comp.BladeHealthMax && comp.IsSparking)
        {
            comp.IsSparking = false;
            Popup.PopupPredicted(Loc.GetString("turbine-spark-stop", ("owner", uid)), uid, user, PopupType.Medium);
        }

        if (comp.BladeHealth <= 0.5 * comp.BladeHealthMax && !comp.IsSmoking)
        {
            comp.IsSmoking = true;
            Popup.PopupPredicted(Loc.GetString("turbine-smoke", ("owner", uid)), uid, user, PopupType.MediumCaution);
        }
        else if (comp.BladeHealth > 0.5 * comp.BladeHealthMax && comp.IsSmoking)
        {
            comp.IsSmoking = false;
            Popup.PopupPredicted(Loc.GetString("turbine-smoke-stop", ("owner", uid)), uid, user, PopupType.Medium);
        }

        EnsureComp<ElectrifiedComponent>(uid).Enabled = comp.IsSparking;

        UpdateAppearance(uid, comp);
    }

    #endregion

    public bool AdjustStatorLoad(Entity<TurbineComponent> ent, float change)
        => SetStatorLoad(ent, ent.Comp.StatorLoad + change);

    public bool SetStatorLoad(Entity<TurbineComponent> ent, float load)
    {
        load = Math.Clamp(load, ent.Comp.MinStatorLoad, ent.Comp.MaxStatorLoad);
        if (ent.Comp.StatorLoad == load)
            return false;

        ent.Comp.StatorLoad = load;
        DirtyField(ent, ent.Comp, nameof(TurbineComponent.StatorLoad));
        return true;
    }

    public bool SetFlowRate(Entity<TurbineComponent> ent, float rate)
    {
        rate = Math.Clamp(rate, 0, ent.Comp.FlowRateMax);
        if (ent.Comp.FlowRate == rate)
            return false;

        ent.Comp.FlowRate = rate;
        DirtyField(ent, ent.Comp, nameof(TurbineComponent.FlowRate));
        return true;
    }

    public void SetRPM(Entity<TurbineComponent> ent, float rpm)
    {
        if (ent.Comp.RPM == rpm)
            return;

        ent.Comp.RPM = rpm;
        DirtyField(ent, ent.Comp, nameof(TurbineComponent.RPM));

        // update rpm on integer changes only, thats circuit resolution
        int floored = (int) Math.Floor(rpm);
        if (ent.Comp.LastSentSpeed == floored)
            return;

        ent.Comp.LastSentSpeed = floored;
        var payload = new NetworkPayload();
        payload["logic_int"] = floored;
        _device.InvokePort(ent, ent.Comp.SpeedPort, payload);

        // update high/low speed ports if they change
        var high = rpm > ent.Comp.BestRPM * 1.05;
        var low = rpm < ent.Comp.BestRPM * 0.95;
        if (ent.Comp.LastSentHigh != high)
        {
            ent.Comp.LastSentHigh = high;
            _device.SendSignal(ent, ent.Comp.SpeedHighPort, high);
        }
        if (ent.Comp.LastSentLow != low)
        {
            ent.Comp.LastSentLow = low;
            _device.SendSignal(ent, ent.Comp.SpeedLowPort, rpm < ent.Comp.BestRPM * 0.95);
        }
    }

    public void SetLastGen(Entity<TurbineComponent> ent, float value)
    {
        var gen = (int) Math.Floor(value);
        if (ent.Comp.LastGen == gen)
            return;

        ent.Comp.LastGen = gen;
        DirtyField(ent, ent.Comp, nameof(TurbineComponent.LastGen));

        var payload = new NetworkPayload();
        payload["logic_int"] = gen;
        _device.InvokePort(ent, ent.Comp.PowerGenPort, payload);
    }

    public void SetPowerSupply(Entity<TurbineComponent> ent, int supply)
    {
        if (ent.Comp.PowerSupply == supply)
            return;

        ent.Comp.PowerSupply = supply;
        DirtyField(ent, ent.Comp, nameof(TurbineComponent.PowerSupply));

        var payload = new NetworkPayload();
        payload["logic_int"] = supply;
        _device.InvokePort(ent, ent.Comp.PowerSupplyPort, payload);
    }

    public void SetRuined(Entity<TurbineComponent> ent, bool ruined = true)
    {
        if (ent.Comp.Ruined == ruined)
            return;

        ent.Comp.Ruined = ruined;
        DirtyField(ent, ent.Comp, nameof(TurbineComponent.Ruined));
    }

    public void SetStalling(Entity<TurbineComponent> ent, bool stalling = true)
    {
        if (ent.Comp.Stalling == stalling)
            return;

        ent.Comp.Stalling = stalling;
        DirtyField(ent, ent.Comp, nameof(TurbineComponent.Stalling));
    }

    public void SetOverspeed(Entity<TurbineComponent> ent, bool overspeed = true)
    {
        if (ent.Comp.Overspeed == overspeed)
            return;

        ent.Comp.Overspeed = overspeed;
        DirtyField(ent, ent.Comp, nameof(TurbineComponent.Overspeed));
    }

    public void SetOvertemp(Entity<TurbineComponent> ent, bool overtemp = true)
    {
        if (ent.Comp.Overtemp == overtemp)
            return;

        ent.Comp.Overtemp = overtemp;
        DirtyField(ent, ent.Comp, nameof(TurbineComponent.Overtemp));
    }

    public void SetUndertemp(Entity<TurbineComponent> ent, bool undertemp = true)
    {
        if (ent.Comp.Undertemp == undertemp)
            return;

        ent.Comp.Undertemp = undertemp;
        DirtyField(ent, ent.Comp, nameof(TurbineComponent.Undertemp));
    }
}
