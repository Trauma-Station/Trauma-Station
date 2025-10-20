using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.SprayPainter.Components;
using Content.Shared.Whitelist; // Trauma

namespace Content.Shared.SprayPainter;

/// <summary>
/// The system handles interactions with spray painter ammo.
/// </summary>
public sealed class SprayPainterAmmoSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!; // Trauma
    [Dependency] private readonly SharedChargesSystem _charges = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SprayPainterAmmoComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<SprayPainterAmmoComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnAfterInteract(Entity<SprayPainterAmmoComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        if (args.Target is not { Valid: true } target ||
            _whitelist.IsWhitelistFail(ent.Comp.Whitelist, target) || // Trauma - use whitelist instead of hardcoded component check
            !TryComp<LimitedChargesComponent>(target, out var charges))
            return;

        var user = args.User;
        args.Handled = true;
        var count = Math.Min(charges.MaxCharges - charges.LastCharges, ent.Comp.Charges);
        if (count <= 0)
        {
            _popup.PopupClient(Loc.GetString("spray-painter-ammo-after-interact-full"), target, user);
            return;
        }

        _popup.PopupClient(Loc.GetString("spray-painter-ammo-after-interact-refilled"), target, user);
        _charges.AddCharges(target, count);
        ent.Comp.Charges -= count;
        Dirty(ent, ent.Comp);

        if (ent.Comp.Charges <= 0)
            PredictedQueueDel(ent.Owner);
    }

    private void OnExamine(Entity<SprayPainterAmmoComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var examineMessage = Loc.GetString("rcd-ammo-component-on-examine", ("charges", ent.Comp.Charges));
        args.PushText(examineMessage);
    }
}
