// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Charges.Systems;
using Content.Shared.Cloning.Events;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Sprite;
using Content.Shared.Stacks;
using Content.Trauma.Common.Polymorph;
using Content.Trauma.Shared.Tools;

namespace Content.Trauma.Shared.Paint;

public sealed partial class PaintSystem : EntitySystem
{
    [Dependency] private OpenableSystem _openable = default!;
    [Dependency] private SharedChargesSystem _charges = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private EntityQuery<PaintCanComponent> _query = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PaintCanComponent, EffectsToolUseAttemptEvent>(OnUseAttempt);
        SubscribeLocalEvent<PaintCanComponent, EffectsToolUsedEvent>(OnUsed);

        SubscribeLocalEvent<PaintVisualsComponent, StackSplitEvent>(OnStackSplit);
        SubscribeLocalEvent<PaintVisualsComponent, PaintAttemptEvent>(OnRepaintAttempt);
        SubscribeLocalEvent<PaintVisualsComponent, CloningItemEvent>(OnClonePaint);
        SubscribeLocalEvent<PaintVisualsComponent, ChameleonDisguisedEvent>(OnPaintDisguised);
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

    private void OnStackSplit(Entity<PaintVisualsComponent> ent, ref StackSplitEvent args)
    {
        Paint(args.NewId, ent.Comp.Color);
    }

    private void OnRepaintAttempt(Entity<PaintVisualsComponent> ent, ref PaintAttemptEvent args)
    {
        // only allow repainting to change color
        if (ent.Comp.Color != args.Color)
            return;

        _popup.PopupClient("It's already painted that color.", ent, args.User);
        args.Cancelled = true;
    }

    private void OnClonePaint(Entity<PaintVisualsComponent> ent, ref CloningItemEvent args)
    {
        Paint(args.CloneUid, ent.Comp.Color);
    }

    private void OnPaintDisguised(Entity<PaintVisualsComponent> ent, ref ChameleonDisguisedEvent args)
    {
        Paint(args.Disguise, ent.Comp.Color);
    }

    private void OnRandomSpritePaintAttempt(Entity<RandomSpriteComponent> ent, ref PaintAttemptEvent args)
    {
        // no painting fish or whatever?
        _popup.PopupClient("It's already colorful enough.", ent, args.User);
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

        Paint(target, comp.Color);
        return true;
    }

    public void Paint(EntityUid target, Color color)
    {
        var painted = EnsureComp<PaintVisualsComponent>(target);
        SetColor((target, painted), color);
    }

    public void SetColor(Entity<PaintVisualsComponent> ent, Color color)
    {
        if (ent.Comp.Color == color)
            return;

        ent.Comp.Color = color;
        Dirty(ent);
        var ev = new PaintedEvent();
        RaiseLocalEvent(ent, ref ev);
    }

    #endregion
}
