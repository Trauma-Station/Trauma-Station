// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.FixedPoint;
using Content.Trauma.Shared.Xenomorphs.Plasma.Components;
using Content.Shared.Alert;

namespace Content.Trauma.Shared.Xenomorphs.Plasma;

public abstract partial class SharedPlasmaSystem : EntitySystem
{
    [Dependency] private AlertsSystem _alerts = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PlasmaVesselComponent, ComponentShutdown>(OnPlasmaVesselShutdown);
        SubscribeLocalEvent<PlasmaVesselComponent, TransferPlasmaActionEvent>(OnPlasmaTransfer);
    }

    private void OnPlasmaVesselShutdown(EntityUid uid, PlasmaVesselComponent component, ComponentShutdown args) =>
        _alerts.ClearAlert(uid, component.PlasmaAlert);

    private void OnPlasmaTransfer(EntityUid uid, PlasmaVesselComponent component, TransferPlasmaActionEvent args)
    {
        if (args.Handled
            || !TryComp<PlasmaVesselComponent>(args.Target, out var plasmaVesselTarget)
            || !ChangePlasmaAmount(uid, -args.Amount, component))
            return;

        ChangePlasmaAmount(args.Target, args.Amount, plasmaVesselTarget);

        args.Handled = true;
    }

    public bool ChangePlasmaAmount(EntityUid uid, FixedPoint2 amount, PlasmaVesselComponent? component = null)
    {
        if (!Resolve(uid, ref component) || component.Plasma + amount < 0)
            return false;

        component.Plasma = FixedPoint2.Min(component.Plasma + amount, component.MaxPlasma);
        Dirty(uid, component);

        var ev = new PlasmaAmountChangeEvent(component.Plasma);
        RaiseLocalEvent(uid, ref ev);

        _alerts.ShowAlert(uid, component.PlasmaAlert);

        return true;
    }

    /// <summary>
    /// Goobstation - checks if a mob has at least a certain amount of plasma.
    /// </summary>
    public bool HasPlasma(EntityUid uid, FixedPoint2 amount)
        => TryComp<PlasmaVesselComponent>(uid, out var comp)
            && comp.Plasma >= amount;
}
