using Content.Shared.Charges.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Sprite;
using Content.Trauma.Shared.Tools;

namespace Content.Trauma.Shared.Paint;

public sealed class PaintSystem : EntitySystem
{
    [Dependency] private readonly OpenableSystem _openable = default!;
    [Dependency] private readonly SharedChargesSystem _charges = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private EntityQuery<PaintCanComponent> _query;

    public override void Initialize()
    {
        base.Initialize();

        _query = GetEntityQuery<PaintCanComponent>();

        SubscribeLocalEvent<PaintCanComponent, EffectsToolUseAttemptEvent>(OnUseAttempt);
        SubscribeLocalEvent<PaintCanComponent, EffectsToolUsedEvent>(OnUsed);

        SubscribeLocalEvent<PaintVisualsComponent, PaintAttemptEvent>(OnRepaintAttempt);
        SubscribeLocalEvent<RandomSpriteComponent, PaintAttemptEvent>(OnRandomSpritePaintAttempt);
    }

    private void OnUseAttempt(Entity<PaintCanComponent> ent, ref EffectsToolUseAttemptEvent args)
    {
        if (!CanPaintPopup(ent, args.Target, args.User))
            args.Cancelled = true;
    }

    private void OnUsed(Entity<PaintCanComponent> ent, ref EffectsToolUsedEvent args)
    {
        _charges.TryUseCharge(ent.Owner);
    }

    private void OnRepaintAttempt(Entity<PaintVisualsComponent> ent, ref PaintAttemptEvent args)
    {
        // only allow repainting to change color
        if (ent.Comp.Color != args.Color)
            return;

        _popup.PopupClient(Loc.GetString("spray-paint-same"), ent, args.User);
        args.Cancelled = true;
    }

    private void OnRandomSpritePaintAttempt(Entity<RandomSpriteComponent> ent, ref PaintAttemptEvent args)
    {
        // no painting fish or whatever?
        _popup.PopupClient(Loc.GetString("spray-paint-fish"), ent, args.User);
        args.Cancelled = true;
    }

    #region Public API

    public bool CanPaintPopup(Entity<PaintCanComponent> ent, EntityUid target, EntityUid user)
    {
        if (_openable.IsClosed(ent.Owner))
        {
            _popup.PopupClient(Loc.GetString("spray-paint-closed", ("can", ent)), ent, user);
            return false;
        }

        var attemptEv = new PaintAttemptEvent(ent, ent.Comp.Color, user);
        RaiseLocalEvent(target, ref attemptEv);
        if (attemptEv.Cancelled)
            return false;

        if (!_charges.HasCharges(ent.Owner, 1))
        {
            _popup.PopupClient(Loc.GetString("spray-paint-empty", ("can", ent)), ent, user);
            return false;
        }

        return true;
    }

    public bool TryPaint(EntityUid tool, EntityUid target)
    {
        if (!_query.TryComp(tool, out var comp))
            return false;

        var painted = EnsureComp<PaintVisualsComponent>(target);
        SetColor((target, painted), comp.Color);
        return true;
    }

    public void SetColor(Entity<PaintVisualsComponent> ent, Color color)
    {
        var client = IoCManager.Resolve<Robust.Shared.Network.INetManager>().IsClient;
        Log.Debug($"client {client} - SetColor {ToPrettyString(ent)} to {color}");
        if (ent.Comp.Color == color)
            return;

        ent.Comp.Color = color;
        Dirty(ent);
        var ev = new PaintedEvent();
        RaiseLocalEvent(ent, ref ev);
    }

    #endregion
}
